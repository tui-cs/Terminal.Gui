#nullable enable
using System.Buffers.Binary;
using System.Globalization;

namespace DrawingTests.ColorTests;

public partial class ColorClassTests
{
    [Fact]
    public void Color_ToString_WithNamedColor ()
    {
        // Arrange
        var color = new Color (ColorName16.Blue);// Blue

        // Act
        var colorString = color.ToString ();

        // Assert
        Assert.Equal ("Blue", colorString);
    }

    [Fact]
    public void Color_ToString_WithRGBColor ()
    {
        // Arrange
        var color = new Color (1, 64, 32); // Custom RGB color

        // Act
        var colorString = color.ToString ();

        // Assert
        Assert.Equal ("#014020", colorString);
    }

    [Theory]
    [CombinatorialData]
    public void Parse_And_ToString_RoundTrip_For_Known_FormatStrings (
        [CombinatorialValues (null, "", "g", "G", "d", "D")] string formatString,
        [CombinatorialValues (0, 64, 255)] byte r,
        [CombinatorialValues (0, 64, 255)] byte g,
        [CombinatorialValues (0, 64, 255)] byte b
    )
    {
        Color constructedColor = new (r, g, b, 255);

        // Pre-conditions for the rest of the test to be valid
        Assert.Equal (r, constructedColor.R);
        Assert.Equal (g, constructedColor.G);
        Assert.Equal (b, constructedColor.B);
        Assert.Equal (255, constructedColor.A);

        //Get the ToString result with the specified format string
        var formattedColorString = constructedColor.ToString (formatString);

        // Now parse that string
        Color parsedColor = Color.Parse (formattedColorString);

        // They should have identical underlying values
        Assert.Equal (constructedColor.Argb, parsedColor.Argb);
    }

    [Theory]
    [MemberData (
                    nameof (ColorTestsTheoryDataGenerators.TryParse_string_Returns_False_For_Invalid_Inputs),
                    MemberType = typeof (ColorTestsTheoryDataGenerators)
                )]
    public void TryParse_string_Returns_False_For_Invalid_Inputs (string? input)
    {
        bool tryParseStatus = Color.TryParse (input ?? string.Empty, out Color? color);
        Assert.False (tryParseStatus);
        Assert.Null (color);
    }

    [Theory]
    [MemberData (
                    nameof (ColorTestsTheoryDataGenerators.TryParse_string_Returns_True_For_Valid_Inputs),
                    MemberType = typeof (ColorTestsTheoryDataGenerators)
                )]
    public void TryParse_string_Returns_True_For_Valid_Inputs (string? input, int expectedColorArgb)
    {
        bool tryParseStatus = Color.TryParse (input ?? string.Empty, out Color? color);
        Assert.True (tryParseStatus);
        Assert.NotNull (color);
        Assert.IsType<Color> (color);
        Assert.Equal (expectedColorArgb, color.Value.Rgba);
    }
}

public static partial class ColorTestsTheoryDataGenerators
{
    public static TheoryData<string?> TryParse_string_Returns_False_For_Invalid_Inputs ()
    {
        TheoryData<string?> values = [];

        for (var i = char.MinValue; i < 255; i++)
        {
            if (!char.IsAsciiDigit (i))
            {
                values.Add ($"rgb({i},{i},{i})");
                values.Add ($"rgba({i},{i},{i})");
            }

            if (!char.IsAsciiHexDigit (i))
            {
                values.Add ($"#{i}{i}{i}{i}{i}{i}");
                values.Add ($"#{i}{i}{i}{i}{i}{i}{i}{i}");
            }
        }

        //Also throw in a couple of just badly formatted strings
        values.Add ("rgbaa(1,2,3,4))");
        values.Add ("#rgb(1,FF,3,4)");
        values.Add ("rgb(1,FF,3,4");
        values.Add ("rgb(1,2,3,4.5))");

        return values;
    }

    public static TheoryData<string?, int> TryParse_string_Returns_True_For_Valid_Inputs ()
    {
        TheoryData<string?, int> values = []
            ;

        for (byte i = 16; i < 224; i += 32)
        {
            // Using this so the span only has to be written one way.
            int expectedRgb = BinaryPrimitives.ReadInt32LittleEndian ([(byte)(i + 16), i, (byte)(i - 16), 255]);
            int expectedRgba = BinaryPrimitives.ReadInt32LittleEndian ([(byte)(i + 16), i, (byte)(i - 16), i]);
            values.Add ($"rgb({i - 16:D},{i:D},{i + 16:D})", expectedRgb);
            values.Add ($"rgb({i - 16:D},{i:D},{i + 16:D},{i:D})", expectedRgba);
            values.Add ($"rgba({i - 16:D},{i:D},{i + 16:D},{i:D})", expectedRgba);
            values.Add ($"#{i - 16:X2}{i:X2}{i + 16:X2}", expectedRgb);
            values.Add ($"#{i:X2}{i - 16:X2}{i:X2}{i + 16:X2}", expectedRgba);
        }

        for (byte i = 1; i < 0xE; i++)
        {
            values.Add (
                        $"#{i - 1:X0}{i:X0}{i + 1:X0}",
                        BinaryPrimitives.ReadInt32LittleEndian (
                                                                [
                                                                    // Have to stick the least significant 4 bits in the most significant 4 bits to duplicate the hex values
                                                                    // Breaking this out just so it's easier to see.
                                                                    (byte)((i + 1) | ((i + 1) << 4)),
                                                                    (byte)(i | (i << 4)),
                                                                    (byte)((i - 1) | ((i - 1) << 4)),
                                                                    255
                                                                ]
                                                               )
                       );

            values.Add (
                        $"#{i:X0}{i - 1:X0}{i:X0}{i + 1:X0}",
                        BinaryPrimitives.ReadInt32LittleEndian (
                                                                [
                                                                    (byte)((i + 1) | ((i + 1) << 4)),
                                                                    (byte)(i | (i << 4)),
                                                                    (byte)((i - 1) | ((i - 1) << 4)),
                                                                    (byte)(i | (i << 4))
                                                                ]
                                                               )
                       );
        }

        return values;

    }
}
