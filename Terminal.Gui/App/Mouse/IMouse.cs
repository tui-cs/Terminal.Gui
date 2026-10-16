using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     Defines a contract for mouse event handling and state management in a Terminal.Gui application.
///     <para>
///         This interface allows for decoupling of mouse-related functionality from the static <see cref="App"/> class,
///         enabling better testability and parallel test execution.
///     </para>
/// </summary>
public interface IMouse : IMouseGrabHandler
{
    /// <summary>
    ///     Sets the application instance that this mouse handler is associated with.
    ///     This provides access to application state without coupling to static Application class.
    /// </summary>
    IApplication? App { get; set; }

    /// <summary>
    ///     Gets or sets the last known position of the mouse in screen coordinates.
    /// </summary>
    Point? LastMousePosition { get; set; }

    /// <summary>
    ///     Gets or sets whether the mouse is disabled. The mouse is enabled by default.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         With the built-in ANSI driver, this also disables terminal mouse reporting so native text selection is
    ///         available. Other drivers only suppress event delivery. To avoid enabling reporting at startup, set this
    ///         property before <see cref="IApplication.Init"/>, or set <c>Application.IsMouseDisabled</c> in configuration
    ///         applied before the application is created.
    ///     </para>
    ///     <para>
    ///         Disabling force-releases any mouse grab without raising the cancelable
    ///         <see cref="IMouseGrabHandler.UnGrabbingMouse"/> event (<see cref="IMouseGrabHandler.UnGrabbedMouse"/> is still
    ///         raised), stops hold repetition, clears hover state, and ends dispatch of the current mouse event.
    ///         <see cref="IMouseGrabHandler.GrabMouse"/> is ignored while the mouse is disabled.
    ///     </para>
    ///     <para>
    ///         To change this during a session, use the UI thread or <see cref="IApplication.Invoke(Action)"/>. When set from
    ///         another thread, the terminal mode changes immediately and the view-state release is queued to the UI thread.
    ///     </para>
    /// </remarks>
    bool IsMouseDisabled { get; set; }

    /// <summary>
    ///     Gets the list of non-<see cref="ViewportSettingsFlags.TransparentMouse"/> views that are currently under the mouse.
    /// </summary>
    List<View?> CachedViewsUnderMouse { get; }

    /// <summary>
    ///     Raised when a mouse event occurs. Can be cancelled by setting <see cref="HandledEventArgs.Handled"/> to
    ///     <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Mouse.ScreenPosition"/> coordinates are screen-relative.
    ///     </para>
    ///     <para>
    ///         <see cref="Mouse.View"/> will be the deepest view under the mouse.
    ///     </para>
    ///     <para>
    ///         <see cref="Mouse.Position"/> coordinates are view-relative. Only valid if <see cref="Mouse.View"/> is set.
    ///     </para>
    ///     <para>
    ///         Use this even to handle mouse events at the application level, before View-specific handling.
    ///     </para>
    /// </remarks>
    event EventHandler<Mouse>? MouseEvent;

    /// <summary>
    ///     INTERNAL API: Called when a mouse event is raised by the driver. Determines the view under the mouse and
    ///     calls the appropriate View mouse event handlers.
    /// </summary>
    /// <remarks>Can be used to simulate mouse events without a driver. Use <see cref="InputInjector"/> for input injection.</remarks>
    /// <param name="mouse">The mouse event with coordinates relative to the screen.</param>
    void RaiseMouseEvent (Mouse mouse);

    /// <summary>
    ///     INTERNAL: Raises the MouseEnter and MouseLeave events for the views that are under the mouse.
    /// </summary>
    /// <param name="screenPosition">The position of the mouse.</param>
    /// <param name="currentViewsUnderMouse">The most recent result from GetViewsUnderLocation().</param>
    void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse);

    /// <summary>
    ///     INTERNAL: Resets mouse state, clearing event handlers and cached views.
    /// </summary>
    void ResetState ();
}
