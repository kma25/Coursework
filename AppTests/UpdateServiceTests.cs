using System.Net;
using MainApp.Models;
using MainApp.Services;

namespace AppTests
{
    /// <summary>
    /// Проверяет обработку ответов GitHub Releases без реальных сетевых запросов.
    /// </summary>
    [TestClass]
    public sealed class UpdateServiceTests
    {
        /// <summary>
        /// Проверяет, что сервис находит ZIP-сборку в блоке Assets релиза.
        /// </summary>
        [TestMethod]
        [TestCategory("Обновления")]
        public async Task CheckAsync_FindsZipAssetWithoutRealGitHub()
        {
            var data = XmlTestData.Case("updates", "проверка обновлений");
            var asset = XmlTestData.Attribute(data, "asset");
            var json = """
                {
                  "tag_name": "{tag}",
                  "name": "Release 1.1.0",
                  "body": "Исправления",
                  "assets": [
                    { "name": "{asset}", "browser_download_url": "https://example.test/{asset}" }
                  ]
                }
                """
                .Replace("{tag}", XmlTestData.Attribute(data, "tag"), StringComparison.Ordinal)
                .Replace("{asset}", asset, StringComparison.Ordinal);
            var service = CreateService(json, HttpStatusCode.OK);

            var result = await service.CheckAsync();

            Assert.IsTrue(result.Result.Success);
            Assert.IsTrue(result.Release!.HasUpdate);
            Assert.AreEqual($"https://example.test/{asset}", result.Release.ZipUrl);
        }

        /// <summary>
        /// Проверяет, что source zip из GitHub не принимается за готовую сборку приложения.
        /// </summary>
        [TestMethod]
        [TestCategory("Обновления")]
        public async Task CheckAsync_DoesNotUseSourceZipWhenApiHasNoAsset()
        {
            var data = XmlTestData.Case("updates", "ответ API без zip");
            var json = """
                {
                  "tag_name": "{tag}",
                  "name": "Release 1.1.0",
                  "body": "Без архива",
                  "zipball_url": "https://example.test/source-code.zip",
                  "assets": [
                    { "name": "{asset}", "browser_download_url": "https://example.test/{asset}" }
                  ]
                }
                """
                .Replace("{tag}", XmlTestData.Attribute(data, "tag"), StringComparison.Ordinal)
                .Replace("{asset}", XmlTestData.Attribute(data, "asset"), StringComparison.Ordinal);
            var service = CreateService(json, HttpStatusCode.OK);

            var result = await service.CheckAsync();

            Assert.IsFalse(result.Result.Success);
            Assert.IsFalse(result.Release!.HasUpdate);
            Assert.IsNull(result.Release.ZipUrl);
            StringAssert.Contains(result.Result.Message, "ZIP-архив сборки");
        }

        /// <summary>
        /// Проверяет контролируемое сообщение при ошибке GitHub API.
        /// </summary>
        [TestMethod]
        [TestCategory("Обновления")]
        public async Task CheckAsync_HandlesApiError()
        {
            var data = XmlTestData.Case("updates", "ошибка API");
            var service = CreateService("{}", (HttpStatusCode)XmlTestData.IntAttribute(data, "status"));

            var result = await service.CheckAsync();

            Assert.IsFalse(result.Result.Success);
            Assert.IsNull(result.Release);
        }

        /// <summary>
        /// Проверяет понятное сообщение при ограничении частоты запросов GitHub API.
        /// </summary>
        [TestMethod]
        [TestCategory("Обновления")]
        public async Task CheckAsync_ExplainsGitHubRateLimit()
        {
            var data = XmlTestData.Case("updates", "лимит GitHub API");
            var gitHubMessage = XmlTestData.Attribute(data, "message");
            var json = $$"""{ "message": "{{gitHubMessage}}" }""";
            var resetAt = DateTimeOffset.UtcNow.AddMinutes(XmlTestData.IntAttribute(data, "resetMinutes")).ToUnixTimeSeconds().ToString();
            var service = CreateService(json, (HttpStatusCode)XmlTestData.IntAttribute(data, "status"), message =>
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
}
