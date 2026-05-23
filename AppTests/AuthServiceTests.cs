using MainApp.Models;
using MainApp.Services;

namespace AppTests;

[TestClass]
public sealed class AuthServiceTests
{
    [TestMethod]
    [DynamicData(nameof(XmlTestData.AuthorizationCases), typeof(XmlTestData))]
    [TestCategory("Авторизация")]
    public async Task Login_UsesXmlDataAndReturnsExpectedResult(string name, string login, string password, bool expectedSuccess)
    {
        var hasher = new PasswordHasher();
        var users = new InMemoryUserRepository();
        users.Seed(new User(1, "admin", hasher.Hash("secret123"), UserRole.Admin, false));
        var logs = new InMemorySecurityLogRepository();
        var service = new AuthService(TestSettings(), users, logs, hasher);

        var result = await service.LoginAsync(login, password);

        TestContext?.WriteLine($"Набор: {name}; вход: {login}; результат: {result.Result.Message}");
        Assert.AreEqual(expectedSuccess, result.Result.Success);
    }

    [TestMethod]
    [TestCategory("Авторизация")]
    public async Task Register_RejectsShortPasswordBeforeDatabaseSpecificChecks()
    {
        var service = new AuthService(TestSettings(), new InMemoryUserRepository(), new InMemorySecurityLogRepository(), new PasswordHasher());

        var result = await service.RegisterAsync("new_user", "123");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "не менее 6");
    }

    [TestMethod]
    [TestCategory("Авторизация")]
    public async Task Register_RejectsUnknownRole()
    {
        var service = new AuthService(TestSettings(), new InMemoryUserRepository(), new InMemorySecurityLogRepository(), new PasswordHasher());

        var result = await service.RegisterAsync("new_user", "secret123", "owner");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "Неизвестная роль");
    }

    public TestContext? TestContext { get; set; }

    private static AppSettings TestSettings() => new()
    {
        DatabaseHost = "localhost",
        DatabasePort = 5432,
        DatabaseName = "it_support_test",
        AuthUsername = "app_auth",
        AuthPassword = "auth_pwd"
    };
}
