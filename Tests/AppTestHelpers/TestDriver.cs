namespace AppTestHelpers;

/// <summary>
///     Which driver simulation should be used for testing
/// </summary>
public enum TestDriver
{
    /// <summary>
    ///     The Windows driver with simulation I/O but core driver classes
    /// </summary>
    Windows,

    /// <summary>
    ///     The DotNet driver with simulation I/O but core driver classes
    /// </summary>
    DotNet,

    /// <summary>
    ///     The Unix driver with simulation I/O but core driver classes
    /// </summary>
    Unix,

    /// <summary>
    ///     The ANSI driver with simulation I/O but core driver classes
    /// </summary>
    ANSI
}
