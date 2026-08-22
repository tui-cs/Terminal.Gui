namespace Terminal.Gui.Configuration;

/// <summary>
///     Immutable settings record for <see cref="Views.PopoverMenu"/> defaults (SettingsScope).
/// </summary>
public sealed record PopoverMenuSettings
{
    /// <summary>Gets the default activation key for popover menus.</summary>
    public Key DefaultKey { get; init; } = Key.F10.WithShift;

    /// <summary>The compile-time-known defaults.</summary>
    public static PopoverMenuSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static PopoverMenuSettings Current
    {
        get => Volatile.Read (ref _current);
        set => Volatile.Write (ref _current, value);
    }

    private static PopoverMenuSettings _current = Default;
}
