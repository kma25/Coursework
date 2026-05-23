using System.Text;
using SystemWatcher.Services;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var configPath = ResolveConfigPath(args);
var settings = WatcherConfiguration.Load(configPath);

if (!settings.Enabled)
{
    Console.WriteLine("Watcher отключен в watcher.yml.");
    return;
}

if (string.IsNullOrWhiteSpace(settings.ConnectionString))
{
    Console.WriteLine("В watcher.yml не задана connectionString.");
    return;
}

var collector = new MetricCollector();
var sender = new MetricSender(settings);

Console.WriteLine($"Watcher запущен для устройства {settings.DeviceName}. Интервал: {settings.IntervalSeconds} секунд.");

while (true)
{
    try
    {
        var snapshot = collector.Collect();
        await sender.SendAsync(snapshot);
        Console.WriteLine($"{snapshot.CreatedAt:g}: CPU {snapshot.CpuPercent:F1}%, RAM {snapshot.RamPercent:F1}%, HDD {snapshot.HddPercent:F1}%");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка watcher: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(settings.IntervalSeconds));
}

static string ResolveConfigPath(string[] args)
{
    var index = Array.FindIndex(args, value => value.Equals("--config", StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : "watcher.yml";
}
