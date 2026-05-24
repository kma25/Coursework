using System.Reflection;
using System.Xml.Linq;
using MainApp.Models;
using MainApp.Services;

namespace AppTests
{
    /// <summary>
    /// Проверяет вход и регистрацию пользователей на XML-наборах входных данных.
    /// </summary>
    [TestClass]
    public sealed class AuthorizationTests
    {
        /// <summary>
        /// Проверяет вход пользователя по разным парам логин-пароль и состояниям учетной записи.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.AuthorizationLoginCases), typeof(XmlTestData), DynamicDataDisplayName = nameof(GetDisplayName))]
        [TestCategory("Авторизация")]
        public async Task Login_UsesXmlDataAndReturnsExpectedResult(string caseName, XElement data)
        {
            var hasher = new PasswordHasher();
            var users = new InMemoryUserRepository();
            users.Seed(new User(
                1,
                XmlTestData.Attribute(data, "seedLogin"),
                hasher.Hash(XmlTestData.Attribute(data, "seedPassword")),
                XmlTestData.RoleAttribute(data, "seedRole"),
                XmlTestData.BoolAttribute(data, "seedBlocked")));
            var logs = new InMemorySecurityLogRepository();
            var service = new AuthService(TestSettings(), users, logs, hasher);

            var result = await service.LoginAsync(
                XmlTestData.Attribute(data, "login"),
                XmlTestData.Attribute(data, "password"));

            TestContext?.WriteLine($"{caseName}: ожидалось={XmlTestData.BoolAttribute(data, "expectedSuccess")}, фактически={result.Result.Success}, сообщение='{result.Result.Message}'");
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedSuccess"), result.Result.Success);
            StringAssert.Contains(result.Result.Message, XmlTestData.Attribute(data, "expectedMessage"));

            var expectedRole = XmlTestData.OptionalAttribute(data, "expectedRole");
            if (!string.IsNullOrWhiteSpace(expectedRole))
            {
                Assert.AreEqual(XmlTestData.RoleAttribute(data, "expectedRole"), result.Session!.User.Role);
            }
        }

        /// <summary>
        /// Проверяет регистрацию пользователей, включая первого администратора и некорректные данные.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.AuthorizationRegistrationCases), typeof(XmlTestData), DynamicDataDisplayName = nameof(GetDisplayName))]
        [TestCategory("Авторизация")]
        public async Task Register_UsesXmlDataAndReturnsExpectedResult(string caseName, XElement data)
        {
            var hasher = new PasswordHasher();
            var users = new InMemoryUserRepository();
            if (XmlTestData.BoolAttribute(data, "seedExistingUser"))
            {
                users.Seed(new User(1, "existing_admin", hasher.Hash("admin123"), UserRole.Admin, false));
            }

            var service = new AuthService(TestSettings(), users, new InMemorySecurityLogRepository(), hasher);

            var result = await service.RegisterAsync(
                XmlTestData.Attribute(data, "login"),
                XmlTestData.Attribute(data, "password"),
                XmlTestData.Attribute(data, "role"));

            TestContext?.WriteLine($"{caseName}: ожидалось={XmlTestData.BoolAttribute(data, "expectedSuccess")}, фактически={result.Success}, сообщение='{result.Message}'");
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedSuccess"), result.Success);
            StringAssert.Contains(result.Message, XmlTestData.Attribute(data, "expectedMessage"));

            var expectedRole = XmlTestData.OptionalAttribute(data, "expectedCreatedRole");
            if (!string.IsNullOrWhiteSpace(expectedRole))
            {
                var created = await users.FindByLoginAsync(XmlTestData.Attribute(data, "login"), TestSettings().BuildConnectionString("app_auth", "auth_pwd"));
                Assert.AreEqual(XmlTestData.RoleAttribute(data, "expectedCreatedRole"), created!.Role);
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

        private static AppSettings TestSettings() => new()
        {
            DatabaseHost = XmlTestData.Setting("databaseHost"),
            DatabasePort = XmlTestData.IntSetting("databasePort"),
            DatabaseName = XmlTestData.Setting("databaseName"),
            AuthUsername = XmlTestData.Setting("authUsername"),
            AuthPassword = XmlTestData.Setting("authPassword")
        };
    }
}
