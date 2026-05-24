using System.Net;
using System.Reflection;
using System.Xml.Linq;
using MainApp.Models;
using MainApp.Services;

namespace AppTests
{
    /// <summary>
    /// Проверяет сценарии работы с GitHub Releases без реальных сетевых запросов.
    /// </summary>
    [TestClass]
    public sealed class GitTests
    {
        /// <summary>
        /// Проверяет ответы GitHub Releases API, описанные в XML-файле тестовых данных.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.GitUpdateCases), typeof(XmlTestData), DynamicDataDisplayName = nameof(GetDisplayName))]
        [TestCategory("Гит")]
        public async Task CheckUpdates_UsesXmlResponseAndReturnsExpectedResult(string caseName, XElement data)
        {
            var service = CreateService(data);

            var result = await service.CheckAsync();

            TestContext?.WriteLine($"{caseName}: ожидалось={XmlTestData.BoolAttribute(data, "expectedSuccess")}, фактически={result.Result.Success}, сообщение='{result.Result.Message}'");
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedSuccess"), result.Result.Success);
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedReleaseExists"), result.Release is not null);
            StringAssert.Contains(result.Result.Message, XmlTestData.Attribute(data, "expectedMessage"));

            if (result.Release is not null)
            {
                Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedHasUpdate"), result.Release.HasUpdate);
                Assert.AreEqual(NullIfEmpty(XmlTestData.Attribute(data, "expectedZipUrl")), result.Release.ZipUrl);
            }
        }

        /// <summary>
        /// Контекст MSTest нужен для вывода результата каждого XML-набора в окне тестов.
        /// </summary>
        public TestContext? TestContext { get; set; }

        /// <summary>
        /// Формирует короткое имя XML-кейса для окна Test Explorer.
        /// </summary>
        public static string GetDisplayName(MethodInfo methodInfo, object[] data)
            => $"{methodInfo.Name}: {data[0]}";

        private static UpdateService CreateService(XElement data)
        {
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var message = new HttpResponseMessage((HttpStatusCode)XmlTestData.IntAttribute(data, "status"))
                {
                    Content = new StringContent(XmlTestData.ElementText(data, "response"))
                };

                var resetMinutes = XmlTestData.OptionalIntAttribute(data, "rateLimitResetMinutes", 0);
                if (resetMinutes > 0)
                {
                    var resetAt = DateTimeOffset.UtcNow.AddMinutes(resetMinutes).ToUnixTimeSeconds().ToString();
                    message.Headers.Add("X-RateLimit-Reset", resetAt);
                }

                return message;
            });

            var settings = new UpdateSettings
            {
                UpdateOwner = XmlTestData.Attribute(data, "updateOwner"),
                UpdateRepo = XmlTestData.Attribute(data, "updateRepo"),
                UpdateAssetExtension = XmlTestData.Attribute(data, "assetExtension"),
                UpdateHttpTimeoutSeconds = XmlTestData.IntAttribute(data, "timeoutSeconds")
            };

            return new UpdateService(settings, XmlTestData.Attribute(data, "currentVersion"), new HttpClient(handler));
        }

        private static string? NullIfEmpty(string value)
            => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
