using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Input processor for <see cref="AnsiInput"/>, processes a <see cref="char"/> stream
///         using pure ANSI escape sequence handling.
///     </para>
///     <para>
///         ANSI Driver Architecture:
///     </para>
///     <para>
///         This processor integrates with Terminal.Gui's ANSI infrastructure:
///         <list type="bullet">
///             <item>
///                 <see cref="AnsiResponseParser"/> - Automatically parses ANSI escape sequences
///                 from the input stream, extracting keyboard events, mouse events, and terminal responses.
///             </item>
///             <item>
///                 <see cref="AnsiRequestScheduler"/> - Manages outgoing ANSI requests (via
///                 <see cref="IDriver.QueueAnsiRequest"/>)
///                 and matches responses from the parser.
///             </item>
///             <item>
///                 <see cref="AnsiKeyConverter"/> - Converts character input to <see cref="Key"/> events,
///                 shared with UnixDriver for consistent ANSI-based key mapping.
///             </item>
///             <item>
///                 <see cref="AnsiKeyboardEncoder"/> and <see cref="AnsiMouseEncoder"/> - Convert
///                 <see cref="Key"/> and <see cref="Mouse"/> events into ANSI sequences for test injection.
///             </item>
///         </list>
///     </para>
///     <para>
///         The parser is configured in the base class <see cref="InputProcessorImpl{TInputRecord}"/> with
///         <c>HandleMouse = true</c> and <c>HandleKeyboard = true</c>, enabling automatic event raising.
///     </para>
/// </summary>
public class AnsiInputProcessor : InputProcessorImpl<char>
{
    private long _inputPosition;
    private string _pendingKittyPrintableSuppression = string.Empty;
    private long _pendingKittyPrintableSuppressionStartPosition;

    /// <inheritdoc/>
    /// <param name="inputBuffer">The input buffer to process.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    public AnsiInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null) : base (inputBuffer,
                                                                                                              new AnsiKeyConverter (),
                                                                                                              timeProvider)
    { }

    /// <inheritdoc/>
    protected override void Process (char input)
    {
        _inputPosition++;
        InvalidatePendingKittyPrintableSuppression (input);

        foreach (Tuple<char, char> released in Parser.ProcessInput (Tuple.Create (input, input)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc/>
    private protected override Key OnKeyboardEventParsed (AnsiKeyboardParserPattern pattern, Key keyEvent)
    {
        keyEvent = base.OnKeyboardEventParsed (pattern, keyEvent);
        ClearPendingKittyPrintableSuppression ();

        if (pattern is not KittyKeyboardPattern
            || keyEvent.EventType != KeyEventType.Press
            || keyEvent.IsAlt
            || keyEvent.IsCtrl
            || keyEvent.IsModifierOnly)
        {
            return keyEvent;
        }

        string printableText = keyEvent.GetPrintableText ();

        if (!string.IsNullOrEmpty (printableText))
        {
            _pendingKittyPrintableSuppression = printableText;
            _pendingKittyPrintableSuppressionStartPosition = _inputPosition + 1;
        }

        return keyEvent;
    }

    /// <inheritdoc/>
    protected override bool ShouldSuppressFallbackKeyDown (Key key)
    {
        string printableText = key.GetPrintableText ();
        string pendingPrintableText = _pendingKittyPrintableSuppression;
        long pendingStartPosition = _pendingKittyPrintableSuppressionStartPosition;
        ClearPendingKittyPrintableSuppression ();

        if (string.IsNullOrEmpty (pendingPrintableText))
        {
            return false;
        }

        long expectedLastPosition = pendingStartPosition + pendingPrintableText.Length - 1;

        return _inputPosition == expectedLastPosition
               && string.Equals (printableText, pendingPrintableText, StringComparison.Ordinal);
    }

    private void InvalidatePendingKittyPrintableSuppression (char input)
    {
        if (string.IsNullOrEmpty (_pendingKittyPrintableSuppression))
        {
            return;
        }

        long offset = _inputPosition - _pendingKittyPrintableSuppressionStartPosition;

        if (offset >= 0
            && offset < _pendingKittyPrintableSuppression.Length
            && input == _pendingKittyPrintableSuppression [(int)offset])
        {
            return;
        }

        ClearPendingKittyPrintableSuppression ();
    }

    private void ClearPendingKittyPrintableSuppression ()
    {
        _pendingKittyPrintableSuppression = string.Empty;
        _pendingKittyPrintableSuppressionStartPosition = 0;
    }

    /// <inheritdoc/>
    public override void InjectKeyDownEvent (Key key)
    {
        // Convert Key → ANSI sequence (if needed) or char
        string sequence = AnsiKeyboardEncoder.Encode (key);

        // Enqueue directly to InputQueue for synchronous processing.
        // This bypasses the background input thread, which is correct for test injection.
        foreach (char ch in sequence)
        {
            InputQueue.Enqueue (ch);
        }
    }

    /// <inheritdoc/>
    public override void InjectMouseEvent (IApplication? app, Mouse mouse)
    {
        base.InjectMouseEvent (app, mouse);

        // Convert Mouse to ANSI SGR format escape sequence
        string ansiSequence = AnsiMouseEncoder.Encode (mouse);

        // Enqueue directly to InputQueue for synchronous processing.
        // This bypasses the background input thread, which is correct for test injection.
        foreach (char ch in ansiSequence)
        {
            InputQueue.Enqueue (ch);
        }
    }
}
