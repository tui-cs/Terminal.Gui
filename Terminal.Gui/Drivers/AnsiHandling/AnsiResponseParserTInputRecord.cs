namespace Terminal.Gui.Drivers;

/// <summary>
///     Generic ANSI response parser that preserves metadata alongside characters.
/// </summary>
/// <typeparam name="TInputRecord">The metadata type associated with each character (e.g., ConsoleKeyInfo).</typeparam>
/// <remarks>
///     This parser variant maintains the association between characters and their metadata throughout
///     the parsing process, useful when the driver needs to preserve platform-specific input information.
/// </remarks>
internal class AnsiResponseParser<TInputRecord> (ITimeProvider timeProvider) : AnsiResponseParserBase (new GenericHeld<TInputRecord> (), timeProvider)
{
    /// <summary>
    ///     Delegate for handling unexpected but complete ANSI escape sequences.
    /// </summary>
    /// <remarks>
    ///     Return <see langword="true"/> to swallow the sequence (prevent it from reaching output).
    ///     Return <see langword="false"/> to release it to the output stream.
    ///     Default behavior is to release (<see langword="false"/>).
    /// </remarks>
    public Func<IEnumerable<Tuple<char, TInputRecord>>, bool> UnexpectedResponseHandler { get; set; } = _ => false;

    /// <summary>
    ///     Processes input and returns output with unrecognized escape sequences either handled or passed through.
    /// </summary>
    /// <param name="input">Input tuples of characters with their associated metadata.</param>
    /// <returns>Output tuples that were not recognized as escape sequences or were explicitly released.</returns>
    public IEnumerable<Tuple<char, TInputRecord>> ProcessInput (params Tuple<char, TInputRecord> [] input)
    {
        List<Tuple<char, TInputRecord>> output = [];

        ProcessInputBase (i => input [i].Item1, i => input [i], c => AppendOutput (output, c), input.Length);

        return output;
    }

    private void AppendOutput (List<Tuple<char, TInputRecord>> output, object c)
    {
        Tuple<char, TInputRecord> tuple = (Tuple<char, TInputRecord>)c;

        //Logging.Trace ($"AnsiResponseParser releasing '{tuple.Item1}'");
        output.Add (tuple);
    }

    /// <summary>
    ///     Releases all currently held content (typically called when a timeout occurs or parser needs to flush).
    /// </summary>
    /// <returns>Array of character-metadata tuples that were being held.</returns>
    public Tuple<char, TInputRecord> [] Release ()
    {
        // Lock in case Release is called from different Thread from parse
        lock (_lockState)
        {
            TryLastMinuteSequences ();

            Tuple<char, TInputRecord> [] result = HeldToEnumerable ().ToArray ();

            ResetState ();

            return result;
        }
    }

    private IEnumerable<Tuple<char, TInputRecord>> HeldToEnumerable () => (IEnumerable<Tuple<char, TInputRecord>>)_heldContent.HeldToObjects ();

    /// <summary>
    ///     Registers an expectation for a response that requires access to both characters and metadata.
    /// </summary>
    /// <remarks>
    ///     This method has a unique name (ExpectResponseT) to avoid ambiguous overload resolution when using lambdas.
    /// </remarks>
    /// <param name="terminator">The terminating character(s) that indicate the response is complete.</param>
    /// <param name="value">The value character(s) that prevents collision with requests with the same terminator.</param>
    /// <param name="response">Callback invoked with the character-metadata tuples when the response arrives.</param>
    /// <param name="abandoned">Optional callback invoked if the expectation is cancelled or times out.</param>
    /// <param name="persistent">
    ///     If <see langword="true"/>, the expectation remains active for multiple responses.
    ///     If <see langword="false"/>, it's removed after the first match.
    /// </param>
    public void ExpectResponseT (string? terminator, string? value, Action<IEnumerable<Tuple<char, TInputRecord>>> response, Action? abandoned, bool persistent)
    {
        lock (_lockExpectedResponses)
        {
            if (persistent)
            {
                _persistentExpectations.Add (new AnsiResponseExpectation (terminator, value, _ => response.Invoke (HeldToEnumerable ()), abandoned));
            }
            else
            {
                _expectedResponses.Add (new AnsiResponseExpectation (terminator, value, _ => response.Invoke (HeldToEnumerable ()), abandoned));
            }
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldSwallowUnexpectedResponse () => UnexpectedResponseHandler.Invoke (HeldToEnumerable ());
}
