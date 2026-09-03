using System.Text.Json.Nodes;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Overlays nested JSON key-binding dictionaries onto
///     <see cref="Application.DefaultKeyBindings"/>, <see cref="View.DefaultKeyBindings"/>,
///     and <see cref="View.ViewKeyBindings"/>. Unmentioned commands keep hard-coded defaults.
///     Each apply first reverts the previous configuration overlay, so a binding removed from
///     configuration regains its prior value on reload instead of persisting until restart.
/// </summary>
internal static class KeyBindingConfiguration
{
    private static Dictionary<Command, PlatformKeyBinding?>? _appPrior;
    private static Dictionary<Command, PlatformKeyBinding?>? _viewPrior;
    private static Dictionary<string, Dictionary<Command, PlatformKeyBinding?>>? _viewTypesPrior;

    /// <summary>Applies key-binding sections from the raw-JSON merged source view to the static facades.</summary>
    public static void Apply (JsonObject mergedJson)
    {
        Application.DefaultKeyBindings = OverlayTracked (
                                                         Application.DefaultKeyBindings,
                                                         BindCommands (mergedJson, "Application"),
                                                         ref _appPrior);

        View.DefaultKeyBindings = OverlayTracked (
                                                  View.DefaultKeyBindings,
                                                  BindCommands (mergedJson, "View"),
                                                  ref _viewPrior);

        OverlayViewKeyBindings (BindViewTypes (mergedJson));
    }

    /// <summary>INTERNAL: Captures the overlay-revert tracking state (for test snapshot/restore).</summary>
    internal static (Dictionary<Command, PlatformKeyBinding?>?, Dictionary<Command, PlatformKeyBinding?>?,
        Dictionary<string, Dictionary<Command, PlatformKeyBinding?>>?) SnapshotOverlayTracking () =>
        (_appPrior is null ? null : new (_appPrior),
         _viewPrior is null ? null : new (_viewPrior),
         _viewTypesPrior?.ToDictionary (p => p.Key, p => new Dictionary<Command, PlatformKeyBinding?> (p.Value), StringComparer.OrdinalIgnoreCase));

    /// <summary>INTERNAL: Restores overlay-revert tracking state captured by <see cref="SnapshotOverlayTracking"/>.</summary>
    internal static void RestoreOverlayTracking (
        (Dictionary<Command, PlatformKeyBinding?>?, Dictionary<Command, PlatformKeyBinding?>?,
            Dictionary<string, Dictionary<Command, PlatformKeyBinding?>>?) state)
    {
        (_appPrior, _viewPrior, _viewTypesPrior) = state;
    }

    private static Dictionary<Command, PlatformKeyBinding>? BindCommands (JsonObject mergedJson, string sectionName) =>
        ConfigurationSectionJson.Deserialize<Dictionary<Command, PlatformKeyBinding>> (
                                                                                       mergedJson [sectionName]? ["DefaultKeyBindings"] as JsonObject,
                                                                                       $"Key bindings ({sectionName}:DefaultKeyBindings)");

    private static Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? BindViewTypes (JsonObject mergedJson) =>
        ConfigurationSectionJson.Deserialize<Dictionary<string, Dictionary<Command, PlatformKeyBinding>>> (
                                                                                                            mergedJson ["View"]? ["ViewKeyBindings"] as JsonObject,
                                                                                                            "ViewKeyBindings (View:ViewKeyBindings)");

    /// <summary>
    ///     Reverts the previous configuration overlay recorded in <paramref name="prior"/>, then overlays
    ///     <paramref name="overlay"/> and records what each overlaid command replaced. App-code mutations to
    ///     commands the configuration does not mention are preserved.
    /// </summary>
    private static Dictionary<Command, PlatformKeyBinding> OverlayTracked (
        Dictionary<Command, PlatformKeyBinding>? current,
        Dictionary<Command, PlatformKeyBinding>? overlay,
        ref Dictionary<Command, PlatformKeyBinding?>? prior)
    {
        Dictionary<Command, PlatformKeyBinding> merged = current is { } existing ? new (existing) : [];

        if (prior is { })
        {
            foreach (KeyValuePair<Command, PlatformKeyBinding?> pair in prior)
            {
                if (pair.Value is { } previous)
                {
                    merged [pair.Key] = previous;
                }
                else
                {
                    merged.Remove (pair.Key);
                }
            }
        }

        prior = null;

        if (overlay is null || overlay.Count == 0)
        {
            return merged;
        }

        prior = [];

        foreach (KeyValuePair<Command, PlatformKeyBinding> pair in overlay)
        {
            prior [pair.Key] = merged.TryGetValue (pair.Key, out PlatformKeyBinding? replaced) ? replaced : null;
            merged [pair.Key] = pair.Value;
        }

        return merged;
    }

    private static void OverlayViewKeyBindings (Dictionary<string, Dictionary<Command, PlatformKeyBinding>>? overlay)
    {
        Dictionary<string, Dictionary<Command, PlatformKeyBinding>> merged =
            View.ViewKeyBindings is { } existing
                ? existing.ToDictionary (p => p.Key, p => new Dictionary<Command, PlatformKeyBinding> (p.Value), StringComparer.OrdinalIgnoreCase)
                : new (StringComparer.OrdinalIgnoreCase);

        if (_viewTypesPrior is { })
        {
            foreach (KeyValuePair<string, Dictionary<Command, PlatformKeyBinding?>> typePair in _viewTypesPrior)
            {
                if (!merged.TryGetValue (typePair.Key, out Dictionary<Command, PlatformKeyBinding>? typeBindings))
                {
                    continue;
                }

                foreach (KeyValuePair<Command, PlatformKeyBinding?> pair in typePair.Value)
                {
                    if (pair.Value is { } previous)
                    {
                        typeBindings [pair.Key] = previous;
                    }
                    else
                    {
                        typeBindings.Remove (pair.Key);
                    }
                }

                if (typeBindings.Count == 0)
                {
                    merged.Remove (typePair.Key);
                }
            }
        }

        _viewTypesPrior = null;

        if (overlay is null || overlay.Count == 0)
        {
            View.ViewKeyBindings = merged.Count == 0 ? null : merged;

            return;
        }

        _viewTypesPrior = new (StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, Dictionary<Command, PlatformKeyBinding>> typePair in overlay)
        {
            if (!merged.TryGetValue (typePair.Key, out Dictionary<Command, PlatformKeyBinding>? typeBindings))
            {
                typeBindings = [];
                merged [typePair.Key] = typeBindings;
            }

            Dictionary<Command, PlatformKeyBinding?> priors = [];

            foreach (KeyValuePair<Command, PlatformKeyBinding> pair in typePair.Value)
            {
                priors [pair.Key] = typeBindings.TryGetValue (pair.Key, out PlatformKeyBinding? replaced) ? replaced : null;
                typeBindings [pair.Key] = pair.Value;
            }

            _viewTypesPrior [typePair.Key] = priors;
        }

        View.ViewKeyBindings = merged;
    }
}
