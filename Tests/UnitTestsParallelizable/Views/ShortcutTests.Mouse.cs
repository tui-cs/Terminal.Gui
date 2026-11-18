using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests
{
    [Theory]
    [CombinatorialData]
    public void CommandView_Click_Raises_Activating_On_Both (bool commandViewCanFocus)
    {
        // Arrange
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        TestCommandView testCommandView = new () { Title = "_Test", CanFocus = commandViewCanFocus };

        Shortcut shortcut = new () { Key = Key.T, CommandView = testCommandView };
        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (testCommandView.Frame.Location));

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, commandViewActivatingRaised);
    }

    [Theory]
    [CombinatorialData]
    public void CommandView_KeyDown_HotKey_Raises_Activating_On_Both (bool commandViewCanFocus)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        CheckBox testCommandView = new () { Title = "_Test", CanFocus = commandViewCanFocus };
        testCommandView.Value = 0;

        Shortcut shortcut = new () { Key = Key.F4, CommandView = testCommandView };
        Assert.Equal (Key.F4, shortcut.Key);
        Assert.Equal (Key.Empty, shortcut.HotKey);
        Assert.Equal (Key.T, testCommandView.HotKey);

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);
        Assert.Equal (commandViewCanFocus, testCommandView.HasFocus);
        Assert.True (shortcut.HasFocus);

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var shortcutActivatedRaised = 0;
        shortcut.Activated += (_, _) => shortcutActivatedRaised++;

        var commandViewActivatingRaised = 0;
        testCommandView.Activating += (_, _) => commandViewActivatingRaised++;

        var shortcutHandlingHotKeyFired = 0;
        shortcut.HandlingHotKey += (_, _) => { shortcutHandlingHotKeyFired++; };

        var commandViewHandlingHotKeyFired = 0;
        testCommandView.HandlingHotKey += (_, _) => { commandViewHandlingHotKeyFired++; };

        Assert.Equal (CheckState.UnChecked, testCommandView.Value);
        app.Keyboard.RaiseKeyDownEvent (testCommandView.HotKey);

        // Assert - Shortcut.Activating should have been raised
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, shortcutActivatedRaised);
        Assert.Equal (0, shortcutHandlingHotKeyFired);
        Assert.Equal (1, commandViewActivatingRaised);
        Assert.Equal (1, commandViewHandlingHotKeyFired);

        Assert.Equal (CheckState.Checked, testCommandView.Value);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing the Shortcut's Key (not the CommandView's HotKey) correctly
    ///     bubbles down to the CommandView, toggling a CheckBox. This tests the fix where
    ///     DefaultHotKeyHandler passes the binding through to InvokeCommand(Command.Activate)
    ///     so Shortcut.OnActivating can detect a user-initiated action and call DispatchDown.
    /// </summary>
    [Theory]
    [CombinatorialData]
    public void Shortcut_Key_Activates_CheckBox_CommandView (bool commandViewCanFocus)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = commandViewCanFocus };

        Shortcut shortcut = new () { Key = Key.F.WithCtrl, CommandView = checkBox };
        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        var shortcutActivatingRaised = 0;
        shortcut.Activating += (_, _) => shortcutActivatingRaised++;

        var checkBoxActivatingRaised = 0;
        checkBox.Activating += (_, _) => checkBoxActivatingRaised++;

        // Act - Press the Shortcut's Key (Ctrl+F), which triggers Command.HotKey on the Shortcut
        app.Keyboard.RaiseKeyDownEvent (Key.F.WithCtrl);

        // Assert - CheckBox should have toggled
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, shortcutActivatingRaised);
        Assert.Equal (1, checkBoxActivatingRaised);
    }

    // Claude - Haiku 4.5
    /// <summary>
    ///     Verifies that Context.TryGetSource() can be used in Activating event handlers to determine
    ///     if the activation came from the CommandView itself.
    ///     This pattern (shown in Shortcuts.cs:374) allows different handling based on the activation source.
    /// </summary>
    [Fact]
    public void Activating_Context_TryGetSource_Identifies_CommandView_Activation ()
    {
        // Arrange
        using TestCommandView commandView = new ();
        commandView.Text = "Command";
        using Shortcut shortcut = new ();
        shortcut.Key = Key.F9;
        shortcut.HelpText = "Test";
        shortcut.CommandView = commandView;

        View? capturedSource = null;
        var activatingFired = 0;

        shortcut.Activating += (_, args) =>
                               {
                                   // ReSharper disable once AccessToModifiedClosure
                                   activatingFired++;
                                   args.Context.TryGetSource (out capturedSource);
                               };

        // Act 1 - Activate the CommandView directly (simulates clicking on the CommandView)
        commandView.InvokeCommand (Command.Activate);

        // Assert - The source should be the CommandView
        Assert.Equal (1, activatingFired);
        Assert.NotNull (capturedSource);
        Assert.Same (commandView, capturedSource);

        // Reset
        capturedSource = null;
        activatingFired = 0;

        // Act 2 - Activate the Shortcut directly (not through CommandView)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - The source should be the Shortcut when activating it directly
        Assert.Equal (1, activatingFired);
        Assert.NotNull (capturedSource);
        Assert.Same (shortcut, capturedSource);
    }

    // Claude - Haiku 4.5
    /// <summary>
    ///     Demonstrates the pattern from Shortcuts.cs:374 where Activating event is marked as Handled
    ///     when activation comes from the CommandView, but custom logic runs otherwise.
    /// </summary>
    [Fact]
    public void Activating_Can_Handle_Differently_Based_On_CommandView_Source ()
    {
        // Arrange
        using TestCommandView commandView = new ();
        commandView.Text = "Command";
        using Shortcut shortcut = new ();
        shortcut.Key = Key.F9;
        shortcut.HelpText = "Cycles value";
        shortcut.CommandView = commandView;

        var customLogicExecuted = false;
        var handledWhenFromCommandView = false;

        shortcut.Activating += (_, args) =>
                               {
                                   // Pattern from Shortcuts.cs:374 - check if activation came from CommandView
                                   if (args.Context.TryGetSource (out View? ctxSource) && ctxSource == shortcut.CommandView)
                                   {
                                       // Mark as handled when coming from CommandView
                                       args.Handled = true;
                                       handledWhenFromCommandView = true;
                                   }
                                   else
                                   {
                                       // Execute custom logic when NOT from CommandView
                                       customLogicExecuted = true;
                                   }
                               };

        // Act 1 - Activate the CommandView directly
        commandView.InvokeCommand (Command.Activate);

        // Assert - Should be marked as handled, custom logic should NOT run
        Assert.True (handledWhenFromCommandView);
        Assert.False (customLogicExecuted);

        // Reset
        handledWhenFromCommandView = false;
        customLogicExecuted = false;

        // Act 2 - Activate the Shortcut directly (not through CommandView)
        shortcut.InvokeCommand (Command.Activate);

        // Assert - Should NOT be marked as handled, custom logic SHOULD run
        Assert.False (handledWhenFromCommandView);
        Assert.True (customLogicExecuted);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing Space when the Shortcut has focus correctly BubbleDowns
    ///     to the CommandView. Space triggers Activate on the Shortcut, which should forward
    ///     to CommandView because Binding.Source is the Shortcut (not CommandView).
    /// </summary>
    [Fact]
    public void Space_Key_BubblesDown_To_CheckBox_CommandView ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.F5, CommandView = checkBox, CanFocus = true };
        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        Assert.True (shortcut.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var checkBoxActivatingCount = 0;
        checkBox.Activating += (_, _) => checkBoxActivatingCount++;

        // Act - Press Space while Shortcut has focus
        app.Keyboard.RaiseKeyDownEvent (Key.Space);

        // Assert - CheckBox should toggle (DispatchDown from Shortcut to CommandView)
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, shortcutActivatingCount);
        Assert.Equal (1, checkBoxActivatingCount);
    }

    [Fact]
    public void CheckBox_Enter_Key_Invokes_Activate ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        CheckBox checkBox = new () { Title = "_Toggle", CanFocus = false };

        Shortcut shortcut = new () { Key = Key.F5, CommandView = checkBox, CanFocus = true };
        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        Assert.True (shortcut.HasFocus);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        var actionInvoked = 0;
        shortcut.Action = () => actionInvoked++;

        // Act - Press Enter while Shortcut has focus
        app.Keyboard.RaiseKeyDownEvent (Key.Enter);

        // Assert - Accept fires, Activate does NOT fire, CheckBox state changed
        Assert.Equal (0, shortcutAcceptingCount);
        Assert.Equal (1, shortcutActivatingCount);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (1, actionInvoked);
    }
}
