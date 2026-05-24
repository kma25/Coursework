using MainApp.Models;
using MainApp.Services;

namespace AppTests
{
    /// <summary>
    /// Проверяет регистрацию, вход и обработку некорректных учетных данных.
    /// </summary>
    [TestClass]
    public sealed class AuthServiceTests
    {
        /// <summary>
        /// Выполняет один параметризованный тест для разных пар логин-пароль из XML-файла.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.AuthorizationCases), typeof(XmlTestData))]
        [TestCategory("Авторизация")]
        public async Task Login_UsesXmlDataAndReturnsExpectedResult(string name, string login, string password, bool expectedSuccess)
        {
            var hasher = new PasswordHasher();
            var users = new InMemoryUserRepository();
            var validUser = XmlTestData.Case("authorization", "успешная авторизация");
            users.Seed(new User(
                1,
                XmlTestData.Attribute(validUser, "login"),
                hasher.Hash(XmlTestData.Attribute(validUser, "password")),
                UserRole.Admin,
                false));
            var logs = new InMemorySecurityLogRepository();
            var service = new AuthService(TestSettings(), users, logs, hasher);

            var result = await service.LoginAsync(login, password);

            TestContext?.WriteLine($"Набор: {name}; вход: {login}; результат: {result.Result.Message}");
            Assert.AreEqual(expectedSuccess, result.Result.Success);
        }

        /// <summary>
        /// Проверяет, что короткий пароль отклоняется до обращения к хранилищу пользователей.
        /// </summary>
        [TestMethod]
        [TestCategory("Авторизация")]
        public async Task Register_RejectsShortPasswordBeforeDatabaseSpecificChecks()
        {
            var data = XmlTestData.Case("registration", "регистрация с коротким паролем");
            var service = new AuthService(TestSettings(), new InMemoryUserRepository(), new InMemorySecurityLogRepository(), new PasswordHasher());

            var result = await service.RegisterAsync(
                XmlTestData.Attribute(data, "login"),
                XmlTestData.Attribute(data, "password"),
                XmlTestData.Attribute(data, "role"));

            Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
            StringAssert.Contains(result.Message, "не менее 6");
        }

        /// <summary>
        /// Проверяет, что неизвестная роль из входных данных не допускается к регистрации.
        /// </summary>
        [TestMethod]
        [TestCategory("Авторизация")]
        public async Task Register_RejectsUnknownRole()
        {
            var data = XmlTestData.Case("registration", "неизвестная роль");
            var service = new AuthService(TestSettings(), new InMemoryUserRepository(), new InMemorySecurityLogRepository(), new PasswordHasher());

            var result = await service.RegisterAsync(
                XmlTestData.Attribute(data, "login"),
                XmlTestData.Attribute(data, "password"),
                XmlTestData.Attribute(data, "role"));

            Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
            StringAssert.Contains(result.Message, "Неизвестная роль");
        }

        /// <summary>
        /// Контекст MSTest используется для вывода имени текущего XML-набора в результатах тестов.
        /// </summary>
        public TestContext? TestContext { get; set; }

        private static AppSettings TestSettings() => new()
        {
            DatabaseHost = "localhost",
            DatabasePort = 5432,
            DatabaseName = "Coursework test bsses",
            AuthUsername = "app_auth",
            AuthPassword = "auth_pwd"
        };
    }
}
