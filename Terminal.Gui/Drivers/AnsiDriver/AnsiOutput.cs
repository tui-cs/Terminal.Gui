using System.Text.RegularExpressions;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Pure ANSI console output.
///     </para>
///     <para>
///         <b>ANSI Output Architecture:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Pure ANSI</b> - All output operations use ANSI escape sequences via <see cref="EscSeqUtils"/>,
///             making it portable across ANSI-compatible terminals (Unix, Windows Terminal, ConEmu, etc.).
///         </item>
///         <item>
///             <b>Buffer Capture</b> - <see cref="GetLastBuffer"/> provides access to the last written
///             <see cref="IOutputBuffer"/> for test verification, independent of actual console output.
///         </item>
///         <item>
///             <b>Graceful Degradation</b> - Detects if console is unavailable or redirected, silently
///             operating in buffer-only mode for CI/headless environments.
///         </item>
///         <item>
///             <b>Size Management</b> - Uses <see cref="SetSize"/> for controlling terminal dimensions
///             in tests. In real terminals, size would be queried via ANSI requests
///             (see <see cref="EscSeqUtils.CSI_ReportWindowSizeInChars"/>) or platform APIs.
///         </item>
///     </list>
///     <para>
///         <b>Color Support:</b> Supports both 16-color (via <see cref="OutputBase.Force16Colors"/>)
///         and true-color (24-bit RGB) output through ANSI SGR sequences.
///     </para>
/// </summary>
public class AnsiOutput : OutputBase, IOutput
{
    internal delegate bool UnixWriter (ReadOnlySpan<byte> output);

    // Tracks which underlying platform APIs are in use
    private readonly AnsiPlatform _platform;
    private readonly UnixWriter? _unixWriter;

    private Size _consoleSize;
    private IOutputBuffer? _lastBuffer;

    private readonly WindowsVTOutputHelper? _windowsVTOutput;

    /// <summary>
    ///     Gets or sets the <see cref="AppModel"/> that this output was initialized with.
    /// </summary>
    internal AppModel AppModel { get; }

    /// <summary>
    ///     Gets or sets a callback that returns the current application <see cref="IApplication.Screen"/>
    ///     rectangle. In inline mode, <see cref="Rectangle.Y"/> is the terminal row offset
    ///     and <see cref="Rectangle.Height"/> is the inline region height.
    ///     Set after the application is fully constructed.
    /// </summary>
    internal Func<Rectangle>? AppScreenGetter { get; set; }

    /// <summary>
    ///     Initializes a new instance of <see cref="AnsiOutput"/>.
    ///     Checks if a real console is available for ANSI output and activates the alternate screen buffer.
    /// </summary>
    /// <param name="appModel">
    ///     The rendering mode. When <see cref="AppModel.Inline"/>, the alternate screen buffer is not activated
    ///     and the terminal's primary (scrollback) buffer is used instead.
    /// </param>
    public AnsiOutput (AppModel appModel = AppModel.FullScreen)
    {
        AppModel = appModel;
        _platform = AnsiPlatform.Degraded;

        _lastBuffer = new OutputBufferImpl ();
        _currentCursor = new Cursor ();

        try
        {
            // Check if we have a real console first
            if (!IsAttachedToTerminal)
            {
                _consoleSize = new Size (80, 25);
                _lastBuffer.SetSize (_consoleSize.Width, _consoleSize.Height);

                Trace.Lifecycle (nameof (AnsiOutput), "Init", "No real terminal attached. Running in degraded mode.");

                return;
            }

            // Initialize platform-specific output helpers
            if (OperatingSystem.IsWindows ())
            {
                // Ensure the console output code page is UTF-8 (65001). The ANSI driver writes
                // UTF-8 encoded bytes via WriteFile; without this, a fresh Windows terminal uses
                // its default OEM code page (e.g. 437), causing multi-byte UTF-8 characters
                // (box-drawing glyphs, etc.) to be misinterpreted as garbled single-byte characters.
                // See https://github.com/tui-cs/Terminal.Gui/issues/4848
                Console.OutputEncoding = Encoding.UTF8;

                _windowsVTOutput = new WindowsVTOutputHelper ();

                if (!_windowsVTOutput.TryEnable ())
                {
                    _windowsVTOutput.Dispose ();
                    _windowsVTOutput = null;

                    Trace.Lifecycle (nameof (AnsiOutput),
                                     "Init",
                                     "Failed to enable Windows VT Input mode. Terminal input will not work. Running in degraded mode.");

                    return;
                }
                _platform = AnsiPlatform.WindowsVT;
            }
            else
            {
                // TerminalDevice owns the controlling terminal descriptor. This output neither
                // duplicates nor closes it; redirected stdout still renders through /dev/tty.
                if (TerminalDevice.OutputFd < 0)
                {
                    Trace.Lifecycle (nameof (AnsiOutput), "Init", "Console output stream is not writable. Running in degraded mode.");
                    return;
                }

                _platform = AnsiPlatform.UnixRaw;
            }

            // Initialize terminal for ANSI output
            if (AppModel == AppModel.Inline)
            {
                // Inline mode: do NOT switch to alternate screen buffer.
                // Stay in the primary (scrollback) buffer.
                Write (EscSeqUtils.CSI_HideCursor);

                // TODO: Move Input related CSI sequences to AnsiInput
                Write (EscSeqUtils.CSI_EnableMouseEvents);
                Write (EscSeqUtils.CSI_EnableBracketedPaste);
            }
            else
            {
                // FullScreen mode: activate alternate screen buffer, hide cursor, enable mouse tracking
                Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
                Write (EscSeqUtils.CSI_ClearScreen (EscSeqUtils.ClearScreenOptions.EntireScreen));
                Write (EscSeqUtils.CSI_SetCursorPosition (1, 1)); // Move to top-left
                Write (EscSeqUtils.CSI_HideCursor);

                // TODO: Move Input related CSI sequences to AnsiInput
                Write (EscSeqUtils.CSI_EnableMouseEvents);
                Write (EscSeqUtils.CSI_EnableBracketedPaste);
            }

            // Flush to ensure all sequences are sent
            AnsiTerminalHelper.FlushNative (_platform);
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (AnsiOutput), "Init", $"Failed to initialize ANSIOutput: {ex.GetType ().Name}: {ex.Message}. Stack trace: {ex.StackTrace}");
            // Initialization may already have switched buffers or enabled input modes.
            // Attempt all restores while the native sink is still available.
            Dispose ();
            _platform = AnsiPlatform.Degraded;
        }
    }

    // Exercises UnixRaw framing without opening a terminal or changing process-wide stdout.
    internal AnsiOutput (UnixWriter unixWriter, AppModel appModel = AppModel.FullScreen)
    {
        AppModel = appModel;
        _platform = AnsiPlatform.UnixRaw;
        _unixWriter = unixWriter;
        _lastBuffer = new OutputBufferImpl ();
        _currentCursor = new Cursor ();
        _consoleSize = new Size (80, 25);
        _lastBuffer.SetSize (_consoleSize.Width, _consoleSize.Height);
    }

    /// <inheritdoc/>
    public void Suspend () => UnixTerminalHelper.Suspend (this);

    /// <summary>
    ///     Gets or sets the last output buffer written. The <see cref="IOutputBuffer.Contents"/> contains
    ///     a reference to the buffer last written with <see cref="Write(IOutputBuffer)"/>.
    /// </summary>
    public IOutputBuffer? GetLastBuffer () => _lastBuffer;

    /// <inheritdoc/>
    public void SetSize (int width, int height) => _consoleSize = new Size (width, height);

    /// <summary>
    ///     When non-<see langword="null"/>, <see cref="GetSize"/> calls this delegate to obtain the
    ///     real terminal size directly from the OS (e.g. <c>ioctl(TIOCGWINSZ)</c> on Unix or the
    ///     Console API on Windows). Set by <see cref="AnsiComponentFactory"/> when
    ///     <see cref="Driver.SizeDetection"/> is <see cref="SizeDetectionMode.Polling"/>.
    /// </summary>
    internal Func<Size?>? NativeSizeQuery { get; set; }

    /// <inheritdoc/>
    public Size GetSize ()
    {
        if (NativeSizeQuery is null)
        {
            return _consoleSize;
        }
        Size? native = NativeSizeQuery ();

        if (native is { })
        {
            _consoleSize = native.Value;
        }

        return _consoleSize;
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        if (_platform == AnsiPlatform.Degraded && !_batchMode)
        {
            base.Write (output);
            return;
        }

        WriteUtf8 (Encoding.UTF8.GetBytes (output.ToString ()));
    }

    /// <inheritdoc/>
    private protected override void WriteEncodedString (string output)
    {
        if (HasCustomStringWriter (typeof (AnsiOutput)))
        {
            WriteLegacyString (new StringBuilder (output));
            return;
        }

        Write (output.AsSpan ());
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        if (_platform == AnsiPlatform.Degraded && !_batchMode)
        {
            CaptureText (text);
            return;
        }

        // Encode bounded chunks into reusable storage. Never divide a surrogate pair:
        // CaptureUtf8 and downstream sinks may decode each physical chunk independently.
        while (!text.IsEmpty)
        {
            int count = Math.Min (text.Length, 16 * 1024);

            if (count < text.Length && char.IsHighSurrogate (text [count - 1]) && char.IsLowSurrogate (text [count]))
            {
                count--;
            }

            _encodedText.Clear ();
            _encodedText.Append (text [..count]);
            Write (_encodedText.AsSpan ());
            text = text [count..];
        }
    }

    /// <inheritdoc cref="IOutput.Write(IOutputBuffer)"/>
    public override void Write (IOutputBuffer buffer)
    {
        _lastBuffer = buffer;
        // An out-of-band legacy attribute hook must take effect between physical writes.
        _batchMode = !UsesLegacyAttributeWriter && !HasCustomStringWriter (typeof (AnsiOutput));
        _frameBuffer.Clear ();

        try
        {
            base.Write (buffer);
        }
        finally
        {
            _batchMode = false;
            _pendingCursorMoves.Clear ();
            _pendingCursorMoves.TrimExcess ();
            _frameBuffer.Clear ();
            _frameBuffer.TrimExcess ();
        }
    }

    internal override void FlushFrame ()
    {
        if (_pendingCursorMoves.Length > 0)
        {
            _frameBuffer.Append (_pendingCursorMoves);
            _pendingCursorMoves.Clear ();
        }

        _batchMode = false;

        if (_frameBuffer.Length > 0)
        {
            Write (_frameBuffer.AsSpan ());
        }
    }

    /// <summary>
    ///     PERF: Write pre-encoded UTF-8 bytes directly to the terminal.
    ///     In batch mode, merges deferred cursor moves with cell content into one WriteFile.
    ///     This is the hot-path write — zero string/byte[] allocation, zero encoding.
    /// </summary>
    protected override void Write (ReadOnlySpan<byte> output)
    {
        if (HasCustomStringWriter (typeof (AnsiOutput)))
        {
            base.Write (output);
            return;
        }

        WriteUtf8 (output);
    }

    private void WriteUtf8 (ReadOnlySpan<byte> output)
    {
        try
        {
            if (_batchMode)
            {
                if (_frameBuffer.Length + (long)_pendingCursorMoves.Length > Utf8Buffer.RetainedCapacityLimit)
                {
                    WriteDirect (_frameBuffer.AsSpan ());
                    _frameBuffer.Clear ();
                }

                if (_pendingCursorMoves.Length > 0)
                {
                    _frameBuffer.Append (_pendingCursorMoves);
                    _pendingCursorMoves.Clear ();
                }

                // Bound cumulative staging too: many small images can exceed the limit.
                if (_frameBuffer.Length > Utf8Buffer.RetainedCapacityLimit - Math.Min (output.Length, Utf8Buffer.RetainedCapacityLimit))
                {
                    WriteDirect (_frameBuffer.AsSpan ());
                    _frameBuffer.Clear ();
                }

                // Large images bypass staging after all preceding content.
                if (output.Length > Utf8Buffer.RetainedCapacityLimit)
                {
                    if (_frameBuffer.Length > 0)
                    {
                        WriteDirect (_frameBuffer.AsSpan ());
                        _frameBuffer.Clear ();
                    }

                    WriteDirect (output);
                    return;
                }

                _frameBuffer.AppendBytes (output);
                return;
            }

            WriteDirect (output);
        }
        catch (IOException)
        {
            // DriverImpl owns failure logging and retry; only drop moves that no longer apply.
            _pendingCursorMoves.Clear ();

            throw;
        }
    }

    private void WriteDirect (ReadOnlySpan<byte> output)
    {
        switch (_platform)
        {
            case AnsiPlatform.WindowsVT:
                _windowsVTOutput!.Write (output);
                break;

            case AnsiPlatform.UnixRaw:
                if (!(_unixWriter?.Invoke (output) ?? UnixIOHelper.TryWriteStdout (output)))
                {
                    throw new IOException ("Failed to write ANSI output to the terminal.");
                }
                break;
        }

        CaptureUtf8 (output);
    }

    private Cursor _currentCursor;

    // PERF: Batch mode — defer cursor-move sequences to merge with cell content, reducing WriteFile P/Invoke count
    private readonly Utf8Buffer _pendingCursorMoves = new ();
    private readonly Utf8Buffer _frameBuffer = new ();
    private readonly Utf8Buffer _encodedText = new ();
    private bool _batchMode;

    internal int RetainedWriteBufferBytes => _pendingCursorMoves.Capacity + _frameBuffer.Capacity + _encodedText.Capacity;

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
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
        {
            // Best effort: a broken sink also fails the next frame, which DriverImpl logs once.
            Logging.Debug ($"Error updating ANSI cursor: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int col, int row)
    {
        if (_platform == AnsiPlatform.Degraded)
        {
            return true;
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        // In inline mode, App.Screen.Y is the terminal row where the inline region starts.
        // Adding it shifts rendering down so buffer row 0 maps to the correct terminal row.
        int inlineRowOffset = AppScreenGetter?.Invoke ().Y ?? 0;

        if (_batchMode)
        {
            // PERF: Defer cursor-move to _pendingCursorMoves (Utf8Buffer); merges with subsequent cell content
            // into a single WriteFile P/Invoke instead of one per cursor move.
            _pendingCursorMoves.Clear ();
            _pendingCursorMoves.AppendAscii (EscSeqUtils.CSI_SetCursorPosition (row + 1 + inlineRowOffset, col + 1));
        }
        else
        {
            Write (EscSeqUtils.CSI_SetCursorPosition (row + 1 + inlineRowOffset, col + 1));
        }

        return true;
    }

    /// <summary>
    ///     Handles ANSI size query responses.
    ///     Expected format: ESC [ 8 ; height ; width t
    /// </summary>
    /// <param name="response">The ANSI response string</param>
    public void HandleSizeQueryResponse (string? response)
    {
        if (string.IsNullOrEmpty (response))
        {
            return;
        }

        try
        {
            // Parse response: ESC [ 8 ; height ; width t
            // Example: "[8;25;80t"
            Match match = Regex.Match (response, @"\[(\d+);(\d+);(\d+)t$");

            if (match is not { Success: true, Groups.Count: 4 })
            {
                return;
            }

            // Group 1 should be "8" (the response value)
            // Group 2 is height, Group 3 is width
            if (int.TryParse (match.Groups [2].Value, out int height) && int.TryParse (match.Groups [3].Value, out int width))
            {
                _consoleSize = new Size (width, height);
            }
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (AnsiOutput), "SizeQuery", $"Failed to parse size query response '{response}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_platform == AnsiPlatform.Degraded)
        {
            return;
        }

        try
        {
            Restore (EscSeqUtils.CSI_DisableBracketedPaste);
            Restore (EscSeqUtils.CSI_DisableMouseEvents);
            Restore (EscSeqUtils.CSI_ResetAttributes);

            if (AppModel == AppModel.Inline)
            {
                Rectangle appScreen = AppScreenGetter?.Invoke () ?? default;
                Restore (EscSeqUtils.CSI_SetCursorPosition (appScreen.Y + appScreen.Height, 1));
                Restore ("\n");
            }
            else
            {
                Restore (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            }

            Restore (EscSeqUtils.CSI_ShowCursor);
        }
        finally
        {
            Trace.Lifecycle (nameof (AnsiOutput), "Dispose", "Releasing output resources.");

            if (_platform == AnsiPlatform.WindowsVT)
            {
                WindowsVTInputHelper.WakePendingRead ();
            }

            _windowsVTOutput?.Dispose ();
        }

        void Restore (string sequence)
        {
            try
            {
                Write (sequence.AsSpan ());
            }
            catch (Exception ex)
            {
                Logging.Error ($"Error restoring ANSI terminal: {ex.Message}");
            }
        }
    }
}
