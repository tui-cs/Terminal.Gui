#nullable disable
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IInput{TInputRecord}"/> implementation that uses native dotnet methods e.g. <see cref="System.Console"/>
///     .
///     The <see cref="Peek"/> and <see cref="Read"/> methods are executed
///     on the input thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTaskAsync"/>.
/// </summary>
public class NetInput : InputImpl<ConsoleKeyInfo>, ITestableInput<ConsoleKeyInfo>
{
    /// <summary>
    ///     Creates a new instance of the class. Implicitly sends
    ///     console mode settings that enable virtual input (mouse
    ///     reporting etc.).
    /// </summary>
    public NetInput ()
    {
        // Check if we have a real console first
        if (!IsAttachedToTerminal)
        {
            Trace.Lifecycle (nameof (NetInput), "Init", "Console is not attached to a terminal. Running in degraded mode.");

            return;
        }

        PlatformID p = Environment.OSVersion.Platform;

        if (p is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows)
        {
            try
            {
                _adjustConsole = new NetWinVTConsole ();
            }
            catch (ApplicationException ex)
            {
                // Likely running as a unit test, or in a non-interactive session.
                Logging.Critical ($"NetWinVTConsole could not configure terminal modes. May indicate running in non-interactive session: {ex}");

                return;
            }
        }

        try
        {
            //Enable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);

            //Set cursor key to application.
            Console.Out.Write (EscSeqUtils.CSI_HideCursor);

            // CSI_EnableMouseEvents enables
            // Mode 1003 (any-event) - Reports all mouse events including motion with/without buttons
            // Mode 1015 (URXVT) - UTF-8 coordinate encoding (fallback for older terminals)
            // Mode 1006 (SGR) - Modern decimal format with unlimited coordinates (preferred)
            Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);

            // Mode 2004 - Bracketed paste mode. Pastes are wrapped in ESC[200~ ... ESC[201~.
            Console.Out.Write (EscSeqUtils.CSI_EnableBracketedPaste);
            Console.TreatControlCAsInput = true;
        }
        catch
        {
            // Swallow any exceptions during initialization for unit tests
        }
    }

    private readonly NetWinVTConsole _adjustConsole;

    /// <inheritdoc/>
    public override void Dispose ()
    {
        base.Dispose ();

        try
        {
            // Disable bracketed paste and mouse events first
            Console.Out.Write (EscSeqUtils.CSI_DisableBracketedPaste);
            Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);

            //Disable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            Console.Out.Write (EscSeqUtils.CSI_ShowCursor);

            _adjustConsole?.Cleanup ();

            // Flush any pending input so no stray events appear
            FlushConsoleInput ();
        }
        catch
        {
            // Swallow any exceptions during Dispose for unit tests
        }
    }

    /// <inheritdoc/>
    public void InjectInput (ConsoleKeyInfo input) => throw new NotImplementedException ();

    /// <inheritdoc/>
    public override bool Peek ()
    {
        try
        {
            return Console.KeyAvailable;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (true)
        {
            ConsoleKeyInfo keyInfo = default;

            try
            {
                if (!Console.KeyAvailable)
                {
                    break;
                }

                keyInfo = Console.ReadKey (true);
            }
            catch (InvalidOperationException)
            {
                // Not connected to a terminal (GitHub Actions, redirected input, etc.)
                yield break;
            }
            catch (IOException)
            {
                // I/O error reading from console
                yield break;
            }

            yield return keyInfo;
        }
    }

    private void FlushConsoleInput ()
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey (true);
        }
    }
}
