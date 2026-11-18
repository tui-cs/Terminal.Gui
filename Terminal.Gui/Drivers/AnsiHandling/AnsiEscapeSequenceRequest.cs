namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes an ongoing ANSI request sent to the console.
///     Use <see cref="ResponseReceived"/> to handle the response
///     when console answers the request.
/// </summary>
public class AnsiEscapeSequenceRequest : AnsiEscapeSequence
{
    /// <summary>
    ///     Invoked when the console responds with an ANSI response code that matches the
    ///     <see cref="AnsiEscapeSequence.Terminator"/>
    /// </summary>
    public required Action<string?> ResponseReceived { get; init; }

    /// <summary>
    ///     Invoked if the console fails to responds to the ANSI response code
    /// </summary>
    public Action? Abandoned { get; init; }

    /// <summary>
    ///     Sends the <see cref="AnsiEscapeSequence.Request"/> to the raw output stream of the current <see cref="IDriver"/>.
    ///     Only call this method from the main UI thread. You should use <see cref="AnsiRequestScheduler"/> if
    ///     sending many requests.
    /// </summary>
    /// <param name="driver"></param>
    public void Send (IDriver? driver) { driver?.WriteRaw (Request); }

}
