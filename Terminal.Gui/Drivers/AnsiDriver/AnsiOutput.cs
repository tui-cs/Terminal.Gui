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
    // Tracks which underlying platform APIs are in use
    private readonly AnsiPlatform _platform;

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
            // Ensure the console output code page is UTF-8 (65001). The ANSI driver writes
            // UTF-8 encoded bytes via WriteFile; without this, a fresh Windows terminal uses
            // its default OEM code page (e.g. 437), causing multi-byte UTF-8 characters
            // (box-drawing glyphs, etc.) to be misinterpreted as garbled single-byte characters.
            // See https://github.com/tui-cs/Terminal.Gui/issues/4848
            Console.OutputEncoding = Encoding.UTF8;

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
                // duplicate the controlling terminal output fd so we don't mess with it.
                // When stdout is redirected this is /dev/tty rather than STDOUT_FILENO,
                // allowing TUI rendering to appear on the real terminal even when the
                // app's stdout participates in a shell pipeline.
                int outputFd = TerminalDevice.OutputFd;

                if (outputFd < 0)
                {
                    Trace.Lifecycle (nameof (AnsiOutput), "Init", "Console output stream is not writable. Running in degraded mode.");

                    return;
                }

                int fdCopy = UnixIOHelper.dup (outputFd);

                if (fdCopy == -1)
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
            _platform = AnsiPlatform.Degraded;
        }
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
        // In batch mode, route through the byte-span path to merge with deferred cursor moves.
        if (_batchMode && _pendingCursorMoves.Length > 0)
        {
            byte [] utf8 = Encoding.UTF8.GetBytes (output.ToString ());
            Write (utf8);

            return;
        }

        base.Write (output);

        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    _windowsVTOutput!.Write (output);

                    break;

                case AnsiPlatform.UnixRaw:
                    byte [] utf8 = Encoding.UTF8.GetBytes (output.ToString ());
                    UnixIOHelper.TryWriteStdout (utf8);

                    return;

                case AnsiPlatform.Degraded:
                default:
                    break;
            }
        }
        catch (Exception)
        {
            // ignore for unit tests
        }
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        StringBuilder capturedOutput = new ();
        capturedOutput.Append (text);
        base.Write (capturedOutput);

        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    _windowsVTOutput!.Write (capturedOutput);

                    break;

                case AnsiPlatform.UnixRaw:
                    byte [] utf8 = Encoding.UTF8.GetBytes (text.ToArray ());
                    UnixIOHelper.TryWriteStdout (utf8);

                    return;

                case AnsiPlatform.Degraded:
                default:
                    break;
            }
        }
        catch (Exception)
        {
            // ignore for unit tests
        }
    }

    /// <inheritdoc cref="IOutput.Write(IOutputBuffer)"/>
    public override void Write (IOutputBuffer buffer)
    {
        _lastBuffer = buffer;
        _batchMode = true;

        try
        {
            base.Write (buffer);
        }
        finally
        {
            _batchMode = false;

            // Flush any remaining deferred cursor moves
            if (_pendingCursorMoves.Length > 0)
            {
                Write (_pendingCursorMoves.AsSpan ());
                _pendingCursorMoves.Clear ();
            }
        }
    }

    /// <summary>
    ///     PERF: Write pre-encoded UTF-8 bytes directly to the terminal.
    ///     In batch mode, merges deferred cursor moves with cell content into one WriteFile.
    ///     This is the hot-path write — zero string/byte[] allocation, zero encoding.
    /// </summary>
    protected override void Write (ReadOnlySpan<byte> output)
    {
        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    if (_batchMode && _pendingCursorMoves.Length > 0)
                    {
                        _pendingCursorMoves.AppendBytes (output);
                        base.Write (_pendingCursorMoves.AsSpan ());
                        _windowsVTOutput!.Write (_pendingCursorMoves.AsSpan ());
                        _pendingCursorMoves.Clear ();
                    }
                    else
                    {
                        base.Write (output);
                        _windowsVTOutput!.Write (output);
                    }

                    break;

                case AnsiPlatform.UnixRaw:
                    if (_batchMode && _pendingCursorMoves.Length > 0)
                    {
                        _pendingCursorMoves.AppendBytes (output);
                        base.Write (_pendingCursorMoves.AsSpan ());
                        WriteUnix (_pendingCursorMoves.AsSpan ());
                        _pendingCursorMoves.Clear ();
                    }
                    else
                    {
                        base.Write (output);
                        WriteUnix (output);
                    }

                    return;

                case AnsiPlatform.Degraded:
                default:
                    base.Write (output);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Swallowed for unit tests (degraded/no-terminal scenarios); logged for production diagnostics.
            Logging.Error ($"Output write failed in {nameof (AnsiOutput)}: {ex.Message}");
        }
    }

    private Cursor _currentCursor;

    /// <summary>
    ///     Copies <paramref name="output"/> into a reusable backing array and writes to stdout,
    ///     avoiding <see cref="ReadOnlySpan{T}.ToArray"/> allocation on every call.
    /// </summary>
    private void WriteUnix (ReadOnlySpan<byte> output)
    {
        int len = output.Length;

        if (len == 0)
        {
            return;
        }

        if (_unixWriteBuffer is null || _unixWriteBuffer.Length < len)
        {
            _unixWriteBuffer = new byte [len];
        }

        output.CopyTo (_unixWriteBuffer);
        UnixIOHelper.TryWriteStdout (_unixWriteBuffer, len);
    }

    // PERF: Batch mode — defer cursor-move sequences to merge with cell content, reducing WriteFile P/Invoke count
    private readonly Utf8Buffer _pendingCursorMoves = new ();
    private bool _batchMode;

    // PERF: Reusable backing array for Unix writes — avoids ToArray() allocation on every Write call.
    private byte []? _unixWriteBuffer;

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
        }
        catch
        {
            // Ignore any exceptions
        }
        finally
        {
            SetCursorPositionImpl (cursor.Position?.X ?? 0, cursor.Position?.Y ?? 0);

            _currentCursor = cursor;
        }
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int col, int row)
    {
        if (_currentCursor.Position is { } && _currentCursor.Position.Value.X == col && _currentCursor.Position.Value.Y == row)
        {
            return false;
        }

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
        try
        {
            if (_platform == AnsiPlatform.Degraded)
            {
                return;
            }

            // Restore terminal state: disable mouse, reset attributes, show cursor
            Write (EscSeqUtils.CSI_DisableBracketedPaste);
            Write (EscSeqUtils.CSI_DisableMouseEvents);
            Write (EscSeqUtils.CSI_ResetAttributes);

            if (AppModel == AppModel.Inline)
            {
                // Inline mode: do NOT restore alternate buffer. Move cursor to just
                // below the inline region so the shell prompt appears naturally.
                // Position to the last row of the inline region (guaranteed to exist),
                // then write \n to scroll the terminal if at the bottom edge.
                Rectangle appScreen = AppScreenGetter?.Invoke () ?? default;
                int lastInlineRow = appScreen.Y + appScreen.Height; // 1-indexed last row
                Write (EscSeqUtils.CSI_SetCursorPosition (lastInlineRow, 1));
                Write ("\n");
            }
            else
            {
                // FullScreen mode: restore alternate buffer and show cursor
                Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            }

            Write (EscSeqUtils.CSI_ShowCursor);
        }
        catch
        {
            // Ignore errors - we're shutting down
        }
        finally
        {
            Trace.Lifecycle (nameof (AnsiOutput), "Dispose", "Flushing output and releasing resources.");

            if (_platform == AnsiPlatform.WindowsVT)
            {
                WindowsVTInputHelper.WakePendingRead ();
            }

            _windowsVTOutput?.Dispose ();
        }
    }
}
