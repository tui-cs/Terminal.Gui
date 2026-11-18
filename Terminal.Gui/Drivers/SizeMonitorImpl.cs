using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <inheritdoc/>
internal class SizeMonitorImpl : ISizeMonitor
{
    private readonly IOutput _consoleOut;
    private Size _lastSize;

    /// <summary>
    ///     Creates a new <see cref="SizeMonitorImpl"/> that polls <paramref name="consoleOut"/> for size changes.
    ///     The initial size is captured from <paramref name="consoleOut"/> at construction time so that the
    ///     first <see cref="Poll"/> call only fires <see cref="SizeChanged"/> when the size has actually changed.
    /// </summary>
    public SizeMonitorImpl (IOutput consoleOut)
    {
        _consoleOut = consoleOut;

        // Capture the current size so the first Poll() is a no-op when the size has not changed.
        _lastSize = consoleOut.GetSize ();
    }

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc/>
    public bool Poll ()
    {
        Size size = _consoleOut.GetSize ();

        if (size == _lastSize)
        {
            return false;
        }

        Trace.Lifecycle (nameof (SizeMonitorImpl), "Poll", $"{_lastSize} → {size}");
        _lastSize = size;
        SizeChanged?.Invoke (this, new SizeChangedEventArgs (size));

        return true;

    }
}
