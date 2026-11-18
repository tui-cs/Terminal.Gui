using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using UnitTests.Parallelizable;
using Terminal.Gui.Tracing;
using UnitTests;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests
{
    /// <summary>Test view that exposes AddCommand publicly for testing TargetView scenarios.</summary>
    private class TestTargetView : View
    {
        public void RegisterCommand (Command command, Func<bool?> impl) => AddCommand (command, impl);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox with CanFocus=false CAN change state when
    ///     Command.Activate is invoked directly on it.
    ///     CanFocus only controls keyboard focus, not the ability to change state.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_Changes_State_On_Direct_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=false SHOULD change state
        // CanFocus only controls keyboard focus, not state changes
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a CheckBox with CanFocus=true DOES change state when
    ///     Command.Activate is invoked directly on it.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_Changes_State_On_Direct_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Directly invoke Command.Activate on the CheckBox
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox with CanFocus=true SHOULD change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that direct InvokeCommand(Activate) on Shortcut does NOT forward to CommandView.
    ///     OnActivating only forwards to CommandView when the activation comes from:
    ///     1. IsFromCommandView - the command originated from CommandView
    ///     2. IsBindingFromShortcut - the command came via a keyboard/mouse binding
    ///     Direct InvokeCommand has neither, so CommandView is not activated.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_False_Direct_InvokeCommand_Does_Not_Change_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.False (checkBox.CanFocus);

        // Act - Invoke Command.Activate directly on the Shortcut (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox does NOT change state because direct InvokeCommand
        // doesn't go through OnActivating's forwarding logic
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that direct InvokeCommand(Activate) on Shortcut does NOT forward to CommandView,
    ///     even when CommandView.CanFocus=true.
    ///     OnActivating only forwards to CommandView when the activation comes via proper bindings.
    /// </summary>
    [Fact]
    public void CheckBox_CanFocus_True_Direct_InvokeCommand_Does_Not_Change_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = true };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.True (checkBox.CanFocus);

        // Act - Invoke Command.Activate directly on the Shortcut (no binding)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - CheckBox does NOT change state because direct InvokeCommand
        // doesn't go through OnActivating's forwarding logic
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that a mouse click on a CheckBox CommandView changes its state.
    ///     CheckBox binds LeftButtonClicked to Command.Activate (overriding the base
    ///     View's LeftButtonReleased binding to prevent double activation on double-click).
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_MouseRelease_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        // BUGBUG: This test tests nothing.
        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Verify CheckBox has the expected mouse binding for LeftButtonClicked
        // (CheckBox overrides the default LeftButtonReleased with LeftButtonClicked to prevent double activation)
        Assert.True (checkBox.MouseBindings.TryGet (MouseFlags.LeftButtonClicked, out MouseBinding binding));
        Assert.Contains (Command.Activate, binding.Commands);

        // Act - Simulate a mouse click by invoking the bound command directly
        checkBox.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that invoking Command.Activate directly on the CheckBox CommandView
    ///     (simulating what a mouse press should do) changes its state.
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_Direct_Activate_Changes_State ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Directly invoke Command.Activate on the CheckBox (what mouse press should trigger)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - CheckBox should change state
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    [Fact]
    public void CommandView_Command_Activate_Bubbles_To_Shortcut ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Fact]
    public void CommandView_Command_Activate_Bubbles_To_Shortcut_SuperView ()
    {
        // Arrange
        View? superView = new () { CanFocus = true };
        superView.CommandsToBubbleUp = [Command.Activate];

        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        superView.Add (shortcut);

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var shortcutActivatedRaised = 0;
        shortcut.Activated += (_, _) => shortcutActivatedRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var commandViewActivatedRaised = 0;
        testCommandView.Activated += (_, _) => commandViewActivatedRaised++;

        var superViewActivatingCount = 0;
        superView.Activating += (_, _) => superViewActivatingCount++;

        var superViewActivatedCount = 0;
        superView.Activated += (_, _) => superViewActivatedCount++;

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, commandViewActivatingRaised);
        Assert.Equal (1, commandViewActivatedRaised);
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, shortcutActivatedRaised);
        Assert.Equal (1, superViewActivatingCount);
        Assert.Equal (1, superViewActivatedCount);
    }

    [Fact]
    public void CommandView_Command_Accept_Forwards_To_Accepting ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutAcceptingRaised = 0;
        shortcut.Accepting += (_, _) => { shortcutAcceptingRaised++; };

        var commandViewAcceptingRaised = 0;
        testCommandView.Accepting += (_, _) => { commandViewAcceptingRaised++; };

        // Act - Invoke Command.HotKey directly on CheckBox
        testCommandView.InvokeCommand (Command.Accept);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutAcceptingRaised);
        Assert.Equal (1, commandViewAcceptingRaised);
        Assert.Equal (0, shortcutActivatingRaised);
        Assert.Equal (0, commandViewActivatingRaised);
    }

    [Fact]
    public void Command_Activate_Direct_InvokeCommand_Raises_Activating_Once ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        shortcut.InvokeCommand (Command.Activate);

        // Assert - Shortcut.Activating should be raised exactly once
        Assert.Equal (1, shortcutActivatingCount);
    }

    [Fact]
    public void CommandView_Command_HotKey_Ignored_Raised_By_Shortcut ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutHandlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { shortcutHandlingHotKeyFired++; };

        var commandViewHandlingHotKeyFired = 0;
        testCommandView.HandlingHotKey += (_, _) => { commandViewHandlingHotKeyFired++; };

        // Act - Invoke Command.HotKey directly on CommandView
        testCommandView.InvokeCommand (Command.HotKey);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (0, shortcutHandlingHotKeyFired);
        Assert.Equal (1, commandViewHandlingHotKeyFired);
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Fact]
    public void Command_Activate_Raises_Activating_Only ()
    {
        Shortcut shortcut = new ();
        var activatingFired = 0;

        shortcut.Activating += (_, _) => activatingFired++;

        var acceptingFired = 0;
        shortcut.Accepting += (_, _) => acceptingFired++;

        var handlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => handlingHotKeyFired++;

        shortcut.InvokeCommand (Command.Activate);

        Assert.Equal (1, activatingFired);
        Assert.Equal (0, acceptingFired);
        Assert.Equal (0, handlingHotKeyFired);

        shortcut.Dispose ();
    }

    [Fact]
    public void Command_Accept_Raises_Accepting_Only ()
    {
        using Shortcut shortcut = new ();
        shortcut.Title = "Test";
        shortcut.Key = Key.T.WithCtrl;
        var activatingFired = false;

        shortcut.Activating += (_, _) => { activatingFired = true; };

        var acceptingFired = false;
        shortcut.Accepting += (_, _) => { acceptingFired = true; };

        var handlingHotKeyFired = false;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired = true; };

        shortcut.InvokeCommand (Command.Accept);

        Assert.False (activatingFired);
        Assert.True (acceptingFired);
        Assert.False (handlingHotKeyFired);
    }

    [Fact]
    public void Command_Accept_Direct_InvokeCommand_Raises_Accepting_Once ()
    {
        // Arrange
        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        shortcut.InvokeCommand (Command.Accept);

        Assert.Equal (1, shortcutAcceptingCount);
    }

    [Fact]
    public void Command_HotKey_Raises_HandlingHotKey_Then_Activating ()
    {
        using Shortcut shortcut = new ();
        var activatingFired = 0;

        shortcut.Activating += (_, _) => { activatingFired++; };

        var acceptingFired = 0;
        shortcut.Accepting += (_, _) => { acceptingFired++; };

        var handlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { handlingHotKeyFired++; };

        shortcut.InvokeCommand (Command.HotKey);

        Assert.Equal (1, handlingHotKeyFired);
        Assert.Equal (1, activatingFired);
        Assert.Equal (0, acceptingFired);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that Accept does NOT invoke Activate. These are separate command paths per the design:
    ///     Accept = confirm/execute, Activate = change state.
    /// </summary>
    [Fact]
    public void Accept_Does_Not_Invoke_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Invoke Accept directly on the Shortcut
        shortcut.InvokeCommand (Command.Accept);

        // Assert - Accepting fires, Activating does NOT fire, CheckBox state unchanged
        Assert.Equal (1, shortcutAcceptingCount);
        Assert.Equal (0, shortcutActivatingCount);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when CommandView's Activate bubbles up to Shortcut, the Shortcut
    ///     returns false (not handled) so the originating CommandView can complete its own
    ///     activation (e.g., CheckBox toggles state in OnActivated).
    /// </summary>
    [Fact]
    public void CheckBox_CommandView_Activate_Bubbles_Up_And_CheckBox_Toggles ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox };

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutActivatedCount = 0;
        shortcut.Activated += (_, _) => shortcutActivatedCount++;

        var checkBoxActivatingCount = 0;
        checkBox.Activating += (_, _) => checkBoxActivatingCount++;

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Invoke Activate directly on the CheckBox (simulates what happens when
        // CheckBox processes a command internally, e.g., from mouse click or HotKey)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Both Activating events fire once, and CheckBox toggles state
        Assert.Equal (1, checkBoxActivatingCount);
        Assert.Equal (1, shortcutActivatingCount);
        Assert.Equal (1, shortcutActivatedCount);
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    [Fact]
    public void Action_Invoked_From_Just_Activate_And_Not_Accept ()
    {
        // Arrange
        var actionCount = 0;

        Shortcut shortcut = new () { Key = Key.F5, Title = "Test", Action = () => actionCount++ };

        // Act 1 - Activate
        shortcut.InvokeCommand (Command.Activate);
        Assert.Equal (1, actionCount);

        // Act 2 - Accept
        shortcut.InvokeCommand (Command.Accept);
        Assert.Equal (1, actionCount);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that <see cref="Shortcut.TargetView"/> and <see cref="Shortcut.Command"/> properties
    ///     exist on Shortcut and can be set/get correctly.
    /// </summary>
    [Fact]
    public void TargetView_And_Command_Properties_Default_And_Set ()
    {
        // Arrange
        using Shortcut shortcut = new ();

        // Assert defaults
        Assert.Null (shortcut.TargetView);
        Assert.Equal (Command.NotBound, shortcut.Command);

        // Act
        View target = new () { Title = "Target" };
        shortcut.TargetView = target;
        shortcut.Command = Command.Save;

        // Assert
        Assert.Same (target, shortcut.TargetView);
        Assert.Equal (Command.Save, shortcut.Command);

        target.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that setting <see cref="Shortcut.Command"/> auto-populates Title and HelpText
    ///     from GlobalResources when they are empty.
    /// </summary>
    [Fact]
    public void Command_Property_Does_Not_Overwrite_Existing_Title ()
    {
        // Arrange
        using Shortcut shortcut = new ();
        shortcut.Title = "My Title";
        shortcut.HelpText = "My Help";

        // Act — setting Command should NOT overwrite existing Title/HelpText
        shortcut.Command = Command.Save;

        // Assert
        Assert.Equal ("My Title", shortcut.Title);
        Assert.Equal ("My Help", shortcut.HelpText);
        Assert.Equal (Command.Save, shortcut.Command);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that Accept does NOT invoke TargetView when Command is NotBound.
    /// </summary>
    [Fact]
    public void Accept_Does_Not_Invoke_TargetView_When_Command_NotBound ()
    {
        // Arrange
        var commandInvoked = false;
        TestTargetView target = new () { Title = "Target" };

        target.RegisterCommand (Command.Save,
                                () =>
                                {
                                    commandInvoked = true;

                                    return true;
                                });

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F5;
        shortcut.Title = "Test";
        shortcut.TargetView = target; // Command is default (NotBound)

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert — Command is NotBound so TargetView should NOT be invoked
        Assert.False (commandInvoked);

        target.Dispose ();
    }

    [Fact]
    public void Accept_Does_Not_Invoke_Action_Or_TargetView ()
    {
        // Arrange
        var actionFired = 0;
        var commandInvoked = 0;

        TestTargetView target = new () { Title = "Target" };

        target.RegisterCommand (Command.Save,
                                () =>
                                {
                                    commandInvoked++;

                                    return true;
                                });
        target.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        using Shortcut shortcut = new ();
        shortcut.Key = Key.S.WithCtrl;
        shortcut.Title = "Save";
        shortcut.TargetView = target;
        shortcut.Command = Command.Save;
        shortcut.Action = () => actionFired++;

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert
        Assert.Equal (0, actionFired);
        Assert.Equal (0, commandInvoked);

        target.Dispose ();
    }

    [Fact]
    public void Activate_Does_Invoke_TargetView ()
    {
        // Arrange
        var commandInvoked = false;
        TestTargetView target = new () { Title = "Target" };

        target.RegisterCommand (Command.Save,
                                () =>
                                {
                                    commandInvoked = true;

                                    return true;
                                });
        target.HotKeyBindings.Add (Key.S.WithCtrl, Command.Save);

        using Shortcut shortcut = new ();
        shortcut.Key = Key.S.WithCtrl;
        shortcut.Title = "Save";
        shortcut.TargetView = target;
        shortcut.Command = Command.Save;

        // Act — Activate, not Accept
        shortcut.InvokeCommand (Command.Activate);

        // Assert — TargetView should NOT be invoked on Activate
        Assert.True (commandInvoked);

        target.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when TargetView is null but Key is valid and Command is set,
    ///     OnAccepted attempts to invoke application-level key bindings. We verify this
    ///     indirectly by checking that the Shortcut's key press triggers the expected path.
    /// </summary>
    [Fact]
    public void Accept_With_Command_And_No_TargetView_Does_Not_Throw ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Shortcut shortcut = new ()
        {
            Key = Key.S.WithCtrl, Title = "Save", Command = Command.Save

            // No TargetView — will attempt app-level key binding invocation
        };

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        // Act — should not throw even though no app-level handler is registered
        Exception? ex = Record.Exception (() => shortcut.InvokeCommand (Command.Accept));

        // Assert
        Assert.Null (ex);
    }

    [Fact]
    public void Accept_Without_TargetView_Or_Command_Does_Not_Fire_Action ()
    {
        // Arrange
        var actionFired = false;

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F5;
        shortcut.Title = "Test";
        shortcut.Action = () => actionFired = true;

        // Act
        shortcut.InvokeCommand (Command.Accept);

        // Assert — only Action fires, no TargetView or app-level invocation
        Assert.False (actionFired);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when a CheckBox CommandView's Activate bubbles up to Shortcut,
    ///     the Shortcut's Action sees the UPDATED CheckBox value (after toggle), not the stale value.
    ///     This is the regression test for the deferred RaiseActivated fix.
    /// </summary>
    [Fact]
    public void Action_Sees_Updated_CheckBox_Value_On_BubbleUp_Activate ()
    {
        // Arrange
        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        CheckState? capturedValue = null;

        Shortcut shortcut = new () { Key = Key.T, CommandView = checkBox, Action = () => capturedValue = checkBox.Value };

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        // Act - Invoke Activate directly on the CheckBox (simulates bubble-up from click)
        checkBox.InvokeCommand (Command.Activate);

        // Assert - Action should see the NEW value (Checked), not the old value (UnChecked)
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.Checked, capturedValue);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies correct event ordering for compound views in Shortcut when activation
    ///     bubbles up from the CommandView (e.g., CheckBox click).
    ///     Expected order:
    ///     1. CheckBox.Activating
    ///     2. Shortcut.Activating (from bubble-up during RaiseActivating)
    ///     3. CheckBox.Activated (from CheckBox.RaiseActivated → OnActivated toggles state)
    ///     4. Shortcut.Activated (from BubbleActivatedUp, after CheckBox completes)
    ///     Key: Shortcut.Activated fires AFTER CheckBox.Activated, so Action sees updated state.
    /// </summary>
    [Fact]
    public void BubbleUp_Activate_Event_Ordering_CommandView_Completes_Before_Shortcut_Activated ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            // Do not set this unless debugging. It is a static that is process wide.
            ////Trace.EnabledCategories = TraceCategory.Command;

            // Arrange
            CheckBox checkBox = new () { Id = "checkBox", Title = "_Toggle", CanFocus = false };

            Shortcut shortcut = new () { Id = "shortcut", Key = Key.T, CommandView = checkBox };

            List<string> eventLog = [];
            CheckState? valueAtShortcutActivated = null;

            checkBox.Activating += (_, _) => eventLog.Add ("CheckBox.Activating");
            checkBox.Activated += (_, _) => eventLog.Add ("CheckBox.Activated");

            shortcut.Activating += (_, _) => eventLog.Add ("Shortcut.Activating");

            shortcut.Activated += (_, _) =>
                                  {
                                      valueAtShortcutActivated = checkBox.Value;
                                      eventLog.Add ("Shortcut.Activated");
                                  };

            // Act - Invoke Activate directly on the CheckBox (simulates bubble-up from click)
            checkBox.InvokeCommand (Command.Activate);

            // Assert - Verify ordering:
            // CheckBox.Activated fires first (state change in OnActivated), then
            // Shortcut.Activated fires via BubbleActivatedUp after CheckBox completes.
            Assert.Equal (4, eventLog.Count);
            Assert.Equal ("CheckBox.Activating", eventLog [0]);
            Assert.Equal ("Shortcut.Activating", eventLog [1]);
            Assert.Equal ("CheckBox.Activated", eventLog [2]);
            Assert.Equal ("Shortcut.Activated", eventLog [3]);

            // CheckBox.OnActivated toggled state BEFORE Shortcut.Activated fired
            Assert.Equal (CheckState.Checked, checkBox.Value);
            Assert.Equal (CheckState.Checked, valueAtShortcutActivated);
        }
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that cancelling activation (Activating.Handled=true) after a prior dispatch
    ///     does NOT fire Activated. This guards against stale _lastDispatchOccurred: if the flag
    ///     is not reset before RaiseActivating, a prior dispatch would cause RaiseActivated to
    ///     fire even though the activation was cancelled.
    /// </summary>
    [Fact]
    public void Activate_Cancelled_After_Dispatch_Does_Not_Fire_Activated ()
    {
        using IDisposable logging = TestLogging.Verbose (output);
        //Trace.EnabledCategories = TraceCategory.Command;

        CheckBox cb = new () { Id = "cb", Title = "_Test" };
        Shortcut shortcut = new () { Id = "shortcut", Key = Key.T, CommandView = cb };
        var activatedCount = 0;
        shortcut.Activated += (_, _) => activatedCount++;

        // First: normal activation with binding (triggers dispatch, which sets _lastDispatchOccurred)
        KeyBinding kb = new ([Command.Activate], Key.Space, shortcut);
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (shortcut), kb);
        shortcut.InvokeCommand (Command.Activate, ctx);
        int afterFirst = activatedCount;

        // Second: cancel activation via Activating handler
        shortcut.Activating += (_, args) => args.Handled = true;
        shortcut.InvokeCommand (Command.Activate, ctx);

        // Activated should NOT have fired again (cancelled)
        Assert.Equal (afterFirst, activatedCount);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that a programmatic InvokeCommand on Shortcut (no binding, dispatch skipped)
    ///     does not prevent a subsequent user click from firing Action.
    /// </summary>
    [Fact]
    public void Shortcut_Programmatic_Activate_Then_User_Click_Both_Fire_Action ()
    {
        using IDisposable logging = TestLogging.Verbose (output);
        //Trace.EnabledCategories = TraceCategory.Command;

        var actionCount = 0;
        CheckBox cb = new () { Id = "cb", Title = "_Test" };
        Shortcut shortcut = new () { Key = Key.T, CommandView = cb, Action = () => actionCount++ };

        // Programmatic invoke (no binding → dispatch skipped, Action fires via OnActivated)
        shortcut.InvokeCommand (Command.Activate);
        Assert.Equal (1, actionCount);

        // Simulate user click (with binding → dispatch runs → CheckBox activates → BubbleActivatedUp fires)
        KeyBinding kb = new ([Command.Activate], Key.Space, cb);
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (cb), kb);
        cb.InvokeCommand (Command.Activate, ctx);

        Assert.Equal (2, actionCount);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies correct event ordering for compound views in Shortcut when activation
    ///     is triggered via DispatchDown (e.g., clicking on Shortcut's HelpView area).
    ///     The framework dispatches DispatchDown after OnActivating but before Activated.
    ///     Expected order:
    ///     1. Shortcut.Activating (fires from RaiseActivating before framework dispatch)
    ///     2. CheckBox.Activating (from DispatchDown during framework dispatch)
    ///     3. CheckBox.Activated (from CheckBox.RaiseActivated inside DispatchDown)
    ///     4. Shortcut.Activated (from Shortcut's own RaiseActivated after dispatch completes)
    /// </summary>
    [Fact]
    public void BubbleDown_Activate_Event_Ordering_With_Binding_Source ()
    {
        using (TestLogging.BindTo (output, LogLevel.Warning))
        {
            //Trace.EnabledCategories = TraceCategory.Command;

            // Arrange
            CheckBox checkBox = new () { Id = "checkBox", Title = "_Toggle", CanFocus = false };

            Shortcut shortcut = new () { Id = "shortcut", Key = Key.T, CommandView = checkBox };

            List<string> eventLog = [];
            CheckState? valueAtShortcutActivated = null;

            checkBox.Activating += (_, _) => eventLog.Add ("CheckBox.Activating");
            checkBox.Activated += (_, _) => eventLog.Add ("CheckBox.Activated");

            shortcut.Activating += (_, _) => eventLog.Add ("Shortcut.Activating");

            shortcut.Activated += (_, _) =>
                                  {
                                      valueAtShortcutActivated = checkBox.Value;
                                      eventLog.Add ("Shortcut.Activated");
                                  };

            // Act - Invoke Activate with a binding source (the Shortcut), which triggers DispatchDown
            // to the CommandView. This simulates what happens on HotKey press or mouse click on
            // the Shortcut's non-CommandView area (e.g., HelpView or KeyView).
            KeyBinding binding = new ([Command.Activate], Key.T, shortcut);
            CommandContext ctx = new (Command.Activate, new WeakReference<View> (shortcut), binding);
            shortcut.InvokeCommand (Command.Activate, ctx);

            // Assert - Shortcut.Activating fires first (notification before dispatch),
            // then DispatchDown fires CommandView events. Shortcut.Activated fires after
            // DispatchDown completes (synchronous dispatch ensures state is updated).
            Assert.Equal (4, eventLog.Count);
            Assert.Equal ("Shortcut.Activating", eventLog [0]);
            Assert.Equal ("CheckBox.Activating", eventLog [1]);
            Assert.Equal ("CheckBox.Activated", eventLog [2]);
            Assert.Equal ("Shortcut.Activated", eventLog [3]);

            // CheckBox should have toggled
            Assert.Equal (CheckState.Checked, checkBox.Value);
            Assert.Equal (CheckState.Checked, valueAtShortcutActivated);
        }
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that when a FlagSelector is used as a Shortcut's CommandView, activating an
    ///     individual checkbox inside the FlagSelector (with a binding whose Source is the inner
    ///     checkbox) causes exactly one ValueChanged and one Shortcut.Activating. Before the
    ///     IsWithinCommandView fix, the Shortcut would DispatchDown back to the FlagSelector
    ///     (because Source was the inner checkbox, not the FlagSelector itself), causing duplicate
    ///     activations and multiple ValueChanged events.
    /// </summary>
    [Fact]
    public void FlagSelector_CommandView_SubView_Activate_Does_Not_Duplicate ()
    {
        using IDisposable logging = TestLogging.Verbose (output);
        //Trace.EnabledCategories = TraceCategory.Command;

        // Arrange
        FlagSelector flagSelector = new () { Id = "flagSelector", Values = [1, 2, 4], Labels = ["Flag1", "Flag2", "Flag3"] };

        Shortcut shortcut = new () { Id = "shortcut", Key = Key.F5, CommandView = flagSelector };

        // FlagSelector defaults Value to the first item (1) when Values is set.
        // Clear it so we can detect a single toggle.
        flagSelector.Value = null;

        var valueChangedCount = 0;
        flagSelector.ValueChanged += (_, _) => valueChangedCount++;

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutActivatedCount = 0;
        shortcut.Activated += (_, _) => shortcutActivatedCount++;

        var flagSelectorActivatingCount = 0;
        flagSelector.Activating += (_, _) => flagSelectorActivatingCount++;

        // Get the first checkbox inside the FlagSelector
        CheckBox firstCheckBox = flagSelector.SubViews.OfType<CheckBox> ().First ();

        // Act - Invoke Activate on the inner checkbox with a binding that has Source = the
        // checkbox. This simulates a mouse click, which creates a MouseBinding with Source
        // pointing to the clicked view. The binding is key: without it, HandleActivate's
        // `ctx.Binding is { Source: { } source }` check fails and DispatchDown is never called.
        KeyBinding binding = new ([Command.Activate], Key.Space, firstCheckBox);
        CommandContext ctx = new (Command.Activate, new WeakReference<View> (firstCheckBox), binding);
        firstCheckBox.InvokeCommand (Command.Activate, ctx);

        // Assert - Value changes exactly once. In the new design, the Activating event always fires
        // as a notification before the framework dispatch consumes the command. This means
        // FlagSelector.Activating fires (subscribers get a chance to cancel before dispatch),
        // but Shortcut.Activating doesn't fire (bubble doesn't reach Shortcut because FlagSelector
        // consumes during dispatch). Shortcut.Activated fires via the deferred path
        // (FlagSelector consumes via ConsumeDispatch → BubbleActivatedUp fires on Shortcut).
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (0, shortcutActivatingCount);
        Assert.Equal (1, shortcutActivatedCount);
        Assert.Equal (1, flagSelectorActivatingCount);

        // The checkbox should be checked (toggled once, not toggled twice back to unchecked)
        Assert.Equal (CheckState.Checked, firstCheckBox.Value);
        Assert.Equal (1, flagSelector.Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing Enter on a focused OptionSelector item inside a Shortcut
    ///     activates (changes Value) and accepts. The OptionSelector has CanFocus=true so
    ///     individual checkboxes can receive focus. The second option is focused and Enter
    ///     is pressed — Value should change from 0 to 1 and Activating should fire.
    /// </summary>
    [Fact]
    public void OptionSelector_CommandView_Enter_Activates_And_Accepts ()
    {
        using IDisposable logging = TestLogging.Verbose (output);
        //Trace.EnabledCategories = TraceCategory.Command;

        // Arrange
        OptionSelector optionSelector = new () { Id = "optionSelector", CanFocus = true, Labels = ["Option1", "Option2", "Option3"] };

        Shortcut shortcut = new () { Id = "shortcut", Key = Key.F6, CommandView = optionSelector };

        Assert.Equal (0, optionSelector.Value); // First option is active by default

        var valueChangedCount = 0;
        optionSelector.ValueChanged += (_, _) => valueChangedCount++;

        var selectorActivatingCount = 0;
        optionSelector.Activating += (_, _) => selectorActivatingCount++;

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        // Give the shortcut focus so the OptionSelector and its first checkbox get focus
        shortcut.SetFocus ();
        shortcut.Layout ();

        // Focus the second checkbox (Option2)
        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);

        // Act - Press Enter on the focused (but not selected) checkbox
        checkBoxes [1].NewKeyDownEvent (Key.Enter);

        // Assert - Per spec: Enter should Activate AND Accept
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (1, selectorActivatingCount);
        Assert.Equal (1, optionSelector.Value); // Should now be Option2
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that invoking Command.Accept directly on an OptionSelector (CommandView of a
    ///     Shortcut) activates the focused item and changes Value. This is the programmatic
    ///     equivalent of pressing Enter — Accept should also Activate the focused item.
    /// </summary>
    [Fact]
    public void OptionSelector_CommandView_Direct_Accept_Activates_Focused_Item ()
    {
        using IDisposable logging = TestLogging.Verbose (output);
        //Trace.EnabledCategories = TraceCategory.Command;

        // Arrange
        OptionSelector optionSelector = new () { Id = "optionSelector", CanFocus = true, Labels = ["Option1", "Option2", "Option3"] };

        Shortcut shortcut = new () { Id = "shortcut", Key = Key.F6, CommandView = optionSelector };

        Assert.Equal (0, optionSelector.Value); // First option is active by default

        var valueChangedCount = 0;
        optionSelector.ValueChanged += (_, _) => valueChangedCount++;

        var selectorActivatingCount = 0;
        optionSelector.Activating += (_, _) => selectorActivatingCount++;

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        // Give the shortcut focus so the OptionSelector and its first checkbox get focus
        shortcut.SetFocus ();
        shortcut.Layout ();

        // Focus the second checkbox (Option2)
        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);

        // Act - Invoke Accept directly on the OptionSelector
        optionSelector.InvokeCommand (Command.Accept);

        // Assert - Accept should also Activate the focused item
        Assert.Equal (1, valueChangedCount);
        Assert.Equal (1, selectorActivatingCount);
        Assert.Equal (1, optionSelector.Value); // Should now be Option2
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that the Command property setter is idempotent — setting the same value
    ///     twice does not re-trigger Title/HelpText logic.
    /// </summary>
    [Fact]
    public void Command_Property_Idempotent ()
    {
        // Arrange
        using Shortcut shortcut = new ();
        shortcut.Command = Command.Save;

        // Capture current title (may have been set from GlobalResources)
        string titleAfterFirst = shortcut.Title;

        // Manually set a different title
        shortcut.Title = "Custom Title";

        // Act — set Command to the same value again
        shortcut.Command = Command.Save;

        // Assert — Title should NOT be overwritten because the setter early-returns
        Assert.Equal ("Custom Title", shortcut.Title);
    }
}
