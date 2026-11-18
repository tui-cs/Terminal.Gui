namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents an exception that is thrown when a layout operation fails.
/// </summary>
[Serializable]
public class LayoutException : Exception
{
    /// <summary>
    ///     Creates a new instance of <see cref="LayoutException"/>.
    /// </summary>
    public LayoutException () { }

    /// <summary>
    ///     Creates a new instance of <see cref="LayoutException"/>.
    /// </summary>
    /// <param name="message"></param>
    public LayoutException (string? message) : base (message) { }

    /// <summary>
    ///     Creates a new instance of <see cref="LayoutException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public LayoutException (string? message, Exception? innerException) : base (message, innerException) { }
}
