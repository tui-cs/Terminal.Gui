// Grok - grok-4.6
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

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
    ///     Schemes added or updated here survive theme switches and configuration re-application.
    /// </summary>
    public static void AddScheme (string schemeName, Scheme scheme)
    {
        lock (_schemesLock)
        {
            _schemes [schemeName] = scheme;
            _runtimeSchemes [schemeName] = scheme;
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

            _runtimeSchemes.Remove (schemeName);
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

    private static Dictionary<string, Scheme?> _runtimeSchemes = new (StringComparer.InvariantCultureIgnoreCase);

    /// <summary>INTERNAL: Gets a copy of the schemes added or updated at runtime via <see cref="AddScheme"/> (for tests).</summary>
    internal static Dictionary<string, Scheme?> GetRuntimeSchemes ()
    {
        lock (_schemesLock)
        {
            return new (_runtimeSchemes, StringComparer.InvariantCultureIgnoreCase);
        }
    }

    /// <summary>INTERNAL: Replaces the runtime-added scheme dictionary (for tests).</summary>
    internal static void SetRuntimeSchemes (Dictionary<string, Scheme?> schemes)
    {
        lock (_schemesLock)
        {
            _runtimeSchemes = new (schemes, StringComparer.InvariantCultureIgnoreCase);
        }
    }

    internal static void LoadToHardCodedDefaults ()
    {
        ReplaceSchemes (HardCodedDictionary ());
        SetRuntimeSchemes (new (StringComparer.InvariantCultureIgnoreCase));
    }

    /// <summary>
    ///     Publishes the schemes for <paramref name="canonicalThemeName"/> from the raw-JSON merged source
    ///     view: hard-coded defaults, overlaid by the root <c>Schemes</c> section deep-merged with the theme's
    ///     <c>Schemes</c> overlay (the root-then-overlay contract every ThemeScope section follows), overlaid
    ///     by schemes the app added or updated at runtime via <see cref="AddScheme"/> (app wins until changed,
    ///     matching the documented "If the name already exists, it is updated" contract).
    /// </summary>
    internal static void ApplyFromConfiguration (JsonObject mergedJson, string? canonicalThemeName)
    {
        Dictionary<string, Scheme?> next = HardCodedDictionary ();

        JsonObject combined = [];

        if (mergedJson ["Schemes"] is JsonObject rootSchemes)
        {
            JsonMerge.DeepMerge (combined, rootSchemes);
        }

        if (canonicalThemeName is { } && mergedJson ["Themes"]? [canonicalThemeName]? ["Schemes"] is JsonObject themeSchemes)
        {
            JsonMerge.DeepMerge (combined, themeSchemes);
        }

        foreach (KeyValuePair<string, JsonNode?> pair in combined)
        {
            if (pair.Value is not JsonObject schemeJson || schemeJson.Count == 0)
            {
                continue;
            }

            // One bad value must not silently revert the user's whole scheme to hard-coded colors
            // with zero diagnostics; Deserialize collects the error for printing at shutdown.
            Scheme? parsed = ConfigurationSectionJson.Deserialize<Scheme> (schemeJson, $"Scheme \"{pair.Key}\"");

            if (parsed is null)
            {
                continue;
            }

            next [pair.Key] = parsed;
        }

        lock (_schemesLock)
        {
            foreach (KeyValuePair<string, Scheme?> pair in _runtimeSchemes)
            {
                next [pair.Key] = pair.Value;
            }
        }

        ReplaceSchemes (next);
    }
}
