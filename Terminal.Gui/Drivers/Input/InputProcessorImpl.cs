using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Base implementation for processing queued input of type <typeparamref name="TInputRecord"/>.
///     Translates driver-specific input into Terminal.Gui events and data models on the main loop thread.
/// </summary>
/// <typeparam name="TInputRecord">The driver-specific input record type (e.g., <see cref="ConsoleKeyInfo"/>).</typeparam>
public abstract class InputProcessorImpl<TInputRecord> : IInputProcessor, IDisposable where TInputRecord : struct
{
    #region Fields and Configuration

    /// <summary>
    ///     Timeout for detecting stale escape sequences. After this duration, held Esc sequences are released.
    /// </summary>
    private readonly TimeSpan _escTimeout = TimeSpan.FromMilliseconds (50);

    /// <summary>
    ///     Timeout for detecting a stranded bracketed-paste session. If the terminal opens a paste with
    ///     <c>ESC[200~</c> but never delivers the matching <c>ESC[201~</c> end marker (dropped
    ///     connection, broken terminal), the buffered content is flushed after this duration so input
    ///     resumes flowing. Pastes legitimately span seconds for large clipboards, so this is much
    ///     longer than <see cref="_escTimeout"/>.
    /// </summary>
    private readonly TimeSpan _bracketedPasteTimeout = TimeSpan.FromSeconds (5);

    /// <summary>
    ///     ANSI response parser for handling escape sequences from the input stream.
    /// </summary>
    internal AnsiResponseParser<TInputRecord> Parser { get; }

    private readonly ITimeProvider _timeProvider;
    private readonly MouseInterpreter _mouseInterpreter;

    /// <summary>
    ///     Thread-safe input queue populated by <see cref="IInput{TInputRecord}"/> on the input thread.
    ///     Dequeued by <see cref="ProcessQueue"/> on the main loop thread.
    /// </summary>
    public ConcurrentQueue<TInputRecord> InputQueue { get; }

    /// <summary>
    ///     Input implementation instance. Set by <see cref="IMainLoopCoordinator"/>.
    /// </summary>
    public IInput<TInputRecord>? InputImpl { get; set; }

    /// <summary>
    ///     External cancellation token source for cooperative cancellation.
    /// </summary>
    public CancellationTokenSource? ExternalCancellationTokenSource { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    ///     Initializes a new instance, wiring parser events and configuring the input queue.
    /// </summary>
    /// <param name="inputBuffer">Thread-safe queue to be populated with input by <see cref="IInput{TInputRecord}"/>.</param>
    /// <param name="keyConverter">Converter for translating <typeparamref name="TInputRecord"/> to <see cref="Key"/>.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control. Defaults to SystemTimeProvider if null.</param>
    protected InputProcessorImpl (ConcurrentQueue<TInputRecord> inputBuffer, IKeyConverter<TInputRecord> keyConverter, ITimeProvider? timeProvider = null)
    {
        ITimeProvider tp = timeProvider ?? new SystemTimeProvider ();
        _timeProvider = tp;
        Parser = new AnsiResponseParser<TInputRecord> (tp);
        _mouseInterpreter = new MouseInterpreter (tp);

        InputQueue = inputBuffer;
        KeyConverter = keyConverter;

        // Enable mouse handling
        Parser.HandleMouse = true;
        Parser.Mouse += (_, mouse) => RaiseMouseEventParsed (mouse);

        // Enable keyboard handling
        Parser.HandleKeyboard = true;

        Parser.Paste += (_, text) => RaisePasteEvent (text);

        Parser.Keyboard += (_, keyEvent) =>
                           {
                               Key normalizedKeyEvent = OnKeyboardEventParsed (keyEvent);

                               if (normalizedKeyEvent.EventType == KeyEventType.Release)
                               {
                                   RaiseKeyUpEvent (normalizedKeyEvent);
                               }
                               else
                               {
                                   RaiseKeyDownEvent (normalizedKeyEvent);
                               }
                           };

        // Configure unexpected response handler
        Parser.UnexpectedResponseHandler = str =>
                                           {
                                               var cur = new string (str.Select (k => k.Item1).ToArray ());

                                               // Check if this is an incomplete mouse sequence (timing issue when Run() blocks)

                                               Logging.Warning ($"{nameof (InputProcessorImpl<TInputRecord>)} ignored unrecognized response '{cur}'. See https://github.com/tui-cs/Terminal.Gui/issues/4587");

                                               AnsiSequenceSwallowed?.Invoke (this, cur);

                                               return true;
                                           };
    }

    #endregion

    #region Core Processing

    /// <inheritdoc/>
    public IAnsiResponseParser GetParser () => Parser;

    /// <inheritdoc/>
    public void ProcessQueue ()
    {
        while (InputQueue.TryDequeue (out TInputRecord input))
        {
            Process (input);
        }

        foreach (TInputRecord input in ReleaseParserHeldKeysIfStale ())
        {
            ProcessAfterParsing (input);
        }

        FlushStaleBracketedPasteIfNeeded ();

        // Check for expired deferred clicks
        CheckForExpiredMouseClicks ();
    }

    private void FlushStaleBracketedPasteIfNeeded ()
    {
        if (Parser.State is not AnsiResponseParserState.InBracketedPaste
            and not AnsiResponseParserState.DiscardingBracketedPasteRemainder)
        {
            return;
        }

        if (_timeProvider.Now - Parser.LastBracketedPasteInputAt < _bracketedPasteTimeout)
        {
            return;
        }

        Logging.Warning (
                         $"{nameof (InputProcessorImpl<TInputRecord>)} abandoning stale bracketed-paste state after {_bracketedPasteTimeout.TotalSeconds}s without activity");

        Parser.FlushStaleBracketedPaste ();
    }

    /// <summary>
    ///     Checks for and emits any deferred single-click events that have exceeded the double-click threshold.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method polls the <see cref="_mouseInterpreter"/> for expired pending clicks that were deferred
    ///         to allow double-click detection. Expired clicks are emitted through the normal mouse event pipeline.
    ///     </para>
    /// </remarks>
    private void CheckForExpiredMouseClicks ()
    {
        foreach (Mouse expiredClick in _mouseInterpreter.CheckForExpiredClicks ())
        {
            //Logging.Trace ($"Emitting expired click: {expiredClick}");
            SyntheticMouseEvent?.Invoke (this, expiredClick);
        }
    }

    /// <summary>
    ///     Releases held escape sequences that have exceeded the timeout threshold.
    /// </summary>
    /// <returns>Enumerable of input records that were held and are now released.</returns>
    private IEnumerable<TInputRecord> ReleaseParserHeldKeysIfStale ()
    {
        if (Parser.State is AnsiResponseParserState.ExpectingEscapeSequence or AnsiResponseParserState.InResponse
            && _timeProvider.Now - Parser.StateChangedAt > _escTimeout)
        {
            return Parser.Release ().Select (o => o.Item2);
        }

        return [];
    }

    /// <summary>
    ///     Processes a single input element dequeued from <see cref="InputQueue"/>.
    ///     Called sequentially for each dequeued value.
    /// </summary>
    /// <param name="input">The input record to process.</param>
    protected abstract void Process (TInputRecord input);

    /// <summary>
    ///     Processes input that bypasses the <see cref="Parser"/> (e.g., stale escape sequences).
    /// </summary>
    /// <param name="input">The input record to process.</param>
    protected virtual void ProcessAfterParsing (TInputRecord input)
    {
        Key key = KeyConverter.ToKey (input);

        // If the key is not valid, we don't want to raise any events.
        if (!IsValidInput (key, out key))
        {
            return;
        }

        if (ShouldSuppressFallbackKeyDown (key))
        {
            return;
        }

        RaiseKeyDownEvent (key);
    }

    /// <summary>
    ///     Called when the ANSI parser raises a keyboard event before it is forwarded to <see cref="KeyDown"/> or
    ///     <see cref="KeyUp"/> subscribers.
    /// </summary>
    /// <param name="keyEvent">The parsed key event.</param>
    /// <returns>The key event to forward.</returns>
    protected virtual Key OnKeyboardEventParsed (Key keyEvent) => keyEvent;

    /// <summary>
    ///     Gives derived processors a chance to suppress keydown events that come from fallback stream processing
    ///     after ANSI parsing.
    /// </summary>
    /// <param name="key">The key produced by fallback processing.</param>
    /// <returns><see langword="true"/> when the keydown should be ignored; otherwise <see langword="false"/>.</returns>
    protected virtual bool ShouldSuppressFallbackKeyDown (Key key) => false;

    private char _highSurrogate = '\0';

    /// <inheritdoc/>
    public bool IsValidInput (Key key, out Key result)
    {
        result = key;

        if (char.IsHighSurrogate ((char)key))
        {
            _highSurrogate = (char)key;

            return false;
        }

        if (_highSurrogate > 0 && char.IsLowSurrogate ((char)key))
        {
            result = (KeyCode)new Rune (_highSurrogate, (char)key).Value;

            if (key.IsAlt)
            {
                result = result.WithAlt;
            }

            if (key.IsCtrl)
            {
                result = result.WithCtrl;
            }

            if (key.IsShift)
            {
                result = result.WithShift;
            }

            _highSurrogate = '\0';

            return true;
        }

        if (char.IsSurrogate ((char)key))
        {
            return false;
        }

        if (_highSurrogate > 0)
        {
            _highSurrogate = '\0';
        }

        if (key.KeyCode == 0)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Keyboard Events

    /// <summary>
    ///     Translates driver-specific <typeparamref name="TInputRecord"/> to Terminal.Gui <see cref="Key"/>.
    /// </summary>
    public IKeyConverter<TInputRecord> KeyConverter { get; }

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc/>
    public void RaiseKeyDownEvent (Key a) => KeyDown?.Invoke (this, a);

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc/>
    public void RaiseKeyUpEvent (Key a) => KeyUp?.Invoke (this, a);

    /// <inheritdoc/>
    public virtual void InjectKeyDownEvent (Key key)
    {
        // Convert Key → TInputRecord
        TInputRecord inputRecord = KeyConverter.ToKeyInfo (key);

        // If input supports testing, use InputImplPeek/Read pipeline
        // which runs on the input thread.
        if (InputImpl is ITestableInput<TInputRecord> testableInput)
        {
            testableInput.InjectInput (inputRecord);
        }
    }

    #endregion

    #region Mouse Events

    /// <inheritdoc/>
    public event EventHandler<Mouse>? MouseEventParsed;

    /// <inheritdoc/>
    public void RaiseMouseEventParsed (Mouse mouse)
    {
        //Logging.Trace ($"{mouse}");
        MouseEventParsed?.Invoke (this, mouse);
        RaiseSyntheticMouseEvent (mouse);
    }

    /// <inheritdoc/>
    public event EventHandler<Mouse>? SyntheticMouseEvent;

    /// <summary>
    /// </summary>
    /// <param name="mouse"></param>
    public void RaiseSyntheticMouseEvent (Mouse mouse)
    {
        //Logging.Trace ($"{mouse}");

        // Process through MouseInterpreter to generate clicks
        // The interpreter yields the original event first, then any synthetic click events
        foreach (Mouse e in _mouseInterpreter.Process (mouse))
        {
            // Raise all events: original + synthetic clicks
            //Logging.Trace ($"Invoking SyntheticMouseEvent: {e}");
            SyntheticMouseEvent?.Invoke (this, e);
        }
    }

    /// <inheritdoc/>
    public virtual void InjectMouseEvent (IApplication? app, Mouse mouse) => mouse.Timestamp ??= DateTime.Now;

    #endregion

    #region ANSI Sequence Handling

    /// <inheritdoc/>
    public event EventHandler<string>? AnsiSequenceSwallowed;

    #endregion

    #region Paste Events

    /// <inheritdoc/>
    public event EventHandler<string>? Paste;

    /// <inheritdoc/>
    public void RaisePasteEvent (string text) => Paste?.Invoke (this, text);

    #endregion

    #region Disposal

    /// <inheritdoc/>
    public void Dispose () => ExternalCancellationTokenSource?.Dispose ();

    #endregion
}
