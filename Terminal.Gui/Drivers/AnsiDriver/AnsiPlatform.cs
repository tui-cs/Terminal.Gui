namespace Terminal.Gui.Drivers;

/// <summary>
///     Indicates which platform the driver is targeting.
/// </summary>
public enum AnsiPlatform
{
    /// <summary>
    ///     Indicates the platform is disabled and all driver operations will be no-ops. Valid when running in test
    ///     environments such as CI/CD workflow runners where there is no console available.
    /// </summary>
    Degraded,

    /// <summary>
    ///     Indicates the platform is Windows and Virtual Terminal Sequences is enabled. The driver will use low-level kernel32
    ///     I/O apis.
    /// </summary>
    WindowsVT,

    /// <summary>
    ///     Represents the platform is Unix-like (MacOS, Linux, or FreeBSD) using raw Unix I/O apis.
    /// </summary>
    UnixRaw
}
