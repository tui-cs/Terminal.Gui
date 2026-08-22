using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Pure ANSI Driver with VT Input Mode on Windows and termios raw mode on Unix/Mac.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IInput{TInputRecord}"/> implementation that uses a character stream for pure ANSI input.
///         Supports both test injection via <see cref="ITestableInput{TInputRecord}"/> and real console reading.
///     </para>
///     <para>
///         This driver reads raw bytes from different sources depending on platform and processes them as
///         ANSI escape sequences. It configures the terminal for proper ANSI input:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Unix/Mac</b> - Uses <see cref="UnixRawModeHelper"/> to disable echo and line buffering (raw mode).
///             Uses <c>poll()</c> and <c>read()</c> syscalls for non-blocking input via <see cref="UnixIOHelper"/>.
///         </item>
///         <item>
///             <b>Windows</b> - Uses <see cref="WindowsVTInputHelper"/> to enable Virtual Terminal Input mode.
///             This mode converts console input to ANSI escape sequences that can be read via
///             <c>ReadFile</c> API. Mouse events, keyboard input, etc. are all provided as VT sequences.
///         </item>
///     </list>
///     <para>
///         <b>Implementation Notes:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Windows</b>: Uses <c>ReadFile</c> API (via <see cref="WindowsVTInputHelper"/>) to read ANSI sequences
///         </item>
///         <item>
///             <b>Unix/Mac</b>: Uses <see cref="UnixIOHelper"/> for <c>poll()</c> and <c>read()</c> syscalls
///         </item>
///         <item>
///             Throttled by <see cref="InputImpl{TInputRecord}.Run"/> (20ms delay between polls)
///         </item>
///         <item>
///             Suitable for both production use and unit testing
///         </item>
///     </list>
///     <para>
///         <b>Architecture:</b>
///     </para>
///     <para>
///         Reads raw bytes from platform-specific APIs, converts them to UTF-8 characters,
///         and feeds them to <see cref="AnsiResponseParser{TInputRecord}"/> which extracts keyboard events,
///         mouse events (SGR format), and terminal responses.
///     </para>
/// </remarks>
public class AnsiInput : InputImpl<char>, ITestableInput<char>
{
    // Tracks which underlying platform APIs are in use
    private readonly AnsiPlatform _platform;

    // Platform-specific helpers
    private readonly UnixRawModeHelper? _unixRawMode;
    private readonly UnixIOHelper.Pollfd []? _pollMap;
    private readonly WindowsVTInputHelper? _windowsVTInput;

    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<char> _testInput = new ();

    private int _peekCallCount;

    /// <summary>
    ///     Gets the number of times <see cref="Peek"/> has been called.
    ///     This is useful for verifying that the input loop throttling is working correctly.
    /// </summary>
    internal int PeekCallCount => _peekCallCount;

    /// <summary>
    ///     Creates a new ANSIInput.
    /// </summary>
    public AnsiInput ()
    {
        _platform = AnsiPlatform.Degraded;

        try
        {
            // Check if we have a real console first
            if (!IsAttachedToTerminal)
            {
                Trace.Lifecycle (nameof (AnsiInput), "Init", "Console is not attached to a terminal. Running in degraded mode.");

                return;
            }

            // Initialize platform-specific input helpers
            if (OperatingSystem.IsWindows ())
            {
                _windowsVTInput = new WindowsVTInputHelper ();

                if (!_windowsVTInput.TryEnable ())
                {
                    _windowsVTInput.Dispose ();
                    _windowsVTInput = null;

                    Trace.Lifecycle (nameof (AnsiInput),
                                     "Init",
                                     "Failed to enable Windows VT Input mode. Terminal input will not work. Running in degraded mode.");

                    return;
                }
                _platform = AnsiPlatform.WindowsVT;
            }
            else
            {
                try
                {
                    // Unix/Mac: Set up poll map for non-blocking input checks using shared helper
                    _pollMap = UnixIOHelper.CreateStdinPollMap ();
                    _unixRawMode = new UnixRawModeHelper ();

                    if (!_unixRawMode.TryEnable ())
                    {
                        Trace.Lifecycle (nameof (AnsiInput),
                                         "Init",
                                         "Failed to enable Unix raw input mode. Terminal input will not work. Running in degraded mode.");
                        _pollMap = null;
                        _unixRawMode?.Dispose ();
                        _unixRawMode = null;

                        return;
                    }
                    _platform = AnsiPlatform.UnixRaw;
                }
                catch (DllNotFoundException ex)
                {
                    Trace.Lifecycle (nameof (AnsiInput),
                                     "Init",
                                     $"Failed to enable Unix raw input mode. libc not available: {ex.Message}. Running in degraded mode.");
                }
            }

            // NOTE: Output operations (alternate buffer, cursor visibility, mouse events)
            // NOTE: are handled by ANSIOutput, not here. ANSIInput only handles input.

            //Logging.Information ("ANSIInput initialized successfully");
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (AnsiInput),
                             "Init",
                             $"Failed to initialize terminal: {ex.GetType ().Name}: {ex.Message}. Running in degraded mode. Stack trace: {ex.StackTrace}");
            _platform = AnsiPlatform.Degraded;
        }
    }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        // Will be called on the input thread.
        Interlocked.Increment (ref _peekCallCount);

        // Check test input first - this allows immediate test input processing
        if (!_testInput.IsEmpty)
        {
            return true;
        }

        switch (_platform)
        {
            case AnsiPlatform.WindowsVT:
                return _windowsVTInput!.Peek ();

            case AnsiPlatform.UnixRaw:
                return _pollMap != null && UnixIOHelper.IsInputAvailable (_pollMap);

            case AnsiPlatform.Degraded:
            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<char> Read ()
    {
        // Will be called on the input thread.
        while (_testInput.TryDequeue (out char input))
        {
            yield return input;
        }

        var buffer = new byte [256];

        switch (_platform)
        {
            case AnsiPlatform.WindowsVT:
                if (!_windowsVTInput!.TryRead (buffer, out int bytesRead))
                {
                    yield break;
                }

                string text = _windowsVTInput!.ConsoleInputEncoding.GetString (buffer, 0, bytesRead);

                //Trace.Lifecycle (nameof (AnsiInput), "Read", $"Read {bytesRead} bytes from Windows VT Input: {text}");

                foreach (char ch in text)
                {
                    yield return ch;
                }

                break;

            case AnsiPlatform.UnixRaw:
                foreach (char c in ReadUnixInput (buffer))
                {
                    yield return c;
                }

                break;

            case AnsiPlatform.Degraded:
            default:
                yield break;
        }
    }

    private IEnumerable<char> ReadUnixInput (byte [] buffer)
    {
        // Poll again to ensure data is still available
        while (_pollMap is { } && UnixIOHelper.IsInputAvailable (_pollMap))
        {
            if ((_pollMap [0].revents & (short)UnixIOHelper.Condition.PollIn) == 0)
            {
                // No more data available
                yield break;
            }

            if (UnixIOHelper.TryReadStdin (buffer, out int readResult))
            {
                switch (readResult)
                {
                    case > 0:
                        {
                            // Convert UTF-8 bytes to characters
                            string text = Encoding.UTF8.GetString (buffer, 0, readResult);

                            foreach (char ch in text)
                            {
                                yield return ch;
                            }

                            break;
                        }

                    case 0:
                        // EOF
                        yield break;

                    default:
                        {
                            // Error
                            int errno = Marshal.GetLastWin32Error ();
                            Logging.Warning ($"{nameof (AnsiInput)}: read() returned {readResult}, errno={errno}");

                            yield break;
                        }
                }
            }
            else
            {
                Logging.Warning ($"{nameof (AnsiInput)}: read() failed");

                yield break;
            }
        }
    }

    /// <summary>
    ///     Flushes any pending input from the console buffer.
    ///     This prevents ANSI responses from leaking into the shell after the app exits.
    /// </summary>
    private void FlushInput ()
    {
        // Flush any pending input to prevent protocol sequences from leaking to the shell

        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    // Flush the Windows console input buffer to prevent Kitty keyboard
                    // key-release events from leaking into the shell after exit.
                    _windowsVTInput?.Flush ();

                    break;

                case AnsiPlatform.UnixRaw:
                    // On Unix, read with poll until no more data
                    if (_pollMap == null)
                    {
                        return;
                    }

                    var buffer = new byte [256];
                    var flushCount = 0;
                    const int MAX_FLUSH_ATTEMPTS = 10;

                    while (flushCount < MAX_FLUSH_ATTEMPTS)
                    {
                        try
                        {
                            // Check if data is available
                            if (UnixIOHelper.IsInputAvailable (_pollMap, 5))
                            {
                                if (UnixIOHelper.TryReadStdin (buffer, out int bytesRead))
                                {
                                    if (bytesRead <= 0)
                                    {
                                        break;
                                    }

                                    flushCount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break; // No more data
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }

                    if (flushCount > 0)
                    {
                        Trace.Lifecycle (nameof (AnsiInput), "FlushInput", $"Flushed input buffer ({flushCount} read attempts)");
                    }

                    break;

                case AnsiPlatform.Degraded:
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ($"{nameof (AnsiInput)}: Error flushing input: {ex.Message}");
        }
    }

    // Will be called on the main loop thread.
    /// <inheritdoc/>
    public void InjectInput (char input) => _testInput.Enqueue (input);

    /// <inheritdoc/>
    public override void Dispose ()
    {
        base.Dispose ();

        try
        {
            // This prevents ANSI responses (like size queries) from leaking into the shell
            FlushInput ();

            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    _windowsVTInput?.Dispose ();

                    break;

                case AnsiPlatform.UnixRaw:

                    // Restore platform-specific terminal settings
                    _unixRawMode?.Dispose ();

                    break;

                case AnsiPlatform.Degraded:
                default:

                    return;
            }
        }
        catch
        {
            // ignore exceptions during disposal
        }
    }
}
