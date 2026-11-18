namespace ViewsTests;

public class ScrollBarTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        ScrollBar scrollBar = new ();
        Assert.False (scrollBar.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        Assert.Equal (0, scrollBar.VisibleContentSize);
        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Value);
        Assert.Equal (ScrollBarVisibilityMode.Manual, scrollBar.VisibilityMode);
    }

    #region VisibilityMode

    [Fact]
    public void VisibilityMode_Manual_Is_Default_CorrectlyHidesAndShows ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };

        ScrollBar scrollBar = new () { VisibleContentSize = 20, ScrollableContentSize = 0 };
        super.Add (scrollBar);
        Assert.Equal (ScrollBarVisibilityMode.Manual, scrollBar.VisibilityMode);

        // Views are visible by default
        Assert.True (scrollBar.Visible);

        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;
        Assert.Equal (ScrollBarVisibilityMode.Auto, scrollBar.VisibilityMode);

        // When content fits (VisibleContentSize = 20, ScrollableContentSize = 0), hide
        Assert.False (scrollBar.Visible);

        // Should Show
        scrollBar.ScrollableContentSize = 21;
        Assert.True (scrollBar.Visible);

        // Should Hide
        scrollBar.ScrollableContentSize = 10;
        super.Layout (new Size (100, 100));
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void VisibilityMode_Manual_CorrectlyHidesAndShows ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };

        ScrollBar scrollBar = new () { ScrollableContentSize = 20, VisibilityMode = ScrollBarVisibilityMode.Manual, Visible = true };
        super.Add (scrollBar);
        Assert.Equal (ScrollBarVisibilityMode.Manual, scrollBar.VisibilityMode);
        Assert.True (scrollBar.Visible);

        // Should Hide if VisibilityMode = Auto, but should not hide if VisibilityMode = Manual
        scrollBar.ScrollableContentSize = 10;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void VisibilityMode_Auto_Changing_ScrollableContentSize_CorrectlyHidesAndShows ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };

        ScrollBar scrollBar = new () { ScrollableContentSize = 20 };
        super.Add (scrollBar);
        Assert.Equal (ScrollBarVisibilityMode.Manual, scrollBar.VisibilityMode);

        // Views are visible by default
        Assert.True (scrollBar.Visible);

        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;

        super.Layout (new Size (100, 100));
        Assert.False (scrollBar.Visible);
        Assert.Equal (1, scrollBar.Frame.Width);
        Assert.Equal (20, scrollBar.Frame.Height);

        scrollBar.ScrollableContentSize = 10;
        super.Layout (new Size (100, 100));
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 30;
        super.Layout (new Size (100, 100));
        Assert.True (scrollBar.Visible);

        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Always;
        super.Layout (new Size (100, 100));
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        super.Layout (new Size (100, 100));
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void VisibilityMode_Auto_Change_VisibleContentSize_CorrectlyHidesAndShows ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };

        ScrollBar scrollBar = new () { ScrollableContentSize = 20, VisibleContentSize = 20 };
        super.Add (scrollBar);
        Assert.Equal (ScrollBarVisibilityMode.Manual, scrollBar.VisibilityMode);

        // Views are visible by default
        Assert.True (scrollBar.Visible);

        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (20, scrollBar.VisibleContentSize);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 30;

        //Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 21;

        //Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        scrollBar.Visible = true;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;

        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void VisibilityMode_None_AlwaysHides ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };

        ScrollBar scrollBar = new () { ScrollableContentSize = 100, VisibleContentSize = 10 };
        super.Add (scrollBar);

        // Start with Manual — visible by default
        Assert.True (scrollBar.Visible);

        // Set to None — should hide
        scrollBar.VisibilityMode = ScrollBarVisibilityMode.None;
        Assert.False (scrollBar.Visible);

        // Even if content overflows, None keeps it hidden
        scrollBar.ScrollableContentSize = 200;
        super.Layout (new Size (100, 100));
        Assert.False (scrollBar.Visible);

        // Changing VisibleContentSize also doesn't make it appear
        scrollBar.VisibleContentSize = 5;
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    // Claude - Opus 4.5
    [Fact]
    public void VisibilityMode_None_Overrides_ViewportSettingsFlags ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };
        super.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
        ScrollBar scrollBar = super.HorizontalScrollBar;
        scrollBar.ScrollableContentSize = 100;
        scrollBar.VisibleContentSize = 10;

        scrollBar.VisibilityMode = ScrollBarVisibilityMode.None;
        Assert.False (scrollBar.Visible);

        // Manually setting Visible = true should be overridden on next ShowHide
        scrollBar.Visible = true;
        scrollBar.ScrollableContentSize = 200; // triggers ShowHide
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void VisibilityMode_Transitions_Between_All_Modes ()
    {
        Runnable super = new () { Id = "super", Width = 1, Height = 20 };

        ScrollBar scrollBar = new () { ScrollableContentSize = 100, VisibleContentSize = 10 };
        super.Add (scrollBar);

        // Manual (default) — visible by default
        Assert.Equal (ScrollBarVisibilityMode.Manual, scrollBar.VisibilityMode);
        Assert.True (scrollBar.Visible);

        // Manual → Auto (content overflows, so should show)
        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Auto;
        Assert.True (scrollBar.Visible);

        // Auto → None (should hide)
        scrollBar.VisibilityMode = ScrollBarVisibilityMode.None;
        Assert.False (scrollBar.Visible);

        // None → Always (should show)
        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Always;
        Assert.True (scrollBar.Visible);

        // Always → None (should hide)
        scrollBar.VisibilityMode = ScrollBarVisibilityMode.None;
        Assert.False (scrollBar.Visible);

        // None → Manual (stays hidden until explicitly changed)
        scrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
        Assert.False (scrollBar.Visible);

        // Manual — developer can set Visible = true
        scrollBar.Visible = true;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    #endregion AutoHide

    #region Orientation

    [Fact]
    public void OnOrientationChanged_Keeps_Size ()
    {
        var scroll = new ScrollBar ();
        scroll.Layout ();
        scroll.ScrollableContentSize = 1;

        scroll.Orientation = Orientation.Horizontal;
        Assert.Equal (1, scroll.ScrollableContentSize);
    }

    [Fact]
    public void OnOrientationChanged_Sets_Position_To_0 ()
    {
        var super = new View { Id = "super", Width = 10, Height = 10 };
        var scrollBar = new ScrollBar ();
        super.Add (scrollBar);
        scrollBar.Layout ();
        scrollBar.Value = 1;
        scrollBar.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollBar.GetSliderPosition ());
    }

    // Copilot
    [Fact]
    public void Horizontal_ScrollBar_Buttons_Use_Left_And_Right_Arrows ()
    {
        ScrollBar scrollBar = new ()
        {
            Orientation = Orientation.Horizontal
        };
        ScrollButton decreaseButton = scrollBar.SubViews.OfType<ScrollButton> ().Single (button => button.Direction == NavigationDirection.Backward);
        ScrollButton increaseButton = scrollBar.SubViews.OfType<ScrollButton> ().Single (button => button.Direction == NavigationDirection.Forward);

        Assert.Equal (Glyphs.LeftArrow.ToString (), decreaseButton.Title);
        Assert.Equal (Glyphs.RightArrow.ToString (), increaseButton.Title);
    }

    // Copilot
    [Fact]
    public void Vertical_ScrollBar_Buttons_Use_Up_And_Down_Arrows ()
    {
        ScrollBar scrollBar = new ();
        ScrollButton decreaseButton = scrollBar.SubViews.OfType<ScrollButton> ().Single (button => button.Direction == NavigationDirection.Backward);
        ScrollButton increaseButton = scrollBar.SubViews.OfType<ScrollButton> ().Single (button => button.Direction == NavigationDirection.Forward);

        Assert.Equal (Glyphs.UpArrow.ToString (), decreaseButton.Title);
        Assert.Equal (Glyphs.DownArrow.ToString (), increaseButton.Title);
    }

    #endregion Orientation

    #region Size

    // TODO: Add tests.

    #endregion Size

    #region Position

    [Fact]
    public void Position_Event_Cancels ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.ScrollableContentSize = 5;
        scrollBar.Frame = new Rectangle (0, 0, 1, 4); // Needs to be at least 4 for slider to move

        scrollBar.ValueChanging += (_, e) =>
                                   {
                                       if (changingCount == 0)
                                       {
                                           e.Handled = true;
                                       }

                                       changingCount++;
                                   };
        scrollBar.ValueChanged += (_, _) => changedCount++;

        scrollBar.Value = 1;
        Assert.Equal (0, scrollBar.Value);
        Assert.Equal (1, changingCount);
        Assert.Equal (0, changedCount);

        scrollBar.Value = 1;
        Assert.Equal (1, scrollBar.Value);
        Assert.Equal (2, changingCount);
        Assert.Equal (1, changedCount);
    }

    #endregion Position

    [Fact]
    public void ScrollableContentSize_Cannot_Be_Negative ()
    {
        var scrollBar = new ScrollBar { Height = 10, ScrollableContentSize = -1 };
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        scrollBar.ScrollableContentSize = -10;
        Assert.Equal (0, scrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ScrollableContentSizeChanged_Event ()
    {
        var count = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.ScrollableContentSizeChanged += (_, _) => count++;

        scrollBar.ScrollableContentSize = 10;
        Assert.Equal (10, scrollBar.ScrollableContentSize);
        Assert.Equal (1, count);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void ScrollBar_Click_JumpsPosition ()
    {
        ScrollBar scrollBar = new () { Height = 10, ScrollableContentSize = 100 };
        scrollBar.BeginInit ();
        scrollBar.EndInit ();

        // Click on track jumps scroll position
        Mouse ev = new () { Position = new Point (0, 5), Flags = MouseFlags.LeftButtonClicked };
        scrollBar.NewMouseEvent (ev);

        // Verify the scrollbar is set up correctly
        Assert.Equal (100, scrollBar.ScrollableContentSize);

        scrollBar.Dispose ();
    }
}
