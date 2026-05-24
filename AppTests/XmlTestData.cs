using System.Xml.Linq;
using MainApp.Models;

namespace AppTests;

/// <summary>
/// Загружает XML-наборы данных, которые используются параметризованными и сценарными unit-тестами.
/// </summary>
internal static class XmlTestData
{
    /// <summary>
    /// Возвращает набор логинов и паролей для единого теста авторизации.
    /// </summary>
    public static IEnumerable<object[]> AuthorizationCases()
    {
        return LoadDocument().Root!
            .Element("authorization")!
            .Elements("case")
            .Select(element => new object[]
            {
                element.Attribute("name")!.Value,
                element.Attribute("login")!.Value,
                element.Attribute("password")!.Value,
                bool.Parse(element.Attribute("success")!.Value)
            });
    }

    /// <summary>
    /// Находит один именованный тест-кейс внутри указанной группы XML-данных.
    /// </summary>
    /// <param name="groupName">Имя XML-группы: например, authorization, notes или roleAccess.</param>
    /// <param name="caseName">Значение атрибута name у конкретного тест-кейса.</param>
    /// <returns>XML-элемент с параметрами теста.</returns>
    /// <exception cref="InvalidOperationException">Возникает, если группа или тест-кейс отсутствует.</exception>
    public static XElement Case(string groupName, string caseName)
    {
        var group = LoadDocument().Root!.Element(groupName)
            ?? throw new InvalidOperationException($"Группа тестовых данных не найдена: {groupName}.");

        return group.Elements("case")
            .FirstOrDefault(element => Attribute(element, "name") == caseName)
            ?? throw new InvalidOperationException($"Тест-кейс не найден: {groupName}/{caseName}.");
    }

    /// <summary>
    /// Читает обязательный строковый атрибут XML-кейса.
    /// </summary>
    public static string Attribute(XElement element, string name)
        => element.Attribute(name)?.Value
            ?? throw new InvalidOperationException($"В тестовых данных не задан атрибут {name}.");

    /// <summary>
    /// Читает обязательный целочисленный атрибут XML-кейса.
    /// </summary>
    public static int IntAttribute(XElement element, string name)
        => int.Parse(Attribute(element, name));

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

    private static XDocument LoadDocument()
        => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "test-input-data.xml"));
}
