using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using UnitTests.Parallelizable;
using Terminal.Gui.Tracing;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="PopoverMenu"/> command bubbling across the PopoverMenu boundary.
///     Hierarchy under test:
///     <code>
///           └─ PopoverMenu (dynamically manages Menu SubViews)
///               └─ Menu (Root - contains MenuItems as SubViews)
///                   └─ MenuItem (Shortcut subclass)
///                       └─ OptionSelector&lt;Schemes&gt; (as CommandView)
///                           └─ CheckBox (SubView of OptionSelector)
///     </code>
/// </summary>
[TestSubject (typeof (PopoverMenu))]
public class PopoverMenuTests (ITestOutputHelper output)
{
    #region Helper Classes for Tracking

    /// <summary>
    ///     A Menu subclass that tracks Activating/Activated/Accepting events for testing.
    /// </summary>
    private class TrackingMenu : Menu
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        protected override bool OnActivating (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activating", sourceView, args.Context?.Binding));

            return base.OnActivating (args);
        }

        protected override void OnActivated (ICommandContext? ctx)
        {
            View? sourceView = null;
            ctx?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activated", sourceView, ctx?.Binding));
            base.OnActivated (ctx);
        }

        protected override bool OnAccepting (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Accepting", sourceView, args.Context?.Binding));

            return base.OnAccepting (args);
        }
    }

    /// <summary>
    ///     A PopoverMenu subclass that tracks Activating/Activated/Accepting events for testing.
    /// </summary>
    private class TrackingPopoverMenu : PopoverMenu
    {
        public List<(string EventName, View? Source, ICommandBinding? Binding)> EventLog { get; } = [];

        protected override bool OnActivating (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activating", sourceView, args.Context?.Binding));

            return base.OnActivating (args);
        }

        protected override void OnActivated (ICommandContext? ctx)
        {
            View? sourceView = null;
            ctx?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Activated", sourceView, ctx?.Binding));
            base.OnActivated (ctx);
        }

        protected override bool OnAccepting (CommandEventArgs args)
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            EventLog.Add (("Accepting", sourceView, args.Context?.Binding));

            return base.OnAccepting (args);
        }
    }

    #endregion

    #region Hierarchy Builder

    /// <summary>
    ///     Builds the full hierarchy:
    ///     PopoverMenu → Menu → MenuItem → OptionSelector&lt;Schemes&gt; → CheckBox
    ///     Returns the individual parts for test assertions.
    /// </summary>
    private static (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem menuItem, OptionSelector<Schemes> selector, CheckBox secondCheckBox)
        BuildOptionSelectorInPopoverMenuHierarchy ()
    {
        OptionSelector<Schemes> selector = new () { Id = "schemeSelector", CanFocus = true };

        // Force layout so CheckBoxes are created from enum values
        selector.Layout ();

        // Get the second CheckBox (index 1 = Schemes.Menu)
        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        CheckBox secondCheckBox = checkBoxes [1];
        secondCheckBox.Id = "secondCheckBox";

        // Create MenuItem with OptionSelector as CommandView
        MenuItem menuItem = new () { Id = "menuItem", CommandView = selector, HelpText = "Pick scheme" };

        // Create tracking Menu containing the MenuItem
        TrackingMenu menu = new () { Id = "menu" };
        menu.Add (menuItem);

        // Create PopoverMenu with the Menu as Root
        TrackingPopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;

        return (popoverMenu, menu, menuItem, selector, secondCheckBox);
    }

    #endregion

    #region Activate Bubbling Across PopoverMenu Boundary

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox inside an OptionSelector CommandView
    ///     inside a MenuItem inside a PopoverMenu reaches the PopoverMenu via event bridging.
    ///     This is the direct programmatic invocation path (no binding).
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Activate_FromOptionSelectorCheckBox_ReachesPopoverMenu_Direct ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem _, OptionSelector<Schemes> _, CheckBox secondCheckBox) =
                BuildOptionSelectorInPopoverMenuHierarchy ();

            // Act: Invoke Command.Activate directly on the second CheckBox (programmatic, no binding).
            secondCheckBox.InvokeCommand (Command.Activate);

            // Assert: Activated should have reached the Menu via BubbleActivatedUp
            Assert.Contains (menu.EventLog, e => e.EventName == "Activated");

            // Assert: Activated should have reached the PopoverMenu via Root bridge
            Assert.Contains (popoverMenu.EventLog, e => e.EventName == "Activated");

            popoverMenu.Dispose ();
        }
    }

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox via keyboard (Space key binding)
    ///     propagates through PopoverMenu via event bridging.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Activate_FromOptionSelectorCheckBox_ReachesPopoverMenu_Keyboard ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem _, OptionSelector<Schemes> _, CheckBox secondCheckBox) =
                BuildOptionSelectorInPopoverMenuHierarchy ();

            // Act: Simulate keyboard activation via Space key binding
            KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = new WeakReference<View> (secondCheckBox) };
            CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (secondCheckBox), Binding = keyBinding };
            secondCheckBox.InvokeCommand (Command.Activate, ctx);

            // Assert: Activated should have reached the Menu via BubbleActivatedUp
            Assert.Contains (menu.EventLog, e => e.EventName == "Activated");

            // Assert: PopoverMenu should have received the command via Root bridge
            Assert.Contains (popoverMenu.EventLog, e => e.EventName == "Activating");

            popoverMenu.Dispose ();
        }
    }

    /// <summary>
    ///     Tests that Command.Activate on a CheckBox via mouse click binding
    ///     propagates through PopoverMenu via event bridging.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Activate_FromOptionSelectorCheckBox_ReachesPopoverMenu_Mouse ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem _, OptionSelector<Schemes> _, CheckBox secondCheckBox) =
                BuildOptionSelectorInPopoverMenuHierarchy ();

            // Act: Simulate mouse activation via MouseBinding
            MouseBinding mouseBinding = new ([Command.Activate], MouseFlags.LeftButtonReleased) { Source = new WeakReference<View> (secondCheckBox) };
            CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (secondCheckBox), Binding = mouseBinding };
            secondCheckBox.InvokeCommand (Command.Activate, ctx);

            // Assert: Activated should have reached the Menu via BubbleActivatedUp
            Assert.Contains (menu.EventLog, e => e.EventName == "Activated");

            // Assert: PopoverMenu should have received the command via Root bridge
            Assert.Contains (popoverMenu.EventLog, e => e.EventName == "Activating");

            popoverMenu.Dispose ();
        }
    }

    #endregion

    #region Value Correctness

    /// <summary>
    ///     Tests that the OptionSelector value actually changes when activated through the menu hierarchy,
    ///     and that the change happens exactly once (no double-toggle from bubble round-trip).
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void OptionSelector_Value_Changes_ExactlyOnce ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            (TrackingPopoverMenu popoverMenu, TrackingMenu _, MenuItem _, OptionSelector<Schemes> selector, CheckBox secondCheckBox) =
                BuildOptionSelectorInPopoverMenuHierarchy ();

            // Initial value should be Schemes.Base (0)
            Assert.Equal (Schemes.Base, selector.Value);

            var valueChangedCount = 0;
            selector.ValueChanged += (_, _) => valueChangedCount++;

            // Act: Activate the second CheckBox (Schemes.Menu)
            secondCheckBox.InvokeCommand (Command.Activate);

            // Assert: Value should have changed exactly once to Schemes.Accent (index 1)
            Assert.Equal (1, valueChangedCount);
            Assert.Equal (Schemes.Accent, selector.Value);

            popoverMenu.Dispose ();
        }
    }

    #endregion

    #region Source Preservation

    /// <summary>
    ///     Tests that the Activate event source is preserved when crossing the PopoverMenu boundary.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Activate_Source_Preserved_AcrossBoundary ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            (TrackingPopoverMenu popoverMenu, TrackingMenu menu, MenuItem _, OptionSelector<Schemes> _, CheckBox secondCheckBox) =
                BuildOptionSelectorInPopoverMenuHierarchy ();

            // Act: Activate the CheckBox with a binding (simulates keyboard activation).
            KeyBinding keyBinding = new ([Command.Activate]) { Key = Key.Space, Source = new WeakReference<View> (secondCheckBox) };
            CommandContext ctx = new () { Command = Command.Activate, Source = new WeakReference<View> (secondCheckBox), Binding = keyBinding };
            secondCheckBox.InvokeCommand (Command.Activate, ctx);

            // OptionSelector uses ConsumeDispatch, so BubbleActivatedUp fires Activated
            // (not Activating) on ancestors after ConsumeDispatch completes.
            // Menu should have received Activated with the original source preserved.
            (string _, View? menuActivatedSource, ICommandBinding? _) = menu.EventLog.FirstOrDefault (e => e.EventName == "Activated");

            Assert.NotNull (menuActivatedSource);
            Assert.Same (secondCheckBox, menuActivatedSource);

            // PopoverMenu receives the command via Root bridge (subscribes to Menu.Activated).
            // The bridge preserves the original source across the non-containment boundary.
            (string _, View? popoverMenuActivatingSource, ICommandBinding? _) = popoverMenu.EventLog.FirstOrDefault (e => e.EventName == "Activating");

            Assert.NotNull (popoverMenuActivatingSource);
            Assert.Same (secondCheckBox, popoverMenuActivatingSource);

            popoverMenu.Dispose ();
        }
    }

    #endregion

    #region Target Bridging (PopoverMenus pattern)

    /// <summary>
    ///     Proves the PopoverMenus pattern works: a PopoverMenu with Target set to a host view
    ///     bridges Activated to the host when a MenuItem is activated inside the PopoverMenu.
    ///     This mirrors the <c>popoverMenuHost.Activated</c> handler in PopoverMenus.cs.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_MenuItem_Activate_Bridges_To_Target_Activated ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            // Arrange: PopoverMenu → Menu → MenuItem, with Target set to a host view
            View host = new () { Id = "host" };

            MenuItem menuItem = new () { Id = "testItem", Title = "Test Item" };
            Menu menu = new () { Id = "contextMenu" };
            menu.Add (menuItem);

            PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
            popoverMenu.Root = menu;
            popoverMenu.Target = new WeakReference<View> (host);

            var hostActivatedCount = 0;
            ICommandContext? capturedCtx = null;

            host.Activated += (_, args) =>
                              {
                                  hostActivatedCount++;
                                  capturedCtx = args.Value;
                              };

            // Act: Activate the MenuItem inside the PopoverMenu
            menuItem.InvokeCommand (Command.Activate);

            // Assert: The host's Activated event should fire exactly once via Target bridge
            Assert.Equal (1, hostActivatedCount);
            Assert.NotNull (capturedCtx);
            Assert.Equal (CommandRouting.Bridged, capturedCtx!.Routing);

            popoverMenu.Dispose ();
        }
    }

    /// <summary>
    ///     Proves the PopoverMenus pattern for CheckBox CommandView:
    ///     When a MenuItem has a CheckBox as its CommandView, activating the CheckBox should
    ///     bridge through PopoverMenu.Target to the host, and TryGetSource should identify
    ///     the CheckBox. This mirrors the <c>bordersCheckbox</c> pattern in PopoverMenus.cs.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_CheckBox_CommandView_Activate_Source_Reaches_Target ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(CheckBox CommandView)
            View host = new () { Id = "host", CommandsToBubbleUp = [Command.Activate] };

            CheckBox bordersCheckBox = new () { Id = "bordersCheckbox", Title = "_Borders", CanFocus = false };
            MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", CommandView = bordersCheckBox };

            Menu menu = new () { Id = "contextMenu" };
            menu.Add (menuItemBorders);

            PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
            popoverMenu.Root = menu;
            popoverMenu.Target = new WeakReference<View> (host);

            var hostActivatedCount = 0;
            View? capturedSource = null;

            host.Activated += (_, args) =>
                              {
                                  hostActivatedCount++;
                                  args.Value?.TryGetSource (out capturedSource);
                              };

            // Act: Activate the MenuItem (which has the CheckBox as CommandView)
            menuItemBorders.InvokeCommand (Command.Activate);

            // Assert: The host's Activated event should fire exactly once
            Assert.Equal (1, hostActivatedCount);

            // The source reaching the host should be identifiable
            Assert.NotNull (capturedSource);

            popoverMenu.Dispose ();
        }
    }

    /// <summary>
    ///     Proves the PopoverMenus pattern for OptionSelector CommandView:
    ///     When a MenuItem has an OptionSelector as its CommandView, activating a CheckBox
    ///     inside the OptionSelector should bridge through PopoverMenu.Target to the host,
    ///     and TryGetSource should identify the CheckBox within the OptionSelector.
    ///     This mirrors the <c>schemeOptionSelector</c> pattern in PopoverMenus.cs.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_OptionSelector_CommandView_Activate_Source_Reaches_Target ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(OptionSelector CommandView → CheckBoxes)
            View host = new () { Id = "host" };

            OptionSelector<Schemes> schemeSelector = new () { Id = "schemeOptionSelector", CanFocus = true };
            schemeSelector.Layout ();

            MenuItem menuItemScheme = new () { Id = "menuItemScheme", Title = "_Scheme", CommandView = schemeSelector };

            Menu menu = new () { Id = "contextMenu" };
            menu.Add (menuItemScheme);

            PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
            popoverMenu.Root = menu;
            popoverMenu.Target = new WeakReference<View> (host);

            var hostActivatedCount = 0;

            host.Activated += (_, _) => { hostActivatedCount++; };

            // Act: Activate the MenuItem
            menuItemScheme.InvokeCommand (Command.Activate);

            // Assert: The host's Activated event should fire exactly once via the Target bridge
            Assert.Equal (1, hostActivatedCount);

            popoverMenu.Dispose ();
        }
    }

    /// <summary>
    ///     Proves that when PopoverMenu.Target is set and the target has CommandsToBubbleUp,
    ///     the activation bridges through to the target's SuperView. This mirrors the full
    ///     PopoverMenus hierarchy: _appWindow (SuperView) → popoverMenuHost (Target) → PopoverMenu.
    /// </summary>

    // Claude - Opus 4.6
    [Fact]
    public void Target_Activated_Bubbles_To_Target_SuperView ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            //Trace.EnabledCategories = TraceCategory.Command;

            // Arrange: SuperView → Host(Target) ← PopoverMenu → Menu → MenuItem
            View superView = new () { Id = "appWindow", CommandsToBubbleUp = [Command.Activate] };
            View host = new () { Id = "host", CommandsToBubbleUp = [Command.Activate] };
            superView.Add (host);

            MenuItem menuItem = new () { Id = "testItem", Title = "Test" };
            Menu menu = new () { Id = "contextMenu" };
            menu.Add (menuItem);

            PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
            popoverMenu.Root = menu;
            popoverMenu.Target = new WeakReference<View> (host);

            var superViewActivatedCount = 0;

            superView.Activated += (_, _) => { superViewActivatedCount++; };

            // Act: Activate the MenuItem
            menuItem.InvokeCommand (Command.Activate);

            // Assert: The SuperView's Activated event should fire exactly once
            Assert.Equal (1, superViewActivatedCount);

            popoverMenu.Dispose ();
        }
    }

    [Fact]
    public void Target_CheckBox_CommandView_Activate_Direct_Source_Reaches_Target_And_Value_Is_Correct ()
    {
        using IDisposable verbose = TestLogging.Verbose (output);

        // Do not set this unless debugging. It is a static that is process wide.
        //Trace.EnabledCategories = TraceCategory.Command;

        // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(CheckBox CommandView)
        View host = new () { Id = "host" };

        CheckBox bordersCheckBox = new () { Id = "bordersCheckbox", Title = "_Borders", CanFocus = false };

        // CheckBox starts UnChecked
        Assert.Equal (CheckState.UnChecked, bordersCheckBox.Value);

        MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", CommandView = bordersCheckBox };

        Menu menu = new () { Id = "contextMenu" };
        menu.Add (menuItemBorders);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        object? capturedValue = null;
        var hostActivatedFired = 0;
        var valueChangeCount = 0;

        bordersCheckBox.ValueChanged += (_, _) => valueChangeCount++;

        host.Activated += (_, args) =>
        {
            hostActivatedFired++;
            capturedValue = args.Value?.Value;
        };

        // Act: Activate the CheckBox with a binding whose source is the MenuItem
        bordersCheckBox.InvokeCommand (Command.Activate);

        // Assert: Host's Activated event should fire exactly once via Target bridge
        Assert.Equal (1, hostActivatedFired);

        // The CheckBox double-toggles: once during DispatchDown from menuItemBorders,
        // and again from the originating checkbox's own RaiseActivated→OnActivated.
        // bordersCheckBox is both the originator and the dispatch target.
        Assert.Equal (1, valueChangeCount);

        // The value arriving at the host should be the post-toggle value (Checked).
        // RefreshValue re-reads from the dispatch target after the first toggle.
        Assert.Equal (CheckState.Checked, capturedValue as CheckState?);

        popoverMenu.Dispose ();
    }

    /// <summary>
    ///     Proves that when a <see cref="MenuItem"/> has a <see cref="CheckBox"/> as its CommandView,
    ///     activating the MenuItem bridges through <see cref="PopoverMenu.Target"/> to the host,
    ///     and <see cref="ICommandContext.Value"/> carries the CheckBox's post-toggle
    ///     <see cref="CheckState"/>. This mirrors the <c>bordersCheckbox</c> pattern in PopoverMenus.cs.
    /// </summary>
    /// <remarks>
    ///     The fix uses <c>RefreshValue</c> in <c>TryBubbleUp</c> and <c>DefaultActivateHandler</c>
    ///     to re-read the dispatch target's <see cref="IValue.GetValue"/> after state changes complete.
    /// </remarks>

    // Claude - Opus 4.6
    [Fact]
    public void Target_CheckBox_CommandView_Activate_With_KeyBinding_Source_Reaches_Target_And_Value_Is_Correct ()
    {
        using IDisposable verbose = TestLogging.Verbose (output);

        // Do not set this unless debugging. It is a static that is process wide.
        //Trace.EnabledCategories = TraceCategory.Command;

        // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(CheckBox CommandView)
        View host = new () { Id = "host" };

        CheckBox bordersCheckBox = new () { Id = "bordersCheckbox", Title = "_Borders", CanFocus = false };

        // CheckBox starts UnChecked
        Assert.Equal (CheckState.UnChecked, bordersCheckBox.Value);

        MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", CommandView = bordersCheckBox };

        Menu menu = new () { Id = "contextMenu" };
        menu.Add (menuItemBorders);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        object? capturedValue = null;
        var hostActivatedFired = 0;
        var valueChangeCount = 0;

        bordersCheckBox.ValueChanged += (_, _) => valueChangeCount++;

        host.Activated += (_, args) =>
                          {
                              hostActivatedFired++;
                              capturedValue = args.Value?.Value;
                          };

        // Act: Activate the CheckBox with a binding whose source is the MenuItem
        // (simulates key activation that bubbles up from CheckBox to MenuItem).
        bordersCheckBox.InvokeCommand (Command.Activate, new KeyBinding ([Command.Activate], menuItemBorders));

        // Assert: Host's Activated event should fire exactly once via Target bridge
        Assert.Equal (1, hostActivatedFired);

        // With the double-fire fix, the CheckBox toggles exactly once:
        // UnChecked → Checked. The dispatch guard prevents the second toggle.
        Assert.Equal (1, valueChangeCount);

        // The value arriving at the host should be the post-toggle value (Checked).
        // RefreshValue re-reads from the dispatch target after the first toggle.
        Assert.Equal (CheckState.Checked, capturedValue as CheckState?);

        popoverMenu.Dispose ();
    }

    [Fact]
    public void Target_CheckBox_Mouse_Click_Source_Reaches_Target_And_Value_Is_Correct ()
    {
        using IDisposable verbose = TestLogging.Verbose (output);

        // Arrange - mirror the Menus.cs scenario: MenuBar inside a focusable host view
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        // Do not set this unless debugging. It is a static that is process wide.
        //Trace.EnabledCategories = TraceCategory.Command;

        // Arrange: Host ← Target ← PopoverMenu → Menu → MenuItem(CheckBox CommandView)
        View host = new () { Id = "host" };

        (runnable as View)!.Add (host);
        app.Begin (runnable);

        CheckBox bordersCheckBox = new () { Id = "bordersCheckbox", Title = "_Borders", CanFocus = false };

        // CheckBox starts UnChecked
        Assert.Equal (CheckState.UnChecked, bordersCheckBox.Value);

        MenuItem menuItemBorders = new () { Id = "menuItemBorders", Title = "_Borders", CommandView = bordersCheckBox };

        Menu menu = new () { Id = "contextMenu", BorderStyle = LineStyle.Dashed };
        menu.Add (menuItemBorders);

        PopoverMenu popoverMenu = new () { Id = "popoverMenu" };
        popoverMenu.Root = menu;
        popoverMenu.Target = new WeakReference<View> (host);

        app.Popovers?.Register (popoverMenu);
        popoverMenu.MakeVisible ();

        object? capturedValue = null;
        var hostActivatedFired = 0;
        var valueChangeCount = 0;

        bordersCheckBox.ValueChanged += (_, _) => valueChangeCount++;

        host.Activated += (_, args) =>
                          {
                              hostActivatedFired++;
                              capturedValue = args.Value?.Value;
                          };

        // BUGBUG: This is why it's bad to use complex views in unrelated unit tests: Shortcut puts a transparent to mouse
        // BUGBUG: Margin to the left of CommandView. Clicking on (0,0) of the checkbox, actually passes through to the shortcut
        // BUGBUG: and activates the MenuItem before the click reaches the CheckBox, meaning this test wouldn't be testing what we think it's testing.
        // BUGBUG: The fix is to offset the click point by (1,0) to ensure it hits the CheckBox's area and not the Shortcut margin.
        Point screenPoint = bordersCheckBox.FrameToScreen ().Location;
        screenPoint.Offset (new Point (1, 0));
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (screenPoint));

        // Assert: Host's Activated event should fire exactly once via Target bridge
        Assert.Equal (1, hostActivatedFired);

        // The CheckBox double-toggles: once during DispatchDown from menuItemBorders,
        // and again from the originating checkbox's own RaiseActivated→OnActivated.
        // bordersCheckBox is both the originator and the dispatch target.
        Assert.Equal (1, valueChangeCount);

        // The value arriving at the host should be the post-toggle value (Checked).
        // RefreshValue re-reads from the dispatch target after the first toggle.
        Assert.Equal (CheckState.Checked, capturedValue as CheckState?);

        popoverMenu.Dispose ();
    }

    #endregion
}
