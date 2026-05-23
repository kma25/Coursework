using MainApp.Infrastructure.Repositories;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Отдает журнал безопасности с проверкой роли.
/// </summary>
public sealed class SecurityLogService
{
    private readonly ISecurityLogRepository _securityLogs;

    /// <summary>
    /// Создает сервис журнала безопасности.
    /// </summary>
    public SecurityLogService(ISecurityLogRepository securityLogs)
    {
        _securityLogs = securityLogs;
    }

    /// <summary>
    /// Возвращает последние события безопасности.
    /// </summary>
    public async Task<(OperationResult Result, IReadOnlyList<SecurityLogEntry> Logs)> GetRecentAsync(
        UserSession session,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (session.User.Role != UserRole.Admin)
        {
            return (OperationResult.Fail("Журнал безопасности доступен только администратору."), Array.Empty<SecurityLogEntry>());
        }

        var logs = await _securityLogs.GetRecentAsync(Math.Clamp(count, 1, 200), session.RoleConnectionString, cancellationToken);
        return (OperationResult.Ok("Журнал безопасности получен."), logs);
    }
}
