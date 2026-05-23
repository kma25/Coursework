using System.Xml.Linq;

namespace AppTests;

internal static class XmlTestData
{
    public static IEnumerable<object[]> AuthorizationCases()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "test-input-data.xml"));
        return document.Root!
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
}
