namespace MainApp.Infrastructure;

/// <summary>
/// Минимальный YAML-парсер для плоских конфигурационных файлов key: value.
/// </summary>
public static class SimpleYamlParser
{
    /// <summary>
    /// Загружает YAML-файл в словарь значений.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Load(string path)
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

            var key = line[..index].Trim();
            var value = line[(index + 1)..].Trim().Trim('"', '\'');
            values[key] = value;
        }

        return values;
    }

    /// <summary>
    /// Возвращает строковое значение или значение по умолчанию.
    /// </summary>
    public static string Get(IReadOnlyDictionary<string, string> values, string key, string defaultValue)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

    /// <summary>
    /// Возвращает числовое значение или значение по умолчанию.
    /// </summary>
    public static int GetInt(IReadOnlyDictionary<string, string> values, string key, int defaultValue)
        => values.TryGetValue(key, out var value) && int.TryParse(value, out var number) ? number : defaultValue;
}
