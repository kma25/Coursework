using System.Net;
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
            if (!response.IsSuccessStatusCode)
            {
                return (OperationResult.Fail(await BuildHttpErrorMessageAsync(response, cancellationToken)), null);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var tagName = GetString(root, "tag_name");
            var name = GetString(root, "name");
            var body = GetString(root, "body");
            var zipUrl = FindAssetUrl(root, _settings.UpdateAssetExtension);
            var isNewerVersion = IsNewerVersion(_currentVersion, tagName);
            var hasUpdate = isNewerVersion && zipUrl is not null;

            var release = new UpdateRelease(hasUpdate, tagName, name, body, zipUrl);
            if (hasUpdate)
            {
                return (OperationResult.Ok("Найдена новая версия приложения."), release);
            }

            return isNewerVersion
                ? (OperationResult.Fail("Найдена новая версия, но к релизу не прикреплен ZIP-архив сборки."), release)
                : (OperationResult.Ok("Обновления не найдены."), release);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return (OperationResult.Fail($"Ошибка проверки обновлений: {ex.Message}"), null);
        }
    }

    private static async Task<string> BuildHttpErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var gitHubMessage = await ReadGitHubMessageAsync(response, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Forbidden
            && gitHubMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
        {
            var resetText = TryGetRateLimitReset(response);
            return resetText is null
                ? "GitHub временно ограничил проверку обновлений. Это не влияет на работу приложения; повторите update check позже."
                : $"GitHub временно ограничил проверку обновлений до {resetText}. Это не влияет на работу приложения; повторите update check позже.";
        }

        var reason = string.IsNullOrWhiteSpace(response.ReasonPhrase)
            ? response.StatusCode.ToString()
            : response.ReasonPhrase;
        return $"Ошибка проверки обновлений: GitHub вернул {(int)response.StatusCode} ({reason}).";
    }

    private static async Task<string> ReadGitHubMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            using var document = JsonDocument.Parse(body);
            return GetString(document.RootElement, "message");
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static string? TryGetRateLimitReset(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
            || !long.TryParse(values.FirstOrDefault(), out var unixSeconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
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

    private static bool IsNewerVersion(string currentVersion, string candidateVersion)
    {
        var currentText = currentVersion.TrimStart('v', 'V');
        var candidateText = candidateVersion.TrimStart('v', 'V');

        return Version.TryParse(currentText, out var current)
            && Version.TryParse(candidateText, out var candidate)
            && candidate > current;
    }
}
