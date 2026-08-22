using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Rebuilds a JSON tree from an MEC section so STJ can deserialize nested dictionaries and arrays.
///     MEC stores JSON arrays as numbered children (<c>"0"</c>, <c>"1"</c>, …); those become <see cref="JsonArray"/>
///     only when the keys are exactly <c>0..n-1</c>.
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

        if (TryAsIndexArray (children, out IConfigurationSection []? ordered))
        {
            JsonArray array = [];

            foreach (IConfigurationSection child in ordered)
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

    /// <summary>
    ///     Deserializes <paramref name="section"/> as <typeparamref name="T"/> via STJ.
    ///     Empty sections return <see langword="null"/>. JSON errors are collected in <see cref="TuiJsonErrors"/>.
    /// </summary>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "T is a JsonSerializable type registered on SourceGenerationContext.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "T is a JsonSerializable type registered on SourceGenerationContext.")]
    public static T? Deserialize<T> (IConfigurationSection section, string errorLabel) where T : class
    {
        if (ToJson (section) is not JsonObject obj || obj.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T> (obj.ToJsonString (), TuiSerializerContext.Instance.Options);
        }
        catch (JsonException ex)
        {
            TuiJsonErrors.Add ($"{errorLabel} ({section.Path}): {ex.Message}");

            return null;
        }
    }

    private static bool TryAsIndexArray (List<IConfigurationSection> children, [NotNullWhen (true)] out IConfigurationSection []? ordered)
    {
        IConfigurationSection [] slots = new IConfigurationSection [children.Count];

        foreach (IConfigurationSection child in children)
        {
            if (!int.TryParse (child.Key, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                || index < 0
                || index >= slots.Length
                || slots [index] is not null)
            {
                ordered = null;

                return false;
            }

            slots [index] = child;
        }

        ordered = slots;

        return true;
    }
}
