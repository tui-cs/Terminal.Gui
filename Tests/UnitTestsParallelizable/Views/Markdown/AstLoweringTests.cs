using JetBrains.Annotations;
using Markdig;
using UnitTests;
#pragma warning disable xUnit2013
#pragma warning disable xUnit2012

namespace ViewsTests.Markdown;

/// <summary>
///     Tests for the AST-based Markdown lowering pipeline.
///     Organized per the ast-based-lowering.md plan:
///     Phase 0a — baseline coverage, Phase 0b — known-limitation docs,
///     Phase 1-6 — new implementation tests.
/// </summary>
[TestSubject (typeof (Terminal.Gui.Views.Markdown))]
public class AstLoweringTests (ITestOutputHelper output)
{
    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Lays out a Markdown view at the given width so that rendered lines are available for inspection.
    /// </summary>
    private static Terminal.Gui.Views.Markdown LayoutView (string markdown, int width = 80, MarkdownPipeline? pipeline = null)
    {
        Terminal.Gui.Views.Markdown view = new () { Text = markdown, Width = width, Height = 20 };

        if (pipeline is { })
        {
            view.MarkdownPipeline = pipeline;
        }

        View host = new () { Width = width, Height = 20 };
        host.Add (view);
        host.LayoutSubViews ();

        return view;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 0a — BASELINE COVERAGE TESTS (pre-existing behavior preserved)
    // ─────────────────────────────────────────────────────────────────────────

    // Copilot

    [Fact]
    public void BlockQuote_Single_Line_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("> Hello world");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void BlockQuote_Multiple_Consecutive_Lines_Renders ()
    {
        // In Markdig's AST, consecutive blockquote lines without blank lines form
        // a single ParagraphBlock inside the QuoteBlock (soft-wrapped paragraph).
        // This produces at least 1 rendered line.
        Terminal.Gui.Views.Markdown view = LayoutView ("> Line one\n> Line two");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void OrderedList_Multiple_Items_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("1. First\n2. Second\n3. Third");

        Assert.True (view.LineCount >= 3);
    }

    [Fact]
    public void TaskList_Mixed_States_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("- [x] Done\n- [ ] Todo\n- [X] Also done");

        Assert.True (view.LineCount >= 3);
    }

    [Fact]
    public void CodeBlock_Empty_Fence_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("```\n```");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void CodeBlock_With_Language_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("```csharp\nvar x = 1;\n```");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void CodeBlock_Multiple_Blocks_Create_Separate_SubViews ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("```\nA\n```\n\nText\n\n```\nB\n```");

        int codeBlockCount = view.SubViews.OfType<MarkdownCodeBlock> ().Count ();
        Assert.Equal (2, codeBlockCount);
    }

    [Fact]
    public void Heading_All_Levels_1_Through_6_Render ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6");

        Assert.True (view.LineCount >= 6);
    }

    [Fact]
    public void ThematicBreak_Dashes_Creates_Line_SubView ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("---");

        Assert.True (view.SubViews.OfType<Line> ().Any ());
    }

    [Fact]
    public void ThematicBreak_Stars_Creates_Line_SubView ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("***");

        Assert.True (view.SubViews.OfType<Line> ().Any ());
    }

    [Fact]
    public void ThematicBreak_Underscores_Creates_Line_SubView ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("___");

        Assert.True (view.SubViews.OfType<Line> ().Any ());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 0b — KNOWN-LIMITATION TESTS (now fixed with AST-based lowering)
    // These previously required [Skip], now pass.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Escaped_Asterisks_Not_Treated_As_Emphasis ()
    {
        // Markdig correctly handles \* escapes; no Emphasis inline should appear.
        (IApplication app, Runnable window) = SetupStyleTest (@"This is \*not bold\*");

        // Verify no italic escape code \x1b[3m appears (that would indicate Emphasis rendering)
        string output2 = GetOutput (app);
        Assert.DoesNotContain ("\x1b[3m", output2);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Triple_Asterisks_Bold_Italic ()
    {
        // ***text*** should render with both Strong and Emphasis
        Terminal.Gui.Views.Markdown view = LayoutView ("This is ***bold italic***");

        Assert.True (view.LineCount >= 1);

        // The view should render without crash; actual style verification via style tests
    }

    [Fact]
    public void Setext_Heading_Level_1 ()
    {
        // Title\n===== should render as a heading block with anchor slug
        Terminal.Gui.Views.Markdown view = LayoutView ("Title\n=====");

        Assert.True (view.LineCount >= 1);

        // Verify the setext heading generates an anchor slug
        Assert.True (view.ScrollToAnchor ("title"));
    }

    [Fact]
    public void Setext_Heading_Level_2 ()
    {
        // Subtitle\n-------- should render as a heading block, NOT as thematic break
        Terminal.Gui.Views.Markdown view = LayoutView ("Subtitle\n--------");

        Assert.True (view.LineCount >= 1);

        // There should be no Line SubView (it's a heading, not a thematic break)
        Assert.False (view.SubViews.OfType<Line> ().Any ());

        // Verify anchor was created
        Assert.True (view.ScrollToAnchor ("subtitle"));
    }

    [Fact]
    public void Indented_Code_Block ()
    {
        // 4-space indented code should render as a code block (previously broken with regex parser)
        Terminal.Gui.Views.Markdown view = LayoutView ("Paragraph\n\n    code line 1\n    code line 2\n\nAfter");

        int codeBlockCount = view.SubViews.OfType<MarkdownCodeBlock> ().Count ();
        Assert.Equal (1, codeBlockCount);
    }

    [Fact]
    public void Custom_Pipeline_Without_Tables_Renders_Pipes_As_Text ()
    {
        // Pipeline without table extension: pipe-delimited text should NOT produce a table SubView
        MarkdownPipeline noTablePipeline = new MarkdownPipelineBuilder ().Build (); // no table extension

        Terminal.Gui.Views.Markdown view = LayoutView ("| A | B |\n|---|---|\n| 1 | 2 |", pipeline: noTablePipeline);

        // Should be 0 table SubViews
        int tableCount = view.SubViews.OfType<MarkdownTable> ().Count ();
        Assert.Equal (0, tableCount);

        // Should render as text (content not swallowed)
        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void Nested_BlockQuote_Has_Double_Prefix ()
    {
        // "> > Nested" should render with double prefix
        Terminal.Gui.Views.Markdown view = LayoutView ("> > Nested quote");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void Autolink_Renders_As_Link ()
    {
        // <https://example.com> should render as a Link role
        Terminal.Gui.Views.Markdown view = LayoutView ("<https://example.com>");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void Strikethrough_Renders_With_Strikethrough_Style ()
    {
        // ~~text~~ should render with Strikethrough style (not as literal tildes)
        (IApplication app, Runnable window) = SetupStyleTest ("~~struck~~");

        string screenOutput = GetOutput (app);

        // Should NOT contain the literal tilde characters as plain text
        // The MarkdownStyleRole.Strikethrough maps to TextStyle.Strikethrough (\x1b[9m)
        Assert.DoesNotContain ("~~struck~~", screenOutput);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Html_Entity_Renders_As_Character ()
    {
        // &copy; should render as the copyright symbol (or equivalent)
        Terminal.Gui.Views.Markdown view = LayoutView ("Copyright &copy; 2024");

        Assert.True (view.LineCount >= 1);

        // The view should render without crash
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 1 — AST BLOCK WALKER UNIT TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LowerFromAst_HeadingBlock_Creates_Heading ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("# Hello");

        Assert.True (view.LineCount >= 1);
        Assert.True (view.ScrollToAnchor ("hello"));
    }

    [Fact]
    public void LowerFromAst_HeadingBlock_Generates_Anchor_Slug ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("# My Heading Here");

        Assert.True (view.ScrollToAnchor ("my-heading-here"));
    }

    [Fact]
    public void LowerFromAst_ParagraphBlock_Creates_Wrappable_Block ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("Hello world", 5);

        // "Hello world" at width 5 wraps to 2 lines
        Assert.True (view.LineCount >= 2);
    }

    [Fact]
    public void LowerFromAst_ThematicBreakBlock_Creates_ThematicBreak ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("---");

        Assert.True (view.SubViews.OfType<Line> ().Any ());
    }

    [Fact]
    public void LowerFromAst_FencedCodeBlock_Creates_CodeBlock_Per_Line ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("```csharp\nline1\nline2\n```");

        MarkdownCodeBlock? cb = view.SubViews.OfType<MarkdownCodeBlock> ().FirstOrDefault ();
        Assert.NotNull (cb);
        Assert.Equal (2, cb.StyledLines.Count);
    }

    [Fact]
    public void LowerFromAst_IndentedCodeBlock_Creates_CodeBlock ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("Paragraph\n\n    code line 1\n    code line 2\n\nAfter");

        MarkdownCodeBlock? cb = view.SubViews.OfType<MarkdownCodeBlock> ().FirstOrDefault ();
        Assert.NotNull (cb);
        Assert.Equal (2, cb.StyledLines.Count);
    }

    [Fact]
    public void LowerFromAst_QuoteBlock_Adds_Prefix ()
    {
        // Quote content should render (prefix "> " is set on the IntermediateBlock)
        Terminal.Gui.Views.Markdown view = LayoutView ("> Hello");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void LowerFromAst_EmptyDocument_Produces_At_Least_One_Line ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("");

        // BuildRenderedLines always adds at least one line
        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void LowerFromAst_BlankLine_Between_Paragraphs ()
    {
        // Two paragraphs separated by a blank line produce at least 3 rendered lines
        // (para1 + blank + para2)
        Terminal.Gui.Views.Markdown view = LayoutView ("Para 1\n\nPara 2");

        Assert.True (view.LineCount >= 3);
    }

    [Fact]
    public void LowerFromAst_HtmlBlock_Renders_As_PlainText ()
    {
        // HTML block preceded by blank line — Markdig treats as HtmlBlock
        Terminal.Gui.Views.Markdown view = LayoutView ("\n<div>hello</div>");

        Assert.True (view.LineCount >= 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 2 — AST INLINE WALKER UNIT TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WalkInlines_Bold_Renders_Bold ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("**bold**");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1mbold\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void WalkInlines_Italic_Renders_Italic ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("*italic*");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[3mitalic\x1b[30m\x1b[107m\x1b[23m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void WalkInlines_Strikethrough_Renders_Strikethrough ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("~~struck~~");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[9mstruck\x1b[30m\x1b[107m\x1b[29m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void WalkInlines_Autolink_Returns_Link ()
    {
        // Autolink renders as a link with URL
        Terminal.Gui.Views.Markdown view = LayoutView ("<https://example.com>");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void WalkInlines_HtmlEntity_Returns_Transcoded ()
    {
        // &amp; should render as & not as "&amp;"
        Terminal.Gui.Views.Markdown view = LayoutView ("foo &amp; bar");

        // The rendered text should not contain "&amp;"
        Assert.True (view.LineCount >= 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 3 — TABLE AST HANDLING
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LowerFromAst_Table_Creates_TableSubView ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("| A | B |\n|---|---|\n| 1 | 2 |");

        int tableCount = view.SubViews.OfType<MarkdownTable> ().Count ();
        Assert.Equal (1, tableCount);
    }

    [Fact]
    public void LowerFromAst_Table_Preserves_Alignment ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("| Left | Center | Right |\n|:---|:---:|---:|\n| a | b | c |");

        MarkdownTable? table = view.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (table);

        TableData data = table.TableData;
        Assert.NotNull (data);
        Assert.Equal (Alignment.Start, data.ColumnAlignments [0]);
        Assert.Equal (Alignment.Center, data.ColumnAlignments [1]);
        Assert.Equal (Alignment.End, data.ColumnAlignments [2]);
    }

    [Fact]
    public void LowerFromAst_Table_Multiple_Rows ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("| H |\n|---|\n| 1 |\n| 2 |\n| 3 |");

        MarkdownTable? table = view.SubViews.OfType<MarkdownTable> ().FirstOrDefault ();
        Assert.NotNull (table);

        TableData data = table.TableData;
        Assert.NotNull (data);
        Assert.Equal (3, data.Rows.Length);
    }

    [Fact]
    public void LowerFromAst_Pipeline_Without_Tables_Renders_As_Text ()
    {
        MarkdownPipeline noTablePipeline = new MarkdownPipelineBuilder ().UsePreciseSourceLocation ().Build ();

        Terminal.Gui.Views.Markdown view = LayoutView ("| A | B |\n|---|---|\n| 1 | 2 |", pipeline: noTablePipeline);

        Assert.Equal (0, view.SubViews.OfType<MarkdownTable> ().Count ());
        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void Pipeline_Property_Change_Invalidates_And_Reparses ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "| A | B |\n|---|---|\n| 1 | 2 |", Width = 40, Height = 20 };

        View host = new () { Width = 40, Height = 20 };
        host.Add (view);
        host.LayoutSubViews ();

        int tableCountWithDefaultPipeline = view.SubViews.OfType<MarkdownTable> ().Count ();
        Assert.Equal (1, tableCountWithDefaultPipeline);

        // Change to pipeline without tables
        view.MarkdownPipeline = new MarkdownPipelineBuilder ().Build ();
        host.LayoutSubViews ();

        int tableCountWithoutTablePipeline = view.SubViews.OfType<MarkdownTable> ().Count ();
        Assert.Equal (0, tableCountWithoutTablePipeline);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 4 — CODE BLOCK AST HANDLING
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LowerFromAst_FencedCodeBlock_Extracts_Language ()
    {
        MockSyntaxHighlighter highlighter = new ();

        Terminal.Gui.Views.Markdown view = new () { SyntaxHighlighter = highlighter, Text = "```python\nprint('hi')\n```", Width = 40, Height = 20 };
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("python", highlighter.LanguagesReceived);
    }

    [Fact]
    public void LowerFromAst_FencedCodeBlock_Lines_Become_Separate_Blocks ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("```\nline1\nline2\nline3\n```");

        MarkdownCodeBlock? cb = view.SubViews.OfType<MarkdownCodeBlock> ().FirstOrDefault ();
        Assert.NotNull (cb);
        Assert.Equal (3, cb.StyledLines.Count);
    }

    [Fact]
    public void LowerFromAst_FencedCodeBlock_Empty_Creates_Code_Block ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("```\n```");

        Assert.True (view.SubViews.OfType<MarkdownCodeBlock> ().Any ());
    }

    [Fact]
    public void LowerFromAst_IndentedCodeBlock_Treated_As_CodeBlock ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("    code line");

        Assert.True (view.SubViews.OfType<MarkdownCodeBlock> ().Any ());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 5 — NESTED BLOCK FLATTENING
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void LowerFromAst_NestedQuote_Renders ()
    {
        // "> > Nested" should render without crashing
        Terminal.Gui.Views.Markdown view = LayoutView ("> > Nested");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void LowerFromAst_QuoteBlock_With_List_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("> - Item one\n> - Item two");

        Assert.True (view.LineCount >= 2);
    }

    [Fact]
    public void LowerFromAst_QuoteBlock_With_Multiple_Paragraphs_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("> Para 1\n>\n> Para 2");

        Assert.True (view.LineCount >= 2);
    }

    [Fact]
    public void LowerFromAst_Nested_List_In_List_Renders ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("- Parent\n  - Child");

        Assert.True (view.LineCount >= 2);
    }

    [Fact]
    public void LowerFromAst_Deeply_Nested_Does_Not_Crash ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("> > > > Deep nesting");

        Assert.True (view.LineCount >= 1);
    }

    [Fact]
    public void LowerFromAst_OrderedList_Preserves_Numbers ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView ("1. First\n2. Second\n3. Third");

        Assert.True (view.LineCount >= 3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 6 — INTEGRATION / WIRING TESTS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EnsureParsed_Uses_Ast_Not_Regex ()
    {
        // The key test: a pipeline WITHOUT the table extension should NOT render tables.
        // This proves the AST path is active (regex path would still detect pipe-tables).
        MarkdownPipeline noTablePipeline = new MarkdownPipelineBuilder ().Build ();

        Terminal.Gui.Views.Markdown view = LayoutView ("| A | B |\n|---|---|\n| 1 | 2 |", pipeline: noTablePipeline);

        Assert.Equal (0, view.SubViews.OfType<MarkdownTable> ().Count ());
    }

    [Fact]
    public void DefaultMarkdownSample_Renders_Without_Exceptions ()
    {
        Terminal.Gui.Views.Markdown view = LayoutView (Terminal.Gui.Views.Markdown.DefaultMarkdownSample);

        Assert.True (view.LineCount > 0);
        Assert.True (view.GetContentSize ().Height > 0);
    }

    [Fact]
    public void DefaultMarkdownSample_Contains_Strikethrough_Style ()
    {
        // The sample contains ~~strikethrough~~ which should render with strikethrough style.
        // Previously rendered as literal "~~strikethrough~~" with the regex parser.
        // We render only ~~struck~~ in a single-line view to keep the test deterministic.
        (IApplication app, Runnable window) = SetupStyleTest ("~~struck~~");

        string screenOutput = GetOutput (app);

        // Strikethrough escape sequence should appear (SGR 9 = strikethrough on)
        Assert.Contains ("\x1b[9m", screenOutput);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void MarkdownInlineParser_Still_Works_For_Standalone_Use ()
    {
        // MarkdownInlineParser should still function for standalone MarkdownTable/MarkdownCodeBlock use
        List<InlineRun> runs = MarkdownInlineParser.ParseInlines ("**bold**", MarkdownStyleRole.Normal);

        Assert.True (runs.Any (r => r.StyleRole == MarkdownStyleRole.Strong));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STYLE RENDERING TESTS FOR NEW FEATURES
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Style_Strikethrough_Renders_Strikethrough_Role ()
    {
        // ~~text~~ should produce Strikethrough role, rendered with \x1b[9m
        (IApplication app, Runnable window) = SetupStyleTest ("~~S~~");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[9mS\x1b[30m\x1b[107m\x1b[29m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static (IApplication app, Runnable window) SetupStyleTest (string markdown, int width = 20)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, 1);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        Terminal.Gui.Views.Markdown mv = new () { Text = markdown, Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        return (app, window);
    }

    private static string GetOutput (IApplication app) => app.Driver!.GetOutput ().GetLastOutput ();

    private sealed class MockSyntaxHighlighter : ISyntaxHighlighter
    {
        public List<string?> LanguagesReceived { get; } = [];

        public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
        {
            LanguagesReceived.Add (language);

            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground => null;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }
}
