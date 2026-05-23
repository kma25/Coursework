using MainApp.Infrastructure.Repositories;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Административные операции с пользователями.
/// </summary>
public sealed class AdminUserService
{
    private readonly IUserRepository _users;
    private readonly ISecurityLogRepository _securityLogs;
    private readonly PasswordHasher _passwordHasher;

    /// <summary>
    /// Создает сервис администрирования.
    /// </summary>
    public AdminUserService(IUserRepository users, ISecurityLogRepository securityLogs, PasswordHasher passwordHasher)
    {
        _users = users;
        _securityLogs = securityLogs;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Создает пользователя администратором.
    /// </summary>
    public async Task<OperationResult> CreateUserAsync(UserSession session, string login, string password, string roleName, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(session, out var denied))
        {
            return denied;
        }

        if (!UserRoleExtensions.TryParseRole(roleName, out var role))
        {
            return OperationResult.Fail("Неизвестная роль пользователя.");
        }

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return OperationResult.Fail("Проверьте логин и пароль. Пароль должен быть не короче 6 символов.");
        }

        var created = await _users.CreateAsync(login, _passwordHasher.Hash(password), role, session.RoleConnectionString, cancellationToken);
        await _securityLogs.AddAsync(session.User.Id, "user_created", $"Создан пользователь {created.Login}", session.RoleConnectionString, cancellationToken);
        return OperationResult.Ok("Пользователь создан.");
    }

    /// <summary>
    /// Возвращает список пользователей.
    /// </summary>
    public async Task<(OperationResult Result, IReadOnlyList<User> Users)> ListUsersAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(session, out var denied))
        {
            return (denied, Array.Empty<User>());
        }

        return (OperationResult.Ok("Список пользователей получен."), await _users.GetAllAsync(session.RoleConnectionString, cancellationToken));
    }

    /// <summary>
    /// Возвращает пользователя по логину для административных команд.
    /// </summary>
    public async Task<(OperationResult Result, User? User)> GetUserAsync(UserSession session, string login, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(session, out var denied))
        {
            return (denied, null);
        }

        var user = await _users.FindByLoginAsync(login, session.RoleConnectionString, cancellationToken);
        return user is null
            ? (OperationResult.Fail("Пользователь не найден."), null)
            : (OperationResult.Ok("Пользователь найден."), user);
    }

    /// <summary>
    /// Блокирует пользователя.
    /// </summary>
    public Task<OperationResult> BlockAsync(UserSession session, string login, CancellationToken cancellationToken = default)
        => SetBlockedAsync(session, login, true, "user_blocked", "Пользователь заблокирован.", cancellationToken);

    /// <summary>
    /// Разблокирует пользователя.
    /// </summary>
    public Task<OperationResult> UnblockAsync(UserSession session, string login, CancellationToken cancellationToken = default)
        => SetBlockedAsync(session, login, false, "user_unblocked", "Пользователь разблокирован.", cancellationToken);

    /// <summary>
    /// Удаляет пользователя.
    /// </summary>
    public async Task<OperationResult> DeleteAsync(UserSession session, string login, CancellationToken cancellationToken = default)
    {
        if (!EnsureAdmin(session, out var denied))
        {
            return denied;
        }

        await _users.DeleteAsync(login, session.RoleConnectionString, cancellationToken);
        await _securityLogs.AddAsync(session.User.Id, "user_deleted", $"Удален пользователь {login}", session.RoleConnectionString, cancellationToken);
        return OperationResult.Ok("Пользователь удален.");
    }

    private async Task<OperationResult> SetBlockedAsync(
        UserSession session,
        string login,
        bool blocked,
        string eventType,
        string message,
        CancellationToken cancellationToken)
    {
        if (!EnsureAdmin(session, out var denied))
        {
            return denied;
        }

        await _users.SetBlockedAsync(login, blocked, session.RoleConnectionString, cancellationToken);
        await _securityLogs.AddAsync(session.User.Id, eventType, $"{message} Логин: {login}", session.RoleConnectionString, cancellationToken);
        return OperationResult.Ok(message);
    }

    private static bool EnsureAdmin(UserSession session, out OperationResult result)
    {
        if (session.User.Role == UserRole.Admin)
        {
            result = OperationResult.Ok("Доступ разрешен.");
            return true;
        }

        result = OperationResult.Fail("Команда доступна только администратору.");
        return false;
    }
}
