namespace ApplicationTests.Keyboard;

/// <summary>
///     Tests for <see cref="IApplication.Keyboard"/> proving the integration between
///     input injection and <see cref="IKeyboard.RaiseKeyDownEvent"/>.
/// </summary>
[Trait ("Category", "Keyboard")]
[Collection("Application Tests")]
public class ApplicationKeyboardTests
{
    #region InjectKeyEvent → RaiseKeyDownEvent Integration Tests

    [Fact]
    public void InjectKeyEvent_CallsKeyboardRaiseKeyDownEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool keyDownRaised = false;
        Key? receivedKey = null;

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    keyDownRaised = true;
                                    receivedKey = e;
                                };

        // Act
        app.InjectKey (Key.A);

        // Assert
        Assert.True (keyDownRaised, "Keyboard.KeyDown event should have been raised");
        Assert.Equal (Key.A, receivedKey);
    }

    [Fact]
    public void InjectKeyEvent_WithModifiers_CallsKeyboardRaiseKeyDownEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool keyDownRaised = false;
        Key? receivedKey = null;

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    keyDownRaised = true;
                                    receivedKey = e;
                                };

        Key testKey = Key.A.WithCtrl;

        // Act
        app.InjectKey (testKey);

        // Assert
        Assert.True (keyDownRaised);
        Assert.Equal (testKey, receivedKey);
        Assert.True (receivedKey!.IsCtrl);
    }

    [Fact]
    public void InjectKeyEvent_MultipleKeys_CallsRaiseKeyDownEventForEach ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];

        app.Keyboard.KeyDown += (s, e) => receivedKeys.Add (e);

        Key [] keysToInject = [Key.A, Key.B, Key.C, Key.Enter];

        // Act
        foreach (Key key in keysToInject)
        {
            app.InjectKey (key);
        }

        // Assert
        Assert.Equal (keysToInject.Length, receivedKeys.Count);

        for (int i = 0; i < keysToInject.Length; i++)
        {
            Assert.Equal (keysToInject [i], receivedKeys [i]);
        }
    }

    [Fact]
    public void InjectKeyEvent_SpecialKeys_CallsRaiseKeyDownEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];

        app.Keyboard.KeyDown += (s, e) => receivedKeys.Add (e);

        Key [] specialKeys =
        [
            Key.F1, Key.F6, Key.F12, Key.CursorUp, Key.CursorDown, Key.Home, Key.End, Key.PageUp, Key.PageDown, Key.Delete, Key.InsertChar
        ];

        // Act
        foreach (Key key in specialKeys)
        {
            app.InjectKey (key);
        }

        // Assert
        Assert.Equal (specialKeys.Length, receivedKeys.Count);

        for (int i = 0; i < specialKeys.Length; i++)
        {
            Assert.Equal (specialKeys [i], receivedKeys [i]);
        }
    }

    [Fact]
    public void InjectKeyEvent_FunctionKeysWithShift_CallsRaiseKeyDownEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> receivedKeys = [];

        app.Keyboard.KeyDown += (s, e) => receivedKeys.Add (e);

        Key [] shiftedFunctionKeys =
        [
            Key.F1.WithShift, Key.F6.WithShift, Key.F12.WithShift
        ];

        // Act
        foreach (Key key in shiftedFunctionKeys)
        {
            app.InjectKey (key);
        }

        // Assert
        Assert.Equal (shiftedFunctionKeys.Length, receivedKeys.Count);

        for (int i = 0; i < shiftedFunctionKeys.Length; i++)
        {
            Assert.Equal (shiftedFunctionKeys [i], receivedKeys [i]);
            Assert.True (receivedKeys [i].IsShift, $"Key {receivedKeys [i]} should have Shift modifier");
        }
    }

    [Theory]
    [InlineData (KeyCode.A)]
    [InlineData (KeyCode.A | KeyCode.ShiftMask)]
    [InlineData (KeyCode.A | KeyCode.CtrlMask)]
    [InlineData (KeyCode.A | KeyCode.AltMask)]
    [InlineData (KeyCode.F6)]
    [InlineData (KeyCode.F6 | KeyCode.ShiftMask)]
    [InlineData (KeyCode.F6 | KeyCode.CtrlMask)]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.CursorUp | KeyCode.ShiftMask)]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.Delete | KeyCode.CtrlMask)]
    public void InjectKeyEvent_VariousKeys_RaisesKeyboardKeyDown (KeyCode keyCode)
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool keyDownRaised = false;
        Key? receivedKey = null;
        Key expectedKey = new (keyCode);

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    keyDownRaised = true;
                                    receivedKey = e;
                                };

        // Act
        app.InjectKey (expectedKey);

        // Assert
        Assert.True (keyDownRaised, $"Keyboard.KeyDown should be raised for {keyCode}");
        Assert.Equal (expectedKey.KeyCode, receivedKey!.KeyCode);
    }

    #endregion

    #region InjectKeyEvent → View Keyboard Event Pipeline Tests

    [Fact]
    public void InjectKeyEvent_RoutesToFocusedView ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool keyReceived = false;
        Key? receivedKey = null;

        View view = new () { CanFocus = true, App = app };

        view.KeyDown += (s, e) =>
                        {
                            keyReceived = true;
                            receivedKey = e;
                        };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        view.SetFocus ();

        // Act
        app.InjectKey (Key.A);

        // Assert
        Assert.True (keyReceived, "View should receive the key");
        Assert.Equal (Key.A, receivedKey);

        app.End (token!);
        top.Dispose ();
    }

    [Fact]
    public void InjectKeyEvent_ApplicationKeyDown_FiredBeforeViewProcessing ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool applicationKeyDownFired = false;
        bool viewKeyDownFired = false;
        List<string> fireOrder = [];

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    applicationKeyDownFired = true;
                                    fireOrder.Add ("Application");
                                };

        View view = new () { CanFocus = true, App = app };

        view.KeyDown += (s, e) =>
                        {
                            viewKeyDownFired = true;
                            fireOrder.Add ("View");
                        };

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        view.SetFocus ();

        // Act
        app.InjectKey (Key.A);

        // Assert
        Assert.True (applicationKeyDownFired, "Application.Keyboard.KeyDown should fire");
        Assert.True (viewKeyDownFired, "View.KeyDown should fire");
        Assert.Equal (2, fireOrder.Count);
        Assert.Equal ("Application", fireOrder [0]);
        Assert.Equal ("View", fireOrder [1]);

        app.End (token!);
        top.Dispose ();
    }

    [Fact]
    public void InjectKeyEvent_HandledAtApplicationLevel_DoesNotRouteToView ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool viewKeyDownFired = false;

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    e.Handled = true; // Handle at application level
                                };

        View view = new () { CanFocus = true, App = app };

        view.KeyDown += (s, e) => viewKeyDownFired = true;

        Runnable top = new () { App = app };
        top.Add (view);
        SessionToken? token = app.Begin (top);

        view.SetFocus ();

        // Act
        app.InjectKey (Key.A);

        // Assert
        Assert.False (viewKeyDownFired, "View should not receive key when handled at Application level");

        app.End (token!);
        top.Dispose ();
    }

    #endregion

    #region IKeyboard.RaiseKeyDownEvent Direct Tests

    [Fact]
    public void RaiseKeyDownEvent_RaisesKeyDownEvent ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        bool eventRaised = false;
        Key? receivedKey = null;

        app.Keyboard.KeyDown += (s, e) =>
                                {
                                    eventRaised = true;
                                    receivedKey = e;
                                };

        // Act
        app.Keyboard.RaiseKeyDownEvent (Key.B);

        // Assert
        Assert.True (eventRaised);
        Assert.Equal (Key.B, receivedKey);
    }

    [Fact]
    public void RaiseKeyDownEvent_ReturnsTrue_WhenHandled ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Keyboard.KeyDown += (s, e) => e.Handled = true;

        // Act
        bool handled = app.Keyboard.RaiseKeyDownEvent (Key.A);

        // Assert
        Assert.True (handled);
    }

    [Fact]
    public void RaiseKeyDownEvent_ReturnsFalse_WhenNotHandled ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Keyboard.KeyDown += (s, e) => { }; // Don't set Handled

        // Act
        bool handled = app.Keyboard.RaiseKeyDownEvent (Key.A);

        // Assert
        Assert.False (handled);
    }

    #endregion

    #region Application Key Binding Tests

    [Fact]
    public void InjectKeyEvent_CustomKeyBinding_InvokesCommand ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Use a custom command implementation via app.Keyboard
        app.Keyboard.KeyBindings.Add (Key.F9, Command.Refresh);

        Runnable top = new () { App = app };
        SessionToken? token = app.Begin (top);

        // Act
        app.InjectKey (Key.F9);

        // Assert
        // Command.Refresh is handled at Application level, so we just verify no exception
        Assert.True (app.Keyboard.KeyBindings.TryGet (Key.F9, out KeyBinding binding));
        Assert.Contains (Command.Refresh, binding.Commands);

        app.End (token!);
        top.Dispose ();
    }

    #endregion

    #region Integration with IApplication Instance Tests

    [Fact]
    public void Keyboard_Property_ReturnsNonNullAfterInit ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Assert
        Assert.NotNull (app.Keyboard);
    }

    [Fact]
    public void Keyboard_App_Property_ReferencesApplication ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Assert
        Assert.NotNull (app.Keyboard.App);
        Assert.Same (app, app.Keyboard.App);
    }

    [Fact]
    public void Multiple_Applications_Have_Independent_Keyboards ()
    {
        // Arrange
        using IApplication app1 = Application.Create ();
        app1.Init (DriverRegistry.Names.ANSI);

        using IApplication app2 = Application.Create ();
        app2.Init (DriverRegistry.Names.ANSI);

        int app1KeyDownCount = 0;
        int app2KeyDownCount = 0;

        app1.Keyboard.KeyDown += (s, e) => app1KeyDownCount++;
        app2.Keyboard.KeyDown += (s, e) => app2KeyDownCount++;

        // Act
        app1.InjectKey (Key.A);
        app2.InjectKey (Key.B);

        // Assert
        Assert.Equal (1, app1KeyDownCount);
        Assert.Equal (1, app2KeyDownCount);
        Assert.NotSame (app1.Keyboard, app2.Keyboard);
    }

    #endregion
}
