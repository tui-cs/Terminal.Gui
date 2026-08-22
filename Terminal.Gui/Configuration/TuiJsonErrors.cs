// Grok - grok-4.6
using Terminal.Gui.App;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Collects JSON load errors from MEC sources so they can be printed at shutdown
///     instead of failing fast (the v2 contract).
/// </summary>
public static class TuiJsonErrors
{
    private static readonly Lock _lock = new ();
    private static readonly List<string> _errors = [];

    /// <summary>Adds a load-error message. Ignored if <paramref name="message"/> is empty.</summary>
    public static void Add (string message)
    {
        if (string.IsNullOrWhiteSpace (message))
        {
            return;
        }

        lock (_lock)
        {
            _errors.Add (message);
        }
    }

    /// <summary>Gets a snapshot of collected errors.</summary>
    public static IReadOnlyList<string> GetErrors ()
    {
        lock (_lock)
        {
            return [.._errors];
        }
    }

    /// <summary>Writes collected errors via <see cref="Logging"/> and clears the list.</summary>
    public static void Print ()
    {
        lock (_lock)
        {
            foreach (string error in _errors)
            {
                Logging.Warning (error);
            }

            _errors.Clear ();
        }
    }
}
