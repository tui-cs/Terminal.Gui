#nullable disable
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

internal class ConcurrentDictionaryJsonConverter<T> : JsonConverter<ConcurrentDictionary<string, T>>
{
    public override ConcurrentDictionary<string, T> Read (
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException ($"Expected a JSON array (\"[ {{ ... }} ]\"), but got \"{reader.TokenType}\".");
        }

        // If the Json options indicate ignoring case, use the invariant culture ignore case comparer
        ConcurrentDictionary<string, T> dictionary = new (
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

                    if (!dictionary.TryAdd (key, (T)value))
                    {
                        throw new JsonException ($"Duplicate key '{key}' in dictionary.");
                    }
                }
            }
            else if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }
        }

        return dictionary;
    }

    public override void Write (Utf8JsonWriter writer, ConcurrentDictionary<string, T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray ();

        foreach (KeyValuePair<string, T> item in value)
        {
            writer.WriteStartObject ();

            writer.WritePropertyName (item.Key);
            JsonSerializer.Serialize (writer, item.Value, typeof (T), TuiSerializerContext.Instance);
            writer.WriteEndObject ();
        }

        writer.WriteEndArray ();
    }
}
