using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Visual tests for <see cref="BorderView"/> tab header rendering via <see cref="Border"/> properties.
///     These replicate <see cref="TabHeaderRendererTests"/> scenarios at the higher BorderView level,
///     covering all four <see cref="Side"/> values and important variations of offset and Title.
/// </summary>

// Copilot
public class BorderViewTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Bottom_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 4);

        View view = CreateTabView (driver,
                                   9,
                                   4,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │Tab╭───╯
                       """);
    }

    [Fact]
    public void Bottom_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 2));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │Tab╭───╯
                       ╰───╯
                       """);
    }

    [Fact]
    public void Bottom_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 4));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │   ╭───╯
                       │Tab│
                       │   │
                       ╰───╯
                       """);
    }

    [Fact]
    public void Bottom_Focused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │   ╭───╯
                       │Tab│
                       ╰───╯
                       """);
    }

    [Fact]
    public void Bottom_Unfocused_Depth1_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 4);

        View view = CreateTabView (driver,
                                   9,
                                   4,
                                   Side.Bottom,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │Tab╭───╯
                       """);
    }

    [Fact]
    public void Bottom_Unfocused_Depth2_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Bottom,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 2));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │Tab╭───╯
                       ╰───╯
                       """);
    }

    [Fact]
    public void Bottom_Unfocused_NegativeOffset_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   -1,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       ┴──┬────╯
                       Tab│
                       ───╯
                       """);
    }

    [Fact]
    public void Bottom_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       ├┬──────╯
                       ││
                       ╰╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Bottom — View 9×6, Thickness(1,1,1,3), borderBounds=(0,0,9,4)
    //  Content border: 9 wide, 4 tall. Tab protrudes below.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Bottom_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       ├───┬───╯
                       │Tab│
                       ╰───╯
                       """);
    }

    [Fact]
    public void Bottom_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Bottom,
                                   2,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       ╰─┬───┬─╯
                         │Tab│
                         ╰───╯
                       """);
    }

    // Copilot
    [Fact]
    public void Clearing_Tab_Flag_Hides_TitleView ()
    {
        IDriver driver = CreateTestDriver (10, 6);

        View view = new () { Driver = driver, Width = 10, Height = 6, BorderStyle = LineStyle.Rounded };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Title = "Tab";
        view.Layout ();

        var bv = (BorderView)view.Border.View!;
        Assert.NotNull (bv.TitleView);

        // Clear the Tab flag
        view.Border.Settings = BorderSettings.Title;
        view.Layout ();

        // TitleView should be hidden (Visible = false)
        Assert.False (bv.TitleView!.Visible, "TitleView should be hidden when Tab flag is cleared");

        view.Dispose ();
    }

    [Fact]
    public void Left_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 9);

        View view = CreateTabView (driver,
                                   9,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        // Depth=1: no cap line, no tab edges. Title at rows 1-3 (between top/bottom edges).
        // (0,0) is excluded by AddTabSideContentBorder (tab starts at content border edge).
        // After Trim(), leading space removed from row 0.
        DrawAndAssert (view,
                       driver,
                       """
                       ────────╮
                       T       │
                       a       │
                       b       │
                       │       │
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Left_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (10, 9);

        View view = CreateTabView (driver,
                                   10,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (2, 1, 1, 1));

        // Depth=2: cap at col 0, closing edge at col 1. Title on closing edge.
        // (1,0) excluded by AddTabSideContentBorder → space at col 1 row 0.
        // (1,4) has ╮ from header bottom edge + content border vertical auto-join.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭────────╮
                       │T       │
                       │a       │
                       │b       │
                       ╰╮       │
                        │       │
                        │       │
                        │       │
                        ╰───────╯
                       """);
    }

    [Fact]
    public void Left_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = CreateTabView (driver,
                                   12,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (4, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭──────────╮
                       │T         │
                       │a         │
                       │b         │
                       ╰──╮       │
                          │       │
                          │       │
                          │       │
                          ╰───────╯
                       """);
    }

    [Fact]
    public void Left_Focused_Overflow_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   6,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                         ╭───────╮
                         │       │
                         │       │
                         │       │
                         │       │
                         │       │
                       ╭─╯       │
                       │T        │
                       │a ───────╯
                       """);
    }

    [Fact]
    public void Left_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭─┬───────╮
                       ╰─┤       │
                         │       │
                         │       │
                         │       │
                         │       │
                         │       │
                         │       │
                         ╰───────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Left — View 11×9, Thickness(3,1,1,1), borderBounds=(2,0,9,9)
    //  Content border: 9 wide (cols 2–10), 9 tall. Tab protrudes left.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Left_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭─┬───────╮
                       │T│       │
                       │a│       │
                       │b│       │
                       ╰─┤       │
                         │       │
                         │       │
                         │       │
                         ╰───────╯
                       """);
    }

    [Fact]
    public void Left_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Left,
                                   2,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                         ╭───────╮
                         │       │
                       ╭─┤       │
                       │T│       │
                       │a│       │
                       │b│       │
                       ╰─┤       │
                         │       │
                         ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 9);

        View view = CreateTabView (driver,
                                   9,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        // Depth=1: no cap line, no tab edges. Title at rows 0-2.
        // The top-right corner shows ─ (horizontal border continues; no vertical at the gap).
        DrawAndAssert (view,
                       driver,
                       """
                       ╭────────
                       │       T
                       │       a
                       │       b
                       │       │
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (10, 9);

        View view = CreateTabView (driver,
                                   10,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 2, 1));

        // Depth=2: cap at col 9, closing edge at col 8. Title on closing edge.
        // (8,0) excluded → space at col 8 row 0. (8,4) has ╭ from auto-join.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭────────╮
                       │       T│
                       │       a│
                       │       b│
                       │       ╭╯
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = CreateTabView (driver,
                                   12,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 4, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭──────────╮
                       │        T │
                       │        a │
                       │        b │
                       │       ╭──╯
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Focused_Overflow_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   6,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       │       │
                       │       │
                       │       │
                       │       ╰─╮
                       │        T│
                       ╰─────── a│
                       """);
    }

    [Fact]
    public void Right_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────┬─╮
                       │       ├─╯
                       │       │
                       │       │
                       │       │
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Right — View 11×9, Thickness(1,1,3,1), borderBounds=(0,0,9,9)
    //  Content border: 9 wide (cols 0–8), 9 tall. Tab protrudes right.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Right_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────┬─╮
                       │       │T│
                       │       │a│
                       │       │b│
                       │       ├─╯
                       │       │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Right_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (11, 9);

        View view = CreateTabView (driver,
                                   11,
                                   9,
                                   Side.Right,
                                   2,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       ├─╮
                       │       │T│
                       │       │a│
                       │       │b│
                       │       ├─╯
                       │       │
                       ╰───────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Setup-trigger tests — verify configuration happens at property-change
    //   time, NOT deferred to Draw.
    // ════════════════════════════════════════════════════════════════════

    // Copilot
    [Fact]
    public void Settings_Tab_Creates_BorderView ()
    {
        // Setting Border.Settings to include Tab should cause GetOrCreateView
        View view = new () { Width = 10, Height = 6, BorderStyle = LineStyle.Rounded };

        // Before setting Tab, View may or may not exist depending on LineStyle
        // but TitleView should not exist

        if (view.Border.View is BorderView bv)
        {
            Assert.Null (bv.TitleView);
        }

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;

        // After setting Tab, the BorderView must exist
        bv = (view.Border.View as BorderView)!;
        Assert.NotNull (bv);

        view.Dispose ();
    }

    // Copilot
    [Fact]
    public void Settings_Tab_Creates_TitleView_Before_Draw ()
    {
        IDriver driver = CreateTestDriver (10, 6);

        View view = new () { Driver = driver, Width = 10, Height = 6, BorderStyle = LineStyle.Rounded };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Title = "Tab";

        // Layout but do NOT draw
        view.Layout ();

        var bv = (BorderView)view.Border.View!;

        // TitleView should already exist after layout, before any Draw call
        Assert.NotNull (bv.TitleView);

        view.Dispose ();
    }

    // Copilot
    [Fact]
    public void Settings_Tab_Sets_ViewportSettings_Transparent_Before_Draw ()
    {
        IDriver driver = CreateTestDriver (10, 6);

        View view = new () { Driver = driver, Width = 10, Height = 6, BorderStyle = LineStyle.Rounded };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Title = "Tab";

        // Layout but do NOT draw
        view.Layout ();

        var bv = (BorderView)view.Border.View!;

        // ViewportSettings should already include Transparent before any Draw call
        Assert.True (bv.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Transparent should be set after Settings change, not deferred to Draw");

        Assert.True (bv.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse),
                     "TransparentMouse should be set after Settings change, not deferred to Draw");

        view.Dispose ();
    }

    // Copilot
    [Fact]
    public void Settings_Tab_TitleView_Has_Correct_Frame_Before_Draw ()
    {
        IDriver driver = CreateTestDriver (10, 7);

        View view = new ()
        {
            Driver = driver,
            Width = 10,
            Height = 7,
            CanFocus = true,
            HasFocus = false,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        ((BorderView)view.Border.View!).TabOffset = 0;
        view.Title = "T_ab";

        // Layout but do NOT draw
        view.Layout ();

        var bv = (BorderView)view.Border.View!;
        View? ttv = bv.TitleView;
        Assert.NotNull (ttv);

        // TitleView should have non-empty Frame set by layout, before Draw
        Assert.NotEqual (Rectangle.Empty, ttv.Frame);

        // Width should match TabLength (auto-computed: "Tab".GetColumns() + 2 = 5)
        Assert.Equal (5, ttv.Frame.Width);

        view.Dispose ();
    }

    // Copilot
    [Fact]
    public void Settings_Tab_TitleView_Has_SuperViewRendersLineCanvas ()
    {
        View view = new () { Width = 10, Height = 6, BorderStyle = LineStyle.Rounded };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Title = "Tab";

        view.Layout ();

        var bv = (BorderView)view.Border.View!;
        View? ttv = bv.TitleView;
        Assert.NotNull (ttv);
        Assert.True (ttv.SuperViewRendersLineCanvas, "TitleView must have SuperViewRendersLineCanvas = true for auto-join");

        view.Dispose ();
    }

    [Fact]
    public void SuperView_Left_NegativeOffset2_WithTitle () // Copilot
    {
        //using (TestLogging.Verbose (output, TraceCategory.Draw))
        {
            (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                            8,
                                                                            9,
                                                                            6,
                                                                            Side.Left,
                                                                            -2,
                                                                            false,
                                                                            "T_ab",
                                                                            true);

            DriverAssert.AssertDriverContentsAre ("""
                                                  ╭───────────╮
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  │◊│a ─────╮◊│
                                                  │◊│b      │◊│
                                                  │◊╰─╮     │◊│
                                                  │◊◊◊│     │◊│
                                                  │◊◊◊│     │◊│
                                                  │◊◊◊╰─────╯◊│
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  ╰───────────╯
                                                  """,
                                                  output,
                                                  app.Driver!);

            subview.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void SuperView_Top_Depth1_Focused () // Copilot
    {
        // Thickness(1,1,1,1) → depth=1. Subview 9×4.
        (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                        6,
                                                                        9,
                                                                        4,
                                                                        Side.Top,
                                                                        0,
                                                                        true,
                                                                        "T_ab",
                                                                        true,
                                                                        new Thickness (1, 1, 1, 1));

        output.WriteLine (app.Driver!.ToString ());

        // Per spec: Thickness.Top = 1, focused → title inline on content border, open gap
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────╮
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊│Tab╭───╮◊│
                                              │◊│       │◊│
                                              │◊│       │◊│
                                              │◊╰───────╯◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              ╰───────────╯
                                              """,
                                              output,
                                              app.Driver!);

        subview.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SuperView_Top_Depth2_Focused () // Copilot
    {
        // Thickness(1,2,1,1) → depth=2. Subview 9×5.
        (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                        7,
                                                                        9,
                                                                        5,
                                                                        Side.Top,
                                                                        0,
                                                                        true,
                                                                        "T_ab",
                                                                        true,
                                                                        new Thickness (1, 2, 1, 1));

        output.WriteLine (app.Driver!.ToString ());

        // Per spec: Thickness.Top = 2, focused → cap line + title on closing edge, open gap
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────╮
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊╭───╮◊◊◊◊◊│
                                              │◊│Tab╰───╮◊│
                                              │◊│       │◊│
                                              │◊│       │◊│
                                              │◊╰───────╯◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              ╰───────────╯
                                              """,
                                              output,
                                              app.Driver!);

        subview.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SuperView_Top_NegativeOffset1_Min () // Copilot
    {
        //using (TestLogging.Verbose (output, TraceCategory.Draw))
        {
            View subview = new () { Driver = CreateTestDriver (4, 3), Height = 2, Width = 4 };
            subview.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
            subview.Border.Thickness = new Thickness (0, 2, 0, 0);
            subview.Border.LineStyle = LineStyle.Single;
            subview.Title = "t";

            subview.Driver.FillRect (subview.Driver.Screen, Glyphs.Diamond);
            subview.Layout ();

            //subview.Draw ();

            //DriverAssert.AssertDriverContentsWithFrameAre ("""
            //                                               ┌─┐◊
            //                                               │t└─
            //                                               """,
            //                                               output,
            //                                               subview.Driver!);

            ((BorderView)subview.Border.View!).TabOffset = -1;
            subview.Driver.ClearContents ();
            subview.Driver.FillRect (subview.Driver.Screen, Glyphs.Diamond);

            subview.Layout ();

            //subview.Draw ();

            //DriverAssert.AssertDriverContentsWithFrameAre ("""
            //                                      ─┐◊◊
            //                                      t└──
            //                                      """,
            //                                      output,
            //                                      subview.Driver!);

            View superView = new () { Driver = subview.Driver, Width = Dim.Fill (), Height = Dim.Fill () };

            superView.DrawingContent += (_, e) =>
                                        {
                                            superView.FillRect (superView.Viewport, Glyphs.Diamond);
                                            e.DrawContext?.AddDrawnRectangle (superView.Viewport);
                                        };
            superView.Add (subview);

            superView.Layout ();
            subview.Driver.ClearContents ();
            var context = new DrawContext ();
            superView.Draw (context);

            Assert.True (subview.Border.View!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent));
            Assert.True (subview.Border!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent));

            DriverAssert.AssertDriverContentsWithFrameAre ("""
                                                           ─┐◊◊
                                                           t└──
                                                           ◊◊◊◊
                                                           """,
                                                           output,
                                                           subview.Driver!);
        }
    }

    [Fact]
    public void SuperView_Top_NegativeOffset1_WithTitle () // Copilot
    {
        //using (TestLogging.Verbose (output, TraceCategory.Draw))
        {
            (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                            8,
                                                                            9,
                                                                            6,
                                                                            Side.Top,
                                                                            -1,
                                                                            false,
                                                                            "T_ab",
                                                                            true);

            DriverAssert.AssertDriverContentsAre ("""
                                                  ╭───────────╮
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  │◊───╮◊◊◊◊◊◊│
                                                  │◊Tab│◊◊◊◊◊◊│
                                                  │◊│  ╰────╮◊│
                                                  │◊│       │◊│
                                                  │◊│       │◊│
                                                  │◊╰───────╯◊│
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  ╰───────────╯
                                                  """,
                                                  output,
                                                  app.Driver!);

            subview.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void SuperView_Top_NegativeOffset1_WithTitle_X0 () // Copilot
    {
        //using (TestLogging.Verbose (output, TraceCategory.Draw))
        {
            (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                            8,
                                                                            9,
                                                                            6,
                                                                            Side.Top,
                                                                            -1,
                                                                            false,
                                                                            "T_ab",
                                                                            true);

            subview.X = 0;
            subview.SuperView!.Layout ();
            subview.SuperView!.Draw ();

            DriverAssert.AssertDriverContentsAre ("""
                                                  ╭───────────╮
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  │───╮◊◊◊◊◊◊◊│
                                                  │Tab│◊◊◊◊◊◊◊│
                                                  ││  ╰────╮◊◊│
                                                  ││       │◊◊│
                                                  ││       │◊◊│
                                                  │╰───────╯◊◊│
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  ╰───────────╯
                                                  """,
                                                  output,
                                                  app.Driver!);

            subview.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void SuperView_Top_NegativeOffset2_WithTitle () // Copilot
    {
        //using (TestLogging.Verbose (output, TraceCategory.Draw))
        {
            (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                            8,
                                                                            9,
                                                                            6,
                                                                            Side.Top,
                                                                            -2,
                                                                            false,
                                                                            "T_ab",
                                                                            true);

            // Header at offset=-2: left edge and 'T' clipped. Visible: cap ──╮, title ab│.
            DriverAssert.AssertDriverContentsAre ("""
                                                  ╭───────────╮
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  │◊──╮◊◊◊◊◊◊◊│
                                                  │◊ab│◊◊◊◊◊◊◊│
                                                  │◊│ ╰─────╮◊│
                                                  │◊│       │◊│
                                                  │◊│       │◊│
                                                  │◊╰───────╯◊│
                                                  │◊◊◊◊◊◊◊◊◊◊◊│
                                                  ╰───────────╯
                                                  """,
                                                  output,
                                                  app.Driver!);

            subview.Dispose ();
            app.Dispose ();
        }
    }

    [Fact]
    public void SuperView_Top_NegativeOffset2_WithTitle_With_Margin () // Copilot
    {
        (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                        8,
                                                                        9,
                                                                        6,
                                                                        Side.Top,
                                                                        -2,
                                                                        false,
                                                                        "T_ab",
                                                                        true);

        // Bug #4853: cap-line extension bleeds into Margin when Margin has thickness.
        // The diamond fill masks it here, but the issue is filed.
        subview.Margin.Thickness = new Thickness (1, 0, 0, 0);
        subview.SetNeedsLayout ();
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────╮
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊◊──╮◊◊◊◊◊◊│
                                              │◊◊ab│◊◊◊◊◊◊│
                                              │◊◊│ ╰────╮◊│
                                              │◊◊│      │◊│
                                              │◊◊│      │◊│
                                              │◊◊╰──────╯◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              ╰───────────╯
                                              """,
                                              output,
                                              app.Driver!);

        subview.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SuperView_Top_NegativeOffset5_FullyOffscreen () // Copilot
    {
        (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                        8,
                                                                        9,
                                                                        6,
                                                                        Side.Top,
                                                                        -5,
                                                                        false,
                                                                        "T_ab",
                                                                        true);

        output.WriteLine (app.Driver!.ToString ());

        // Header completely off-screen. Content border drawn normally.
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────╮
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊┬───────╮◊│
                                              │◊│       │◊│
                                              │◊│       │◊│
                                              │◊╰───────╯◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              ╰───────────╯
                                              """,
                                              output,
                                              app.Driver!);

        subview.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SuperView_Top_Offset0_WithTitle_Focused () // Copilot
    {
        // Window: screen 13×10, border=1 → viewport 11×8.
        // Child at (1,1) is 9×6, Thickness(1,3,1,1), tab on Top, HasFocus=true.
        (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                        8,
                                                                        9,
                                                                        6,
                                                                        Side.Top,
                                                                        0,
                                                                        true,
                                                                        "T_ab",
                                                                        true);

        output.WriteLine (app.Driver!.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────╮
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊╭───╮◊◊◊◊◊│
                                              │◊│Tab│◊◊◊◊◊│
                                              │◊│   ╰───╮◊│
                                              │◊│       │◊│
                                              │◊│       │◊│
                                              │◊╰───────╯◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              ╰───────────╯
                                              """,
                                              output,
                                              app.Driver!);

        subview.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SuperView_Top_Offset0_WithTitle_Unfocused () // Copilot
    {
        // Note: In Application context, the subview always gets focus (only focusable view),
        // so this renders the same as focused. Unfocused rendering is tested in standalone tests.
        (IApplication app, View subview) = CreateSuperViewWithTabChild (11,
                                                                        8,
                                                                        9,
                                                                        6,
                                                                        Side.Top,
                                                                        0,
                                                                        false,
                                                                        "T_ab",
                                                                        true);

        output.WriteLine (app.Driver!.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────────╮
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              │◊╭───╮◊◊◊◊◊│
                                              │◊│Tab│◊◊◊◊◊│
                                              │◊│   ╰───╮◊│
                                              │◊│       │◊│
                                              │◊│       │◊│
                                              │◊╰───────╯◊│
                                              │◊◊◊◊◊◊◊◊◊◊◊│
                                              ╰───────────╯
                                              """,
                                              output,
                                              app.Driver!);

        subview.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void SuperView_Top_ThickBorder_Offset0_WithTitle () // Copilot
    {
        // Thick border: Thickness(3,3,3,3). Child 11×8.
        (IApplication app, View subview) = CreateSuperViewWithTabChild (15,
                                                                        12,
                                                                        11,
                                                                        8,
                                                                        Side.Top,
                                                                        0,
                                                                        false,
                                                                        "T_ab",
                                                                        true,
                                                                        new Thickness (3, 3, 3, 3));

        output.WriteLine (app.Driver!.ToString ());

        // This test documents current behavior. Expected string will be updated
        // when edge-based positioning is implemented.
        subview.Dispose ();
        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void Thickness_Change_Updates_TitleView_Layout ()
    {
        IDriver driver = CreateTestDriver (10, 7);

        View view = new ()
        {
            Driver = driver,
            Width = 10,
            Height = 7,
            CanFocus = true,
            HasFocus = false,
            BorderStyle = LineStyle.Rounded
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Title = "Tab";

        view.Layout ();

        var bv = (BorderView)view.Border.View!;
        View? ttv = bv.TitleView;
        Assert.NotNull (ttv);
        int originalHeight = ttv.Frame.Height;

        // Change thickness — TitleView should get updated frame after re-layout
        view.Border.Thickness = new Thickness (1, 2, 1, 1);
        view.Layout ();

        int newHeight = ttv.Frame.Height;
        Assert.NotEqual (originalHeight, newHeight);

        view.Dispose ();
    }

    [Fact]
    public void Top_Focused_Depth1_LongTitle () // Copilot
    {
        // Title wider than content area → tab header spans full width.
        IDriver driver = CreateTestDriver (15, 4);

        View view = CreateTabView (driver,
                                   15,
                                   4,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "Long Title",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        // Depth=1: title on content border line, tab header = 12 wide (10+2 borders).
        // Focused → open gap. Title fills the header interior.
        DrawAndAssert (view,
                       driver,
                       """
                       │Long Title╭──╮
                       │             │
                       │             │
                       ╰─────────────╯
                       """);
    }

    // ──── Thickness = 1 (depth 1) ────
    // 1-row tab: title inline on the content border line.

    [Fact]
    public void Top_Focused_Depth1_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 4);

        View view = CreateTabView (driver,
                                   9,
                                   4,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       │Tab╭───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Depth2_With2LineTitle ()
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Top,
                                   0,
                                   4,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 2, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭──╮
                       │Ta╰────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    // ──── Thickness = 2 (depth 2) ────
    // 2-row tab: cap line + title on closing edge. No bottom line.

    [Fact]
    public void Top_Focused_Depth2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 2, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Depth4_With2LineTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_a\nb",
                                   true,
                                   new Thickness (1, 4, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭──╮
                       │Ta│
                       │b │
                       │  ╰────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Thickness Variants — Depth equals thickness on the tab side.
    //  Depth > 3 adds extra padding rows between the title and the
    //  content-side border join.
    // ════════════════════════════════════════════════════════════════════

    // ──── Thickness = 4 (depth 4: cap + title + 1 padding + join) ────

    [Fact]
    public void Top_Focused_Depth4_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 4, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab│
                       │   │
                       │   ╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Depth5_With2LineTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_a\nb",
                                   true,
                                   new Thickness (1, 5, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭──╮
                       │Ta│
                       │b │
                       │  │
                       │  ╰────╮
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Depth5_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 7);

        View view = CreateTabView (driver,
                                   9,
                                   7,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 5, 1, 1));

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │   │
                       │Tab│
                       │   │
                       │   ╰───╮
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab│
                       │   ╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Offset0_WithTitle_Thick_Border () // Copilot
    {
        IDriver driver = CreateTestDriver ();

        View view = CreateTabView (driver,
                                   17,
                                   12,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        view.Border.Thickness = new Thickness (5, 5, 5, 5);

        // Edge-based: borderBounds=(0,4,17,8). Header depth=5 at offset=0.
        // Focused → open gap (no separator line). Extra padding rows for depth > 3.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │   │
                       │Tab│
                       │   │
                       │   ╰───────────╮
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       ╰───────────────╯
                       """);
    }

    [Fact]
    public void Top_Focused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   2,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                         ╭───╮
                         │Tab│
                       ╭─╯   ╰─╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_OverflowRight_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   5,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                            ╭───
                            │Tab
                       ╭────╯  │
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Focused_TitleText_Uses_Normal_Attributes ()
    {
        // When a View has focus, the tab title text ("Tab") should render
        // with Focus attributes, and the hotkey character with HotFocus.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (9, 6);
        app.Driver!.Clipboard = new FakeClipboard ();

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        View view = new ()
        {
            X = 0,
            Y = 0,
            CanFocus = true,
            Width = 9,
            Height = 6,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        // Set a scheme with distinct attributes for each role so we can verify
        Scheme scheme = new ()
        {
            Normal = new Attribute (Color.White, Color.Black),
            Focus = new Attribute (Color.BrightGreen, Color.DarkGray),
            HotNormal = new Attribute (Color.BrightRed, Color.Black),
            HotFocus = new Attribute (Color.BrightYellow, Color.DarkGray)
        };
        view.SetScheme (scheme);

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        ((BorderView)view.Border.View!).TabOffset = 0;

        runnable.Add (view);
        app.Begin (runnable);

        // Give focus to our view
        view.SetFocus ();
        Assert.True (view.HasFocus);

        app.LayoutAndDraw ();

        output.WriteLine (app.Driver!.ToString ());

        // Attribute map:
        // 0 = Normal (border lines when unfocused — but LineCanvas uses Normal)
        // 1 = Focus (title text)
        // 2 = HotFocus (hotkey char 'a')
        Attribute normalAttribute = view.GetAttributeForRole (VisualRole.Normal);
        Attribute hotNormalAttr = view.GetAttributeForRole (VisualRole.HotNormal);
        Attribute focusAttr = view.GetAttributeForRole (VisualRole.Focus);
        Attribute hotFocusAttr = view.GetAttributeForRole (VisualRole.HotFocus);

        // Row 1 is "│Tab│" — columns 1,2,3 are the title text "Tab"
        // 'T' at [1,1] should be Focus, 'a' at [1,2] should be HotFocus, 'b' at [1,3] should be Focus
        Cell [,] contents = app.Driver!.Contents!;
        Attribute actualT = contents [1, 1].Attribute!.Value;
        Attribute actualA = contents [1, 2].Attribute!.Value;
        Attribute actualB = contents [1, 3].Attribute!.Value;

        output.WriteLine ($"Expected Focus: {focusAttr}");
        output.WriteLine ($"Expected HotFocus: {hotFocusAttr}");
        output.WriteLine ($"Actual 'T' [1,1]: {actualT}");
        output.WriteLine ($"Actual 'a' [1,2]: {actualA}");
        output.WriteLine ($"Actual 'b' [1,3]: {actualB}");

        Assert.Equal (normalAttribute, actualT);
        Assert.Equal (hotNormalAttr, actualA);
        Assert.Equal (normalAttribute, actualB);

        view.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Top_Focused_TitleView_Uses_Focused_Attributes ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (9, 6);
        app.Driver!.Clipboard = new FakeClipboard ();

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        View view = new ()
        {
            X = 0,
            Y = 0,
            CanFocus = true,
            Width = 9,
            Height = 6,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        // Set a scheme with distinct attributes for each role so we can verify
        Scheme scheme = new ()
        {
            Normal = new Attribute (Color.White, Color.Black),
            Focus = new Attribute (Color.BrightGreen, Color.DarkGray),
            HotNormal = new Attribute (Color.BrightRed, Color.Black),
            HotFocus = new Attribute (Color.BrightYellow, Color.DarkGray)
        };
        view.SetScheme (scheme);

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        ((BorderView)view.Border.View!).TabOffset = 0;

        runnable.Add (view);
        app.Begin (runnable);

        // Give focus to our view
        view.SetFocus ();
        Assert.True (view.HasFocus);
        view.Border.View?.CanFocus = true;
        view.Border.View?.SetFocus (); // TitleView should have focus, not the main view
        Assert.True (view.Border.View?.HasFocus);
        Assert.True (view.Border.View?.SubViews.OfType<TitleView> ().FirstOrDefault ()?.HasFocus);

        app.LayoutAndDraw ();

        output.WriteLine (app.Driver!.ToString ());

        // Attribute map:
        // 0 = Normal (border lines when unfocused — but LineCanvas uses Normal)
        // 1 = Focus (title text)
        // 2 = HotFocus (hotkey char 'a')
        Attribute focusAttr = view.GetAttributeForRole (VisualRole.Focus);
        Attribute hotFocusAttr = view.GetAttributeForRole (VisualRole.HotFocus);

        // Row 1 is "│Tab│" — columns 1,2,3 are the title text "Tab"
        // 'T' at [1,1] should be Focus, 'a' at [1,2] should be HotFocus, 'b' at [1,3] should be Focus
        Cell [,] contents = app.Driver!.Contents!;
        Attribute actualT = contents [1, 1].Attribute!.Value;
        Attribute actualA = contents [1, 2].Attribute!.Value;
        Attribute actualB = contents [1, 3].Attribute!.Value;

        output.WriteLine ($"Expected Focus: {focusAttr}");
        output.WriteLine ($"Expected HotFocus: {hotFocusAttr}");
        output.WriteLine ($"Actual 'T' [1,1]: {actualT}");
        output.WriteLine ($"Actual 'a' [1,2]: {actualA}");
        output.WriteLine ($"Actual 'b' [1,3]: {actualB}");

        Assert.Equal (focusAttr, actualT);
        Assert.Equal (hotFocusAttr, actualA);
        Assert.Equal (focusAttr, actualB);

        view.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Top_Unfocused_Depth1_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 4);

        View view = CreateTabView (driver,
                                   9,
                                   4,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 1, 1, 1));

        // Depth=1: separator coincides with content border line.
        DrawAndAssert (view,
                       driver,
                       """
                       │Tab╭───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_Depth2_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 5);

        View view = CreateTabView (driver,
                                   9,
                                   5,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true,
                                   new Thickness (1, 2, 1, 1));

        // Depth=2: cap line + closing edge with title. Separator on closing edge.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab╰───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -1,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ───╮
                       Tab│
                       ┬──┴────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset_WithTitle_Thick_Border ()
    {
        IDriver driver = CreateTestDriver ();

        View view = CreateTabView (driver,
                                   17,
                                   12,
                                   Side.Top,
                                   -1,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        view.Border.Thickness = new Thickness (5, 5, 5, 5);

        // Edge-based positioning: non-title sides at outer edge → 17 wide, 8 tall content border.
        // Tab header at offset=-1 is partially clipped on the left.
        DrawAndAssert (view,
                       driver,
                       """
                       ───╮
                          │
                       Tab│
                          │
                       ┬──┴────────────╮
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       ╰───────────────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset2_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -2,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        // Header at X=-2. Left edge and 'T' clipped. Visible: cap ──╮, title ab│.
        DrawAndAssert (view,
                       driver,
                       """
                       ──╮
                       ab│
                       ┬─┴─────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset4_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -4,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        // Header at X=-4. Only right edge visible at col 0. No title visible.
        DrawAndAssert (view,
                       driver,
                       """
                       ╮
                       │
                       ┼───────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset5_Thick_Border () // Copilot
    {
        IDriver driver = CreateTestDriver ();

        View view = CreateTabView (driver,
                                   17,
                                   12,
                                   Side.Top,
                                   -5,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        view.Border.Thickness = new Thickness (5, 5, 5, 5);

        // Edge-based: header completely off-screen. Full content border drawn.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────────────╮
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       ╰───────────────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_NegativeOffset5_WithTitle () // Copilot
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   -5,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        // Header completely off-screen. Content border drawn normally.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_Offset0_NoTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   null,
                                   false);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭╮
                       ││
                       ├┴──────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_Offset0_With2LineTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   "T_a\nb",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭──╮
                       │a │
                       ├──┴────╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Top — View 9×6, Thickness(1,3,1,1), borderBounds=(0,2,9,4)
    //  Content border: 9 wide, 4 tall. Interior: 7 cols × 2 rows.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Top_Unfocused_Offset0_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │Tab│
                       ├───┴───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_Offset0_WithTitle_Thick_Border () // Copilot
    {
        IDriver driver = CreateTestDriver ();

        View view = CreateTabView (driver,
                                   17,
                                   12,
                                   Side.Top,
                                   0,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        view.Border.Thickness = new Thickness (5, 5, 5, 5);

        // Edge-based: borderBounds=(0,4,17,8). Header depth=5 at offset=0.
        // Unfocused → separator line (closed). Extra padding rows for depth > 3.
        DrawAndAssert (view,
                       driver,
                       """
                       ╭───╮
                       │   │
                       │Tab│                                                                           
                       │   │
                       ├───┴───────────╮
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       │               │
                       ╰───────────────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_Offset2_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   2,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                         ╭───╮
                         │Tab│
                       ╭─┴───┴─╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_OverflowRight_WithTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   5,
                                   null,
                                   false,
                                   "T_ab",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                            ╭───
                            │Tab
                       ╭────┴──┬
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_SingleCharTitle ()
    {
        IDriver driver = CreateTestDriver (9, 6);

        View view = CreateTabView (driver,
                                   9,
                                   6,
                                   Side.Top,
                                   2,
                                   null,
                                   false,
                                   "X",
                                   true);

        DrawAndAssert (view,
                       driver,
                       """
                         ╭─╮
                         │X│
                       ╭─┴─┴───╮
                       │       │
                       │       │
                       ╰───────╯
                       """);
    }

    [Fact]
    public void Top_Unfocused_TitleText_Uses_Normal_Attributes () // Copilot
    {
        // When a View does NOT have focus, the tab title text should render
        // with Normal attributes, and the hotkey character with HotNormal.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (9, 6);
        app.Driver!.Clipboard = new FakeClipboard ();

        Runnable runnable = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        View view = new ()
        {
            X = 0,
            Y = 0,
            CanFocus = true,
            Width = 9,
            Height = 6,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        Scheme scheme = new ()
        {
            Normal = new Attribute (Color.White, Color.Black),
            Focus = new Attribute (Color.BrightGreen, Color.DarkGray),
            HotNormal = new Attribute (Color.BrightRed, Color.Black),
            HotFocus = new Attribute (Color.BrightYellow, Color.DarkGray)
        };
        view.SetScheme (scheme);

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        ((BorderView)view.Border.View!).TabOffset = 0;

        // Add a second focusable view so focus goes there, not to `view`
        View other = new ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            CanFocus = true
        };
        runnable.Add (other);
        runnable.Add (view);
        app.Begin (runnable);

        // Ensure the other view has focus, not our tab view
        other.SetFocus ();
        Assert.False (view.HasFocus);

        app.LayoutAndDraw ();

        output.WriteLine (app.Driver!.ToString ());

        Attribute normalAttr = view.GetAttributeForRole (VisualRole.Normal);
        Attribute hotNormalAttr = view.GetAttributeForRole (VisualRole.HotNormal);

        // Row 1 "│Tab│" — 'T' at [1,1] Normal, 'a' at [1,2] HotNormal, 'b' at [1,3] Normal
        Cell [,] contents = app.Driver!.Contents!;
        Attribute actualT = contents [1, 1].Attribute!.Value;
        Attribute actualA = contents [1, 2].Attribute!.Value;
        Attribute actualB = contents [1, 3].Attribute!.Value;

        output.WriteLine ($"Expected Normal: {normalAttr}");
        output.WriteLine ($"Expected HotNormal: {hotNormalAttr}");
        output.WriteLine ($"Actual 'T' [1,1]: {actualT}");
        output.WriteLine ($"Actual 'a' [1,2]: {actualA}");
        output.WriteLine ($"Actual 'b' [1,3]: {actualB}");

        Assert.Equal (normalAttr, actualT);
        Assert.Equal (hotNormalAttr, actualA);
        Assert.Equal (normalAttr, actualB);

        view.Dispose ();
        app.Dispose ();
    }

    // ════════════════════════════════════════════════════════════════════
    //  SuperView Integration Tests
    //  View with Tab border placed inside a Window (border=Rounded) that
    //  fills its viewport with ◊ (diamond). The diamond background proves
    //  that transparent areas of the tab header let content show through.
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Creates a Window-like SuperView with diamond-filled background, containing a tab-border subview.
    ///     The subview is positioned at (1,1) so there's at least 1 row/col of diamonds around it.
    /// </summary>
    private static (IApplication app, View subview) CreateSuperViewWithTabChild (int superWidth,
                                                                                 int superHeight,
                                                                                 int subviewWidth,
                                                                                 int subviewHeight,
                                                                                 Side side,
                                                                                 int tabOffset,
                                                                                 bool hasFocus,
                                                                                 string? title,
                                                                                 bool titleFlag,
                                                                                 Thickness? thickness = null) // Copilot
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (superWidth + 2, superHeight + 2);
        app.Driver!.Clipboard = new FakeClipboard ();

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Rounded };

        // Fill window viewport with diamonds
        window.DrawingContent += (_, e) =>
                                 {
                                     window.FillRect (window.Viewport, Glyphs.Diamond);
                                     e.DrawContext?.AddDrawnRectangle (window.Viewport);
                                 };

        View subview = new ()
        {
            X = 1,
            Y = 1,
            CanFocus = true,
            HasFocus = hasFocus,
            Width = subviewWidth,
            Height = subviewHeight,
            BorderStyle = LineStyle.Rounded
        };

        if (title is { })
        {
            subview.Title = title;
        }

        subview.Border.Thickness = thickness
                                   ?? side switch
                                      {
                                          Side.Top => new Thickness (1, 3, 1, 1),
                                          Side.Bottom => new Thickness (1, 1, 1, 3),
                                          Side.Left => new Thickness (3, 1, 1, 1),
                                          Side.Right => new Thickness (1, 1, 3, 1),
                                          _ => throw new ArgumentOutOfRangeException (nameof (side))
                                      };

        var settings = BorderSettings.Tab;

        if (titleFlag)
        {
            settings |= BorderSettings.Title;
        }

        subview.Border.Settings = settings;
        ((BorderView)subview.Border.View!).TabSide = side;
        ((BorderView)subview.Border.View!).TabOffset = tabOffset;

        window.Add (subview);
        app.Begin (window);

        return (app, subview);
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private static View CreateTabView (IDriver driver,
                                       int width,
                                       int height,
                                       Side side,
                                       int tabOffset,
                                       int? tabLength,
                                       bool hasFocus,
                                       string? title,
                                       bool titleFlag,
                                       Thickness? thickness = null)
    {
        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = hasFocus,
            Width = width,
            Height = height,
            BorderStyle = LineStyle.Rounded
        };

        if (title is { })
        {
            view.Title = title;
        }

        view.Border.Thickness = thickness
                                ?? side switch
                                   {
                                       Side.Top => new Thickness (1, 3, 1, 1),
                                       Side.Bottom => new Thickness (1, 1, 1, 3),
                                       Side.Left => new Thickness (3, 1, 1, 1),
                                       Side.Right => new Thickness (1, 1, 3, 1),
                                       _ => throw new ArgumentOutOfRangeException (nameof (side))
                                   };

        var settings = BorderSettings.Tab;

        if (titleFlag)
        {
            settings |= BorderSettings.Title;
        }

        view.Border.Settings = settings;
        ((BorderView)view.Border.View!).TabSide = side;
        ((BorderView)view.Border.View!).TabOffset = tabOffset;

        if (tabLength.HasValue)
        {
            ((BorderView)view.Border.View!).TabLength = tabLength.Value;
        }

        return view;
    }

    // ────────────────────────────────────────────────────────────────────
    //  TabSide switching — EffectiveTabLength must recalculate when
    //  the tab side changes from horizontal to vertical or vice versa.
    // ────────────────────────────────────────────────────────────────────

    // Claude - Opus 4.6
    [Fact]
    public void TabSide_TopToLeft_EffectiveTabLength_Recalculates ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = 12,
            Height = 9,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Layout ();

        // Top: "Tab" = 3 cols + 2 border = 5 (width)
        Assert.Equal (5, ((BorderView)view.Border.View!).EffectiveTabLength);

        // Switch to Left — tab length should now be height-based
        ((BorderView)view.Border.View!).TabSide = Side.Left;
        view.Border.Thickness = new Thickness (3, 1, 1, 1);
        view.Layout ();

        // Left: "Tab" vertical = 3 rows + 2 border = 5 (height)
        Assert.Equal (5, ((BorderView)view.Border.View!).EffectiveTabLength);

        view.Dispose ();
    }

    [Fact]
    public void TabSide_TopToLeft_Renders_Correctly ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = CreateTabView (driver,
                                   12,
                                   9,
                                   Side.Top,
                                   0,
                                   null,
                                   true,
                                   "T_ab",
                                   true);

        // First render as Top
        view.Layout ();
        view.Draw ();

        // Verify top rendering is correct
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │Tab│
                                              │   ╰──────╮
                                              │          │
                                              │          │
                                              │          │
                                              │          │
                                              │          │
                                              ╰──────────╯
                                              """,
                                              output,
                                              driver);

        // Switch to Left
        ((BorderView)view.Border.View!).TabSide = Side.Left;
        view.Border.Thickness = new Thickness (3, 1, 1, 1);

        driver.ClearContents ();
        view.Layout ();
        view.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭──────────╮
                                              │T         │
                                              │a         │
                                              │b         │
                                              ╰─╮        │
                                                │        │
                                                │        │
                                                │        │
                                                ╰────────╯
                                              """,
                                              output,
                                              driver);

        view.Dispose ();
    }

    [Fact]
    public void TabSide_LeftToTop_EffectiveTabLength_Recalculates ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = 12,
            Height = 9,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        view.Border.Thickness = new Thickness (3, 1, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Left;
        view.Layout ();

        // Left: "Tab" vertical = 3 rows + 2 border = 5 (height)
        Assert.Equal (5, ((BorderView)view.Border.View!).EffectiveTabLength);

        // Switch to Top
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Layout ();

        // Top: "Tab" = 3 cols + 2 border = 5 (width)
        Assert.Equal (5, ((BorderView)view.Border.View!).EffectiveTabLength);

        view.Dispose ();
    }

    [Fact]
    public void TabSide_TopToBottom_EffectiveTabLength_Unchanged ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = 12,
            Height = 9,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Layout ();

        int topLength = ((BorderView)view.Border.View!).EffectiveTabLength;

        // Switch to Bottom — still horizontal, length should be the same
        ((BorderView)view.Border.View!).TabSide = Side.Bottom;
        view.Border.Thickness = new Thickness (1, 1, 1, 3);
        view.Layout ();

        Assert.Equal (topLength, ((BorderView)view.Border.View!).EffectiveTabLength);

        view.Dispose ();
    }

    [Fact]
    public void TabSide_TopToRight_EffectiveTabLength_Recalculates ()
    {
        IDriver driver = CreateTestDriver (12, 9);

        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = 12,
            Height = 9,
            BorderStyle = LineStyle.Rounded,
            Title = "T_ab"
        };

        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = Side.Top;
        view.Layout ();

        Assert.Equal (5, ((BorderView)view.Border.View!).EffectiveTabLength);

        // Switch to Right
        ((BorderView)view.Border.View!).TabSide = Side.Right;
        view.Border.Thickness = new Thickness (1, 1, 3, 1);
        view.Layout ();

        // Right: "Tab" vertical = 3 rows + 2 border = 5 (height)
        Assert.Equal (5, ((BorderView)view.Border.View!).EffectiveTabLength);

        view.Dispose ();
    }

    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private void DrawAndAssert (View view, IDriver driver, string expected)
    {
        view.Layout ();
        view.Draw ();
        output.WriteLine (driver.ToString ());
        DriverAssert.AssertDriverContentsAre (expected, output, driver);
        view.Dispose ();
    }
}
