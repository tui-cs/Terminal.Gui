namespace Terminal.Gui.Testing;

/// <summary>
///     Extension methods for input injection. See <see cref="InputInjector"/> for details.
/// </summary>
public static class InputInjectionExtensions
{
    /// <param name="app">The application instance.</param>
    extension(IApplication app)
    {
        /// <summary>
        ///     Injects a key event (convenience method).
        /// </summary>
        /// <param name="key">The key to inject.</param>
        public void InjectKey (Key key) => app.GetInputInjector ().InjectKey (key);

        /// <summary>
        ///     Injects a mouse event (convenience method).
        /// </summary>
        /// <param name="mouseEvent">The mouse event to inject.</param>
        public void InjectMouse (Mouse mouseEvent) => app.GetInputInjector ().InjectMouse (mouseEvent);

        /// <summary>
        ///     Injects a sequence of events (convenience method).
        /// </summary>
        /// <param name="events">The events to inject.</param>
        public void InjectSequence (params InputInjectionEvent [] events) => app.GetInputInjector ().InjectSequence (events);
    }

    /// <summary>
    ///     Gets the injection events for a left button click at the specified point.
    /// </summary>
    /// <param name="p">The screen position for the click.</param>
    /// <returns>An array of injection events representing a left button click.</returns>
    public static InputInjectionEvent [] LeftButtonClick (Point p) =>
    [
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) },
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) }
    ];

    /// <summary>
    ///     Gets the injection events for a right button click at the specified point.
    /// </summary>
    /// <param name="p">The screen position for the click.</param>
    /// <returns>An array of injection events representing a right button click.</returns>
    public static InputInjectionEvent [] RightButtonClick (Point p) =>
    [
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.RightButtonPressed, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) },
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.RightButtonReleased, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) }
    ];

    /// <summary>
    ///     Gets the injection events for a left button double-click at the specified point.
    /// </summary>
    /// <param name="p">The screen position for the double-click.</param>
    /// <returns>An array of injection events representing a left button double-click.</returns>
    public static InputInjectionEvent [] LeftButtonDoubleClick (Point p) =>
    [
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (0) },
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) },
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) },
        new MouseInjectionEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = p }) { Delay = TimeSpan.FromMilliseconds (10) }
    ];
}
