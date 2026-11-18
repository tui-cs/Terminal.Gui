using TgMarkdown = Terminal.Gui.Views.Markdown;

namespace ViewsTests.Markdown;

// CoPilot - GPT-6
public class MarkdownAnchorLifecycleTests
{
    [Fact]
    public void Anchors_Survive_Resize_And_Are_Replaced_With_Content ()
    {
        using TgMarkdown view = new ()
        {
            Text = "# Same\n\nbody\n\n# Same",
            Width = 40,
            Height = 2
        };
        view.Layout ();
        Assert.True (view.ScrollToAnchor ("same-1"));
        view.Width = 20;
        view.Layout ();
        Assert.True (view.ScrollToAnchor ("same-1"));
        view.Text = "# New";
        view.Layout ();

        Assert.False (view.ScrollToAnchor ("same"));
        Assert.False (view.ScrollToAnchor ("same-1"));
        Assert.True (view.ScrollToAnchor ("new"));
    }

    [Fact]
    public void Narrow_Layout_Defers_Anchors_Until_Content_Can_Be_Rendered ()
    {
        using TgMarkdown view = new ()
        {
            Text = "# Heading",
            Width = 1,
            Height = 2
        };
        view.Layout ();
        Assert.False (view.ScrollToAnchor ("heading"));
        view.Width = 40;
        view.Layout ();

        Assert.True (view.ScrollToAnchor ("heading"));
    }
}
