using System.Text.Json;
using Moq;
using UnitTests;

namespace ConfigurationTests;

public class AttributeJsonConverterTests
{
    public static readonly JsonSerializerOptions JsonOptions = new ()
    {
        Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
    };

    [Fact]
    public void TestDeserialize ()
    {
        // Test deserializing from human-readable color names
        var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\"}";
        var attribute = JsonSerializer.Deserialize<Attribute> (json, JsonOptions);
        Assert.Equal (Color.Blue, attribute.Foreground.GetClosestNamedColor16 ());
        Assert.Equal (Color.Green, attribute.Background.GetClosestNamedColor16 ());

        // Test deserializing from RGB values
        json = "{\"Foreground\":\"rgb(255,0,0)\",\"Background\":\"rgb(0,255,0)\"}";
        attribute = JsonSerializer.Deserialize<Attribute> (json, JsonOptions);
        Assert.Equal (Color.Red, attribute.Foreground.GetClosestNamedColor16 ());
        Assert.Equal (Color.BrightGreen, attribute.Background.GetClosestNamedColor16 ());
    }


    [Fact]
    public void Deserialize_TextStyle ()
    {
        var justStyleJson = "\"Bold\"";
        TextStyle textStyle = JsonSerializer.Deserialize<TextStyle> (justStyleJson, JsonOptions);
        Assert.Equal (TextStyle.Bold, textStyle);

        justStyleJson = "\"Bold,Underline\"";
        textStyle = JsonSerializer.Deserialize<TextStyle> (justStyleJson, JsonOptions);
        Assert.Equal (TextStyle.Bold | TextStyle.Underline, textStyle);

        var json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\",\"Style\":\"Bold\"}";
        Attribute attribute = JsonSerializer.Deserialize<Attribute> (json, JsonOptions);
        Assert.Equal (TextStyle.Bold, attribute.Style);
    }

    // Copilot
    [Fact]
    public void Deserialize_TextStyle_WithNonStringToken_ThrowsJsonException ()
    {
        string json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\",\"Style\":1}";

        JsonException exception = Assert.Throws<JsonException> (() => JsonSerializer.Deserialize<Attribute> (json, JsonOptions));
        Assert.Contains ("Style", exception.Message);
        Assert.Contains ("Expected a string value", exception.Message);
    }

    // Copilot
    [Fact]
    public void Deserialize_TextStyle_WithInvalidString_ThrowsJsonException ()
    {
        string json = "{\"Foreground\":\"Blue\",\"Background\":\"Green\",\"Style\":\"Bogus\"}";

        JsonException exception = Assert.Throws<JsonException> (() => JsonSerializer.Deserialize<Attribute> (json, JsonOptions));
        Assert.Contains ("Style", exception.Message);
        Assert.Contains ("Expected a valid text style value", exception.Message);
    }

    [Fact]
    public void TestSerialize ()
    {
        // Test serializing to human-readable color names
        var attribute = new Attribute (Color.Blue, Color.Green);
        string json = JsonSerializer.Serialize (attribute, JsonOptions);
        Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\"}", json);
    }

    [Fact]
    public void Serialize_TextStyle ()
    {
        // Test serializing to human-readable color names
        var attribute = new Attribute (Color.Blue, Color.Green, TextStyle.Bold);
        string json = JsonSerializer.Serialize (attribute, JsonOptions);
        Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\",\"Style\":\"Bold\"}", json);

        attribute = new Attribute (Color.Blue, Color.Green, TextStyle.Bold | TextStyle.Italic);
        json = JsonSerializer.Serialize (attribute, JsonOptions);
        Assert.Equal ("{\"Foreground\":\"Blue\",\"Background\":\"Green\",\"Style\":\"Bold, Italic\"}", json);

    }

    [Fact]
    public void JsonRoundTrip_PreservesEquality ()
    {
        Attribute original = new (Color.Red, Color.Green, TextStyle.None);

        string json = JsonSerializer.Serialize (original, new JsonSerializerOptions
        {
            Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
        });

        Attribute roundTripped = JsonSerializer.Deserialize<Attribute> (json, new JsonSerializerOptions
        {
            Converters = { new AttributeJsonConverter (), new ColorJsonConverter () }
        })!;

        Assert.Equal (original, roundTripped); // ✅ This should pass if all fields are faithfully round-tripped
    }

}
