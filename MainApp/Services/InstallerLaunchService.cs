using System.Diagnostics;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Запускает отдельный установщик обновлений.
/// </summary>
public sealed class InstallerLaunchService
{
    /// <summary>
    /// Запускает AppInstaller с URL архива обновления.
    /// </summary>
    public OperationResult Start(string downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return OperationResult.Fail("Не указан URL архива обновления.");
        }

        var startInfo = CreateStartInfo(downloadUrl);
        if (startInfo is null)
        {
            return OperationResult.Fail("AppInstaller не найден. Соберите решение или запустите установщик вручную.");
        }

        try
        {
            Process.Start(startInfo);
            return OperationResult.Ok("AppInstaller запущен. Закройте MainApp, чтобы установщик смог заменить файлы.");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Не удалось запустить AppInstaller: {ex.Message}");
        }
    }

    private static ProcessStartInfo? CreateStartInfo(string downloadUrl)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var exePath = Path.Combine(baseDirectory, "AppInstaller.exe");
        if (File.Exists(exePath))
        {
            return CreateProcessStartInfo(exePath, downloadUrl);
        }

        var dllPath = Path.Combine(baseDirectory, "AppInstaller.dll");
        if (File.Exists(dllPath))
        {
            var info = CreateDotnetStartInfo();
            info.ArgumentList.Add(dllPath);
            AddInstallerArguments(info, downloadUrl);
            return info;
        }

        var projectPath = FindAppInstallerProject();
        if (projectPath is not null)
        {
            // В режиме разработки AppInstaller лежит отдельным проектом, а не рядом с MainApp.exe.
            var info = CreateDotnetStartInfo();
            info.ArgumentList.Add("run");
            info.ArgumentList.Add("--project");
            info.ArgumentList.Add(projectPath);
            info.ArgumentList.Add("--");
            AddInstallerArguments(info, downloadUrl);
            return info;
        }

        return null;
    }

    private static ProcessStartInfo CreateProcessStartInfo(string fileName, string downloadUrl)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        AddInstallerArguments(info, downloadUrl);
        return info;
    }

    private static ProcessStartInfo CreateDotnetStartInfo() => new()
    {
        FileName = "dotnet",
        UseShellExecute = false,
        CreateNoWindow = false
    };

    private static void AddInstallerArguments(ProcessStartInfo info, string downloadUrl)
    {
        info.ArgumentList.Add("--download-url");
        info.ArgumentList.Add(downloadUrl);
        info.ArgumentList.Add("--target");
        info.ArgumentList.Add(AppContext.BaseDirectory);
        info.ArgumentList.Add("--main-pid");
        info.ArgumentList.Add(Environment.ProcessId.ToString());
        info.ArgumentList.Add("--app");
        info.ArgumentList.Add("MainApp.exe");
    }

    private static string? FindAppInstallerProject()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var projectPath = Path.Combine(directory.FullName, "AppInstaller", "AppInstaller.csproj");
            if (File.Exists(projectPath))
            {
                return projectPath;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
