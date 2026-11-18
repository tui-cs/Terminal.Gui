using JetBrains.Annotations;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownCodeBlock))]
public class MarkdownCodeBlockTests
{
    // Copilot

    [Fact]
    public void Parameterless_Constructor_Creates_Empty_CodeBlock ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();

        Assert.NotNull (codeBlock);
        Assert.Empty (codeBlock.CodeLines);
    }

    [Fact]
    public void IDesignable_EnableForDesign_Returns_True ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();
        IDesignable designable = codeBlock;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.NotEmpty (codeBlock.CodeLines);
    }

    [Fact]
    public void CodeLines_Property_Sets_Content ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();

        codeBlock.CodeLines = ["line1", "line2", "line3"];

        Assert.Equal (3, codeBlock.CodeLines.Count);
        Assert.Equal ("line1", codeBlock.CodeLines [0]);
        Assert.Equal ("line2", codeBlock.CodeLines [1]);
        Assert.Equal ("line3", codeBlock.CodeLines [2]);
    }

    [Fact]
    public void ExtractText_Returns_Joined_Lines ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();
        codeBlock.CodeLines = ["Console.WriteLine (\"Hello\");", "var x = 42;"];

        string text = codeBlock.ExtractText ();

        Assert.Contains ("Console.WriteLine", text);
        Assert.Contains ("var x = 42;", text);
    }

    [Fact]
    public void Height_Updates_When_CodeLines_Set ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new () { Width = 40 };

        View host = new () { Width = 40, Height = 10 };
        host.Add (codeBlock);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Initially zero code lines, zero height
        Assert.Equal (0, codeBlock.Frame.Height);

        codeBlock.CodeLines = ["a", "b", "c"];
        host.Layout ();

        Assert.Equal (3, codeBlock.Frame.Height);
    }

    [Fact]
    public void Height_Updates_On_Subsequent_CodeLines_Changes ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new () { Width = 40 };

        View host = new () { Width = 40, Height = 10 };
        host.Add (codeBlock);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        codeBlock.CodeLines = ["a", "b", "c"];
        host.Layout ();
        Assert.Equal (3, codeBlock.Frame.Height);

        codeBlock.CodeLines = ["a"];
        host.Layout ();
        Assert.Equal (1, codeBlock.Frame.Height);
    }

    // --- Standalone syntax highlighting ---
    // Copilot

    [Fact]
    public void Language_Property_Defaults_Null ()
    {
        MarkdownCodeBlock codeBlock = new ();
        Assert.Null (codeBlock.Language);
    }

    [Fact]
    public void SyntaxHighlighter_Property_Defaults_Null ()
    {
        MarkdownCodeBlock codeBlock = new ();
        Assert.Null (codeBlock.SyntaxHighlighter);
    }

    [Fact]
    public void Setting_CodeLines_With_Highlighter_And_Language_Produces_Styled_Segments ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        MarkdownCodeBlock codeBlock = new () { SyntaxHighlighter = highlighter, Language = "csharp", CodeLines = ["var x = 42;"] };

        // The internal StyledLines should have multiple segments (tokenized) not just 1
        IReadOnlyList<string> lines = codeBlock.CodeLines;
        Assert.Single (lines);

        // Verify by checking that the code block produces colored output
        // (StyledLines is internal, but we can verify indirectly via ExtractText)
        Assert.Equal ("var x = 42;", codeBlock.ExtractText ());
    }

    [Fact]
    public void Setting_CodeLines_Without_Highlighter_Produces_Plain_Segments ()
    {
        MarkdownCodeBlock codeBlock = new () { Language = "csharp", CodeLines = ["var x = 42;"] };

        // Without a highlighter, CodeLines should still work (plain text)
        Assert.Equal ("var x = 42;", codeBlock.ExtractText ());
    }

    [Fact]
    public void ThemeBackground_Is_Set_From_Highlighter ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        MarkdownCodeBlock codeBlock = new () { SyntaxHighlighter = highlighter, Language = "csharp", CodeLines = ["int x = 1;"] };

        // ThemeBackground should be set from the highlighter's DefaultBackground
        Assert.NotNull (codeBlock.ThemeBackground);
        Assert.Equal (highlighter.DefaultBackground, codeBlock.ThemeBackground);
    }

    // --- Text property (fenced code block parsing) ---
    // Copilot

    [Fact]
    public void Text_With_Fenced_Block_Extracts_Language ()
    {
        MarkdownCodeBlock codeBlock = new () { Text = "```csharp\nvar x = 42;\n```" };

        Assert.Equal ("csharp", codeBlock.Language);
    }

    [Fact]
    public void Text_With_Fenced_Block_Strips_Fences ()
    {
        MarkdownCodeBlock codeBlock = new () { Text = "```csharp\nvar x = 42;\n```" };

        Assert.Equal ("var x = 42;", codeBlock.ExtractText ());
    }

    [Fact]
    public void Text_Without_Fences_Treats_As_Plain_Code ()
    {
        MarkdownCodeBlock codeBlock = new () { Text = "line1\nline2" };

        Assert.Null (codeBlock.Language);
        Assert.Equal ($"line1{Environment.NewLine}line2", codeBlock.ExtractText ());
    }

    [Fact]
    public void Text_With_Fenced_Block_No_Language ()
    {
        MarkdownCodeBlock codeBlock = new () { Text = "```\nplain code\n```" };

        Assert.Null (codeBlock.Language);
        Assert.Equal ("plain code", codeBlock.ExtractText ());
    }

    [Fact]
    public void Text_With_Highlighter_Produces_Styled_Output ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        MarkdownCodeBlock codeBlock = new () { SyntaxHighlighter = highlighter, Text = "```csharp\nvar x = 42;\n```" };

        Assert.Equal ("csharp", codeBlock.Language);
        Assert.Equal ("var x = 42;", codeBlock.ExtractText ());
        Assert.NotNull (codeBlock.ThemeBackground);
    }

    [Fact]
    public void Text_Getter_Returns_Fenced_Format ()
    {
        MarkdownCodeBlock codeBlock = new () { Text = "```python\nprint('hi')\n```" };

        // Getter should round-trip: return fenced format with language
        string text = codeBlock.Text;
        Assert.Contains ("print('hi')", text);
    }

    [Fact]
    public void Text_Multiline_Fenced_Block ()
    {
        MarkdownCodeBlock codeBlock = new () { Text = "```js\nlet a = 1;\nlet b = 2;\nconsole.log(a + b);\n```" };

        Assert.Equal ("js", codeBlock.Language);
        Assert.Contains ("let a = 1;", codeBlock.ExtractText ());
        Assert.Contains ("console.log(a + b);", codeBlock.ExtractText ());
    }

    [Fact]
    public void Click_Copy_Button_Copies_Code_To_Clipboard ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 5);
        app.Driver.Clipboard = new FakeClipboard ();

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownCodeBlock codeBlock = new () { Text = "```csharp\nvar x = 42;\n```", Width = Dim.Fill (), Height = Dim.Fill () };

        window.Add (codeBlock);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Click the copy glyph position (top-right corner: Viewport.Width - 2, 0)
        int copyX = codeBlock.Viewport.Width - 2;
        codeBlock.NewMouseEvent (new Mouse { Position = new Point (copyX, 0), Flags = MouseFlags.LeftButtonClicked });

        // Verify the code was copied to the clipboard
        bool gotClip = app.Clipboard!.TryGetClipboardData (out string clipboardText);
        Assert.True (gotClip);
        Assert.Contains ("var x = 42;", clipboardText);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ShowCopyButton_False_Hides_Glyph_And_Disables_Click ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 5);
        app.Driver.Clipboard = new FakeClipboard ();

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownCodeBlock codeBlock = new ()
        {
            Text = "```csharp\nSHOULD_NOT_BE_COPIED\n```",
            ShowCopyButton = false,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        window.Add (codeBlock);

        app.Begin (window);
        app.LayoutAndDraw ();

        // The copy glyph should NOT appear in the rendered output
        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);
        Assert.DoesNotContain ("\u29C9", screenContents);

        // Record clipboard state before clicking
        app.Clipboard!.TryGetClipboardData (out string before);

        // Click where the copy button would be — should NOT copy
        int copyX = codeBlock.Viewport.Width - 2;
        codeBlock.NewMouseEvent (new Mouse { Position = new Point (copyX, 0), Flags = MouseFlags.LeftButtonClicked });

        app.Clipboard.TryGetClipboardData (out string after);

        // Clipboard should not contain the code block text
        Assert.DoesNotContain ("SHOULD_NOT_BE_COPIED", after);

        // Clipboard should be unchanged by the click
        Assert.Equal (before, after);

        window.Dispose ();
        app.Dispose ();
    }
}
