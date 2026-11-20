using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Terminal.Gui.Views;

/// <summary>Parse standalone table-cell Markdown with the same inline syntax and lowering as AST-backed tables.</summary>
internal static class MarkdownInlineParser
{
    private static readonly InlineParserList _parsers = CreateInlineParsers ();

    private static InlineParserList CreateInlineParsers ()
    {
        MarkdownPipelineBuilder builder = new ();
        builder.UseAdvancedExtensions ();
        builder.Build ();

        return new InlineParserList (builder.InlineParsers);
    }

    /// <summary>Parse inline Markdown source into display runs with semantic styles and link metadata.</summary>
    /// <param name="text">The cell's Markdown inline source, including any backslash escapes.</param>
    /// <param name="defaultRole">The semantic style for unformatted text.</param>
    /// <returns>The display runs for the cell.</returns>
    public static List<InlineRun> ParseInlines (string text, MarkdownStyleRole defaultRole)
    {
        MarkdownDocument document = new ();
        ParagraphBlock paragraph = new ()
        {
            Lines = new StringLineGroup (text)
        };
        document.Add (paragraph);

        // Retain spans so unsupported inline leaves can fall back to their original source.
        InlineProcessor processor = new (document, _parsers, true, null);

        try
        {
            // Process only inlines: leading '#' or '>' in a cell must remain ordinary text.
            processor.ProcessInlineLeaf (paragraph);
        }
        catch (ArgumentException)
        {
            return [new InlineRun (text, defaultRole)];
        }

        return Markdown.LowerCellInlines (paragraph.Inline?.FirstChild, defaultRole, text);
    }
}
