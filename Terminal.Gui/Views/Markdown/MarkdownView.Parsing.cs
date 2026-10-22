using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Views;

public partial class Markdown
{
    private static readonly MarkdownPipeline _defaultPipeline = new MarkdownPipelineBuilder ().UseAdvancedExtensions ().Build ();
    private static readonly Regex _htmlTagPattern = new ("<[^>]+>", RegexOptions.Compiled);

    private Dictionary<string, int> _slugCounts = new (StringComparer.OrdinalIgnoreCase);

    private void EnsureParsed ()
    {
        if (_parsed)
        {
            return;
        }

        _blocks.Clear ();

        MarkdownPipeline pipeline = MarkdownPipeline ?? _defaultPipeline;
        MarkdownDocument doc;

        try
        {
            doc = Markdig.Markdown.Parse (_markdown, pipeline);
        }
        catch (ArgumentException exception)
        {
            // Markdig uses ArgumentException for nesting limits; avoid depending on its message.
            Logging.Logger.LogWarning (exception, "Markdown parsing failed; rendering the source as literal text.");
            doc = new MarkdownDocument ();
            int lineNumber = 0;

            // Lower literal paragraphs through the same state-reset and layout path as parsed input.
            foreach (string line in _markdown.Replace ("\r\n", "\n", StringComparison.Ordinal).Replace ('\r', '\n').Split ('\n'))
            {
                ContainerInline inline = new ();
                inline.AppendChild (new LiteralInline (line));
                doc.Add (new ParagraphBlock
                {
                    Line = lineNumber++,
                    Inline = inline
                });
            }
        }

        LowerFromAst (doc);

        _parsed = true;
    }

    private void LowerFromAst (MarkdownDocument doc)
    {
        _slugCounts = new Dictionary<string, int> (StringComparer.OrdinalIgnoreCase);

        Block? prevBlock = null;

        foreach (Block block in doc)
        {
            if (block is LinkReferenceDefinitionGroup)
            {
                prevBlock = block;

                continue;
            }

            if (prevBlock is { } and not LinkReferenceDefinitionGroup)
            {
                int prevEndLine = GetBlockEndLine (prevBlock);
                int thisStartLine = block.Line;

                if (thisStartLine > prevEndLine + 1)
                {
                    _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.Normal)], true));
                }
            }

            WalkBlock (block, string.Empty, string.Empty, MarkdownStyleRole.Normal);
            prevBlock = block;
        }
    }

    private void WalkBlock (Block block, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        switch (block)
        {
            case HeadingBlock heading:
                HandleHeadingBlock (heading, prefix, contPrefix);

                break;

            case ParagraphBlock para:
                HandleParagraphBlock (para, prefix, contPrefix, defaultRole);

                break;

            case DefinitionTerm term:
                _blocks.Add (new IntermediateBlock (WalkInlines (term.Inline?.FirstChild, defaultRole, _markdown), true, prefix, contPrefix));

                break;

            case DefinitionList definitions:
                foreach (Block item in definitions)
                {
                    int previousCount = _blocks.Count;
                    WalkBlock (item, prefix, contPrefix, defaultRole);

                    if (_blocks.Count == previousCount)
                    {
                        continue;
                    }

                    prefix = contPrefix;
                }

                break;

            case DefinitionItem definition:
                foreach (Block content in definition)
                {
                    int previousCount = _blocks.Count;
                    string indent = content is DefinitionTerm ? string.Empty : "  ";
                    WalkBlock (content, prefix + indent, contPrefix + indent, defaultRole);

                    if (_blocks.Count == previousCount)
                    {
                        continue;
                    }

                    prefix = contPrefix;
                }

                break;

            case FencedCodeBlock:
            case CodeBlock:
                HandleCodeBlockNode ((LeafBlock)block);

                break;

            case ThematicBreakBlock:
                _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.ThematicBreak)], false, isThematicBreak: true));

                break;

            case AlertBlock alert:
                // Markdig accepts custom alphabetic kinds; retain their names and markers too.
                string kind = alert.Kind.ToString ();
                string marker = TranslateListMarkerText (prefix + "> ") + $"[!{kind}]";
                _blocks.Add (new IntermediateBlock ([new InlineRun (kind.ToUpperInvariant (), MarkdownStyleRole.Strong)], true, prefix + "> ", contPrefix + "> ", sourceText: marker));

                foreach (Block content in alert)
                {
                    WalkBlock (content, contPrefix + "> ", contPrefix + "> ", MarkdownStyleRole.Quote);
                }

                break;

            case QuoteBlock quote:
                foreach (Block child in quote)
                {
                    WalkBlock (child, prefix + "> ", contPrefix + "> ", MarkdownStyleRole.Quote);
                }

                break;

            case ListBlock list:
                HandleListBlock (list, prefix, contPrefix, defaultRole);

                break;

            case Table table:
                HandleTableBlock (table);

                break;

            case HtmlBlock html:
                HandleHtmlBlock (html, prefix, contPrefix, defaultRole);

                break;

            case LinkReferenceDefinitionGroup:
                // Consumed by Markdig during inline resolution; nothing to render.
                break;

            default:
                HandleUnknownBlock (block, prefix, contPrefix, defaultRole);

                break;
        }
    }

    private void HandleHeadingBlock (HeadingBlock heading, string prefix, string contPrefix)
    {
        List<InlineRun> runs = WalkInlines (heading.Inline?.FirstChild, MarkdownStyleRole.Heading, _markdown);

        // Compute anchor slug from heading text before inserting the marker
        string headingText = string.Concat (runs.Select (r => r.Text)).Replace ('\n', ' ');
        string anchor = DeduplicateSlug (GenerateAnchorSlug (headingText), _slugCounts);

        if (ShowHeadingPrefix)
        {
            var hashes = new string ('#', heading.Level);
            runs.Insert (0, new InlineRun ($"{prefix}{hashes} ", MarkdownStyleRole.HeadingMarker));
        }
        else if (!string.IsNullOrEmpty (prefix))
        {
            runs.Insert (0, new InlineRun (prefix, MarkdownStyleRole.HeadingMarker));
        }

        _blocks.Add (new IntermediateBlock (runs, true, continuationPrefix: contPrefix, anchor: anchor));
    }

    private void HandleParagraphBlock (ParagraphBlock para, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        List<InlineRun> runs = WalkInlines (para.Inline?.FirstChild, defaultRole, _markdown);
        _blocks.Add (new IntermediateBlock (runs, true, prefix, contPrefix));
    }

    private void HandleCodeBlockNode (LeafBlock codeBlock)
    {
        string? language = (codeBlock as FencedCodeBlock)?.Info;

        // Markdig returns empty string for fences with no language; normalize to null
        if (string.IsNullOrEmpty (language))
        {
            language = null;
        }

        List<string> lines = [];

        foreach (StringLine line in codeBlock.Lines)
        {
            lines.Add (line.Slice.ToString ());
        }

        AddCodeBlockLines (lines, language);
    }

    private void HandleListBlock (ListBlock list, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        foreach (Block item in list)
        {
            if (item is not ListItemBlock listItem)
            {
                continue;
            }

            string marker = list.IsOrdered ? $"{listItem.Order}. " : "• ";
            string itemPrefix = prefix + marker;
            string itemCont = contPrefix + new string (' ', marker.GetColumns ());

            if (listItem.Count == 0)
            {
                _blocks.Add (new IntermediateBlock ([], true, itemPrefix, itemCont));

                continue;
            }

            var isFirst = true;

            foreach (Block child in listItem)
            {
                if (isFirst && child is ParagraphBlock para)
                {
                    Inline? firstInline = para.Inline?.FirstChild;

                    if (firstInline is TaskList tl)
                    {
                        bool done = tl.Checked;
                        MarkdownStyleRole role = done ? MarkdownStyleRole.TaskDone : MarkdownStyleRole.TaskTodo;
                        string checkbox = done ? $"{Glyphs.CheckStateChecked} " : $"{Glyphs.CheckStateUnChecked} ";
                        string taskPrefix = itemPrefix + checkbox;
                        // To align wrapped task text after variable-width glyph markers, include the trailing space in the continuation indent.
                        string taskCont = contPrefix + new string (' ', marker.GetColumns () + checkbox.GetColumns ());

                        List<InlineRun> runs = WalkInlines (firstInline.NextSibling, role, _markdown);
                        TrimLeadingSpace (runs);
                        _blocks.Add (new IntermediateBlock (runs, true, taskPrefix, taskCont));
                    }
                    else
                    {
                        List<InlineRun> runs = WalkInlines (firstInline, defaultRole, _markdown);
                        _blocks.Add (new IntermediateBlock (runs, true, itemPrefix, itemCont));
                    }

                    isFirst = false;
                }
                else
                {
                    WalkBlock (child, isFirst ? itemPrefix : itemCont, itemCont, defaultRole);
                    isFirst = false;
                }
            }
        }
    }

    private void HandleTableBlock (Table table)
    {
        bool grid = table.Parser is GridTableParser;
        List<TableRowData> rows = [];
        List<MarkdownTableCellContent> cells = [];
        HashSet<(int Row, int Column)> occupied = [];
        int columnCount = table.ColumnDefinitions.Count;
        int rowIndex = 0;

        foreach (TableRow row in table.Cast<TableRow> ())
        {
            List<TableCellData> definitions = [];
            int nextColumn = 0;

            foreach (TableCell cell in row.Cast<TableCell> ())
            {
                while (occupied.Contains ((rowIndex, nextColumn)))
                {
                    nextColumn++;
                }

                int column = cell.ColumnIndex >= 0 ? cell.ColumnIndex : nextColumn;
                string source = grid ? MarkdownTableCellParser.Serialize (cell, GetCellInlineSource) : ExtractCellText (cell);
                TableCellData definition = new (column, source, cell.ColumnSpan, cell.RowSpan);
                definitions.Add (definition);
                IReadOnlyList<IntermediateBlock> blocks = grid
                                                             ? MarkdownTableCellParser.Lower (cell, _markdown)
                                                             : [new (LowerCellInlines (cell.OfType<ParagraphBlock> ().FirstOrDefault ()?.Inline?.FirstChild,
                                                                                      row.IsHeader ? MarkdownStyleRole.Heading : MarkdownStyleRole.Normal, _markdown), true)];
                cells.Add (new (rowIndex, definition, blocks));
                columnCount = Math.Max (columnCount, checked(column + cell.ColumnSpan));

                for (int y = rowIndex; y < checked(rowIndex + cell.RowSpan); y++)
                {
                    for (int x = column; x < column + cell.ColumnSpan; x++)
                    {
                        occupied.Add ((y, x));
                    }
                }

                nextColumn = column + cell.ColumnSpan;
            }

            rows.Add (new (row.IsHeader, definitions));
            rowIndex++;
        }

        if (columnCount == 0)
        {
            return;
        }

        int requiredRows = cells.Count == 0 ? rows.Count : cells.Max (cell => cell.Row + cell.Definition.RowSpan);

        while (rows.Count < requiredRows)
        {
            rows.Add (new (false, []));
        }

        TableColumnData [] columns = [.. Enumerable.Range (0, columnCount).Select (column =>
        {
            TableColumnDefinition? definition = column < table.ColumnDefinitions.Count ? table.ColumnDefinitions [column] : null;
            Alignment alignment = definition?.Alignment switch
            {
                TableColumnAlign.Center => Alignment.Center,
                TableColumnAlign.Right => Alignment.End,
                _ => Alignment.Start
            };

            return new TableColumnData (alignment, definition?.Width);
        })];
        string original = MarkdownTableSource.Extract (table, _markdown);
        TableData data = new (columns, rows, grid, grid, original);
        MarkdownTableContent content = new (data, cells);
        _blocks.Add (new ([new ("", MarkdownStyleRole.Table)], false, tableData: data, tableContent: content));
    }

    private string GetCellInlineSource (Inline? inline)
    {
        StringBuilder source = new ();
        AppendInlineSource (inline, source);

        return source.ToString ();
    }

    internal static List<InlineRun> LowerCellInlines (Inline? inline, MarkdownStyleRole defaultRole, string source) =>
        WalkInlines (inline, defaultRole, source, isTableCell: true);

    private string ExtractCellText (TableCell cell)
    {
        // Reuse literal source only when reparsing the entire cell preserves its display semantics.
        if (MarkdownPipeline is { }
            && cell.Count == 1
            && cell [0] is ParagraphBlock { Inline.FirstChild: LiteralInline { NextSibling: null } literal })
        {
            string original = EscapeCellPipes (GetInlineSource (literal, _markdown).Trim ());
            List<InlineRun> runs = MarkdownInlineParser.ParseInlines (original, MarkdownStyleRole.Normal);

            if (string.Concat (runs.Select (run => run.Text)) == literal.Content.ToString ()
                && runs.All (run => run.StyleRole == MarkdownStyleRole.Normal
                                    && run.AdditionalStyle == TextStyle.None
                                    && run.Url is null
                                    && run.ImageSource is null))
            {
                return original;
            }
        }

        StringBuilder source = new ();

        foreach (Block block in cell)
        {
            if (block is not ParagraphBlock paragraph)
            {
                continue;
            }

            if (source.Length > 0)
            {
                source.Append (' ');
            }

            AppendInlineSource (paragraph.Inline?.FirstChild, source);
        }

        return EscapeCellPipes (source.ToString ().Trim ());
    }

    private static string EscapeCellPipes (string source)
    {
        if (!source.Contains ('|'))
        {
            return source;
        }

        // Grid cells can contain raw pipes; clipboard rows always use pipe-table syntax.
        StringBuilder escaped = new (source.Length);
        int backslashes = 0;

        foreach (char character in source)
        {
            if (character == '|' && backslashes % 2 == 0)
            {
                escaped.Append ('\\');
            }

            escaped.Append (character);
            backslashes = character == '\\' ? backslashes + 1 : 0;
        }

        return escaped.ToString ();
    }

    private void AppendInlineSource (Inline? inline, StringBuilder source)
    {
        while (inline is { })
        {
            string original = GetInlineSource (inline, _markdown);

            switch (inline)
            {
                case LiteralInline literal:
                    string text = literal.Content.ToString ();

                    // Encode custom-pipeline literals so reparsing cannot enable other extensions.
                    if (MarkdownPipeline is { })
                    {
                        source.Append (EncodeCellLiteral (text));

                        break;
                    }

                    source.Append (literal.IsFirstCharacterEscaped && original.Length > 0 ? original : text);

                    break;

                case EmphasisInline emphasis:
                    string marker = new (emphasis.DelimiterChar, emphasis.DelimiterCount);
                    source.Append (marker);
                    AppendInlineSource (emphasis.FirstChild, source);
                    source.Append (marker);

                    break;

                case CodeInline code:
                    if (original.Length > 0 && !original.Contains ('\n') && !original.Contains ('\r'))
                    {
                        source.Append (original);

                        break;
                    }

                    AppendCellCode (source, code.Content);

                    break;

                case LinkInline link:
                    source.Append (link.IsImage ? "![" : "[");
                    AppendInlineSource (link.FirstChild, source);
                    source.Append ("](<");
                    source.Append (EscapeCellDestination (link.Url));
                    source.Append ('>');

                    if (link.Title is { })
                    {
                        source.Append (" \"");
                        source.Append (EncodeCellLiteral (link.Title));
                        source.Append ('"');
                    }

                    source.Append (')');

                    break;

                case HtmlEntityInline entity:
                    source.Append (original.Length > 0 ? original : EncodeCellLiteral (entity.Transcoded.ToString ()));

                    break;

                case AbbreviationInline abbreviation:
                    source.Append (EncodeCellLiteral (abbreviation.Abbreviation.Label ?? string.Empty));

                    break;

                case AutolinkInline auto:
                    source.Append (original.Length > 0 ? original : $"<{EscapeCellDestination (auto.Url)}>");

                    break;

                case LineBreakInline lineBreak:
                    // Keep a pipe-table row on one physical line, including explicit hard breaks.
                    source.Append (lineBreak.IsHard ? "&#10;" : " ");

                    break;

                case HtmlInline:
                    // Match the display path, which omits inline HTML tags.

                    break;

                case ContainerInline group:
                    AppendInlineSource (group.FirstChild, source);

                    break;

                case LeafInline:
                    source.Append (EncodeCellLiteral (original));

                    break;
            }

            inline = inline.NextSibling;
        }
    }

    private static void AppendCellCode (StringBuilder source, string content)
    {
        int longestRun = 0;
        int currentRun = 0;

        foreach (char character in content)
        {
            currentRun = character == '`' ? currentRun + 1 : 0;
            longestRun = Math.Max (longestRun, currentRun);
        }

        string delimiter = new ('`', longestRun + 1);
        bool pad = content.StartsWith ('`') || content.EndsWith ('`')
                   || (content.StartsWith (' ') && content.EndsWith (' ') && !string.IsNullOrWhiteSpace (content));
        source.Append (delimiter);

        if (pad)
        {
            source.Append (' ');
        }

        source.Append (content.Replace ("\\|", "|", StringComparison.Ordinal).Replace ("|", "\\|", StringComparison.Ordinal));

        if (pad)
        {
            source.Append (' ');
        }

        source.Append (delimiter);
    }

    private static string GetInlineSource (Inline inline, string source) =>
        inline.Span.Start >= 0 && inline.Span.End >= inline.Span.Start && inline.Span.End < source.Length
            ? source.Substring (inline.Span.Start, inline.Span.Length)
            : string.Empty;

    private static string EscapeCellDestination (string? destination) =>
        (destination ?? string.Empty).Replace ("\\", "\\\\", StringComparison.Ordinal)
                                     .Replace ("|", "\\|", StringComparison.Ordinal)
                                     .Replace ("(", "\\(", StringComparison.Ordinal)
                                     .Replace (")", "\\)", StringComparison.Ordinal)
                                     .Replace ("<", "\\<", StringComparison.Ordinal)
                                     .Replace (">", "\\>", StringComparison.Ordinal);

    private static string EncodeCellLiteral (string text)
    {
        StringBuilder source = new ();

        foreach (char character in text)
        {
            if (!character.IsAsciiPunctuation ())
            {
                source.Append (character);

                continue;
            }

            // Encode punctuation systematically so literal text cannot enable inline extensions.
            source.Append ("&#").Append ((int)character).Append (';');
        }

        return source.ToString ();
    }

    private void HandleHtmlBlock (HtmlBlock html, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        foreach (StringLine line in html.Lines)
        {
            string text = line.Slice.ToString ().Trim ();

            if (string.IsNullOrEmpty (text))
            {
                continue;
            }

            // Strip HTML tags for plain-text terminal rendering
            string stripped = _htmlTagPattern.Replace (text, string.Empty).Trim ();

            if (string.IsNullOrEmpty (stripped))
            {
                continue;
            }

            _blocks.Add (new IntermediateBlock ([new InlineRun (stripped, defaultRole)], true, prefix, contPrefix));
        }
    }

    private void HandleUnknownBlock (Block block, string prefix, string contPrefix, MarkdownStyleRole defaultRole)
    {
        // For unrecognized block types: extract text from leaf blocks or recurse containers.
        if (block is LeafBlock { Lines.Count: > 0 } leaf)
        {
            StringBuilder sb = new ();

            foreach (StringLine line in leaf.Lines)
            {
                if (sb.Length > 0)
                {
                    sb.Append (' ');
                }

                sb.Append (line.Slice.ToString ());
            }

            string text = sb.ToString ().Trim ();

            if (!string.IsNullOrEmpty (text))
            {
                _blocks.Add (new IntermediateBlock ([new InlineRun (text, defaultRole)], true, prefix, contPrefix));
            }
        }
        else if (block is ContainerBlock container)
        {
            foreach (Block child in container)
            {
                WalkBlock (child, prefix, contPrefix, defaultRole);
            }
        }
    }

    private static List<InlineRun> WalkInlines (Inline? inline, MarkdownStyleRole defaultRole, string source, bool isTableCell = false)
    {
        List<InlineRun> runs = [];

        while (inline is { })
        {
            switch (inline)
            {
                case LiteralInline lit:
                    var litText = lit.Content.ToString ();

                    if (!string.IsNullOrEmpty (litText))
                    {
                        runs.Add (new InlineRun (litText, defaultRole));
                    }

                    break;

                case EmphasisInline em:
                    MarkdownStyleRole emRole = em is { DelimiterChar: '~', DelimiterCount: >= 2 } ? MarkdownStyleRole.Strikethrough :
                                               em.DelimiterCount >= 2 ? MarkdownStyleRole.Strong : MarkdownStyleRole.Emphasis;
                    TextStyle emphasisStyle = emRole switch
                    {
                        MarkdownStyleRole.Strong => TextStyle.Bold,
                        MarkdownStyleRole.Strikethrough => TextStyle.Strikethrough,
                        _ => TextStyle.Italic
                    };
                    runs.AddRange (WalkInlines (em.FirstChild, emRole, source, isTableCell)
                                       .Select (run => run with
                                       {
                                           AdditionalStyle = run.AdditionalStyle | emphasisStyle
                                       }));

                    break;

                case CodeInline code:
                    // Normalize table separators before a surrounding link replaces the code role.
                    string codeText = isTableCell ? code.Content.Replace ("\\|", "|", StringComparison.Ordinal) : code.Content;
                    runs.Add (new InlineRun (codeText, MarkdownStyleRole.InlineCode));

                    break;

                case LinkInline { IsImage: true } link:
                    List<InlineRun> altRuns = WalkInlines (link.FirstChild, MarkdownStyleRole.ImageAlt, source, isTableCell);
                    string altText = string.Concat (altRuns.Select (r => r.Text));
                    string fallback = MarkdownImageResolver.GetFallbackText (altText);
                    runs.Add (new InlineRun (fallback, MarkdownStyleRole.ImageAlt, ImageSource: link.Url));

                    break;

                case LinkInline link:
                    string? url = link.Url;
                    List<InlineRun> linkRuns = WalkInlines (link.FirstChild, MarkdownStyleRole.Link, source, isTableCell);
                    runs.AddRange (linkRuns.Select (r => r with
                    {
                        StyleRole = MarkdownStyleRole.Link,
                        Url = url,
                        LinkIdentity = link.Span.Start
                    }));

                    break;

                case AutolinkInline auto:
                    runs.Add (new InlineRun (auto.Url, MarkdownStyleRole.Link, auto.Url) { LinkIdentity = auto.Span.Start });

                    break;

                case LineBreakInline lineBreak:
                    runs.Add (new InlineRun (lineBreak.IsHard ? "\n" : " ", defaultRole));

                    break;

                case HtmlEntityInline entity:
                    var entityText = entity.Transcoded.ToString ();

                    if (!string.IsNullOrEmpty (entityText))
                    {
                        runs.Add (new InlineRun (entityText, defaultRole));
                    }

                    break;

                case HtmlInline:
                    // Inline HTML — skip in terminal context.
                    break;

                case AbbreviationInline abbreviation:
                    runs.Add (new InlineRun (abbreviation.Abbreviation.Label ?? string.Empty, defaultRole));

                    break;

                case LeafInline unknown:
                    // Preserve unsupported leaf syntax instead of silently dropping its text.
                    runs.Add (new InlineRun (GetInlineSource (unknown, source), defaultRole));

                    break;

                default:
                    // For unrecognized container inline types (e.g. PipeTableDelimiterInline),
                    // recurse into children to extract any text content.
                    if (inline is ContainerInline unknownContainer)
                    {
                        runs.AddRange (WalkInlines (unknownContainer.FirstChild, defaultRole, source, isTableCell));
                    }

                    break;
            }

            inline = inline.NextSibling;
        }

        return runs;
    }

    private static void TrimLeadingSpace (List<InlineRun> runs)
    {
        if (runs.Count == 0)
        {
            return;
        }

        string trimmed = runs [0].Text.TrimStart ();

        if (trimmed == runs [0].Text)
        {
            return;
        }

        InlineRun first = runs [0];
        runs [0] = first with
        {
            Text = trimmed
        };

        if (runs [0].Text.Length == 0)
        {
            runs.RemoveAt (0);
        }
    }

    internal static int GetBlockEndLine (Block block) =>
        block switch
        {
            FencedCodeBlock fcb => fcb.Line + fcb.Lines.Count + 1, // opening fence + content + closing fence
            CodeBlock cb => cb.Line + Math.Max (cb.Lines.Count - 1, 0),
            QuoteBlock qb => qb.Count > 0 ? GetBlockEndLine (qb [^1]) : qb.Line,
            ListBlock lb => lb.Count > 0 ? GetBlockEndLine (lb [^1]) : lb.Line,
            ListItemBlock lib => lib.Count > 0 ? GetBlockEndLine (lib [^1]) : lib.Line,
            DefinitionList definitions => definitions.Count > 0 ? GetBlockEndLine (definitions [^1]) : definitions.Line,
            DefinitionItem definition => definition.Count > 0 ? GetBlockEndLine (definition [^1]) : definition.Line,
            Table t => t.Count > 0 ? GetBlockEndLine (t [^1]) : t.Line,
            _ => block.Line
        };

    private void AddCodeBlockLines (IReadOnlyList<string> codeLines, string? language)
    {
        if (codeLines.Count == 0)
        {
            _blocks.Add (new IntermediateBlock ([new InlineRun ("", MarkdownStyleRole.CodeBlock, Role: VisualRole.Code)], false, isCodeBlock: true, language: language));

            return;
        }

        SyntaxHighlighter?.ResetState ();

        foreach (string line in codeLines)
        {
            IReadOnlyList<InlineRun> runs;

            if (SyntaxHighlighter is null)
            {
                runs = [new InlineRun (line, MarkdownStyleRole.CodeBlock, Role: VisualRole.Code)];
            }
            else
            {
                IReadOnlyList<StyledSegment> highlighted = SyntaxHighlighter.Highlight (line, language);
                List<InlineRun> converted = [];

                converted.AddRange (highlighted.Select (InlineRun.FromStyledSegment));

                runs = converted;
            }

            _blocks.Add (new IntermediateBlock (runs, false, isCodeBlock: true, language: language));
        }
    }

    private static string DeduplicateSlug (string baseSlug, Dictionary<string, int> slugCounts)
    {
        if (!slugCounts.TryGetValue (baseSlug, out int count))
        {
            slugCounts [baseSlug] = 1;

            return baseSlug;
        }

        slugCounts [baseSlug] = count + 1;
        var deduped = $"{baseSlug}-{count}";

        // Ensure the deduped slug itself is tracked
        slugCounts [deduped] = 1;

        return deduped;
    }
}
