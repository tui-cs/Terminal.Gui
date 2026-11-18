namespace DocSnippetValidator;

/// <summary>Extracts fenced C# code blocks from markdown files.</summary>
public static class SnippetExtractor
{
    /// <summary>
    ///     Markers that exclude a block from compilation. Blocks demonstrating anti-patterns are
    ///     intentionally wrong; blocks preceded by <c>&lt;!-- snippet: ignore --&gt;</c> are opted out.
    /// </summary>
    private static readonly string [] _wrongMarkers = ["// WRONG", "❌", "✗"];

    /// <summary>Extracts all fenced <c>```csharp</c> / <c>```cs</c> blocks from <paramref name="filePath"/>.</summary>
    public static IEnumerable<Snippet> Extract (string filePath)
    {
        string [] lines = File.ReadAllLines (filePath);
        List<Snippet> snippets = [];

        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines [i].TrimStart ();

            if (!trimmed.StartsWith ("```csharp", StringComparison.OrdinalIgnoreCase)
                && !string.Equals (trimmed, "```cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string indent = lines [i] [..(lines [i].Length - trimmed.Length)];
            bool ignored = HasIgnoreMarker (lines, i);
            int fenceLine = i + 1;
            List<string> code = [];
            i++;

            while (i < lines.Length && lines [i].TrimStart () != "```")
            {
                code.Add (StripIndent (lines [i], indent));
                i++;
            }

            string body = string.Join ('\n', code);

            if (_wrongMarkers.Any (body.Contains))
            {
                ignored = true;
            }

            snippets.Add (new (filePath, fenceLine, body, ignored));
        }

        return snippets;
    }

    private static bool HasIgnoreMarker (string [] lines, int fenceIndex)
    {
        for (int i = Math.Max (0, fenceIndex - 2); i < fenceIndex; i++)
        {
            if (lines [i].Contains ("snippet: ignore", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripIndent (string line, string indent)
    {
        if (indent.Length > 0 && line.StartsWith (indent, StringComparison.Ordinal))
        {
            return line [indent.Length..];
        }

        return line;
    }
}
