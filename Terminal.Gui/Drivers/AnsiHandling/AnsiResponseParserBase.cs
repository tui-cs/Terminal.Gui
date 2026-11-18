using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     The base class for ANSI response parsers handling escape sequences, expected responses, mouse, and keyboard events.
/// </summary>
internal abstract class AnsiResponseParserBase (IHeld heldContent, ITimeProvider timeProvider) : IAnsiResponseParser
{
    #region Fields and State Management

    private const char ESCAPE = '\x1B';
    private const char BEL = '\a';

    /// <summary>
    ///     Maximum number of characters that can be accumulated in held content before the parser
    ///     abandons the current escape sequence and releases buffered content. This prevents unbounded
    ///     memory growth from malformed or malicious unterminated escape sequences.
    /// </summary>
    internal const int MaxHeldLength = 8 * 1024;

    /// <summary>
    ///     Maximum number of characters allowed in a single bracketed-paste payload. Pastes larger
    ///     than this are truncated and the truncated content is delivered immediately. Any remaining
    ///     bytes from the same paste are discarded until the matching <c>ESC[201~</c> end marker
    ///     arrives so tail bytes cannot leak into normal input processing. Guards against unbounded
    ///     memory growth from a missing or stripped end marker.
    /// </summary>
    internal const int MaxBracketedPasteLength = 1 * 1024 * 1024;

    /// <summary>
    ///     Buffer accumulating pasted text while the parser is in
    ///     <see cref="AnsiResponseParserState.InBracketedPaste"/>.
    /// </summary>
    private readonly StringBuilder _pasteBuffer = new ();

    /// <summary>
    ///     Trailing characters of <see cref="_pasteBuffer"/> that may form the start of the
    ///     <c>ESC[201~</c> end marker. Tracked so the marker bytes are not delivered as paste content.
    /// </summary>
    private int _pasteEndMatchLength;

    /// <summary>
    ///     Tracks whether the parser is currently inside an OSC (Operating System Command) sequence.
    ///     OSC responses can be terminated by ST (ESC \) which requires special handling because
    ///     ESC normally starts a new sequence in the parser.
    /// </summary>
    private bool _inOscSequence;

    /// <summary>
    ///     Tracks whether we've seen ESC within an OSC sequence, indicating a potential ST (ESC \) terminator.
    /// </summary>
    private bool _oscExpectingBackslash;

    protected object _lockExpectedResponses = new ();
    protected object _lockState = new ();
    protected readonly IHeld _heldContent = heldContent;
    protected readonly ITimeProvider _timeProvider = timeProvider;

    /// <summary>
    ///     Responses we are expecting to come in (one-time expectations).
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _expectedResponses = [];

    /// <summary>
    ///     Collection of responses that have been stopped via <see cref="StopExpecting"/>.
    ///     These are swallowed but do not invoke callbacks to avoid corrupting downstream processing.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _lateResponses = [];

    /// <summary>
    ///     Persistent expectations that remain active across multiple responses (e.g., continuous mouse events).
    /// </summary>
    protected readonly List<AnsiResponseExpectation> _persistentExpectations = [];

    // Valid ANSI response terminators per CSI specification
    // See https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
    // Note: N and O are intentionally excluded as they have special handling
    protected readonly HashSet<char> _knownTerminators = [.. EscSeqUtils.KnownTerminators];

    /// <inheritdoc/>
    public AnsiResponseParserState State
    {
        get;
        protected set
        {
            StateChangedAt = _timeProvider.Now;
            field = value;
        }
    } = AnsiResponseParserState.Normal;

    /// <summary>
    ///     Timestamp when <see cref="State"/> was last changed. Used to detect stale escape sequences.
    /// </summary>
    public DateTime StateChangedAt { get; private set; }

    /// <summary>
    ///     Timestamp when the parser last received a byte belonging to the current bracketed paste.
    ///     Used to detect idle paste sessions without treating an active slow paste as stale.
    /// </summary>
    internal DateTime LastBracketedPasteInputAt { get; private set; }

    #endregion

    #region Constructor and State Management

    protected void ResetState ()
    {
        lock (_lockState)
        {
            State = AnsiResponseParserState.Normal;
            _inOscSequence = false;
            _oscExpectingBackslash = false;
            _pasteBuffer.Clear ();
            _pasteEndMatchLength = 0;
            _heldContent.ClearHeld ();
        }
    }

    #endregion

    #region Input Processing

    /// <summary>
    ///     Processes an input collection of objects <paramref name="inputLength"/> long.
    ///     Parses ANSI escape sequences and routes them to appropriate handlers (mouse, keyboard, expected responses).
    /// </summary>
    /// <param name="getCharAtIndex">Function to get the character representation of element i in the input collection.</param>
    /// <param name="getObjectAtIndex">Function to get the actual element at index i (e.g., char or Tuple&lt;char,T&gt;).</param>
    /// <param name="appendOutput">
    ///     Action invoked when the parser confirms an element should be appended to the output stream
    ///     (i.e., it's not part of a recognized escape sequence).
    /// </param>
    /// <param name="inputLength">The total number of elements in the input collection.</param>
    protected void ProcessInputBase (Func<int, char> getCharAtIndex, Func<int, object> getObjectAtIndex, Action<object> appendOutput, int inputLength)
    {
        List<string> pendingPastes = [];

        lock (_lockState)
        {
            ProcessInputBaseImpl (getCharAtIndex, getObjectAtIndex, appendOutput, inputLength, pendingPastes);
        }

        foreach (string text in pendingPastes)
        {
            Paste?.Invoke (this, text);
        }
    }

    private void ProcessInputBaseImpl (
        Func<int, char> getCharAtIndex,
        Func<int, object> getObjectAtIndex,
        Action<object> appendOutput,
        int inputLength,
        List<string> pendingPastes
    )
    {
        var index = 0; // Tracks position in the input string

        while (index < inputLength)
        {
            char currentChar = getCharAtIndex (index);
            object currentObj = getObjectAtIndex (index);

            bool isEscape = currentChar == ESCAPE;

            switch (State)
            {
                case AnsiResponseParserState.Normal:
                    if (isEscape)
                    {
                        // Escape character detected, move to ExpectingBracket state
                        State = AnsiResponseParserState.ExpectingEscapeSequence;
                        _heldContent.AddToHeld (currentObj); // Hold the escape character
                    }
                    else
                    {
                        // Normal character, append to output
                        appendOutput (currentObj);
                    }

                    break;

                case AnsiResponseParserState.ExpectingEscapeSequence:
                    if (isEscape)
                    {
                        // Second escape so we must release first
                        ReleaseHeld (appendOutput, AnsiResponseParserState.ExpectingEscapeSequence);
                        _heldContent.AddToHeld (currentObj); // Hold the new escape
                    }
                    else if (_heldContent.Length == 1)
                    {
                        //We need O for SS3 mode F1-F4 e.g. "<esc>OP" => F1
                        //We need any letter or digit for Alt+Letter (see EscAsAltPattern)
                        //In fact lets just always see what comes after esc

                        // Track whether this is an OSC sequence (ESC ])
                        _inOscSequence = currentChar == ']';

                        // Detected '[', ']', 'O', etc., transition to InResponse state
                        State = AnsiResponseParserState.InResponse;
                        _heldContent.AddToHeld (currentObj); // Hold the letter
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        ReleaseHeld (appendOutput);
                        appendOutput (currentObj); // Add current character
                    }

                    break;

                case AnsiResponseParserState.InBracketedPaste:
                    NoteBracketedPasteActivity ();
                    AppendToPaste (currentChar, pendingPastes);

                    if (_pasteBuffer.Length >= MaxBracketedPasteLength)
                    {
                        FlushPaste (AnsiResponseParserState.DiscardingBracketedPasteRemainder, pendingPastes);
                    }

                    break;

                case AnsiResponseParserState.DiscardingBracketedPasteRemainder:
                    NoteBracketedPasteActivity ();
                    DiscardBracketedPasteRemainder (currentChar);

                    break;

                case AnsiResponseParserState.InResponse:

                    // Guard against unbounded memory growth from malformed/unterminated sequences
                    if (_heldContent.Length >= MaxHeldLength)
                    {
                        ReleaseHeld (appendOutput);
                        appendOutput (currentObj);

                        break;
                    }

                    if (_inOscSequence)
                    {
                        if (_oscExpectingBackslash)
                        {
                            // We previously saw ESC inside an OSC sequence
                            if (currentChar == '\\')
                            {
                                // ST complete (ESC \) — add backslash and try to match
                                _heldContent.AddToHeld (currentObj);
                                _oscExpectingBackslash = false;

                                if (!HandleHeldContent ())
                                {
                                    // No match — release as normal output
                                    ReleaseHeld (appendOutput);
                                }
                            }
                            else
                            {
                                // Malformed: ESC followed by non-backslash inside OSC — release everything
                                _oscExpectingBackslash = false;
                                _inOscSequence = false;
                                ReleaseHeld (appendOutput);
                                appendOutput (currentObj);
                            }
                        }
                        else if (isEscape)
                        {
                            // Possible start of ST (ESC \) — accumulate ESC and wait for next char
                            _heldContent.AddToHeld (currentObj);
                            _oscExpectingBackslash = true;
                        }
                        else if (currentChar == BEL)
                        {
                            // BEL terminates OSC sequences in many terminals
                            _heldContent.AddToHeld (currentObj);

                            if (!HandleHeldContent ())
                            {
                                // No match — release as normal output
                                ReleaseHeld (appendOutput);
                            }
                        }
                        else
                        {
                            // Continue accumulating OSC content
                            _heldContent.AddToHeld (currentObj);
                        }
                    }
                    else if (isEscape)
                    {
                        // if seeing another esc, we must resolve the current one first
                        ReleaseHeld (appendOutput);
                        State = AnsiResponseParserState.ExpectingEscapeSequence;
                        _heldContent.AddToHeld (currentObj);
                    }
                    else
                    {
                        // Non esc, so continue to build sequence
                        _heldContent.AddToHeld (currentObj);

                        // Raise mouse or keyboard events if applicable
                        if (HandleHeldContent ())
                        {
                            ReleaseHeld (appendOutput);
                        }
                    }

                    break;
            }

            index++;
        }
    }

    #endregion

    #region Held Content Management

    private void ReleaseHeld (Action<object> appendOutput, AnsiResponseParserState newState = AnsiResponseParserState.Normal)
    {
        TryLastMinuteSequences ();

        foreach (object o in _heldContent.HeldToObjects ())
        {
            appendOutput (o);
        }

        State = newState;
        _inOscSequence = false;
        _oscExpectingBackslash = false;
        _heldContent.ClearHeld ();
    }

    /// <summary>
    ///     Checks currently held characters against sequences that have conflicts with longer sequences
    ///     (e.g., Esc as Alt sequences which can conflict with ESC O P SS3 sequences).
    /// </summary>
    /// <remarks>
    ///     This is called as a last resort before releasing held content to handle ambiguous sequences
    ///     where shorter patterns might match, but we need to wait to see if a longer pattern emerges.
    /// </remarks>
    protected void TryLastMinuteSequences ()
    {
        lock (_lockState)
        {
            string cur = _heldContent.HeldToString ();

            if (HandleKeyboard)
            {
                AnsiKeyboardParserPattern? pattern = _keyboardParser.IsKeyboard (cur, true);

                if (pattern != null)
                {
                    // BUGBUG: We are on the UI thread. This will 'block' as whatever handles the event does its work.
                    // BUGBUG: Thus, we should be calling ResetState first, to clear held content before raising event.
                    RaiseKeyboardEvent (pattern, cur);
                    _heldContent.ClearHeld ();

                    return;
                }
            }

            // We have something totally unexpected, not a CSI and
            // still Esc+<something>. So give last minute swallow chance
            if (cur.Length < 2 || cur [0] != ESCAPE)
            {
                return;
            }

            // Maybe swallow anyway if user has custom delegate
            if (!ShouldSwallowUnexpectedResponse ())
            {
                return;
            }
            _heldContent.ClearHeld ();

            Tracing.Trace.Lifecycle (string.Empty, "Ansi", $"AnsiResponseParser last minute swallowed '{cur}'");
        }
    }

    /// <summary>
    ///     Handles currently held content (raising events as needed). Return value indicates whether the held content should
    ///     be released based on accumulated escape sequence.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> to release held content to output stream;
    ///     <see langword="false"/> to continue accumulating or if content was handled internally.
    /// </returns>
    private bool HandleHeldContent ()
    {
        // BUGBUG: No need to lock, as this is only called from within a locked context
        lock (_lockState)
        {
            string cur = _heldContent.HeldToString ();

            // Bracketed paste start (ESC[200~). Switch into paste-collecting mode and discard
            // the marker. Subsequent input is accumulated as paste content until the matching
            // end marker (ESC[201~) is seen — see ProcessInputBaseImpl InBracketedPaste case.
            if (cur == EscSeqUtils.CSI_BracketedPasteStart)
            {
                _heldContent.ClearHeld ();
                _pasteBuffer.Clear ();
                _pasteEndMatchLength = 0;
                State = AnsiResponseParserState.InBracketedPaste;
                NoteBracketedPasteActivity ();

                return false;
            }

            if (HandleMouse && IsMouse (cur))
            {
                // See https://github.com/tui-cs/Terminal.Gui/issues/4587#issuecomment-3770132337 for why
                // we call ResetState first
                ResetState ();
                RaiseMouseEvent (cur);

                return false;
            }

            lock (_lockExpectedResponses)
            {
                // Expected responses take priority over keyboard pattern matching.
                // A registered expectation is an explicit request from the app, and its
                // response may collide with a keyboard pattern — e.g. a CPR reply
                // "ESC[1;1R" looks identical to xterm's Shift/modifier F3 sequence
                // "ESC[1;<n>R" and would otherwise be misdelivered as Key.F3 (#4956).
                if (MatchResponse (cur, _expectedResponses, true, true))
                {
                    return false;
                }

                // Also try looking for late requests - in which case we do not invoke but still swallow content to avoid corrupting downstream
                if (MatchResponse (cur, _lateResponses, false, true))
                {
                    return false;
                }

                // Look for persistent requests
                if (MatchResponse (cur, _persistentExpectations, true, false))
                {
                    return false;
                }
            }

            if (HandleKeyboard)
            {
                AnsiKeyboardParserPattern? pattern = _keyboardParser.IsKeyboard (cur);

                if (pattern != null)
                {
                    // See https://github.com/tui-cs/Terminal.Gui/issues/4587#issuecomment-3770132337 for why
                    // we call ResetState first
                    ResetState ();
                    RaiseKeyboardEvent (pattern, cur);

                    return false;
                }
            }

            // Finally if it is a valid ansi response but not one we are expect (e.g. its mouse activity)
            // then we can release it back to input processing stream
            bool isCompleteCsi = cur.StartsWith (EscSeqUtils.CSI, StringComparison.Ordinal) && _knownTerminators.Contains (cur.Last ());

            bool isCompleteOsc = _inOscSequence
                                 && cur.StartsWith (EscSeqUtils.OSC, StringComparison.Ordinal)
                                 && (cur.EndsWith (EscSeqUtils.ST, StringComparison.Ordinal) || cur [^1] == BEL);

            if (!isCompleteCsi && !isCompleteOsc)
            {
                return false; // Continue accumulating
            }

            // We have found a terminator so bail
            State = AnsiResponseParserState.Normal;

            switch (ShouldSwallowUnexpectedResponse ())
            {
                case true:
                    ResetState ();

                    Tracing.Trace.Lifecycle (string.Empty, "Ansi", $"AnsiResponseParser swallowed '{cur}'");

                    // Do not send back to input stream
                    return false;

                default:
                    // Do release back to input stream
                    return true;
            }
        }
    }

    /// <summary>
    ///     When overridden in a derived class, determines whether an unexpected but valid ANSI response
    ///     should be swallowed (not released to output) or released to the input stream.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is only called for complete ANSI responses (sequences ending with a known terminator
    ///         from <see cref="_knownTerminators"/>) that don't match any expected response patterns.
    ///     </para>
    ///     <para>
    ///         Implement this to provide custom handling for unexpected escape sequences.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> to swallow the sequence (prevent it from reaching output stream);
    ///     <see langword="false"/> to release it to the output stream.
    /// </returns>
    protected abstract bool ShouldSwallowUnexpectedResponse ();

    #endregion

    #region Response Expectation Management

    /// <summary>
    ///     Attempts to match the current accumulated input against a collection of expected responses.
    /// </summary>
    /// <param name="cur">The current accumulated input string.</param>
    /// <param name="collection">The collection of expectations to match against.</param>
    /// <param name="invokeCallback">Whether to invoke the response callback if a match is found.</param>
    /// <param name="removeExpectation">Whether to remove the expectation from the collection after matching.</param>
    /// <returns><see langword="true"/> if a match was found; <see langword="false"/> otherwise.</returns>
    private bool MatchResponse (string? cur, List<AnsiResponseExpectation> collection, bool invokeCallback, bool removeExpectation)
    {
        // Check for expected responses
        AnsiResponseExpectation? matchingResponse = collection.FirstOrDefault (r => r.Matches (cur));

        if (matchingResponse?.Response == null)
        {
            return false;
        }

        //Logging.Trace ($"AnsiResponseParser processed '{cur}'");

        if (invokeCallback)
        {
            matchingResponse.Response.Invoke (_heldContent);
        }

        ResetState ();

        if (removeExpectation)
        {
            collection.Remove (matchingResponse);
        }

        return true;
    }

    /// <inheritdoc/>
    public void ExpectResponse (string? terminator, string? value, Action<string?> response, Action? abandoned, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                _persistentExpectations.Add (new AnsiResponseExpectation (terminator, value, h => response.Invoke (h.HeldToString ()), abandoned));
            }
            else
            {
                _expectedResponses.Add (new AnsiResponseExpectation (terminator, value, h => response.Invoke (h.HeldToString ()), abandoned));
            }
        }
    }

    /// <inheritdoc/>
    public bool IsExpecting (string? terminator, string? value)
    {
        lock (_lockExpectedResponses)
        {
            if (string.IsNullOrEmpty (terminator))
            {
                return false;
            }

            // Conservative collision logic: any existing expectation with same terminator
            // collides unless both have a specific value and those values differ.
            return _expectedResponses.Any (r => r.Terminator != null
                                                && r.Terminator!.Any (terminator.Contains)
                                                && (string.IsNullOrEmpty (r.Value) || string.IsNullOrEmpty (value) || r.Value == value));
        }
    }

    /// <inheritdoc/>
    public void StopExpecting (string? terminator, string? value, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                AnsiResponseExpectation [] removed;

                if (string.IsNullOrEmpty (value))
                {
                    removed = _persistentExpectations.Where (r => r.Terminator == terminator).ToArray ();
                }
                else
                {
                    removed = _persistentExpectations.Where (r => r.Terminator == terminator && r.Value == value).ToArray ();
                }

                foreach (AnsiResponseExpectation toRemove in removed)
                {
                    _persistentExpectations.Remove (toRemove);
                    toRemove.Abandoned?.Invoke ();
                }
            }
            else
            {
                AnsiResponseExpectation [] removed;

                if (string.IsNullOrEmpty (value))
                {
                    removed = _expectedResponses.Where (r => r.Terminator == terminator).ToArray ();
                }
                else
                {
                    removed = _expectedResponses.Where (r => r.Terminator == terminator && r.Value == value).ToArray ();
                }

                foreach (AnsiResponseExpectation r in removed)
                {
                    _expectedResponses.Remove (r);
                    _lateResponses.Add (r);
                    r.Abandoned?.Invoke ();
                }
            }
        }
    }

    #endregion

    #region Mouse Handling

    private readonly AnsiMouseParser _mouseParser = new ();

    /// <summary>
    ///     Event raised when mouse events are detected in the input stream.
    /// </summary>
    /// <remarks>
    ///     Requires setting <see cref="HandleMouse"/> to <see langword="true"/>.
    ///     Mouse events follow SGR extended format (ESC[&lt;button;x;yM/m) when
    ///     <see cref="EscSeqUtils.CSI_EnableMouseEvents"/> is enabled.
    /// </remarks>
    public event EventHandler<Mouse>? Mouse;

    /// <summary>
    ///     Gets or sets whether to explicitly handle mouse escape sequences by raising the <see cref="Mouse"/> event.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/>, mouse sequences are parsed and raised as events.
    ///     When <see langword="false"/> (default), mouse sequences are treated as regular input.
    /// </remarks>
    public bool HandleMouse { get; set; } = false;

    private void RaiseMouseEvent (string? cur)
    {
        //Logging.Trace ($"{cur}");
        Mouse? ev = _mouseParser.ProcessMouseInput (cur);

        if (ev != null)
        {
            Mouse?.Invoke (this, ev);
        }
    }

    private bool IsMouse (string? cur) => _mouseParser.IsMouse (cur);

    #endregion

    #region Keyboard Handling

    protected readonly AnsiKeyboardParser _keyboardParser = new ();

    /// <summary>
    ///     Event raised when keyboard escape sequences are detected (e.g., cursor keys, function keys).
    /// </summary>
    /// <remarks>
    ///     Requires setting <see cref="HandleKeyboard"/> to <see langword="true"/>.
    ///     Handles sequences like ESC[A (cursor up), ESC[1;5A (Ctrl+cursor up), ESC OP (F1 in SS3 mode).
    /// </remarks>
    public event EventHandler<Key>? Keyboard;

    /// <summary>
    ///     Raised with the parser pattern that recognized a keyboard escape sequence.
    /// </summary>
    internal event Action<AnsiKeyboardParserPattern, Key>? KeyboardPatternMatched;

    /// <summary>
    ///     Gets or sets whether to explicitly handle keyboard escape sequences by raising the <see cref="Keyboard"/> event.
    /// </summary>
    /// <remarks>
    ///     When <see langword="true"/>, keyboard sequences are parsed and raised as events.
    ///     When <see langword="false"/> (default), keyboard sequences are treated as regular input.
    /// </remarks>
    public bool HandleKeyboard { get; set; } = false;

    protected void RaiseKeyboardEvent (AnsiKeyboardParserPattern pattern, string? cur)
    {
        Key? k = pattern.GetKey (cur);

        if (k is null)
        {
            Logging.Logger.LogError ($"Failed to determine a Key for given Keyboard escape sequence '{cur}'");
        }
        else
        {
            KeyboardPatternMatched?.Invoke (pattern, k);
            Keyboard?.Invoke (this, k);
        }
    }

    #endregion

    #region Bracketed Paste Handling

    /// <summary>
    ///     Event raised when a complete bracketed paste sequence is detected. The string carries the
    ///     pasted content with the bracketing markers (<c>ESC[200~</c> / <c>ESC[201~</c>) stripped.
    /// </summary>
    /// <remarks>
    ///     Bracketed paste mode must be enabled by writing <see cref="EscSeqUtils.CSI_EnableBracketedPaste"/>
    ///     to the terminal. Without it, terminals deliver pasted text as raw input characters which are
    ///     indistinguishable from typing.
    /// </remarks>
    public event EventHandler<string>? Paste;

    private void AppendToPaste (char c, List<string> pendingPastes)
    {
        _pasteBuffer.Append (c);

        if (!AdvanceBracketedPasteEndMatch (c))
        {
            return;
        }

        // Full end marker matched — strip its characters from the buffer and dispatch.
        _pasteBuffer.Length -= EscSeqUtils.CSI_BracketedPasteEnd.Length;
        FlushPaste (AnsiResponseParserState.Normal, pendingPastes);
    }

    private void DiscardBracketedPasteRemainder (char c)
    {
        if (!AdvanceBracketedPasteEndMatch (c))
        {
            return;
        }

        _pasteEndMatchLength = 0;
        State = AnsiResponseParserState.Normal;
    }

    private bool AdvanceBracketedPasteEndMatch (char c)
    {
        // Track an in-flight suffix match against ESC[201~ so we can strip or discard the marker
        // without scanning the whole buffer on every character.
        string endMarker = EscSeqUtils.CSI_BracketedPasteEnd;

        if (_pasteEndMatchLength < endMarker.Length && c == endMarker [_pasteEndMatchLength])
        {
            _pasteEndMatchLength++;
        }
        else
        {
            // Mismatch: ESC[201~ has no repeated prefix, so the only restart is at a fresh ESC.
            _pasteEndMatchLength = c == ESCAPE ? 1 : 0;
        }

        return _pasteEndMatchLength == endMarker.Length;
    }

    private string DrainPasteBuffer (AnsiResponseParserState nextState)
    {
        if (nextState == AnsiResponseParserState.DiscardingBracketedPasteRemainder && _pasteEndMatchLength > 0)
        {
            _pasteBuffer.Length -= _pasteEndMatchLength;
        }

        string text = _pasteBuffer.ToString ();
        _pasteBuffer.Clear ();

        if (nextState != AnsiResponseParserState.DiscardingBracketedPasteRemainder)
        {
            _pasteEndMatchLength = 0;
        }

        State = nextState;

        return text;
    }

    private void FlushPaste (AnsiResponseParserState nextState, List<string> pendingPastes)
    {
        string text = DrainPasteBuffer (nextState);
        pendingPastes.Add (text);
    }

    private void NoteBracketedPasteActivity () => LastBracketedPasteInputAt = _timeProvider.Now;

    /// <summary>
    ///     Flushes any in-flight bracketed-paste buffer as a <see cref="Paste"/> event and returns the
    ///     parser to <see cref="AnsiResponseParserState.Normal"/>. Called by the input processor when
    ///     the paste has been idle too long, so a terminal that drops the <c>ESC[201~</c> end marker
    ///     does not strand pasted content forever.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the parser was reset from a bracketed-paste state;
    ///     <see langword="false"/> if the parser was not in a bracketed-paste state.
    /// </returns>
    internal bool FlushStaleBracketedPaste ()
    {
        string? text = null;

        lock (_lockState)
        {
            if (State == AnsiResponseParserState.InBracketedPaste)
            {
                text = DrainPasteBuffer (AnsiResponseParserState.Normal);
            }
            else if (State == AnsiResponseParserState.DiscardingBracketedPasteRemainder)
            {
                ResetState ();
            }
            else
            {
                return false;
            }
        }

        if (text is { })
        {
            Paste?.Invoke (this, text);
        }

        return true;
    }

    #endregion
}
