using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for the pure ANSI Driver.
/// </summary>
/// <remarks>
///     <para>
///         The ANSI driver demonstrates proper use of <see cref="AnsiResponseParser"/> for
///         querying terminal capabilities via ANSI escape sequences. It showcases:
///     </para>
///     <list type="bullet">
///         <item>Sending ANSI queries (e.g., <see cref="EscSeqUtils.CSI_ReportWindowSizeInChars"/>)</item>
///         <item>Registering response expectations with <see cref="AnsiResponseParser"/></item>
///         <item>Handling responses asynchronously through callbacks</item>
///         <item>Coordinating between input (response parsing) and output (query sending)</item>
///     </list>
/// </remarks>
public class AnsiComponentFactory : ComponentFactoryImpl<char>
{
    /// <inheritdoc/>
    public override string? GetDriverName () => DriverRegistry.Names.ANSI;

    private readonly AnsiInput? _input;
    private readonly IOutput? _output;
    private readonly ISizeMonitor? _injectedSizeMonitor;
    private readonly Func<Size?>? _nativeSizeQuery;

    /// <summary>
    ///     Creates a new ANSIComponentFactory with optional output capture.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output">Optional fake output to capture what would be written to console.</param>
    /// <param name="sizeMonitor">
    ///     Optional size monitor override (used in tests; if <see langword="null"/>, the monitor is chosen based on
    ///     <see cref="Driver.SizeDetection"/>).
    /// </param>
    public AnsiComponentFactory (AnsiInput? input = null, IOutput? output = null, ISizeMonitor? sizeMonitor = null)
        : this (input, output, sizeMonitor, null)
    { }

    internal AnsiComponentFactory (AnsiInput? input, IOutput? output, ISizeMonitor? sizeMonitor, Func<Size?>? nativeSizeQuery)
    {
        _input = input;
        _output = output;
        _injectedSizeMonitor = sizeMonitor;
        _nativeSizeQuery = nativeSizeQuery;
    }

    /// <inheritdoc/>
    public override ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        // Return injected monitor (e.g. from test harness) if one was provided.
        if (_injectedSizeMonitor is { })
        {
            return _injectedSizeMonitor;
        }

        if (consoleOutput is not AnsiOutput ansiOutput)
        {
            return new SizeMonitorImpl (consoleOutput);
        }

        if (Driver.SizeDetection != SizeDetectionMode.Polling)
        {
            ansiOutput.NativeSizeQuery = null;

            return new AnsiSizeMonitor (ansiOutput);
        }

        if (ansiOutput.NativeSizeQuery is null && !TryConfigureNativeSizeQuery (ansiOutput))
        {
            return new AnsiSizeMonitor (ansiOutput);
        }

        return new AnsiSizeMonitor (ansiOutput, queryTerminalSize: false);
    }

    private bool TryConfigureNativeSizeQuery (AnsiOutput output)
    {
        Func<Size?> nativeSizeQuery = output.NativeSizeQuery ?? _nativeSizeQuery ?? CreateNativeSizeQuery ();
        Size? initialSize = nativeSizeQuery ();

        if (initialSize is null)
        {
            return false;
        }

        output.NativeSizeQuery = nativeSizeQuery;
        output.SetSize (initialSize.Value.Width, initialSize.Value.Height);

        return true;
    }

    /// <summary>
    ///     Returns a delegate that queries the real terminal size from the OS.
    ///     On Windows this uses <see cref="Console.WindowWidth"/> / <see cref="Console.WindowHeight"/>;
    ///     on Unix/macOS it uses <c>ioctl(TIOCGWINSZ)</c> via <see cref="UnixIOHelper.TryGetTerminalSize"/>.
    /// </summary>
    internal static Func<Size?> CreateNativeSizeQuery ()
    {
        if (OperatingSystem.IsWindows ())
        {
            return () =>
                   {
                       try
                       {
                           int w = Console.WindowWidth;
                           int h = Console.WindowHeight;

                           return w > 0 && h > 0 ? new Size (w, h) : null;
                       }
                       catch (Exception ex)
                       {
                           Trace.Lifecycle (nameof (AnsiComponentFactory), "NativeSizeQuery", $"Console size query failed: {ex.GetType ().Name}: {ex.Message}");

                           return null;
                       }
                   };
        }

        return () => UnixIOHelper.TryGetTerminalSize (out Size s) ? s : null;
    }

    /// <inheritdoc/>
    public override IInput<char> CreateInput () => _input ?? new AnsiInput ();

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<char> inputBuffer, ITimeProvider? timeProvider = null) =>
        new AnsiInputProcessor (inputBuffer, timeProvider);

    /// <inheritdoc/>
    public override IOutput CreateOutput ()
    {
        IOutput output = _output ?? new AnsiOutput (AppModel);

        if (Driver.SizeDetection == SizeDetectionMode.Polling
            && output is AnsiOutput { NativeSizeQuery: null } ansiOutput)
        {
            TryConfigureNativeSizeQuery (ansiOutput);
        }

        return output;
    }
}
