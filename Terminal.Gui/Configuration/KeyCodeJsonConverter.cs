#nullable disable
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

internal class KeyCodeJsonConverter : JsonConverter<KeyCode>
{
    public override KeyCode Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string propertyName = string.Empty;

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var key = KeyCode.Null;

            Dictionary<string, KeyCode> modifierDict =
                new (StringComparer.InvariantCultureIgnoreCase)
                {
                    { "Shift", KeyCode.ShiftMask }, { "Ctrl", KeyCode.CtrlMask }, { "Alt", KeyCode.AltMask }
                };

            List<KeyCode> modifiers = new ();

            while (reader.Read ())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString ();
                    reader.Read ();

                    switch (propertyName!.ToLowerInvariant ())
                    {
                        case "key":
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                if (Enum.TryParse (reader.GetString (), false, out key))
                                {
                                    break;
                                }

                                // The enum uses "D0..D9" for the number keys
                                if (Enum.TryParse (reader.GetString ()!.TrimStart ('D', 'd'), false, out key))
                                {
                                    break;
                                }

                                if (key == KeyCode.Null)
                                {
                                    throw new JsonException (
                                                             $"{propertyName}: \"{reader.GetString ()}\" is not a valid Key."
                                                            );
                                }
                            }
                            else if (reader.TokenType == JsonTokenType.Number)
                            {
                                try
                                {
                                    key = (KeyCode)reader.GetInt32 ();
                                }
                                catch (InvalidOperationException ioe)
                                {
                                    throw new JsonException ($"{propertyName}: Error parsing Key value: {ioe.Message}", ioe);
                                }
                                catch (FormatException ioe)
                                {
                                    throw new JsonException ($"{propertyName}: Error parsing Key value: {ioe.Message}", ioe);
                                }
                            }

                            break;

                        case "modifiers":
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                while (reader.Read ())
                                {
                                    if (reader.TokenType == JsonTokenType.EndArray)
                                    {
                                        break;
                                    }

                                    string mod = reader.GetString ();

                                    try
                                    {
                                        modifiers.Add (modifierDict [mod]);
                                    }
                                    catch (KeyNotFoundException e)
                                    {
                                        throw new JsonException ($"{propertyName}: \"{mod}\" is not a valid modifier.", e);
                                    }
                                }
                            }
                            else
                            {
                                throw new JsonException (
                                                         $"{propertyName}: Expected an array of modifiers, but got \"{reader.TokenType}\"."
                                                        );
                            }

                            break;

                        default:
                            throw new JsonException ($"{propertyName}: Unexpected Key property.");
                    }
                }
            }

            foreach (KeyCode modifier in modifiers)
            {
                key |= modifier;
            }

            return key;
        }

        throw new JsonException ($"{propertyName}: Unexpected StartObject token when parsing Key: {reader.TokenType}.");
    }

    public override void Write (Utf8JsonWriter writer, KeyCode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject ();

        var keyName = (value & ~KeyCode.CtrlMask & ~KeyCode.ShiftMask & ~KeyCode.AltMask).ToString ();

        writer.WriteString ("Key", keyName);

        Dictionary<string, KeyCode> modifierDict = new ()
        {
            { "Shift", KeyCode.ShiftMask }, { "Ctrl", KeyCode.CtrlMask }, { "Alt", KeyCode.AltMask }
        };

        List<string> modifiers = new ();

        foreach (KeyValuePair<string, KeyCode> pair in modifierDict)
        {
            if ((value & pair.Value) == pair.Value)
            {
                modifiers.Add (pair.Key);
            }
        }

        if (modifiers.Count > 0)
        {
            writer.WritePropertyName ("Modifiers");
            writer.WriteStartArray ();

            foreach (string modifier in modifiers)
            {
                writer.WriteStringValue (modifier);
            }

            writer.WriteEndArray ();
        }

        writer.WriteEndObject ();
    }
}
