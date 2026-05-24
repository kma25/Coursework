using System.Xml.Linq;
using MainApp.Models;

namespace AppTests
{
    /// <summary>
    /// Загружает XML-наборы данных для unit-тестов и преобразует атрибуты в нужные типы.
    /// </summary>
    internal static class XmlTestData
    {
        /// <summary>
        /// Возвращает наборы для проверки входа пользователей.
        /// </summary>
        public static IEnumerable<object[]> AuthorizationLoginCases()
            => Rows("authorizationLogin");

        /// <summary>
        /// Возвращает наборы для проверки регистрации пользователей.
        /// </summary>
        public static IEnumerable<object[]> AuthorizationRegistrationCases()
            => Rows("authorizationRegistration");

        /// <summary>
        /// Возвращает наборы для проверки добавления заметок.
        /// </summary>
        public static IEnumerable<object[]> NoteAddCases()
            => Rows("notesAdd");

        /// <summary>
        /// Возвращает наборы для проверки редактирования заметок.
        /// </summary>
        public static IEnumerable<object[]> NoteEditCases()
            => Rows("notesEdit");

        /// <summary>
        /// Возвращает наборы для проверки строк подключения к базе данных.
        /// </summary>
        public static IEnumerable<object[]> DatabaseConnectionCases()
            => Rows("databaseConnection");

        /// <summary>
        /// Возвращает наборы для проверки обновлений через GitHub Releases.
        /// </summary>
        public static IEnumerable<object[]> GitUpdateCases()
            => Rows("gitUpdates");

        /// <summary>
        /// Читает обязательный строковый атрибут XML-кейса.
        /// </summary>
        public static string Attribute(XElement element, string name)
            => element.Attribute(name)?.Value
                ?? throw new InvalidOperationException($"В тестовых данных не задан атрибут {name}.");

        /// <summary>
        /// Читает необязательный строковый атрибут XML-кейса.
        /// </summary>
        public static string OptionalAttribute(XElement element, string name)
            => element.Attribute(name)?.Value ?? string.Empty;

        /// <summary>
        /// Читает обязательный целочисленный атрибут XML-кейса.
        /// </summary>
        public static int IntAttribute(XElement element, string name)
            => int.Parse(Attribute(element, name));

        /// <summary>
        /// Читает необязательный целочисленный атрибут XML-кейса.
        /// </summary>
        public static int OptionalIntAttribute(XElement element, string name, int fallback)
            => int.TryParse(OptionalAttribute(element, name), out var value) ? value : fallback;

        /// <summary>
        /// Читает обязательный логический атрибут XML-кейса.
        /// </summary>
        public static bool BoolAttribute(XElement element, string name)
            => bool.Parse(Attribute(element, name));

        /// <summary>
        /// Читает роль пользователя из XML и преобразует её в доменную модель.
        /// </summary>
        public static UserRole RoleAttribute(XElement element, string name)
            => UserRoleExtensions.TryParseRole(Attribute(element, name), out var role)
                ? role
                : throw new InvalidOperationException($"В тестовых данных указана неизвестная роль: {Attribute(element, name)}.");

        /// <summary>
        /// Возвращает JSON/XML-текст, вложенный в тестовый кейс.
        /// </summary>
        public static string ElementText(XElement element, string name)
            => element.Element(name)?.Value.Trim()
                ?? throw new InvalidOperationException($"В тестовых данных не найден элемент {name}.");

        /// <summary>
        /// Читает общий параметр тестовой среды из корневого XML-файла.
        /// </summary>
        public static string Setting(string name)
            => LoadDocument().Root!.Element("settings")?.Attribute(name)?.Value
                ?? throw new InvalidOperationException($"В тестовых настройках не задан параметр {name}.");

        /// <summary>
        /// Читает общий целочисленный параметр тестовой среды.
        /// </summary>
        public static int IntSetting(string name)
            => int.Parse(Setting(name));

        private static IEnumerable<object[]> Rows(string groupName)
        {
            var group = LoadDocument().Root!.Element(groupName)
                ?? throw new InvalidOperationException($"Группа тестовых данных не найдена: {groupName}.");

            return group.Elements("case")
                .Select(element => new object[]
                {
                    Attribute(element, "name"),
                    element
                })
                .ToArray();
        }

        private static XDocument LoadDocument()
            => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "test-input-data.xml"));
    }
}
