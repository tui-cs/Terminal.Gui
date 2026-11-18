#nullable enable
using System.Collections.Concurrent;

namespace DriverTests.DotnetDriver;

/// <summary>
///     Low-level tests for NetInput and NetOutput implementations.
///     These tests are designed to fail with good error messages when run in environments
///     without a real terminal (like GitHub Actions).
/// </summary>
[Collection ("Driver Tests")]
public class NetInputOutputTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var input = new NetInput ();
                                                     _output.WriteLine ("NetInput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Peek_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var input = new NetInput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     bool hasInput = input.Peek ();
                                                     _output.WriteLine ($"NetInput.Peek() returned: {hasInput}");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput.Peek() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Read_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var input = new NetInput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     List<ConsoleKeyInfo> items = input.Read ().ToList ();
                                                     _output.WriteLine ($"NetInput.Read() returned {items.Count} items");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput.Read() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Dispose_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        var input = new NetInput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     input.Dispose ();
                                                     _output.WriteLine ("NetInput disposed successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput.Dispose() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var output = new NetOutput ();
                                                     _output.WriteLine ("NetOutput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_GetSize_ReturnsDefaultSize_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Size size = default;

        Exception? exception = Record.Exception (() =>
                                                 {
                                                     size = output.GetSize ();
                                                     _output.WriteLine ($"NetOutput.GetSize() returned: {size.Width}x{size.Height}");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput.GetSize() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
        Assert.True (size.Width > 0, "Width should be > 0");
        Assert.True (size.Height > 0, "Height should be > 0");
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_Write_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     ReadOnlySpan<char> text = "Test".AsSpan ();
                                                     output.Write (text);
                                                     _output.WriteLine ("NetOutput.Write() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput.Write() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_SetCursor_Position_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     output.SetCursor (new () { Position = new (0, 0) });
                                                     _output.WriteLine ("NetOutput.SetCursorPosition() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput.SetCursorPosition() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_Suspend_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Exception? exception = Record.Exception (() => output.Suspend ());

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInputProcessor_Constructor_DoesNotThrow ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     ConcurrentQueue<ConsoleKeyInfo> queue = new ();
                                                     var processor = new NetInputProcessor (queue, null);
                                                     _output.WriteLine ("NetInputProcessor created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInputProcessor constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetComponentFactory_CreateInput_DoesNotThrow ()
    {
        // Arrange
        var factory = new NetComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IInput<ConsoleKeyInfo> input = factory.CreateInput ();
                                                     _output.WriteLine ("NetComponentFactory.CreateInput() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetComponentFactory.CreateInput() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetComponentFactory_CreateOutput_DoesNotThrow ()
    {
        // Arrange
        var factory = new NetComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IOutput output = factory.CreateOutput ();
                                                     _output.WriteLine ("NetComponentFactory.CreateOutput() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetComponentFactory.CreateOutput() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetDriver_IsAttachedToTerminal_ReturnsFalse_InTestHarness ()
    {
        // Copilot - generated.
        // Act — Driver.IsAttachedToTerminal is the shared entry point all drivers use.
        bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

        // Assert
        Assert.False (result, "NetDriver: IsAttachedToTerminal should return false in test harness");
        Assert.False (inputAttached);
        Assert.False (outputAttached);
    }
}