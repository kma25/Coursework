using System.Diagnostics;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Отвечает за запуск и проверку watcher-агента.
/// </summary>
public sealed class WatcherProcessService
{
    private readonly AppSettings _settings;

    /// <summary>
    /// Создает сервис watcher-процесса.
    /// </summary>
    public WatcherProcessService(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Запускает watcher, если он еще не запущен.
    /// </summary>
    public OperationResult StartIfNeeded()
    {
        if (IsWatcherRunning())
        {
            return OperationResult.Ok("Watcher-агент уже запущен.");
        }

        if (!File.Exists(_settings.WatcherExecutablePath))
        {
            return OperationResult.Fail($"Watcher не найден: {_settings.WatcherExecutablePath}");
        }

        try
        {
            var info = new ProcessStartInfo
            {
                FileName = _settings.WatcherExecutablePath,
                Arguments = $"--config \"{_settings.WatcherConfigPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(info);
            return OperationResult.Ok("Watcher-агент запущен.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Не удалось запустить watcher: {ex.Message}");
        }
    }

    /// <summary>
    /// Проверяет наличие процесса SystemWatcher.
    /// </summary>
    public bool IsWatcherRunning()
    {
        var expectedName = Path.GetFileNameWithoutExtension(_settings.WatcherExecutablePath);
        return Process.GetProcessesByName(expectedName).Any();
    }
}
