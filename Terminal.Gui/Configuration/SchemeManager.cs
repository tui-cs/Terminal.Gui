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

    internal static void LoadToHardCodedDefaults () => ReplaceSchemes (HardCodedDictionary ());

    /// <summary>
    ///     Overlays <paramref name="themeName"/>'s <c>Schemes</c> section from <paramref name="config"/> onto
    ///     hard-coded schemes and publishes the result.
    /// </summary>
    internal static void ApplyFromConfiguration (IConfiguration config, string themeName)
    {
        Dictionary<string, Scheme?> next = HardCodedDictionary ();
        IConfigurationSection? named = ThemeCatalog.Find (config, themeName);
        IConfigurationSection schemesSection = named is not null ? named.GetSection ("Schemes") : config.GetSection ("Schemes");

        foreach (IConfigurationSection schemeChild in schemesSection.GetChildren ())
        {
            Scheme? parsed = TryBindScheme (schemeChild);

            if (parsed is not null)
            {
                next [schemeChild.Key] = parsed;
            }
        }

        ReplaceSchemes (next);
    }

    private static Scheme? TryBindScheme (IConfigurationSection section)
    {
        JsonNode? node = SectionToJson (section);

        if (node is not JsonObject obj || obj.Count == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize (obj.ToJsonString (), TuiSerializerContext.Instance.Scheme);
        }
        catch (JsonException)
        {
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
