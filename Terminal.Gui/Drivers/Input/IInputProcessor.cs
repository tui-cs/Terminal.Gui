namespace Terminal.Gui.Drivers;

/// <summary>
///     Processes queued input on the main loop thread, translating driver-specific input
///     into Terminal.Gui events and data models.
/// </summary>
public interface IInputProcessor
{
    #region Configuration and Core Processing

    /// <summary>
    ///     Drains the input queue, processing all available input. Must be called on the main loop thread.
    /// </summary>
    void ProcessQueue ();

    /// <summary>
    ///     Gets the ANSI response parser for handling escape sequences.
    /// </summary>
    /// <returns>The configured ANSI response parser instance.</returns>
    IAnsiResponseParser GetParser ();

    /// <summary>
    ///     Validates and processes Unicode surrogate pairs in the input stream.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <param name="result">The validated key or completed surrogate pair.</param>
    /// <returns><see langword="true"/> if the result is valid; <see langword="false"/> if more input is needed.</returns>
    bool IsValidInput (Key key, out Key result);

    #endregion

    #region Keyboard Events

    /// <summary>
    ///     Raises the <see cref="KeyDown"/> event after a key down event is dequeued.
    /// </summary>
    /// <param name="key">The key event data.</param>
    void RaiseKeyDownEvent (Key key);

    /// <summary>
    ///     Event raised when a key down event is dequeued.
    /// </summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Injects a key down event. For unit tests.
    /// </summary>
    /// <param name="key">The key to enqueue.</param>
    void InjectKeyDownEvent (Key key);

    /// <summary>
    ///     Raises the <see cref="KeyUp"/> event after a key release event is dequeued.
    /// </summary>
    /// <param name="key">The key event data.</param>
    void RaiseKeyUpEvent (Key key);

    /// <summary>
    ///     Event raised when a key is released.
    /// </summary>
    /// <remarks>
    ///     This event is only raised when the driver provides key release information.
    ///     Not all drivers support key-up events.
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    #endregion

    #region Mouse Events

    /// <summary>
    ///     Raises the <see cref="MouseEventParsed"/> event after a mouse event is parsed from the driver.
    /// </summary>
    /// <param name="mouse">The parsed mouse event data.</param>
    void RaiseMouseEventParsed (Mouse mouse);

    /// <summary>
    ///     Event raised when a mouse event is parsed from the driver. For debugging and unit tests.
    /// </summary>
    event EventHandler<Mouse>? MouseEventParsed;

    /// <summary>
    ///     Event raised when synthetic mouse events (clicks, double-clicks, triple-clicks) are generated.
    /// </summary>
    event EventHandler<Mouse>? SyntheticMouseEvent;

    /// <summary>
    ///     Enqueues a mouse event. For unit tests.
    /// </summary>
    /// <param name="app">
    ///     Application instance for cross-thread marshalling. When called from non-main thread,
    ///     uses <see cref="IApplication.Invoke(Action)"/> to raise events on the main thread.
    /// </param>
    /// <param name="mouse">The mouse event to enqueue.</param>
    void InjectMouseEvent (IApplication? app, Mouse mouse);

    #endregion

    #region ANSI Sequence Handling

    /// <summary>
    ///     Event raised when an unrecognized ANSI escape sequence is ignored.
    /// </summary>
    event EventHandler<string>? AnsiSequenceSwallowed;

    #endregion

    #region Paste Events

    /// <summary>
    ///     Event raised when bracketed paste content is delivered. The string contains the raw pasted
    ///     text with the ANSI bracketing markers stripped.
    /// </summary>
    /// <remarks>
    ///     Bracketed paste mode must be enabled by the driver (typically by writing
    ///     <see cref="EscSeqUtils.CSI_EnableBracketedPaste"/> at startup). On terminals that do not
    ///     support bracketed paste, pasted text is delivered as ordinary key events instead.
    /// </remarks>
    event EventHandler<string>? Paste;

    /// <summary>
    ///     Raises the <see cref="Paste"/> event. For unit tests and driver implementations.
    /// </summary>
    /// <param name="text">The pasted content with bracketing markers stripped.</param>
    void RaisePasteEvent (string text);

    #endregion
}
