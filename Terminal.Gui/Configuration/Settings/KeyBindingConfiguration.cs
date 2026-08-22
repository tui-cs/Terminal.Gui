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
        Dictionary<Command, PlatformKeyBinding>? appOverlay = BindCommands (config, "Application:DefaultKeyBindings");

        if (appOverlay is not null)
        {
            Application.DefaultKeyBindings = Overlay (Application.DefaultKeyBindings, appOverlay);
        }

        Dictionary<Command, PlatformKeyBinding>? viewOverlay = BindCommands (config, "View:DefaultKeyBindings");

        if (viewOverlay is not null)
        {
            View.DefaultKeyBindings = Overlay (View.DefaultKeyBindings, viewOverlay);
        }

        OverlayViewKeyBindings (BindViewTypes (config));
    }

    private static Dictionary<Command, PlatformKeyBinding>? BindCommands (IConfiguration config, string path) =>
        ConfigurationSectionJson.Deserialize<Dictionary<Command, PlatformKeyBinding>> (config.GetSection (path), "Key bindings");

    private static Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? BindViewTypes (IConfiguration config) =>
        ConfigurationSectionJson.Deserialize<Dictionary<string, Dictionary<Command, PlatformKeyBinding>>> (
                                                                                                           config.GetSection ("View:ViewKeyBindings"), "ViewKeyBindings");

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

        foreach (KeyValuePair<string, Dictionary<Command, PlatformKeyBinding>> pair in overlay)
        {
            Dictionary<Command, PlatformKeyBinding>? next = Overlay (merged.GetValueOrDefault (pair.Key), pair.Value);

            if (next is null)
            {
                continue;
            }

            merged [pair.Key] = next;
        }

        View.ViewKeyBindings = merged;
    }

    private static Dictionary<Command, PlatformKeyBinding>? Overlay (
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
}
