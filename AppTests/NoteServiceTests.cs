using MainApp.Models;
using MainApp.Services;

namespace AppTests
{
    /// <summary>
    /// Проверяет прикладные правила работы с пользовательскими заметками без подключения к PostgreSQL.
    /// </summary>
    [TestClass]
    public sealed class NoteServiceTests
    {
        /// <summary>
        /// Проверяет, что пустой текст из XML-набора данных не сохраняется как заметка.
        /// </summary>
        [TestMethod]
        [TestCategory("Заметки")]
        public async Task AddAsync_RejectsEmptyNote()
        {
            var data = XmlTestData.Case("notes", "пустая заметка");
            var service = new NoteService(new InMemoryNoteRepository());
            var session = UserSession(1, UserRole.User);

            var result = await service.AddAsync(session, XmlTestData.Attribute(data, "text"));

            Assert.IsFalse(result.Result.Success);
            StringAssert.Contains(result.Result.Message, "пустую заметку");
        }

        /// <summary>
        /// Проверяет запрет на изменение заметки, принадлежащей другому обычному пользователю.
        /// </summary>
        [TestMethod]
        [TestCategory("Заметки")]
        public async Task EditAsync_RejectsForeignNoteForRegularUser()
        {
            var data = XmlTestData.Case("roleAccess", "userCannotEditForeignNote");
            var repository = new InMemoryNoteRepository();
            var service = new NoteService(repository);
            var owner = UserSession(XmlTestData.IntAttribute(data, "ownerId"), UserRole.User);
            var another = UserSession(XmlTestData.IntAttribute(data, "actorId"), XmlTestData.RoleAttribute(data, "actorRole"));
            var created = await service.AddAsync(owner, XmlTestData.Attribute(data, "noteText"));

            var result = await service.EditAsync(another, created.Note!.Id, XmlTestData.Attribute(data, "newText"));

            Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
            StringAssert.Contains(result.Message, "чужую заметку");
        }

        /// <summary>
        /// Проверяет административное право на изменение заметки любого пользователя.
        /// </summary>
        [TestMethod]
        [TestCategory("Заметки")]
        public async Task Admin_CanEditAnyNote()
        {
            var data = XmlTestData.Case("roleAccess", "adminCanEditForeignNote");
            var repository = new InMemoryNoteRepository();
            var service = new NoteService(repository);
            var owner = UserSession(XmlTestData.IntAttribute(data, "ownerId"), UserRole.User);
            var admin = UserSession(XmlTestData.IntAttribute(data, "actorId"), XmlTestData.RoleAttribute(data, "actorRole"));
            var created = await service.AddAsync(owner, XmlTestData.Attribute(data, "noteText"));

            var result = await service.EditAsync(admin, created.Note!.Id, XmlTestData.Attribute(data, "newText"));

            Assert.AreEqual(XmlTestData.BoolAttribute(data, "success"), result.Success);
        }

        private static UserSession UserSession(int id, UserRole role)
            => new(new User(id, $"user{id}", "hash", role, false), "Host=localhost;Username=app_user_role;Password=pwd");
    }
}
