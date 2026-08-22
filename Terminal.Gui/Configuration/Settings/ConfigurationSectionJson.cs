using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Rebuilds a JSON tree from an MEC section so STJ can deserialize nested dictionaries and arrays.
///     MEC stores JSON arrays as numbered children (<c>"0"</c>, <c>"1"</c>, …); those become <see cref="JsonArray"/>.
/// </summary>
internal static class ConfigurationSectionJson
{
    /// <summary>Converts <paramref name="section"/> to a <see cref="JsonNode"/> tree.</summary>
    public static JsonNode? ToJson (IConfigurationSection section)
    {
        List<IConfigurationSection> children = [.. section.GetChildren ()];

        if (children.Count == 0)
        {
            return section.Value is null ? null : JsonValue.Create (section.Value);
        }

        if (IsIndexArray (children))
        {
            JsonArray array = [];

            foreach (IConfigurationSection child in children.OrderBy (static c => int.Parse (c.Key, CultureInfo.InvariantCulture)))
            {
                array.Add (ToJson (child));
            }

            return array;
        }

        JsonObject obj = [];

        foreach (IConfigurationSection child in children)
        {
            JsonNode? nested = ToJson (child);

            if (nested is not null)
            {
                obj [child.Key] = nested;
            }
        }

        return obj;
    }

    private static bool IsIndexArray (List<IConfigurationSection> children)
    {
        foreach (IConfigurationSection child in children)
        {
            if (!int.TryParse (child.Key, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }
        }

        return true;
    }
}
