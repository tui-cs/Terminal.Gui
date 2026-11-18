using UnitTests;

namespace ViewsTests;

// Claude - Opus 4.6

/// <summary>
///     Tests for the <see cref="Tabs"/> class.
/// </summary>
public class TabsTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Add_View_ConfiguresAsTabs ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.True (tab1.CanFocus);
        Assert.Equal (TabBehavior.TabStop, tab1.TabStop);
        Assert.Equal (BorderSettings.Tab | BorderSettings.Title, tab1.Border.Settings);
        Assert.Equal (ViewArrangement.Overlapped, tab1.Arrangement);
        Assert.True (tab1.SuperViewRendersLineCanvas);
    }

    [Fact]
    public void Add_View_Value_Is_First_Added ()
    {
        Tabs tabs = new ();

        View tab1 = new () { Title = "Tab1" };
        tabs.Add (tab1);
        Assert.True (tab1.HasFocus);
        Assert.Same (tab1, tabs.Value);

        View tab2 = new () { Title = "Tab2" };
        tabs.Add (tab2);
        Assert.True (tab1.HasFocus);
        Assert.Same (tab1, tabs.Value);
    }

    [Fact]
    public void Add_View_Value_Is_First_Added_SuperView ()
    {
        View superView = new () { CanFocus = true };
        Tabs tabs = new ();
        superView.Add (tabs);

        View tab1 = new () { Title = "Tab1" };
        tabs.Add (tab1);
        Assert.True (tab1.HasFocus);
        Assert.Same (tab1, tabs.Value);

        View tab2 = new () { Title = "Tab2" };
        tabs.Add (tab2);
        superView.Layout ();

        Assert.True (tab1.HasFocus);
        Assert.Same (tab1, tabs.Value);
    }

    [Fact]
    public void Add_View_SetsBorderThickness_Bottom ()
    {
        Tabs tabs = new () { TabSide = Side.Bottom };
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (1, 1, 1, 3), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_View_SetsBorderThickness_Left ()
    {
        Tabs tabs = new () { TabSide = Side.Left };
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (3, 1, 1, 1), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_View_SetsBorderThickness_Right ()
    {
        Tabs tabs = new () { TabSide = Side.Right };
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (1, 1, 3, 1), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_View_SetsBorderThickness_Top ()
    {
        Tabs tabs = new () { TabSide = Side.Top };
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (new Thickness (1, 3, 1, 1), tab1.Border.Thickness);
    }

    [Fact]
    public void Add_View_SetsLineStyle ()
    {
        Tabs tabs = new () { TabLineStyle = LineStyle.Single };
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (LineStyle.Single, tab1.BorderStyle);
    }

    [Fact]
    public void Add_View_SetsTabSide ()
    {
        Tabs tabs = new () { TabSide = Side.Bottom };
        View tab1 = new () { Title = "Tab1" };

        tabs.Add (tab1);

        Assert.Equal (Side.Bottom, ((BorderView)tab1.Border.View!).TabSide);
    }

    [Fact]
    public void Constructor_SetsExpectedDefaults ()
    {
        Tabs tabs = new ();

        Assert.True (tabs.CanFocus);
        Assert.Equal (Side.Top, tabs.TabSide);
        Assert.Equal (LineStyle.Rounded, tabs.TabLineStyle);
        Assert.Null (tabs.Value);
    }

    [Fact]
    public void EnableForDesign_CreatesTabs ()
    {
        Tabs tabs = new ();
        bool result = ((IDesignable)tabs).EnableForDesign ();

        Assert.True (result);
        Assert.Equal (4, tabs.TabCollection.Count ());
        Assert.NotNull (tabs.Value);
    }

    [Fact]
    public void EnableForDesign_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (46, 22);

        View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        Tabs tabs = new ();
        superView.Add (tabs);

        tabs.EnableForDesign ();

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─────────╮──────────╮────────────╮──────────╮
                                              │Attribute│Line Style│Tab Settings│Add/Remove│
                                              │         ╰──────────┴────────────┴──────────┤
                                              │                                            │
                                              │┌───────────────────────────────────────────│
                                              │├┤Style├────────┐                           │
                                              ││☐ Bold         │                           │
                                              ││☐ Faint        │                           │
                                              ││☐ Italic       │                           │
                                              ││☐ Underline    │                           │
                                              ││☐ Blink        │                           │
                                              ││☐ Reverse      │                           │
                                              ││☐ Strikethrough│                           │
                                              ││               │                           │
                                              ││               │                           │
                                              ││               │                           │
                                              ││               │                           │
                                              │├───────────────┘                           │
                                              ││                         Sample Text       │
                                              │└───────────────────────────────────────────│
                                              │                                            │
                                              ╰────────────────────────────────────────────╯
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void App_EnableForDesign_DrawsCorrectly ()
    {
        IApplication? app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        IDriver? driver = app.Driver;
        Runnable runnable = new ();

        Tabs tabs = new ();
        tabs.EnableForDesign ();

        runnable.Add (tabs);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─────────╮──────────╮────────────╮──────────╮
                                              │Attribute│Line Style│Tab Settings│Add/Remove│
                                              │         ╰──────────┴────────────┴──────────┴─────────────────────────────────╮
                                              │                                                                              │
                                              │┌────────────────────────────────────────────────────────────────────────────┐│
                                              ││┌┤Foreground├─────────────────────────────────────────────┬┤Style├────────┐ ││
                                              │││H:▲                                                  0   │☐ Bold         │ ││
                                              │││S:▲                                                  0   │☐ Faint        │ ││
                                              │││V:                                                  ▲100 │☐ Italic       │ ││
                                              │││Name: White              ▼                               │☐ Underline    │ ││
                                              │││Hex:#FFFFFF  ■                                           │☐ Blink        │ ││
                                              ││├┼Background┼─────────────────────────────────────────────┤☐ Reverse      │ ││
                                              │││H:▲                                                  0   │☐ Strikethrough│ ││
                                              │││S:▲                                                  0   │               │ ││
                                              │││V:▲                                                  0   │               │ ││
                                              │││Name: Black              ▼                               │               │ ││
                                              │││Hex:#000000  ■                                           │               │ ││
                                              ││└─────────────────────────────────────────────────────────┴───────────────┘ ││
                                              ││                                Sample Text                                 ││
                                              │└────────────────────────────────────────────────────────────────────────────┘│
                                              │                                                                              │
                                              │                                                                              │
                                              │                                                                              │
                                              │                                                                              │
                                              ╰──────────────────────────────────────────────────────────────────────────────╯
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void IndexOf_ReturnsCorrectIndex ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);

        Assert.Equal (0, tabs.IndexOf (tab1));
        Assert.Equal (1, tabs.IndexOf (tab2));
        Assert.Equal (2, tabs.IndexOf (tab3));
    }

    [Fact]
    public void IndexOf_ReturnsMinusOne_ForUnknownView ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View unknown = new () { Title = "Unknown" };

        tabs.Add (tab1);

        Assert.Equal (-1, tabs.IndexOf (unknown));
    }

    [Fact]
    public void Remove_View_UpdatesTabCollection ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Remove (tab2);

        List<View> ordered = tabs.TabCollection.ToList ();
        Assert.Equal (2, ordered.Count);
        Assert.Same (tab1, ordered [0]);
        Assert.Same (tab3, ordered [1]);

        Assert.Equal (0, tabs.IndexOf (tab1));
        Assert.Equal (1, tabs.IndexOf (tab3));
    }

    [Fact]
    public void TabCollection_ReturnsViewsInLogicalOrder ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);

        List<View> ordered = tabs.TabCollection.ToList ();
        Assert.Equal (3, ordered.Count);
        Assert.Same (tab1, ordered [0]);
        Assert.Same (tab2, ordered [1]);
        Assert.Same (tab3, ordered [2]);
    }

    // ChatGPT - GPT-5
    [Fact]
    public void GetSubViews_ReturnsLogicalOrder_AfterFocusedTabChangesDrawOrder ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Value = tab2;

        List<View> ordered = tabs.GetSubViews ().ToList ();

        Assert.Equal (3, ordered.Count);
        Assert.Same (tab1, ordered [0]);
        Assert.Same (tab2, ordered [1]);
        Assert.Same (tab3, ordered [2]);

        tabs.Dispose ();
    }

    // ChatGPT - GPT-5
    [Fact]
    public void GetSubViews_IncludesNonTabAdornmentSubViews_AfterLogicalTabs ()
    {
        Tabs tabs = new () { BorderStyle = LineStyle.Single };
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View borderSubView = new () { Id = "borderSubView", Width = 1, Height = 1 };

        tabs.Add (tab1, tab2);
        tabs.Border.View!.Add (borderSubView);

        List<View> ordered = tabs.GetSubViews (includeBorder: true).ToList ();

        Assert.Same (tab1, ordered [0]);
        Assert.Same (tab2, ordered [1]);
        Assert.Same (borderSubView, ordered [2]);

        tabs.Dispose ();
    }

    // ChatGPT - GPT-5
    [Fact]
    public void InsertTab_InvalidatesLogicalOrderCache ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab3);
        _ = tabs.TabCollection.ToList ();

        tabs.InsertTab (1, tab2);

        List<View> ordered = tabs.TabCollection.ToList ();

        Assert.Equal (0, tabs.IndexOf (tab1));
        Assert.Equal (1, tabs.IndexOf (tab2));
        Assert.Equal (2, tabs.IndexOf (tab3));
        Assert.Same (tab1, ordered [0]);
        Assert.Same (tab2, ordered [1]);
        Assert.Same (tab3, ordered [2]);

        tabs.Dispose ();
    }

    [Fact]
    public void TabLineStyle_Change_UpdatesAllTabs ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);

        tabs.TabLineStyle = LineStyle.Double;

        Assert.Equal (LineStyle.Double, tab1.BorderStyle);
        Assert.Equal (LineStyle.Double, tab2.BorderStyle);
    }

    // Claude - Opus 4.6
    [Fact]
    public void TabLineStyle_None_ThenBack_RestoresRendering ()
    {
        IDriver driver = CreateTestDriver (14, 5);

        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        View tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        View tab2 = new () { Title = "Tab2", Text = "Tab2 content" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        // Draw with default Rounded style and capture expected output
        tabs.Layout ();
        tabs.Draw ();

        // Verify initial thickness is correct for top tabs (1, TabDepth=3, 1, 1)
        Assert.Equal (new Thickness (1, 3, 1, 1), tab1.Border.Thickness);
        Assert.Equal (new Thickness (1, 3, 1, 1), tab2.Border.Thickness);

        // Change to None and back to Rounded
        tabs.TabLineStyle = LineStyle.None;
        tabs.TabLineStyle = LineStyle.Rounded;

        // Thickness must be restored to tab-specific values, not generic (1,1,1,1)
        Assert.Equal (new Thickness (1, 3, 1, 1), tab1.Border.Thickness);
        Assert.Equal (new Thickness (1, 3, 1, 1), tab2.Border.Thickness);

        // Re-draw and verify rendering matches original
        driver.ClearContents ();
        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────╮────╮
                                              │Tab1│Tab2│
                                              │    ╰────┴──╮
                                              │Tab1 content│
                                              ╰────────────╯
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void TabSide_Change_UpdatesAllTabs ()
    {
        Tabs tabs = new () { TabSide = Side.Top };
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);

        tabs.TabSide = Side.Bottom;

        Assert.Equal (Side.Bottom, ((BorderView)tab1.Border.View!).TabSide);
        Assert.Equal (Side.Bottom, ((BorderView)tab2.Border.View!).TabSide);
        Assert.Equal (new Thickness (1, 1, 1, 3), tab1.Border.Thickness);
        Assert.Equal (new Thickness (1, 1, 1, 3), tab2.Border.Thickness);
    }

    [Fact]
    public void Top_ThreeTabs_Tab2Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (20, 5);

        Tabs tabs = new () { Driver = driver, Width = 20, Height = 5 };

        View tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        View tab2 = new () { Title = "Tab2", Text = "Tab2 content" };
        View tab3 = new () { Title = "Tab3", Text = "Tab3 content" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Value = tab2;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("╭────╭────╮────╮    \r\n"
                                                       + "│Tab1│Tab2│Tab3│    \r\n"
                                                       + "├────╯    ╰────┴───╮\r\n"
                                                       + "│Tab2 content      │\r\n"
                                                       + "╰──────────────────╯",
                                                       output,
                                                       driver);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_TwoTabs_Tab1Focused_HotKey_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (14, 5);

        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        View tab1 = new () { Title = "Tab_1", Text = "Tab1 content" };
        View tab2 = new () { Title = "Tab _2", Text = "Tab2 content" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────╮─────╮
                                              │Tab1│Tab 2│
                                              │    ╰─────┴─╮
                                              │Tab1 content│
                                              ╰────────────╯
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_TwoTabs_Tab1Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (14, 5);

        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        View tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        View tab2 = new () { Title = "Tab2", Text = "Tab2 content" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────╮────╮
                                              │Tab1│Tab2│
                                              │    ╰────┴──╮
                                              │Tab1 content│
                                              ╰────────────╯
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void Top_TwoTabs_Tab2Focused_DrawsCorrectly ()
    {
        IDriver driver = CreateTestDriver (14, 5);

        Tabs tabs = new () { Driver = driver, Width = 14, Height = 5 };

        View tab1 = new () { Title = "Tab1", Text = "Tab1 content" };
        View tab2 = new () { Title = "Tab2", Text = "Tab2 content" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab2;

        tabs.Layout ();
        tabs.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭────╭────╮
                                              │Tab1│Tab2│
                                              ├────╯    ╰──╮
                                              │Tab2 content│
                                              ╰────────────╯
                                              """,
                                              output,
                                              driver);

        tabs.Dispose ();
    }

    [Fact]
    public void UpdateTabOffsets_ComputesCumulativeOffsets ()
    {
        Tabs tabs = new ();

        // "Tab1" title -> TabLength = 6 (4 chars + 2 border cells)
        View tab1 = new () { Title = "Tab1" };

        // "Tab2" title -> TabLength = 6
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);

        // Tab1 starts at 0
        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);

        // Tab2 starts at TabLength-1 = 5 (sharing one edge)
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
    }

    [Fact]
    public void UpdateTabOffsets_ThreeTabs ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);

        Assert.Equal (0, ((BorderView)tab1.Border.View!).TabOffset);
        Assert.Equal (5, ((BorderView)tab2.Border.View!).TabOffset);
        Assert.Equal (10, ((BorderView)tab3.Border.View!).TabOffset);
    }

    [Fact]
    public void Value_SetsFocus ()
    {
        IDriver driver = CreateTestDriver ();

        Tabs tabs = new () { Driver = driver, Width = 40, Height = 10 };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Layout ();

        tabs.Value = tab2;

        Assert.Same (tab2, tabs.Value);
    }

    [Fact]
    public void ValueChanged_Fires ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        View? newValue = null;
        tabs.ValueChanged += (_, args) => newValue = args.NewValue;

        tabs.Value = tab2;

        Assert.Same (tab2, newValue);
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        Tabs tabs = new ();
        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Value = tab1;

        tabs.ValueChanging += (_, args) => args.Handled = true;

        tabs.Value = tab2;

        // Change was cancelled, value remains tab1
        Assert.Same (tab1, tabs.Value);
    }

    [Fact]
    public void Nav_Top_Command_Right_SelectsNextTab ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Top };

        View tab1 = new () { Title = "Tab_11", CanFocus = true };
        tabs.Add (tab1);
        View tab2 = new () { Title = "Tab_2", CanFocus = true };
        tabs.Add (tab2);

        (tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.SetFocus ();
        Assert.True ((tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);

        tabs.InvokeCommand (Command.Right);

        Assert.True ((tab2.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);

        tabs.Dispose ();
    }

    [Fact]
    public void Nav_Top_Command_Left_SelectsPreviousTab ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Top };

        View tab1 = new () { Title = "Tab_11", CanFocus = true };
        tabs.Add (tab1);
        View tab2 = new () { Title = "Tab_2", CanFocus = true };
        tabs.Add (tab2);

        (tab2.Border.View?.SubViews.ElementAt (0) as TitleView)?.SetFocus ();
        Assert.True ((tab2.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);

        tabs.InvokeCommand (Command.Left);

        Assert.True ((tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);

        tabs.Dispose ();
    }

    [Fact]
    public void Nav_Top_CursorRight_SelectsNextTab ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Top };

        View tab1 = new () { Title = "Tab_11", CanFocus = true };
        tabs.Add (tab1);
        View tab2 = new () { Title = "Tab_2", CanFocus = true };
        tabs.Add (tab2);

        (tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.SetFocus ();
        Assert.True ((tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);

        (tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.NewKeyDownEvent (Key.CursorRight);

        Assert.True ((tab2.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);

        tabs.Dispose ();
    }

    [Theory]
    [InlineData (Side.Left)]
    [InlineData (Side.Right)]
    public void Nav_Left_Or_Right_CursorDown_OnTabTitle_MovesToNextTab (Side tabSide)
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = tabSide };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tab1.Border.View?.SetFocus ();

        tabs.NewKeyDownEvent (Key.CursorDown);

        Assert.True (tab2.Border.View?.HasFocus ?? tab2.HasFocus);

        tabs.Dispose ();
    }

    [Theory]
    [InlineData (Side.Left)]
    [InlineData (Side.Right)]
    public void Nav_Left_Or_Right_CursorDown_OnLastTabTitle_WrapsToFirstTab (Side tabSide)
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = tabSide };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Layout ();

        tab2.Border.View?.SetFocus ();

        tabs.NewKeyDownEvent (Key.CursorDown);

        Assert.True (tab1.Border.View?.HasFocus ?? tab1.HasFocus);

        tabs.Dispose ();
    }

    [Theory]
    [InlineData (Side.Left)]
    [InlineData (Side.Right)]
    public void Nav_Left_Or_Right_CursorUp_OnTabTitle_MovesToPreviousTab (Side tabSide)
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = tabSide };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };
        View tab3 = new () { Title = "Tab3" };

        tabs.Add (tab1, tab2, tab3);
        tabs.Layout ();

        tab3.Border.View?.SetFocus ();

        tabs.NewKeyDownEvent (Key.CursorUp);

        Assert.True (tab2.Border.View?.HasFocus ?? tab2.HasFocus);

        tabs.Dispose ();
    }

    [Theory]
    [InlineData (Side.Left)]
    [InlineData (Side.Right)]
    public void Nav_Left_Or_Right_CursorUp_OnFirstTabTitle_WrapsToLastTab (Side tabSide)
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = tabSide };

        View tab1 = new () { Title = "Tab1" };
        View tab2 = new () { Title = "Tab2" };

        tabs.Add (tab1, tab2);
        tabs.Layout ();

        tab1.Border.View?.SetFocus ();

        tabs.NewKeyDownEvent (Key.CursorUp);

        Assert.True (tab2.Border.View?.HasFocus ?? tab2.HasFocus);

        tabs.Dispose ();
    }

    [Fact]
    public void Nav_Left_CursorRight_OnTabTitle_MovesIntoTabContent ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Left };

        View tab1 = new () { Title = "Tab1", CanFocus = true };
        View contentButton = new () { Title = "OK", CanFocus = true, Width = 4, Height = 1 };
        tab1.Add (contentButton);

        tabs.Add (tab1);
        tabs.Layout ();

        tab1.Border.View?.SetFocus ();
        Assert.True ((tab1.Border.View as BorderView)?.TitleView?.HasFocus);

        tabs.NewKeyDownEvent (Key.CursorRight);

        Assert.True (contentButton.HasFocus);

        tabs.Dispose ();
    }

    [Fact]
    public void Nav_Left_CursorLeft_OnTabTitle_MovesToPreviousTab ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Left };

        View tab1 = new () { Title = "Tab1", CanFocus = true };
        tabs.Add (tab1);
        View tab2 = new () { Title = "Tab2", CanFocus = true };
        tabs.Add (tab2);
        tabs.Layout ();

        (tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.SetFocus ();
        Assert.True ((tab1.Border.View?.SubViews.ElementAt (0) as TitleView)?.HasFocus);
        Assert.True (tab1.HasFocus);

        tabs.NewKeyDownEvent (Key.CursorLeft);

        Assert.True (tab2.HasFocus);

        tabs.Dispose ();
    }

    [Fact]
    public void Nav_Right_CursorLeft_OnTabTitle_MovesIntoTabContent ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Right };

        View tab1 = new () { Title = "Tab1" };
        View contentButton = new () { Title = "OK", CanFocus = true, Width = 4, Height = 1 };
        tab1.Add (contentButton);

        tabs.Add (tab1);
        tabs.Layout ();

        tab1.Border.View?.SetFocus ();

        tabs.NewKeyDownEvent (Key.CursorLeft);

        Assert.True (contentButton.HasFocus);

        tabs.Dispose ();
    }

    [Fact]
    public void Nav_Right_CursorRight_OnTabTitle_MovesToNextTab ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        Tabs tabs = new () { Driver = driver, Width = 20, Height = 10, TabSide = Side.Right };

        View tab1 = new () { Title = "Tab1" };
        tabs.Add (tab1);
        View tab2 = new () { Title = "Tab2" };
        tabs.Add (tab2);
        tabs.Layout ();

        tab1.Border.View?.SetFocus ();

        tabs.NewKeyDownEvent (Key.CursorRight);

        Assert.True (tab2.HasFocus);

        tabs.Dispose ();
    }

    // BUGBUG: This test should be failing because the same basic thing happens in user testing...
    [Fact]
    public void TitleView_Border_Does_Not_Overflow_Into_Tabs_Border ()
    {
        IDriver driver = CreateTestDriver (7, 8);

        View superView = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            BorderStyle = LineStyle.Dotted
        };

        View tabHost = new ()
        {
            Title = "H",
            Height = Dim.Fill (),
            Width = Dim.Fill (),
            BorderStyle = LineStyle.Double,
            SuperViewRendersLineCanvas = true
        };
        superView.Add (tabHost);

        View tab = new () { Title = "A", Height = Dim.Fill (), Width = Dim.Fill () };
        tab.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        tab.Border.LineStyle = LineStyle.Single;
        tab.Border.Thickness = new Thickness (1, 3, 1, 1);
        ((BorderView)tab.Border.View!).TabSide = Side.Top;
        ((BorderView)tab.Border.View!).TabOffset = 0;

        tabHost.Add (tab);

        superView.Layout ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┐
                                              ┊╔╡H╞╗┊
                                              ┊║┌─┐║┊
                                              ┊║│A│║┊
                                              ┊║├─┤║┊
                                              ┊║└─┘║┊
                                              ┊╚═══╝┊
                                              └┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);

        ((BorderView)tab.Border.View!).TabOffset = 1;
        superView.Layout ();
        driver.ClearContents ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ┌┄┄┄┄┄┐
                                              ┊╔╡H╞╗┊
                                              ┊║ ┌─║┊
                                              ┊║ │A║┊
                                              ┊║┌┴┬║┊
                                              ┊║└─┘║┊
                                              ┊╚═══╝┊
                                              └┄┄┄┄┄┘
                                              """,
                                              output,
                                              driver);
        superView.Dispose ();
    }
}
