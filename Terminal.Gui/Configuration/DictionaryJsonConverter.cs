#nullable disable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

internal class DictionaryJsonConverter<T> : JsonConverter<Dictionary<string, T>>
{
    public override Dictionary<string, T> Read (
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException ($"Expected a JSON array (\"[ {{ ... }} ]\"), but got \"{reader.TokenType}\".");
        }

        // If the Json options indicate ignoring case, use the invariant culture ignore case comparer.
        Dictionary<string, T> dictionary = new (
                                                options.PropertyNameCaseInsensitive
                                                    ? StringComparer.InvariantCultureIgnoreCase
                                                    : StringComparer.InvariantCulture);

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Read ();

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string key = reader.GetString ();
                    reader.Read ();
                    object value = JsonSerializer.Deserialize (ref reader, typeof (T), TuiSerializerContext.Instance);
                    dictionary.Add (key, (T)value);
                }
            }
            else if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }
        }

        return dictionary;
    }

    public override void Write (Utf8JsonWriter writer, Dictionary<string, T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray ();

        foreach (KeyValuePair<string, T> item in value)
        {
            writer.WriteStartObject ();

            //writer.WriteString (item.Key, item.Key);
            writer.WritePropertyName (item.Key);
            JsonSerializer.Serialize (writer, item.Value, typeof (T), TuiSerializerContext.Instance);
            writer.WriteEndObject ();
        }

        writer.WriteEndArray ();
    }
}
