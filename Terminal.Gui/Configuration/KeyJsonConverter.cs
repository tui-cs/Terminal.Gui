using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>Support for <see cref="Key"/> in JSON in the form of "Ctrl-X" or "Alt-Shift-F1".</summary>
public class KeyJsonConverter : JsonConverter<Key>
{
    /// <inheritdoc/>
    public override Key Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Key.TryParse (reader.GetString ()!, out Key key) ? key : Key.Empty;
    }

    /// <inheritdoc/>
    public override void Write (Utf8JsonWriter writer, Key value, JsonSerializerOptions options) { writer.WriteStringValue (value.ToString ()); }
}
