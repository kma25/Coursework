using System.Diagnostics;
using MainApp.Models;

namespace MainApp.Services
{
    /// <summary>
    /// Отвечает за запуск и проверку watcher-агента.
    /// </summary>
    public sealed class WatcherProcessService
    {
        private const string WatcherProjectName = "SystemWatcher";
        private static readonly List<Process> StartedWatchers = [];
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
                return OperationResult.Ok("Watcher-агент уже запущен в фоновом режиме.");
            }

            var startInfo = CreateStartInfo();
            if (startInfo is null)
            {
                return OperationResult.Fail("Watcher не найден. Соберите SystemWatcher или проверьте путь в App.config.");
            }

            try
            {
                var process = Process.Start(startInfo);
                if (process is null)
                {
                    return OperationResult.Fail("Не удалось запустить watcher-процесс.");
                }

                TrackProcess(process);
                WritePid(process.Id);

                return OperationResult.Ok("Watcher-агент запущен в фоновом режиме.");
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
            if (StartedWatchers.Any(IsAlive))
            {
                return true;
            }

            var savedPid = ReadPid();
            if (savedPid is not null && IsProcessAlive(savedPid.Value))
            {
                return true;
            }

            var expectedName = Path.GetFileNameWithoutExtension(_settings.WatcherExecutablePath);
            return !string.IsNullOrWhiteSpace(expectedName) && Process.GetProcessesByName(expectedName).Any();
        }

        private ProcessStartInfo? CreateStartInfo()
        {
            var configPath = ResolveWatcherConfigPath();
            var executablePath = ResolveWatcherExecutablePath();
            if (executablePath is not null)
            {
                var info = CreateBackgroundStartInfo(executablePath);
                info.ArgumentList.Add("--config");
                info.ArgumentList.Add(configPath);
                return info;
            }

            var projectPath = FindSystemWatcherProject();
            if (projectPath is null)
            {
                return null;
            }

            var dotnet = CreateBackgroundStartInfo("dotnet");
            dotnet.ArgumentList.Add("run");
            dotnet.ArgumentList.Add("--project");
            dotnet.ArgumentList.Add(projectPath);
            dotnet.ArgumentList.Add("--");
            dotnet.ArgumentList.Add("--config");
            dotnet.ArgumentList.Add(configPath);
            return dotnet;
        }

        private static ProcessStartInfo CreateBackgroundStartInfo(string fileName) => new()
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        private string ResolveWatcherConfigPath()
        {
            var candidates = new[]
            {
                _settings.WatcherConfigPath,
                Path.Combine(AppContext.BaseDirectory, _settings.WatcherConfigPath),
                Path.Combine(Directory.GetCurrentDirectory(), _settings.WatcherConfigPath),
                FindSystemWatcherProject() is string projectPath
                    ? Path.Combine(Path.GetDirectoryName(projectPath)!, "watcher.yml")
                    : string.Empty
            };

            return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                ?? _settings.WatcherConfigPath;
        }

        private string? ResolveWatcherExecutablePath()
        {
            var candidates = new[]
            {
                _settings.WatcherExecutablePath,
                Path.Combine(AppContext.BaseDirectory, _settings.WatcherExecutablePath),
                Path.Combine(Directory.GetCurrentDirectory(), _settings.WatcherExecutablePath)
            };

            return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        }

        private static string? FindSystemWatcherProject()
        {
            var startDirectories = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var startDirectory in startDirectories)
            {
                var directory = new DirectoryInfo(startDirectory);
                while (directory is not null)
                {
                    var projectPath = Path.Combine(directory.FullName, WatcherProjectName, $"{WatcherProjectName}.csproj");
                    if (File.Exists(projectPath))
                    {
                        return projectPath;
                    }

                    directory = directory.Parent;
                }
            }

            return null;
        }

        private static void TrackProcess(Process process)
        {
            process.EnableRaisingEvents = true;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            StartedWatchers.Add(process);
        }

        private static bool IsAlive(Process process)
        {
            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static bool IsProcessAlive(int processId)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string PidFilePath
            => Path.Combine(Path.GetTempPath(), "Coursework", "system-watcher.pid");

        private static void WritePid(int processId)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PidFilePath)!);
            File.WriteAllText(PidFilePath, processId.ToString());
        }

        private static int? ReadPid()
        {
            return File.Exists(PidFilePath) && int.TryParse(File.ReadAllText(PidFilePath), out var processId)
                ? processId
                : null;
        }
    }
}
