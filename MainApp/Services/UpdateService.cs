using System.Net.Http.Headers;
using System.Text.Json;
using MainApp.Models;

namespace MainApp.Services;

/// <summary>
/// Проверяет обновления через GitHub Releases-совместимый API.
/// </summary>
public sealed class UpdateService
{
    private readonly UpdateSettings _settings;
    private readonly string _currentVersion;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Создает сервис проверки обновлений.
    /// </summary>
    public UpdateService(UpdateSettings settings, string currentVersion, HttpClient? httpClient = null)
    {
        _settings = settings;
        _currentVersion = currentVersion;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.UpdateHttpTimeoutSeconds));

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Coursework", currentVersion));
        }
    }

    /// <summary>
    /// Запрашивает последний релиз и определяет, есть ли новая версия.
    /// </summary>
    public async Task<(OperationResult Result, UpdateRelease? Release)> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{_settings.UpdateOwner}/{_settings.UpdateRepo}/releases/latest";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var tagName = GetString(root, "tag_name");
            var name = GetString(root, "name");
            var body = GetString(root, "body");
            var zipUrl = FindAssetUrl(root, _settings.UpdateAssetExtension) ?? FindSourceArchiveUrl(root, _settings.UpdateAssetExtension);
            var hasUpdate = IsNewerVersion(_currentVersion, tagName) && zipUrl is not null;

            var release = new UpdateRelease(hasUpdate, tagName, name, body, zipUrl);
            return hasUpdate
                ? (OperationResult.Ok("Найдена новая версия приложения."), release)
                : (OperationResult.Ok("Обновления не найдены."), release);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return (OperationResult.Fail($"Ошибка проверки обновлений: {ex.Message}"), null);
        }
    }

    private static string GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static string? FindAssetUrl(JsonElement root, string extension)
    {
        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            var assetName = GetString(asset, "name");
            var url = GetString(asset, "browser_download_url");

            // У GitHub в релизе может быть несколько файлов; приложению нужен именно архив сборки.
            if (assetName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return null;
    }

    private static string? FindSourceArchiveUrl(JsonElement root, string extension)
    {
        if (!extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return root.TryGetProperty("zipball_url", out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool IsNewerVersion(string currentVersion, string candidateVersion)
    {
        var currentText = currentVersion.TrimStart('v', 'V');
        var candidateText = candidateVersion.TrimStart('v', 'V');

        return Version.TryParse(currentText, out var current)
            && Version.TryParse(candidateText, out var candidate)
            && candidate > current;
    }
}
