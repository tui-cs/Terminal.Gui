#nullable disable
using UnitTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace ViewBaseTests.Keyboard;

public class KeyboardEventTests (ITestOutputHelper output) : TestsAllViews
{
    /// <summary>
    ///     This tests that when a new key down event is sent to the view  will fire the key-down related
    ///     events: KeyDown and KeyDownNotHandled.
    /// </summary>
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewKeyDownEvent_All_EventsFire (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"ERROR: Skipping generic view: {viewType}");

            return;
        }

        output.WriteLine ($"Testing {viewType}");

        var keyDown = false;

        view.KeyDown += (s, a) =>
                        {
                            a.Handled = false; // don't handle it so the other events are called
                            keyDown = true;
                        };

        var keyDownNotHandled = false;

        view.KeyDownNotHandled += (s, a) =>
                                  {
                                      a.Handled = true;
                                      keyDownNotHandled = true;
                                  };

        // Key.Empty is invalid, but it's used here to test that the event is fired
        Assert.True (view.NewKeyDownEvent (Key.Empty)); // this will be true because the ProcessKeyDown event handled it
        Assert.True (keyDown);
        Assert.True (keyDownNotHandled);
        view.Dispose ();
    }

    [Theory]
    [InlineData (true, false, false)]
    [InlineData (true, true, false)]
    [InlineData (true, true, true)]
    public void NewKeyDownEvent_Raised_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
    {
        var keyDown = false;
        var keyDownNotHandled = false;

        var view = new OnNewKeyTestView ();
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.Null, e.KeyCode & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask);
                            Assert.Equal (shift, e.IsShift);
                            Assert.Equal (alt, e.IsAlt);
                            Assert.Equal (control, e.IsCtrl);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            keyDown = true;
                        };
        view.KeyDownNotHandled += (s, e) => { keyDownNotHandled = true; };

        view.NewKeyDownEvent (new Key (KeyCode.Null | (shift ? KeyCode.ShiftMask : 0) | (alt ? KeyCode.AltMask : 0) | (control ? KeyCode.CtrlMask : 0)));
        Assert.True (keyDownNotHandled);
        Assert.True (view.OnKeyDownCalled);
        Assert.True (view.OnProcessKeyDownCalled);
    }

    [Fact]
    public void NewKeyDownEvent_Handled_True_Stops_Processing ()
    {
        var keyDown = false;
        var keyDownNotHandled = false;

        var view = new OnNewKeyTestView ();
        Assert.True (view.CanFocus);
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.A, e.KeyCode);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            e.Handled = true;
                            keyDown = true;
                        };

        view.KeyDownNotHandled += (s, e) =>
                                  {
                                      Assert.Equal (KeyCode.A, e.KeyCode);
                                      Assert.False (keyDownNotHandled);
                                      Assert.False (view.OnProcessKeyDownCalled);
                                      e.Handled = true;
                                      keyDownNotHandled = true;
                                  };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.False (keyDownNotHandled);

        Assert.True (view.OnKeyDownCalled);
        Assert.False (view.OnProcessKeyDownCalled);
    }

    [Fact]
    public void NewKeyDownEvent_KeyDown_Handled_Stops_Processing ()
    {
        var view = new View ();
        var keyDownNotHandled = false;
        var setHandledTo = false;

        view.KeyDown += (s, e) =>
                        {
                            e.Handled = setHandledTo;
                            Assert.Equal (setHandledTo, e.Handled);
                            Assert.Equal (KeyCode.N, e.KeyCode);
                        };

        view.KeyDownNotHandled += (s, e) =>
                                  {
                                      keyDownNotHandled = true;
                                      Assert.False (e.Handled);
                                      Assert.Equal (KeyCode.N, e.KeyCode);
                                  };

        view.NewKeyDownEvent (Key.N);
        Assert.True (keyDownNotHandled);

        keyDownNotHandled = false;
        setHandledTo = true;
        view.NewKeyDownEvent (Key.N);
        Assert.False (keyDownNotHandled);
    }

    [Fact]
    public void NewKeyDownEvent_ProcessKeyDown_Handled_Stops_Processing ()
    {
        var keyDown = false;
        var keyDownNotHandled = false;

        var view = new OnNewKeyTestView ();
        Assert.True (view.CanFocus);
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.A, e.KeyCode);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            e.Handled = false;
                            keyDown = true;
                        };

        view.KeyDownNotHandled += (s, e) =>
                                  {
                                      Assert.Equal (KeyCode.A, e.KeyCode);
                                      Assert.False (keyDownNotHandled);
                                      Assert.True (view.OnProcessKeyDownCalled);
                                      e.Handled = true;
                                      keyDownNotHandled = true;
                                  };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.True (keyDownNotHandled);

        Assert.True (view.OnKeyDownCalled);
        Assert.True (view.OnProcessKeyDownCalled);
    }

    [Theory]
    [InlineData (null, null)]
    [InlineData (true, true)]
    [InlineData (false, false)]
    public void InvokeCommands_Returns_Nullable_Properly (bool? toReturn, bool? expected)
    {
        var view = new KeyBindingsTestView ();
        view.CommandReturns = toReturn;

        bool? result = view.InvokeCommandsBoundToKey (Key.A);
        Assert.Equal (expected, result);
    }

    /// <summary>A view that overrides the OnKey* methods so we can test that they are called.</summary>
    public class KeyBindingsTestView : View
    {
        public KeyBindingsTestView ()
        {
            CanFocus = true;
            AddCommand (Command.HotKey, () => CommandReturns);
            KeyBindings.Add (Key.A, Command.HotKey);
        }

        public bool? CommandReturns { get; set; }
    }

    /// <summary>
    ///     Baseline: A view that does NOT subscribe to KeyDownNotHandled does not interfere
    ///     with HotKey routing. Alt+T reaches the sibling Label's HotKey as expected.
    /// </summary>
    [Fact]
    public void AltKey_Routed_To_Sibling_HotKey_When_FocusedView_Does_Not_Handle_KeyDownNotHandled ()
    {
        // Copilot
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Window win = new ();

        Label label = new () { Text = "_Type text here:" };
        var hotKeyInvoked = false;
        label.HandlingHotKey += (_, _) => hotKeyInvoked = true;

        // A plain view that can focus but does NOT handle KeyDownNotHandled
        View focusable = new () { CanFocus = true, Width = 20, Y = 1 };

        win.Add (label, focusable);

        SessionToken token = app.Begin (win);
        focusable.SetFocus ();
        Assert.True (focusable.HasFocus);

        Key altT = new (Key.T.WithAlt) { AssociatedText = "t" };
        app.InjectKey (altT);

        Assert.True (hotKeyInvoked, "Label's HotKey should fire when focused view ignores the key");

        app.End (token!);
        win.Dispose ();
    }

    /// <summary>A view that overrides the OnKey* methods so we can test that they are called.</summary>
    public class OnNewKeyTestView : View
    {
        public OnNewKeyTestView () => CanFocus = true;
        public bool CancelVirtualMethods { set; private get; }
        public bool OnKeyDownCalled { get; set; }
        public bool OnProcessKeyDownCalled { get; set; }
        public new string Text { get; set; }

        protected override bool OnKeyDown (Key keyEvent)
        {
            OnKeyDownCalled = true;

            return CancelVirtualMethods;
        }

        protected override bool OnKeyDownNotHandled (Key keyEvent)
        {
            OnProcessKeyDownCalled = true;

            return CancelVirtualMethods;
        }
    }
}
