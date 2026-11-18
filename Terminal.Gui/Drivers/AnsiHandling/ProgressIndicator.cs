namespace Terminal.Gui.Drivers;

/// <summary>
///     Writes terminal progress sequences for hosts that support OSC 9;4 style progress indicators.
/// </summary>
public class ProgressIndicator
{
    private readonly DriverImpl _driver;
    private string? _lastSequence;

    internal ProgressIndicator (DriverImpl driver)
    {
        ArgumentNullException.ThrowIfNull (driver);
        _driver = driver;
    }

    /// <summary>
    ///     Gets whether terminal progress escape sequences should be written for the current output stream.
    /// </summary>
    /// <param name="outputAttached"><see langword="true"/> if standard output is attached to a terminal device.</param>
    /// <param name="outputRedirected"><see langword="true"/> if standard output is redirected away from the terminal.</param>
    /// <param name="term">The <c>TERM</c> environment variable value for the current host, if any.</param>
    /// <returns>
    ///     <see langword="true"/> when output is attached, not redirected, and the host is not marked as
    ///     <c>dumb</c>; otherwise, <see langword="false"/>.
    /// </returns>
    internal static bool IsSupportedOutput (bool outputAttached, bool outputRedirected, string? term)
    {
        if (!outputAttached || outputRedirected)
        {
            return false;
        }

        return !string.Equals (term, "dumb", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Clears any terminal progress indicator state previously written by this instance.
    /// </summary>
    public void Clear ()
    {
        if (_lastSequence is null)
        {
            return;
        }

        // Keep the last sequence if the clear fails so a later Clear retries it.
        if (!WriteSequence (EscSeqUtils.OSC_ClearProgress ()))
        {
            return;
        }

        _lastSequence = null;
    }

    /// <summary>
    ///     Sets determinate progress using a percentage from 0 to 100.
    /// </summary>
    /// <param name="progress">Progress percentage.</param>
    public void SetValue (int progress) => WriteSequence (EscSeqUtils.OSC_SetProgressValue (progress));

    /// <summary>
    ///     Sets an error progress state using a percentage from 0 to 100.
    /// </summary>
    /// <param name="progress">Progress percentage.</param>
    public void SetError (int progress = 0) => WriteSequence (EscSeqUtils.OSC_SetProgressError (progress));

    /// <summary>
    ///     Sets a paused progress state using a percentage from 0 to 100.
    /// </summary>
    /// <param name="progress">Progress percentage.</param>
    public void SetPaused (int progress = 0) => WriteSequence (EscSeqUtils.OSC_SetProgressPaused (progress));

    /// <summary>
    ///     Sets indeterminate terminal progress.
    /// </summary>
    public void SetIndeterminate () => WriteSequence (EscSeqUtils.OSC_SetProgressIndeterminate ());

    private bool WriteSequence (string sequence)
    {
        if (_lastSequence == sequence || _driver.IsLegacyConsole)
        {
            return true;
        }

        // Only remember sequences the terminal actually received; otherwise a repeat would be skipped.
        if (!_driver.TryWriteRaw (sequence))
        {
            return false;
        }

        _lastSequence = sequence;

        return true;
    }
}
