using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>Event args for draw events</summary>
public class DrawEventArgs : CancelEventArgs
{
    /// <summary>Creates a new instance of the <see cref="DrawEventArgs"/> class.</summary>
    /// <param name="newViewport">
    ///     The Content-relative rectangle describing the new visible viewport into the
    ///     <see cref="View"/>.
    /// </param>
    /// <param name="oldViewport">
    ///     The Content-relative rectangle describing the old visible viewport into the
    ///     <see cref="View"/>.
    /// </param>
    /// <param name="drawContext">
    ///     Add any regions that have been drawn to during <see cref="View.Draw(DrawContext?)"/> operations to this context. This is
    ///     primarily
    ///     in support of <see cref="ViewportSettingsFlags.Transparent"/>.
    /// </param>
    public DrawEventArgs (Rectangle newViewport, Rectangle oldViewport, DrawContext? drawContext)
    {
        NewViewport = newViewport;
        OldViewport = oldViewport;
        DrawContext = drawContext;
    }

    /// <summary>Gets the Content-relative rectangle describing the old visible viewport into the <see cref="View"/>.</summary>
    public Rectangle OldViewport { get; }

    /// <summary>Gets the Content-relative rectangle describing the currently visible viewport into the <see cref="View"/>.</summary>
    public Rectangle NewViewport { get; }

    /// <summary>
    ///     Add any regions that have been drawn to during <see cref="View.Draw(DrawContext?)"/> operations to this context. This is
    ///     primarily
    ///     in support of <see cref="ViewportSettingsFlags.Transparent"/>.
    /// </summary>
    public DrawContext? DrawContext { get; }
}
