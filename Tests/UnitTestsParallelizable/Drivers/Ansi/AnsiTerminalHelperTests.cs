#nullable enable

namespace DriverTests;

/// <summary>
///     Tests for <see cref="Driver.IsAttachedToTerminal"/> environment-variable gating.
///     Copilot - generated.
/// </summary>
public class DriverIsAttachedToTerminalTests (ITestOutputHelper output)
{
    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void DisableRealDriverIO_EnvironmentVariable_IsSet ()
    {
        // Diagnostic: verify the env var actually reaches the test process.
        string? value = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        output.WriteLine ($"DisableRealDriverIO = '{value ?? "(null)"}'");

        Assert.Equal ("1", value);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void IsAttachedToTerminal_ReturnsFalse_WhenDisableRealDriverIO_IsSet ()
    {
        // Arrange – the env var should already be "1" via the test harness.
        string? value = Environment.GetEnvironmentVariable ("DisableRealDriverIO");
        output.WriteLine ($"DisableRealDriverIO = '{value ?? "(null)"}'");

        // Act
        bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

        // Assert
        Assert.False (result, "IsAttachedToTerminal should return false when DisableRealDriverIO=1");
        Assert.False (inputAttached, "inputAttached should be false when DisableRealDriverIO=1");
        Assert.False (outputAttached, "outputAttached should be false when DisableRealDriverIO=1");
    }
}
