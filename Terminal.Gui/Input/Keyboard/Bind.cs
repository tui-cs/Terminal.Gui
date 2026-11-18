namespace Terminal.Gui.Input;

/// <summary>
///     Provides ergonomic factory methods for creating <see cref="PlatformKeyBinding"/> instances.
/// </summary>
public static class Bind
{
    /// <summary>Creates a binding where all platforms get these keys.</summary>
    public static PlatformKeyBinding All (params Key [] keys) => new () { All = keys };

    /// <summary>
    ///     Creates a binding where all platforms get the base key, with additional keys for specific platforms.
    /// </summary>
    public static PlatformKeyBinding AllPlus (Key key, Key []? nonWindows = null, Key []? windows = null, Key []? linux = null, Key []? macos = null) =>
        new ()
        {
            All = [key],
            Windows = windows,
            Linux = nonWindows is { } && linux is null ? nonWindows : linux,
            Macos = nonWindows is { } && macos is null ? nonWindows : macos
        };

    /// <summary>Creates a binding where only Linux and macOS get these keys.</summary>
    public static PlatformKeyBinding NonWindows (params Key [] keys) => new () { Linux = keys, Macos = keys };

    /// <summary>Creates a binding with platform-specific keys only (no "all" entry).</summary>
    public static PlatformKeyBinding Platform (Key []? windows = null, Key []? linux = null, Key []? macos = null) =>
        new () { Windows = windows, Linux = linux, Macos = macos };
}
