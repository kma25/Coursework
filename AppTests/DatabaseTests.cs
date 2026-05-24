using System.Reflection;
using System.Xml.Linq;
using MainApp.Models;

namespace AppTests
{
    /// <summary>
    /// Проверяет правила формирования строк подключения к PostgreSQL.
    /// </summary>
    [TestClass]
    public sealed class DatabaseTests
    {
        /// <summary>
        /// Проверяет строки подключения и запрет использования postgres для рабочих операций.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.DatabaseConnectionCases), typeof(XmlTestData), DynamicDataDisplayName = nameof(GetDisplayName))]
        [TestCategory("База данных")]
        public void ConnectionString_UsesXmlDataAndReturnsExpectedResult(string caseName, XElement data)
        {
            var settings = SettingsFromXml(data);
            var result = ExecuteConnectionCase(settings, data);

            TestContext?.WriteLine($"{caseName}: ожидалось={XmlTestData.BoolAttribute(data, "expectedSuccess")}, фактически={result.Success}, результат='{result.Value ?? result.Error}'");
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedSuccess"), result.Success);
            StringAssert.Contains(result.Value ?? result.Error ?? string.Empty, XmlTestData.Attribute(data, "expectedContains"));
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

        private static AppSettings SettingsFromXml(XElement data) => new()
        {
            DatabaseHost = XmlTestData.Attribute(data, "host"),
            DatabasePort = XmlTestData.IntAttribute(data, "port"),
            DatabaseName = XmlTestData.Attribute(data, "database")
        };

        private static (bool Success, string? Value, string? Error) ExecuteConnectionCase(AppSettings settings, XElement data)
        {
            try
            {
                var value = XmlTestData.Attribute(data, "mode") == "roleFragment"
                    ? settings.BuildRoleConnectionString(XmlTestData.Attribute(data, "roleFragment"))
                    : settings.BuildConnectionString(XmlTestData.Attribute(data, "username"), XmlTestData.Attribute(data, "password"));

                return (true, value, null);
            }
            catch (InvalidOperationException ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}
