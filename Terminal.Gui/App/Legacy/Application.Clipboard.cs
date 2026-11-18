namespace Terminal.Gui.App;

public static partial class Application // Clipboard handling
{
    /// <summary>
    ///     Gets the clipboard for the application.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides access to the OS clipboard through the driver.
    ///     </para>
    /// </remarks>
    [Obsolete ("The legacy static Application object is going away. Use IApplication.Clipboard instead.")]
    public static IClipboard? Clipboard => ApplicationImpl.Instance.Clipboard;
}
