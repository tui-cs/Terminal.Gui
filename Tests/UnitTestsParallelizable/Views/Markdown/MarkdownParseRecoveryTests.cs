using Markdig;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests;
using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownParseRecoveryTests : TestDriverBase
{
    [Fact]
    public void Parse_ArgumentException_Does_Not_Depend_On_Message ()
    {
        using IDriver driver = CreateTestDriver (60, 10);
        int parses = 0;
        MarkdownPipelineBuilder builder = new ();
        builder.DocumentProcessed += _ =>
                                     {
                                         parses++;

                                         throw new ArgumentException ("A different parser failure message");
                                     };
        using TgMarkdown view = new ()
        {
            Driver = driver,
            Text = "**literal**",
            MarkdownPipeline = builder.Build (),
            Width = 60,
            Height = 10
        };
        view.Layout ();
        view.Width = 40;
        view.Layout ();

        view.Draw ();

        Assert.Equal (1, parses);
        Assert.Equal ("**literal**", ReadRow (driver, 0));
    }

    [Fact]
    public void Parse_Fallback_Logs_The_Original_Exception ()
    {
        ArgumentException failure = new ("custom pipeline failure");
        MarkdownPipelineBuilder builder = new ();
        builder.DocumentProcessed += _ => throw failure;
        Mock<ILogger> logger = new ();
        using IDisposable scope = Logging.PushLogger (logger.Object);
        using TgMarkdown view = new ()
        {
            MarkdownPipeline = builder.Build (),
            Text = "literal",
            Width = 60,
            Height = 10
        };
        view.Layout ();

        Assert.Contains (logger.Invocations, invocation => invocation.Method.Name == nameof (ILogger.Log)
                                                          && Equals (invocation.Arguments [0], LogLevel.Warning)
                                                          && ReferenceEquals (invocation.Arguments [3], failure));
    }

    [Fact]
    public void Parse_Fallback_Preserves_Blank_Source_Lines ()
    {
        using IDriver driver = CreateTestDriver (60, 10);
        MarkdownPipelineBuilder builder = new ();
        builder.DocumentProcessed += _ => throw new ArgumentException ("parse failure");
        using TgMarkdown view = new ()
        {
            MarkdownPipeline = builder.Build (),
            Driver = driver,
            Text = "first\n\nthird",
            Width = 60,
            Height = 10
        };
        view.Layout ();

        view.Draw ();

        Assert.Equal (3, view.LineCount);
        Assert.Equal (["first", "", "third"], Enumerable.Range (0, 3).Select (row => ReadRow (driver, row)));
    }

    [Fact]
    public void Depth_Fallback_Resets_Anchors_And_Slugs_And_Allows_Recovery ()
    {
        using TgMarkdown view = new ()
        {
            MarkdownPipeline = new MarkdownPipelineBuilder { MaximumNestingDepth = 16 }.Build (),
            Text = "# Old",
            Width = 60,
            Height = 10
        };
        view.Layout ();
        Assert.True (view.ScrollToAnchor ("old"));
        view.Text = new string ('>', 20);
        view.Layout ();

        Assert.Equal (1, view.LineCount);
        Assert.False (view.ScrollToAnchor ("old"));

        view.Text = "# Old\n\n# Old";
        view.Layout ();

        Assert.True (view.ScrollToAnchor ("old"));
        Assert.True (view.ScrollToAnchor ("old-1"));
        Assert.False (view.ScrollToAnchor ("old-2"));
    }

    [Fact]
    public void ArgumentException_From_Lowering_Is_Not_Swallowed ()
    {
        Mock<ISyntaxHighlighter> highlighter = new ();
        highlighter.Setup (value => value.Highlight (It.IsAny<string> (), It.IsAny<string?> ())).Throws (new ArgumentException ("lowering"));
        using TgMarkdown view = new ()
        {
            SyntaxHighlighter = highlighter.Object,
            Width = 60,
            Height = 10
        };

        ArgumentException exception = Assert.Throws<ArgumentException> (() =>
        {
            view.Text = "```cs\ncode\n```";
            view.Layout ();
        });

        Assert.Equal ("lowering", exception.Message);
    }

    private static string ReadRow (IDriver driver, int row) =>
        string.Concat (Enumerable.Range (0, driver.Screen.Width).Select (column => driver.Contents! [row, column].Grapheme)).TrimEnd ();
}
