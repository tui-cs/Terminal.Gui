namespace InputTests;

/// <summary>
///     Tests for InputInjectionExtensions helper methods.
///     These tests verify the helper methods that create common mouse click sequences.
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "InputInjection")]
public class InputInjectionExtensionsTests
{
    #region LeftButtonClick Tests

    [Fact]
    public void LeftButtonClick_ReturnsCorrectSequence ()
    {
        // Arrange
        Point clickPosition = new (10, 5);

        // Act
        InputInjectionEvent [] events = InputInjectionExtensions.LeftButtonClick (clickPosition);

        // Assert
        Assert.NotNull (events);
        Assert.Equal (2, events.Length);

        // First event: LeftButtonPressed
        Assert.IsType<MouseInjectionEvent> (events [0]);
        var pressEvent = (MouseInjectionEvent)events [0];
        Assert.Equal (MouseFlags.LeftButtonPressed, pressEvent.Mouse.Flags);
        Assert.Equal (clickPosition, pressEvent.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), pressEvent.Delay);

        // Second event: LeftButtonReleased
        Assert.IsType<MouseInjectionEvent> (events [1]);
        var releaseEvent = (MouseInjectionEvent)events [1];
        Assert.Equal (MouseFlags.LeftButtonReleased, releaseEvent.Mouse.Flags);
        Assert.Equal (clickPosition, releaseEvent.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), releaseEvent.Delay);
    }

    [Fact]
    public void LeftButtonClick_WithApplication_ProducesClickEvent ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        Point clickPosition = new (5, 5);

        // Act
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (clickPosition));

        // Assert - Should receive Press, Release, and synthesized Click
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonReleased));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.Equal (3, receivedFlags.Count);
    }

    #endregion

    #region RightButtonClick Tests

    [Fact]
    public void RightButtonClick_ReturnsCorrectSequence ()
    {
        // Arrange
        Point clickPosition = new (15, 8);

        // Act
        InputInjectionEvent [] events = InputInjectionExtensions.RightButtonClick (clickPosition);

        // Assert
        Assert.NotNull (events);
        Assert.Equal (2, events.Length);

        // First event: RightButtonPressed
        Assert.IsType<MouseInjectionEvent> (events [0]);
        var pressEvent = (MouseInjectionEvent)events [0];
        Assert.Equal (MouseFlags.RightButtonPressed, pressEvent.Mouse.Flags);
        Assert.Equal (clickPosition, pressEvent.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), pressEvent.Delay);

        // Second event: RightButtonReleased
        Assert.IsType<MouseInjectionEvent> (events [1]);
        var releaseEvent = (MouseInjectionEvent)events [1];
        Assert.Equal (MouseFlags.RightButtonReleased, releaseEvent.Mouse.Flags);
        Assert.Equal (clickPosition, releaseEvent.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), releaseEvent.Delay);
    }

    [Fact]
    public void RightButtonClick_WithApplication_ProducesClickEvent ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        Point clickPosition = new (3, 2);

        // Act
        app.InjectSequence (InputInjectionExtensions.RightButtonClick (clickPosition));

        // Assert - Should receive Press, Release, and synthesized Click
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonPressed));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonReleased));
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.RightButtonClicked));
        Assert.Equal (3, receivedFlags.Count);
    }

    #endregion

    #region LeftButtonDoubleClick Tests

    [Fact]
    public void LeftButtonDoubleClick_ReturnsCorrectSequence ()
    {
        // Arrange
        Point clickPosition = new (20, 10);

        // Act
        InputInjectionEvent [] events = InputInjectionExtensions.LeftButtonDoubleClick (clickPosition);

        // Assert
        Assert.NotNull (events);
        Assert.Equal (4, events.Length);

        // First click: Press
        Assert.IsType<MouseInjectionEvent> (events [0]);
        var firstPress = (MouseInjectionEvent)events [0];
        Assert.Equal (MouseFlags.LeftButtonPressed, firstPress.Mouse.Flags);
        Assert.Equal (clickPosition, firstPress.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (0), firstPress.Delay);

        // First click: Release
        Assert.IsType<MouseInjectionEvent> (events [1]);
        var firstRelease = (MouseInjectionEvent)events [1];
        Assert.Equal (MouseFlags.LeftButtonReleased, firstRelease.Mouse.Flags);
        Assert.Equal (clickPosition, firstRelease.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), firstRelease.Delay);

        // Second click: Press
        Assert.IsType<MouseInjectionEvent> (events [2]);
        var secondPress = (MouseInjectionEvent)events [2];
        Assert.Equal (MouseFlags.LeftButtonPressed, secondPress.Mouse.Flags);
        Assert.Equal (clickPosition, secondPress.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), secondPress.Delay);

        // Second click: Release
        Assert.IsType<MouseInjectionEvent> (events [3]);
        var secondRelease = (MouseInjectionEvent)events [3];
        Assert.Equal (MouseFlags.LeftButtonReleased, secondRelease.Mouse.Flags);
        Assert.Equal (clickPosition, secondRelease.Mouse.ScreenPosition);
        Assert.Equal (TimeSpan.FromMilliseconds (10), secondRelease.Delay);
    }

    [Fact]
    public void LeftButtonDoubleClick_WithApplication_ProducesDoubleClickEvent ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        List<MouseFlags> receivedFlags = [];
        app.Mouse.MouseEvent += (s, e) => receivedFlags.Add (e.Flags);

        Point clickPosition = new (7, 4);

        // Act
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (clickPosition));

        // Assert - Should receive two clicks plus double-click event
        int pressCount = receivedFlags.Count (f => f.HasFlag (MouseFlags.LeftButtonPressed));
        int releaseCount = receivedFlags.Count (f => f.HasFlag (MouseFlags.LeftButtonReleased));

        Assert.Equal (2, pressCount);
        Assert.Equal (2, releaseCount);
        Assert.Contains (receivedFlags, f => f.HasFlag (MouseFlags.LeftButtonDoubleClicked));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void LeftButtonClick_OnButton_TriggersAccepting ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        Button button = new () { Text = "Click Me" };
        runnable?.Add (button);

        var acceptingCalled = false;
        button.Accepting += (s, e) => acceptingCalled = true;

        // Act
        app.InjectSequence (InputInjectionExtensions.LeftButtonClick (new Point (0, 0)));

        // Assert
        Assert.True (acceptingCalled);
        runnable?.Dispose ();
    }

    [Fact]
    public void LeftButtonDoubleClick_OnCheckBox_TogglesCheckState ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        CheckBox checkBox = new () { Text = "_Checkbox" };
        runnable?.Add (checkBox);

        CheckState initialState = checkBox.Value;

        // Act - Double-click should toggle twice, returning to initial state
        app.InjectSequence (InputInjectionExtensions.LeftButtonDoubleClick (new Point (0, 0)));

        // Assert - After double-click, state should toggle
        Assert.NotEqual (initialState, checkBox.Value);
        runnable?.Dispose ();
    }

    [Fact]
    public void RightButtonClick_OnView_RaisesMouseEvent ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        app.Begin (runnable);

        View testView = new () { Width = 10, Height = 5 };
        runnable?.Add (testView);

        var rightClickReceived = false;

        testView.MouseEvent += (s, e) =>
                               {
                                   if (e.Flags.HasFlag (MouseFlags.RightButtonClicked))
                                   {
                                       rightClickReceived = true;
                                   }
                               };

        // Act
        app.InjectSequence (InputInjectionExtensions.RightButtonClick (new Point (5, 2)));

        // Assert
        Assert.True (rightClickReceived);
        runnable?.Dispose ();
    }

    #endregion
}
