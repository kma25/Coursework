using SystemWatcher.Models;

namespace SystemWatcher.Services
{
    /// <summary>
    /// Загружает плоский YAML-файл watcher.
    /// </summary>
    public static class WatcherConfiguration
    {
        /// <summary>
        /// Загружает настройки из watcher.yml.
        /// </summary>
        public static WatcherSettings Load(string path)
        {
            var values = LoadYaml(path);

            return new WatcherSettings
            {
                Enabled = GetBool(values, "enabled", true),
                IntervalSeconds = Math.Max(5, GetInt(values, "intervalSeconds", 60)),
                DeviceKey = Get(values, "deviceKey", Environment.MachineName),
                DeviceName = Get(values, "deviceName", Environment.MachineName),
                DatabaseTimeoutSeconds = Math.Max(1, GetInt(values, "databaseTimeoutSeconds", 10)),
                ConnectionString = Get(values, "connectionString", string.Empty)
            };
        }

        private static Dictionary<string, string> LoadYaml(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path))
            {
                return values;
            }

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var index = line.IndexOf(':');
                if (index <= 0)
                {
                    continue;
                }

                values[line[..index].Trim()] = line[(index + 1)..].Trim().Trim('"', '\'');
            }

            return values;
        }

        private static string Get(IReadOnlyDictionary<string, string> values, string key, string defaultValue)
            => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

        private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int defaultValue)
            => values.TryGetValue(key, out var value) && int.TryParse(value, out var number) ? number : defaultValue;

        private static bool GetBool(IReadOnlyDictionary<string, string> values, string key, bool defaultValue)
            => values.TryGetValue(key, out var value) && bool.TryParse(value, out var flag) ? flag : defaultValue;
    }
}
