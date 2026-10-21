using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

[TestSubject (typeof (TreeView))]
public class TreeViewTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Draw_EnableForDesign_Size_Absolute_Draws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();

        TreeView tree = new () { Driver = driver };

        tree.EnableForDesign ();
        tree.Frame = driver.Screen;
        tree.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ├-Root1
                                              │ ├─Child1.1
                                              │ └─Child1.2
                                              └-Root2
                                                ├-Child2.1
                                                │ ├─Child2.1.1
                                                │ └─Child2.1.2
                                                └─Child2.2
                                              """,
                                              output,
                                              driver);
    }

    [Fact]
    public void Draw_EnableForDesign_Size_Fill_Draws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();

        TreeView tree = new () { Driver = driver };

        tree.EnableForDesign ();
        tree.Width = Dim.Fill ();
        tree.Height = Dim.Fill ();
        tree.SetRelativeLayout (driver.Screen.Size);
        tree.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ├-Root1
                                              │ ├─Child1.1
                                              │ └─Child1.2
                                              └-Root2
                                                ├-Child2.1
                                                │ ├─Child2.1.1
                                                │ └─Child2.1.2
                                                └─Child2.2
                                              """,
                                              output,
                                              driver);
    }

    // CoPilot - Grok 4.6
    [Theory]
    [InlineData ("file-\U0001F600.txt", "\U0001F600", false, "└─file-")] // The emoji is on the right edge of the cell and cannot be displayed.
    [InlineData ("e\u0301", "e\u0301", true, "└─é")]
    [InlineData ("\U0001F600ABC", "\U0001F600", true, "└─😀ABC")]
    [InlineData ("ABC\U0001F468\u200D\U0001F469\u200D\U0001F467", "\U0001F468\u200D\U0001F469\u200D\U0001F467", true, "└─ABC👨‍👩‍👧")]
    [InlineData ("\U0001F34E\U0001F9AE\U0001F468\u200D\U0001F469\u200D\U0001F467", "\U0001F34E", true, "└─🍎🦮👨‍👩‍👧")]
    public void Draw_Preserves_Unicode_Grapheme_Clusters (
        string text,
        string expectedGrapheme,
        bool expectedFound,
        string expectedOutput
    )
    {
        // Arrange
        IDriver driver = CreateTestDriver ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Driver = driver,
            Frame = new Rectangle (0, 0, 8, 2) // 2 ensure at least one row is drawn
        };
        tree.AddObject (text);

        // Act
        // Drawing must not throw for supplementary-plane characters
        // or grapheme clusters containing combining marks.
        tree.Draw ();

        // The expected grapheme must remain intact in a single cell.
        bool found = false;
        string normalizedGrapheme = expectedGrapheme.Normalize ();

        for (int x = 0; x < driver.Contents!.GetLength (1); x++)
        {
            if (normalizedGrapheme == driver.Contents [0, x].Grapheme)
            {
                found = true;

                break;
            }
        }

        // Assert
        // The expected grapheme must be preserved in a single cell if not truncated.
        Assert.Equal (expectedFound, found);

        DriverAssert.AssertDriverContentsWithFrameAre (expectedOutput, output, driver);

        tree.Dispose ();
    }

    // CoPilot - Grok 4.6
    [Fact]
    public void Draw_RightEdge_Wide_Grapheme_Leaves_Blank_Column ()
    {
        IDriver driver = CreateTestDriver ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Driver = driver,
            Frame = new Rectangle (0, 0, 8, 2)
        };
        tree.AddObject ("file-\U0001F600.txt");
        tree.Draw ();

        // └─file- uses columns 0-6; the emoji needs 2 columns so column 7 is padded.
        Assert.Equal (" ", driver.Contents! [0, 7].Grapheme);

        tree.Dispose ();
    }

    // CoPilot - Grok 4.6
    [Theory]
    [InlineData ("\U0001F600ABC", 1, "─😀ABC\n─ABCDEFGHI")]
    [InlineData ("\U0001F600ABC", 2, "😀ABC\nABCDEFGHIJ")]
    [InlineData ("\U0001F600ABC", 3, " ABC\nBCDEFGHIJK")]
    [InlineData ("\U0001F600ABC", 4, "ABC\nCDEFGHIJKL")]
    [InlineData ("\U0000754CABC", 1, "─界ABC\n─ABCDEFGHI")]
    [InlineData ("\U0000754CABC", 2, "界ABC\nABCDEFGHIJ")]
    [InlineData ("\U0000754CABC", 3, " ABC\nBCDEFGHIJK")]
    [InlineData ("\U0000754CABC", 4, "ABC\nCDEFGHIJKL")]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 1, "─👨‍👩‍👧ABC\n─ABCDEFGHI")]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 2, "👨‍👩‍👧ABC\nABCDEFGHIJ")]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 3, " ABC\nBCDEFGHIJK")]
    [InlineData ("\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 4, "ABC\nCDEFGHIJKL")]
    [InlineData ("\U0001F34E\U0001F600ABC", 1, "─🍎😀ABC\n─ABCDEFGHI")]
    [InlineData ("\U0001F34E\U0001F600ABC", 2, "🍎😀ABC\nABCDEFGHIJ")]
    [InlineData ("\U0001F34E\U0001F600ABC", 3, " 😀ABC\nBCDEFGHIJK")]
    [InlineData ("\U0001F34E\U0001F600ABC", 4, "😀ABC\nCDEFGHIJKL")]
    [InlineData ("\U0000754B\U0000754CABC", 1, "─畋界ABC\n─ABCDEFGHI")]
    [InlineData ("\U0000754B\U0000754CABC", 2, "畋界ABC\nABCDEFGHIJ")]
    [InlineData ("\U0000754B\U0000754CABC", 3, " 界ABC\nBCDEFGHIJK")]
    [InlineData ("\U0000754B\U0000754CABC", 4, "界ABC\nCDEFGHIJKL")]
    [InlineData ("\U0001F468\u200D\U0001F692\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 1, "─👨‍🚒👨‍👩‍👧ABC\n─ABCDEFGHI")]
    [InlineData ("\U0001F468\u200D\U0001F692\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 2, "👨‍🚒👨‍👩‍👧ABC\nABCDEFGHIJ")]
    [InlineData ("\U0001F468\u200D\U0001F692\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 3, " 👨‍👩‍👧ABC\nBCDEFGHIJK")]
    [InlineData ("\U0001F468\u200D\U0001F692\U0001F468\u200D\U0001F469\u200D\U0001F467ABC", 4, "👨‍👩‍👧ABC\nCDEFGHIJKL")]
    [InlineData ("\u00AD\u0192\u00FF\u00C7", 1, "─\u00ADƒÿÇ\n─ABCDEFGHI")] // Soft Hyphen (SHY) should normally be invisible unless used for line breaking.
    [InlineData ("\u00AD\u0192\u00FF\u00C7", 2, "\u00ADƒÿÇ\nABCDEFGHIJ")] // Some terminals render it as '-' instead, so this also tests terminal compatibility.
    [InlineData ("\u00AD\u0192\u00FF\u00C7", 3, "ƒÿÇ\nBCDEFGHIJK")]
    [InlineData ("\u00AD\u0192\u00FF\u00C7", 4, "ÿÇ\nCDEFGHIJKL")]
    public void Draw_HorizontalScroll_Preserves_Wide_Grapheme_Boundaries (
        string text,
        int scrollOffsetHorizontal,
        string expectedOutput
    )
    {
        // Arrange
        IDriver driver = CreateTestDriver ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Driver = driver,
            Frame = new Rectangle (0, 0, 10, 3)
        };

        tree.AddObject (text);
        tree.AddObject ("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        tree.ScrollOffsetHorizontal = scrollOffsetHorizontal;

        // Act
        tree.Draw ();

        // Assert
        // WithFrameAre keeps first-line fill spaces when a longer row sets the frame width.
        DriverAssert.AssertDriverContentsWithFrameAre (PadFirstLineToWidth (expectedOutput, 10), output, driver);

        tree.Dispose ();
    }

    // CoPilot - Grok 4.6
    [Theory]
    [InlineData ("\U0001F600ABC", 0, "├─☐ 😀ABC\n└─☐ ABCDEF")]
    [InlineData ("\U0001F600ABC", 1, "─☐ 😀ABC\n─☐ ABCDEFG")]
    [InlineData ("\U0001F600ABC", 2, "☐ 😀ABC\n☐ ABCDEFGH")]
    [InlineData ("\U0001F600ABC", 3, " 😀ABC\n ABCDEFGHI")]
    [InlineData ("\U0001F600ABC", 4, "😀ABC\nABCDEFGHIJ")]
    [InlineData ("\U0001F600ABC", 5, " ABC\nBCDEFGHIJK")]
    [InlineData ("\U0001F600ABC", 6, "ABC\nCDEFGHIJKL")]
    [InlineData ("\U0000754CABC", 0, "├─☐ 界ABC\n└─☐ ABCDEF")]
    [InlineData ("\U0000754CABC", 4, "界ABC\nABCDEFGHIJ")]
    [InlineData ("\U0000754CABC", 5, " ABC\nBCDEFGHIJK")]
    [InlineData ("ABC\U0001F468\u200D\U0001F469\u200D\U0001F467", 4, "ABC👨‍👩‍👧\nABCDEFGHIJ")]
    [InlineData ("ABC\U0001F468\u200D\U0001F469\u200D\U0001F467", 7, "👨‍👩‍👧\nDEFGHIJKLM")]
    [InlineData ("ABC\U0001F468\u200D\U0001F469\u200D\U0001F467", 8, "EFGHIJKLMN")]
    public void Draw_HorizontalScroll_CheckboxMode_Preserves_Wide_Grapheme_Boundaries (
        string text,
        int scrollOffsetHorizontal,
        string expectedOutput
    )
    {
        IDriver driver = CreateTestDriver ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Driver = driver,
            Frame = new Rectangle (0, 0, 10, 3),
            CheckboxMode = true
        };

        tree.AddObject (text);
        tree.AddObject ("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        tree.ScrollOffsetHorizontal = scrollOffsetHorizontal;
        tree.Draw ();

        string expected = expectedOutput.Replace ("☐", Glyphs.CheckStateUnChecked.ToString ());
        DriverAssert.AssertDriverContentsWithFrameAre (PadFirstLineToWidth (expected, 10), output, driver);

        tree.Dispose ();
    }

    // CoPilot - Grok 4.6
    [Fact]
    public void Draw_CheckboxMode_RightEdge_Wide_Grapheme_Leaves_Blank_Column ()
    {
        IDriver driver = CreateTestDriver ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Driver = driver,
            Frame = new Rectangle (0, 0, 10, 2),
            CheckboxMode = true
        };

        // └─☐ file- uses columns 0-8; the emoji needs 2 columns so column 9 is padded.
        tree.AddObject ("file-\U0001F600.txt");
        tree.Draw ();

        Assert.Equal (" ", driver.Contents! [0, 9].Grapheme);
        Assert.Equal (Glyphs.CheckStateUnChecked.ToString (), driver.Contents! [0, 2].Grapheme);

        tree.Dispose ();
    }

    // CoPilot - Grok 4.6
    [Fact]
    public void Draw_HorizontalScroll_Preserves_ZeroWidth_Grapheme_At_Skip_Boundary ()
    {
        IDriver driver = CreateTestDriver ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Driver = driver,
            Frame = new Rectangle (0, 0, 10, 2)
        };

        // Prefix └─ (2 columns) then 'X' (1). Scroll 3 lands on the ZWSP before ABC.
        // Extra letters keep content wider than the viewport so the offset is not clamped.
        tree.AddObject ("X\u200BABCDEFGHIJKLMN");
        tree.ScrollOffsetHorizontal = 3;
        tree.Draw ();

        bool foundZwsp = false;

        for (int x = 0; x < driver.Contents!.GetLength (1); x++)
        {
            if (driver.Contents [0, x].Grapheme == "\u200B")
            {
                foundZwsp = true;

                break;
            }
        }

        Assert.True (foundZwsp);
        Assert.Equal ("A", driver.Contents! [0, 1].Grapheme);

        tree.Dispose ();
    }

    [Fact]
    public void Draw_EnableForDesign_Size_Auto_Draws_Correctly ()
    {
        IDriver driver = CreateTestDriver ();

        TreeView tree = new () { Driver = driver };

        tree.EnableForDesign ();
        tree.Width = Dim.Auto ();
        tree.Height = Dim.Auto ();
        tree.SetRelativeLayout (driver.Screen.Size);
        tree.Layout ();
        tree.Draw ();

        DriverAssert.AssertDriverContentsAre ("""
                                              ├-Root1
                                              │ ├─Child1.1
                                              │ └─Child1.2
                                              └-Root2
                                                ├-Child2.1
                                                │ ├─Child2.1.1
                                                │ └─Child2.1.2
                                                └─Child2.2
                                              """,
                                              output,
                                              driver);
    }

    // Codex - gpt 5.5 medium
    // Regression for issue #5509.
    [Fact]
    public void Draw_ColorGetter_UsesActiveForSelectedRow_WhenTreeViewDoesNotHaveFocus ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 4);

        const string selected = "selected";
        Attribute normal = new (Color.BrightRed, Color.Black);
        Attribute focus = new (Color.BrightYellow, Color.Blue);
        Attribute active = new (Color.BrightYellow, Color.DarkGray);

        Scheme treeScheme = new ()
        {
            Normal = new Attribute (Color.BrightGreen, Color.Black),
            Focus = new Attribute (Color.Black, Color.BrightGreen),
            Active = new Attribute (Color.BrightGreen, Color.DarkGray)
        };

        Scheme customScheme = new ()
        {
            Normal = normal,
            Focus = focus,
            Active = active
        };

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false))
        {
            Width = 20,
            Height = 2,
            ColorGetter = item => item == selected ? customScheme : null
        };
        tree.Style.ShowBranchLines = false;
        tree.SetScheme (treeScheme);
        tree.AddObject (selected);
        tree.SelectedObject = selected;

        View otherView = new () { Y = Pos.Bottom (tree), Width = 1, Height = 1, CanFocus = true };

        using Runnable top = new ();
        top.Add (tree, otherView);

        app.Begin (top);
        tree.SetFocus ();
        app.LayoutAndDraw ();
        Assert.Equal (focus, app.Driver.Contents! [0, 1].Attribute);

        otherView.SetFocus ();
        app.LayoutAndDraw ();

        Assert.False (tree.HasFocus);
        Assert.Equal (active, app.Driver.Contents! [0, 1].Attribute);
    }

    [Fact]
    public void CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        var tree = new TreeView ();

        tree.AddObjects ([
                             new TreeNode { Text = "apricot" },
                             new TreeNode { Text = "arm" },
                             new TreeNode { Text = "bat" },
                             new TreeNode { Text = "batman" },
                             new TreeNode { Text = "bates hotel" },
                             new TreeNode { Text = "candle" }
                         ]);

        tree.SetFocus ();

        tree.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal ("apricot", tree.SelectedObject?.Text);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (tree.NewKeyDownEvent (Key.B));
        Assert.NotNull (tree.SelectedObject);
        Assert.Equal ("arm", tree.SelectedObject.Text);

        Assert.True (tree.NewKeyDownEvent (Key.B));
        Assert.Equal ("bat", tree.SelectedObject.Text);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (tree.NewKeyDownEvent (Key.C));
        Assert.Equal ("candle", tree.SelectedObject.Text);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Command_Accept_ActivatesNode ()
    {
        TreeView treeView = new ();
        TreeNode root = new () { Text = "Root" };
        treeView.AddObject (root);
        treeView.SelectedObject = root;

        var acceptingFired = false;

        treeView.Accepting += (_, e) =>
                              {
                                  acceptingFired = true;
                                  e.Handled = true;
                              };

        bool? result = treeView.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        treeView.Dispose ();
    }

    // Claude - Opus 4.6
    // Activate should NOT raise Accepting (it only activates the object).
    // Accept SHOULD raise Accepting. HotKey invokes Activate, so it must not trigger Accept.
    [Fact]
    public void Command_Activate_Does_Not_Accept ()
    {
        TreeView treeView = new ();
        TreeNode root = new () { Text = "Root" };
        treeView.AddObject (root);
        treeView.SelectedObject = root;

        var acceptingCount = 0;

        treeView.Accepting += (_, e) =>
                              {
                                  acceptingCount++;
                                  e.Handled = true;
                              };

        treeView.InvokeCommand (Command.Activate);
        int afterActivate = acceptingCount;

        treeView.InvokeCommand (Command.Accept);
        int afterAccept = acceptingCount;

        // Activate should not trigger Accepting; Accept should
        Assert.Equal (0, afterActivate);
        Assert.Equal (1, afterAccept);

        treeView.Dispose ();
    }

    [Fact]
    public void Command_HotKey_SetsFocus ()
    {
        TreeView treeView = new () { Text = "Test" };
        treeView.BeginInit ();
        treeView.EndInit ();
        Assert.False (treeView.HasFocus);

        treeView.InvokeCommand (Command.HotKey);

        Assert.True (treeView.HasFocus);

        treeView.Dispose ();
    }

    [Fact]
    public void Command_Toggle_ExpandCollapse ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);

        // Factory is the root and is initially collapsed
        Assert.False (tree.IsExpanded (f));

        // Select the factory node
        tree.SelectedObject = f;

        // Space should expand the selected node via Command.Activate
        tree.InvokeCommand (Command.Toggle);

        Assert.True (tree.IsExpanded (f));

        // Space again should collapse it
        tree.InvokeCommand (Command.Toggle);

        Assert.False (tree.IsExpanded (f));

        tree.Dispose ();
    }

    // Copilot
    [Fact]
    public void CheckboxMode_Space_Toggles_Checked_State_Not_Expansion ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out Car car2);
        tree.CheckboxMode = true;
        tree.SelectedObject = f;

        List<object> checkedObjects = [];
        tree.CheckedChanged += (_, e) =>
                               {
                                   checkedObjects.Add (e.Object!);
                               };

        tree.NewKeyDownEvent (Key.Space);

        Assert.False (tree.IsExpanded (f));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (f));

        // Propagation: f and all its children should be checked
        Assert.Equal (CheckState.Checked, tree.GetCheckState (car1));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (car2));

        // Events fire for each node that changed
        Assert.Contains (f, checkedObjects);
        Assert.Contains (car1, checkedObjects);
        Assert.Contains (car2, checkedObjects);
    }

    // Copilot
    [Fact]
    public void CheckboxMode_Draws_Checkbox_Glyphs_And_Indeterminate_Parent ()
    {
        IDriver driver = CreateTestDriver ();
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);
        tree.Driver = driver;
        tree.CheckboxMode = true;
        tree.Frame = new Rectangle (0, 0, 20, 3);
        tree.Expand (f);

        tree.SetChecked (car1, CheckState.Checked);
        tree.Draw ();

        DriverAssert.AssertDriverContentsAre ($"""
                                               └-{Glyphs.CheckStateNone} Factory
                                                 ├─{Glyphs.CheckStateChecked} 
                                                 └─{Glyphs.CheckStateUnChecked} 
                                               """,
                                               output,
                                               driver);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_MouseClick_OnCheckbox_Toggles_Check ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        root.Children.Add (new TreeNode { Text = "Child1" });
        tree.AddObject (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Layout (no border): └ + ☐ · R o o t
        // Pos:                  0 1 2 3 4 5 6 7
        // Checkbox is at screen position x=2

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (2, 0)));

        Assert.False (tree.IsExpanded (root));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void MouseClick_OnExpandSymbol_Expands_Node ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };

        TreeNode root = new () { Text = "Root" };
        root.Children.Add (new TreeNode { Text = "Child1" });
        tree.AddObject (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Layout (no border): └ + R o o t
        // Pos:                  0 1 2 3 4 5
        // Expand symbol at position 1

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (1, 0)));

        Assert.True (tree.IsExpanded (root));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_MouseClick_OnExpandSymbol_Expands_Not_Toggles_Check ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        root.Children.Add (new TreeNode { Text = "Child1" });
        tree.AddObject (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Layout (no border): └ + ☐ · R o o t
        // Pos:                  0 1 2 3 4 5 6 7
        // Expand symbol at position 1, checkbox at position 2

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (1, 0)));

        Assert.True (tree.IsExpanded (root));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_TriState_Parent_Reflects_Children ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);

        // Initially all unchecked
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));

        // Check one child → parent becomes indeterminate
        tree.SetChecked (child1, CheckState.Checked);
        Assert.Equal (CheckState.None, tree.GetCheckState (root));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child2));

        // Check all children → parent becomes checked
        tree.SetChecked (child2, CheckState.Checked);
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));

        // Uncheck one child → parent becomes indeterminate again
        tree.SetChecked (child1, CheckState.UnChecked);
        Assert.Equal (CheckState.None, tree.GetCheckState (root));

        // Uncheck all children → parent becomes unchecked
        tree.SetChecked (child2, CheckState.UnChecked);
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_Toggling_Parent_Propagates_To_Children ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);

        // Toggle parent ON → all children become checked
        tree.SetChecked (root, CheckState.Checked);
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (child2));

        // Toggle parent OFF → all children become unchecked
        tree.SetChecked (root, CheckState.UnChecked);
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child2));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_Toggle_Parent_When_All_Children_Checked_Unchecks_All ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Check all children so parent derives as Checked
        tree.SetChecked (child1, CheckState.Checked);
        tree.SetChecked (child2, CheckState.Checked);
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));

        // Select root and press Space to toggle it OFF
        tree.SelectedObject = root;
        tree.NewKeyDownEvent (Key.Space);

        // Parent and all children should now be unchecked
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child2));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_MouseClick_Uncheck_Parent_When_Children_Checked ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 20, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Check all children so parent derives as Checked
        tree.SetChecked (child1, CheckState.Checked);
        tree.SetChecked (child2, CheckState.Checked);
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));

        // Click on root's checkbox glyph (x=2 for expanded root with branch lines)
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (2, 0)));

        // Parent and all children should now be unchecked
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child2));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_MouseClick_Children_Then_Uncheck_Parent ()
    {
        // Reproduce exact user scenario: click child1 cb, click child2 cb, then click root cb
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 30, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Layout with ShowBranchLines=true (default), CheckboxMode=true:
        // GetLinePrefix yields: for each parent 2 elements (line + space), then 1 junction element.
        // IsHitOnCheckbox = GetLinePrefix().Count() + GetExpandableSymbol().GetColumns()
        // Root (depth=0): prefix count=1 (junction only), expand=1 col → checkbox at x=2
        // Children (depth=1): prefix count=3 (parent line+space + junction), expand=1 col → checkbox at x=4

        // Click child1's checkbox at (4, 1)
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (4, 1)));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.None, tree.GetCheckState (root)); // indeterminate

        // Click child2's checkbox at (4, 2)
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (4, 2)));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (child2));
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root)); // all children checked

        // Click root's checkbox at (2, 0) to uncheck everything
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (2, 0)));

        // Parent and all children should now be unchecked
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child1));
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (child2));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_Collapsed_Parent_Shows_Indeterminate_When_One_Child_Checked ()
    {
        // When only one child is checked and parent is collapsed, parent should show
        // indeterminate (None), not Checked or UnChecked.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 30, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Check only child1 - parent should be indeterminate
        tree.SetChecked (child1, CheckState.Checked);
        Assert.Equal (CheckState.None, tree.GetCheckState (root));

        // Collapse root - should STILL show indeterminate
        tree.Collapse (root);
        Assert.Equal (CheckState.None, tree.GetCheckState (root));

        // Expand again - should still be indeterminate
        tree.Expand (root);
        Assert.Equal (CheckState.None, tree.GetCheckState (root));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_Collapsed_Parent_Shows_Checked_When_All_Children_Checked ()
    {
        // When all children are checked and parent is collapsed, parent should show Checked.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 30, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // Check both children
        tree.SetChecked (child1, CheckState.Checked);
        tree.SetChecked (child2, CheckState.Checked);
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));

        // Collapse root - should STILL show Checked
        tree.Collapse (root);
        Assert.Equal (CheckState.Checked, tree.GetCheckState (root));

        top.Dispose ();
        app.Dispose ();
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_Collapsed_Parent_Shows_UnChecked_When_No_Children_Checked ()
    {
        // When no children are checked and parent is collapsed, parent should show UnChecked.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TreeView tree = new () { Width = 30, Height = 5 };
        tree.CheckboxMode = true;

        TreeNode root = new () { Text = "Root" };
        TreeNode child1 = new () { Text = "C1" };
        TreeNode child2 = new () { Text = "C2" };
        root.Children.Add (child1);
        root.Children.Add (child2);
        tree.AddObject (root);
        tree.Expand (root);

        Runnable top = new ();
        top.Add (tree);
        app.Begin (top);
        app.LayoutAndDraw ();

        // No children checked - parent is UnChecked
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));

        // Collapse root - should STILL show UnChecked
        tree.Collapse (root);
        Assert.Equal (CheckState.UnChecked, tree.GetCheckState (root));

        top.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ContentWidth_BiggerAfterExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);
        tree.BeginInit ();
        tree.EndInit ();

        tree.Frame = new Rectangle (0, 0, 10, 10);

        //-+Factory
        Assert.Equal (9, tree.GetContentWidth (true));

        car1.Name = "123456789";

        tree.Expand (f);

        //..├-123456789
        Assert.Equal (13, tree.GetContentWidth (true));

        tree.Collapse (f);

        //-+Factory
        Assert.Equal (9, tree.GetContentWidth (true));
    }

    [Fact]
    public void ContentWidth_VisibleVsAll ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out Car car2);
        tree.BeginInit ();
        tree.EndInit ();

        // control only allows 1 row to be viewed at once
        tree.Frame = new Rectangle (0, 0, 20, 1);

        //-+Factory
        Assert.Equal (9, tree.GetContentWidth (true));
        Assert.Equal (9, tree.GetContentWidth (false));

        car1.Name = "123456789";
        car2.Name = "12345678";

        tree.Expand (f);

        // Although expanded the bigger (longer) child node is not in the rendered area of the control
        Assert.Equal (9, tree.GetContentWidth (true));

        Assert.Equal (13, tree.GetContentWidth (false)); // If you ask for the global max width it includes the longer child

        // Now that we have scrolled down 1 row we should see the big child
        tree.ScrollOffsetVertical = 1;
        Assert.Equal (13, tree.GetContentWidth (true));
        Assert.Equal (13, tree.GetContentWidth (false));

        // Scroll down so only car2 is visible (3 items - 1 viewport = max offset 2)
        tree.ScrollOffsetVertical = 2;
        Assert.Equal (12, tree.GetContentWidth (true));
        Assert.Equal (13, tree.GetContentWidth (false));

        // With content-area clamping, offset 5 is clamped to 2 (3 items - 1 viewport height)
        tree.ScrollOffsetVertical = 5;
        Assert.Equal (2, tree.ScrollOffsetVertical);
        Assert.Equal (12, tree.GetContentWidth (true));
        Assert.Equal (13, tree.GetContentWidth (false));
    }

    // Copilot - Opus 4.6
    [Fact]
    public void DoubleClick_Raises_Accepting ()
    {
        TreeView<object> tree = CreateTree (out _, out _, out _);

        var acceptingFired = false;

        tree.Accepting += (_, e) =>
                          {
                              acceptingFired = true;
                              e.Handled = true;
                          };

        // Double-click should raise Accepting via Command.Accept
        tree.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonDoubleClicked });

        Assert.True (acceptingFired);

        tree.Dispose ();
    }

    [Fact]
    public void DoubleClick_SelectsObject_And_Accepts ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);

        Assert.NotSame (f, tree.SelectedObject);

        var acceptingFired = false;

        // Double-click now routes through Command.Accept (CWP flow)
        tree.Accepted += (_, _) => { acceptingFired = true; };

        Assert.False (acceptingFired);

        tree.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed });
        tree.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased });
        tree.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonClicked });
        tree.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonDoubleClicked });

        Assert.True (acceptingFired);
        Assert.Same (f, tree.SelectedObject);
    }

    [Fact]
    public void EmptyTreeView_ContentSizes ()
    {
        var emptyTree = new TreeView ();
        Assert.Equal (0, emptyTree.GetContentWidth (true));
        Assert.Equal (0, emptyTree.GetContentWidth (false));
    }

    [Fact]
    public void EmptyTreeViewGeneric_ContentSizes ()
    {
        TreeView<string> emptyTree = new ();
        Assert.Equal (0, emptyTree.GetContentWidth (true));
        Assert.Equal (0, emptyTree.GetContentWidth (false));
    }

    // Copilot - Opus 4.6
    [Fact]
    public void EnterKey_Raises_Accepting ()
    {
        TreeView<object> tree = CreateTree (out _, out _, out _);

        var acceptingFired = false;

        tree.Accepting += (_, e) =>
                          {
                              acceptingFired = true;
                              e.Handled = true;
                          };

        // Enter key should raise Accepting via Command.Accept
        tree.NewKeyDownEvent (Key.Enter);

        Assert.True (acceptingFired);

        tree.Dispose ();
    }

    /// <summary>
    ///     Tests that <see cref="TreeView{T}.GetChildren"/> returns the child objects for the factory.  Note that
    ///     the method only works once the parent branch (Factory) is expanded to expose the child (Car)
    /// </summary>
    [Fact]
    public void GetChildren_ReturnsChildrenOnlyWhenExpanded ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        Assert.Empty (tree.GetChildren (f));
        Assert.Empty (tree.GetChildren (c1));
        Assert.Empty (tree.GetChildren (c2));

        // now when we expand the factory we discover the cars
        tree.Expand (f);

        Assert.Contains (c1, tree.GetChildren (f));
        Assert.Contains (c2, tree.GetChildren (f));
        Assert.Empty (tree.GetChildren (c1));
        Assert.Empty (tree.GetChildren (c2));

        tree.Collapse (f);

        Assert.Empty (tree.GetChildren (f));
        Assert.Empty (tree.GetChildren (c1));
        Assert.Empty (tree.GetChildren (c2));
    }

    /// <summary>
    ///     Tests that <see cref="TreeView{T}.GetParent"/> returns the parent object for Cars (Factories).  Note that
    ///     the method only works once the parent branch (Factory) is expanded to expose the child (Car)
    /// </summary>
    [Fact]
    public void GetParent_ReturnsParentOnlyWhenExpanded ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        Assert.Null (tree.GetParent (f));
        Assert.Null (tree.GetParent (c1));
        Assert.Null (tree.GetParent (c2));

        // now when we expand the factory we discover the cars
        tree.Expand (f);

        Assert.Null (tree.GetParent (f));
        Assert.Equal (f, tree.GetParent (c1));
        Assert.Equal (f, tree.GetParent (c2));

        tree.Collapse (f);

        Assert.Null (tree.GetParent (f));
        Assert.Null (tree.GetParent (c1));
        Assert.Null (tree.GetParent (c2));
    }

    /// <summary>Tests <see cref="TreeView{T}.GetScrollOffsetOf"/> for objects that are as yet undiscovered by the tree</summary>
    [Fact]
    public void GetScrollOffsetOf_MinusOneForUnRevealed ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        // to start with the tree is collapsed and only knows about the root object
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c2));

        // reveal it by expanding the root object
        tree.Expand (f);

        // tree now knows about children
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (2, tree.GetScrollOffsetOf (c2));

        // after collapsing the root node again
        tree.Collapse (f);

        // tree no longer knows about the locations of these objects
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c2));
    }

    // Copilot
    [Fact]
    public void GoTo_OnlyAppliesToExposedObjects ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car car1, out _);
        tree.BeginInit ();
        tree.EndInit ();

        // Make tree bounds 1 in height so that EnsureVisible always requires updating scroll offset
        tree.Frame = new Rectangle (0, 0, 50, 1);

        Assert.Null (tree.SelectedObject);
        Assert.Equal (0, tree.ScrollOffsetVertical);

        // car 1 is not yet exposed
        tree.GoTo (car1);

        Assert.Null (tree.SelectedObject);
        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.Expand (f);

        // Car1 is now exposed by expanding the factory
        tree.GoTo (car1);

        Assert.Equal (car1, tree.SelectedObject);
        Assert.Equal (1, tree.Viewport.Y);
    }

    [Fact]
    public void GoToEnd_ShouldNotFailOnEmptyTreeView ()
    {
        var tree = new TreeView ();

        Exception? exception = Record.Exception (() => tree.GoToEnd ());

        Assert.Null (exception);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var treeView = new TreeView ();
        var accepted = false;

        treeView.Accepting += OnAccept;
        treeView.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object? sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new TreeView ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    /// <summary>
    ///     Tests that <see cref="TreeView{T}.IsExpanded"/> and <see cref="TreeView{T}.Expand()"/> behaves
    ///     correctly when an object cannot be expanded (because it has no children)
    /// </summary>
    [Fact]
    public void IsExpanded_FalseIfCannotExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c, out _);

        // expose the car by expanding the factory
        tree.Expand (f);

        // car is not expanded
        Assert.False (tree.IsExpanded (c));

        //try to expand the car (should have no effect because cars have no children)
        tree.Expand (c);

        Assert.False (tree.IsExpanded (c));

        // should also be ignored
        tree.Collapse (c);

        Assert.False (tree.IsExpanded (c));
    }

    /// <summary>Tests that <see cref="TreeView{T}.Expand()"/> and <see cref="TreeView{T}.IsExpanded"/> are consistent</summary>
    [Fact]
    public void IsExpanded_TrueAfterExpand ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);
        Assert.False (tree.IsExpanded (f));

        tree.Expand (f);
        Assert.True (tree.IsExpanded (f));

        tree.Collapse (f);
        Assert.False (tree.IsExpanded (f));
    }

    [Fact]
    public void MultiSelect_GetAllSelectedObjects ()
    {
        var tree = new TreeView ();

        TreeNode l1;
        TreeNode l2;
        TreeNode l3;

        var root = new TreeNode { Text = "Root" };
        root.Children.Add (l1 = new TreeNode { Text = "Leaf1" });
        root.Children.Add (l2 = new TreeNode { Text = "Leaf2" });
        root.Children.Add (l3 = new TreeNode { Text = "Leaf3" });
        root.Children.Add (new TreeNode { Text = "Leaf4" });

        tree.AddObject (root);
        tree.MultiSelect = true;

        tree.Expand (root);
        Assert.Empty (tree.GetAllSelectedObjects ());

        tree.SelectedObject = root;

        Assert.Single (tree.GetAllSelectedObjects (), root);

        // move selection down 1
        tree.AdjustSelection (1);

        Assert.Single (tree.GetAllSelectedObjects (), l1);

        // expand selection down 2 (e.g. shift down twice)
        tree.AdjustSelection (1, true);
        tree.AdjustSelection (1, true);

        Assert.Equal (3, tree.GetAllSelectedObjects ().Count ());
        Assert.Contains (l1, tree.GetAllSelectedObjects ());
        Assert.Contains (l2, tree.GetAllSelectedObjects ());
        Assert.Contains (l3, tree.GetAllSelectedObjects ());

        tree.Collapse (root);

        // No selected objects since the root was collapsed
        Assert.Empty (tree.GetAllSelectedObjects ());
    }

    /// <summary>
    ///     Same as <see cref="RefreshObject_AfterChangingChildrenGetterDuringRuntime"/> but uses
    ///     <see cref="TreeView{T}.RebuildTree"/> instead of <see cref="TreeView{T}.RefreshObject"/>
    /// </summary>
    [Fact]
    public void RebuildTree_AfterChangingChildrenGetterDuringRuntime ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        var wheel = "Shiny Wheel";

        // Expand the Factory
        tree.Expand (f);

        // c1 cannot have children
        Assert.Equal (f, tree.GetParent (c1));

        // expanding it does nothing
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        // change the children getter so that now cars can have wheels
        tree.TreeBuilder = new DelegateTreeBuilder<object> (o =>

                                                                // factories have cars
                                                                o is Factory
                                                                    ? new object [] { c1, c2 }

                                                                    // cars have wheels
                                                                    : new object [] { wheel },
                                                            _ => true);

        // still cannot expand
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        // Rebuild the tree
        tree.RebuildTree ();

        // Rebuild should not have collapsed any branches or done anything wierd
        Assert.True (tree.IsExpanded (f));

        tree.Expand (c1);
        Assert.True (tree.IsExpanded (c1));
        Assert.Equal (wheel, tree.GetChildren (c1).FirstOrDefault ());
    }

    /// <summary>
    ///     Tests how the tree adapts to changes in the ChildrenGetter delegate during runtime when some branches are
    ///     expanded and the new delegate returns children for a node that previously didn't have any children
    /// </summary>
    [Fact]
    public void RefreshObject_AfterChangingChildrenGetterDuringRuntime ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        var wheel = "Shiny Wheel";

        // Expand the Factory
        tree.Expand (f);

        // c1 cannot have children
        Assert.Equal (f, tree.GetParent (c1));

        // expanding it does nothing
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        // change the children getter so that now cars can have wheels
        tree.TreeBuilder = new DelegateTreeBuilder<object> (o =>

                                                                // factories have cars
                                                                o is Factory
                                                                    ? new object [] { c1, c2 }

                                                                    // cars have wheels
                                                                    : new object [] { wheel },
                                                            _ => true);

        // still cannot expand
        tree.Expand (c1);
        Assert.False (tree.IsExpanded (c1));

        tree.RefreshObject (c1);
        tree.Expand (c1);
        Assert.True (tree.IsExpanded (c1));
        Assert.Equal (wheel, tree.GetChildren (c1).FirstOrDefault ());
    }

    /// <summary>
    ///     Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree
    ///     using <see cref="TreeView{T}.RefreshObject"/>
    /// </summary>
    [Fact]
    public void RefreshObject_ChildRemoved ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out Car c1, out Car c2);

        //reveal it by expanding the root object
        tree.Expand (f);

        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (2, tree.GetScrollOffsetOf (c2));

        // Factory now no longer makes Car c1 (only c2)
        f.Cars = new [] { c2 };

        // Tree does not know this yet
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (2, tree.GetScrollOffsetOf (c2));

        // If the user has selected the node c1
        tree.SelectedObject = c1;

        // When we refresh the tree
        tree.RefreshObject (f);

        // Now tree knows that factory has only one child node c2
        Assert.Equal (0, tree.GetScrollOffsetOf (f));
        Assert.Equal (-1, tree.GetScrollOffsetOf (c1));
        Assert.Equal (1, tree.GetScrollOffsetOf (c2));

        // The old selection was c1 which is now gone so selection should default to the parent of that branch (the factory)
        Assert.Equal (f, tree.SelectedObject);
    }

    /// <summary>
    ///     Simulates behind the scenes changes to an object (which children it has) and how to sync that into the tree
    ///     using <see cref="TreeView{T}.RefreshObject"/>
    /// </summary>
    [Fact]
    public void RefreshObject_EqualityTest ()
    {
        var obj1 = new EqualityTestObject { Name = "Bob", Age = 1 };
        var obj2 = new EqualityTestObject { Name = "Bob", Age = 2 };

        var root = "root";

        TreeView<object> tree = new ();

        tree.TreeBuilder = new DelegateTreeBuilder<object> (s => ReferenceEquals (s, root) ? new object [] { obj1 } : Array.Empty<object> (), _ => true);
        tree.AddObject (root);

        // Tree is not expanded so the root has no children yet
        Assert.Empty (tree.GetChildren (root));

        tree.Expand (root);

        // now that the tree is expanded we should get our child returned
        Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj1, child)));

        // change the getter to return an Equal object (but not the same reference - obj2)
        tree.TreeBuilder = new DelegateTreeBuilder<object> (s => ReferenceEquals (s, root) ? new object [] { obj2 } : Array.Empty<object> (), _ => true);

        // tree has cached the knowledge of what children the root has so won't know about the change (we still get obj1)
        Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj1, child)));

        // now that we refresh the root we should get the new child reference (obj2)
        tree.RefreshObject (root);
        Assert.Equal (1, tree.GetChildren (root).Count (child => ReferenceEquals (obj2, child)));
    }

    [Fact]
    public void ScrollOffset_CannotBeNegative ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);

        // Expand so there are 3 visible lines, then give the tree a small viewport
        tree.Expand (f);
        tree.BeginInit ();
        tree.EndInit ();
        tree.Frame = new Rectangle (0, 0, 20, 1);

        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.ScrollOffsetVertical = -100;
        Assert.Equal (0, tree.ScrollOffsetVertical);

        // With 3 items and viewport height 1, the content-area system clamps to max 2 (3 - 1).
        tree.ScrollOffsetVertical = 10;
        Assert.Equal (2, tree.ScrollOffsetVertical);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void SpaceKey_Toggles_ExpandCollapse ()
    {
        TreeView<object> tree = CreateTree (out Factory f, out _, out _);

        // Factory is the root and is initially collapsed
        Assert.False (tree.IsExpanded (f));

        // Select the factory node
        tree.SelectedObject = f;

        // Space should expand the selected node via Command.Activate
        tree.NewKeyDownEvent (Key.Space);

        Assert.True (tree.IsExpanded (f));

        // Space again should collapse it
        tree.NewKeyDownEvent (Key.Space);

        Assert.False (tree.IsExpanded (f));

        tree.Dispose ();
    }

    /// <summary>
    ///     Verifies that <see cref="TreeView{T}"/> measures branch text width using grapheme-aware
    ///     <c>string.GetColumns()</c> rather than <c>string.Length</c>.
    ///     Wide CJK characters occupy 2 terminal cells each but have <c>string.Length</c> of 1,
    ///     so <c>.Length</c> under-counts the display width while <c>.GetColumns()</c> is correct.
    /// </summary>
    [Fact]
    public void TestTreeView_GetWidth_GraphemeCluster ()
    {
        // setup
        IDriver driver = CreateTestDriver ();
        var cjkText = "\u4F60\u597D"; // 你好
        Assert.Equal (2, cjkText.Length);
        Assert.Equal (4, cjkText.GetColumns ());

        var tv = new TreeView { Driver = driver, Width = 20, Height = 5 };
        var node = new TreeNode { Text = cjkText };
        tv.AddObject (node);
        tv.SetScheme (new Scheme ());
        tv.Style.ShowBranchLines = false;

        // execute
        tv.LayoutSubViews ();
        tv.SetClipToScreen ();
        tv.Draw ();

        // verify
        var actual = driver.ToString ();
        string [] lines = actual.Replace ("\r\n", "\n").Split ('\n');
        string firstLine = lines [0];
        Assert.Contains (cjkText, firstLine);
    }

    [Fact]
    public void TreeNode_WorksWithoutDelegate ()
    {
        var tree = new TreeView ();

        var root = new TreeNode { Text = "Root" };
        root.Children.Add (new TreeNode { Text = "Leaf1" });
        root.Children.Add (new TreeNode { Text = "Leaf2" });

        tree.AddObject (root);

        tree.Expand (root);
        Assert.Equal (2, tree.GetChildren (root).Count ());
    }

    /// <summary>Test object which considers for equality only <see cref="Name"/></summary>
    private class EqualityTestObject
    {
        [UsedImplicitly]
        public int Age { get; set; }

        public override bool Equals (object? obj) => obj is EqualityTestObject eto && Equals (Name, eto.Name);
        public override int GetHashCode () => Name.GetHashCode ();
        public required string Name { get; init; }
    }

    private static string PadFirstLineToWidth (string expected, int width)
    {
        string normalized = expected.Replace ("\r\n", "\n");
        int newline = normalized.IndexOf ('\n');
        string firstLine = newline < 0 ? normalized : normalized [..newline];
        int pad = width - firstLine.GetColumns ();

        if (pad <= 0)
        {
            return expected;
        }

        string padded = firstLine + new string (' ', pad);

        return newline < 0 ? padded : padded + normalized [newline..];
    }

    #region Test Setup Methods

    private class Factory
    {
        public required Car [] Cars { get; set; }
        public override string ToString () => "Factory";
    }

    private class Car
    {
        public required string Name { get; set; }
        public override string ToString () => Name;
    }

    private TreeView<object> CreateTree (out Factory factory1, out Car car1, out Car car2)
    {
        car1 = new Car { Name = string.Empty };
        car2 = new Car { Name = string.Empty };

        factory1 = new Factory { Cars = [car1, car2] };

        TreeView<object> tree = new (new DelegateTreeBuilder<object> (s => s is Factory f ? f.Cars : Array.Empty<object> (), _ => false));
        tree.AddObject (factory1);

        return tree;
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_SetChecked_Handles_Cyclic_TreeBuilder ()
    {
        // A tree builder that creates a cycle: A -> B -> A
        object a = "A";
        object b = "B";

        Dictionary<object, object []> graph = new ()
        {
            { a, [b] },
            { b, [a] }
        };

        TreeView<object> tree = new (new DelegateTreeBuilder<object> (
                                         o => graph.TryGetValue (o, out object []? children) ? children : [],
                                         _ => true));
        tree.CheckboxMode = true;
        tree.AddObject (a);

        // Should not stack overflow - cycle protection should prevent infinite recursion
        Exception? ex = Record.Exception (() => tree.SetChecked (a, CheckState.Checked));
        Assert.Null (ex);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void CheckboxMode_GetCheckState_Handles_Cyclic_TreeBuilder ()
    {
        // A tree builder that creates a cycle: A -> B -> A
        object a = "A";
        object b = "B";

        Dictionary<object, object []> graph = new ()
        {
            { a, [b] },
            { b, [a] }
        };

        TreeView<object> tree = new (new DelegateTreeBuilder<object> (
                                         o => graph.TryGetValue (o, out object []? children) ? children : [],
                                         _ => true));
        tree.CheckboxMode = true;
        tree.AddObject (a);

        tree.SetChecked (a, CheckState.Checked);

        // Should not stack overflow when deriving state
        Exception? ex = Record.Exception (() => tree.GetCheckState (a));
        Assert.Null (ex);
    }

    #endregion
}
