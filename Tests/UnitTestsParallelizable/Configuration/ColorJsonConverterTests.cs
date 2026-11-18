using System.Text.Json;

namespace ConfigurationTests;

public class ColorJsonConverterTests
{
    public static readonly JsonSerializerOptions JsonOptions = new ()
    {
        Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
    };

    [Theory]
    [InlineData ("\"#000000\"", 0, 0, 0)]
    public void DeserializesFromHexCode (string hexCode, int r, int g, int b)
    {
        // Arrange
        var expected = new Color (r, g, b);

        // Act
        var actual = JsonSerializer.Deserialize<Color> (
                                                        hexCode,
                                                        new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                       );

        //Assert
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("\"rgb(0,0,0)\"", 0, 0, 0)]
    public void DeserializesFromRgb (string rgb, int r, int g, int b)
    {
        // Arrange
        var expected = new Color (r, g, b);

        // Act
        var actual = JsonSerializer.Deserialize<Color> (
                                                        rgb,
                                                        new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                       );

        //Assert
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData (ColorName16.Black, "Black")]
    [InlineData (ColorName16.Blue, "Blue")]
    [InlineData (ColorName16.Green, "Green")]
    [InlineData (ColorName16.Cyan, "Aqua")] // W3C+ Standard overrides
    [InlineData (ColorName16.Gray, "Gray")]
    [InlineData (ColorName16.Red, "Red")]
    [InlineData (ColorName16.Magenta, "Fuchsia")]   // W3C+ Standard overrides
    [InlineData (ColorName16.Yellow, "Yellow")]
    [InlineData (ColorName16.DarkGray, "BrightBlack")] // Legacy ColorName16.DarkGray now serializes as BrightBlack (first alphabetical match)
    [InlineData (ColorName16.BrightBlue, "BrightBlue")]
    [InlineData (ColorName16.BrightGreen, "BrightGreen")]
    [InlineData (ColorName16.BrightCyan, "BrightCyan")]
    [InlineData (ColorName16.BrightRed, "BrightRed")]
    [InlineData (ColorName16.BrightMagenta, "BrightMagenta")]
    [InlineData (ColorName16.BrightYellow, "BrightYellow")]
    [InlineData (ColorName16.White, "White")]
    public void SerializesColorName16ValuesAsStrings (ColorName16 colorName, string expectedJson)
    {
        var converter = new ColorJsonConverter ();
        var options = new JsonSerializerOptions { Converters = { converter } };

        string serialized = JsonSerializer.Serialize (new Color (colorName), options);

        Assert.Equal ($"\"{expectedJson}\"", serialized);
    }

    [Theory]
    [InlineData (1, 0, 0, "\"#010000\"")]
    [InlineData (0, 0, 1, "\"#000001\"")]
    public void SerializesToHexCode (int r, int g, int b, string expected)
    {
        // Arrange

        // Act
        string actual = JsonSerializer.Serialize (
                                                  new Color (r, g, b),
                                                  new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                 );

        //Assert
        Assert.Equal (expected, actual);
    }

    [Theory]
    [InlineData ("Black", Color.Black)]
    [InlineData ("Blue", Color.Blue)]
    [InlineData ("BrightBlue", Color.BrightBlue)]
    [InlineData ("BrightCyan", Color.BrightCyan)]
    [InlineData ("BrightGreen", Color.BrightGreen)]
    [InlineData ("BrightMagenta", Color.BrightMagenta)]
    [InlineData ("BrightRed", Color.BrightRed)]
    [InlineData ("BrightYellow", Color.BrightYellow)]
    [InlineData ("Yellow", Color.Yellow)]
    [InlineData ("Cyan", Color.Cyan)]
    [InlineData ("BrightBlack", Color.DarkGray)] // Legacy ColorName16.DarkGray is now accessible as BrightBlack
    [InlineData ("Gray", Color.Gray)]
    [InlineData ("Green", Color.Green)]
    [InlineData ("Magenta", Color.Magenta)]
    [InlineData ("Red", Color.Red)]
    [InlineData ("White", Color.White)]
    public void TestColorDeserializationFromHumanReadableColorName16 (string colorName, ColorName16 expectedColor)
    {
        // Arrange
        var json = $"\"{colorName}\"";

        // Act
        var actualColor = JsonSerializer.Deserialize<Color> (json, JsonOptions);

        // Assert
        Assert.Equal (new (expectedColor), actualColor);
    }

    [Fact]
    public void TestDeserializeColor_Black ()
    {
        // Arrange
        var json = "\"Black\"";
        var expectedColor = new Color ("Black");

        // Act
        var color = JsonSerializer.Deserialize<Color> (
                                                       json,
                                                       new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                      );

        // Assert
        Assert.Equal (expectedColor, color);
    }

    [Fact]
    public void TestDeserializeColor_BrightRed ()
    {
        // Arrange
        var json = "\"BrightRed\"";
        var expectedColor = Color.BrightRed;

        // Act
        var color = JsonSerializer.Deserialize<Color> (
                                                       json,
                                                       new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                                      );

        // Assert
        Assert.Equal (expectedColor, color);
    }

    [Fact]
    public void TestSerializeColor_Black ()
    {
        // Arrange
        var expectedJson = "\"Black\"";

        // Act
        string json = JsonSerializer.Serialize (
                                                new Color (Color.Black),
                                                new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                               );

        // Assert
        Assert.Equal (expectedJson, json);
    }

    [Fact]
    public void TestSerializeColor_BrightRed ()
    {
        // Arrange
        var expectedJson = "\"BrightRed\"";

        // Act
        string json = JsonSerializer.Serialize (
                                                new Color (Color.BrightRed),
                                                new JsonSerializerOptions { Converters = { new ColorJsonConverter () } }
                                               );

        // Assert
        Assert.Equal (expectedJson, json);
    }
}