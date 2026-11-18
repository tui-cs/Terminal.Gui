namespace Terminal.Gui.Drivers;

/// <summary>
///     Represents the status of an ANSI escape sequence request made to the terminal using
///     <see cref="EscSeqRequests"/>.
/// </summary>
/// <remarks></remarks>
public class EscSeqReqStatus
{
    /// <summary>Creates a new state of escape sequence request.</summary>
    /// <param name="terminator">The terminator.</param>
    /// <param name="numReq">The number of requests.</param>
    public EscSeqReqStatus (string terminator, int numReq)
    {
        Terminator = terminator;
        NumRequests = NumOutstanding = numReq;
    }

    /// <summary>Gets the number of unfinished requests.</summary>
    public int NumOutstanding { get; internal set; }

    /// <summary>Gets the number of requests.</summary>
    public int NumRequests { get; internal set; }

    /// <summary>Gets the Escape Sequence Terminator (e.g. ESC[8t ... t is the terminator).</summary>
    public string Terminator { get; }
}
