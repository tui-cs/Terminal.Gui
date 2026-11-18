namespace DriverTests.AnsiDriver;

/// <summary>
///     Low-level tests for AnsiInput and AnsiOutput implementations.
///     These tests verify that ANSI driver components work correctly in all environments.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiInputOutputTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiInput_Constructor_DoesNotThrow ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var input = new AnsiInput ();
                                                     _output.WriteLine ("AnsiInput created successfully");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_Constructor_DoesNotThrow ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var output = new AnsiOutput ();
                                                     _output.WriteLine ("AnsiOutput created successfully");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_GetSize_ReturnsExpectedSize ()
    {
        // Arrange
        using var output = new AnsiOutput ();

        // Act
        Size size = output.GetSize ();
        _output.WriteLine ($"AnsiOutput.GetSize() returned: {size.Width}x{size.Height}");

        // Assert
        Assert.True (size.Width > 0);
        Assert.True (size.Height > 0);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_Suspend_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new AnsiOutput ();

        // Act
        Exception? exception = Record.Exception (() => output.Suspend ());

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiComponentFactory_CreateInput_DoesNotThrow ()
    {
        // Arrange
        var factory = new AnsiComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IInput<char> input = factory.CreateInput ();
                                                     _output.WriteLine ("AnsiComponentFactory.CreateInput() succeeded");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiComponentFactory_CreateOutput_DoesNotThrow ()
    {
        // Arrange
        var factory = new AnsiComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IOutput output = factory.CreateOutput ();
                                                     _output.WriteLine ("AnsiComponentFactory.CreateOutput() succeeded");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiDriver_IsAttachedToTerminal_ReturnsFalse_InTestHarness ()
    {
        // Copilot - generated.
        // Act — Driver.IsAttachedToTerminal is the shared entry point all drivers use.
        bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

        // Assert
        Assert.False (result, "AnsiDriver: IsAttachedToTerminal should return false in test harness");
        Assert.False (inputAttached);
        Assert.False (outputAttached);
    }
}
