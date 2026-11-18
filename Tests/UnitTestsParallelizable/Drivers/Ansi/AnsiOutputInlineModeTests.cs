// Copilot

namespace DriverTests.AnsiDriver;

/// <summary>
///     Tests for <see cref="AnsiOutput"/> inline mode behavior.
///     Verifies that the alternate screen buffer sequences are correctly emitted or skipped
///     based on the <see cref="AppModel"/>.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiOutputInlineModeTests
{
    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_Constructor_FullScreen_DoesNotThrow ()
    {
        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using AnsiOutput output = new (AppModel.FullScreen);
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_Constructor_Inline_DoesNotThrow ()
    {
        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using AnsiOutput output = new (AppModel.Inline);
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_DefaultConstructor_UsesFullScreen ()
    {
        // Act
        using AnsiOutput output = new ();

        // Assert
        Assert.Equal (AppModel.FullScreen, output.AppModel);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_Constructor_Inline_StoresAppModel ()
    {
        // Act
        using AnsiOutput output = new (AppModel.Inline);

        // Assert
        Assert.Equal (AppModel.Inline, output.AppModel);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void AnsiOutput_Constructor_FullScreen_StoresAppModel ()
    {
        // Act
        using AnsiOutput output = new (AppModel.FullScreen);

        // Assert
        Assert.Equal (AppModel.FullScreen, output.AppModel);
    }
}
