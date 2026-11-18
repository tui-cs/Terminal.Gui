namespace ViewBaseTests.MouseTests;

// Claude - Opus 4.5
/// <summary>
///     Tests for default View activation behavior (LeftButtonReleased → Command.Activate).
///     Verifies that the base View class follows industry-standard GUI conventions by activating
///     on button release rather than press, allowing cancellation by dragging away.
///     Related to issue #4674: https://github.com/tui-cs/Terminal.Gui/issues/4674
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "Mouse")]
public class DefaultActivationTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Default Activation on Released, Not Pressed

    [Fact]
    public void DefaultActivation_FiresOnRelease_NotOnPress ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View view = new () { Width = 10, Height = 10 };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatedCount = 0;
        view.Activating += (_, _) => activatedCount++;

        // Act - Press should NOT activate
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.Equal (0, activatedCount); // Should NOT activate on press

        // Act - Release should activate
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });
        Assert.Equal (1, activatedCount); // Should activate on release

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void DefaultActivation_WithCtrl_BoundToRelease ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        View view = new () { Width = 10, Height = 10 };

        // Assert - Context command bound to Released with Ctrl
        IEnumerable<MouseFlags> contextBindings = view.MouseBindings.GetAllFromCommands (Command.Context);
        Assert.Contains (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl, contextBindings);

        // Assert - Context command NOT bound to Pressed
        Assert.DoesNotContain (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, contextBindings);
    }

    #endregion

    #region Cancellation Behavior

    [Fact]
    public void DefaultActivation_ReleaseInside_Activates ()
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
            MouseHighlightStates = MouseState.Pressed
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activated = false;
        view.Activating += (_, _) => activated = true;

        // Act - Press inside, release inside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.True (app.Mouse.IsGrabbed (view)); // Should grab on press

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });

        // Assert
        Assert.True (activated); // Should activate when released inside
        Assert.False (app.Mouse.IsGrabbed (view)); // Should ungrab after clicked event

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void DefaultActivation_Cancellation_ReleaseOutside ()
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
            MouseHighlightStates = MouseState.Pressed
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activated = false;
        view.Activating += (_, _) => activated = true;

        // Act - Press inside, release outside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.True (app.Mouse.IsGrabbed (view)); // Should grab on press

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (50, 50) }); // Outside

        // Assert
        Assert.False (activated); // Should NOT activate when released outside
        Assert.False (app.Mouse.IsGrabbed (view)); // Should ungrab after clicked event

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void DefaultActivation_Cancellation_DragAway ()
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
            MouseHighlightStates = MouseState.Pressed
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activated = false;
        view.Activating += (_, _) => activated = true;

        // Act - Press inside, drag to edge, then outside, release outside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.True (app.Mouse.IsGrabbed (view));

        // Drag to edge
        app.InjectMouse (new Mouse { Flags = MouseFlags.PositionReport, ScreenPosition = new Point (9, 9) });

        // Drag outside
        app.InjectMouse (new Mouse { Flags = MouseFlags.PositionReport, ScreenPosition = new Point (15, 15) });

        // Release outside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (15, 15) });

        // Assert
        Assert.False (activated); // Should NOT activate when released outside after drag
        Assert.False (app.Mouse.IsGrabbed (view));

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region AutoGrab with MouseHighlightStates

    [Fact]
    public void DefaultActivation_AutoGrab_ActivatesOnReleaseInside ()
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
            MouseHighlightStates = MouseState.Pressed
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activated = false;
        view.Activating += (_, _) => activated = true;

        // Act - Press and Release inside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.False (activated); // Should NOT activate on press

        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });
        Assert.True (activated); // Should activate on release inside

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void DefaultActivation_AutoGrab_MouseStateUpdated ()
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
            MouseHighlightStates = MouseState.Pressed
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        // Initial state
        Assert.True ((view.MouseState & MouseState.Pressed) == MouseState.None);

        // Act - Press inside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });

        // Assert - MouseState.Pressed set
        Assert.True ((view.MouseState & MouseState.Pressed) != MouseState.None);

        // Act - Release inside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });

        // Assert - MouseState.Pressed cleared
        Assert.True ((view.MouseState & MouseState.Pressed) == MouseState.None);

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region Backward Compatibility - Custom Bindings

    [Fact]
    public void CustomPressedBinding_StillWorks ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View view = new () { Width = 10, Height = 10 };
        (runnable as View)?.Add (view);

        // Replace default Released binding with Pressed binding (old behavior)
        view.MouseBindings.Clear ();
        view.MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);

        app.Begin (runnable);

        var activatedCount = 0;
        view.Activating += (_, _) => activatedCount++;

        // Act - Press should activate (custom binding)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.Equal (1, activatedCount); // Should activate on press with custom binding

        // Act - Release should NOT activate (no binding)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });
        Assert.Equal (1, activatedCount); // Should remain 1 (no additional activation)

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void CustomReleasedBinding_ReplacesDefault ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        View view = new () { Width = 10, Height = 10 };
        (runnable as View)?.Add (view);

        // Replace default Released binding (Activate) with Accept
        view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonReleased, Command.Accept);

        app.Begin (runnable);

        var activatedCount = 0;
        var acceptedCount = 0;
        view.Activating += (_, _) => activatedCount++;
        view.Accepting += (_, _) => acceptedCount++;

        // Act - Release should trigger only Accept (replaced Activate)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });

        // Assert - Only Accept invoked (Activate was replaced)
        Assert.Equal (0, activatedCount); // Replaced
        Assert.Equal (1, acceptedCount); // Custom binding

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region No MouseHighlightStates (No Auto-Grab)

    [Fact]
    public void DefaultActivation_NoAutoGrab_ReleasedStillInvokesCommand ()
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
            MouseHighlightStates = MouseState.None // No auto-grab
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activated = false;
        view.Activating += (_, _) => activated = true;

        // Act - Press (no grab without MouseHighlightStates)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.False (app.Mouse.IsGrabbed (view)); // Should NOT grab without MouseHighlightStates

        // Act - Release inside
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });

        // Assert - Command still invoked even without grab
        Assert.True (activated);

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void DefaultActivation_NoAutoGrab_ReleaseOutside_DoesNotActivate ()
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
            MouseHighlightStates = MouseState.None // No auto-grab
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activated = false;
        view.Activating += (_, _) => activated = true;

        // Act - Press inside (no grab)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.False (app.Mouse.IsGrabbed (view));

        // Act - Release outside (view won't receive it without grab)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (50, 50) });

        // Assert - Should NOT activate (release was outside and no grab means view doesn't see it)
        Assert.False (activated);

        (runnable as View)?.Dispose ();
    }

    #endregion

    #region MouseHoldRepeat Interaction

    [Fact]
    public void DefaultActivation_MouseHoldRepeat_Null_UsesDefaultReleasedBinding ()
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
            MouseHighlightStates = MouseState.Pressed,
            MouseHoldRepeat = null // Disabled
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatedCount = 0;
        view.Activating += (_, _) => activatedCount++;

        // Act - Press should NOT activate
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.Equal (0, activatedCount);

        // Act - Release should activate
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });
        Assert.Equal (1, activatedCount);

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void DefaultActivation_MouseHoldRepeat_Released_OverridesDefault ()
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
            MouseHighlightStates = MouseState.Pressed,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased // Override to Released
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatedCount = 0;
        view.Activating += (_, _) => activatedCount++;

        // Act - Press starts timer, but doesn't activate immediately
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new Point (5, 5) });
        Assert.Equal (0, activatedCount);

        // Act - Release activates (MouseHoldRepeat flag matches)
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new Point (5, 5) });
        Assert.Equal (1, activatedCount);

        (runnable as View)?.Dispose ();
    }

    #endregion
}
