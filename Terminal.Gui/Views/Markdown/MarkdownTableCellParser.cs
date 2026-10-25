using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Terminal.Gui.Views;

/// <summary>Lowers cell blocks without changing document blocks, anchors, or navigation state.</summary>
internal static class MarkdownTableCellParser
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();

    public static IReadOnlyList<IntermediateBlock> Parse (string source) => Lower (Markdig.Markdown.Parse (source, _pipeline), source);

    public static IReadOnlyList<IntermediateBlock> Lower (IEnumerable<Block> blocks, string source)
    {
        List<IntermediateBlock> output = [];
        VisitSequence (blocks, string.Empty, string.Empty, true);

        return output;

        void VisitSequence (IEnumerable<Block> sequence, string prefix, string continuation, bool separate)
        {
            bool first = true;

            foreach (Block block in sequence)
            {
                if (block is LinkReferenceDefinitionGroup)
                {
                    continue;
                }

                if (!first && separate)
                {
                    output.Add (new ([], true));
                }

                Visit (block, first ? prefix : continuation, continuation);
                first = false;
            }
        }

        void Visit (Block block, string prefix, string continuation)
        {
            if (block is ListBlock list)
            {
                bool firstItem = true;

                foreach (ListItemBlock item in list.Cast<ListItemBlock> ())
                {
                    if (!firstItem && list.IsLoose)
                    {
                        output.Add (new ([], true));
                    }

                    string marker = list.IsOrdered ? $"{item.Order}. " : "• ";
                    string itemPrefix = (firstItem ? prefix : continuation) + marker;
                    string indent = continuation + new string (' ', marker.GetColumns ());
                    firstItem = false;

                    if (item.Count == 0)
                    {
                        output.Add (new ([], true, itemPrefix, indent));

                        continue;
                    }

                    VisitSequence (item, itemPrefix, indent, list.IsLoose);
                }

                return;
            }

            if (block is QuoteBlock quote)
            {
                VisitSequence (quote, prefix + "> ", continuation + "> ", true);

                return;
            }

            if (block is CodeBlock code)
            {
                string? language = (code as FencedCodeBlock)?.Info;

                foreach (StringLine line in code.Lines)
                {
                    output.Add (new ([new (ExpandTabs (line.Slice.ToString ()), MarkdownStyleRole.CodeBlock)], false,
                                     prefix, continuation, isCodeBlock: true, language: language));
                    prefix = continuation;
                }

                return;
            }

            if (block is LeafBlock { Inline: { } inline })
            {
                MarkdownStyleRole role = block is HeadingBlock ? MarkdownStyleRole.Heading : MarkdownStyleRole.Normal;
                Inline? first = inline.FirstChild;

                if (first is TaskList task)
                {
                    prefix += task.Checked ? $"{Glyphs.CheckStateChecked} " : $"{Glyphs.CheckStateUnChecked} ";
                    continuation += "  ";
                    first = first.NextSibling;
                }

                output.Add (new (Markdown.LowerCellInlines (first, role, source), true, prefix, continuation));

                return;
            }

            if (block is ContainerBlock group)
            {
                VisitSequence (group, prefix, continuation, true);

                return;
            }

            if (block is LeafBlock leaf)
            {
                output.Add (new ([new (leaf.Lines.ToString (), MarkdownStyleRole.Normal)], true, prefix, continuation));
            }
        }
    }

    public static string Serialize (IEnumerable<Block> blocks, Func<Inline?, string> inlineSource)
    {
        List<string> sources = [];

        foreach (Block block in blocks)
        {
            if (block is CodeBlock code)
            {
                List<string> codeLines = [];

                foreach (StringLine line in code.Lines)
                {
                    codeLines.Add (line.Slice.ToString ());
                }

                string text = string.Join ('\n', codeLines);
                string fence = new ('`', Math.Max (3, text.Split ('`').Length));
                sources.Add ($"{fence}{(code as FencedCodeBlock)?.Info}\n{text}\n{fence}");

                continue;
            }

            if (block is ListBlock list)
            {
                List<string> items = [];

                foreach (ListItemBlock item in list.Cast<ListItemBlock> ())
                {
                    string marker = list.IsOrdered ? $"{item.Order}. " : "- ";
                    string body = Serialize (item, inlineSource);
                    items.Add (marker + body.Replace ("\n", "\n" + new string (' ', marker.Length), StringComparison.Ordinal));
                }

                sources.Add (string.Join ('\n', items));

                continue;
            }

            if (block is LeafBlock { Inline: { } inline })
            {
                string prefix = block is HeadingBlock heading ? new string ('#', heading.Level) + " " : string.Empty;
                sources.Add (prefix + inlineSource (inline.FirstChild));

                continue;
            }

            if (block is QuoteBlock quote)
            {
                sources.Add (string.Join ('\n', Serialize (quote, inlineSource).Split ('\n').Select (line => "> " + line)));

                continue;
            }

            if (block is ContainerBlock group)
            {
                sources.Add (Serialize (group, inlineSource));

                continue;
            }

            if (block is LeafBlock leaf)
            {
                sources.Add (leaf.Lines.ToString ());
            }
        }

        return string.Join ("\n\n", sources);
    }

    public static MarkdownTableContent CreateContent (TableData data)
    {
        List<MarkdownTableCellContent> cells = [];
        IReadOnlyList<TableRowData> rows = data.TableRows;

        for (int row = 0; row < rows.Count; row++)
        {
            foreach (TableCellData definition in rows [row].Cells)
            {
                IReadOnlyList<IntermediateBlock> blocks = data.CellSourcesAreBlocks
                                                             ? Parse (definition.Markdown)
                                                             : [new (MarkdownInlineParser.ParseInlines (definition.Markdown,
                                                                                                        rows [row].IsHeader ? MarkdownStyleRole.Heading : MarkdownStyleRole.Normal), true)];
                cells.Add (new (row, definition, blocks));
            }
        }

        return new (data, cells);
    }

    private static string ExpandTabs (string text)
    {
        if (!text.Contains ('\t'))
        {
            return text;
        }

        StringBuilder expanded = new ();
        int column = 0;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
        {
            if (grapheme == "\t")
            {
                int spaces = 4 - column % 4;
                expanded.Append (' ', spaces);
                column += spaces;

                continue;
            }

            expanded.Append (grapheme);
            column += grapheme.GetColumns ();
        }

        return expanded.ToString ();
    }
}
