using JetBrains.Annotations;

namespace ViewsTests;

[TestSubject (typeof (Shortcut))]
public partial class ShortcutTests
{
    // Copilot
    /// <summary>
    ///     Verifies that setting <see cref="Shortcut.Key"/> to <see cref="Key.Empty"/> removes the key binding,
    ///     so the previously-bound key no longer triggers the action.
    /// </summary>
    [Fact]
    public void Setting_Key_To_Empty_Removes_KeyBinding ()
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var action = 0;

        Shortcut shortcut = new () { Key = Key.F10, Title = "Test", Action = () => action++ };

        runnable.Add (shortcut);

        app.Keyboard.RaiseKeyDownEvent (Key.F10);
        Assert.Equal (1, action);

        shortcut.Key = Key.Empty;

        app.Keyboard.RaiseKeyDownEvent (Key.F10);
        Assert.Equal (1, action); // Should not have incremented; old binding must be gone
    }

    // Copilot
    /// <summary>
    ///     Verifies that setting <see cref="Shortcut.Key"/> to <see cref="Key.Empty"/> removes the
    ///     application-level key binding when <see cref="Shortcut.BindKeyToApplication"/> is <see langword="true"/>.
    /// </summary>
    [Fact]
    public void Setting_Key_To_Empty_Removes_AppLevel_KeyBinding ()
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var action = 0;

        Shortcut shortcut = new ()
        {
            App = app,
            Key = Key.F10,
            Title = "Test",
            BindKeyToApplication = true,
            Action = () => action++
        };

        runnable.Add (shortcut);

        app.Keyboard.RaiseKeyDownEvent (Key.F10);
        Assert.Equal (1, action);

        shortcut.Key = Key.Empty;

        app.Keyboard.RaiseKeyDownEvent (Key.F10);
        Assert.Equal (1, action); // Should not have incremented; old app binding must be gone
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing various keys invokes the <see cref="Shortcut.Action"/> delegate.
    ///     Action is invoked via <see cref="Shortcut.OnActivated"/> (for Activate) or
    ///     <see cref="Shortcut.OnAccepted"/> (for Accept).
    /// </summary>
    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 1)]
    [InlineData (true, KeyCode.F1, 0)]
    [InlineData (false, KeyCode.A, 1)]
    [InlineData (false, KeyCode.C, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (false, KeyCode.Enter, 0)]
    [InlineData (false, KeyCode.Space, 0)]
    [InlineData (false, KeyCode.F1, 0)]
    public void KeyDown_Invokes_Action (bool canFocus, KeyCode key, int expectedAction)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var action = 0;

        Shortcut shortcut = new ()
        {
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus,
            Action = () => action++
        };

        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAction, action);
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that pressing various keys invokes the <see cref="Shortcut.Action"/> delegate
    ///     when <see cref="Shortcut.BindKeyToApplication"/> is <see langword="true"/>.
    ///     The Shortcut's Key is bound at the application level so it works regardless of focus.
    /// </summary>
    [Theory]
    [InlineData (true, KeyCode.A, 1)]
    [InlineData (true, KeyCode.C, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (true, KeyCode.Enter, 1)]
    [InlineData (true, KeyCode.Space, 1)]
    [InlineData (true, KeyCode.F1, 0)]
    [InlineData (false, KeyCode.A, 1)]
    [InlineData (false, KeyCode.C, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 1)]
    [InlineData (false, KeyCode.Enter, 0)]
    [InlineData (false, KeyCode.Space, 0)]
    [InlineData (false, KeyCode.F1, 0)]
    public void KeyDown_App_Scope_Invokes_Action (bool canFocus, KeyCode key, int expectedAction)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        var action = 0;

        Shortcut shortcut = new ()
        {
            App = app,
            Key = Key.A,
            Text = "0",
            Title = "_C",
            CanFocus = canFocus,
            BindKeyToApplication = true,
            Action = () => action++
        };

        runnable.Add (shortcut);
        runnable.SetFocus ();

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAction, action);
    }

    [Theory]
    [CombinatorialData]
    public void KeyDown_Key_Raises_HandlingHotKey_And_Accepting (bool canFocus)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", Title = "_C", CanFocus = canFocus };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, _) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, _) => { activated++; };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, _) => { handlingHotKey++; };

        app.Keyboard.RaiseKeyDownEvent (shortcut.Key);

        Assert.Equal (0, accepting);
        Assert.Equal (1, handlingHotKey);
        Assert.Equal (1, activated);
    }

    [Theory]
    [CombinatorialData]
    public void CheckBox_KeyDown_Key_Raises_HandlingHotKey_And_Accepting (bool canFocus)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.F4, Text = "0", CanFocus = canFocus, CommandView = new CheckBox { Title = "_Test", CanFocus = canFocus } };
        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, _) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, _) => { activated++; };

        var handlingHotKey = 0;

        shortcut.HandlingHotKey += (_, _) => { handlingHotKey++; };

        app.Keyboard.RaiseKeyDownEvent (shortcut.Key);

        Assert.Equal (0, accepting);
        Assert.Equal (1, handlingHotKey);
        Assert.Equal (1, activated);
    }

    [Theory]
    [InlineData (true, KeyCode.A, 0, 1)]
    [InlineData (true, KeyCode.C, 0, 1)]
    [InlineData (true, KeyCode.C | KeyCode.AltMask, 0, 1)]
    [InlineData (true, KeyCode.Enter, 0, 1)]
    [InlineData (true, KeyCode.Space, 0, 1)]
    [InlineData (true, KeyCode.F1, 0, 0)]
    [InlineData (false, KeyCode.A, 0, 1)]
    [InlineData (false, KeyCode.C, 0, 1)]
    [InlineData (false, KeyCode.C | KeyCode.AltMask, 0, 1)]
    [InlineData (false, KeyCode.Enter, 0, 0)]
    [InlineData (false, KeyCode.Space, 0, 0)]
    [InlineData (false, KeyCode.F1, 0, 0)]
    public void KeyDown_Valid_Keys_Raises_Accepted_Activated_Correctly (bool canFocus, KeyCode key, int expectedAccept, int expectedActivate)
    {
        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        Shortcut shortcut = new () { Key = Key.A, Text = "0", Title = "_C", CanFocus = canFocus };

        // The default CommandView does not have a HotKey, so only the Shortcut's Key should trigger activation, not the CommandView's HotKey
        Assert.Equal (Key.A, shortcut.Key);
        Assert.Equal (Key.C, shortcut.HotKey);
        Assert.Equal (Key.Empty, shortcut.CommandView?.HotKey);

        runnable.Add (shortcut);

        Assert.Equal (canFocus, shortcut.HasFocus);

        var accepting = 0;

        shortcut.Accepting += (_, _) => { accepting++; };

        var activated = 0;

        shortcut.Activating += (_, _) => { activated++; };

        app.Keyboard.RaiseKeyDownEvent (key);

        Assert.Equal (expectedAccept, accepting);
        Assert.Equal (expectedActivate, activated);
    }

    [Fact]
    public void Mouse_Click_On_CommandView_Causes_Activation ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F1;
        shortcut.HelpText = "Help text";
        shortcut.Title = "Command";
        shortcut.Width = 40; // Wide enough to create gaps between subviews
        shortcut.Height = 1;

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var activatingCount = 0;

        shortcut.Activating += (_, _) => { activatingCount++; };

        // Verify layout created gaps
        Assert.True (shortcut.Frame.Width >= 40, "Shortcut should be wide enough for gaps");
        Assert.True (shortcut.CommandView?.Frame.Width > 0, "CommandView should be visible");
        Assert.True (shortcut.HelpView.Frame.Width > 0, "HelpView should be visible");
        Assert.True (shortcut.KeyView.Frame.Width > 0, "KeyView should be visible");

        // Act & Assert - Click at various X positions across the entire CommandView
        Rectangle screen = shortcut.CommandView.FrameToScreen ();

        for (int x = screen.X; x < screen.X + screen.Width; x++)
        {
            int expectedCount = activatingCount + 1;

            // Simulate mouse click at position x
            app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (x, 0)));

            Assert.True (activatingCount == expectedCount,
                         $"Click at X={x} should activate the Shortcut. Expected: {expectedCount}, Actual: {activatingCount}");
        }
    }

    [Fact]
    public void Mouse_Click_On_HelpView_Causes_Activation ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F1;
        shortcut.HelpText = "Help text";
        shortcut.Title = "Command";
        shortcut.Width = 40; // Wide enough to create gaps between subviews
        shortcut.Height = 1;

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var activatingCount = 0;

        shortcut.Activating += (_, _) => { activatingCount++; };

        // Verify layout created gaps
        Assert.True (shortcut.Frame.Width >= 40, "Shortcut should be wide enough for gaps");
        Assert.True (shortcut.CommandView?.Frame.Width > 0, "CommandView should be visible");
        Assert.True (shortcut.HelpView.Frame.Width > 0, "HelpView should be visible");
        Assert.True (shortcut.KeyView.Frame.Width > 0, "KeyView should be visible");

        // Act & Assert - Click at various X positions across the entire HelpView
        Rectangle screen = shortcut.HelpView.FrameToScreen ();

        for (int x = screen.X; x < screen.X + screen.Width; x++)
        {
            int expectedCount = activatingCount + 1;

            // Simulate mouse click at position x
            app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (x, 0)));

            Assert.True (activatingCount == expectedCount,
                         $"Click at X={x} should activate the Shortcut. Expected: {expectedCount}, Actual: {activatingCount}");
        }
    }

    [Fact]
    public void Mouse_Click_On_KeyView_Causes_Activation ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F1;
        shortcut.HelpText = "Help text";
        shortcut.Title = "Command";
        shortcut.Width = 40; // Wide enough to create gaps between subviews
        shortcut.Height = 1;

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var activatingCount = 0;

        shortcut.Activating += (_, _) => { activatingCount++; };

        // Verify layout created gaps
        Assert.True (shortcut.Frame.Width >= 40, "Shortcut should be wide enough for gaps");
        Assert.True (shortcut.CommandView?.Frame.Width > 0, "CommandView should be visible");
        Assert.True (shortcut.HelpView.Frame.Width > 0, "HelpView should be visible");
        Assert.True (shortcut.KeyView.Frame.Width > 0, "KeyView should be visible");

        // Act & Assert - Click at various X positions across the entire KeyView
        Rectangle screen = shortcut.KeyView.FrameToScreen ();

        for (int x = screen.X + 1; x < screen.X + screen.Width; x++)
        {
            int expectedCount = activatingCount + 1;

            // Simulate mouse click at position x
            app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (x, 0)));

            Assert.True (activatingCount == expectedCount,
                         $"Click at X={x} should activate the Shortcut. Expected: {expectedCount}, Actual: {activatingCount}");
        }
    }

    /// <summary>
    ///     Verifies that clicking anywhere across the entire width of a Shortcut causes activation,
    ///     including clicks in gaps between CommandView, HelpView, and KeyView.
    /// </summary>
    [Fact]
    public void Mouse_Click_Anywhere_On_Shortcut_Causes_Activation ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        using Shortcut shortcut = new ();
        shortcut.Key = Key.F1;
        shortcut.HelpText = "Help text";
        shortcut.Title = "Command";
        shortcut.Width = 40; // Wide enough to create gaps between subviews
        shortcut.Height = 1;

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        var activatingCount = 0;

        shortcut.Activating += (_, _) => { activatingCount++; };

        // Verify layout created gaps
        Assert.True (shortcut.Frame.Width >= 40, "Shortcut should be wide enough for gaps");
        Assert.True (shortcut.CommandView?.Frame.Width > 0, "CommandView should be visible");
        Assert.True (shortcut.HelpView.Frame.Width > 0, "HelpView should be visible");
        Assert.True (shortcut.KeyView.Frame.Width > 0, "KeyView should be visible");

        // Act & Assert - Click at various X positions across the entire width
        for (var x = 0; x < shortcut.Frame.Width; x++)
        {
            int expectedCount = activatingCount + 1;

            // Simulate mouse click at position x
            app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (x, 0)));

            Assert.True (activatingCount == expectedCount,
                         $"Click at X={x} should activate the Shortcut. Expected: {expectedCount}, Actual: {activatingCount}");
        }
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that clicking directly on a Button CommandView routes through Accept (not Activate),
    ///     because Button maps <see cref="MouseFlags.LeftButtonClicked"/> to <see cref="Command.Accept"/>.
    ///     The Accept bubbles up to the Shortcut, which also raises its Accepting event.
    ///     Neither Activating event fires because the click goes through the Accept path.
    /// </summary>
    [Theory]
    [CombinatorialData]
    public void Mouse_Click_Button_CommandView_Raises_Accepting_On_Both (bool commandViewCanFocus)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        Button button = new () { Title = "C", NoDecorations = true, NoPadding = true, CanFocus = commandViewCanFocus };

        Shortcut shortcut = new () { Key = Key.A, Text = "0", CommandView = button };

        (runnable as View)?.Add (shortcut);
        app.Begin (runnable);

        // Verify the Shortcut and Button have been laid out
        Assert.True (shortcut.Frame.Width > 0, "Shortcut should have width");
        Assert.True (button.Frame.Width > 0, "Button should have width");

        var shortcutAcceptingCount = 0;
        shortcut.Accepting += (_, _) => shortcutAcceptingCount++;

        var shortcutActivatingCount = 0;
        shortcut.Activating += (_, _) => shortcutActivatingCount++;

        var buttonAcceptingCount = 0;
        button.Accepting += (_, _) => buttonAcceptingCount++;

        var buttonActivatingCount = 0;
        button.Activating += (_, _) => buttonActivatingCount++;

        // Act - click directly on the Button (CommandView)
        // Button maps LeftButtonClicked → Command.Accept, so this goes through Accept path
        Point buttonScreenPos = button.ViewportToScreen (Point.Empty);
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (buttonScreenPos));

        // Assert - Accept fires on Button, then bubbles to Shortcut
        Assert.Equal (1, buttonAcceptingCount);
        Assert.Equal (1, shortcutAcceptingCount);

        // Activating does NOT fire because the click was routed to Accept by Button
        Assert.Equal (0, buttonActivatingCount);
        Assert.Equal (0, shortcutActivatingCount);
    }
}
