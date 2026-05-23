using MainApp.Models;
using MainApp.Services;

namespace AppTests;

[TestClass]
public sealed class NoteServiceTests
{
    [TestMethod]
    [TestCategory("Заметки")]
    public async Task AddAsync_RejectsEmptyNote()
    {
        var service = new NoteService(new InMemoryNoteRepository());
        var session = UserSession(1, UserRole.User);

        var result = await service.AddAsync(session, "   ");

        Assert.IsFalse(result.Result.Success);
        StringAssert.Contains(result.Result.Message, "пустую заметку");
    }

    [TestMethod]
    [TestCategory("Заметки")]
    public async Task EditAsync_RejectsForeignNoteForRegularUser()
    {
        var repository = new InMemoryNoteRepository();
        var service = new NoteService(repository);
        var owner = UserSession(1, UserRole.User);
        var another = UserSession(2, UserRole.User);
        var created = await service.AddAsync(owner, "Первичная заметка");

        var result = await service.EditAsync(another, created.Note!.Id, "Чужое изменение");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "чужую заметку");
    }

    [TestMethod]
    [TestCategory("Заметки")]
    public async Task Admin_CanEditAnyNote()
    {
        var repository = new InMemoryNoteRepository();
        var service = new NoteService(repository);
        var owner = UserSession(1, UserRole.User);
        var admin = UserSession(99, UserRole.Admin);
        var created = await service.AddAsync(owner, "Заметка пользователя");

        var result = await service.EditAsync(admin, created.Note!.Id, "Исправлено администратором");

        Assert.IsTrue(result.Success);
    }

    private static UserSession UserSession(int id, UserRole role)
        => new(new User(id, $"user{id}", "hash", role, false), "Host=localhost;Username=app_user_role;Password=pwd");
}
