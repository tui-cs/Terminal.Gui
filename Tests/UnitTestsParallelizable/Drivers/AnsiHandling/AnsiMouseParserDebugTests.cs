namespace DriverTests.AnsiHandling;

/// <summary>
///     Debug tests to understand ANSI mouse button code mapping.
/// </summary>
[Collection ("Driver Tests")]
public class AnsiMouseParserDebugTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (0, 'M', "LeftButtonPressed")]
    [InlineData (1, 'M', "MiddleButtonPressed")]
    [InlineData (2, 'M', "RightButtonPressed")]
    [InlineData (8, 'M', "LeftButtonPressed,Alt")]
    [InlineData (16, 'M', "LeftButtonPressed,Ctrl")]
    [InlineData (24, 'M', "LeftButtonPressed,Ctrl,Alt")]
    [InlineData (22, 'M', "RightButtonPressed,Ctrl,Shift")]
    [InlineData (64, 'M', "WheeledUp")]
    [InlineData (65, 'M', "WheeledDown")]
    [InlineData (68, 'M', "WheeledLeft")]
    [InlineData (69, 'M', "WheeledRight")]
    public void AnsiMouseParser_ButtonCodeMapping (int buttonCode, char terminator, string expectedFlagsDescription)
    {
        // Arrange
        var parser = new AnsiMouseParser ();
        string ansiSequence = $"\u001B[<{buttonCode};10;10{terminator}";

        // Act
        Mouse? mouse = parser.ProcessMouseInput (ansiSequence);

        // Assert
        Assert.NotNull (mouse);
        _output.WriteLine ($"Button code {buttonCode} with terminator '{terminator}' produces: {mouse.Flags}");
        _output.WriteLine ($"Expected: {expectedFlagsDescription}");
    }
}

