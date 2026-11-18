using UnitTests;

namespace ViewsTests;

// Claude - Opus 4.6

/// <summary>
///     Scrolling tests for the <see cref="Tabs"/> class, focused on <see cref="Side.Top"/>.
///     Based on the scrolling drawings in plans/tabview-border-based-design.md.
/// </summary>
public class TabsScrollingTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Step 0: All 5 tabs fit in 26 columns. No scroll needed. Tab1 selected.
    /// </summary>
    [Fact]
    public void ScrollOffset_AllTabsFit_NoScroll_Tab1Selected ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╮────╮────╮────╮────╮  ┊
                                              ┊│Tab1│Tab2│Tab3│Tab4│Tab5│  ┊
                                              ┊│    ╰────┴────┴────┴────┴─╮┊
                                              ┊│Tab1 content              │┊
                                              ┊│                          │┊
                                              ┊╰──────────────────────────╯┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Step 1: Width reduced to 18. Tab1 selected. Tab4 clipped, Tab5 off-screen.
    ///     Right scroll indicator appears.
    /// </summary>
    [Fact]
    public void ReducedWidth_Tab1Selected_RightIndicator ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 26, Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╮────╮────╮──          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊│    ╰────┴────┴─►          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_ReducedWidth_Tab1Selected_Scroll_Right_1 ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 26, Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╮────╮────╮──          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊│    ╰────┴────┴─►          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.ScrollOffset += 1;
        superView.Layout ();
        driver.ClearContents ();
        superView.Draw ();

        Assert.Equal (1, tabs.ScrollOffset);

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊────╮────╮────╮───          ┊
                                              ┊Tab1│Tab2│Tab3│Tab          ┊
                                              ┊◄   ╰────┴────┴──►          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_ReducedWidth_Tab1Selected_Scroll_Right_Past_End ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 26, Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╮────╮────╮──          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊│    ╰────┴────┴─►          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.ScrollOffset += 100;
        superView.Layout ();
        driver.ClearContents ();
        superView.Draw ();

        // Assert.Equal (0, tabs.ScrollOffset);

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊──╮────╮────╮────╮          ┊
                                              ┊b2│Tab3│Tab4│Tab5│          ┊
                                              ┊◄─┴────┴────┴────┤          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_ReducedWidth_Tab5Selected_Scroll_Left_Past_Start ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 26, Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;
        tabs.Value = tab5;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊──╭────╭────╭────╮          ┊
                                              ┊b2│Tab3│Tab4│Tab5│          ┊
                                              ┊◄─┴────┴────╯    │          ┊
                                              ┊│Tab5 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.ScrollOffset -= 100;
        superView.Layout ();
        driver.ClearContents ();
        superView.Draw ();

        // Assert.Equal (0, tabs.ScrollOffset);

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╭────╭────╭──          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊├────┴────┴────┴─►          ┊
                                              ┊│Tab5 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Step 2: Width 18, Tab2 selected. No scroll offset change, just focus change.
    /// </summary>
    [Fact]
    public void ScrollOffset_Width18_Tab2Selected_NoScrollChange ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab2;

        tabs.Width = 18;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╭────╮────╮──          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊├────╯    ╰────┴─►          ┊
                                              ┊│Tab2 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Step 4: Select Tab4 to scroll right, then select Tab2. EnsureTabVisible scrolls
    ///     the minimum needed. Tab1 is partially clipped, Tab2 is fully visible.
    /// </summary>
    [Fact]
    public void ScrollOffset_SelectTab4ThenTab2_MinimalScroll ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;

        superView.Layout ();

        // Select Tab4 — EnsureTabVisible scrolls just enough to show Tab4
        tabs.Value = tab4;
        superView.Layout ();

        // Select Tab2 — already visible, so no additional scrolling
        tabs.Value = tab2;
        superView.Layout ();
        superView.Draw ();

        // _scrollOffset = 3 (minimum to show Tab4): Tab1 partially clipped, Tab5 partially visible
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊──╭────╮────╮────╮          ┊
                                              ┊b1│Tab2│Tab3│Tab4│          ┊
                                              ┊◄─╯    ╰────┴────►          ┊
                                              ┊│Tab2 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Step 6: Select Tab4 with width 18. Tab1 partially clipped, Tab5 partially visible.
    /// </summary>
    [Fact]
    public void ScrollOffset_Tab4Selected_ScrolledRight ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;

        // Select Tab4 — EnsureTabVisible should scroll right so Tab4 is visible
        tabs.Value = tab4;
        superView.Layout ();
        superView.Draw ();

        // _scrollOffset = 3 (minimum to show Tab4): Tab1 clipped by 3 chars, Tab5 at edge
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊──╭────╭────╭────╮          ┊
                                              ┊b1│Tab2│Tab3│Tab4│          ┊
                                              ┊◄─┴────┴────╯    ►          ┊
                                              ┊│Tab4 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Step 9: Select Tab1 after being scrolled. EnsureTabVisible scrolls back to offset 0.
    /// </summary>
    [Fact]
    public void ScrollOffset_SelectTab1_ScrollsBackToStart ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);

        tabs.Width = 18;

        // Scroll right by selecting Tab4
        tabs.Value = tab4;
        superView.Layout ();

        // Select Tab1 — should scroll back to start
        tabs.Value = tab1;
        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╮────╮────╮──          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊│    ╰────┴────┴─►          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊╰────────────────╯          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Step 11: Width back to 26. All tabs fit. No scroll indicators. Tab4 selected.
    /// </summary>
    [Fact]
    public void FullWidth_AllFit_NoScrollIndicators ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab4;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────╭────╭────╭────╮────╮  ┊
                                              ┊│Tab1│Tab2│Tab3│Tab4│Tab5│  ┊
                                              ┊├────┴────┴────╯    ╰────┴─╮┊
                                              ┊│Tab4 content              │┊
                                              ┊│                          │┊
                                              ┊╰──────────────────────────╯┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Verifies scrolling back to first tab resets offsets.
    /// </summary>
    [Fact]
    public void TabOffsets_AfterScrollBack_ResetToNatural ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        superView.Layout ();

        // Scroll right
        tabs.Value = tab5;
        superView.Layout ();

        // Scroll back
        tabs.Value = tab1;
        superView.Layout ();

        // Offsets should be back to natural (scrollOffset = 0)
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);
        Assert.Equal (15, ((BorderView)tab4.Border.View!).TabOffset);
        Assert.Equal (20, ((BorderView)tab5.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    /// <summary>
    ///     Verifies TabOffset values are correct after scrolling right.
    /// </summary>
    [Fact]
    public void TabOffsets_AfterScrollRight_AreNegativeForOffScreenTabs ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 18, Height = Dim.Fill () };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        superView.Layout ();

        // All tabs at natural offsets (scroll offset = 0)
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);

        // Select tab5 — should scroll right since tab5 is off-screen at width 18
        tabs.Value = tab5;
        superView.Layout ();

        // Tab1 should now have negative offset (scrolled off-screen)
        Assert.True (((BorderView)tab1.Border.View!).TabOffset < 0, "Tab1 should have negative offset after scrolling right");
        Assert.True (((BorderView)tab5.Border.View!).TabOffset >= 0, "Tab5 should have non-negative offset (visible)");

        tabs.Dispose ();
    }

    /// <summary>
    ///     Creates 5 tabs with titles "Tab1" through "Tab5". Each has TabLength = 6 (4-char title + 2 border cells).
    ///     Total header span = 26 (5 × 6 − 4 shared edges).
    /// </summary>
    private static (View tab1, View tab2, View tab3, View tab4, View tab5) CreateFiveTabs ()
    {
        View tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        View tab2 = new () { Title = "Tab2", Text = "Tab2 content" };
        View tab3 = new () { Title = "Tab3", Text = "Tab3 content" };
        View tab4 = new () { Title = "Tab4", Text = "Tab4 content" };
        View tab5 = new () { Title = "Tab5", Text = "Tab5 content" };

        return (tab1, tab2, tab3, tab4, tab5);
    }

    #region Scrolling Tests (Side.Top)

    [Fact]
    public void Top_TabsFit_NoScrollOffset ()
    {
        // Three tabs that fit within 20 columns — no scrolling should occur
        IDriver driver = CreateTestDriver (20, 5);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // All offsets should be normal (no scroll applied)
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_TabsOverflow_SelectingLastTab_ScrollsRight ()
    {
        // Narrow Tabs (10 wide) with 3 tabs (each ~6 wide = 15 total span).
        // Selecting the last tab should scroll so it's visible.
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Select tab3 — should trigger EnsureTabVisible which scrolls right
        tabs.Value = tab3;
        tabs.Layout ();

        // Tab3's absolute offset is 10, length is 6, so tabEnd = 16.
        // Viewport width is 10, so _scrollOffset = 16 - 10 = 6.
        // tab1.TabOffset = 0 - 6 = -6
        // tab2.TabOffset = 5 - 6 = -1
        // tab3.TabOffset = 10 - 6 = 4
        Assert.True (((BorderView)tab1.Border.View!).TabOffset < 0, "Tab1 should have scrolled off-screen (negative offset)");
        Assert.True (((BorderView)tab3.Border.View!).TabOffset >= 0, "Tab3 should be visible (non-negative offset)");

        tabs.Dispose ();
    }

    [Fact]
    public void Top_ScrolledRight_SelectingFirstTab_ScrollsBackLeft ()
    {
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Scroll right by selecting tab3
        tabs.Value = tab3;
        tabs.Layout ();

        // Now scroll back by selecting tab1
        tabs.Value = tab1;
        tabs.Layout ();

        // Tab1 should be at offset 0 (scrolled back to start)
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_ScrolledRight_MiddleTabOffset_IsCorrect ()
    {
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────╮────
                                              │Tab1│Tab2
                                              │    ╰───►
                                              │        │
                                              ╰────────╯
                                              """,
                                              output,
                                              driver);

        // Select tab3 to scroll right
        tabs.Value = tab3;
        tabs.Layout ();
        driver.ClearContents ();
        tabs.Draw ();
        ////                                    0123456789
        //DriverAssert.AssertDriverContentsAre ("""
        //                                      ◄        ►
        //                                      ────╭────╮
        //                                      Tab2│Tab3│
        //                                      ┬───╯    │
        //                                      ╰────────╯
        //                                      """,
        //                                      output,
        //                                      driver);

        // Verify exact offsets: _scrollOffset = tabEnd(tab3) - viewportWidth = 16 - 10 = 6
        Assert.Equal (-6, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (-1, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (4, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_ManyTabs_SelectingMiddleTab_ScrollsMinimally ()
    {
        IDriver driver = CreateTestDriver (14, 5);
        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        View tab1 = new () { Title = "A" };
        View tab2 = new () { Title = "B" };
        View tab3 = new () { Title = "C" };
        View tab4 = new () { Title = "D" };
        View tab5 = new () { Title = "E" };

        // Each tab title is 1 char → TabLength = 3 (1 + 2 borders)
        // Cumulative offsets: 0, 2, 4, 6, 8; last tabEnd = 8 + 3 = 11
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Layout ();

        // All 5 tabs fit in 14 columns (total span = 11), so no scrolling
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (2, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (4, ((BorderView)tab3.Border.View!).TabOffset);
        Assert.Equal (6, ((BorderView)tab4.Border.View!).TabOffset);
        Assert.Equal (8, ((BorderView)tab5.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_InsertTab_UpdatesScrollBarSizing ()
    {
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        tabs.Add (tab1);
        tabs.Layout ();

        // Single tab fits — offset is 0
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);

        // Insert many tabs to cause overflow
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.InsertTab (1, tab2);
        tabs.InsertTab (2, tab3);
        tabs.Layout ();

        // Tab1 should still be at 0 (no scroll yet since tab1 is selected)
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);

        // Select tab3 to trigger scroll
        tabs.Value = tab3;
        tabs.Layout ();

        // Tab1 should now have a negative offset
        Assert.True (((BorderView)tab1.Border.View!).TabOffset < 0);

        tabs.Dispose ();
    }

    #endregion

    #region Scrolling Tests (Side.Bottom)

    [Fact]
    public void Bottom_TabsFit_NoScrollOffset ()
    {
        IDriver driver = CreateTestDriver (20, 5);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5, TabSide = Side.Bottom };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Bottom_TabsOverflow_SelectingLastTab_Scrolls ()
    {
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5, TabSide = Side.Bottom };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.Value = tab3;
        tabs.Layout ();

        Assert.True (((BorderView)tab1.Border.View!).TabOffset < 0, "Tab1 should have negative offset");
        Assert.True (((BorderView)tab3.Border.View!).TabOffset >= 0, "Tab3 should be visible");

        tabs.Dispose ();
    }

    [Fact]
    public void Bottom_ScrolledRight_SelectingFirstTab_ScrollsBack ()
    {
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5, TabSide = Side.Bottom };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.Value = tab3;
        tabs.Layout ();

        tabs.Value = tab1;
        tabs.Layout ();

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Bottom_AllFit_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill (), TabSide = Side.Bottom };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab1;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭──────────────────────────╮┊
                                              ┊│Tab1 content              │┊
                                              ┊│                          │┊
                                              ┊│    ╭────┬────┬────┬────┬─╯┊
                                              ┊│Tab1│Tab2│Tab3│Tab4│Tab5│  ┊
                                              ┊╰────╯────╯────╯────╯────╯  ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When tabs overflow the available width with <see cref="Side.Bottom"/>, a forward scroll indicator
    ///     (►) should appear on the separator line at the right edge. The separator line is the boundary
    ///     between content and tabs — for Bottom, it's the top line of the tab header area.
    /// </summary>
    [Fact]
    public void Bottom_TabsOverflow_ScrollIndicatorAppearsOnSeparator ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 18, Height = Dim.Fill (), TabSide = Side.Bottom };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab1;

        superView.Layout ();
        superView.Draw ();

        // The ► indicator should be on the separator line (the line between content and tabs).
        // For Side.Bottom, the separator is the top border of the tab headers.
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────────────────╮          ┊
                                              ┊│Tab1 content    │          ┊
                                              ┊│                │          ┊
                                              ┊│    ╭────┬────┬─►          ┊
                                              ┊│Tab1│Tab2│Tab3│Ta          ┊
                                              ┊╰────╯────╯────╯──          ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When scrolled right with <see cref="Side.Bottom"/>, a backward scroll indicator (◄) should
    ///     appear on the separator line at the left edge.
    /// </summary>
    [Fact]
    public void Bottom_ScrolledRight_BothScrollIndicatorsAppear ()
    {
        IDriver driver = CreateTestDriver (30, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = 18, Height = Dim.Fill (), TabSide = Side.Bottom };
        superView.Add (tabs);

        (View tab1, View tab2, View tab3, View tab4, View tab5) = CreateFiveTabs ();
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab3;
        tabs.ScrollOffset = 1;

        superView.Layout ();
        superView.Draw ();

        // Both ◄ and ► should appear on the separator line (between content and tab headers).
        // For Side.Bottom, the separator is the line with ╭────┬── connectors.
        // They must NOT appear on the content border (the ╭─────╮ line at top).
        var outputStr = driver.ToString ();
        string [] lines = outputStr.Split ('\n');

        // Find lines containing the indicators
        var backOnSeparator = false;
        var forwardOnSeparator = false;

        foreach (string line in lines)
        {
            // The separator line contains tab junction glyphs (┬, ╭ adjacent to tab text)
            bool isSeparatorLine = line.Contains ('┬') || (line.Contains ('╭') && line.Contains ('┤'));

            if (line.Contains ('◄'))
            {
                backOnSeparator = isSeparatorLine;
            }

            if (line.Contains ('►'))
            {
                forwardOnSeparator = isSeparatorLine;
            }
        }

        Assert.True (backOnSeparator, "◄ indicator should be on the separator line, not the content border");
        Assert.True (forwardOnSeparator, "► indicator should be on the separator line, not the content border");

        tabs.Dispose ();
    }

    #endregion

    #region Scrolling Tests (Side.Left)

    [Fact]
    public void Left_TabsFit_NoScrollOffset ()
    {
        IDriver driver = CreateTestDriver (20, 20);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 20, TabSide = Side.Left };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Left_TabsOverflow_SelectingLastTab_Scrolls ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Left };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.Value = tab3;
        tabs.Layout ();

        Assert.True (((BorderView)tab1.Border.View!).TabOffset < 0, "Tab1 should have negative offset");
        Assert.True (((BorderView)tab3.Border.View!).TabOffset >= 0, "Tab3 should be visible");

        tabs.Dispose ();
    }

    [Fact]
    public void Left_ScrolledDown_SelectingFirstTab_ScrollsBack ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Left };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.Value = tab3;
        tabs.Layout ();

        tabs.Value = tab1;
        tabs.Layout ();

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Left_AllFit_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (20, 20);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill (), TabSide = Side.Left };
        superView.Add (tabs);

        View tab1 = new () { Title = "T1", Text = "Content" };
        View tab2 = new () { Title = "T2", Text = "Content" };
        View tab3 = new () { Title = "T3", Text = "Content" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Value = tab1;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────────────────╮┊
                                              ┊│T Content       │┊
                                              ┊│1               │┊
                                              ┊╰─╮              │┊
                                              ┊│T│              │┊
                                              ┊│2│              │┊
                                              ┊╰─┤              │┊
                                              ┊│T│              │┊
                                              ┊│3│              │┊
                                              ┊╰─┤              │┊
                                              ┊  │              │┊
                                              ┊  │              │┊
                                              ┊  │              │┊
                                              ┊  │              │┊
                                              ┊  │              │┊
                                              ┊  │              │┊
                                              ┊  │              │┊
                                              ┊  ╰──────────────╯┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    #endregion

    #region Scrolling Tests (Side.Right)

    [Fact]
    public void Right_TabsFit_NoScrollOffset ()
    {
        IDriver driver = CreateTestDriver (20, 20);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 20, TabSide = Side.Right };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Right_TabsOverflow_SelectingLastTab_Scrolls ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Right };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.Value = tab3;
        tabs.Layout ();

        Assert.True (((BorderView)tab1.Border.View!).TabOffset < 0, "Tab1 should have negative offset");
        Assert.True (((BorderView)tab3.Border.View!).TabOffset >= 0, "Tab3 should be visible");

        tabs.Dispose ();
    }

    [Fact]
    public void Right_ScrolledDown_SelectingFirstTab_ScrollsBack ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Right };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.Value = tab3;
        tabs.Layout ();

        tabs.Value = tab1;
        tabs.Layout ();

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void Right_AllFit_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (20, 20);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill (), TabSide = Side.Right };
        superView.Add (tabs);

        View tab1 = new () { Title = "T1", Text = "Content" };
        View tab2 = new () { Title = "T2", Text = "Content" };
        View tab3 = new () { Title = "T3", Text = "Content" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Value = tab1;

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐
                                              ┊╭────────────────╮┊
                                              ┊│Content        T│┊
                                              ┊│               1│┊
                                              ┊│              ╭─╯┊
                                              ┊│              │T│┊
                                              ┊│              │2│┊
                                              ┊│              ├─╯┊
                                              ┊│              │T│┊
                                              ┊│              │3│┊
                                              ┊│              ├─╯┊
                                              ┊│              │  ┊
                                              ┊│              │  ┊
                                              ┊│              │  ┊
                                              ┊│              │  ┊
                                              ┊│              │  ┊
                                              ┊│              │  ┊
                                              ┊│              │  ┊
                                              ┊╰──────────────╯  ┊
                                              └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When tabs overflow the available height with <see cref="Side.Right"/>, a forward scroll indicator
    ///     (▼) should appear on the separator line at the bottom edge. The separator line is the vertical
    ///     boundary between content and tabs — for Right, it's the left line of the tab header area.
    /// </summary>
    [Fact]
    public void Right_TabsOverflow_ScrollIndicatorAppearsOnSeparator ()
    {
        IDriver driver = CreateTestDriver (20, 12);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = 10, TabSide = Side.Right };
        superView.Add (tabs);

        View tab1 = new () { Title = "T1", Text = "Content" };
        View tab2 = new () { Title = "T2", Text = "Content" };
        View tab3 = new () { Title = "T3", Text = "Content" };
        View tab4 = new () { Title = "T4", Text = "Content" };
        View tab5 = new () { Title = "T5", Text = "Content" };
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab1;

        superView.Layout ();
        superView.Draw ();

        // The ▼ indicator should be on the separator column (vertical boundary between content and tabs).
        // For Side.Right, the separator is the left border of the tab headers — column where ├ and ╭ appear.
        // It must NOT appear on the content border's left edge (column 0 of the border viewport).
        var outputStr = driver.ToString ();
        string [] lines = outputStr.Split ('\n');

        // Find the column where tab junctions (├ or ╭) appear — that's the separator column
        int separatorCol = -1;

        foreach (string line in lines)
        {
            int idx = line.IndexOf ('├');

            if (idx < 0)
            {
                idx = line.IndexOf ('╭');

                // Skip the content border's ╭ at the start — we want the one in the tab area
                if (idx >= 0 && idx < line.Length - 3 && line [idx + 1] == '─')
                {
                    idx = line.IndexOf ('╭', idx + 1);
                }
            }

            if (idx < 0)
            {
                continue;
            }
            separatorCol = idx;

            break;
        }

        Assert.True (separatorCol >= 0, "Could not find separator column (├ or ╭ glyph) in output");

        // Find the ▼ indicator and verify it's on the separator column
        int downArrowCol = -1;

        foreach (string line in lines)
        {
            int idx = line.IndexOf ('▼');

            if (idx < 0)
            {
                continue;
            }
            downArrowCol = idx;

            break;
        }

        Assert.True (downArrowCol >= 0, "▼ indicator not found in output");
        Assert.Equal (separatorCol, downArrowCol);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     When scrolled down with <see cref="Side.Right"/>, a backward scroll indicator (▲) should
    ///     appear on the separator column, and a forward indicator (▼) at the bottom.
    ///     Both must be on the separator column, not on the content border.
    /// </summary>
    [Fact]
    public void Right_ScrolledDown_BothScrollIndicatorsAppear ()
    {
        IDriver driver = CreateTestDriver (20, 12);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };
        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = 10, TabSide = Side.Right };
        superView.Add (tabs);

        View tab1 = new () { Title = "T1", Text = "Content" };
        View tab2 = new () { Title = "T2", Text = "Content" };
        View tab3 = new () { Title = "T3", Text = "Content" };
        View tab4 = new () { Title = "T4", Text = "Content" };
        View tab5 = new () { Title = "T5", Text = "Content" };
        tabs.Add (tab1, tab2, tab3, tab4, tab5);
        tabs.Value = tab3;
        tabs.ScrollOffset = 1;

        superView.Layout ();
        superView.Draw ();

        // Both ▲ and ▼ should appear on the separator column (where ├ junctions are).
        // They must NOT appear on column 0 (content border left edge).
        var outputStr = driver.ToString ();
        string [] lines = outputStr.Split ('\n');

        // Find separator column from junction glyphs
        int separatorCol = -1;

        foreach (string line in lines)
        {
            int idx = line.IndexOf ('├');

            if (idx < 0)
            {
                continue;
            }
            separatorCol = idx;

            break;
        }

        Assert.True (separatorCol >= 0, "Could not find separator column (├ glyph) in output");

        // Verify ▲ is on the separator column
        int upCol = -1;

        foreach (string line in lines)
        {
            int idx = line.IndexOf ('▲');

            if (idx < 0)
            {
                continue;
            }
            upCol = idx;

            break;
        }

        Assert.True (upCol >= 0, "▲ indicator not found in output");
        Assert.Equal (separatorCol, upCol);

        // Verify ▼ is on the separator column
        int downCol = -1;

        foreach (string line in lines)
        {
            int idx = line.IndexOf ('▼');

            if (idx < 0)
            {
                continue;
            }
            downCol = idx;

            break;
        }

        Assert.True (downCol >= 0, "▼ indicator not found in output");
        Assert.Equal (separatorCol, downCol);

        tabs.Dispose ();
    }

    #endregion

    #region TabSpacing Tests

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies the default TabSpacing is -1 and produces the current overlap behavior:
    ///     adjacent tabs share one border edge.
    /// </summary>
    [Fact]
    public void TabSpacing_Default_IsNegativeOne ()
    {
        IDriver driver = CreateTestDriver (30, 5);
        Tabs tabs = new () { Driver = driver, Width = 30, Height = 5 };

        Assert.Equal (-1, tabs.TabSpacing);

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Default overlap: each tab shares one border edge with its neighbor
        // Tab1 width=6, offset=0; Tab2 offset=6-1=5; Tab3 offset=5+(6-1)=10
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Setting TabSpacing = -1 explicitly should produce the same offsets as the default.
    /// </summary>
    [Fact]
    public void TabSpacing_NegativeOne_MatchesOriginalBehavior ()
    {
        IDriver driver = CreateTestDriver (30, 5);
        Tabs tabs = new () { Driver = driver, Width = 30, Height = 5, TabSpacing = -1 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Same as default: 0, 5, 10
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     TabSpacing = 0 means tabs are placed edge-to-edge with no overlap or gap.
    ///     Each tab starts at the full width of the previous tab.
    /// </summary>
    [Fact]
    public void TabSpacing_Zero_TabsEdgeToEdge ()
    {
        IDriver driver = CreateTestDriver (30, 5);
        Tabs tabs = new () { Driver = driver, Width = 30, Height = 5, TabSpacing = 0 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Each tab EffectiveTabLength is 6 (4 chars + 2 border cells).
        // With spacing=0: Tab1 at 0, Tab2 at 6, Tab3 at 12.
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (6, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (12, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     TabSpacing = 1 inserts a 1-cell gap between adjacent tab headers.
    /// </summary>
    [Fact]
    public void TabSpacing_Positive_InsertsGap ()
    {
        IDriver driver = CreateTestDriver (40, 5);
        Tabs tabs = new () { Driver = driver, Width = 40, Height = 5, TabSpacing = 1 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Each tab EffectiveTabLength is 6.
        // With spacing=1: Tab1 at 0, Tab2 at 6+1=7, Tab3 at 7+6+1=14.
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (7, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (14, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies GetTotalHeaderSpan computes correctly for various TabSpacing values.
    ///     3 tabs, each EffectiveTabLength = 6.
    /// </summary>
    [Theory]
    [InlineData (-1, 16)] // (6-1)+(6-1)+(6-1)+1 = 5+5+5+1 = 16
    [InlineData (0, 18)] // (6+0)+(6+0)+(6+0) = 18
    [InlineData (1, 20)] // (6+1)+(6+1)+(6+1)-1 = 7+7+7-1 = 20
    [InlineData (2, 22)] // (6+2)+(6+2)+(6+2)-2 = 8+8+8-2 = 22
    public void TabSpacing_TotalHeaderSpan_CorrectForAllValues (int spacing, int expectedSpan)
    {
        IDriver driver = CreateTestDriver (40, 5);
        Tabs tabs = new () { Driver = driver, Width = 40, Height = 5, TabSpacing = spacing };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // GetTotalHeaderSpan is private — verify indirectly via tab offsets + last tab length
        int lastOffset = ((BorderView)tab3.Border.View!).TabOffset;
        int lastLength = ((BorderView)tab3.Border.View!).EffectiveTabLength;
        int computedSpan = lastOffset + lastLength;

        Assert.Equal (expectedSpan, computedSpan);

        tabs.Dispose ();
    }

    #endregion

    #region ScrollOffset Clamping Tests

    [Fact]
    public void ScrollOffset_NegativeValue_ClampedToZero ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (20, 5);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        tabs.Add (tab1, tab2);
        tabs.Layout ();

        tabs.ScrollOffset = -5;

        Assert.Equal (0, tabs.ScrollOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_Zero_IsValid ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (20, 5);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        tabs.Add (tab1, tab2);
        tabs.Layout ();

        tabs.ScrollOffset = 0;

        Assert.Equal (0, tabs.ScrollOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_ExceedsScrollableContent_IsClamped ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Total header span is 16 (3 tabs × 6 - 2 shared edges).
        // Setting an absurdly high value should be clamped.
        tabs.ScrollOffset = 100;

        Assert.True (tabs.ScrollOffset < 100, "ScrollOffset should be clamped when exceeding scrollable content");
        Assert.True (tabs.ScrollOffset >= 0, "ScrollOffset should never be negative after clamping");

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_ValidMiddleValue_IsAccepted ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // A value within the valid range should be accepted as-is
        tabs.ScrollOffset = 3;

        Assert.Equal (3, tabs.ScrollOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_SameValue_DoesNotChange ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tabs.ScrollOffset = 3;
        int offsetAfterFirst = tabs.ScrollOffset;

        // Setting same value again should be a no-op
        tabs.ScrollOffset = 3;

        Assert.Equal (offsetAfterFirst, tabs.ScrollOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_NoTabs_StaysAtZero ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };
        tabs.Layout ();

        tabs.ScrollOffset = 5;

        // With no tabs, there's nothing to scroll
        Assert.Equal (0, tabs.ScrollOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_AllTabsFit_ValueIsAccepted ()
    {
        // Claude - Opus 4.6
        // When all tabs fit, there's no scrollbar constraining the value,
        // so the setter accepts it. EnsureTabVisible will keep it at 0
        // when selecting tabs, but direct assignment is not clamped here.
        IDriver driver = CreateTestDriver (20, 5);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5 };

        View tab1 = new () { Title = "A" };
        View tab2 = new () { Title = "B" };
        tabs.Add (tab1, tab2);
        tabs.Layout ();

        // Two tiny tabs (TabLength = 3 each, total span = 5) fit in 20 columns.
        tabs.ScrollOffset = 3;

        Assert.Equal (3, tabs.ScrollOffset);

        tabs.Dispose ();
    }

    [Fact]
    public void ScrollOffset_UpdatesTabOffsets ()
    {
        // Claude - Opus 4.6
        IDriver driver = CreateTestDriver (10, 5);
        Tabs tabs = new () { Driver = driver, Width = 10, Height = 5 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };
        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        // Before scrolling — natural offsets
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);

        // After scrolling — offsets shift by scroll amount
        tabs.ScrollOffset = 4;

        Assert.Equal (-4, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (1, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (6, ((BorderView)tab3.Border.View!).TabOffset);

        tabs.Dispose ();
    }

    #endregion
}
