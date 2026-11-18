using System.Text;

namespace ViewBaseTests.Keyboard;

/// <summary>
///     Tests for View.KeyBindings
/// </summary>
public class KeyBindingsTests
{
    [Fact]
    public void Focused_HotKey_Application_All_Work ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app!.TopRunnableView!.Add (view);

        app.Keyboard.RaiseKeyDownEvent (Key.A);
        Assert.False (keyWasHandled);
        Assert.True (view.ApplicationCommandInvoked);

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommandInvoked);
        Assert.False (keyWasHandled);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.FocusedCommandInvoked);

        keyWasHandled = false;
        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.True (view.FocusedCommandInvoked);
        Assert.False (keyWasHandled); // Command was invoked, but wasn't handled

        Assert.True (view.ApplicationCommandInvoked);
        Assert.True (view.HotKeyCommandInvoked);
    }

    [Fact]
    public void KeyBinding_Negative ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app.Keyboard.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommandInvoked);
        Assert.False (view.HotKeyCommandInvoked);
        Assert.False (view.FocusedCommandInvoked);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommandInvoked);
        Assert.False (view.HotKeyCommandInvoked);
        Assert.False (view.FocusedCommandInvoked);
    }

    [Fact]
    public void HotKey_KeyBinding ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        app!.TopRunnableView!.Add (view);

        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommandInvoked);
        Assert.False (keyWasHandled);

        view.HotKey = KeyCode.Z;
        keyWasHandled = false;
        view.HotKeyCommandInvoked = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H); // old hot key
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommandInvoked);

        app.Keyboard.RaiseKeyDownEvent (Key.Z); // new hot key
        Assert.True (view.HotKeyCommandInvoked);
        Assert.False (keyWasHandled);
    }

    [Fact]
    public void HotKey_KeyBinding_Negative ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app.Keyboard.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommandInvoked);

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommandInvoked);
    }

    [Fact]
    public void HotKey_Enabled_False_Does_Not_Invoke ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> ());

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app!.TopRunnableView!.Add (view);

        app.Keyboard.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommandInvoked);

        keyWasHandled = false;
        view.Enabled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommandInvoked);
    }

    [Fact]
    public void HotKey_Raises_HandlingHotKey ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> ());
        var hotKeyRaised = 0;
        var acceptRaised = 0;
        var activateRaised = 0;

        View view = new () { CanFocus = true, HotKeySpecifier = new Rune ('_'), Title = "_Test" };
        app!.TopRunnableView!.Add (view);

        view.HandlingHotKey += (_, _) => hotKeyRaised++;
        view.Accepting += (_, _) => acceptRaised++;
        view.Activating += (_, _) => activateRaised++;

        Assert.Equal (KeyCode.T, view.HotKey);
        app.Keyboard.RaiseKeyDownEvent (Key.T);
        Assert.Equal (1, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (1, activateRaised);

        app.Keyboard.RaiseKeyDownEvent (Key.T.WithAlt);
        Assert.Equal (2, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (2, activateRaised);

        view.HotKey = KeyCode.E;
        app.Keyboard.RaiseKeyDownEvent (Key.E.WithAlt);
        Assert.Equal (3, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (3, activateRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Validates that unhandled default KeyBinding commands (Activate for Space, Accept for Enter)
    ///     do not block HotKey dispatch. When a focused plain View has a default binding for a key but
    ///     doesn't genuinely handle the command (no dispatch target, no bubble config), the key should
    ///     propagate to <see cref="View.InvokeCommandsBoundToHotKey"/> so that HotKeyBindings on
    ///     sibling/ancestor views can fire. See issue #4759.
    /// </summary>
    [Theory]
    [InlineData ("Space")] // Space → Command.Activate (via SetupKeyboard)
    [InlineData ("Enter")] // Enter → Command.Accept (via SetupKeyboard)
    public void Unhandled_Default_KeyBinding_Does_Not_Block_HotKey (string keyName)
    {
        Key key = keyName == "Space" ? Key.Space : Key.Enter;

        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        // A view with a HotKeyBinding for the key under test
        View hotKeyView = new () { CanFocus = false };
        var hotKeyFired = false;
        hotKeyView.HandlingHotKey += (_, _) => hotKeyFired = true;
        hotKeyView.HotKeyBindings.Add (key, Command.HotKey);
        app!.TopRunnableView!.Add (hotKeyView);

        // A separate plain focused view — has default KeyBindings from SetupKeyboard
        // but does not genuinely handle Activate or Accept
        View focusableView = new () { CanFocus = true };
        app!.TopRunnableView!.Add (focusableView);

        // HotKey fires when nothing is focused
        app.Keyboard.RaiseKeyDownEvent (key);
        Assert.True (hotKeyFired);

        // HotKey still fires when a sibling with unhandled default bindings is focused
        focusableView.SetFocus ();
        Assert.True (focusableView.HasFocus);

        hotKeyFired = false;
        app.Keyboard.RaiseKeyDownEvent (key);
        Assert.True (hotKeyFired);
    }

    // Copilot - Opus 4.6
    /// <summary>
    ///     A view with Visible = false should not have its HotKey invoked via InvokeCommandsBoundToHotKey.
    /// </summary>
    [Fact]
    public void HotKey_Visible_False_Does_Not_Invoke ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> ());

        ScopedKeyBindingView view = new ();
        app!.TopRunnableView!.Add (view);

        // Sanity check: hotkey works when visible
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommandInvoked);

        // Now hide the view and try again
        view.HotKeyCommandInvoked = false;
        view.Visible = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.False (view.HotKeyCommandInvoked);
    }

    // Copilot - Opus 4.6
    /// <summary>
    ///     A SubView of an invisible SuperView should not have its HotKey invoked.
    /// </summary>
    [Fact]
    public void HotKey_Invisible_SuperView_SubView_Does_Not_Invoke ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> ());

        View container = new () { Id = "container" };
        ScopedKeyBindingView subView = new ();
        container.Add (subView);
        app!.TopRunnableView!.Add (container);

        // Sanity check: hotkey works when container is visible
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (subView.HotKeyCommandInvoked);

        // Hide the container and try again
        subView.HotKeyCommandInvoked = false;
        container.Visible = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.False (subView.HotKeyCommandInvoked);
    }

    // tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

    public class ScopedKeyBindingView : View
    {
        /// <summary>
        ///     If set, an additional HotKeyBinding is added via <see cref="View.HotKeyBindings"/> for this key.
        ///     This bypasses <see cref="View.AddKeyBindingsForHotKey"/> validation, allowing keys like Space.
        /// </summary>
        public Key? ExtraHotKey { get; set; }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();
            AddCommand (Command.Save, () => ApplicationCommandInvoked = true);
            AddCommand (Command.HotKey, () => HotKeyCommandInvoked = true);
            AddCommand (Command.Left, () => FocusedCommandInvoked = true);

            App!.Keyboard.KeyBindings.AddApp (Key.A, this, Command.Save);
            HotKey = KeyCode.H;
            KeyBindings.Add (Key.F, Command.Left);

            if (ExtraHotKey is { })
            {
                HotKeyBindings.Add (ExtraHotKey, Command.HotKey);
            }
        }

        public bool ApplicationCommandInvoked { get; set; }
        public bool FocusedCommandInvoked { get; set; }
        public bool HotKeyCommandInvoked { get; set; }
    }
}
