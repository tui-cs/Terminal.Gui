#nullable enable

namespace ViewsTests.TextViewTests;

/// <summary>
/// Tests for TextView scrollbar integration when content changes.
/// These tests expose bugs where scrollbars don't update after text modifications.
/// Claude - Opus 4.5
/// </summary>
public class TextViewScrollingTests
{
    /// <summary>
    /// Tests that content size height updates when lines are inserted.
    /// </summary>
    [Fact]
    public void ContentSize_Updates_When_Lines_Inserted ()
    {
        // Arrange: TextView with small viewport
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Initial content: 1 line
        tv.Text = "Line 1";
        int initialHeight = tv.GetContentSize ().Height;

        // Act: Insert text that adds lines (simulating typing)
        tv.Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10";

        // Assert: Content size height should reflect new line count
        int newHeight = tv.GetContentSize ().Height;
        Assert.Equal (10, newHeight);
        Assert.True (newHeight > initialHeight, $"Expected content height to increase from {initialHeight} to at least 10, but got {newHeight}");
    }

    /// <summary>
    /// Tests that content size height updates when lines are deleted.
    /// </summary>
    [Fact]
    public void ContentSize_Updates_When_Lines_Deleted ()
    {
        // Arrange: TextView with multi-line content
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        int initialHeight = tv.GetContentSize ().Height;
        Assert.Equal (10, initialHeight);

        // Act: Delete text (reduce to 3 lines)
        tv.Text = "Line 1\nLine 2\nLine 3";

        // Assert: Content size height should reflect reduced line count
        int newHeight = tv.GetContentSize ().Height;
        Assert.Equal (3, newHeight);
    }

    /// <summary>
    /// Tests that vertical scrollbar becomes visible when content grows to exceed viewport.
    /// This is one of the key bug scenarios reported.
    /// BUG: UpdateHorizontalScrollBarVisibility() is not called when Text property changes.
    /// </summary>
    [Fact]
    public void VerticalScrollBar_Becomes_Visible_When_Content_Exceeds_Viewport ()
    {
        // Arrange: TextView with ScrollBars=true, content initially fits in viewport
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2" // 2 lines, fits in height=5
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Verify: Scrollbar should not be visible initially (content fits)
        // Note: ContentSize is correctly updated (height=2), but visibility check isn't triggered
        Assert.Equal (2, tv.GetContentSize ().Height); // This passes - content size IS updated
        Assert.False (tv.VerticalScrollBar.Visible, "Scrollbar should not be visible when content fits in viewport");

        // Act: Insert text that exceeds viewport height
        tv.Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10";

        // Verify content size is correctly updated
        Assert.Equal (10, tv.GetContentSize ().Height); // This passes - content size IS updated

        // BUG: Scrollbar visibility is NOT updated because UpdateHorizontalScrollBarVisibility()
        // is not called when Text property changes (only called from EndInit, ScrollBars setter,
        // and OnViewportChanged)
        Assert.True (tv.VerticalScrollBar.Visible, "Scrollbar should be visible when content exceeds viewport");
    }

    /// <summary>
    /// Tests that vertical scrollbar hides when content shrinks to fit viewport.
    /// BUG: Scrollbar visibility is not updated when Text property changes.
    /// </summary>
    [Fact]
    public void VerticalScrollBar_Hides_When_Content_Fits_Viewport ()
    {
        // Arrange: TextView with ScrollBars=true, content exceeds viewport (scrollbar visible)
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Verify content size is correct (10 lines)
        Assert.Equal (10, tv.GetContentSize ().Height); // Content size IS updated correctly

        // BUG: Scrollbar visibility is not updated even though content exceeds viewport
        // This fails because UpdateHorizontalScrollBarVisibility() is not called when Text is set
        Assert.True (tv.VerticalScrollBar.Visible, "Scrollbar should be visible initially when content exceeds viewport");

        // Act: Delete text until content fits
        tv.Text = "Line 1\nLine 2";

        // Assert: Scrollbar should now be hidden
        Assert.False (tv.VerticalScrollBar.Visible, "Scrollbar should hide when content fits in viewport");
    }

    /// <summary>
    /// Tests that changing scrollbar position updates viewport after content change.
    /// This is the specific bug reported: "moving the scrollbar initially has no effect".
    /// BUG: Scrollbar visibility not updated, so scrollbar remains hidden and non-functional.
    /// </summary>
    [Fact]
    public void ScrollBar_Position_Change_Updates_Viewport_After_Content_Change ()
    {
        // Arrange: TextView with ScrollBars=true
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Initial"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Act: Insert text exceeding viewport
        tv.Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10";

        // Verify content size is correctly updated
        Assert.Equal (10, tv.GetContentSize ().Height); // This passes - content size IS updated

        // BUG: Scrollbar ScrollableContentSize IS correctly updated (via ContentSizeChanged event)
        // but scrollbar Visible is NOT updated (UpdateHorizontalScrollBarVisibility not called)
        Assert.Equal (10, tv.VerticalScrollBar.ScrollableContentSize); // This should pass

        // BUG: This fails - scrollbar visibility not updated when Text changes
        Assert.True (tv.VerticalScrollBar.Visible, "Scrollbar should be visible after content change");

        // Act: Change scrollbar position
        tv.VerticalScrollBar.Value = 5;

        // Assert: Viewport.Y should change to match scrollbar position
        Assert.Equal (5, tv.Viewport.Y);
    }

    /// <summary>
    /// Tests that viewport change updates scrollbar position.
    /// </summary>
    [Fact]
    public void Viewport_Change_Updates_ScrollBar_Position ()
    {
        // Arrange: TextView with content exceeding viewport
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Act: Programmatically set Viewport.Y
        tv.Viewport = tv.Viewport with { Y = 3 };

        // Assert: ScrollBar position should match Viewport.Y
        Assert.Equal (3, tv.VerticalScrollBar.Value);
    }

    /// <summary>
    /// Tests that setting ReadOnly to true does not change viewport position but does set NeedsDraw.
    /// </summary>>
    [Fact]
    public void ReadOnly_Set_True_Keeps_ViewportX_And_Sets_NeedDraw ()
    {
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            WordWrap = false,
            Text = "Short line"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();
        tv.ClearNeedsDraw ();

        Rectangle initialViewport = tv.Viewport;

        tv.ReadOnly = true;

        Assert.Equal (initialViewport.X, tv.Viewport.X);
        Assert.Equal (initialViewport.Y, tv.Viewport.Y);
        Assert.True (tv.NeedsDraw);
    }

    /// <summary>
    /// Tests that horizontal scrollbar becomes visible when line length exceeds width (WordWrap=false).
    /// BUG: Same as vertical - visibility not updated when Text changes.
    /// </summary>
    [Fact]
    public void HorizontalScrollBar_Becomes_Visible_When_Line_Exceeds_Width ()
    {
        // Arrange: TextView with ScrollBars=true, WordWrap=false
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            WordWrap = false,
            Text = "Short"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Verify: Horizontal scrollbar should not be visible initially
        Assert.False (tv.HorizontalScrollBar.Visible, "Horizontal scrollbar should not be visible when line fits");

        // Act: Insert very long line
        tv.Text = "This is a very long line that definitely exceeds the 20 character width of the viewport";

        // Content width IS correctly updated (via UpdateContentSize -> SetContentSize -> ContentSizeChanged)
        int contentWidth = tv.GetContentSize ().Width;
        Assert.True (contentWidth > 20, $"Content width should exceed viewport width (20), but was {contentWidth}");

        // BUG: Horizontal scrollbar visibility is NOT updated because UpdateHorizontalScrollBarVisibility()
        // is not called when Text property changes
        Assert.True (tv.HorizontalScrollBar.Visible, "Horizontal scrollbar should be visible when line exceeds width");
    }

    /// <summary>
    /// Tests that ScrollBar.VisibleContentSize is properly set after layout.
    /// If VisibleContentSize is 0, scrollbar interaction is silently ignored (ScrollBar.cs:433-436).
    /// </summary>
    [Fact]
    public void ScrollBar_VisibleContentSize_Is_Set_After_Layout ()
    {
        // Arrange: TextView with content exceeding viewport
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Verify scrollbar is visible
        Assert.True (tv.VerticalScrollBar.Visible, "Scrollbar should be visible");

        // BUG CHECK: If VisibleContentSize is 0, scrollbar interaction will be silently ignored
        // because ScrollBar.SliderOnPositionChanged and SliderOnScroll return early when VisibleContentSize == 0
        Assert.True (tv.VerticalScrollBar.VisibleContentSize > 0,
            $"VisibleContentSize should be > 0, but was {tv.VerticalScrollBar.VisibleContentSize}. " +
            "If 0, scrollbar interaction is silently ignored (see ScrollBar.cs:433-436)");

        // Also verify ScrollableContentSize is correct
        Assert.Equal (10, tv.VerticalScrollBar.ScrollableContentSize);
    }

    /// <summary>
    /// Tests that scrollbar position change actually updates viewport.
    /// This is the key integration test - if this fails, scrollbar dragging has no effect.
    /// </summary>
    [Fact]
    public void ScrollBar_Position_Change_Actually_Updates_Viewport ()
    {
        // Arrange: TextView with content exceeding viewport
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Pre-conditions: scrollbar must be properly configured
        Assert.True (tv.VerticalScrollBar.Visible, "Scrollbar must be visible");
        Assert.True (tv.VerticalScrollBar.VisibleContentSize > 0, "VisibleContentSize must be > 0");
        Assert.Equal (10, tv.VerticalScrollBar.ScrollableContentSize);

        // Record initial viewport position
        int initialViewportY = tv.Viewport.Y;

        // Act: Change scrollbar position (simulating user dragging)
        tv.VerticalScrollBar.Value = 5;

        // Assert: Viewport should have changed
        Assert.Equal (5, tv.Viewport.Y);
        Assert.NotEqual (initialViewportY, tv.Viewport.Y);
    }

    /// <summary>
    /// Tests that scrollbar position is not reset by AdjustViewport when cursor is at position 0.
    /// This simulates the scenario where user scrolls but cursor is at the start of content.
    /// </summary>
    [Fact]
    public void ScrollBar_Position_Not_Reset_By_AdjustViewport_When_Cursor_At_Start ()
    {
        // Arrange: TextView with content exceeding viewport, cursor at start
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Cursor should be at start (0, 0)
        Assert.Equal (0, tv.CurrentRow);
        Assert.Equal (0, tv.CurrentColumn);

        // Act: Scroll down via scrollbar
        tv.VerticalScrollBar.Value = 5;

        // Verify viewport changed
        Assert.Equal (5, tv.Viewport.Y);

        // Now simulate something that might call AdjustViewport (like a redraw event)
        // By setting NeedsDraw and calling LayoutSubViews
        tv.SetNeedsDraw ();
        tv.LayoutSubViews ();

        // BUG CHECK: Does AdjustViewport reset the viewport to show the cursor?
        // If cursor is at (0,0) and AdjustViewport is called, it might force Viewport.Y back to 0
        // This would explain why "scrolling has no effect initially"
        Assert.True (tv.Viewport.Y == 5, "Viewport.Y should remain at 5 after LayoutSubViews, not reset to cursor position");
    }

    /// <summary>
    /// Tests that scrollbar position is maintained after focus changes.
    /// Viewport should NOT scroll to cursor when gaining focus - only when cursor moves.
    /// </summary>
    [Fact]
    public void ScrollBar_Position_Maintained_After_Focus ()
    {
        // Arrange: TextView with content exceeding viewport
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Scroll down
        tv.VerticalScrollBar.Value = 5;
        Assert.Equal (5, tv.Viewport.Y);

        // Simulate focus change (this might trigger PositionCursor or other methods)
        tv.SetFocus ();

        // Viewport should remain scrolled - focus alone should not change scroll position
        Assert.Equal (5, tv.Viewport.Y);
    }

    /// <summary>
    /// Tests that toggling WordWrap updates content size appropriately.
    /// When WordWrap is enabled, long lines wrap and increase the content height.
    /// </summary>
    [Fact]
    public void WordWrap_Toggle_Updates_ContentSize ()
    {
        // Arrange: TextView with long lines that would wrap
        string longLine = new ('X', 50); // 50 chars, viewport is 20 wide = ~3 wrapped lines
        TextView tv = new ()
        {
            Width = 20,
            Height = 10,
            ScrollBars = true,
            WordWrap = false,
            Text = longLine
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Record initial content size (1 line, unwrapped)
        int unwrappedHeight = tv.GetContentSize ().Height;
        Assert.Equal (1, unwrappedHeight);

        // Act: Toggle WordWrap on
        tv.WordWrap = true;
        tv.LayoutSubViews (); // May need layout to apply wrapping

        // Assert: Content height should increase (more lines from wrapping)
        int wrappedHeight = tv.GetContentSize ().Height;
        Assert.True (wrappedHeight > unwrappedHeight,
            $"Wrapped content height ({wrappedHeight}) should be greater than unwrapped ({unwrappedHeight})");

        // With 50 chars in 20-width viewport, expect at least 3 lines
        Assert.True (wrappedHeight >= 3,
            $"Expected at least 3 wrapped lines for 50 chars in 20-width viewport, got {wrappedHeight}");
    }

    [Fact]
    public void WorldWrap_Typing_At_Middle_Of_Viewport_Y_Does_Not_Cause_Scroll_Up ()
    {
        // Arrange: TextView with content exceeding viewport height, WordWrap on
        TextView tv = new ()
        {
            Width = 20,
            Height = 5,
            ScrollBars = true,
            WordWrap = true,
            Text = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5\nLine 6\nLine 7\nLine 8\nLine 9\nLine 10"
        };
        tv.BeginInit ();
        tv.EndInit ();
        tv.LayoutSubViews ();

        // Scroll down to middle of content
        tv.VerticalScrollBar.Value = 5;

        // Move cursor to middle of viewport (simulate user clicking or navigating there)
        tv.InsertionPoint = new Point (6, 7); // Column 6, Row 7 (middle of viewport)

        Assert.Equal (5, tv.Viewport.Y);

        // Act: Simulate typing at current cursor position (which is at the end of the line)
        tv.NewKeyDownEvent (Key.A);

        // Assert: Viewport should not scroll up - it should remain at the same Y position
        Assert.Equal (5, tv.Viewport.Y);
        Assert.Equal (new Point (7, 7), tv.InsertionPoint); // Cursor should move right by 1
    }
}
