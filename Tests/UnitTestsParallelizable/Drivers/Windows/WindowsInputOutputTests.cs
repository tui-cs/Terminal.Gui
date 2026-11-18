#nullable enable

namespace DriverTests.WindowsDriver;

/// <summary>
///     Low-level tests for WindowsInput and WindowsOutput implementations.
///     These tests are designed to fail with good error messages when run in environments
///     without a real terminal (like GitHub Actions).
/// </summary>
[Trait ("Platform", "Windows")]
[Collection ("Driver Tests")]
public class WindowsInputOutputTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void WindowsInput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Windows test on non-Windows");

            return;
        }

        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var input = new WindowsInput ();
                                                     _output.WriteLine ("WindowsInput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: WindowsInput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void WindowsOutput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Windows test on non-Windows");

            return;
        }

        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     try
                                                     {
                                                         using var output = new WindowsOutput ();
                                                         _output.WriteLine ("WindowsOutput created successfully");
                                                     }
                                                     catch (Exception ex)
                                                     {
                                                         _output.WriteLine ($"WindowsOutput threw during construction: {ex.GetType ().Name}: {ex.Message}");

                                                         throw;
                                                     }
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: WindowsOutput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void WindowsOutput_Suspend_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new WindowsOutput ();

        // Act
        Exception? exception = Record.Exception (() => output.Suspend ());

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void WindowsDriver_IsAttachedToTerminal_ReturnsFalse_InTestHarness ()
    {
        // Copilot - generated.
        // Act — Driver.IsAttachedToTerminal is the shared entry point all drivers use.
        bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

        // Assert
        Assert.False (result, "WindowsDriver: IsAttachedToTerminal should return false in test harness");
        Assert.False (inputAttached);
        Assert.False (outputAttached);
    }
}