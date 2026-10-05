using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     The low-level interface drivers implement to provide output capabilities; encapsulates platform-specific
///     output functionality.
/// </summary>
public interface IOutput : IDisposable
{
    /// <seealso cref="IDriver.Force16Colors"/>
    bool Force16Colors { get; set; }

    /// <seealso cref="IDriver.IsLegacyConsole"/>
    bool IsLegacyConsole { get; set; }

    /// <summary>
    ///     Gets or sets whether raster images should be emitted using the Kitty graphics protocol
    ///     instead of Sixel. Set to <see langword="true"/> when the terminal reports Kitty support.
    /// </summary>
    bool UseKittyGraphics { get; set; }

    /// <seealso cref="IDriver.GetSixels"/>
    ConcurrentQueue<SixelToRender> GetSixels ();

    /// <summary>
    ///     Returns the current size of the console in rows/columns (i.e.
    ///     of characters not pixels).
    /// </summary>
    /// <returns></returns>
    Size GetSize ();

    /// <summary>
    ///     Sets the size of the console.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    void SetSize (int width, int height);

    /// <summary>
    ///     Sets the cursor for this output.
    /// </summary>
    /// <param name="cursor">
    ///     The cursor to set. Position must be in screen-absolute coordinates.
    ///     Use <c>ContentToScreen()</c> or <c>ViewportToScreen()</c> to convert from view-relative coordinates.
    ///     Set Position to null to hide the cursor.
    /// </param>
    public void SetCursor (Cursor cursor);

    /// <summary>
    ///     Gets the current cursor for this output.
    /// </summary>
    /// <returns></returns>
    public Cursor GetCursor ();

    /// <summary>
    ///     Writes the given text directly to the console. Use to send
    ///     ansi escape codes etc. Regular screen output should use the
    ///     <see cref="IOutputBuffer"/> overload.
    /// </summary>
    /// <param name="text"></param>
    void Write (ReadOnlySpan<char> text);

    /// <summary>
    ///     Suspend the application / terminal (e.g. SIGTSTP on Unix) and perform any
    ///     driver-specific state save/restore required across the suspend/resume cycle.
    ///     Implementations on platforms that do not support suspension may be a no-op.
    /// </summary>
    void Suspend ();

    /// <summary>
    ///     Write the contents of the <paramref name="buffer"/> to the console
    /// </summary>
    /// <param name="buffer"></param>
    void Write (IOutputBuffer buffer);

    /// <summary>
    ///     Gets a string containing the ANSI escape sequences and content most recently written
    ///     to the terminal via <see cref="Write(IOutputBuffer)"/>. Terminal-attached
    ///     <see cref="OutputBase"/> implementations capture only after
    ///     <see cref="OutputBase.CaptureOutput"/> is enabled; headless outputs capture by default.
    /// </summary>
    string GetLastOutput ();

    /// <summary>
    ///     Generates an ANSI escape sequence string representation of the given <paramref name="buffer"/> contents.
    ///     This is the same output that would be written to the terminal to recreate the current screen contents.
    /// </summary>
    /// <param name="buffer">The output buffer to convert to ANSI.</param>
    /// <returns>A string containing ANSI escape sequences representing the buffer contents.</returns>
    string ToAnsi (IOutputBuffer buffer);
}
