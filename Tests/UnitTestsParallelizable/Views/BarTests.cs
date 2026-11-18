using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Bar))]
public class BarTests
{
    // Claude - Opus 4.6
    [Fact]
    public void Has_CommandsToBubbleUp ()
    {
        Bar bar = new ();

        Assert.Contains (Command.Accept, bar.CommandsToBubbleUp);
        Assert.Contains (Command.Activate, bar.CommandsToBubbleUp);

        bar.Dispose ();
    }

    [Fact]
    public void Command_Activate_BubblesDownToShortcuts ()
    {
        Bar bar = new ();
        Shortcut shortcut1 = new () { Title = "Test1", Key = Key.D0.WithCtrl };
        bar.Add (shortcut1);

        var shortcut1ActivatingFired = 0;

        shortcut1.Activating += (_, e) => { shortcut1ActivatingFired++; };

        Shortcut shortcut2 = new () { Title = "Test2", Key = Key.D1.WithCtrl };
        bar.Add (shortcut2);

        var shortcut2ActivatingFired = 0;

        shortcut2.Activating += (_, e) => { shortcut2ActivatingFired++; };

        var barActivatingFired = 0;

        bar.Activating += (_, e) => { barActivatingFired++; };

        shortcut1.SetFocus ();

        // Invoke on Bar
        bar.InvokeCommand (Command.Activate);

        // Bar does not DispatchDown to shortcuts; it fires its own events
        Assert.Equal (0, shortcut1ActivatingFired);
        Assert.Equal (0, shortcut2ActivatingFired);
        Assert.Equal (1, barActivatingFired);

        bar.Dispose ();
    }

    [Fact]
    public void Command_Activate_On_Shortcut_BubblesDownToShortcuts ()
    {
        Bar bar = new ();
        Shortcut shortcut1 = new () { Title = "Test1", Key = Key.D0.WithCtrl };
        bar.Add (shortcut1);

        var shortcut1ActivatingFired = 0;

        shortcut1.Activating += (_, e) => { shortcut1ActivatingFired++; };

        Shortcut shortcut2 = new () { Title = "Test2", Key = Key.D1.WithCtrl };
        bar.Add (shortcut2);

        var shortcut2ActivatingFired = 0;

        shortcut2.Activating += (_, e) => { shortcut2ActivatingFired++; };

        var barActivatingFired = 0;

        bar.Activating += (_, e) => { barActivatingFired++; };

        shortcut1.SetFocus ();

        // Invoke on Shortcut
        shortcut1.InvokeCommand (Command.Activate);

        Assert.Equal (1, shortcut1ActivatingFired);
        Assert.Equal (0, shortcut2ActivatingFired);
        Assert.Equal (1, barActivatingFired); // Activate bubbles up from Shortcut to Bar

        bar.Dispose ();
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        var bar = new Bar ();

        Assert.NotNull (bar);
        Assert.True (bar.CanFocus);
        Assert.IsType<DimAuto> (bar.Width);
        Assert.IsType<DimAuto> (bar.Height);

        // TOOD: more
    }

    [Fact]
    public void Constructor_InitializesEmpty_WhenNoShortcutsProvided ()
    {
        var bar = new Bar ();
        Assert.Empty (bar.SubViews);
    }

    [Fact]
    public void Constructor_InitializesWithShortcuts_WhenProvided ()
    {
        List<Shortcut> shortcuts = new () { new Shortcut (Key.Empty, "Command1", null), new Shortcut (Key.Empty, "Command2", null) };

        var bar = new Bar (shortcuts);

        Assert.Equal (shortcuts.Count, bar.SubViews.Count);

        for (var i = 0; i < shortcuts.Count; i++)
        {
            Assert.Same (shortcuts [i], bar.SubViews.ElementAt (i));
        }
    }

    [Fact]
    public void AddShortcutAt_InsertsShortcutCorrectly ()
    {
        var bar = new Bar ();
        var shortcut = new Shortcut (Key.Empty, "Command", null);
        bar.AddShortcutAt (0, shortcut);

        Assert.Contains (shortcut, bar.SubViews);
    }

    [Fact]
    public void AlignmentModesProperty_SetsCorrectly ()
    {
        var bar = new Bar ();
        Assert.Equal (AlignmentModes.StartToEnd, bar.AlignmentModes); // Default value

        bar.AlignmentModes = AlignmentModes.EndToStart;
        Assert.Equal (AlignmentModes.EndToStart, bar.AlignmentModes);
    }

    [Fact]
    public void GetAttributeForRole_DoesNotDeferToSuperView_WhenSchemeNameIsSet ()
    {
        // This test would fail before the fix that checks SchemeName in GetAttributeForRole
        // StatusBar and MenuBar set SchemeName = "Menu", and should use Menu scheme
        // instead of deferring to parent's customized attributes

        var parentView = new View { SchemeName = "Base" };
        var statusBar = new StatusBar ();
        parentView.Add (statusBar);

        // Parent customizes attribute resolution
        var customAttribute = new Attribute (Color.BrightMagenta, Color.BrightGreen);

        parentView.GettingAttributeForRole += (_, args) =>
                                              {
                                                  if (args.Role != VisualRole.Normal)
                                                  {
                                                      return;
                                                  }
                                                  args.Result = customAttribute;
                                                  args.Handled = true;
                                              };

        // StatusBar sets SchemeName = "Menu" in its constructor
        // Before the fix: StatusBar would defer to parent and get customAttribute (WRONG)
        // After the fix: StatusBar uses Menu scheme (CORRECT)
        Scheme? menuScheme = SchemeManager.GetHardCodedSchemes ()? ["Menu"];
        Assert.NotEqual (customAttribute, statusBar.GetAttributeForRole (VisualRole.Normal));
        Assert.Equal (menuScheme!.Normal, statusBar.GetAttributeForRole (VisualRole.Normal));

        statusBar.Dispose ();
        parentView.Dispose ();
    }

    [Fact]
    public void Layout_ChangesBasedOnOrientation ()
    {
        var shortcut1 = new Shortcut (Key.Empty, "Command1", null);
        var shortcut2 = new Shortcut (Key.Empty, "Command2", null);
        var bar = new Bar (new List<Shortcut> { shortcut1, shortcut2 });

        bar.Orientation = Orientation.Horizontal;
        bar.LayoutSubViews ();

        // TODO: Assert specific layout expectations for horizontal orientation

        bar.Orientation = Orientation.Vertical;
        bar.LayoutSubViews ();

        // TODO: Assert specific layout expectations for vertical orientation
    }

    [Fact]
    public void OrientationProperty_SetsCorrectly ()
    {
        var bar = new Bar ();
        Assert.Equal (Orientation.Horizontal, bar.Orientation); // Default value

        bar.Orientation = Orientation.Vertical;
        Assert.Equal (Orientation.Vertical, bar.Orientation);
    }

    [Fact]
    public void RemoveShortcut_RemovesShortcutCorrectly ()
    {
        var shortcut1 = new Shortcut (Key.Empty, "Command1", null);
        var shortcut2 = new Shortcut (Key.Empty, "Command2", null);
        var bar = new Bar (new List<Shortcut> { shortcut1, shortcut2 });

        Shortcut? removedShortcut = bar.RemoveShortcut (0);

        Assert.Same (shortcut1, removedShortcut);
        Assert.DoesNotContain (shortcut1, bar.SubViews);
        Assert.Contains (shortcut2, bar.SubViews);
    }

    // Claude - Opus 4.6
    [Fact]
    public void MouseWheel_Navigates_Focus ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Shortcut sc1 = new () { Title = "First", Key = Key.F1 };
        Shortcut sc2 = new () { Title = "Second", Key = Key.F2 };
        Shortcut sc3 = new () { Title = "Third", Key = Key.F3 };

        Bar bar = new ([sc1, sc2, sc3]);
        (runnable as View)?.Add (bar);
        app.Begin (runnable);

        // First shortcut should have focus initially
        Assert.True (sc1.HasFocus);

        // Use NewMouseEvent directly on the bar to simulate wheel events
        // WheeledUp moves Forward (toward next subview in order)
        bar.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledUp, Position = Point.Empty });

        Assert.True (sc2.HasFocus);

        // Wheel up again to move to third
        bar.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledUp, Position = Point.Empty });

        Assert.True (sc3.HasFocus);

        // WheeledDown moves Backward (toward previous subview)
        bar.NewMouseEvent (new Mouse { Flags = MouseFlags.WheeledDown, Position = Point.Empty });

        Assert.True (sc2.HasFocus);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Vertical_Layout_Aligns_KeyViews ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        // Keys with different text lengths: "F1" (2 chars) vs "Ctrl+Shift+F12" (longer)
        Shortcut sc1 = new () { Title = "Short", Key = Key.F1 };
        Shortcut sc2 = new () { Title = "Medium", Key = Key.F12.WithCtrl.WithShift };
        Shortcut sc3 = new () { Title = "Long", Key = Key.F5 };

        Bar bar = new ([sc1, sc2, sc3]);
        bar.Orientation = Orientation.Vertical;

        (runnable as View)?.Add (bar);
        app.Begin (runnable);

        // After layout, all shortcuts should have the same MinimumKeyTextSize
        // which equals the max key text width among them
        int expected = sc1.MinimumKeyTextSize;
        Assert.True (expected > 0);
        Assert.Equal (expected, sc2.MinimumKeyTextSize);
        Assert.Equal (expected, sc3.MinimumKeyTextSize);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void AddShortcutAt_Rebuilds_SubViews_Order ()
    {
        Shortcut sc1 = new () { Title = "First", Key = Key.F1 };
        Shortcut sc2 = new () { Title = "Second", Key = Key.F2 };
        Bar bar = new ([sc1, sc2]);

        // Insert a new shortcut at index 1 (between sc1 and sc2)
        Shortcut scInserted = new () { Title = "Inserted", Key = Key.F3 };
        bar.AddShortcutAt (1, scInserted);

        Assert.Equal (3, bar.SubViews.Count);
        Assert.Same (sc1, bar.SubViews.ElementAt (0));
        Assert.Same (scInserted, bar.SubViews.ElementAt (1));
        Assert.Same (sc2, bar.SubViews.ElementAt (2));

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void RemoveShortcut_Returns_Removed_Item ()
    {
        Shortcut sc1 = new () { Title = "First", Key = Key.F1 };
        Shortcut sc2 = new () { Title = "Second", Key = Key.F2 };
        Shortcut sc3 = new () { Title = "Third", Key = Key.F3 };
        Bar bar = new ([sc1, sc2, sc3]);

        Assert.Equal (3, bar.SubViews.Count);

        // Remove the middle shortcut (index 1)
        Shortcut? removed = bar.RemoveShortcut (1);

        Assert.Same (sc2, removed);
        Assert.Equal (2, bar.SubViews.Count);
        Assert.Same (sc1, bar.SubViews.ElementAt (0));
        Assert.Same (sc3, bar.SubViews.ElementAt (1));

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Activate_Bubbles_To_Bar ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        var barActivatingFired = 0;

        bar.Activating += (_, _) => { barActivatingFired++; };

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, barActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Accept_Bubbles_To_Bar ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        var barAcceptingFired = 0;

        bar.Accepting += (_, _) => { barAcceptingFired++; };

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (1, barAcceptingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Activate_Handled_Does_Not_Bubble_To_Bar ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        var barActivatingFired = 0;

        shortcut.Activating += (_, e) => { e.Handled = true; };
        bar.Activating += (_, _) => { barActivatingFired++; };

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (0, barActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Accept_Handled_Does_Not_Bubble_To_Bar ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        var barAcceptingFired = 0;

        shortcut.Accepting += (_, e) => { e.Handled = true; };
        bar.Accepting += (_, _) => { barAcceptingFired++; };

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (0, barAcceptingFired);

        bar.Dispose ();
    }


    // Claude - Opus 4.6
    [Fact (Skip = "Fix in #4620 - genericized deferred raising")]
    public void Shortcut_Action_Fires_On_Accept ()
    {
        Bar bar = new ();

        int actionFired = 0;
        Shortcut shortcut = new () { Title = "Test", Action = () => actionFired++ };
        bar.Add (shortcut);

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (1, actionFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Action_Fires_On_Activate ()
    {
        Bar bar = new ();

        int actionFired = 0;
        Shortcut shortcut = new () { Title = "Test", Action = () => actionFired++ };
        bar.Add (shortcut);

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, actionFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Bar_Activate_Does_Not_BubbleDown_To_Shortcuts ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        var shortcutActivatingFired = 0;

        shortcut.Activating += (_, _) => { shortcutActivatingFired++; };

        // Invoke Activate directly on Bar
        bar.InvokeCommand (Command.Activate);

        // Bar fires its own events, does NOT DispatchDown to shortcuts
        Assert.Equal (0, shortcutActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Activate_Bubbles_Through_Nested_Bars ()
    {
        // Use Bar as the outer container since it already has CommandsToBubbleUp + custom handlers
        Bar outerBar = new () { Id = "outerBar" };
        Bar innerBar = new () { Id = "innerBar" };
        outerBar.Add (innerBar);

        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        innerBar.Add (shortcut);

        var innerBarActivatingFired = 0;
        var outerBarActivatingFired = 0;

        innerBar.Activating += (_, _) => { innerBarActivatingFired++; };
        outerBar.Activating += (_, _) => { outerBarActivatingFired++; };

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, innerBarActivatingFired);
        Assert.Equal (1, outerBarActivatingFired);

        outerBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Activate_Event_Order ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        List<string> eventOrder = [];

        shortcut.Activating += (_, _) => { eventOrder.Add ("Shortcut.Activating"); };
        bar.Activating += (_, _) => { eventOrder.Add ("Bar.Activating"); };
        shortcut.Activated += (_, _) => { eventOrder.Add ("Shortcut.Activated"); };
        bar.Activated += (_, _) => { eventOrder.Add ("Bar.Activated"); };

        shortcut.InvokeCommand (Command.Activate);

        // Shortcut.Activating fires first, then bubbles to Bar.Activating,
        // then Shortcut.Activated, then Bar.Activated
        Assert.Contains ("Shortcut.Activating", eventOrder);
        Assert.Contains ("Bar.Activating", eventOrder);

        int shortcutActivatingIdx = eventOrder.IndexOf ("Shortcut.Activating");
        int barActivatingIdx = eventOrder.IndexOf ("Bar.Activating");
        Assert.True (shortcutActivatingIdx < barActivatingIdx);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CheckBox_CommandView_Activate_Bubbles_To_Bar ()
    {
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };
        Shortcut shortcut = new () { Title = "Test", CommandView = checkBox };

        Bar bar = new ();
        bar.Add (shortcut);

        var barActivatingFired = 0;

        bar.Activating += (_, _) => { barActivatingFired++; };

        // Simulate activating the Shortcut (which BubblesDown to CommandView)
        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, barActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CheckBox_CommandView_Toggles_Exactly_Once ()
    {
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };
        Shortcut shortcut = new () { Title = "Test", CommandView = checkBox };

        Bar bar = new ();
        bar.Add (shortcut);

        CheckState initialState = checkBox.Value;

        // Activate the CheckBox directly — this bubbles up through Shortcut → Bar
        checkBox.InvokeCommand (Command.Activate);

        // CheckBox should have toggled exactly once (not double-toggled by the bubble)
        Assert.NotEqual (initialState, checkBox.Value);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Bar_Accept_Does_Not_BubbleDown_To_Shortcuts ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        var shortcutAcceptingFired = 0;

        shortcut.Accepting += (_, _) => { shortcutAcceptingFired++; };

        bar.InvokeCommand (Command.Accept);

        // Bar fires its own events, does NOT DispatchDown to shortcuts
        Assert.Equal (0, shortcutAcceptingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Shortcut_Not_In_Bar_Does_Not_Bubble ()
    {
        // A plain View does NOT have CommandsToBubbleUp, so Shortcut shouldn't bubble
        View superView = new () { Id = "plainView" };
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        superView.Add (shortcut);

        var superViewActivatingFired = 0;

        superView.Activating += (_, _) => { superViewActivatingFired++; };

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (0, superViewActivatingFired);

        superView.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Multiple_Shortcuts_Only_Invoked_One_Bubbles ()
    {
        Bar bar = new ();
        Shortcut sc1 = new () { Title = "First", Key = Key.F1 };
        Shortcut sc2 = new () { Title = "Second", Key = Key.F2 };
        bar.Add (sc1);
        bar.Add (sc2);

        var sc1ActivatingFired = 0;
        var sc2ActivatingFired = 0;
        var barActivatingFired = 0;

        sc1.Activating += (_, _) => { sc1ActivatingFired++; };
        sc2.Activating += (_, _) => { sc2ActivatingFired++; };
        bar.Activating += (_, _) => { barActivatingFired++; };

        // Activate only sc2
        sc2.InvokeCommand (Command.Activate);

        Assert.Equal (0, sc1ActivatingFired);
        Assert.Equal (1, sc2ActivatingFired);
        Assert.Equal (1, barActivatingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Nested_Bar_Handled_Does_Not_Bubble_To_Outer ()
    {
        Bar outerBar = new () { Id = "outerBar" };
        Bar innerBar = new () { Id = "innerBar" };
        outerBar.Add (innerBar);

        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        innerBar.Add (shortcut);

        var outerBarActivatingFired = 0;

        // Inner bar handler cancels the bubble
        innerBar.Activating += (_, e) => { e.Handled = true; };
        outerBar.Activating += (_, _) => { outerBarActivatingFired++; };

        shortcut.InvokeCommand (Command.Activate);

        // Outer bar should NOT receive the event because inner bar handled it
        Assert.Equal (0, outerBarActivatingFired);

        outerBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void CommandView_Accept_Bubbles_To_Bar ()
    {
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };
        Shortcut shortcut = new () { Title = "Test", CommandView = checkBox };

        Bar bar = new ();
        bar.Add (shortcut);

        var barAcceptingFired = 0;

        bar.Accepting += (_, _) => { barAcceptingFired++; };

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (1, barAcceptingFired);

        bar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Clearing_CommandsToBubbleUp_Prevents_Bubbling ()
    {
        Bar bar = new ();
        Shortcut shortcut = new () { Title = "Test", Key = Key.D0.WithCtrl };
        bar.Add (shortcut);

        // Clear CommandsToBubbleUp
        bar.CommandsToBubbleUp = [];

        var barActivatingFired = 0;

        bar.Activating += (_, _) => { barActivatingFired++; };

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (0, barActivatingFired);

        bar.Dispose ();
    }
}
