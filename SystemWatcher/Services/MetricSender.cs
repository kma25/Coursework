using Npgsql;
using SystemWatcher.Models;

namespace SystemWatcher.Services
{
    /// <summary>
    /// Отправляет метрики watcher в PostgreSQL.
    /// </summary>
    public sealed class MetricSender
    {
        private readonly WatcherSettings _settings;

        /// <summary>
        /// Создает отправитель метрик.
        /// </summary>
        public MetricSender(WatcherSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Сохраняет метрику через функцию save_system_metric.
        /// </summary>
        public async Task SendAsync(MetricSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (_settings.ConnectionString.Contains("Username=postgres", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Watcher не должен подключаться к БД под postgres.");
            }

            await using var connection = new NpgsqlConnection(_settings.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                "select save_system_metric(@device_key, @device_name, @cpu, @ram, @hdd)",
                connection);
            command.Parameters.AddWithValue("device_key", _settings.DeviceKey);
            command.Parameters.AddWithValue("device_name", _settings.DeviceName);
            command.Parameters.AddWithValue("cpu", snapshot.CpuPercent);
            command.Parameters.AddWithValue("ram", snapshot.RamPercent);
            command.Parameters.AddWithValue("hdd", snapshot.HddPercent);
            command.CommandTimeout = _settings.DatabaseTimeoutSeconds;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
