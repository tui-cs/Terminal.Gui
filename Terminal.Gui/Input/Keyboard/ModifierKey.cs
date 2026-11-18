namespace Terminal.Gui.Input;

/// <summary>
///     Identifies a specific modifier key for standalone modifier key events.
/// </summary>
/// <remarks>
///     <para>
///         Used by <see cref="Key.ModifierKey"/> to indicate which modifier key was pressed or released
///         as a standalone event (e.g., pressing Shift alone without any other key).
///     </para>
///     <para>
///         Not all drivers distinguish left from right modifier keys. When the driver cannot distinguish,
///         the non-sided variant (e.g., <see cref="Shift"/>) is used.
///     </para>
/// </remarks>
public enum ModifierKey
{
    /// <summary>
    ///     Not a modifier key event.
    /// </summary>
    None = 0,

    /// <summary>Shift (side not distinguished).</summary>
    Shift,

    /// <summary>Left Shift.</summary>
    LeftShift,

    /// <summary>Right Shift.</summary>
    RightShift,

    /// <summary>Ctrl (side not distinguished).</summary>
    Ctrl,

    /// <summary>Left Ctrl.</summary>
    LeftCtrl,

    /// <summary>Right Ctrl.</summary>
    RightCtrl,

    /// <summary>Alt (side not distinguished).</summary>
    Alt,

    /// <summary>Left Alt.</summary>
    LeftAlt,

    /// <summary>Right Alt.</summary>
    RightAlt,

    /// <summary>AltGr / ISO Level 3 Shift.</summary>
    AltGr,

    /// <summary>Super / Windows / Cmd key (side not distinguished).</summary>
    Super,

    /// <summary>Left Super / Windows / Cmd key.</summary>
    LeftSuper,

    /// <summary>Right Super / Windows / Cmd key.</summary>
    RightSuper,

    /// <summary>Hyper key (side not distinguished).</summary>
    Hyper,

    /// <summary>Left Hyper key.</summary>
    LeftHyper,

    /// <summary>Right Hyper key.</summary>
    RightHyper,

    /// <summary>Caps Lock.</summary>
    CapsLock,

    /// <summary>Num Lock.</summary>
    NumLock,

    /// <summary>Scroll Lock.</summary>
    ScrollLock
}
