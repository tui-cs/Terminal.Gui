using System.Text.Json.Nodes;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Deep-merges JSON objects: object values merge recursively; any other value — including arrays —
///     replaces the target wholesale. This gives configuration sources atomic array overrides (a
///     higher-priority source's shorter array fully replaces a lower-priority source's array), unlike
///     MEC's per-index key merge.
/// </summary>
internal static class JsonMerge
{
    /// <summary>Merges <paramref name="source"/> into <paramref name="target"/>. Source nodes are cloned.</summary>
    public static void DeepMerge (JsonObject target, JsonObject source)
    {
        foreach (KeyValuePair<string, JsonNode?> pair in source)
        {
            if (target [pair.Key] is JsonObject existing && pair.Value is JsonObject incoming)
            {
                DeepMerge (existing, incoming);

                continue;
            }

            target [pair.Key] = pair.Value?.DeepClone ();
        }
    }
}
