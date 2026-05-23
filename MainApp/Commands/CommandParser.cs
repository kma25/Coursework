using System.Text;

namespace MainApp.Commands;

/// <summary>
/// Разбирает командную строку с учетом кавычек.
/// </summary>
public static class CommandParser
{
    /// <summary>
    /// Делит ввод пользователя на аргументы.
    /// </summary>
    public static IReadOnlyList<string> Parse(string input)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var symbol in input)
        {
            if (symbol == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(symbol) && !inQuotes)
            {
                Flush(result, current);
                continue;
            }

            current.Append(symbol);
        }

        Flush(result, current);
        return result;
    }

    private static void Flush(ICollection<string> result, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        result.Add(current.ToString());
        current.Clear();
    }
}
