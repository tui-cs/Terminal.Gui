namespace Terminal.Gui.Tracing;

/// <summary>
///     A backend that forwards trace entries to <see cref="Logging.Trace"/>.
/// </summary>
public sealed class LoggingBackend : ITraceBackend
{
    /// <inheritdoc/>
    public void Log (TraceEntry entry)
    {
        string prefix = entry.Category switch
        {
            TraceCategory.Lifecycle => FormatLifecycle (entry),
            TraceCategory.Command => FormatCommand (entry),
            TraceCategory.Mouse => FormatMouse (entry),
            TraceCategory.Keyboard => FormatKeyboard (entry),
            TraceCategory.Navigation => FormatNavigation (entry),
            TraceCategory.Configuration => string.Empty,
            _ => $"[{entry.Category}]"
        };

        var message = $"{prefix}@\"{entry.Id}\"";

        if (!string.IsNullOrEmpty (entry.Message))
        {
            message += $" - {entry.Message}";
        }

        if (entry.Data is not null)
        {
            message += $" - {entry.Data}";
        }

        // ReSharper disable once ExplicitCallerInfoArgument
        Logging.Trace (message, entry.Method, $"{entry.Category}:{entry.Phase}");
    }

    private string FormatNavigation (TraceEntry _) => string.Empty;

    private string FormatLifecycle (TraceEntry _) => string.Empty;

    private static string FormatCommand (TraceEntry entry)
    {
        if (entry.Data is not (Command cmd, CommandRouting routing))
        {
            return string.Empty;
        }

        string arrow = routing switch
        {
            CommandRouting.BubblingUp => "↑",
            CommandRouting.DispatchingDown => "↓",
            CommandRouting.Bridged => "↔",
            _ => "•"
        };

        return $"{arrow} {cmd}";
    }

    private static string FormatMouse (TraceEntry entry)
    {
        switch (entry.Data)
        {
            case (MouseFlags flags, Point pos): return $"{flags} @({pos.X},{pos.Y})";

            case Mouse mouse:
                {
                    Point mousePos = mouse.Position ?? Point.Empty;

                    return $"{mouse.Flags} @({mousePos.X},{mousePos.Y})";
                }

            default: return string.Empty;
        }
    }

    private static string FormatKeyboard (TraceEntry entry)
    {
        if (entry.Data is Key key)
        {
            return $"{key}";
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public void Clear () { }
}
