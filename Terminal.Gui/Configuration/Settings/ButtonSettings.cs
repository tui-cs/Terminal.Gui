namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.Button"/> visual defaults (ThemeScope).
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Default"/> is the compile-time-known fallback (constructor defaults).
///         <see cref="Current"/> holds the currently effective values and is updated atomically by
///         <see cref="MecThemeManager"/> via <c>Volatile.Write</c> at startup and on theme switch. Mid-render
///         consumers always observe either the previous or the next reference — never a partially populated one.
///     </para>
/// </remarks>
public sealed record ButtonSettings
{
    /// <summary>Gets the default shadow style for buttons.</summary>
    public ShadowStyles DefaultShadow { get; init; } = ShadowStyles.Opaque;

    /// <summary>Gets the default mouse highlight states for buttons.</summary>
    public MouseState DefaultMouseHighlightStates { get; init; } = MouseState.In | MouseState.Pressed | MouseState.PressedOutside;

    /// <summary>The compile-time-known defaults.</summary>
    public static ButtonSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static ButtonSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static ButtonSettings _current = Default;
}
