using JetBrains.Annotations;
using Terminal.Gui.Drawing;

namespace ViewsTests.Markdown;

// Copilot

[TestSubject (typeof (Terminal.Gui.Views.Markdown))]
public class MarkdownRenderToAnsiTests
{
    [Fact]
    public void RenderToAnsi_BasicMarkdown_ReturnsNonEmptyAnsi ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Hello\n\nWorld" };

        string result = view.RenderToAnsi ();

        Assert.NotEmpty (result);
        // ANSI escape sequences start with ESC [
        Assert.Contains ("\x1b[", result);
        // The text content should be present
        Assert.Contains ("Hello", result);
        Assert.Contains ("World", result);
    }

    [Fact]
    public void RenderToAnsi_WithMarkdownParameter_OverridesText ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "Original" };

        string result = view.RenderToAnsi ("**Override**");

        Assert.Contains ("Override", result);
        Assert.DoesNotContain ("Original", result);
    }

    [Fact]
    public void RenderToAnsi_EmptyText_ReturnsEmpty ()
    {
        Terminal.Gui.Views.Markdown view = new ();

        string result = view.RenderToAnsi ("");

        Assert.Equal (string.Empty, result);
    }

    [Fact]
    public void RenderToAnsi_NullMarkdownUsesInstanceText ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Title" };

        string result = view.RenderToAnsi (null);

        Assert.Contains ("Title", result);
    }

    [Fact]
    public void RenderToAnsi_WidthAffectsWrapping ()
    {
        string longLine = "This is a very long line that should be wrapped when the width is narrow enough to force wrapping behavior.";
        Terminal.Gui.Views.Markdown view = new () { Text = longLine };

        string narrow = view.RenderToAnsi (width: 20);
        string wide = view.RenderToAnsi (width: 200);

        // Narrow output should have more newlines (more lines due to wrapping)
        int narrowNewlines = narrow.Split ('\n').Length;
        int wideNewlines = wide.Split ('\n').Length;
        Assert.True (narrowNewlines > wideNewlines, $"Narrow ({narrowNewlines} lines) should have more lines than wide ({wideNewlines} lines)");
    }

    [Fact]
    public void RenderToAnsi_WithSyntaxHighlighter_ProducesOutput ()
    {
        Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "```csharp\nint x = 42;\n```",
            SyntaxHighlighter = new TextMateSyntaxHighlighter ()
        };

        string result = view.RenderToAnsi ();

        Assert.NotEmpty (result);
        Assert.Contains ("42", result);
    }

    [Fact]
    public void RenderToAnsi_SmallWidth_ClampsToMinimum ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "Hello" };

        // Width below MIN_WRAP_WIDTH (4) should not throw
        string result = view.RenderToAnsi (width: 1);

        Assert.NotEmpty (result);
    }

    [Fact]
    public void RenderToAnsi_DoesNotMutateInstance ()
    {
        Terminal.Gui.Views.Markdown view = new () { Text = "# Original" };

        _ = view.RenderToAnsi ("# Different");

        // Original text should be unchanged
        Assert.Equal ("# Original", view.Text);
    }

    [Fact]
    public void RenderToAnsi_UseThemeBackground_False_ProducesOutput ()
    {
        Terminal.Gui.Views.Markdown view = new ()
        {
            Text = "# Hello",
            UseThemeBackground = false
        };

        string result = view.RenderToAnsi ();

        Assert.NotEmpty (result);
        Assert.Contains ("Hello", result);
    }
}
