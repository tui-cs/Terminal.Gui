using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>Base interface for Terminal.Gui Driver implementations.</summary>
public interface IDriver : IDisposable
{
    #region Driver Lifecycle

    /// <summary>Initializes the driver</summary>
    void Init ();

    /// <summary>
    ///     INTERNAL: Updates the terminal with the current output buffer. Should not be used by applications. Drawing occurs
    ///     once each Application main loop iteration.
    /// </summary>
    void Refresh ();

    /// <summary>
    ///     Gets the name of the driver implementation.
    /// </summary>
    string? GetName ();

    /// <summary>Suspends the application (e.g. on Linux via SIGTSTP) and upon resume, resets the console driver.</summary>
    /// <remarks>This is only implemented in UnixDriver.</remarks>
    void Suspend ();

    /// <summary>
    ///     Gets whether the driver has detected the console requires legacy console API (Windows Console API without ANSI/VT
    ///     support).
    ///     Returns <see langword="true"/> for legacy consoles that don't support modern ANSI escape sequences (e.g. Windows
    ///     conhost);
    ///     <see langword="false"/> for modern terminals with ANSI/VT support.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property indicates whether the terminal supports modern ANSI escape sequences for input/output.
    ///         On Windows, this maps to whether Virtual Terminal processing is enabled.
    ///         On Unix-like systems, this is typically <see langword="false"/> as they support ANSI by default.
    ///     </para>
    ///     <para>
    ///         When <see langword="true"/>, the driver must use legacy Windows Console API functions
    ///         (e.g., WriteConsoleW, SetConsoleTextAttribute) instead of ANSI escape sequences.
    ///     </para>
    /// </remarks>
    bool IsLegacyConsole { get; internal set; }

    #endregion Driver Lifecycle

    #region Driver Components

    /// <summary>
    ///     Class responsible for processing native driver input objects
    ///     e.g. <see cref="ConsoleKeyInfo"/> into <see cref="Key"/> events
    ///     and detecting and processing ansi escape sequences.
    /// </summary>
    IInputProcessor GetInputProcessor ();

    /// <summary>
    ///     Gets the <see cref="IOutputBuffer"/> containing the buffered screen contents.
    /// </summary>
    /// <returns></returns>
    IOutputBuffer GetOutputBuffer ();

    /// <summary>
    ///     Gets the <see cref="IOutput"/> responsible for writing to the terminal.
    /// </summary>
    IOutput GetOutput ();

    /// <summary>Gets or sets the clipboard.</summary>
    IClipboard? Clipboard { get; set; }

    /// <summary>
    ///     Gets the terminal progress indicator when direct terminal progress output is available.
    /// </summary>
    ProgressIndicator? ProgressIndicator { get; }

    /// <summary>
    /// Gets the ANSI startup readiness gate, if enabled for this driver instance.
    /// </summary>
    IAnsiStartupGate? AnsiStartupGate { get; }

    #endregion Driver Components

    #region Screen and Display

    /// <summary>Gets the location and size of the terminal screen.</summary>
    Rectangle Screen { get; }

    /// <summary>
    ///     Sets the screen size. <see cref="Screen"/> is the source of truth for screen dimensions.
    /// </summary>
    /// <param name="width">The new width in columns.</param>
    /// <param name="height">The new height in rows.</param>
    void SetScreenSize (int width, int height);

    /// <summary>
    ///     The event fired when the screen changes (size, position, etc.).
    ///     <see cref="Screen"/> is the source of truth for screen dimensions.
    /// </summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <summary>The number of columns visible in the terminal.</summary>
    int Cols { get; set; }

    /// <summary>The number of rows visible in the terminal.</summary>
    int Rows { get; set; }

    /// <summary>The leftmost column in the terminal.</summary>
    int Left { get; set; }

    /// <summary>The topmost row in the terminal.</summary>
    int Top { get; set; }

    /// <summary>
    ///     Gets or sets the inline-mode cursor position. Only meaningful when <see cref="AppModel"/>
    ///     is <see cref="AppModel.Inline"/>. The <c>Y</c> component is the terminal row where
    ///     the inline region starts; <c>X</c> is reserved for future use.
    /// </summary>
    Point InlinePosition { get; set; }

    /// <summary>
    ///     Gets or sets how the application using this driver interacts with the terminal buffer.
    /// </summary>
    AppModel AppModel { get; set; }

    #endregion Screen and Display

    #region Color Support

    /// <summary>
    ///     Gets the terminal's sixel support capabilities, detected during driver initialization.
    /// </summary>
    /// <remarks>
    ///     <see langword="null"/> if detection has not been performed.
    /// </remarks>
    SixelSupportResult? SixelSupport { get; }

    /// <summary>
    ///     Raised when <see cref="SixelSupport"/> changes (e.g. after terminal color detection completes).
    /// </summary>
    event EventHandler<ValueChangedEventArgs<SixelSupportResult?>>? SixelSupportChanged;

    /// <summary>
    ///     Sets the terminal's <see cref="SixelSupport"/> result and raises <see cref="SixelSupportChanged"/>.
    /// </summary>
    /// <remarks>
    ///     Normally populated automatically during driver initialization. Call this to override detection — for
    ///     example, to force Sixel output on a headless or PTY driver that skips the terminal handshake.
    /// </remarks>
    /// <param name="result">The sixel support capabilities to apply.</param>
    void SetSixelSupport (SixelSupportResult result);

    /// <summary>
    ///     Gets the terminal's Kitty graphics protocol support capabilities, detected during driver initialization.
    /// </summary>
    /// <remarks>
    ///     <see langword="null"/> if detection has not been performed.
    /// </remarks>
    KittyGraphicsSupportResult? KittyGraphicsSupport { get; }

    /// <summary>
    ///     Raised when <see cref="KittyGraphicsSupport"/> changes (e.g. after terminal detection completes).
    /// </summary>
    event EventHandler<ValueChangedEventArgs<KittyGraphicsSupportResult?>>? KittyGraphicsSupportChanged;

    /// <summary>
    ///     Sets the terminal's <see cref="KittyGraphicsSupport"/> result and raises
    ///     <see cref="KittyGraphicsSupportChanged"/>.
    /// </summary>
    /// <remarks>
    ///     Normally populated automatically during driver initialization. Call this to override detection — for
    ///     example, to force Kitty graphics output on a headless or PTY driver that skips the terminal handshake.
    /// </remarks>
    /// <param name="result">The Kitty graphics protocol support capabilities to apply.</param>
    void SetKittyGraphicsSupport (KittyGraphicsSupportResult result);

    /// <summary>Gets whether the <see cref="IDriver"/> supports TrueColor output.</summary>
    bool SupportsTrueColor { get; }

    /// <summary>
    ///     Gets or sets whether the <see cref="IDriver"/> should use 16 colors instead of the default TrueColors.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Will be forced to <see langword="true"/> if <see cref="IDriver.SupportsTrueColor"/> is
    ///         <see langword="false"/>, indicating that the <see cref="IDriver"/> cannot support TrueColor.
    ///     </para>
    /// </remarks>
    /// <seealso cref="Driver.Force16Colors"/>
    bool Force16Colors { get; set; }

    /// <summary>
    ///     Gets the terminal's actual default foreground and background colors,
    ///     queried via OSC 10/11 at driver startup.
    ///     <see langword="null"/> if the terminal did not respond.
    /// </summary>
    Attribute? DefaultAttribute { get; }

    /// <summary>
    ///     Raised when <see cref="DefaultAttribute"/> changes (e.g. after terminal color detection completes).
    /// </summary>
    event EventHandler<ValueChangedEventArgs<Attribute?>>? DefaultAttributeChanged;

    /// <summary>
    ///     Gets the terminal's color capabilities as detected from environment variables.
    ///     <see langword="null"/> if detection has not been performed.
    /// </summary>
    TerminalColorCapabilities? ColorCapabilities { get; }

    #endregion Color Support

    #region Content Buffer

    // BUGBUG: This should not be publicly settable.
    /// <summary>
    ///     Gets or sets the contents of the application output. The driver outputs this buffer to the terminal.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    Cell [,]? Contents { get; set; }

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    Region? Clip { get; set; }

    /// <summary>Clears the <see cref="IDriver.Contents"/> of the driver.</summary>
    void ClearContents ();

    /// <summary>
    ///     Fills the specified rectangle with the specified rune, using <see cref="IDriver.CurrentAttribute"/>
    /// </summary>
    event EventHandler<EventArgs> ClearedContents;

    #endregion Content Buffer

    #region Drawing and Rendering

    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    int Col { get; }

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    int Row { get; }

    /// <summary>
    ///     The <see cref="System.Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or
    ///     <see cref="AddStr"/>
    ///     call.
    /// </summary>
    Attribute CurrentAttribute { get; set; }

    /// <summary>
    ///     Gets or sets the URL that will be associated with cells added via <see cref="AddRune(Rune)"/> or
    ///     <see cref="AddStr(string)"/>.
    ///     When set, subsequent cells will include this URL for OSC 8 hyperlink rendering.
    ///     Set to <see langword="null"/> to stop associating URLs with cells.
    /// </summary>
    string? CurrentUrl { get; set; }

    /// <summary>
    ///     Updates <see cref="IDriver.Col"/> and <see cref="IDriver.Row"/> to the specified column and row in
    ///     <see cref="IDriver.Contents"/>.
    ///     Used by <see cref="IDriver.AddRune(System.Text.Rune)"/> and <see cref="IDriver.AddStr"/> to determine
    ///     where to add content.
    /// </summary>
    /// <remarks>
    ///     <para>This does not move the cursor on the screen, it only updates the internal state of the driver.</para>
    ///     <para>
    ///         If <paramref name="col"/> or <paramref name="row"/> are negative or beyond  <see cref="IDriver.Cols"/>
    ///         and
    ///         <see cref="IDriver.Rows"/>, the method still sets those properties.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column to move to.</param>
    /// <param name="row">Row to move to.</param>
    void Move (int col, int row);

    /// <summary>Tests if the specified rune is supported by the driver.</summary>
    /// <param name="rune"></param>
    /// <returns>
    ///     <see langword="true"/> if the rune can be properly presented; <see langword="false"/> if the driver does not
    ///     support displaying this rune.
    /// </returns>
    bool IsRuneSupported (Rune rune);

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Text.</summary>
    /// <param name="text">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of
    ///     <see cref="IDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    bool IsValidLocation (string text, int col, int row);

    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="IDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="rune"/> required, even if the new column value is outside the
    ///         <see cref="IDriver.Clip"/> or screen
    ///         dimensions defined by <see cref="IDriver.Cols"/>.
    ///     </para>
    ///     <para>
    ///         If <paramref name="rune"/> requires more than one column, and <see cref="IDriver.Col"/> plus the number
    ///         of columns
    ///         needed exceeds the <see cref="IDriver.Clip"/> or screen dimensions, the default Unicode replacement
    ///         character (U+FFFD)
    ///         will be added instead.
    ///     </para>
    /// </remarks>
    /// <param name="rune">Rune to add.</param>
    void AddRune (Rune rune);

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="IDriver.AddRune(System.Text.Rune)"/> with the <see cref="Rune"/>
    ///     constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    void AddRune (char c);

    /// <summary>Adds the <paramref name="str"/> to the display at the cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="IDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="str"/> required, unless the new column value is outside the <see cref="IDriver.Clip"/>
    ///         or screen
    ///         dimensions defined by <see cref="IDriver.Cols"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    void AddStr (string str);

    /// <summary>Fills the specified rectangle with the specified rune, using <see cref="IDriver.CurrentAttribute"/></summary>
    /// <remarks>
    ///     The value of <see cref="IDriver.Clip"/> is honored. Any parts of the rectangle not in the clip will not be
    ///     drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    void FillRect (Rectangle rect, Rune rune = default);

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks>Implementations should call <c>base.SetAttribute(c)</c>.</remarks>
    /// <param name="c">C.</param>
    Attribute SetAttribute (Attribute c);

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    Attribute GetAttribute ();

    /// <summary>
    ///     Provide proper writing to send escape sequence recognized by the <see cref="IDriver"/>.
    /// </summary>
    /// <param name="ansi"></param>
    void WriteRaw (string ansi);

    /// <summary>
    ///     Sets the terminal title using OSC 0..2.
    /// </summary>
    /// <param name="title">The title text.</param>
    /// <param name="mode">
    ///     The OSC title selector:
    ///     0 = icon and window title, 1 = icon title, 2 = window title.
    /// </param>
    void SetTerminalTitle (string title, int mode = 0);

    /// <summary>
    ///     Gets the queue of sixel images to write out to screen when updating.
    ///     If the terminal does not support Sixel, adding to this queue has no effect.
    /// </summary>
    ConcurrentQueue<SixelToRender> GetSixels ();

    /// <summary>
    ///     Gets a full-grid textual representation of <see cref="Contents"/>.
    /// </summary>
    /// <remarks>
    ///     This returns the complete current screen grid and is suitable for deterministic snapshot/golden comparisons.
    /// </remarks>
    /// <returns></returns>
    public string ToString ();

    /// <summary>
    ///     Gets an ANSI escape sequence representation of <see cref="Contents"/> for terminal output.
    /// </summary>
    /// <remarks>
    ///     This output is intended for terminal replay and may include incremental escape-sequence behavior.
    ///     Prefer <see cref="ToString"/> as the canonical source for snapshot/golden assertions.
    /// </remarks>
    /// <returns></returns>
    public string ToAnsi ();

    #endregion Drawing and Rendering

    #region Cursor

    /// <summary>
    ///     Sets the cursor for this driver.
    /// </summary>
    /// <param name="cursor">
    ///     The cursor to set. Position must be in screen-absolute coordinates.
    ///     Use <c>ContentToScreen()</c> or <c>ViewportToScreen()</c> to convert from view-relative coordinates.
    ///     Set Position to null to hide the cursor.
    /// </param>
    public void SetCursor (Cursor cursor);

    /// <summary>
    ///     Gets the current cursor for this driver.
    /// </summary>
    /// <returns></returns>
    public Cursor GetCursor ();

    /// <summary>
    ///     Gets whether the terminal cursor needs to be updated.
    /// </summary>
    /// <returns></returns>
    bool GetCursorNeedsUpdate ();

    /// <summary>
    ///     Signals that the cursor needs to be updated without requiring a full redraw.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is called by <see cref="View.SetCursorNeedsUpdate"/> when a view's cursor position
    ///         or shape changes but the view content does not need to be redrawn.
    ///     </para>
    /// </remarks>
    /// <param name="needsUpdate">Indicates whether the cursor needs to be updated.</param>
    public void SetCursorNeedsUpdate (bool needsUpdate);

    #endregion Cursor

    #region Input Events

    /// <summary>
    ///     Gets the terminal kitty keyboard protocol capabilities detected at startup.
    ///     <see langword="null"/> if the terminal was not queried, detection has not completed, or the terminal did not
    ///     respond and kitty keyboard protocol support could not be confirmed.
    ///     When non-<see langword="null"/>, use <see cref="KittyKeyboardCapabilities.IsSupported"/> to determine whether the
    ///     terminal supports the protocol.
    /// </summary>
    KittyKeyboardCapabilities? KittyKeyboardCapabilities { get; }

    /// <summary>Event fired when a key is pressed down.</summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Event fired when a key is released. Only raised when the driver provides key release information.
    ///     Not all drivers support key-up events.
    /// </summary>
    event EventHandler<Key>? KeyUp;

    /// <summary>Event fired when a mouse event occurs.</summary>
    event EventHandler<Mouse>? MouseEvent;

    /// <summary>
    ///     Event fired when a bracketed paste is received from the terminal. The string contains the
    ///     raw pasted text with the ANSI bracketing markers stripped. Only fires on terminals that
    ///     support bracketed paste mode (most modern terminals).
    /// </summary>
    event EventHandler<string>? Paste;

    #endregion Input Events

    #region ANSI Escape Sequences

    /// <summary>
    ///     Queues the given <paramref name="request"/> for execution
    /// </summary>
    /// <param name="request"></param>
    public void QueueAnsiRequest (AnsiEscapeSequenceRequest request);

    #endregion ANSI Escape Sequences
}