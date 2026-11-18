namespace Terminal.Gui.Drivers;

internal enum ReasonCannotSend
{
    /// <summary>
    ///     No reason given.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The parser is already waiting for a request to complete with the given terminator.
    /// </summary>
    OutstandingRequest,

    /// <summary>
    ///     There have been too many requests sent recently, new requests will be put into
    ///     queue to prevent console becoming unresponsive.
    /// </summary>
    TooManyRequests
}
