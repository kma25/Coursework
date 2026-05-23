using MainApp.Infrastructure.Repositories;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Выполняет регистрацию, вход и создание пользовательской сессии.
/// </summary>
public sealed class AuthService
{
    private readonly AppSettings _settings;
    private readonly IUserRepository _users;
    private readonly ISecurityLogRepository _securityLogs;
    private readonly PasswordHasher _passwordHasher;

    /// <summary>
    /// Создает сервис авторизации.
    /// </summary>
    public AuthService(
        AppSettings settings,
        IUserRepository users,
        ISecurityLogRepository securityLogs,
        PasswordHasher passwordHasher)
    {
        _settings = settings;
        _users = users;
        _securityLogs = securityLogs;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Регистрирует нового пользователя.
    /// </summary>
    public async Task<OperationResult> RegisterAsync(string login, string password, string roleName = "user", CancellationToken cancellationToken = default)
    {
        var validation = ValidateCredentials(login, password);
        if (!validation.Success)
        {
            return validation;
        }

        if (!UserRoleExtensions.TryParseRole(roleName, out var role))
        {
            return OperationResult.Fail("Неизвестная роль пользователя.");
        }

        var authConnection = GetAuthConnectionString();
        var existingUser = await _users.FindByLoginAsync(login, authConnection, cancellationToken);
        if (existingUser is not null)
        {
            return OperationResult.Fail("Пользователь с таким логином уже существует.");
        }

        var users = await _users.GetAllAsync(authConnection, cancellationToken);
        if (users.Count == 0 && role == UserRole.User)
        {
            // В новой базе еще нет администратора, поэтому первый зарегистрированный пользователь
            // получает роль admin. Иначе управлять пользователями пришлось бы вручную через SQL.
            role = UserRole.Admin;
        }

        var createdUser = await _users.CreateAsync(login, _passwordHasher.Hash(password), role, authConnection, cancellationToken);
        await _securityLogs.AddAsync(createdUser.Id, "registration", $"Зарегистрирован пользователь {login}", authConnection, cancellationToken);

        return OperationResult.Ok(role == UserRole.Admin
            ? "Регистрация выполнена успешно. Первый пользователь создан с ролью admin."
            : "Регистрация выполнена успешно.");
    }

    /// <summary>
    /// Выполняет вход пользователя и создает рабочую сессию.
    /// </summary>
    public async Task<(OperationResult Result, UserSession? Session)> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return (OperationResult.Fail("Логин не может быть пустым."), null);
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return (OperationResult.Fail("Пароль не может быть пустым."), null);
        }

        var authConnection = GetAuthConnectionString();
        var user = await _users.FindByLoginAsync(login, authConnection, cancellationToken);
        if (user is null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            await _securityLogs.AddAsync(user?.Id, "login_failed", $"Неуспешный вход: {login}", authConnection, cancellationToken);
            return (OperationResult.Fail("Неверный логин или пароль."), null);
        }

        if (user.IsBlocked)
        {
            await _securityLogs.AddAsync(user.Id, "login_blocked", $"Попытка входа заблокированного пользователя: {login}", authConnection, cancellationToken);
            return (OperationResult.Fail("Учетная запись заблокирована."), null);
        }

        var roleConnectionFragment = await _users.GetRoleConnectionFragmentAsync(user.Role, authConnection, cancellationToken);
        var roleConnection = _settings.BuildRoleConnectionString(roleConnectionFragment);
        await _securityLogs.AddAsync(user.Id, "login_success", $"Успешный вход: {login}", authConnection, cancellationToken);

        return (OperationResult.Ok("Вход выполнен успешно."), new UserSession(user, roleConnection));
    }

    /// <summary>
    /// Логирует выход пользователя из системы.
    /// </summary>
    public Task LogoutAsync(UserSession session, CancellationToken cancellationToken = default)
        => _securityLogs.AddAsync(session.User.Id, "logout", $"Выход пользователя {session.User.Login}", GetAuthConnectionString(), cancellationToken);

    private OperationResult ValidateCredentials(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return OperationResult.Fail("Логин не может быть пустым.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return OperationResult.Fail("Пароль не может быть пустым.");
        }

        if (password.Length < 6)
        {
            return OperationResult.Fail("Пароль должен содержать не менее 6 символов.");
        }

        return OperationResult.Ok("Данные корректны.");
    }

    private string GetAuthConnectionString() => _settings.BuildConnectionString(_settings.AuthUsername, _settings.AuthPassword);
}
