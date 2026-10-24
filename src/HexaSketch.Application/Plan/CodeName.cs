using System.Text;

namespace HexaSketch.Application.Plan;

/// <summary>Turns a display name into a C# identifier: "In-memory store" becomes "InMemoryStore".</summary>
public static class CodeName
{
    public static string From(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var words = new List<string>();
        var word = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c)) word.Append(c);
            else Flush();
        }
        Flush();
        return string.Concat(words.Select(Pascal));

        void Flush()
        {
            if (word.Length > 0) words.Add(word.ToString());
            word.Clear();
        }
    }

    public static bool IsValid(string codeName) => codeName.Length > 0 && !char.IsDigit(codeName[0]);

    // .NET naming guidelines: two-letter acronyms stay upper case (UI), longer ones don't (Wpf).
    private static string Pascal(string word) =>
        word.Length > 2 && word.All(char.IsUpper)
            ? word[0] + word[1..].ToLowerInvariant()
            : char.ToUpperInvariant(word[0]) + word[1..];
}
