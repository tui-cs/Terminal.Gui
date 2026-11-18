#nullable enable
using Terminal.Gui.Drivers;

namespace DriverTests;

/// <summary>
///     Tests for <see cref="TerminalDevice"/> — the helper that resolves the controlling terminal
///     device for input/output, falling back to <c>/dev/tty</c> (Unix) or <c>CONIN$</c>/<c>CONOUT$</c>
///     (Windows) when stdin/stdout are redirected.
/// </summary>
[Collection ("Driver Tests")]
public class TerminalDeviceTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    // Copilot
    public void IsInputAttached_And_IsOutputAttached_AreFalse_WhenDisableRealDriverIO ()
    {
        // Arrange — emulate the test-harness environment used elsewhere in the repo.
        string? prev = Environment.GetEnvironmentVariable ("DisableRealDriverIO");

        try
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
            TerminalDevice.ResetForTesting ();

            // Act
            bool inputAttached = TerminalDevice.IsInputAttached;
            bool outputAttached = TerminalDevice.IsOutputAttached;

            // Assert — when the harness disables real driver IO, no terminal device is
            // ever returned, so the AnsiDriver stays in degraded mode in CI.
            Assert.False (inputAttached);
            Assert.False (outputAttached);
            Assert.Equal (-1, TerminalDevice.InputFd);
            Assert.Equal (-1, TerminalDevice.OutputFd);
            Assert.Equal (nint.Zero, TerminalDevice.InputHandle);
            Assert.Equal (nint.Zero, TerminalDevice.OutputHandle);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", prev);
            TerminalDevice.ResetForTesting ();
        }
    }

    [Fact]
    // Copilot
    public void Driver_IsAttachedToTerminal_ReturnsFalse_WhenDisableRealDriverIO ()
    {
        // Arrange — Driver.IsAttachedToTerminal must continue to honour the harness override
        // even after routing through TerminalDevice.
        string? prev = Environment.GetEnvironmentVariable ("DisableRealDriverIO");

        try
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
            TerminalDevice.ResetForTesting ();

            // Act
            bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

            // Assert
            Assert.False (result);
            Assert.False (inputAttached);
            Assert.False (outputAttached);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", prev);
            TerminalDevice.ResetForTesting ();
        }
    }

    [Fact]
    // Copilot
    public void ResetForTesting_ClearsCachedState ()
    {
        // Arrange — populate the cache once.
        TerminalDevice.ResetForTesting ();
        bool _ = TerminalDevice.IsInputAttached;

        // Act — reset, then change the env var and re-resolve to ensure values are not cached
        // across resets.
        string? prev = Environment.GetEnvironmentVariable ("DisableRealDriverIO");

        try
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
            TerminalDevice.ResetForTesting ();

            // Assert — after reset+disable, lookups return the disabled state.
            Assert.False (TerminalDevice.IsInputAttached);
            Assert.False (TerminalDevice.IsOutputAttached);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", prev);
            TerminalDevice.ResetForTesting ();
        }
    }

    [Fact]
    // Copilot
    public void TryWriteStdout_ReturnsFalse_WhenNoTerminalDevice ()
    {
        // Arrange
        string? prev = Environment.GetEnvironmentVariable ("DisableRealDriverIO");

        try
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
            TerminalDevice.ResetForTesting ();

            // Act — TryWriteStdout must gracefully no-op when no terminal device is available
            // rather than writing to fd 1 (which would corrupt the redirected stdout stream).
            bool result = UnixIOHelper.TryWriteStdout ([0x41, 0x42, 0x43]);

            // Assert
            Assert.False (result);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", prev);
            TerminalDevice.ResetForTesting ();
        }
    }

    [Fact]
    // Copilot
    public void TryReadStdin_ReturnsFalse_WhenNoTerminalDevice ()
    {
        // Arrange
        string? prev = Environment.GetEnvironmentVariable ("DisableRealDriverIO");

        try
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", "1");
            TerminalDevice.ResetForTesting ();
            byte [] buffer = new byte [16];

            // Act
            bool result = UnixIOHelper.TryReadStdin (buffer, out int bytesRead);

            // Assert — TryReadStdin must not silently read from STDIN_FILENO when no terminal
            // device is available, otherwise we would consume bytes intended for the app's
            // redirected stdin pipeline (e.g. `echo foo | myapp`).
            Assert.False (result);
            Assert.Equal (0, bytesRead);
        }
        finally
        {
            Environment.SetEnvironmentVariable ("DisableRealDriverIO", prev);
            TerminalDevice.ResetForTesting ();
        }
    }
}
