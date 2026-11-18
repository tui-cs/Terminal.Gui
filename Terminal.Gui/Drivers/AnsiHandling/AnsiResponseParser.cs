namespace Terminal.Gui.Drivers;

/// <summary>
///     String-based ANSI response parser for simple character stream processing.
/// </summary>
/// <remarks>
///     This parser variant works with plain strings without metadata, suitable for simpler
///     input processing scenarios or when platform-specific metadata is not needed.
/// </remarks>
internal class AnsiResponseParser (ITimeProvider timeProvider) : AnsiResponseParserBase (new StringHeld (), timeProvider)
{
    /// <summary>
    ///     Delegate for handling unexpected but complete ANSI escape sequences.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Return <see langword="true"/> to swallow the sequence (prevent it from reaching output stream).
    ///         Return <see langword="false"/> to release it to the output stream for downstream processing.
    ///     </para>
    ///     <para>
    ///         Default behavior returns <see langword="false"/>, releasing unrecognized sequences to the output.
    ///     </para>
    /// </remarks>
    public Func<string?, bool> UnknownResponseHandler { get; set; } = _ => false;

    /// <summary>
    ///     Processes input string and returns output with unrecognized escape sequences either handled or passed through.
    /// </summary>
    /// <param name="input">Input character string to process.</param>
    /// <returns>Output string with recognized escape sequences removed and unrecognized sequences either removed or retained.</returns>
    public string ProcessInput (string input)
    {
        var output = new StringBuilder ();

        ProcessInputBase (i => input [i],
                          i => input [i], // For string there is no T so object is same as char
                          c => AppendOutput (output, (char)c),
                          input.Length);

        return output.ToString ();
    }

    private void AppendOutput (StringBuilder output, char c) =>

        //Logging.Trace ($"AnsiResponseParser releasing '{c}'");
        output.Append (c);

    /// <summary>
    ///     Releases all currently held content (typically called when a timeout occurs or parser needs to flush).
    /// </summary>
    /// <returns>String representation of characters that were being held, or <see langword="null"/> if none.</returns>
    public string? Release ()
    {
        lock (_lockState)
        {
            TryLastMinuteSequences ();

            string output = _heldContent.HeldToString ();
            ResetState ();

            return output;
        }
    }

    /// <inheritdoc/>
    protected override bool ShouldSwallowUnexpectedResponse ()
    {
        lock (_lockState)
        {
            return UnknownResponseHandler.Invoke (_heldContent.HeldToString ());
        }
    }
}
