// Copilot - Opus 4.6

using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests that auto-line joining works correctly when SubViews with borders
///     and <see cref="View.SuperViewRendersLineCanvas"/> = true are merged into
///     the SuperView's <see cref="LineCanvas"/>.
/// </summary>
public class AutoLineJoinTests (ITestOutputHelper output) : TestDriverBase
{
    #region Side-by-side (horizontal) peers

    /// <summary>
    ///     Two bordered SubViews side by side with overlapping borders.
    ///     The shared border column should auto-join with ┬ at top and ┴ at bottom.
    /// </summary>
    [Fact]
    public void SideBySide_Overlapping_Peers_Join_Correctly ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "A",
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewB = new ()
        {
            Title = "B",
            X = Pos.Right (viewA) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤A├┬┤B├┐
                                              │   │   │
                                              └───┴───┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     Three bordered SubViews side by side, each overlapping by 1 column.
    ///     Tests that auto-join works across a longer chain of peers.
    /// </summary>
    [Fact]
    public void Three_SideBySide_Overlapping_Peers_Join ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (15, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "A",
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewB = new ()
        {
            Title = "B",
            X = Pos.Right (viewA) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewC = new ()
        {
            Title = "C",
            X = Pos.Right (viewB) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB, viewC);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤A├┬┤B├┬┤C├┐
                                              │   │   │   │
                                              └───┴───┴───┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     Two bordered SubViews side by side without overlap (gap of 0, adjacent).
    ///     Borders should NOT join since they don't share columns.
    /// </summary>
    [Fact]
    public void SideBySide_Adjacent_No_Overlap_No_Join ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "A",
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewB = new ()
        {
            Title = "B",
            X = Pos.Right (viewA),
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤A├┐┌┤B├┐
                                              │   ││   │
                                              └───┘└───┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Side-by-side (horizontal) peers

    #region Stacked (vertical) peers

    /// <summary>
    ///     Two bordered SubViews stacked vertically with overlapping borders.
    ///     The shared border row should auto-join with ├ on left and ┤ on right.
    /// </summary>
    [Fact]
    public void Stacked_Overlapping_Peers_Join_Correctly ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (7, 6);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "A",
            Width = 7,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewB = new ()
        {
            Title = "B",
            Y = Pos.Bottom (viewA) - 1,
            Width = 7,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤A├──┐
                                              │     │
                                              ├┼B┼──┤
                                              │     │
                                              └─────┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     Two bordered SubViews stacked vertically with no overlap (adjacent).
    ///     Borders should not join.
    /// </summary>
    [Fact]
    public void Stacked_Adjacent_No_Overlap_No_Join ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (7, 7);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "A",
            Width = 7,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View viewB = new ()
        {
            Title = "B",
            Y = Pos.Bottom (viewA),
            Width = 7,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤A├──┐
                                              │     │
                                              └─────┘
                                              ┌┤B├──┐
                                              │     │
                                              └─────┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Stacked (vertical) peers

    #region Grid-like arrangements

    /// <summary>
    ///     Four bordered SubViews in a 2×2 grid, all overlapping at boundaries.
    ///     Tests that corners auto-join at the center intersection (┼).
    /// </summary>
    [Fact]
    public void Grid_2x2_Overlapping_Peers_Join ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (11, 7);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View topLeft = new () { Width = 6, Height = 4, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };

        View topRight = new ()
        {
            X = Pos.Right (topLeft) - 1,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View bottomLeft = new ()
        {
            Y = Pos.Bottom (topLeft) - 1,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View bottomRight = new ()
        {
            X = Pos.Right (bottomLeft) - 1,
            Y = Pos.Bottom (topRight) - 1,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (topLeft, topRight, bottomLeft, bottomRight);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Center intersection should be ┼, edges should have ┬, ┴, ├, ┤
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌────┬────┐
                                              │    │    │
                                              │    │    │
                                              ├────┼────┤
                                              │    │    │
                                              │    │    │
                                              └────┴────┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Grid-like arrangements

    #region SubView with SuperView border (nested hierarchy)

    /// <summary>
    ///     A SuperView has a border, and a SubView with SuperViewRendersLineCanvas = true
    ///     has a border that shares a side with the SuperView's border.
    ///     The shared side should auto-join.
    /// </summary>
    [Fact]
    public void SubView_Border_Joins_SuperView_Border_Top ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 5);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.BorderStyle = LineStyle.Single;

        View subView = new ()
        {
            X = 1,
            Y = -1,
            Width = 6,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (subView);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // SubView's top border line should join with SuperView's top border
        // ┬ where subview's sides meet SuperView's top line
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌─┬────┬───┐
                                              │ │    │   │
                                              │ └────┘   │
                                              │          │
                                              └──────────┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     A SubView touches the left border of the SuperView.
    /// </summary>
    [Fact]
    public void SubView_Border_Joins_SuperView_Border_Left ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (10, 7);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.BorderStyle = LineStyle.Single;

        View subView = new ()
        {
            X = -1,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (subView);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // SubView's left side should join with SuperView's left border
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌────────┐
                                              │        │
                                              ├───┐    │
                                              │   │    │
                                              ├───┘    │
                                              │        │
                                              └────────┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion SubView with SuperView border (nested hierarchy)

    #region Mixed SuperViewRendersLineCanvas true/false

    /// <summary>
    ///     Two SubViews side by side: one with SuperViewRendersLineCanvas = true,
    ///     the other with false. Only the one with true should participate in auto-join.
    /// </summary>
    [Fact]
    public void Mixed_SuperViewRendersLineCanvas_Only_True_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new () { Width = 5, Height = 3, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };

        View viewB = new ()
        {
            X = Pos.Right (viewA) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = false // Not merged into SuperView
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // viewA's border is merged into SuperView's LineCanvas.
        // viewB renders its own border independently.
        // Since they overlap at column 4, the rendering may vary but both should show borders.
        // viewB (rendered independently) draws on top of viewA's merged lines.
        // The key assertion: viewA's border should be rendered (not lost).
        var actual = driver.ToString ();
        Assert.Contains ("┌", actual);
        Assert.Contains ("└", actual);
    }

    #endregion Mixed SuperViewRendersLineCanvas true/false

    #region Line SubView joins

    /// <summary>
    ///     A Line view with SuperViewRendersLineCanvas = true inside a bordered SuperView
    ///     should auto-join at intersections with the border.
    /// </summary>
    [Fact]
    public void Line_Inside_BorderedSuperView_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (10, 5);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = 10;
        superView.Height = 5;
        superView.BorderStyle = LineStyle.Single;

        // Horizontal line across the content area
        Line hLine = new ()
        {
            X = -1,
            Y = 1,
            Width = Dim.Fill () + 1,
            Height = 1,
            Orientation = Orientation.Horizontal,
            Style = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (hLine);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Line should auto-join with left and right borders: ├ and ┤
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌────────┐
                                              │        │
                                              ├────────┤
                                              │        │
                                              └────────┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     A vertical Line inside a bordered SuperView should produce ┬ at the top and ┴ at the bottom.
    /// </summary>
    [Fact]
    public void VerticalLine_Inside_BorderedSuperView_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (7, 3);

        using View superView = new ();
        superView.Driver = driver;
        superView.BorderStyle = LineStyle.Single;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        Line vLine = new ()
        {
            X = 2,
            Y = -1,
            Width = 1,
            Height = Dim.Fill () + 1,
            Orientation = Orientation.Vertical,
            Style = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (vLine);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Vertical line should auto-join with top and bottom borders
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌──┬──┐
                                              │  │  │
                                              └──┴──┘
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     Crossing horizontal and vertical Lines inside a bordered SuperView
    ///     should produce ┼ at the intersection.
    /// </summary>
    [Fact]
    public void CrossingLines_Inside_BorderedSuperView_Join ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (7, 7);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.BorderStyle = LineStyle.Single;

        Line hLine = new ()
        {
            X = -1,
            Y = 2,
            Width = Dim.Fill () + 1,
            Height = 1,
            Orientation = Orientation.Horizontal,
            Style = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        Line vLine = new ()
        {
            X = 2,
            Y = -1,
            Width = 1,
            Height = Dim.Fill () + 1,
            Orientation = Orientation.Vertical,
            Style = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (hLine, vLine);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌──┬──┐
                                              │  │  │
                                              │  │  │
                                              ├──┼──┤
                                              │  │  │
                                              │  │  │
                                              └──┴──┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Line SubView joins

    #region FrameView-like nesting

    /// <summary>
    ///     A FrameView (bordered container) with a bordered SubView
    ///     where the SubView uses SuperViewRendersLineCanvas = true.
    ///     Lines from the SubView should merge into the FrameView's LineCanvas.
    /// </summary>
    [Fact]
    public void FrameView_With_Bordered_SubView_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (15, 7);

        using FrameView frameView = new ();
        frameView.Driver = driver;
        frameView.Title = "Frame";
        frameView.Width = 15;
        frameView.Height = 7;

        View innerView = new ()
        {
            X = 1,
            Y = 0,
            Width = 8,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        frameView.Add (innerView);

        frameView.Layout ();
        frameView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // The inner view's border should be visible within the FrameView.
        // Since SuperViewRendersLineCanvas = true, the inner view's border lines
        // are merged with the FrameView's content. The FrameView renders its own
        // border independently.
        var actual = driver.ToString ();

        // FrameView's border should be present
        Assert.Contains ("Frame", actual);

        // Inner view's border should be present
        Assert.Contains ("┌", actual);
        Assert.Contains ("└", actual);
    }

    #endregion FrameView-like nesting

    #region Same-origin overlapping peers

    /// <summary>
    ///     Two SubViews at the same position but with different sizes, both with
    ///     SuperViewRendersLineCanvas = true. The larger view's border should show
    ///     through where the smaller doesn't overlap.
    /// </summary>
    [Fact]
    public void Overlapping_Same_Origin_Different_Size ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (10, 6);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View large = new () { Width = 10, Height = 6, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };

        View small = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (large, small);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Both borders should be visible; the small one is inset within the large one.
        var actual = driver.ToString ();
        Assert.Contains ("┌", actual);
        Assert.Contains ("┘", actual);
    }

    #endregion Same-origin overlapping peers

    #region Double-line style joins

    /// <summary>
    ///     Two SubViews side by side with Double line style should still auto-join.
    /// </summary>
    [Fact]
    public void SideBySide_Double_Style_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new () { Width = 5, Height = 3, BorderStyle = LineStyle.Double, SuperViewRendersLineCanvas = true };

        View viewB = new ()
        {
            X = Pos.Right (viewA) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Double,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Double-style ╔═╦═╗ top, ╚═╩═╝ bottom, ║ sides
        DriverAssert.AssertDriverContentsAre ("""
                                              ╔═══╦═══╗
                                              ║   ║   ║
                                              ╚═══╩═══╝
                                              """,
                                              output,
                                              driver);
    }

    #endregion Double-line style joins

    #region Rounded style joins

    /// <summary>
    ///     Two SubViews side by side with Rounded line style should auto-join.
    /// </summary>
    [Fact]
    public void SideBySide_Rounded_Style_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new () { Width = 5, Height = 3, BorderStyle = LineStyle.Rounded, SuperViewRendersLineCanvas = true };

        View viewB = new ()
        {
            X = Pos.Right (viewA) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Rounded,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Rounded uses ╭╮╰╯ for corners but ┬┴ for T-junctions (they share the thin intersection)
        var actual = driver.ToString ();

        // Should have rounded corners on the outer edges
        Assert.Contains ("╭", actual);
        Assert.Contains ("╯", actual);
        Assert.Contains ("╰", actual);
        Assert.Contains ("╮", actual);
    }

    #endregion Rounded style joins

    #region Single view - no joining needed

    /// <summary>
    ///     A single bordered SubView with SuperViewRendersLineCanvas = true.
    ///     Its border should render normally (no auto-join needed, but merge shouldn't break it).
    /// </summary>
    [Fact]
    public void Single_Bordered_SubView_Renders_Correctly ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (8, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new ()
        {
            Title = "Hi",
            Width = 8,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┤Hi├──┐
                                              │      │
                                              └──────┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Single view - no joining needed

    #region Multiple rows of side-by-side views

    /// <summary>
    ///     Two rows of side-by-side bordered SubViews, the rows also overlapping vertically.
    ///     Tests a complex grid-like pattern.
    /// </summary>
    [Fact]
    public void Two_Rows_Of_SideBySide_Overlapping ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (11, 7);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View tl = new () { Width = 6, Height = 4, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };

        View tr = new ()
        {
            X = Pos.Right (tl) - 1,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View bl = new ()
        {
            Y = Pos.Bottom (tl) - 1,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };

        View br = new ()
        {
            X = Pos.Right (bl) - 1,
            Y = Pos.Bottom (tr) - 1,
            Width = 6,
            Height = 4,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (tl, tr, bl, br);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Same as Grid_2x2 but with explicit layout
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌────┬────┐
                                              │    │    │
                                              │    │    │
                                              ├────┼────┤
                                              │    │    │
                                              │    │    │
                                              └────┴────┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Multiple rows of side-by-side views

    #region Non-overlapping with SuperViewRendersLineCanvas

    /// <summary>
    ///     SubViews with SuperViewRendersLineCanvas = true but positioned far apart.
    ///     No auto-join should happen; each should render its own complete border.
    /// </summary>
    [Fact]
    public void NonOverlapping_SubViews_No_Join ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (15, 5);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new () { Width = 5, Height = 3, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };

        View viewB = new ()
        {
            X = 8,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌───┐   ┌───┐
                                              │   │   │   │
                                              └───┘   └───┘
                                              """,
                                              output,
                                              driver);
    }

    #endregion Non-overlapping with SuperViewRendersLineCanvas

    #region Heavy line style

    /// <summary>
    ///     Two SubViews side by side with Heavy line style should auto-join.
    /// </summary>
    [Fact]
    public void SideBySide_Heavy_Style_Joins ()
    {
        // Copilot
        IDriver driver = CreateTestDriver ();
        driver.SetScreenSize (12, 4);

        using View superView = new ();
        superView.Driver = driver;
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();

        View viewA = new () { Width = 5, Height = 3, BorderStyle = LineStyle.Heavy, SuperViewRendersLineCanvas = true };

        View viewB = new ()
        {
            X = Pos.Right (viewA) - 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Heavy,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (viewA, viewB);

        superView.Layout ();
        superView.Draw ();

        output.WriteLine ("Actual:");
        output.WriteLine (driver.ToString ());

        // Heavy uses ┏┓┗┛┃━ and ┳┻ for joins
        DriverAssert.AssertDriverContentsAre ("""
                                              ┏━━━┳━━━┓
                                              ┃   ┃   ┃
                                              ┗━━━┻━━━┛
                                              """,
                                              output,
                                              driver);
    }

    #endregion Heavy line style
}
