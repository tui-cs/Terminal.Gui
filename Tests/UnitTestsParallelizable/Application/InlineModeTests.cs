// Copilot

namespace ApplicationTests;

/// <summary>
///     Comprehensive tests for inline mode behavior: App.Screen positioning, View.Frame sizing,
///     overflow scrolling, dynamic growth, resize handling, and output buffer state.
/// </summary>
[Collection ("Application Tests")]
public class InlineModeTests
{
    /// <summary>
    ///     Helper: initializes an inline-mode app at a given cursor row. Sets both
    ///     <c>ForceInlinePosition</c> and <c>Driver.InlinePosition</c>
    ///     so the value is available for <c>LayoutAndDraw</c> without running the main loop.
    /// </summary>
    private static IApplication CreateInlineApp (int cursorRow, int termWidth = 80, int termHeight = 25)
    {
        IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.ForceInlinePosition = new Point (0, cursorRow);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (termWidth, termHeight);
        app.Driver!.InlinePosition = new Point (0, cursorRow);

        return app;
    }

    /// <summary>
    ///     Helper: calls <see cref="IApplication.Begin"/> and then explicitly triggers
    ///     <see cref="IApplication.LayoutAndDraw"/> because in test mode the ANSI startup
    ///     gate defers the initial draw that <c>Begin</c> would normally perform.
    /// </summary>
    private static SessionToken? BeginAndLayout (IApplication app, IRunnable view)
    {
        SessionToken? token = app.Begin (view);
        app.LayoutAndDraw ();

        return token;
    }

    #region Initial App.Screen Positioning

    /// <summary>
    ///     Verifies that in inline mode, <c>App.Screen.Y</c> is set from
    ///     <see cref="IApplication.ForceInlinePosition"/> when the first LayoutAndDraw runs.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ScreenY_MatchesCursorRow ()
    {
        using IApplication app = CreateInlineApp (5);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        // Act
        SessionToken? token = BeginAndLayout (app, view);

        // Assert
        Assert.Equal (5, app.Screen.Y);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     When the cursor is at row 0, <c>App.Screen.Y</c> should be 0.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_CursorAtRowZero_ScreenY_IsZero ()
    {
        using IApplication app = CreateInlineApp (0);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        Assert.Equal (0, app.Screen.Y);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     <c>App.Screen.Width</c> should match the full terminal width.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ScreenWidth_MatchesTerminalWidth ()
    {
        using IApplication app = CreateInlineApp (0, 132, 40);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        Assert.Equal (132, app.Screen.Width);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     <c>App.Screen.X</c> should always be 0 in inline mode — the inline region
    ///     always spans the full terminal width starting at column 0.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ScreenX_IsAlwaysZero ()
    {
        using IApplication app = CreateInlineApp (10);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        Assert.Equal (0, app.Screen.X);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region Screen Height Sizing

    /// <summary>
    ///     <c>App.Screen.Height</c> should match the view's <c>Frame.Height</c> when the view
    ///     uses <see cref="DimAutoStyle.Content"/>-based sizing.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ScreenHeight_MatchesViewFrameHeight ()
    {
        using IApplication app = CreateInlineApp (0);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 7) };

        SessionToken? token = BeginAndLayout (app, view);

        // Screen.Height should be the view's actual frame height, not the terminal height
        Assert.True (app.Screen.Height < 25, "Screen.Height should be less than terminal height");
        Assert.True (app.Screen.Height >= 7, "Screen.Height should be >= minimumContentDim");
        Assert.Equal (view.Frame.Height, app.Screen.Height);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     A view with <c>Dim.Fill()</c> should have its <c>App.Screen.Height</c> capped
    ///     at the available terminal height below the cursor row.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_DimFill_HeightCappedAtAvailableSpace ()
    {
        using IApplication app = CreateInlineApp (10);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        SessionToken? token = BeginAndLayout (app, view);

        // With cursor at row 10, terminal height 25: available = 25 - 10 = 15
        // But Dim.Fill will want the full terminal (25), so overflow scrolling kicks in.
        // The height should be capped at terminal height (25) at most.
        Assert.True (app.Screen.Height <= 25, $"Screen.Height ({app.Screen.Height}) should be <= terminal height");
        Assert.True (app.Screen.Height > 0, "Screen.Height should be > 0");

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region Overflow / Scrolling

    /// <summary>
    ///     When the view's desired height exceeds the space below the cursor, <c>App.Screen.Y</c>
    ///     should be adjusted upward (the terminal scrolls to make room).
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ViewOverflows_ScreenY_AdjustedUp ()
    {
        using IApplication app = CreateInlineApp (20); // Near bottom of 25-row terminal

        // View wants 10 rows, but only 5 available below cursor (25 - 20 = 5)
        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 10) };

        SessionToken? token = BeginAndLayout (app, view);

        // Should have scrolled: Y = 20 - overflow, where overflow = 20 + viewHeight - 25
        Assert.True (app.Screen.Y < 20, $"Screen.Y ({app.Screen.Y}) should be < original cursor row (20)");
        Assert.True (app.Screen.Height >= 10, $"Screen.Height ({app.Screen.Height}) should be >= 10");

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     When the view is taller than the entire terminal, <c>App.Screen.Height</c>
    ///     should be capped at the terminal height and <c>App.Screen.Y</c> should be 0.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ViewTallerThanTerminal_CappedAtTerminalHeight ()
    {
        using IApplication app = CreateInlineApp (10);

        // View wants 50 rows — more than the 25-row terminal
        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 50) };

        SessionToken? token = BeginAndLayout (app, view);

        Assert.Equal (0, app.Screen.Y);
        Assert.Equal (25, app.Screen.Height);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     When the cursor is at the last terminal row and the view height is 1 (fits exactly),
    ///     <c>App.Screen.Y</c> equals the cursor row — no extra scroll should push it up.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_CursorAtBottom_SingleRowView_NoExtraScroll () // Copilot
    {
        // Terminal is 25 rows (0-24). Cursor at row 24. View needs 1 row — fits exactly.
        using IApplication app = CreateInlineApp (24);

        Window view = new () { Width = Dim.Fill (), Height = 1 };

        SessionToken? token = BeginAndLayout (app, view);

        // The view fits at cursorRow without scrolling. App.Screen.Y should be 24, not 23.
        Assert.Equal (24, app.Screen.Y);
        Assert.Equal (1, app.Screen.Height);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     When the view fits entirely below the cursor, no scrolling occurs and
    ///     <c>App.Screen.Y</c> equals <c>ForceInlinePosition.Y</c>.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ViewFits_NoScrolling ()
    {
        using IApplication app = CreateInlineApp (5);

        // View wants 3 rows, and 20 rows are available below cursor (25 - 5 = 20)
        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        Assert.Equal (5, app.Screen.Y);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region Dynamic Growth

    /// <summary>
    ///     When a view grows (e.g., items added to a ListView), <c>App.Screen.Height</c> should
    ///     increase to match the new view height.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_ViewGrows_ScreenHeightIncreases ()
    {
        using IApplication app = CreateInlineApp (0);

        // Start with a small view
        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        int initialHeight = app.Screen.Height;

        // Add content to make the view taller
        Label label1 = new () { Y = 5, Text = "line 1" };
        Label label2 = new () { Y = 6, Text = "line 2" };
        Label label3 = new () { Y = 7, Text = "line 3" };
        Label label4 = new () { Y = 8, Text = "line 4" };
        view.Add (label1, label2, label3, label4);

        // Trigger layout/draw again
        app.LayoutAndDraw ();

        Assert.True (app.Screen.Height > initialHeight, $"Screen.Height ({app.Screen.Height}) should have grown from initial ({initialHeight})");

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     Dynamic growth should not exceed the terminal height.
    /// </summary>
    [Fact]
    public void LayoutAndDraw_Inline_DynamicGrowth_CappedAtTerminalHeight ()
    {
        using IApplication app = CreateInlineApp (0, termHeight: 15);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        // Add enough content to exceed the terminal
        for (var i = 0; i < 30; i++)
        {
            view.Add (new Label { Y = i + 3, Text = $"line {i}" });
        }

        app.LayoutAndDraw ();

        Assert.True (app.Screen.Height <= 15, $"Screen.Height ({app.Screen.Height}) should not exceed terminal height (15)");

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region Resize Behavior

    /// <summary>
    ///     Headless inline resize preserves the known origin.
    /// </summary>
    [Fact]
    public void Resize_Inline_ScreenY_PreservesOrigin ()
    {
        using IApplication app = CreateInlineApp (15, 120);

        app.Screen = new Rectangle (0, 15, 120, 5);
        Assert.Equal (15, app.Screen.Y);

        // Act
        app.Driver!.SetScreenSize (80, 25);

        // Assert
        Assert.Equal (15, app.Screen.Y);
    }

    /// <summary>
    ///     Terminal resize in inline mode updates <c>App.Screen.Width</c> to the new terminal width.
    /// </summary>
    [Fact]
    public void Resize_Inline_ScreenWidth_UpdatesToNewWidth ()
    {
        using IApplication app = CreateInlineApp (0, 120);

        app.Screen = new Rectangle (0, 0, 120, 5);
        Assert.Equal (120, app.Screen.Width);

        app.Driver!.SetScreenSize (80, 25);

        Assert.Equal (80, app.Screen.Width);
    }

    /// <summary>
    ///     Headless resize preserves <c>InlinePosition.Y</c> while the region still fits.
    /// </summary>
    [Fact]
    public void Resize_Inline_InlinePosition_PreservesOrigin ()
    {
        using IApplication app = CreateInlineApp (20, 100);
        app.Screen = new Rectangle (0, 20, 100, 3);

        app.Driver!.SetScreenSize (80, 25);

        Assert.Equal (20, app.Driver!.InlinePosition.Y);
    }

    /// <summary>
    ///     Inline resize does not request a display clear.
    /// </summary>
    [Fact]
    public void Resize_Inline_DoesNotSetClearScreenNextIteration ()
    {
        using IApplication app = CreateInlineApp (10, 100);

        app.Screen = new Rectangle (0, 10, 100, 5);

        app.Driver!.SetScreenSize (80, 25);

        Assert.False (app.ClearScreenNextIteration);
    }

    /// <summary>
    ///     After resize, the next layout uses the new width and retained origin.
    /// </summary>
    [Fact]
    public void Resize_Inline_NextLayoutAndDraw_RetainsOrigin ()
    {
        using IApplication app = CreateInlineApp (10, 120, 40);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 5) };

        SessionToken? token = BeginAndLayout (app, view);

        // Capture initial state
        Assert.Equal (10, app.Screen.Y);

        // Resize updates the available width.
        app.Driver!.SetScreenSize (80, 25);

        // The next layout retains the existing origin.
        app.LayoutAndDraw ();

        Assert.Equal (10, app.Screen.Y);
        Assert.Equal (80, app.Screen.Width);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region Driver.Screen vs App.Screen Independence

    /// <summary>
    ///     In inline mode, <c>Driver.Screen</c> always reflects the full terminal dimensions,
    ///     while <c>App.Screen</c> is the inline sub-rectangle.
    /// </summary>
    [Fact]
    public void Inline_DriverScreen_IsFullTerminal_AppScreen_IsSubRectangle ()
    {
        using IApplication app = CreateInlineApp (10);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 5) };

        SessionToken? token = BeginAndLayout (app, view);

        // Driver.Screen = full terminal
        Assert.Equal (80, app.Driver!.Screen.Width);
        Assert.Equal (25, app.Driver!.Screen.Height);
        Assert.Equal (0, app.Driver!.Screen.Y);

        // App.Screen = inline sub-rectangle
        Assert.Equal (80, app.Screen.Width);
        Assert.True (app.Screen.Height < 25, "App.Screen.Height should be smaller than terminal");
        Assert.True (app.Screen.Y >= 0, "App.Screen.Y should be >= 0");

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    /// <summary>
    ///     In fullscreen mode, <c>App.Screen</c> equals <c>Driver.Screen</c>.
    /// </summary>
    [Fact]
    public void FullScreen_AppScreen_Equals_DriverScreen ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.FullScreen;
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Assert.Equal (app.Driver!.Screen, app.Screen);
    }

    #endregion

    #region View.Frame Positioning

    /// <summary>
    ///     In inline mode, the top view's <c>Frame</c> is relative to <c>App.Screen</c>,
    ///     so <c>Frame.Y</c> should be 0 (not the terminal row offset).
    /// </summary>
    [Fact]
    public void Inline_ViewFrame_IsRelativeToAppScreen ()
    {
        using IApplication app = CreateInlineApp (15);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 5) };

        SessionToken? token = BeginAndLayout (app, view);

        // The view's Frame is in App.Screen coordinates, not terminal coordinates
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (0, view.Frame.X);
        Assert.Equal (app.Screen.Width, view.Frame.Width);

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region ForceInlinePosition

    /// <summary>
    ///     <see cref="IApplication.ForceInlinePosition"/> bypasses CPR and sets the inline
    ///     cursor position directly for testing purposes.
    /// </summary>
    [Theory]
    [InlineData (0)]
    [InlineData (5)]
    [InlineData (20)]
    public void ForceInlinePosition_SetsInitialPosition (int cursorRow)
    {
        using IApplication app = CreateInlineApp (cursorRow);

        Window view = new () { Width = Dim.Fill (), Height = Dim.Auto (minimumContentDim: 3) };

        SessionToken? token = BeginAndLayout (app, view);

        // App.Screen.Y should match the forced cursor row (possibly adjusted for overflow)

        Assert.True (app.Screen.Y <= cursorRow, $"Screen.Y ({app.Screen.Y}) should be <= ForceInlinePosition.Y ({cursorRow})");

        if (token is { })
        {
            app.End (token);
        }

        view.Dispose ();
    }

    #endregion

    #region Mouse Coordinate Adjustment

    /// <summary>
    ///     Mouse events in inline mode have <c>ScreenPosition</c> adjusted by subtracting
    ///     <c>App.Screen.Y</c> so views receive coordinates relative to the inline region.
    /// </summary>
    [Fact]
    public void Inline_MouseEvent_ScreenPosition_AdjustedByScreenY ()
    {
        using IApplication app = CreateInlineApp (10);

        app.Screen = new Rectangle (0, 10, 80, 5);

        Point? receivedPosition = null;

        app.Mouse.MouseEvent += (_, e) => { receivedPosition = e.ScreenPosition; };

        // Inject at terminal row 12 (should be adjusted to row 2 within inline region)
        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 12), Flags = MouseFlags.LeftButtonPressed });

        Assert.NotNull (receivedPosition);
        Assert.Equal (new Point (5, 2), receivedPosition.Value);
    }

    /// <summary>
    ///     In fullscreen mode (Screen.Y == 0), mouse events are NOT adjusted.
    /// </summary>
    [Fact]
    public void FullScreen_MouseEvent_ScreenPosition_NotAdjusted ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.FullScreen;
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Point? receivedPosition = null;

        app.Mouse.MouseEvent += (_, e) => { receivedPosition = e.ScreenPosition; };

        app.InjectMouse (new Mouse { ScreenPosition = new Point (5, 12), Flags = MouseFlags.LeftButtonPressed });

        Assert.NotNull (receivedPosition);
        Assert.Equal (new Point (5, 12), receivedPosition.Value);
    }

    #endregion

    #region Output Buffer Inline Mode

    /// <summary>
    ///     In inline mode, <c>ClearContents()</c> leaves cells NOT dirty so only
    ///     explicitly-drawn cells are flushed to the terminal.
    /// </summary>
    [Fact]
    public void Inline_ClearContents_CellsStartClean ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver!.ClearContents ();

        // All DirtyLines should be false
        for (var row = 0; row < app.Driver!.Rows; row++)
        {
            Assert.False (app.Driver!.Contents! [row, 0].IsDirty, $"Cell at row {row} should not be dirty after ClearContents in inline mode");
        }
    }

    /// <summary>
    ///     In fullscreen mode, <c>ClearContents()</c> marks cells dirty so the entire
    ///     screen is redrawn.
    /// </summary>
    [Fact]
    public void FullScreen_ClearContents_CellsStartDirty ()
    {
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.FullScreen;
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver!.ClearContents ();

        // All cells should be dirty
        for (var row = 0; row < app.Driver!.Rows; row++)
        {
            Assert.True (app.Driver!.Contents! [row, 0].IsDirty, $"Cell at row {row} should be dirty after ClearContents in fullscreen mode");
        }
    }

    #endregion
}
