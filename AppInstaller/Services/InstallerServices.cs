using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace AppInstaller.Services;

/// <summary>
/// Получает URL ZIP-архива из GitHub Releases-совместимого ответа.
/// </summary>
public sealed class GitHubReleaseService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Создает сервис релизов.
    /// </summary>
    public GitHubReleaseService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CourseworkInstaller/1.0");
    }

    /// <summary>
    /// Возвращает ссылку на ZIP-архив релиза.
    /// </summary>
    public async Task<string> GetArchiveUrlAsync(string releaseApiUrl, string assetExtension, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(releaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        foreach (var asset in document.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (name.EndsWith(assetExtension, StringComparison.OrdinalIgnoreCase))
            {
                return asset.GetProperty("browser_download_url").GetString() ?? throw new InvalidOperationException("У архива нет URL загрузки.");
            }
        }

        throw new InvalidOperationException("В релизе не найден ZIP-архив.");
    }
}

/// <summary>
/// Скачивает архив обновления.
/// </summary>
public sealed class ReleaseDownloadService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Создает сервис загрузки.
    /// </summary>
    public ReleaseDownloadService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CourseworkInstaller/1.0");
    }

    /// <summary>
    /// Загружает ZIP-архив во временную папку.
    /// </summary>
    public async Task<string> DownloadAsync(string url, string tempDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(tempDirectory);
        var archivePath = Path.Combine(tempDirectory, "release.zip");

        await using var input = await _httpClient.GetStreamAsync(url, cancellationToken);
        await using var output = File.Create(archivePath);
        await input.CopyToAsync(output, cancellationToken);

        return archivePath;
    }
}

/// <summary>
/// Распаковывает архив обновления.
/// </summary>
public sealed class ArchiveExtractorService
{
    /// <summary>
    /// Распаковывает ZIP во временную папку.
    /// </summary>
    public string Extract(string archivePath, string tempDirectory)
    {
        var extractDirectory = Path.Combine(tempDirectory, "extracted");
        if (Directory.Exists(extractDirectory))
        {
            Directory.Delete(extractDirectory, recursive: true);
        }

        ZipFile.ExtractToDirectory(archivePath, extractDirectory);
        return extractDirectory;
    }
}

/// <summary>
/// Ожидает завершения основного приложения.
/// </summary>
public sealed class ProcessWaitService
{
    /// <summary>
    /// Ждет завершения процесса, если передан его PID.
    /// </summary>
    public async Task WaitAsync(int? processId, CancellationToken cancellationToken = default)
    {
        if (processId is null)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(processId.Value);
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (ArgumentException)
        {
            // Если пользователь закрыл приложение быстрее установщика, просто продолжаем обновление.
        }
    }
}

/// <summary>
/// Копирует файлы обновления в папку установки.
/// </summary>
public sealed class FileDeploymentService
{
    /// <summary>
    /// Копирует распакованный релиз поверх текущей установки.
    /// </summary>
    public void Deploy(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }
}

/// <summary>
/// Запускает приложение после обновления.
/// </summary>
public sealed class AppStarterService
{
    /// <summary>
    /// Запускает основной exe.
    /// </summary>
    public void Start(string targetDirectory, string executable)
    {
        var path = Path.Combine(targetDirectory, executable);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Основной файл не найден: {path}");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = targetDirectory,
            UseShellExecute = true
        });
    }
}

/// <summary>
/// Координирует полный сценарий установки обновления.
/// </summary>
public sealed class InstallerOrchestrator
{
    private readonly GitHubReleaseService _releaseService = new();
    private readonly ReleaseDownloadService _downloadService = new();
    private readonly ArchiveExtractorService _extractor = new();
    private readonly ProcessWaitService _processWaiter = new();
    private readonly FileDeploymentService _deployment = new();
    private readonly AppStarterService _starter = new();

    /// <summary>
    /// Выполняет обновление.
    /// </summary>
    public async Task RunAsync(InstallerOptions options, CancellationToken cancellationToken = default)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "CourseworkInstaller", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        Console.WriteLine("Подготовка обновления...");
        var downloadUrl = options.DownloadUrl;
        if (string.IsNullOrWhiteSpace(downloadUrl) && !string.IsNullOrWhiteSpace(options.ReleaseApiUrl))
        {
            downloadUrl = await _releaseService.GetArchiveUrlAsync(options.ReleaseApiUrl, options.AssetExtension, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            throw new InvalidOperationException("Не указан URL архива обновления.");
        }

        var archivePath = await _downloadService.DownloadAsync(downloadUrl, tempDirectory, cancellationToken);
        var extractedPath = _extractor.Extract(archivePath, tempDirectory);

        Console.WriteLine("Ожидание завершения основного приложения...");
        await _processWaiter.WaitAsync(options.MainProcessId, cancellationToken);

        Console.WriteLine("Копирование файлов обновления...");
        _deployment.Deploy(extractedPath, options.TargetDirectory);

        Console.WriteLine("Запуск приложения после обновления...");
        _starter.Start(options.TargetDirectory, options.AppExecutable);
    }
}
