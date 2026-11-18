namespace ViewBaseTests.MouseTests;

// Claude - Opus 4.5
/// <summary>
///     Tests for MouseBinding with xxxReleased flags.
///     Verifies that custom bindings for Released events are properly invoked.
///     Related to issue #4674: https://github.com/tui-cs/Terminal.Gui/issues/4674
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "Mouse")]
public class MouseReleasedBindingTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Phase 1.1: Basic Released Binding Invocation

    [Fact]
    public void LeftButtonReleased_CustomBinding_InvokesCommand_WhenMouseHighlightStatesNone ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = MouseState.None
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Replace default Released binding (Activate) with custom binding (Accept)
        view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonReleased, Command.Accept);

        var commandInvoked = false;
        view.Accepting += (_, _) => commandInvoked = true;

        // Act - Press then Release
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert
        Assert.True (commandInvoked, "Command.Accept should have been invoked on LeftButtonReleased");

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void MiddleButtonReleased_CustomBinding_InvokesCommand ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = MouseState.None
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Add custom binding for Released
        view.MouseBindings.Add (MouseFlags.MiddleButtonReleased, Command.Accept);

        var commandInvoked = false;
        view.Accepting += (_, _) => commandInvoked = true;

        // Act - Press then Release
        app.InjectMouse (new Mouse { Flags = MouseFlags.MiddleButtonPressed, ScreenPosition = new Point (0, 0) });
        app.InjectMouse (new Mouse { Flags = MouseFlags.MiddleButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert
        Assert.True (commandInvoked, "Command.Accept should have been invoked on MiddleButtonReleased");

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void RightButtonReleased_CustomBinding_InvokesCommand ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = MouseState.None
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Add custom binding for Released
        view.MouseBindings.Add (MouseFlags.RightButtonReleased, Command.Accept);

        var commandInvoked = false;
        view.Accepting += (_, _) => commandInvoked = true;

        // Act - Press then Release
        app.InjectMouse (new Mouse { Flags = MouseFlags.RightButtonPressed, ScreenPosition = new Point (0, 0) });
        app.InjectMouse (new Mouse { Flags = MouseFlags.RightButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert
        Assert.True (commandInvoked, "Command.Accept should have been invoked on RightButtonReleased");

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region Phase 1.2: Released Binding with AutoGrab (MouseHighlightStates)

    [Theory]
    [InlineData (MouseState.In)]
    [InlineData (MouseState.Pressed)]
    [InlineData (MouseState.In | MouseState.Pressed)]
    public void LeftButtonReleased_CustomBinding_InvokesCommand_WithMouseHighlightStates (MouseState highlightStates)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = highlightStates // Triggers AutoGrab
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Replace default Released binding (Activate) with custom binding (Accept)
        view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonReleased, Command.Accept);

        var commandInvoked = false;
        view.Accepting += (_, _) => commandInvoked = true;

        // Act - Press (triggers grab), then Release (triggers ungrab)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });
        Assert.True (app.Mouse.IsGrabbed (view), "Mouse should be grabbed after press");

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert
        Assert.True (commandInvoked, "Command.Accept should have been invoked on LeftButtonReleased");
        Assert.False (app.Mouse.IsGrabbed (view), "Mouse should be ungrabbed after click completes");

        (runnable as View)?.Dispose ();
    }

    [Theory]
    [InlineData (MouseState.In)]
    [InlineData (MouseState.Pressed)]
    public void MiddleButtonReleased_CustomBinding_InvokesCommand_WithMouseHighlightStates (MouseState highlightStates)
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = highlightStates
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Add custom binding for Released
        view.MouseBindings.Add (MouseFlags.MiddleButtonReleased, Command.Accept);

        var commandInvoked = false;
        view.Accepting += (_, _) => commandInvoked = true;

        // Act
        app.InjectMouse (new Mouse { Flags = MouseFlags.MiddleButtonPressed, ScreenPosition = new Point (0, 0) });
        app.InjectMouse (new Mouse { Flags = MouseFlags.MiddleButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert
        Assert.True (commandInvoked, "Command.Accept should have been invoked on MiddleButtonReleased");

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region Phase 1.3: Released Binding vs. Default Behavior

    [Fact]
    public void LeftButtonReleased_DefaultBinding_InvokesActivate ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = MouseState.Pressed // Enable AutoGrab
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Use default binding - test new default behavior (issue #4674)
        var activatingInvoked = false;
        var acceptingInvoked = false;
        view.Activating += (_, _) => activatingInvoked = true;
        view.Accepting += (_, _) => acceptingInvoked = true;

        // Act - Press then Release
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });

        // Press should NOT activate (default changed to Released)
        Assert.False (activatingInvoked, "Pressed should not invoke Activating with default bindings");

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Released SHOULD invoke Activate by default (new behavior)
        Assert.True (activatingInvoked, "Released event should invoke Activating with default binding");
        Assert.False (acceptingInvoked, "Released event should not invoke Accepting by default");

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void LeftButtonPressed_CustomBinding_CoexistsWithDefaultReleasedBinding ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = MouseState.Pressed // Enable AutoGrab
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Add custom Pressed binding alongside default Released binding (default changed to Released, issue #4674)
        view.MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Accept);

        var activatingInvoked = false;
        var acceptingInvoked = false;
        view.Activating += (_, _) => activatingInvoked = true;
        view.Accepting += (_, _) => acceptingInvoked = true;

        // Act - Press then Release
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });
        Assert.True (acceptingInvoked, "Command.Accept (custom Pressed binding) should have been invoked");

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Both Pressed and Released bindings should fire
        Assert.True (acceptingInvoked, "Command.Accept (Pressed) should have been invoked");
        Assert.True (activatingInvoked, "Command.Activate (default Released) should have been invoked");

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void LeftButtonReleased_MultipleCommands_InvokesAllCommands ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();

        View view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10,
            MouseHighlightStates = MouseState.Pressed // Enable AutoGrab
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Replace default Released binding with multiple commands
        view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonReleased, Command.Accept, Command.HotKey);

        var acceptingInvoked = false;
        var hotKeyInvoked = false;
        view.Accepting += (_, _) => acceptingInvoked = true;
        view.HandlingHotKey += (_, _) => hotKeyInvoked = true;

        // Act - Press then Release
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (0, 0) });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (0, 0) });

        // Assert - Both commands should be invoked
        Assert.True (acceptingInvoked, "Command.Accept should have been invoked");
        Assert.True (hotKeyInvoked, "Command.HotKey should have been invoked");

        (runnable as View)?.Dispose ();
    }

    #endregion
}
