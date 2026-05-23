namespace SystemWatcher.Models;

/// <summary>
/// Настройки watcher-агента из watcher.yml.
/// </summary>
public sealed class WatcherSettings
{
    public bool Enabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 60;
    public string DeviceKey { get; init; } = Environment.MachineName;
    public string DeviceName { get; init; } = Environment.MachineName;
    public int DatabaseTimeoutSeconds { get; init; } = 10;
    public string ConnectionString { get; init; } = string.Empty;
}

/// <summary>
/// Снимок метрик устройства.
/// </summary>
public sealed record MetricSnapshot(double CpuPercent, double RamPercent, double HddPercent, DateTime CreatedAt);
