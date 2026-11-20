using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace Terminal.Gui.Views;

/// <summary>Extracts standalone table source without enclosing quote markers or list indentation.</summary>
internal static class MarkdownTableSource
{
    public static string Extract (Table table, string source)
    {
        if (table.Span.Start < 0 || table.Span.End < table.Span.Start || table.Span.End >= source.Length)
        {
            return string.Empty;
        }

        string original = source [table.Span.Start..(table.Span.End + 1)].TrimEnd ('\r', '\n');
        List<Block> ancestors = [];

        for (Block? block = table.Parent; block is { }; block = block.Parent)
        {
            if (block is not QuoteBlock and not ListItemBlock)
            {
                continue;
            }

            ancestors.Add (block);
        }

        if (ancestors.Count == 0)
        {
            return original;
        }

        ancestors.Reverse ();
        int firstLineStart = source.LastIndexOf ('\n', table.Span.Start) + 1;
        string firstPrefix = source [firstLineStart..table.Span.Start];
        (_, int prefixColumns) = SkipAncestors (firstPrefix, true);
        int localIndent = Math.Max (0, table.Column - prefixColumns);
        string [] lines = original.Split ('\n');

        // The AST span starts at the first table character, but following lines still include ancestors.
        for (int line = 1; line < lines.Length; line++)
        {
            (int offset, int column) = SkipAncestors (lines [line], false);
            int target = column + localIndent;

            while (offset < lines [line].Length && column < target && lines [line] [offset] is ' ' or '\t')
            {
                column += lines [line] [offset++] == '\t' ? 4 - column % 4 : 1;
            }

            lines [line] = lines [line] [offset..];
        }

        return string.Join ('\n', lines);

        (int Offset, int Columns) SkipAncestors (string line, bool first)
        {
            int offset = 0;
            int column = 0;

            foreach (Block ancestor in ancestors)
            {
                if (ancestor is ListItemBlock item)
                {
                    int target = column + item.ColumnWidth;

                    while (offset < line.Length && column < target && (first || line [offset] is ' ' or '\t'))
                    {
                        column += line [offset++] == '\t' ? 4 - column % 4 : 1;
                    }

                    continue;
                }

                int marker = offset;

                while (marker < line.Length && marker - offset < 3 && line [marker] == ' ')
                {
                    marker++;
                }

                if (marker >= line.Length || line [marker] != '>')
                {
                    continue;
                }

                column += marker - offset + 1;
                offset = marker + 1;

                if (offset < line.Length && line [offset] is ' ' or '\t')
                {
                    column += line [offset++] == '\t' ? 4 - column % 4 : 1;
                }
            }

            return (offset, column);
        }
    }
}
