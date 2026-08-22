// Grok - grok-4.6
using System.Text.Json.Nodes;

namespace Terminal.Gui.Tools.MigrateConfig;

/// <summary>
///     Transforms a pre-MEC Terminal.Gui config.json (flat-key, array-themes shape)
///     into the nested MEC-native shape consumed by <c>TuiConfigurationBuilder</c>.
/// </summary>
public static class ConfigJsonMigrator
{
    /// <summary>
    ///     Migrates a top-level configuration object. Nested objects are walked recursively.
    /// </summary>
    public static JsonObject MigrateObject (JsonObject src)
    {
        JsonObject result = [];

        foreach (KeyValuePair<string, JsonNode?> pair in src)
        {
            JsonNode? value = pair.Value is null ? null : Clone (pair.Value);
            JsonNode? migratedValue = MigrateValue (pair.Key, value);

            MergeDottedKey (result, pair.Key, migratedValue);
        }

        return result;
    }

    internal static JsonNode? MigrateValue (string keyName, JsonNode? value)
    {
        if (value is JsonObject obj)
        {
            return MigrateObject (obj);
        }

        if (value is JsonArray arr && IsArrayDictKey (keyName) && (arr.Count == 0 || IsArrayOfSingleKeyObjects (arr)))
        {
            JsonObject dict = [];

            foreach (JsonNode? item in arr)
            {
                if (item is not JsonObject itemObj)
                {
                    continue;
                }

                foreach (KeyValuePair<string, JsonNode?> entry in itemObj)
                {
                    JsonNode? entryValue = entry.Value is null ? null : MigrateValue (entry.Key, Clone (entry.Value));
                    dict [entry.Key] = entryValue;
                }
            }

            return dict;
        }

        return value;
    }

    private static bool IsArrayDictKey (string keyName) =>
        keyName is "Themes" or "Schemes";

    private static bool IsArrayOfSingleKeyObjects (JsonArray arr)
    {
        if (arr.Count == 0)
        {
            return false;
        }

        foreach (JsonNode? item in arr)
        {
            if (item is not JsonObject obj || obj.Count != 1)
            {
                return false;
            }
        }

        return true;
    }

    private static void MergeDottedKey (JsonObject target, string key, JsonNode? value)
    {
        if (!key.Contains ('.'))
        {
            target [key] = value;

            return;
        }

        string [] parts = key.Split ('.');
        JsonObject cursor = target;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            string part = parts [i];

            if (cursor [part] is not JsonObject next)
            {
                next = [];
                cursor [part] = next;
            }

            cursor = next;
        }

        cursor [parts [^1]] = value;
    }

    private static JsonNode Clone (JsonNode node) =>
        JsonNode.Parse (node.ToJsonString ())!;
}
