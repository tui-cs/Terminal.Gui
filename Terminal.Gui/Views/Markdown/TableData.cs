namespace Terminal.Gui.Views;

/// <summary>Represents a Markdown table's source cells, column preferences, headers, and spans.</summary>
/// <remarks>
///     Header and body cells contain Markdown inline source, including escapes needed to preserve literal text.
///     These strings are not display text. Tables lowered from an AST retain inline source where possible,
///     expand resolved links with their titles, and encode transformed literals to preserve display semantics.
///     Cell paragraphs and soft breaks are separated by spaces; hard breaks use newline character entities.
///     The resulting source can differ from the input while retaining its rendered content and styles.
/// </remarks>
public sealed partial class TableData
{
    /// <summary>Initializes a new <see cref="TableData"/> from parsed table rows.</summary>
    /// <param name="headers">The header cells as Markdown inline source.</param>
    /// <param name="alignments">The column alignments parsed from the separator row.</param>
    /// <param name="rows">The body cells as Markdown inline source.</param>
    public TableData (string [] headers, Alignment [] alignments, string [] [] rows)
    {
        Headers = headers;
        ColumnAlignments = alignments;
        Rows = rows;
        ColumnCount = headers.Length;
    }

    /// <summary>Gets the first header row's source projection, or an empty array for a headerless structured table.</summary>
    public string [] Headers { get; }

    /// <summary>Gets the alignment for each column, parsed from the separator row.</summary>
    public Alignment [] ColumnAlignments { get; }

    /// <summary>Gets the legacy source projection of all rows except the first explicit header row.</summary>
    public string [] [] Rows { get; }

    /// <summary>Gets the number of columns in the table.</summary>
    public int ColumnCount { get; }

    /// <summary>Parses consecutive raw table lines into a <see cref="TableData"/> instance.</summary>
    /// <param name="lines">Raw markdown table lines (header, separator, body rows).</param>
    /// <returns>
    ///     A <see cref="TableData"/> if at least a header and separator row are present; otherwise <see langword="null"/>
    ///     .
    /// </returns>
    public static TableData? TryParse (IReadOnlyList<string> lines)
    {
        if (lines.Count < 2)
        {
            return null;
        }

        string [] headers = SplitRow (lines [0]);

        if (headers.Length == 0)
        {
            return null;
        }

        // Second line must be the separator row
        string [] separators = SplitRow (lines [1]);

        if (separators.Length == 0 || !IsSeparatorRow (separators))
        {
            return null;
        }

        Alignment [] alignments = ParseAlignments (separators, headers.Length);

        List<string []> rows = [];

        for (var i = 2; i < lines.Count; i++)
        {
            string [] cells = SplitRow (lines [i]);

            // Pad or truncate to match header column count
            var normalized = new string [headers.Length];

            for (var c = 0; c < headers.Length; c++)
            {
                normalized [c] = c < cells.Length ? cells [c] : string.Empty;
            }

            rows.Add (normalized);
        }

        return new TableData (headers, alignments, rows.ToArray ());
    }

    private static string [] SplitRow (string line)
    {
        string trimmed = line.Trim ();

        if (string.IsNullOrEmpty (trimmed))
        {
            return [];
        }

        if (trimmed.StartsWith ('|'))
        {
            trimmed = trimmed [1..];
        }

        if (trimmed.EndsWith ('|'))
        {
            int precedingBackslashes = 0;

            for (int index = trimmed.Length - 2; index >= 0 && trimmed [index] == '\\'; index--)
            {
                precedingBackslashes++;
            }

            if (precedingBackslashes % 2 == 0)
            {
                trimmed = trimmed [..^1];
            }
        }

        List<string> cells = [];
        int start = 0;
        int backslashes = 0;

        for (int index = 0; index < trimmed.Length; index++)
        {
            char character = trimmed [index];

            if (character == '|' && backslashes % 2 == 0)
            {
                cells.Add (trimmed [start..index].Trim ());
                start = index + 1;
            }

            backslashes = character == '\\' ? backslashes + 1 : 0;
        }

        cells.Add (trimmed [start..].Trim ());

        return [.. cells];
    }

    private static bool IsSeparatorRow (string [] cells)
    {
        foreach (string cell in cells)
        {
            string trimmed = cell.Trim (':').Trim ();

            if (trimmed.Length == 0 || trimmed.Any (c => c != '-'))
            {
                return false;
            }
        }

        return true;
    }

    private static Alignment [] ParseAlignments (string [] separators, int columnCount)
    {
        Alignment [] alignments = new Alignment [columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            if (i >= separators.Length)
            {
                alignments [i] = Alignment.Start;

                continue;
            }

            string sep = separators [i].Trim ();
            bool leftColon = sep.StartsWith (':');
            bool rightColon = sep.EndsWith (':');

            if (leftColon && rightColon)
            {
                alignments [i] = Alignment.Center;
            }
            else if (rightColon)
            {
                alignments [i] = Alignment.End;
            }
            else
            {
                alignments [i] = Alignment.Start;
            }
        }

        return alignments;
    }
}
