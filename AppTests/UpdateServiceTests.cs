using System.Net;
using MainApp.Models;
using MainApp.Services;

namespace AppTests;

[TestClass]
public sealed class UpdateServiceTests
{
    [TestMethod]
    [TestCategory("Обновления")]
    public async Task CheckAsync_FindsZipAssetWithoutRealGitHub()
    {
        var json = """
            {
              "tag_name": "v1.1.0",
              "name": "Release 1.1.0",
              "body": "Исправления",
              "assets": [
                { "name": "app.zip", "browser_download_url": "https://example.test/app.zip" }
              ]
            }
            """;
        var service = CreateService(json, HttpStatusCode.OK);

        var result = await service.CheckAsync();

        Assert.IsTrue(result.Result.Success);
        Assert.IsTrue(result.Release!.HasUpdate);
        Assert.AreEqual("https://example.test/app.zip", result.Release.ZipUrl);
    }

    [TestMethod]
    [TestCategory("Обновления")]
    public async Task CheckAsync_DoesNotUseSourceZipWhenApiHasNoAsset()
    {
        var json = """
            {
              "tag_name": "v1.1.0",
              "name": "Release 1.1.0",
              "body": "Без архива",
              "zipball_url": "https://example.test/source-code.zip",
              "assets": [
                { "name": "readme.txt", "browser_download_url": "https://example.test/readme.txt" }
              ]
            }
            """;
        var service = CreateService(json, HttpStatusCode.OK);

        var result = await service.CheckAsync();

        Assert.IsFalse(result.Result.Success);
        Assert.IsFalse(result.Release!.HasUpdate);
        Assert.IsNull(result.Release.ZipUrl);
        StringAssert.Contains(result.Result.Message, "ZIP-архив сборки");
    }

    [TestMethod]
    [TestCategory("Обновления")]
    public async Task CheckAsync_HandlesApiError()
    {
        var service = CreateService("{}", HttpStatusCode.InternalServerError);

        var result = await service.CheckAsync();

        Assert.IsFalse(result.Result.Success);
        Assert.IsNull(result.Release);
    }

    [TestMethod]
    [TestCategory("Обновления")]
    public async Task CheckAsync_ExplainsGitHubRateLimit()
    {
        var json = """{ "message": "API rate limit exceeded" }""";
        var resetAt = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString();
        var service = CreateService(json, HttpStatusCode.Forbidden, message =>
        {
            message.Headers.Add("X-RateLimit-Reset", resetAt);
        });

        var result = await service.CheckAsync();

        Assert.IsFalse(result.Result.Success);
        Assert.IsNull(result.Release);
        StringAssert.Contains(result.Result.Message, "GitHub временно ограничил");
        StringAssert.Contains(result.Result.Message, "update check");
    }

    private static UpdateService CreateService(
        string response,
        HttpStatusCode statusCode,
        Action<HttpResponseMessage>? configureResponse = null)
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var message = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response)
            };
            configureResponse?.Invoke(message);
            return message;
        });

        var settings = new UpdateSettings
        {
            UpdateOwner = "demo",
            UpdateRepo = "infra",
            UpdateAssetExtension = ".zip",
            UpdateHttpTimeoutSeconds = 5
        };

        return new UpdateService(settings, "1.0.0", new HttpClient(handler));
    }
}
