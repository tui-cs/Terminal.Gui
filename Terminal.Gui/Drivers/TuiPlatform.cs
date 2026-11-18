namespace Terminal.Gui.Drivers;

/// <summary>Identifies the operating system for platform-specific key binding resolution.</summary>
public enum TuiPlatform
{
    /// <summary>Microsoft Windows.</summary>
    Windows,

    /// <summary>Linux.</summary>
    Linux,

    /// <summary>macOS (Darwin).</summary>
    Macos
}
