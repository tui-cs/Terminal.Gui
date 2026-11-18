using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests that multiple Views with tab-style borders compose correctly when sharing
///     a parent's LineCanvas via <see cref="View.SuperViewRendersLineCanvas"/>.
/// </summary>

// Copilot
public class TabCompositionTests (ITestOutputHelper output) : TestDriverBase
{
    // ────────────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────────────

    private static View CreateTabView (IDriver driver, Side side, int tabOffset, bool hasFocus, string title)
    {
        View view = new ()
        {
            Driver = driver,
            CanFocus = true,
            HasFocus = hasFocus,
            SuperViewRendersLineCanvas = true,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            BorderStyle = LineStyle.Rounded,
            Title = title,
            Text = $"{title} content",
            Arrangement = ViewArrangement.Overlapped
        };

        view.Border.Thickness = side switch
                                {
                                    Side.Top => new Thickness (1, 3, 1, 1),
                                    Side.Bottom => new Thickness (1, 1, 1, 3),
                                    Side.Left => new Thickness (3, 1, 1, 1),
                                    Side.Right => new Thickness (1, 1, 3, 1),
                                    _ => throw new ArgumentOutOfRangeException (nameof (side))
                                };

        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        ((BorderView)view.Border.View!).TabSide = side;
        ((BorderView)view.Border.View!).TabOffset = tabOffset;

        if (side is Side.Left or Side.Right)
        {
            view.Height = view.Text.GetColumns () + 2; // +2 for top and bottom border thickness
        }

        view.Layout ();
        driver.SetScreenSize (view.Frame.Width, view.Frame.Height);

        return view;
    }

    private void DrawAndAssert (View view, IDriver driver, string expected)
    {
        view.Layout ();
        view.Draw ();
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output, driver);
        view.Dispose ();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Top — Two tabs, Tab1 focused
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Top_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver ();

        // Tab1: focused, offset 0. Title "Tab1" → TabLength = 6.
        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");

        // Tab2: unfocused, offset 6 (right after Tab1). Title "Tab2" → TabLength = 6.
        View tab2 = CreateTabView (driver, Side.Top, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();
        Assert.Equal (tab1, superView.SubViews.ElementAt (1));

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╮────╮∙∙∙
                       │Tab1│Tab2│∙∙∙
                       │    ╰────┴──╮
                       │Tab1 content│
                       ╰────────────╯
                       """);
    }

    [Fact]
    public void Top_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver ();

        // Tab1: focused, offset 0. Title "Tab1" → TabLength = 6.
        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");

        // Tab2: unfocused, offset 6 (right after Tab1). Title "Tab2" → TabLength = 6.
        View tab2 = CreateTabView (driver, Side.Top, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╭────╮∙∙∙
                       │Tab1│Tab2│∙∙∙
                       ├────╯    ╰──╮
                       │Tab2 content│
                       ╰────────────╯
                       """);
    }

    [Fact]
    public void Top_ThreeTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver ();

        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Top, 5, true, "Tab2");
        View tab3 = CreateTabView (driver, Side.Top, 10, false, "Tab3");

        tab1.Width = tab2.Width = tab3.Width = 20;
        tab3.Layout ();
        driver.SetScreenSize (tab3.Frame.Width, tab3.Frame.Height);
        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab3, tab2, tab1);

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╮────╮────╮∙∙∙∙
                       │Tab1│Tab2│Tab3│∙∙∙∙
                       │    ╰────┴────┴───╮
                       │Tab1 content      │
                       ╰──────────────────╯
                       """);
    }

    [Fact]
    public void Top_ThreeTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver ();

        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Top, 5, true, "Tab2");
        View tab3 = CreateTabView (driver, Side.Top, 10, false, "Tab3");

        tab1.Width = tab2.Width = tab3.Width = 20;
        tab3.Layout ();
        driver.SetScreenSize (tab3.Frame.Width, tab3.Frame.Height);
        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab3, tab2, tab1);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╭────╮────╮∙∙∙∙
                       │Tab1│Tab2│Tab3│∙∙∙∙
                       ├────╯    ╰────┴───╮
                       │Tab2 content      │
                       ╰──────────────────╯
                       """);
    }

    [Fact]
    public void Top_ThreeTabs_Tab3Focused ()
    {
        IDriver driver = CreateTestDriver ();

        View tab1 = CreateTabView (driver, Side.Top, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Top, 5, true, "Tab2");
        View tab3 = CreateTabView (driver, Side.Top, 10, false, "Tab3");

        tab1.Width = tab2.Width = tab3.Width = 20;
        tab3.Layout ();
        driver.SetScreenSize (tab3.Frame.Width, tab3.Frame.Height);
        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab3, tab2, tab1);
        tab1.SetFocus ();
        tab2.SetFocus ();
        tab3.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────╭────╭────╮∙∙∙∙
                       │Tab1│Tab2│Tab3│∙∙∙∙
                       ├────┴────╯    ╰───╮
                       │Tab3 content      │
                       ╰──────────────────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Bottom — Two tabs
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Bottom_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver ();

        View tab1 = CreateTabView (driver, Side.Bottom, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Bottom, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────────────╮
                       │Tab1 content│
                       │    ╭────┬──╯
                       │Tab1│Tab2│∙∙∙
                       ╰────╯────╯∙∙∙
                       """);
    }

    [Fact]
    public void Bottom_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver ();

        View tab1 = CreateTabView (driver, Side.Bottom, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Bottom, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────────────╮
                       │Tab2 content│
                       ├────╮    ╭──╯
                       │Tab1│Tab2│∙∙∙
                       ╰────╰────╯∙∙∙
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Left — Two tabs
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Left_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (20, 12);

        View tab1 = CreateTabView (driver, Side.Left, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Left, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────────╮
                       │T Tab1 content│
                       │a             │
                       │b             │
                       │1             │
                       ╰─╮            │
                       │T│            │
                       │a│            │
                       │b│            │
                       │2│            │
                       ╰─┤            │
                       ∙∙│            │
                       ∙∙│            │
                       ∙∙╰────────────╯
                       """);
    }

    [Fact]
    public void Left_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (20, 12);

        View tab1 = CreateTabView (driver, Side.Left, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Left, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭─┬────────────╮
                       │T│Tab2 content│
                       │a│            │
                       │b│            │
                       │1│            │
                       ╭─╯            │
                       │T             │
                       │a             │
                       │b             │
                       │2             │
                       ╰─╮            │
                       ∙∙│            │
                       ∙∙│            │
                       ∙∙╰────────────╯
                       """);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Side.Right — Two tabs
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Right_TwoTabs_Tab1Focused ()
    {
        IDriver driver = CreateTestDriver (20, 12);

        View tab1 = CreateTabView (driver, Side.Right, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Right, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab1.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭──────────────╮
                       │Tab1 content T│
                       │             a│
                       │             b│
                       │             1│
                       │            ╭─╯
                       │            │T│
                       │            │a│
                       │            │b│
                       │            │2│
                       │            ├─╯
                       │            │∙∙
                       │            │∙∙
                       ╰────────────╯∙∙
                       """);
    }

    [Fact]
    public void Right_TwoTabs_Tab2Focused ()
    {
        IDriver driver = CreateTestDriver (20, 12);

        View tab1 = CreateTabView (driver, Side.Right, 0, false, "Tab1");
        View tab2 = CreateTabView (driver, Side.Right, 5, false, "Tab2");

        View superView = new () { CanFocus = true, Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };

        superView.DrawingContent += (_, e) =>
                                    {
                                        superView.FillRect (superView.Viewport, Glyphs.Dot);
                                        e.Cancel = true;
                                    };
        superView.Add (tab1, tab2);
        tab2.SetFocus ();

        DrawAndAssert (superView,
                       driver,
                       """
                       ╭────────────┬─╮
                       │Tab2 content│T│
                       │            │a│
                       │            │b│
                       │            │1│
                       │            ╰─╮
                       │             T│
                       │             a│
                       │             b│
                       │             2│
                       │            ╭─╯
                       │            │∙∙
                       │            │∙∙
                       ╰────────────╯∙∙
                       """);
    }
}
