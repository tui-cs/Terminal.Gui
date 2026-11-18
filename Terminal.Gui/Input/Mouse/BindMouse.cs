namespace Terminal.Gui.Input;

/// <summary>
///     Provides ergonomic factory methods for creating <see cref="PlatformMouseBinding"/> instances.
/// </summary>
public static class BindMouse
{
    /// <summary>Creates a binding where all platforms get these mouse flags.</summary>
    public static PlatformMouseBinding All (params MouseFlags [] mouseFlags) => new () { All = mouseFlags };

    /// <summary>Creates a binding with platform-specific mouse flags only (no "all" entry).</summary>
    public static PlatformMouseBinding Platform (MouseFlags []? windows = null, MouseFlags []? linux = null, MouseFlags []? macos = null) =>
        new () { Windows = windows, Linux = linux, Macos = macos };
}
