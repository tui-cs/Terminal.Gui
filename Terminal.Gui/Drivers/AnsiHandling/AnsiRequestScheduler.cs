using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Manages <see cref="AnsiEscapeSequenceRequest"/> made to an <see cref="IAnsiResponseParser"/>.
///     Ensures there are not 2+ outstanding requests with the same terminator, throttles request sends
///     to prevent console becoming unresponsive and handles evicting ignored requests (no reply from
///     terminal).
/// </summary>
public class AnsiRequestScheduler
{
    private readonly IAnsiResponseParser _parser;

    /// <summary>
    ///     Function for returning the current time. Use in unit tests to
    ///     ensure repeatable tests.
    /// </summary>
    internal Func<DateTime> Now { get; set; }

    private readonly HashSet<Tuple<AnsiEscapeSequenceRequest, DateTime>> _queuedRequests = new ();
    private readonly object _lockQueuedRequests = new ();

    internal IReadOnlyCollection<AnsiEscapeSequenceRequest> QueuedRequests
    {
        get
        {
            lock (_lockQueuedRequests)
            {
                return _queuedRequests.Select (r => r.Item1).ToList ();
            }
        }
    }

    /// <summary>
    ///     <para>
    ///         Dictionary where key is ansi request terminator and value tuple, when we last sent a request for
    ///         this terminator, preventing different requests with the same terminator (e.g. 't' with value "6" vs "8").
    ///         Combined with <see cref="_throttle"/> this prevents hammering the console with too many requests in sequence
    ///         which can cause console to freeze as there is no space for regular screen drawing / mouse events etc to come
    ///         in.
    ///     </para>
    ///     <para>
    ///         When user exceeds the throttle, new requests accumulate in <see cref="_queuedRequests"/> (i.e. remain
    ///         queued).
    ///     </para>
    /// </summary>
    private readonly ConcurrentDictionary<(string, string?), DateTime> _lastSend = new ();

    /// <summary>
    ///     Number of milliseconds after sending a request that we allow
    ///     another request to go out.
    /// </summary>
    private readonly TimeSpan _throttle = TimeSpan.FromMilliseconds (100);

    private readonly TimeSpan _runScheduleThrottle = TimeSpan.FromMilliseconds (100);

    /// <summary>
    ///     If console has not responded to a request after this period of time, we assume that it is never going
    ///     to respond. Only affects when we try to send a new request with the same terminator - at which point
    ///     we tell the parser to stop expecting the old request and start expecting the new request.
    /// </summary>
    private readonly TimeSpan _staleTimeout = TimeSpan.FromSeconds (1);

    private readonly DateTime _lastRun;

    /// <summary>
    ///     Creates a new instance.
    /// </summary>
    /// <param name="parser"></param>
    /// <param name="now"></param>
    public AnsiRequestScheduler (IAnsiResponseParser parser, Func<DateTime>? now = null)
    {
        _parser = parser;
        Now = now ?? (() => DateTime.Now);
        _lastRun = Now ();
    }

    /// <summary>
    ///     Sends the <paramref name="request"/> immediately or queues it if there is already
    ///     an outstanding request for the given <see cref="AnsiEscapeSequence.Terminator"/>.
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="request"></param>
    /// <returns><see langword="true"/> if request was sent immediately. <see langword="false"/> if it was queued.</returns>
    public bool SendOrSchedule (IDriver? driver, AnsiEscapeSequenceRequest request) => SendOrSchedule (driver, request, true);

    private bool SendOrSchedule (IDriver? driver, AnsiEscapeSequenceRequest request, bool addToQueue)
    {
        if (CanSend (request, out ReasonCannotSend reason))
        {
            Send (driver, request);

            //Logging.Trace ($"AnsiRequestScheduler: Sent request '{request.Request}' (Terminator='{request.Terminator}', Value='{request.Value ?? "<null>"}')");

            return true;
        }

        if (reason == ReasonCannotSend.OutstandingRequest)
        {
            // If we can evict an old request (no response from terminal after ages)
            if (EvictStaleRequests (request.Terminator, request.Value))
            {
                // Try again after evicting
                if (CanSend (request, out _))
                {
                    Send (driver, request);

                    return true;
                }
            }
        }

        if (addToQueue)
        {
            lock (_lockQueuedRequests)
            {
                _queuedRequests.Add (Tuple.Create (request, Now ()));

                //Logging.Trace ($"AnsiRequestScheduler: Queued request '{request.Request}' (QueueSize={_queuedRequests.Count})");
            }
        }

        return false;
    }

    private void EvictStaleRequests ()
    {
        foreach ((string stale, string? value) in _lastSend.Where (v => IsStale (v.Value)).Select (k => k.Key))
        {
            EvictStaleRequests (stale, value);
        }
    }

    private bool IsStale (DateTime dt) => Now () - dt > _staleTimeout;

    /// <summary>
    ///     Looks to see if the last time we sent <paramref name="withTerminator"/>
    ///     is a long time ago. If so we assume that we will never get a response and
    ///     can proceed with a new request for this terminator (returning <see langword="true"/>).
    /// </summary>
    /// <param name="withTerminator"></param>
    /// <param name="withValue"></param>
    /// <returns></returns>
    private bool EvictStaleRequests (string? withTerminator, string? withValue)
    {
        if (!_lastSend.TryGetValue ((withTerminator!, withValue), out DateTime dt))
        {
            return false;
        }

        if (!IsStale (dt))
        {
            return false;
        }

        _parser.StopExpecting (withTerminator, withValue, false);

        return true;
    }

    /// <summary>
    ///     Identifies and runs any <see cref="_queuedRequests"/> that can be sent based on the
    ///     current outstanding requests of the parser.
    /// </summary>
    /// <param name="driver"></param>
    /// <param name="force">
    ///     Repeated requests to run the schedule over short period of time will be ignored.
    ///     Pass <see langword="true"/> to override this behaviour and force evaluation of outstanding requests.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a request was found and run. <see langword="false"/>
    ///     if no outstanding requests or all have existing outstanding requests underway in parser.
    /// </returns>
    public bool RunSchedule (IDriver? driver, bool force = false)
    {
        if (!force && Now () - _lastRun < _runScheduleThrottle)
        {
            return false;
        }

        // Get oldest request
        Tuple<AnsiEscapeSequenceRequest, DateTime>? opportunity;

        lock (_lockQueuedRequests)
        {
            opportunity = _queuedRequests.MinBy (r => r.Item2);
        }

        if (opportunity != null)
        {
            // Give it another go
            if (SendOrSchedule (driver, opportunity.Item1, false))
            {
                lock (_lockQueuedRequests)
                {
                    _queuedRequests.Remove (opportunity);
                }

                return true;
            }
        }

        EvictStaleRequests ();

        return false;
    }

    private void Send (IDriver? driver, AnsiEscapeSequenceRequest r)
    {
        //Logging.Trace ($"AnsiRequestScheduler.Send: Terminator='{r.Terminator}' Value='{r.Value ?? "<null>"}'");

        _lastSend.AddOrUpdate ((r.Terminator!, r.Value), _ => Now (), (_, _) => Now ());
        _parser.ExpectResponse (r.Terminator, r.Value, r.ResponseReceived, r.Abandoned, false);
        r.Send (driver);
    }

    private bool CanSend (AnsiEscapeSequenceRequest r, out ReasonCannotSend reason)
    {
        if (ShouldThrottle (r))
        {
            reason = ReasonCannotSend.TooManyRequests;

            return false;
        }

        if (_parser.IsExpecting (r.Terminator, r.Value))
        {
            reason = ReasonCannotSend.OutstandingRequest;

            return false;
        }

        reason = default (ReasonCannotSend);

        return true;
    }

    private bool ShouldThrottle (AnsiEscapeSequenceRequest r)
    {
        if (_lastSend.TryGetValue ((r.Terminator!, r.Value), out DateTime value))
        {
            return Now () - value < _throttle;
        }

        return false;
    }
}
