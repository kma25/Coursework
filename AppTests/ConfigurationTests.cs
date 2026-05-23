using MainApp.Infrastructure;
using MainApp.Models;

namespace AppTests;

[TestClass]
public sealed class ConfigurationTests
{
    [TestMethod]
    [TestCategory("Подключение к БД")]
    public void BuildConnectionString_RejectsPostgresUser()
    {
        var settings = new AppSettings();

        var thrown = false;

        try
        {
            settings.BuildConnectionString("postgres", "secret");
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }

        Assert.IsTrue(thrown, "Ожидался запрет подключения под postgres.");
    }

    [TestMethod]
    [TestCategory("Подключение к БД")]
    public void ParseRoleCredentials_AllowsOnlyUsernameAndPassword()
    {
        var credentials = AppSettings.ParseRoleCredentials("Username=app_user_role;Password=pwd;");

        Assert.AreEqual("app_user_role", credentials.Username);
        Assert.AreEqual("pwd", credentials.Password);
    }

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
}
