using UnitTests;

namespace ViewsTests;

/// <summary>
///     Pure unit tests for <see cref="Label"/> that don't require Application.Driver or Application context.
///     These tests can run in parallel without interference.
/// </summary>
public class LabelTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void Text_Mirrors_Title ()
    {
        var label = new Label ();
        label.Title = "Hello";
        Assert.Equal ("Hello", label.Title);
        Assert.Equal ("Hello", label.TitleTextFormatter.Text);

        Assert.Equal ("Hello", label.Text);
        Assert.Equal ("Hello", label.TextFormatter.Text);
    }

    [Fact]
    public void Title_Mirrors_Text ()
    {
        var label = new Label ();
        label.Text = "Hello";
        Assert.Equal ("Hello", label.Text);
        Assert.Equal ("Hello", label.TextFormatter.Text);

        Assert.Equal ("Hello", label.Title);
        Assert.Equal ("Hello", label.TitleTextFormatter.Text);
    }

    [Theory]
    [CombinatorialData]
    public void HotKey_Command_SetsFocus_OnNextSubView (bool hasHotKey)
    {
        var superView = new View { CanFocus = true };
        var label = new Label ();
        label.HotKey = hasHotKey ? Key.A.WithAlt : Key.Empty;
        var nextSubView = new View { CanFocus = true };
        superView.Add (label, nextSubView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (label.HasFocus);
        Assert.False (nextSubView.HasFocus);

        label.InvokeCommand (Command.HotKey);
        Assert.False (label.HasFocus);
        Assert.Equal (hasHotKey, nextSubView.HasFocus);
    }

    [Theory]
    [CombinatorialData]
    public void LeftButtonReleased_SetsFocus_OnNextSubView (bool hasHotKey)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var label = new Label { X = 2 };
        label.HotKey = hasHotKey ? Key.X.WithAlt : Key.Empty;

        var nextSubView = new View { CanFocus = true, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, label, nextSubView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (focusedView.HasFocus);
        Assert.False (label.HasFocus);
        Assert.False (nextSubView.HasFocus);

        label.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        Assert.False (label.HasFocus);
        Assert.Equal (hasHotKey, nextSubView.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var label = new Label ();
        var accepted = false;

        label.Accepting += LabelOnAccept;
        label.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void LabelOnAccept (object? sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var label = new Label ();
        Assert.Equal (string.Empty, label.Text);
        Assert.Equal (Alignment.Start, label.TextAlignment);
        Assert.False (label.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 0, 0), label.Frame);
        Assert.Equal (KeyCode.Null, label.HotKey);
    }

    [Fact]
    public void Label_HotKeyChanged_EventFires ()
    {
        var label = new Label ();
        var fired = false;
        var oldKey = Key.Empty;
        var newKey = Key.Empty;

        label.HotKeyChanged += (s, e) =>
                               {
                                   fired = true;
                                   oldKey = e.OldKey;
                                   newKey = e.NewKey;
                               };

        label.HotKey = Key.A.WithAlt;

        Assert.True (fired);
        Assert.Equal (Key.Empty, oldKey);
        Assert.Equal (Key.A.WithAlt, newKey);
    }

    [Fact]
    public void Label_HotKeyChanged_EventFires_WithNone ()
    {
        var label = new Label { HotKey = Key.A.WithAlt };
        var fired = false;
        var oldKey = Key.Empty;
        var newKey = Key.Empty;

        label.HotKeyChanged += (s, e) =>
                               {
                                   fired = true;
                                   oldKey = e.OldKey;
                                   newKey = e.NewKey;
                               };

        label.HotKey = Key.Empty;

        Assert.True (fired);
        Assert.Equal (Key.A.WithAlt, oldKey);
        Assert.Equal (Key.Empty, newKey);
    }

    [Fact]
    public void TestAssignTextToLabel ()
    {
        var label = new Label ();
        label.Text = "Test";
        Assert.Equal ("Test", label.Text);
    }

    [Fact]
    public void CanFocus_False_HotKey_SetsFocus_Next ()
    {
        View otherView = new () { Text = "otherView", CanFocus = true };

        Label label = new () { Text = "_label" };

        View nextView = new () { Text = "nextView", CanFocus = true };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (otherView, label, nextView);
        otherView.SetFocus ();

        // runnable.SetFocus ();
        Assert.True (otherView.HasFocus);

        app.Keyboard.RaiseKeyDownEvent (label.HotKey);
        Assert.False (otherView.HasFocus);
        Assert.False (label.HasFocus);
        Assert.True (nextView.HasFocus);
    }

    [Fact]
    public void CanFocus_False_LeftButtonReleased_SetsFocus_Next ()
    {
        View otherView = new ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Id = "otherView",
            CanFocus = true
        };
        Label label = new () { X = 0, Y = 1, Text = "_label" };

        View nextView = new ()
        {
            X = Pos.Right (label),
            Y = Pos.Top (label),
            Width = 1,
            Height = 1,
            Id = "nextView",
            CanFocus = true
        };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (otherView, label, nextView);
        otherView.SetFocus ();

        // click on label
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = label.Frame.Location, Flags = MouseFlags.LeftButtonReleased });
        Assert.False (label.HasFocus);
        Assert.True (nextView.HasFocus);
    }

    [Fact]
    public void CanFocus_True_HotKey_SetsFocus ()
    {
        Label label = new () { Text = "_label", CanFocus = true };

        View view = new () { Text = "view", CanFocus = true };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new ();
        app.Begin (runnable);
        runnable.Add (label, view);

        view.SetFocus ();
        Assert.True (label.CanFocus);
        Assert.False (label.HasFocus);
        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        // No focused view accepts Tab, and there's no other view to focus, so OnKeyDown returns false
        app.Keyboard.RaiseKeyDownEvent (label.HotKey);
        Assert.True (label.HasFocus);
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void CanFocus_True_LeftButtonReleased_Focuses ()
    {
        Label label = new () { Text = "label", X = 0, Y = 0, CanFocus = true };

        View otherView = new ()
        {
            Text = "view",
            X = 0,
            Y = 1,
            Width = 4,
            Height = 1,
            CanFocus = true
        };

        IApplication app = Application.Create ();
        Runnable<bool> runnable = new () { Width = 10, Height = 10 };
        ;
        app.Begin (runnable);
        runnable.Add (label, otherView);
        label.SetFocus ();

        Assert.True (label.CanFocus);
        Assert.True (label.HasFocus);
        Assert.True (otherView.CanFocus);
        Assert.False (otherView.HasFocus);

        otherView.SetFocus ();
        Assert.True (otherView.HasFocus);

        // label can focus, so clicking on it set focus
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        Assert.True (label.HasFocus);
        Assert.False (otherView.HasFocus);

        // click on view
        app.Mouse.RaiseMouseEvent (new Mouse { ScreenPosition = new Point (0, 1), Flags = MouseFlags.LeftButtonReleased });
        Assert.False (label.HasFocus);
        Assert.True (otherView.HasFocus);
    }

    [Fact]
    public void With_Top_Margin_Without_Top_Border ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        Runnable<bool> runnable = new () { Width = 10, Height = 10 };
        ;
        app.Begin (runnable);

        var label = new Label { Text = "Test", /*Width = 6, Height = 3,*/ BorderStyle = LineStyle.Single };
        label.Margin.Thickness = new Thickness (0, 1, 0, 0);
        label.Border.Thickness = new Thickness (1, 0, 1, 1);
        runnable.Add (label);
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 0, 6, 3), label.Frame);
        Assert.Equal (new Rectangle (0, 0, 4, 1), label.Viewport);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
│Test│
└────┘",
                                                       output,
                                                       app.Driver);
    }

    [Fact]
    public void Without_Top_Border ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        Runnable<bool> runnable = new () { Width = 10, Height = 10 };
        ;
        app.Begin (runnable);

        var label = new Label { Text = "Test", /* Width = 6, Height = 3, */BorderStyle = LineStyle.Single };
        label.Border.Thickness = new Thickness (1, 0, 1, 1);
        runnable.Add (label);
        app.LayoutAndDraw ();

        Assert.Equal (new Rectangle (0, 0, 6, 2), label.Frame);
        Assert.Equal (new Rectangle (0, 0, 4, 1), label.Viewport);

        DriverAssert.AssertDriverContentsWithFrameAre (@"
│Test│
└────┘",
                                                       output,
                                                       app.Driver);
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void Label_CannotFocus_ByDefault ()
    {
        Label label = new () { Text = "Test" };

        // Label usually has CanFocus = false
        Assert.False (label.CanFocus);

        label.Dispose ();
    }

    // BUGBUG: This test does not actually test what it says; just tests that the invoke returns true
    [Fact]
    public void Label_HotKey_ForwardsToNextFocusable ()
    {
        Label label = new () { Text = "_Test" };

        // Label's HotKey command is always handled by the default handler even when
        // there's no next focusable view - DefaultHotKeyHandler returns true
        bool? result = label.InvokeCommand (Command.HotKey);

        Assert.True (result);

        label.Dispose ();
    }
}
