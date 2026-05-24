using System.Reflection;
using System.Xml.Linq;
using MainApp.Models;
using MainApp.Services;

namespace AppTests
{
    /// <summary>
    /// Проверяет основные операции с заметками и ограничения доступа к чужим данным.
    /// </summary>
    [TestClass]
    public sealed class NotesTests
    {
        /// <summary>
        /// Проверяет добавление заметок для разных ролей и вариантов текста.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.NoteAddCases), typeof(XmlTestData), DynamicDataDisplayName = nameof(GetDisplayName))]
        [TestCategory("Заметки")]
        public async Task Add_UsesXmlDataAndReturnsExpectedResult(string caseName, XElement data)
        {
            var service = new NoteService(new InMemoryNoteRepository());
            var session = UserSession(
                XmlTestData.IntAttribute(data, "actorId"),
                XmlTestData.RoleAttribute(data, "actorRole"));

            var result = await service.AddAsync(session, XmlTestData.Attribute(data, "text"));

            TestContext?.WriteLine($"{caseName}: ожидалось={XmlTestData.BoolAttribute(data, "expectedSuccess")}, фактически={result.Result.Success}, сообщение='{result.Result.Message}'");
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedSuccess"), result.Result.Success);
            StringAssert.Contains(result.Result.Message, XmlTestData.Attribute(data, "expectedMessage"));

            var expectedText = XmlTestData.OptionalAttribute(data, "expectedSavedText");
            if (!string.IsNullOrWhiteSpace(expectedText))
            {
                Assert.AreEqual(expectedText, result.Note!.Text);
            }
        }

        /// <summary>
        /// Проверяет редактирование заметок владельцем, администратором и запрещенными ролями.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(XmlTestData.NoteEditCases), typeof(XmlTestData), DynamicDataDisplayName = nameof(GetDisplayName))]
        [TestCategory("Заметки")]
        public async Task Edit_UsesXmlDataAndReturnsExpectedResult(string caseName, XElement data)
        {
            var repository = new InMemoryNoteRepository();
            var service = new NoteService(repository);
            var owner = UserSession(XmlTestData.IntAttribute(data, "ownerId"), UserRole.User);
            var actor = UserSession(
                XmlTestData.IntAttribute(data, "actorId"),
                XmlTestData.RoleAttribute(data, "actorRole"));
            var created = await service.AddAsync(owner, XmlTestData.Attribute(data, "noteText"));

            var result = await service.EditAsync(actor, created.Note!.Id, XmlTestData.Attribute(data, "newText"));

            TestContext?.WriteLine($"{caseName}: ожидалось={XmlTestData.BoolAttribute(data, "expectedSuccess")}, фактически={result.Success}, сообщение='{result.Message}'");
            Assert.AreEqual(XmlTestData.BoolAttribute(data, "expectedSuccess"), result.Success);
            StringAssert.Contains(result.Message, XmlTestData.Attribute(data, "expectedMessage"));
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

        private static UserSession UserSession(int id, UserRole role)
        {
            var connection = XmlTestData.Setting("roleConnectionTemplate")
                .Replace("{role}", role.ToDatabaseName(), StringComparison.Ordinal);

            return new(new User(id, $"user{id}", "hash", role, false), connection);
        }
    }
}
