using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Overlays nested MEC key-binding dictionaries onto
///     <see cref="Application.DefaultKeyBindings"/>, <see cref="View.DefaultKeyBindings"/>,
///     and <see cref="View.ViewKeyBindings"/>. Unmentioned commands keep hard-coded defaults.
/// </summary>
internal static class KeyBindingConfiguration
{
    /// <summary>Applies key-binding sections from <paramref name="config"/> to the static facades.</summary>
    public static void Apply (IConfiguration config)
    {
        Dictionary<Command, PlatformKeyBinding>? appOverlay =
            DeserializeCommandBindings (config.GetSection ("Application").GetSection ("DefaultKeyBindings"));

        if (appOverlay is not null)
        {
            Application.DefaultKeyBindings = OverlayCommandBindings (Application.DefaultKeyBindings, appOverlay);
        }

        Dictionary<Command, PlatformKeyBinding>? viewOverlay =
            DeserializeCommandBindings (config.GetSection ("View").GetSection ("DefaultKeyBindings"));

        if (viewOverlay is not null)
        {
            View.DefaultKeyBindings = OverlayCommandBindings (View.DefaultKeyBindings, viewOverlay);
        }

        OverlayViewKeyBindings (DeserializeViewKeyBindings (config.GetSection ("View").GetSection ("ViewKeyBindings")));
    }

    private static Dictionary<Command, PlatformKeyBinding>? OverlayCommandBindings (
        Dictionary<Command, PlatformKeyBinding>? target,
        Dictionary<Command, PlatformKeyBinding>? overlay)
    {
        if (overlay is null || overlay.Count == 0)
        {
            return target;
        }

        Dictionary<Command, PlatformKeyBinding> merged = target is { } existing ? new (existing) : [];

        foreach (KeyValuePair<Command, PlatformKeyBinding> pair in overlay)
        {
            merged [pair.Key] = pair.Value;
        }

        return merged;
    }

    private static void OverlayViewKeyBindings (Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? overlay)
    {
        if (overlay is null || overlay.Count == 0)
        {
            return;
        }

        Dictionary<string, Dictionary<Command, PlatformKeyBinding>> merged =
            View.ViewKeyBindings is { } existing
                ? new (existing, StringComparer.OrdinalIgnoreCase)
                : new (StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, Dictionary<Command, PlatformKeyBinding>> typePair in overlay)
        {
            if (!merged.TryGetValue (typePair.Key, out Dictionary<Command, PlatformKeyBinding>? typeBindings))
            {
                merged [typePair.Key] = new (typePair.Value);

                continue;
            }

            Dictionary<Command, PlatformKeyBinding> next = new (typeBindings);

            foreach (KeyValuePair<Command, PlatformKeyBinding> commandPair in typePair.Value)
            {
                next [commandPair.Key] = commandPair.Value;
            }

            merged [typePair.Key] = next;
        }

        View.ViewKeyBindings = merged;
    }

    private static Dictionary<Command, PlatformKeyBinding>? DeserializeCommandBindings (IConfigurationSection section)
    {
        if (ConfigurationSectionJson.ToJson (section) is not JsonObject obj || obj.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<Command, PlatformKeyBinding>> (obj.ToJsonString (), TuiSerializerContext.Instance.Options);
        }
        catch (JsonException ex)
        {
            TuiJsonErrors.Add ($"Key bindings ({section.Path}): {ex.Message}");

            return null;
        }
    }

    private static Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? DeserializeViewKeyBindings (IConfigurationSection section)
    {
        if (ConfigurationSectionJson.ToJson (section) is not JsonObject obj || obj.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<Command, PlatformKeyBinding>>> (obj.ToJsonString (), TuiSerializerContext.Instance.Options);
        }
        catch (JsonException ex)
        {
            TuiJsonErrors.Add ($"ViewKeyBindings ({section.Path}): {ex.Message}");

            return null;
        }
    }
}
