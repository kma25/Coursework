using System.Xml.Linq;
using MainApp.Models;

namespace MainApp.Infrastructure;

/// <summary>
/// Загружает App.config и YAML-настройки без хранения секретов в коде.
/// </summary>
public static class AppConfiguration
{
    /// <summary>
    /// Загружает настройки приложения из App.config.
    /// </summary>
    public static AppSettings LoadAppSettings(string path = "App.config")
    {
        var values = LoadAppSettingsMap(path);

        return new AppSettings
        {
            DatabaseHost = Get(values, "DatabaseHost", "localhost"),
            DatabasePort = GetInt(values, "DatabasePort", 5432),
            DatabaseName = Get(values, "DatabaseName", "Coursework tester"),
            AuthUsername = Get(values, "AuthUsername", "app_auth"),
            AuthPassword = Get(values, "AuthPassword", "change_me"),
            Version = Get(values, "Version", "1.0.0"),
            UpdateConfigPath = Get(values, "UpdateConfigPath", "update.yml"),
            CheckUpdatesOnStartup = GetBool(values, "CheckUpdatesOnStartup", false),
            WatcherConfigPath = Get(values, "WatcherConfigPath", "watcher.yml"),
            WatcherExecutablePath = Get(values, "WatcherExecutablePath", "SystemWatcher.exe")
        };
    }

    /// <summary>
    /// Загружает настройки обновлений из YAML.
    /// </summary>
    public static UpdateSettings LoadUpdateSettings(string path)
    {
        var values = SimpleYamlParser.Load(path);
        return new UpdateSettings
        {
            UpdateOwner = SimpleYamlParser.Get(values, "updateOwner", "example"),
            UpdateRepo = SimpleYamlParser.Get(values, "updateRepo", "it-support"),
            UpdateAssetExtension = SimpleYamlParser.Get(values, "updateAssetExtension", ".zip"),
            UpdateHttpTimeoutSeconds = SimpleYamlParser.GetInt(values, "updateHttpTimeoutSeconds", 15)
        };
    }

    private static Dictionary<string, string> LoadAppSettingsMap(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
        {
            return values;
        }

        var document = XDocument.Load(path);
        foreach (var element in document.Descendants("add"))
        {
            var key = element.Attribute("key")?.Value;
            var value = element.Attribute("value")?.Value;
            if (!string.IsNullOrWhiteSpace(key) && value is not null)
            {
                values[key] = value;
            }
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
