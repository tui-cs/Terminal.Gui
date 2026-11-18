using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>Json converter from the <see cref="Attribute"/> class.</summary>
internal class AttributeJsonConverter : JsonConverter<Attribute>
{
    private static AttributeJsonConverter? _instance;

    /// <summary></summary>
    public static AttributeJsonConverter Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new AttributeJsonConverter ();
            }

            return _instance;
        }
    }

    public override Attribute Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException ($"Unexpected StartObject token when parsing Attribute: {reader.TokenType}.");
        }

        var attribute = new Attribute ();
        Color? foreground = null;
        Color? background = null;
        TextStyle? style = null;

        string propertyName = string.Empty;

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (foreground is null || background is null)
                {
                    throw new JsonException ($"{propertyName}: Both Foreground and Background colors must be provided.");
                }

                if (style.HasValue)
                {
                    return new Attribute (foreground.Value, background.Value, style.Value);
                }
                else
                {
                    return new Attribute (foreground.Value, background.Value);
                }
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException ($"{propertyName}: Unexpected token when parsing Attribute: {reader.TokenType}.");
            }

            propertyName = reader.GetString ()!;
            reader.Read ();
            string property = reader.TokenType == JsonTokenType.String
                                  ? $"\"{reader.GetString ()}\""
                                  : $"<{reader.TokenType}>";

            try
            {
                switch (propertyName?.ToLower ())
                {
                    case "foreground":
                        foreground = JsonSerializer.Deserialize (ref reader, TuiSerializerContext.Instance.Color);

                        break;
                    case "background":
                        background = JsonSerializer.Deserialize (ref reader, TuiSerializerContext.Instance.Color);

                        break;
                    case "style":
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException ($"{propertyName}: Expected a string value.");
                        }

                        try
                        {
                            style = Enum.Parse<TextStyle> (reader.GetString ()!, ignoreCase: true);
                        }
                        catch (ArgumentException ex)
                        {
                            throw new JsonException ("Expected a valid text style value.", ex);
                        }

                        break;

                    default:
                        throw new JsonException ($"{propertyName}: Unknown Attribute property .");
                }
            }
            catch (JsonException ex)
            {
                throw new JsonException ($"{propertyName}: \"{property}\" - {ex.Message}");
            }
        }

        throw new JsonException ($"{propertyName}: Bad Attribute.");
    }

    public override void Write (Utf8JsonWriter writer, Attribute value, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();
        writer.WritePropertyName (nameof (Attribute.Foreground));
        ColorJsonConverter.Instance.Write (writer, value.Foreground, options);
        writer.WritePropertyName (nameof (Attribute.Background));
        ColorJsonConverter.Instance.Write (writer, value.Background, options);
        if (value.Style != TextStyle.None)
        {
            writer.WritePropertyName (nameof (Attribute.Style));
            writer.WriteStringValue (value.Style.ToString ());
        }

        writer.WriteEndObject ();
    }
}
