using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Serializes and deserializes <see cref="MouseFlags"/> arrays as JSON string arrays
///     (e.g. <c>["LeftButtonPressed+Shift", "LeftButtonReleased"]</c>).
/// </summary>
public class MouseFlagsArrayJsonConverter : JsonConverter<MouseFlags []?>
{
    /// <inheritdoc/>
    public override MouseFlags []? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException ("Expected start of array for MouseFlags[].");
        }

        List<MouseFlags> mouseFlagsList = [];

        while (reader.Read ())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return mouseFlagsList.ToArray ();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException ($"Expected string token in MouseFlags array, got {reader.TokenType}.");
            }

            string mouseFlagsString = reader.GetString ()!;
            mouseFlagsString = mouseFlagsString.Replace ("+", ", ").Replace ("|", ", ");

            mouseFlagsList.Add (Enum.TryParse (mouseFlagsString, true, out MouseFlags mouseFlags) ? mouseFlags : MouseFlags.None);
        }

        throw new JsonException ("Unexpected end of JSON while reading MouseFlags array.");
    }

    /// <inheritdoc/>
    public override void Write (Utf8JsonWriter writer, MouseFlags []? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue ();

            return;
        }

        writer.WriteStartArray ();

        foreach (MouseFlags mouseFlags in value)
        {
            writer.WriteStringValue (mouseFlags.ToString ().Replace (", ", "+"));
        }

        writer.WriteEndArray ();
    }
}
