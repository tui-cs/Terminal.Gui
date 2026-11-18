using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Serializes and deserializes <see cref="Key"/> arrays as JSON string arrays (e.g. <c>["Ctrl+A", "Home"]</c>).
///     Each element uses <see cref="Key.ToString()"/> for writing and <see cref="Key.TryParse"/> for reading.
/// </summary>
public class KeyArrayJsonConverter : JsonConverter<Key []?>
{
    /// <inheritdoc/>
    public override Key []? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException ("Expected start of array for Key[].");
        }

        List<Key> keys = [];

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return keys.ToArray ();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException ($"Expected string token in Key array, got {reader.TokenType}.");
            }

            string keyString = reader.GetString ()!;

            keys.Add (Key.TryParse (keyString, out Key key) ? key : Key.Empty);
        }

        throw new JsonException ("Unexpected end of JSON while reading Key array.");
    }

    /// <inheritdoc/>
    public override void Write (Utf8JsonWriter writer, Key []? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue ();

            return;
        }

        writer.WriteStartArray ();

        foreach (Key key in value)
        {
            writer.WriteStringValue (key.ToString ());
        }

        writer.WriteEndArray ();
    }
}
