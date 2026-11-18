using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
/// Json converter for the <see cref="Color"/> class.
/// <para>
///     Serialization outputs a string with the color name if the color matches a name in <see cref="ColorStrings"/>
///     or the "#RRGGBB" hexadecimal representation (e.g. "#FF0000" for red).
/// </para>
/// <para>
///     Deserialization formats supported are "#RGB", "#RRGGBB", "#ARGB", "#AARRGGBB", "rgb(r,g,b)",
///     "rgb(r,g,b,a)", "rgba(r,g,b)", "rgba(r,g,b,a)", or any W3C color name.</para>
/// </summary>
internal class ColorJsonConverter : JsonConverter<Color>
{
    private static ColorJsonConverter? _instance;

    /// <summary>Singleton</summary>
    public static ColorJsonConverter Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new ();
            }

            return _instance;
        }
    }

    public override Color Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Check if the value is a string
        if (reader.TokenType == JsonTokenType.String)
        {
            // Get the color string
            ReadOnlySpan<char> colorString = reader.GetString ();

            if (ColorStrings.TryParseNamedColor (colorString, out Color namedColor))
            {
                return namedColor;
            }

            if (Color.TryParse (colorString, null, out Color parsedColor))
            {
                return parsedColor;
            }

            throw new JsonException ($"Unexpected color name: {colorString}.");
        }

        throw new JsonException ($"Unexpected token when parsing Color: {reader.TokenType}");
    }

    public override void Write (Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue (value.ToString ());
    }
}
