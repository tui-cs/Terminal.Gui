namespace Terminal.Gui.Drivers;

/// <summary>
///     When implemented in a derived class, allows watching an input stream of characters
///     (i.e. console input) for ANSI response sequences (mouse, cursor, query responses etc.).
/// </summary>
public interface IAnsiResponseParser
{
    /// <summary>
    ///     Current state of the parser based on what sequence of characters it has
    ///     read from the console input stream.
    /// </summary>
    AnsiResponseParserState State { get; }

    /// <summary>
    ///     Notifies the parser that you are expecting a response to come in
    ///     with the given <paramref name="terminator"/> (i.e. because you have
    ///     sent an ANSI request out).
    /// </summary>
    /// <param name="terminator">The terminator you expect to see on response.</param>
    /// <param name="value">The value you expect to see on response.</param>
    /// <param name="response">Callback to invoke when the response is seen in console input.</param>
    /// <param name="abandoned"></param>
    /// <param name="persistent">
    ///     <see langword="true"/> if you want this to persist permanently
    ///     and be raised for every event matching the <paramref name="terminator"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     If trying to register a persistent request for a terminator
    ///     that already has one.
    ///     exists.
    /// </exception>
    void ExpectResponse (string? terminator, string? value, Action<string?> response, Action? abandoned, bool persistent);

    /// <summary>
    ///     Returns true if there is an existing expectation (i.e. we are waiting a response
    ///     from console) for the given <paramref name="terminator"/>.
    /// </summary>
    /// <param name="terminator"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool IsExpecting (string? terminator, string? value);

    /// <summary>
    ///     Removes callback and expectation that we will get a response for the
    ///     given <pararef name="requestTerminator"/>. Use to give up on very old
    ///     requests e.g. if you want to send a different one with the same terminator.
    /// </summary>
    /// <param name="requestTerminator"></param>
    /// <param name="value"></param>
    /// <param name="persistent">
    ///     <see langword="true"/> if you want to remove a persistent
    ///     request listener.
    /// </param>
    void StopExpecting (string? requestTerminator, string? value, bool persistent);
}
