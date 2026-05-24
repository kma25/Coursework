namespace SystemWatcher.Models;

/// <summary>
/// Настройки watcher-агента из watcher.yml.
/// </summary>
public sealed class WatcherSettings
{
    /// <summary>
    /// Управляет запуском агента без удаления конфигурационного файла.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Интервал между отправками метрик в секундах.
    /// </summary>
    public int IntervalSeconds { get; init; } = 60;

    /// <summary>
    /// Стабильный технический ключ устройства в таблице мониторинга.
    /// </summary>
    public string DeviceKey { get; init; } = Environment.MachineName;

    /// <summary>
    /// Отображаемое имя компьютера, которое видит пользователь в командах watch.
    /// </summary>
    public string DeviceName { get; init; } = Environment.MachineName;

    /// <summary>
    /// Таймаут операций записи метрик в PostgreSQL.
    /// </summary>
    public int DatabaseTimeoutSeconds { get; init; } = 10;

    /// <summary>
    /// Строка подключения роли watcher к базе данных.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}

/// <summary>
/// Снимок метрик устройства.
/// </summary>
/// <param name="CpuPercent">Текущая загрузка процессора в процентах.</param>
/// <param name="RamPercent">Текущая загрузка оперативной памяти в процентах.</param>
/// <param name="HddPercent">Текущая заполненность системного диска в процентах.</param>
/// <param name="CreatedAt">Момент, когда снимок был сформирован.</param>
public sealed record MetricSnapshot(double CpuPercent, double RamPercent, double HddPercent, DateTime CreatedAt);
