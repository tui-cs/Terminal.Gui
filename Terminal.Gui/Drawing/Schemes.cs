namespace Terminal.Gui.Drawing;

// ReSharper disable InconsistentNaming
/// <summary>
///     The built-in scheme names used by <see cref="SchemeManager"/>.
///     <see cref="SchemeManager.GetSchemeNames"/> returns a collection valid scheme names, based on this enum.
/// </summary>
public enum Schemes
{
    /// <summary>
    ///     The base scheme used for most Views.
    /// </summary>
    Base,

    /// <summary>
    ///     The accent scheme; a secondary/alternate scheme for visual distinction.
    ///     Used for panels, event logs, secondary content areas, or any view that needs visual separation.
    ///     Colors are algorithmically derived from <see cref="Base"/> at draw time with an opaque background.
    /// </summary>
    Accent,

    /// <summary>
    ///     The dialog scheme, used for Dialog, MessageBox, and other views dialog-like views.
    /// </summary>
    Dialog,

    /// <summary>
    ///     The menu scheme, used for Terminal.Gui.Menu, MenuBar, and StatusBar.
    /// </summary>
    Menu,

    /// <summary>
    ///     The scheme for showing errors.
    /// </summary>
    Error
}
