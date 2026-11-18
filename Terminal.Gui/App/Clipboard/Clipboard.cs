namespace Terminal.Gui.App;

/// <summary>Provides cut, copy, and paste support for the OS clipboard.</summary>
/// <remarks>
///     <para>
///         <b>DEPRECATED:</b> This static class is obsolete. Use <see cref="IApplication.Clipboard"/> instead.
///     </para>
///     <para>On Windows, the <see cref="Clipboard"/> class uses the Windows Clipboard APIs via P/Invoke.</para>
///     <para>
///         On Linux, when not running under Windows Subsystem for Linux (WSL), the <see cref="Clipboard"/> class uses
///         the xclip command line tool. If xclip is not installed, the clipboard will not work.
///     </para>
///     <para>
///         On Linux, when running under Windows Subsystem for Linux (WSL), the <see cref="Clipboard"/> class launches
///         Windows' powershell.exe via WSL interop and uses the "Set-Clipboard" and "Get-Clipboard" Powershell CmdLets.
///     </para>
///     <para>
///         On the Mac, the <see cref="Clipboard"/> class uses the MacO OS X pbcopy and pbpaste command line tools and
///         the Mac clipboard APIs vai P/Invoke.
///     </para>
/// </remarks>
[Obsolete ("Use IApplication.Clipboard instead. The static Clipboard class will be removed in a future release.")]
public static class Clipboard
{
    private static string? _contents = string.Empty;

    /// <summary>Gets (copies from) or sets (pastes to) the contents of the OS clipboard.</summary>
    public static string? Contents
    {
        get
        {
            try
            {
                if (IsSupported)
                {
                    // throw new InvalidOperationException ($"{Application.Driver?.GetType ().Name}.GetClipboardData returned null instead of string.Empty");
                    string clipData = Application.Driver?.Clipboard?.GetClipboardData () ?? string.Empty;

                    _contents = clipData;
                }
            }
            catch (Exception)
            {
                _contents = string.Empty;
            }

            return _contents;
        }
        set
        {
            try
            {
                if (IsSupported)
                {
                    value ??= string.Empty;

                    Application.Driver?.Clipboard?.SetClipboardData (value);
                }

                _contents = value;
            }
            catch (Exception)
            {
                _contents = value;
            }
        }
    }

    /// <summary>Returns true if the environmental dependencies are in place to interact with the OS clipboard.</summary>
    /// <remarks></remarks>
    public static bool IsSupported => Application.Driver?.Clipboard?.IsSupported ?? false;

    /// <summary>Gets the OS clipboard data if possible.</summary>
    /// <param name="result">The clipboard data if successful.</param>
    /// <returns><see langword="true"/> if the clipboard data was retrieved successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetClipboardData (out string result)
    {
        result = string.Empty;

        if (IsSupported && Application.Driver?.Clipboard is { })
        {
            return Application.Driver.Clipboard.TryGetClipboardData (out result);
        }

        return false;
    }

    /// <summary>Sets the OS clipboard data if possible.</summary>
    /// <param name="text">The text to set.</param>
    /// <returns><see langword="true"/> if the clipboard data was set successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TrySetClipboardData (string text)
    {
        if (IsSupported && Application.Driver?.Clipboard is { })
        {
            return Application.Driver.Clipboard.TrySetClipboardData (text);
        }

        return false;
    }
}
