// Grok - grok-4.6
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Holds the <see cref="Drawing.Scheme"/>s that define the <see cref="Attribute"/>s views use to render.
///     A Scheme maps <see cref="Drawing.VisualRole"/>s to <see cref="Attribute"/>s.
/// </summary>
public sealed class SchemeManager : ISchemeManager
{
    private static readonly Lock _schemesLock = new ();
    private static Dictionary<string, Scheme?> _schemes = HardCodedDictionary ();

    /// <summary>INTERNAL: Hard-coded schemes used before configuration is applied and as the overlay base.</summary>
    internal static ImmutableSortedDictionary<string, Scheme?> GetHardCodedSchemes ()
    {
        ImmutableSortedDictionary<string, Scheme> hardCoded = Scheme.GetHardCodedSchemes ()!;

        return hardCoded.ToImmutableSortedDictionary (
                                                      kv => kv.Key,
                                                      kv => (Scheme?)kv.Value,
                                                      StringComparer.InvariantCultureIgnoreCase);
    }

    private static Dictionary<string, Scheme?> HardCodedDictionary () =>
        GetHardCodedSchemes ().ToDictionary (StringComparer.InvariantCultureIgnoreCase);

    /// <summary>Gets the dictionary of defined schemes for the current theme.</summary>
    public static Dictionary<string, Scheme?> Schemes
    {
        get => GetSchemes ();
        private set => ReplaceSchemes (value);
    }

    internal static Dictionary<string, Scheme?> GetSchemes ()
    {
        lock (_schemesLock)
        {
            return _schemes;
        }
    }

    /// <summary>Replaces the current theme's scheme dictionary (called from configuration apply).</summary>
    internal static void ReplaceSchemes (Dictionary<string, Scheme?> schemes)
    {
        lock (_schemesLock)
        {
            _schemes = new (schemes, StringComparer.InvariantCultureIgnoreCase);
        }
    }

    /// <summary>
    ///     Adds a new <see cref="Scheme"/>. If the name already exists, it is updated.
    /// </summary>
    public static void AddScheme (string schemeName, Scheme scheme)
    {
        lock (_schemesLock)
        {
            _schemes [schemeName] = scheme;
        }
    }

    /// <summary>Removes a previously added (non-built-in) scheme.</summary>
    public static void RemoveScheme (string schemeName)
    {
        if (SchemeNameToSchemes (schemeName) is { })
        {
            throw new InvalidOperationException ($@"{schemeName}: Cannot remove a built-in Scheme.");
        }

        lock (_schemesLock)
        {
            if (!_schemes.Remove (schemeName))
            {
                throw new InvalidOperationException ($@"{schemeName}: Does not exist in Schemes.");
            }
        }
    }

    /// <summary>Gets the <see cref="Scheme"/> for a built-in <see cref="Schemes"/> value.</summary>
    public static Scheme GetScheme (Schemes schemeName)
    {
        string? schemeNameString = SchemesToSchemeName (schemeName);

        return schemeNameString is null
                   ? throw new ArgumentException ($"Invalid scheme name: {schemeName}")
                   : GetSchemesForCurrentTheme () [schemeNameString]!;
    }

    /// <summary>Gets the <see cref="Scheme"/> for the specified name.</summary>
    public static Scheme GetScheme (string schemeName) => GetSchemesForCurrentTheme () [schemeName]!;

    /// <summary>Attempts to get a scheme without throwing.</summary>
    public static bool TryGetScheme (string schemeName, [NotNullWhen (true)] out Scheme? scheme)
    {
        lock (_schemesLock)
        {
            if (_schemes.TryGetValue (schemeName, out Scheme? s) && s is { })
            {
                scheme = s;

                return true;
            }

            scheme = null;

            return false;
        }
    }

    /// <summary>Gets the name of a built-in <see cref="Schemes"/> value.</summary>
    public static string? SchemesToSchemeName (Schemes schemeName) => Enum.GetName (typeof (Schemes), schemeName);

    /// <summary>Converts a string to a built-in scheme name, or <see langword="null"/>.</summary>
    public static string? SchemeNameToSchemes (string schemeName)
    {
        if (Enum.TryParse (typeof (Schemes), schemeName, out object? value))
        {
            return value.ToString ();
        }

        return null;
    }

    /// <summary>Gets the dictionary of schemes for the active theme.</summary>
    public static Dictionary<string, Scheme?> GetSchemesForCurrentTheme ()
    {
        lock (_schemesLock)
        {
            return _schemes;
        }
    }

    /// <summary>Gets the names of the schemes in the current theme.</summary>
    public static ImmutableList<string> GetSchemeNames ()
    {
        lock (_schemesLock)
        {
            return _schemes.Keys.ToImmutableList ();
        }
    }

    IReadOnlyList<string> ISchemeManager.SchemeNames => GetSchemeNames ();

    Scheme? ISchemeManager.GetScheme (string name) => TryGetScheme (name, out Scheme? scheme) ? scheme : null;

    void ISchemeManager.AddScheme (string name, Scheme scheme) => AddScheme (name, scheme);

    private static HashSet<string> _configSourcedSchemeNames = new (StringComparer.InvariantCultureIgnoreCase);

    /// <summary>INTERNAL: Gets a copy of the scheme names the last configuration apply sourced (for tests).</summary>
    internal static HashSet<string> GetConfigSourcedSchemeNames ()
    {
        lock (_schemesLock)
        {
            return new (_configSourcedSchemeNames, StringComparer.InvariantCultureIgnoreCase);
        }
    }

    /// <summary>INTERNAL: Replaces the config-sourced scheme name set (for tests).</summary>
    internal static void SetConfigSourcedSchemeNames (IEnumerable<string> names)
    {
        lock (_schemesLock)
        {
            _configSourcedSchemeNames = new (names, StringComparer.InvariantCultureIgnoreCase);
        }
    }

    internal static void LoadToHardCodedDefaults ()
    {
        ReplaceSchemes (HardCodedDictionary ());
        SetConfigSourcedSchemeNames ([]);
    }

    /// <summary>
    ///     Publishes the schemes for <paramref name="themeName"/>: hard-coded defaults, plus schemes added
    ///     at runtime via <see cref="AddScheme"/>, plus the root <c>Schemes</c> section deep-merged with the
    ///     theme's <c>Schemes</c> overlay (the root-then-overlay contract every ThemeScope section follows).
    /// </summary>
    internal static void ApplyFromConfiguration (IConfiguration config, string themeName)
    {
        Dictionary<string, Scheme?> next = HardCodedDictionary ();

        // Preserve schemes added at runtime via AddScheme: anything currently present that is neither
        // hard-coded nor sourced from configuration by a prior apply.
        lock (_schemesLock)
        {
            foreach (KeyValuePair<string, Scheme?> pair in _schemes)
            {
                if (next.ContainsKey (pair.Key) || _configSourcedSchemeNames.Contains (pair.Key))
                {
                    continue;
                }

                next [pair.Key] = pair.Value;
            }
        }

        Dictionary<string, JsonObject> merged = new (StringComparer.InvariantCultureIgnoreCase);
        MergeSchemesSection (merged, config.GetSection ("Schemes"));
        IConfigurationSection? named = ThemeCatalog.Find (config, themeName);

        if (named is not null)
        {
            MergeSchemesSection (merged, named.GetSection ("Schemes"));
        }

        HashSet<string> configSourced = new (StringComparer.InvariantCultureIgnoreCase);

        foreach (KeyValuePair<string, JsonObject> pair in merged)
        {
            Scheme? parsed = TryBindScheme (pair.Key, pair.Value);

            if (parsed is null)
            {
                continue;
            }

            next [pair.Key] = parsed;
            configSourced.Add (pair.Key);
        }

        ReplaceSchemes (next);
        SetConfigSourcedSchemeNames (configSourced);
    }

    private static void MergeSchemesSection (Dictionary<string, JsonObject> merged, IConfigurationSection schemesSection)
    {
        foreach (IConfigurationSection schemeChild in schemesSection.GetChildren ())
        {
            if (SectionToJson (schemeChild) is not JsonObject obj || obj.Count == 0)
            {
                continue;
            }

            if (merged.TryGetValue (schemeChild.Key, out JsonObject? existing))
            {
                DeepMerge (existing, obj);

                continue;
            }

            merged [schemeChild.Key] = obj;
        }
    }

    private static void DeepMerge (JsonObject target, JsonObject source)
    {
        foreach (KeyValuePair<string, JsonNode?> pair in source)
        {
            if (target [pair.Key] is JsonObject existingChild && pair.Value is JsonObject incomingChild)
            {
                DeepMerge (existingChild, incomingChild);

                continue;
            }

            target [pair.Key] = pair.Value is null ? null : JsonNode.Parse (pair.Value.ToJsonString ());
        }
    }

    private static Scheme? TryBindScheme (string name, JsonObject obj)
    {
        try
        {
            return JsonSerializer.Deserialize (obj.ToJsonString (), TuiSerializerContext.Instance.Scheme);
        }
        catch (JsonException ex)
        {
            // One bad value must not silently revert the user's whole scheme to hard-coded colors
            // with zero diagnostics; the error is printed at shutdown.
            TuiJsonErrors.Add ($"Scheme \"{name}\": {ex.Message}");

            return null;
        }
    }

    private static JsonNode? SectionToJson (IConfigurationSection section)
    {
        List<IConfigurationSection> children = [.. section.GetChildren ()];

        if (children.Count == 0)
        {
            return section.Value is null ? null : JsonValue.Create (section.Value);
        }

        JsonObject obj = [];

        foreach (IConfigurationSection child in children)
        {
            JsonNode? nested = SectionToJson (child);

            if (nested is not null)
            {
                obj [child.Key] = nested;
            }
        }

        return obj;
    }
}
