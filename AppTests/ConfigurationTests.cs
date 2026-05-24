using MainApp.Infrastructure;
using MainApp.Models;

namespace AppTests
{
    /// <summary>
    /// Проверяет загрузку конфигурации и защитные правила формирования строк подключения.
    /// </summary>
    [TestClass]
    public sealed class ConfigurationTests
    {
        /// <summary>
        /// Проверяет, что приложение не разрешает рабочее подключение под суперпользователем postgres.
        /// </summary>
        [TestMethod]
        [TestCategory("Подключение к БД")]
        public void BuildConnectionString_RejectsPostgresUser()
        {
            var data = XmlTestData.Case("database", "запрет postgres");
            var settings = new AppSettings();

            var thrown = false;

            try
            {
                settings.BuildConnectionString(XmlTestData.Attribute(data, "username"), "secret");
            }
            catch (InvalidOperationException)
            {
                thrown = true;
            }

            Assert.IsTrue(thrown, "Ожидался запрет подключения под postgres.");
        }

        /// <summary>
        /// Проверяет, что поставляемый с проектом App.config включает проверку обновлений при запуске.
        /// </summary>
        [TestMethod]
        [TestCategory("Конфигурация")]
        public void MainAppConfig_EnablesStartupUpdateCheckByDefault()
        {
            var configPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "MainApp",
                "App.config"));

            Assert.IsTrue(File.Exists(configPath), "Не найден основной App.config проекта MainApp.");

            var settings = AppConfiguration.LoadAppSettings(configPath);

            Assert.IsTrue(settings.CheckUpdatesOnStartup);
        }
    }
}
