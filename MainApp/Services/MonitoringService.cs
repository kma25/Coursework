using MainApp.Infrastructure.Repositories;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Реализует просмотр данных watcher.
/// </summary>
public sealed class MonitoringService
{
    private readonly IMonitoringRepository _monitoring;
    private readonly WatcherProcessService _watcher;

    /// <summary>
    /// Создает сервис мониторинга.
    /// </summary>
    public MonitoringService(IMonitoringRepository monitoring, WatcherProcessService watcher)
    {
        _monitoring = monitoring;
        _watcher = watcher;
    }

    /// <summary>
    /// Возвращает список устройств мониторинга.
    /// </summary>
    public async Task<(OperationResult Result, IReadOnlyList<MonitoredDevice> Devices)> GetDevicesAsync(
        UserSession session,
        CancellationToken cancellationToken = default)
    {
        if (!CanViewMonitoring(session))
        {
            return (OperationResult.Fail("Мониторинг доступен только admin и statistician."), Array.Empty<MonitoredDevice>());
        }

        return (OperationResult.Ok("Устройства получены."), await _monitoring.GetDevicesAsync(session.RoleConnectionString, cancellationToken));
    }

    /// <summary>
    /// Возвращает историю метрик устройства.
    /// </summary>
    public async Task<(OperationResult Result, IReadOnlyList<SystemMetric> Metrics)> GetMetricsAsync(
        UserSession session,
        string deviceName,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (!CanViewMonitoring(session))
        {
            return (OperationResult.Fail("Мониторинг доступен только admin и statistician."), Array.Empty<SystemMetric>());
        }

        var metrics = await _monitoring.GetMetricsAsync(deviceName, Math.Clamp(count, 1, 200), session.RoleConnectionString, cancellationToken);
        return (OperationResult.Ok("История нагрузки получена."), metrics);
    }

    /// <summary>
    /// Проверяет, запущен ли watcher.
    /// </summary>
    public OperationResult GetWatcherStatus(UserSession session)
    {
        if (!CanViewMonitoring(session))
        {
            return OperationResult.Fail("Мониторинг доступен только admin и statistician.");
        }

        return _watcher.IsWatcherRunning()
            ? OperationResult.Ok("Watcher-агент запущен.")
            : OperationResult.Fail("Watcher-агент не найден среди запущенных процессов.");
    }

    private static bool CanViewMonitoring(UserSession session)
        => session.User.Role is UserRole.Admin or UserRole.Statistician;
}
