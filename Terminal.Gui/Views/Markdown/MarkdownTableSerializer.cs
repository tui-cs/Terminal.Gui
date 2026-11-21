namespace Terminal.Gui.Views;

/// <summary>Preserves rich source and serializes ordinary tables without using display text as Markdown.</summary>
internal static class MarkdownTableSerializer
{
    public static string Serialize (MarkdownTableContent content)
    {
        TableData data = content.Data;

        if (data.HasRowSeparators)
        {
            if (!string.IsNullOrEmpty (data.OriginalSource))
            {
                return data.OriginalSource;
            }

            List<string> rows = [];

            for (int row = 0; row < data.TableRows.Count; row++)
            {
                string [] fields = new string [data.ColumnCount];

                for (int column = 0; column < fields.Length; column++)
                {
                    MarkdownTableCellContent? cell = content.Occupancy [row, column];
                    string text = cell is { } && cell.Row == row && cell.Definition.ColumnIndex == column
                                      ? string.Join ('\n', cell.Blocks.Select (block => block.Prefix + string.Concat (block.Runs.Select (run => run.Text))))
                                      : string.Empty;
                    fields [column] = text.Contains ('\n') || text.Contains ('\t') || text.Contains ('"')
                                          ? "\"" + text.Replace ("\"", "\"\"", StringComparison.Ordinal) + "\""
                                          : text;
                }

                rows.Add (string.Join ('\t', fields));
            }

            return string.Join ('\n', rows);
        }

        if (data.ColumnCount == 0)
        {
            return string.Empty;
        }

        List<string> lines = ["| " + string.Join (" | ", data.Headers) + " |"];
        string separator = "| " + string.Join (" | ", data.Columns.Select (column => column.Alignment switch
        {
            Alignment.Center => ":---:",
            Alignment.End => "---:",
            _ => "---"
        })) + " |";

        if (data.Columns.Any (column => column.PreferredWidthPercentage > 0) && data.OriginalSource is { } source)
        {
            string [] originalLines = source.Split ('\n');

            if (originalLines.Length > 1)
            {
                separator = originalLines [1].TrimEnd ('\r');
            }
        }

        lines.Add (separator);
        lines.AddRange (data.Rows.Select (row => "| " + string.Join (" | ", row) + " |"));

        return string.Join ('\n', lines);
    }
}
