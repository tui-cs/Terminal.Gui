using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IOutput"/> that uses native dotnet
///     methods e.g. <see cref="System.Console"/>
/// </summary>
public class NetOutput : OutputBase, IOutput
{
    private readonly bool _isWinPlatform;
    private char []? _utf16DecodeBuffer;

    /// <summary>
    ///     Creates a new instance of the <see cref="NetOutput"/> class.
    /// </summary>
    public NetOutput ()
    {
        // Logging.Information ($"Creating {nameof (NetOutput)}");

        if (!IsAttachedToTerminal)
        {
            Trace.Lifecycle (nameof (NetOutput), "Init", "No real terminal attached. Output operations will be no-op.");

            return;
        }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch
        {
            // ignore for unit tests
        }

        PlatformID p = Environment.OSVersion.Platform;

        if (p is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows)
        {
            _isWinPlatform = true;
        }
    }

    /// <inheritdoc/>
    public Size GetSize ()
    {
        if (!IsAttachedToTerminal)
        {
            return new Size (80, 25);
        }

        try
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return new Size (80, 25);
            }
            Size size = new (Console.WindowWidth, Console.WindowHeight);

            return size.IsEmpty ? new Size (80, 25) : size;
        }
        catch (IOException)
        {
            // Not connected to a terminal; return a default size
            return new Size (80, 25);
        }
    }

    /// <inheritdoc/>
    public void SetSize (int width, int height)
    {
        // Do Nothing.
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        if (!IsAttachedToTerminal)
        {
            return;
        }

        Console.Out.Write (text);
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        base.Write (output);

        if (!IsAttachedToTerminal)
        {
            return;
        }

        Console.Out.Write (output);
    }

    /// <inheritdoc/>
    private protected override void WriteEncodedString (string output)
    {
        if (HasCustomStringWriter (typeof (NetOutput)))
        {
            WriteLegacyString (new StringBuilder (output));
            return;
        }

        CaptureText (output.AsSpan ());

        if (!IsAttachedToTerminal)
        {
            return;
        }

        Console.Out.Write (output.AsSpan ());
    }

    /// <inheritdoc/>
    protected override void Write (ReadOnlySpan<byte> output)
    {
        if (HasCustomStringWriter (typeof (NetOutput)))
        {
            base.Write (output);
            return;
        }

        if (output.IsEmpty)
        {
            return;
        }

        int maxCharCount = Encoding.UTF8.GetMaxCharCount (output.Length);
        char [] decoded;

        if (maxCharCount > Utf8Buffer.RetainedCapacityLimit)
        {
            decoded = new char [maxCharCount];
        }
        else
        {
            if (_utf16DecodeBuffer is null || _utf16DecodeBuffer.Length < maxCharCount)
            {
                _utf16DecodeBuffer = new char [maxCharCount];
            }

            decoded = _utf16DecodeBuffer;
        }

        int charCount = Encoding.UTF8.GetChars (output, decoded);
        ReadOnlySpan<char> text = decoded.AsSpan (0, charCount);
        CaptureText (text);

        if (!IsAttachedToTerminal)
        {
            return;
        }

        Console.Out.Write (text);
    }

    private Cursor _currentCursor = new ();

    /// <inheritdoc/>
    public Cursor GetCursor () => _currentCursor;

    /// <inheritdoc/>
    public void SetCursor (Cursor cursor)
    {
        try
        {
            if (!cursor.IsVisible)
            {
                Write (EscSeqUtils.CSI_HideCursor);
            }
            else
            {
                if (_currentCursor.Style != cursor.Style)
                {
                    Write (EscSeqUtils.CSI_SetCursorStyle (cursor.Style));
                }

                Write (EscSeqUtils.CSI_ShowCursor);
            }

            SetCursorPositionImpl (cursor.Position?.X ?? 0, cursor.Position?.Y ?? 0);
            _currentCursor = cursor;
        }
        catch (IOException ex)
        {
            // Best effort: a broken sink also fails the next frame, which DriverImpl logs once.
            Logging.Debug ($"Error updating .NET console cursor: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int col, int row)
    {
        if (_isWinPlatform)
        {
            try
            {
                Console.SetCursorPosition (col, row);
            }
            catch
            {
                // Could happen that the windows is still resizing and the col is bigger than Console.WindowWidth.
            }

            return true;
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        EscSeqUtils.CSI_WriteCursorPosition (Console.Out, row + 1, col + 1);

        return true;
    }

    /// <inheritdoc/>
    public void Dispose () { }

    /// <inheritdoc/>
    public void Suspend ()
    {
        if (OperatingSystem.IsWindows () && !IsAttachedToTerminal)
        {
            return;
        }

        // Best-effort: mirror behavior of ANSI/Unix outputs for consoles that accept CSI sequences.
        try
        {
            // Disable bracketed paste and mouse events while suspended.
            Write (EscSeqUtils.CSI_DisableBracketedPaste);
            Write (EscSeqUtils.CSI_DisableMouseEvents);

            // Check if we have a real console first
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Logging.Information ($"Console redirected (Output: {Console.IsOutputRedirected}, Input: {Console.IsInputRedirected}). Running in degraded mode.");

                return;
            }

            Console.ResetColor ();
            Console.Clear ();

            //Disable alternative screen buffer.
            Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            Write (EscSeqUtils.CSI_ShowCursor);

            if (!SuspendHelper.Suspend ())
            {
                return;
            }

            //Enable alternative screen buffer.
            Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
        }
        catch (Exception ex)
        {
            Logging.Error ($"Error suspending terminal: {ex.Message}");
        }
        finally
        {
            // Re-enable mouse events and bracketed paste after resume.
            Write (EscSeqUtils.CSI_EnableMouseEvents);
            Write (EscSeqUtils.CSI_EnableBracketedPaste);
        }
    }
}
