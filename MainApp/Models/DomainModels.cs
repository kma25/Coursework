namespace MainApp.Models;

/// <summary>
/// Роль пользователя в консольной системе.
/// </summary>
public enum UserRole
{
    User,
    Admin,
    Statistician
}

/// <summary>
/// Вспомогательные операции для ролей пользователей.
/// </summary>
public static class UserRoleExtensions
{
    /// <summary>
    /// Преобразует текстовую роль из БД или команды в перечисление.
    /// </summary>
    public static bool TryParseRole(string? value, out UserRole role)
    {
        role = UserRole.User;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "user":
                role = UserRole.User;
                return true;
            case "admin":
                role = UserRole.Admin;
                return true;
            case "statistician":
                role = UserRole.Statistician;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Возвращает имя роли в формате, который используется в таблице roles.
    /// </summary>
    public static string ToDatabaseName(this UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Statistician => "statistician",
        _ => "user"
    };
}

/// <summary>
/// Пользователь приложения.
/// </summary>
public sealed record User(int Id, string Login, string PasswordHash, UserRole Role, bool IsBlocked);

/// <summary>
/// Активная пользовательская сессия с рабочей строкой подключения роли.
/// </summary>
public sealed record UserSession(User User, string RoleConnectionString)
{
    /// <summary>
    /// Проверяет, входит ли пользователь в одну из разрешенных ролей.
    /// </summary>
    public bool IsInRole(params UserRole[] roles) => roles.Contains(User.Role);
}

/// <summary>
/// Заметка пользователя.
/// </summary>
public sealed record Note(int Id, int UserId, string Text, DateTime CreatedAt, DateTime UpdatedAt);

/// <summary>
/// Запись журнала безопасности.
/// </summary>
public sealed record SecurityLogEntry(int Id, int? UserId, string EventType, string Details, DateTime CreatedAt);

/// <summary>
/// Устройство, с которого watcher отправляет метрики.
/// </summary>
public sealed record MonitoredDevice(int Id, string DeviceKey, string DeviceName, DateTime LastSeenAt);

/// <summary>
/// Снимок нагрузки CPU/RAM/HDD.
/// </summary>
public sealed record SystemMetric(
    int Id,
    int DeviceId,
    double CpuPercent,
    double RamPercent,
    double HddPercent,
    DateTime CreatedAt);

/// <summary>
/// Универсальный результат выполнения операции.
/// </summary>
public sealed record OperationResult(bool Success, string Message)
{
    /// <summary>
    /// Создает успешный результат.
    /// </summary>
    public static OperationResult Ok(string message) => new(true, message);

    /// <summary>
    /// Создает результат с контролируемой ошибкой.
    /// </summary>
    public static OperationResult Fail(string message) => new(false, message);
}
