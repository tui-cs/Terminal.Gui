#nullable disable
using System.Diagnostics;

namespace Terminal.Gui.App;

/// <summary>Shared abstract class to enforce rules from the implementation of the <see cref="IClipboard"/> interface.</summary>
public abstract class ClipboardBase : IClipboard
{
    /// <summary>Returns true if the environmental dependencies are in place to interact with the OS clipboard</summary>
    public abstract bool IsSupported { get; }

    /// <summary>Returns the contents of the OS clipboard if possible.</summary>
    /// <returns>The contents of the OS clipboard if successful.</returns>
    /// <exception cref="NotSupportedException">Thrown if it was not possible to copy from the OS clipboard.</exception>
    public string GetClipboardData ()
    {
        try
        {
            string result = GetClipboardDataImpl ();

            if (result is null)
            {
                return string.Empty;
            }

            return result;
        }
        catch (NotSupportedException ex)
        {
            throw new NotSupportedException ("Failed to copy from the OS clipboard.", ex);
        }
    }

    /// <summary>Pastes the <paramref name="text"/> to the OS clipboard if possible.</summary>
    /// <param name="text">The text to paste to the OS clipboard.</param>
    /// <exception cref="NotSupportedException">Thrown if it was not possible to paste to the OS clipboard.</exception>
    public void SetClipboardData (string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException (nameof (text));
        }

        try
        {
            SetClipboardDataImpl (text);
        }
        catch (NotSupportedException ex)
        {
            throw new NotSupportedException ("Failed to paste to the OS clipboard.", ex);
        }
    }

    /// <summary>Copies the contents of the OS clipboard to <paramref name="result"/> if possible.</summary>
    /// <param name="result">The contents of the OS clipboard if successful.</param>
    /// <returns><see langword="true"/> the OS clipboard was retrieved, <see langword="false"/> otherwise.</returns>
    public bool TryGetClipboardData (out string result)
    {
        result = string.Empty;

        // Don't even try to read because environment is not set up.
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            result = GetClipboardDataImpl ();

            return true;
        }
        catch (NotSupportedException ex)
        {
            Debug.WriteLine ($"TryGetClipboardData: {ex.Message}");

            return false;
        }
    }

    /// <summary>Pastes the <paramref name="text"/> to the OS clipboard if possible.</summary>
    /// <param name="text">The text to paste to the OS clipboard.</param>
    /// <returns><see langword="true"/> the OS clipboard was set, <see langword="false"/> otherwise.</returns>
    public bool TrySetClipboardData (string text)
    {
        // Don't even try to set because environment is not set up
        if (!IsSupported)
        {
            return false;
        }

        try
        {
            SetClipboardDataImpl (text);

            return true;
        }
        catch (NotSupportedException ex)
        {
            Debug.WriteLine ($"TrySetClipboardData: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Returns the contents of the OS clipboard if possible. Implemented by <see cref="IDriver"/>-specific
    ///     subclasses.
    /// </summary>
    /// <returns>The contents of the OS clipboard if successful.</returns>
    /// <exception cref="NotSupportedException">Thrown if it was not possible to copy from the OS clipboard.</exception>
    protected abstract string GetClipboardDataImpl ();

    /// <summary>
    ///     Pastes the <paramref name="text"/> to the OS clipboard if possible. Implemented by <see cref="IDriver"/>
    ///     -specific subclasses.
    /// </summary>
    /// <param name="text">The text to paste to the OS clipboard.</param>
    /// <exception cref="NotSupportedException">Thrown if it was not possible to paste to the OS clipboard.</exception>
    protected abstract void SetClipboardDataImpl (string text);
}
