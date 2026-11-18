using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     JSON converter for <see cref="TraceCategory"/> flags enum.
///     Supports both string array format (["Command", "Mouse"]) and integer format (6).
/// </summary>
internal class TraceCategoryJsonConverter : JsonConverter<TraceCategory>
{
    public override TraceCategory Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle numeric format: 6
            return (TraceCategory)reader.GetInt32 ();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            // Handle single string: "Command"
            string value = reader.GetString ()!;

            if (Enum.TryParse (value, true, out TraceCategory result))
            {
                return result;
            }

            throw new JsonException ($"Invalid TraceCategory value: '{value}'");
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Handle array format: ["Command", "Mouse"]
            var result = TraceCategory.None;

            while (reader.Read ())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException ($"Unexpected token type in TraceCategory array: {reader.TokenType}");
                }
                string value = reader.GetString ()!;

                if (Enum.TryParse (value, true, out TraceCategory category))
                {
                    result |= category;
                }
                else
                {
                    throw new JsonException ($"Invalid TraceCategory value in array: '{value}'");
                }
            }

            return result;
        }

        throw new JsonException ($"Unexpected token type for TraceCategory: {reader.TokenType}");
    }

    public override void Write (Utf8JsonWriter writer, TraceCategory value, JsonSerializerOptions options)
    {
        if (value == TraceCategory.None)
        {
            writer.WriteStringValue ("None");

            return;
        }

        if (value == TraceCategory.All)
        {
            writer.WriteStringValue ("All");

            return;
        }

        // Check if it's a single flag (power of 2)
        if (((int)value & ((int)value - 1)) == 0)
        {
            // Single flag - write as string
            writer.WriteStringValue (value.ToString ());

            return;
        }

        // Multiple flags - write as array
        writer.WriteStartArray ();

        foreach (TraceCategory flag in Enum.GetValues<TraceCategory> ())
        {
            if (flag != TraceCategory.None && flag != TraceCategory.All && value.HasFlag (flag))
            {
                writer.WriteStringValue (flag.ToString ());
            }
        }

        writer.WriteEndArray ();
    }
}
