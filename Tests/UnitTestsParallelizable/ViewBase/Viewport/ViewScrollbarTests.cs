using System.Collections.ObjectModel;

namespace ViewBaseTests.Viewport;

public class ViewScrollbarTests (ITestOutputHelper output)
{
    [Fact]
    public void Horizonal_Constructor_Defaults ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);
        Assert.Null (view.Padding.View);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.False (view.HorizontalScrollBar.Visible);
    }

    [Fact]
    public void Horizontal_ScrollBarVisibilityMode_Manual_Creates_Invisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.HorizontalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        Assert.False (view.HorizontalScrollBar.Visible);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.Equal (new Rectangle (0, 10, 10, 1), view.HorizontalScrollBar.Frame);
    }

    [Fact]
    public void Horizontal_HasHorizontalScrollbar_Sets_VisibilityMode_Auto ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;
        Assert.Equal (ScrollBarVisibilityMode.Auto, view.HorizontalScrollBar.VisibilityMode);
        Assert.False (view.HorizontalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 0, 0), view.Padding.Thickness);
    }

    [Fact]
    public void Horizontal_HasHorizontalScrollBar_SetContentSize_MakesVisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;
        view.SetContentSize (new Size (20, 10));
        Assert.True (view.HorizontalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 0, 1), view.Padding.Thickness);
    }

    [Fact] // Copilot
    public void Horizontal_ScrollBar_Initialized_When_View_Initialized ()
    {
        View view = new ();

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;

        Assert.False (view.IsInitialized);
        Assert.False (view.HorizontalScrollBar.IsInitialized);
        Assert.False (view.Padding.View?.IsInitialized);

        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.True (view.Padding.View?.IsInitialized);
        Assert.True (view.HorizontalScrollBar.IsInitialized);
        Assert.False (view.HorizontalScrollBar.Visible);

        Assert.Equal (0, view.HorizontalScrollBar.ScrollableContentSize);
    }

    [Fact] // Copilot
    public void Horizontal_ScrollBar_View_Set_Frame_Updates_ScrollableContentSize ()
    {
        View view = new ();

        view.ViewportSettings = ViewportSettingsFlags.HasHorizontalScrollBar;

        Assert.Equal (0, view.HorizontalScrollBar.ScrollableContentSize);
        Assert.Equal (0, view.HorizontalScrollBar.VisibleContentSize);
        view.Frame = new Rectangle (0, 0, 10, 10);
        Assert.Equal (new Size (10, 10), view.GetContentSize ());
        Assert.Equal (new Size (10, 10), view.Viewport.Size);
        Assert.Equal (10, view.HorizontalScrollBar.VisibleContentSize);
        Assert.Equal (10, view.HorizontalScrollBar.ScrollableContentSize);
        Assert.False (view.HorizontalScrollBar.Visible);
    }

    [Fact]
    public void Vertical_Constructor_Defaults ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);
        Assert.Null (view.Padding.View);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.False (view.VerticalScrollBar.Visible);
    }

    [Fact]
    public void Vertical_ScrollBarVisibilityMode_Manual_Creates_Invisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        Assert.False (view.VerticalScrollBar.Visible);
        Assert.Equal (Thickness.Empty, view.Padding.Thickness);
        Assert.Equal (new Rectangle (10, 0, 1, 10), view.VerticalScrollBar.Frame);
    }

    [Fact]
    public void Vertical_HasVerticalScrollbar_Sets_VisibilityMode_Auto ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
        Assert.Equal (ScrollBarVisibilityMode.Auto, view.VerticalScrollBar.VisibilityMode);
        Assert.False (view.VerticalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 0, 0), view.Padding.Thickness);
    }

    [Fact]
    public void Vertical_HasVerticalScrollBar_SetContentSize_MakesVisible ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (10, 20));
        Assert.True (view.VerticalScrollBar.Visible);
        Assert.Equal (new Thickness (0, 0, 1, 0), view.Padding.Thickness);
    }

    [Fact] // Copilot
    public void Vertical_ScrollBar_Initialized_When_View_Initialized ()
    {
        View view = new ();
        view.Frame = new Rectangle (0, 0, 10, 10);

        view.ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (10, 20));

        Assert.False (view.IsInitialized);
        Assert.False (view.VerticalScrollBar.IsInitialized);

        view.BeginInit ();
        view.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.True (view.VerticalScrollBar.IsInitialized);
        Assert.True (view.VerticalScrollBar.Visible);
    }

    [Fact] // Copilot
    public void Vertical_ScrollBar_With_Border_Positioned_At_Right_Edge ()
    {
        View view = new ()
        {
            Frame = new Rectangle (0, 0, 20, 10),
            BorderStyle = LineStyle.Rounded
        };

        view.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
        view.SetContentSize (new Size (20, 30));

        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // Verify scrollbar is visible
        Assert.True (view.VerticalScrollBar.Visible);

        // Border should add thickness (1,1,1,1) for Rounded style
        Assert.Equal (new Thickness (1, 1, 1, 1), view.Border.Thickness);

        // Padding should have Right=1 for the scrollbar
        Assert.Equal (new Thickness (0, 0, 1, 0), view.Padding.Thickness);

        // PaddingView frame should be inside the border
        Rectangle expectedPaddingFrame = new Rectangle (1, 1, 18, 8);
        Assert.Equal (expectedPaddingFrame, view.Padding.View!.Frame);

        // The scrollbar should be at the right edge of PaddingView
        // PaddingView width = 18, ScrollBar width = 1
        // AnchorEnd() = 18 - 1 = 17, minus Func(Padding.Thickness.Right - 1) = Func(0) = 0
        // So X should be 17
        Assert.Equal (17, view.VerticalScrollBar.Frame.X);
    }

    [Fact] // Copilot
    public void ListView_Scrolled_Items_Draw_At_Top_Of_Viewport ()
    {
        // Regression: when a ListView with a scrollbar is scrolled and then LayoutAndDraw runs,
        // the items at the top of the visible area must render (not be blank).
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 10);

        using Runnable top = new ();
        SessionToken? token = app.Begin (top);

        ListView lv = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 8,
            Source = new ListWrapper<string> (["Item_00", "Item_01", "Item_02", "Item_03", "Item_04",
                                               "Item_05", "Item_06", "Item_07", "Item_08", "Item_09",
                                               "Item_10", "Item_11", "Item_12", "Item_13", "Item_14",
                                               "Item_15", "Item_16", "Item_17", "Item_18", "Item_19"]),
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar
        };

        top.Add (lv);
        app.LayoutAndDraw ();

        Assert.True (lv.VerticalScrollBar.Visible, "ScrollBar should be visible");
        Assert.Equal (0, lv.Viewport.Y);

        // Scroll down 5 items
        lv.VerticalScrollBar.Value = 5;
        app.LayoutAndDraw ();

        output.WriteLine ($"Viewport.Y = {lv.Viewport.Y}");
        Assert.Equal (5, lv.Viewport.Y);

        // Check the rendered output
        Cell [,]? contents = app.Driver.GetOutputBuffer ()?.Contents;
        Assert.NotNull (contents);

        // The first visible row should show "Item_05"
        var topRowText = "";

        for (var col = 0; col < 7; col++)
        {
            topRowText += contents [0, col].Grapheme;
        }

        output.WriteLine ($"Row 0 text: '{topRowText}'");
        Assert.Contains ("Item_05", topRowText);

        app.End (token!);
    }
}
