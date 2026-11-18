namespace Terminal.Gui.Drivers;

/// <summary>
///     Manages ANSI Escape Sequence requests and responses. The list of <see cref="EscSeqReqStatus"/>
///     contains the
///     status of the request. Each request is identified by the terminator (e.g. ESC[8t ... t is the terminator).
/// </summary>
public static class EscSeqRequests
{
    /// <summary>Gets the <see cref="EscSeqReqStatus"/> list.</summary>
    public static List<EscSeqReqStatus> Statuses { get; } = [];

    /// <summary>
    ///     Adds a new request for the ANSI Escape Sequence defined by <paramref name="terminator"/>. Adds a
    ///     <see cref="EscSeqReqStatus"/> instance to <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="terminator">The terminator.</param>
    /// <param name="numRequests">The number of requests.</param>
    public static void Add (string? terminator, int numRequests = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty (terminator);

        int numReq = Math.Max (numRequests, 1);

        lock (Statuses)
        {
            EscSeqReqStatus? found = Statuses.Find (x => x.Terminator == terminator);

            if (found is null)
            {
                Statuses.Add (new (terminator, numReq));
            }
            else
            {
                found.NumRequests += numReq;
                found.NumOutstanding += numReq;
            }
        }
    }

    /// <summary>
    ///     Clear the <see cref="Statuses"/> property.
    /// </summary>
    public static void Clear ()
    {
        lock (Statuses)
        {
            Statuses.Clear ();
        }
    }

    /// <summary>
    ///     Indicates if a <see cref="EscSeqReqStatus"/> with the <paramref name="terminator"/> exists in the
    ///     <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="terminator"></param>
    /// <returns><see langword="true"/> if exist, <see langword="false"/> otherwise.</returns>
    public static bool HasResponse (string terminator)
    {
        lock (Statuses)
        {
            EscSeqReqStatus? found = Statuses.Find (x => x.Terminator == terminator);

            return found is { };
        }
    }

    /// <summary>
    ///     Removes a request defined by <paramref name="terminator"/>. If a matching <see cref="EscSeqReqStatus"/> is
    ///     found and the number of outstanding requests is greater than 0, the number of outstanding requests is decremented.
    ///     If the number of outstanding requests is 0, the <see cref="EscSeqReqStatus"/> is removed from
    ///     <see cref="Statuses"/>.
    /// </summary>
    /// <param name="terminator">The terminating string.</param>
    public static void Remove (string terminator)
    {
        ArgumentException.ThrowIfNullOrEmpty (terminator);

        lock (Statuses)
        {
            EscSeqReqStatus? found = Statuses.Find (x => x.Terminator == terminator);

            if (found is null)
            {
                return;
            }

            found.NumOutstanding--;

            if (found.NumOutstanding == 0)
            {
                Statuses.Remove (found);
            }
        }
    }
}
