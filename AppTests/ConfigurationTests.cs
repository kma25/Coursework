using MainApp.Infrastructure;
using MainApp.Models;

namespace AppTests;

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
    /// Проверяет разбор фрагмента учетных данных роли без подмешивания Host, Port и Database.
    /// </summary>
    [TestMethod]
    [TestCategory("Подключение к БД")]
    public void ParseRoleCredentials_AllowsOnlyUsernameAndPassword()
    {
        var data = XmlTestData.Case("database", "строка подключения");
        var credentials = AppSettings.ParseRoleCredentials(XmlTestData.Attribute(data, "value"));

        Assert.AreEqual("app_user_role", credentials.Username);
        Assert.AreEqual("pwd", credentials.Password);
    }

    /// <summary>
    /// Проверяет чтение простого YAML-файла с настройками GitHub Releases.
    /// </summary>
    [TestMethod]
    [TestCategory("Подключение к БД")]
    public void SimpleYamlParser_ReadsUpdateSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"update-{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, "updateOwner: demo\nupdateRepo: infra\nupdateHttpTimeoutSeconds: 7\n");

        var values = SimpleYamlParser.Load(path);

        Assert.AreEqual("demo", SimpleYamlParser.Get(values, "updateOwner", ""));
        Assert.AreEqual(7, SimpleYamlParser.GetInt(values, "updateHttpTimeoutSeconds", 0));
    }

    /// <summary>
    /// Проверяет, что флаг автоматической проверки обновлений читается из App.config.
    /// </summary>
    [TestMethod]
    [TestCategory("Подключение к БД")]
    public void LoadAppSettings_ReadsStartupUpdateFlag()
    {
        var path = Path.Combine(Path.GetTempPath(), $"app-{Guid.NewGuid():N}.config");
        File.WriteAllText(path, """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="CheckUpdatesOnStartup" value="true" />
              </appSettings>
            </configuration>
            """);

        var settings = AppConfiguration.LoadAppSettings(path);

        Assert.IsTrue(settings.CheckUpdatesOnStartup);
    }

    /// <summary>
    /// Проверяет, что автоматический запуск watcher можно отключить настройкой App.config.
    /// </summary>
    [TestMethod]
    [TestCategory("Подключение к БД")]
    public void LoadAppSettings_ReadsWatcherAutoStartFlag()
    {
        var path = Path.Combine(Path.GetTempPath(), $"app-{Guid.NewGuid():N}.config");
        File.WriteAllText(path, """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="AutoStartWatcher" value="false" />
              </appSettings>
            </configuration>
            """);

        var settings = AppConfiguration.LoadAppSettings(path);

        Assert.IsFalse(settings.AutoStartWatcher);
    }
}
