using MainApp.Models;
using MainApp.Services;

namespace AppTests;

/// <summary>
/// Проверяет разграничение прав между ролями user, statistician и admin на уровне сервисов приложения.
/// </summary>
[TestClass]
public sealed class AccessControlTests
{
    /// <summary>
    /// Проверяет, что статистик может получить список устройств мониторинга.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Statistician_CanViewMonitoringDevices()
    {
        var data = XmlTestData.Case("roleAccess", "statisticianCanViewMonitoring");
        var service = CreateMonitoringService();
        var session = UserSession(10, XmlTestData.RoleAttribute(data, "role"));

        var result = await service.GetDevicesAsync(session);

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Result.Success);
        Assert.IsTrue(result.Devices.Any(device => device.DeviceName == XmlTestData.Attribute(data, "deviceName")));
    }

    /// <summary>
    /// Проверяет, что обычный пользователь не получает доступ к статистике мониторинга.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task User_CannotViewMonitoringDevices()
    {
        var data = XmlTestData.Case("roleAccess", "userCannotViewMonitoring");
        var service = CreateMonitoringService();
        var session = UserSession(11, XmlTestData.RoleAttribute(data, "role"));

        var result = await service.GetDevicesAsync(session);

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Result.Success);
        Assert.IsEmpty(result.Devices);
    }

    /// <summary>
    /// Проверяет, что статистик не может создавать заметки.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Statistician_CannotAddNote()
    {
        var data = XmlTestData.Case("roleAccess", "statisticianCannotAddNote");
        var service = new NoteService(new InMemoryNoteRepository());
        var session = UserSession(12, XmlTestData.RoleAttribute(data, "role"));

        var result = await service.AddAsync(session, XmlTestData.Attribute(data, "text"));

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Result.Success);
        StringAssert.Contains(result.Result.Message, "statistician");
    }

    /// <summary>
    /// Проверяет, что статистик не может редактировать заметки.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Statistician_CannotEditNote()
    {
        var data = XmlTestData.Case("roleAccess", "statisticianCannotEditNote");
        var repository = new InMemoryNoteRepository();
        var service = new NoteService(repository);
        var owner = UserSession(XmlTestData.IntAttribute(data, "ownerId"), UserRole.User);
        var statistician = UserSession(13, XmlTestData.RoleAttribute(data, "role"));
        var created = await service.AddAsync(owner, XmlTestData.Attribute(data, "noteText"));

        var result = await service.EditAsync(statistician, created.Note!.Id, XmlTestData.Attribute(data, "newText"));

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
        StringAssert.Contains(result.Message, "statistician");
    }

    /// <summary>
    /// Проверяет, что статистик не может удалять заметки.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Statistician_CannotDeleteNote()
    {
        var data = XmlTestData.Case("roleAccess", "statisticianCannotDeleteNote");
        var repository = new InMemoryNoteRepository();
        var service = new NoteService(repository);
        var owner = UserSession(XmlTestData.IntAttribute(data, "ownerId"), UserRole.User);
        var statistician = UserSession(14, XmlTestData.RoleAttribute(data, "role"));
        var created = await service.AddAsync(owner, XmlTestData.Attribute(data, "noteText"));

        var result = await service.DeleteAsync(statistician, created.Note!.Id);

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
        StringAssert.Contains(result.Message, "statistician");
    }

    /// <summary>
    /// Проверяет, что администратор может удалять заметки любого пользователя.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Admin_CanDeleteForeignNote()
    {
        var data = XmlTestData.Case("roleAccess", "adminCanDeleteForeignNote");
        var repository = new InMemoryNoteRepository();
        var service = new NoteService(repository);
        var owner = UserSession(XmlTestData.IntAttribute(data, "ownerId"), UserRole.User);
        var admin = UserSession(XmlTestData.IntAttribute(data, "actorId"), XmlTestData.RoleAttribute(data, "actorRole"));
        var created = await service.AddAsync(owner, XmlTestData.Attribute(data, "noteText"));

        var result = await service.DeleteAsync(admin, created.Note!.Id);

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
    }

    /// <summary>
    /// Проверяет, что статистик не может выполнять административное создание пользователей.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Statistician_CannotCreateUsers()
    {
        var data = XmlTestData.Case("roleAccess", "statisticianCannotUseAdminUsers");
        var service = CreateAdminUserService();
        var session = UserSession(15, XmlTestData.RoleAttribute(data, "role"));

        var result = await service.CreateUserAsync(
            session,
            XmlTestData.Attribute(data, "login"),
            XmlTestData.Attribute(data, "password"),
            XmlTestData.Attribute(data, "newRole"));

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
        StringAssert.Contains(result.Message, "администратору");
    }

    /// <summary>
    /// Проверяет, что администратор может создавать пользователей через административный сервис.
    /// </summary>
    [TestMethod]
    [TestCategory("Права доступа")]
    public async Task Admin_CanCreateUsers()
    {
        var data = XmlTestData.Case("roleAccess", "adminCanCreateUser");
        var service = CreateAdminUserService();
        var session = UserSession(16, XmlTestData.RoleAttribute(data, "role"));

        var result = await service.CreateUserAsync(
            session,
            XmlTestData.Attribute(data, "login"),
            XmlTestData.Attribute(data, "password"),
            XmlTestData.Attribute(data, "newRole"));

        Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
    }

    private static MonitoringService CreateMonitoringService()
        => new(new InMemoryMonitoringRepository(), new WatcherProcessService(new AppSettings()));

    private static AdminUserService CreateAdminUserService()
        => new(new InMemoryUserRepository(), new InMemorySecurityLogRepository(), new PasswordHasher());

    private static UserSession UserSession(int id, UserRole role)
        => new(new User(id, $"user{id}", "hash", role, false), $"Host=localhost;Username=app_{role.ToDatabaseName()}_role;Password=pwd");
}
