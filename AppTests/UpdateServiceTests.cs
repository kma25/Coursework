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
    public async Task CheckAsync_ReturnsNoUpdateWhenApiHasNoZip()
    {
        var json = """
            {
              "tag_name": "v1.1.0",
              "name": "Release 1.1.0",
              "body": "Без архива",
              "assets": [
                { "name": "readme.txt", "browser_download_url": "https://example.test/readme.txt" }
              ]
            }
            """;
        var service = CreateService(json, HttpStatusCode.OK);

        var result = await service.CheckAsync();

        Assert.IsTrue(result.Result.Success);
        Assert.IsFalse(result.Release!.HasUpdate);
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

    private static UpdateService CreateService(string response, HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(response)
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
