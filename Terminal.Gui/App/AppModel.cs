namespace Terminal.Gui.App;

/// <summary>
///     Defines the rendering mode for the application.
/// </summary>
public enum AppModel
{
    /// <summary>
    ///     The application occupies the full terminal screen via the alternate screen buffer.
    ///     This is the default behavior.
    /// </summary>
    FullScreen,

    /// <summary>
    ///     The application renders inline within the primary (scrollback) buffer, anchored
    ///     to the bottom of the visible terminal. No alternate screen buffer is used.
    /// </summary>
    Inline
}
