// Claude - Fable 5
using System.Text.Json.Nodes;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Tests for <see cref="SchemeManager.ApplyFromConfiguration"/>: runtime-added and runtime-updated
///     schemes survive theme switches, root <c>Schemes</c> customizations merge with theme overlays
///     (matching the root-then-overlay contract of every other ThemeScope section), and invalid scheme
///     JSON is reported, not swallowed.
/// </summary>
[Collection ("StaticSettingsTests")]
public class SchemeManagerConfigTests
{
    /// <summary>Captures and restores the process-wide scheme state SchemeManager holds.</summary>
    private sealed class SchemeSnapshot : IDisposable
    {
        private readonly Dictionary<string, Scheme?> _schemes = new (SchemeManager.GetSchemes (), StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<string, Scheme?> _runtime = SchemeManager.GetRuntimeSchemes ();

        public void Dispose ()
        {
            SchemeManager.ReplaceSchemes (_schemes);
            SchemeManager.SetRuntimeSchemes (_runtime);
        }
    }

    private static JsonObject MergedJson (string json) => JsonNode.Parse (json)!.AsObject ();

    [Fact]
    public void ApplyFromConfiguration_PreservesRuntimeAddedSchemes ()
    {
        using SchemeSnapshot snapshot = new ();

        Scheme custom = new () { Normal = new (Color.White, Color.Blue) };
        SchemeManager.AddScheme ("MyScheme", custom);

        SchemeManager.ApplyFromConfiguration (MergedJson ("""{ "Themes": { "Dark": {} } }"""), "Dark");

        Assert.True (SchemeManager.TryGetScheme ("MyScheme", out Scheme? survived));
        Assert.Equal (Color.White, survived!.Normal.Foreground);
    }

    // A runtime UPDATE to an existing scheme must survive re-apply just like a runtime ADD; the
    // documented AddScheme contract is "If the name already exists, it is updated".
    [Fact]
    public void ApplyFromConfiguration_PreservesRuntimeUpdatedBuiltInScheme ()
    {
        using SchemeSnapshot snapshot = new ();

        Scheme custom = new () { Normal = new (Color.White, Color.Magenta) };
        SchemeManager.AddScheme ("Base", custom);

        SchemeManager.ApplyFromConfiguration (MergedJson ("""{ "Themes": { "Dark": {} } }"""), "Dark");

        Assert.True (SchemeManager.TryGetScheme ("Base", out Scheme? survived));
        Assert.Equal (Color.Magenta, survived!.Normal.Background);
    }

    [Fact]
    public void ApplyFromConfiguration_RootSchemes_ApplyWhenThemeHasNoSchemesSection ()
    {
        using SchemeSnapshot snapshot = new ();

        JsonObject merged = MergedJson (
                                        """
                                        {
                                          "Schemes": {
                                            "Base": { "Normal": { "Foreground": "White", "Background": "Blue" } }
                                          },
                                          "Themes": { "Dark": {} }
                                        }
                                        """);
        SchemeManager.ApplyFromConfiguration (merged, "Dark");

        Assert.True (SchemeManager.TryGetScheme ("Base", out Scheme? baseScheme));
        Assert.Equal (Color.White, baseScheme!.Normal.Foreground);
        Assert.Equal (Color.Blue, baseScheme.Normal.Background);
    }

    [Fact]
    public void ApplyFromConfiguration_RootSchemes_SurviveThemeWithOwnSchemesSection ()
    {
        using SchemeSnapshot snapshot = new ();

        JsonObject merged = MergedJson (
                                        """
                                        {
                                          "Schemes": {
                                            "Base": { "Normal": { "Foreground": "White", "Background": "Blue" } }
                                          },
                                          "Themes": {
                                            "Dark": {
                                              "Schemes": {
                                                "Error": { "Normal": { "Foreground": "Red", "Background": "Black" } }
                                              }
                                            }
                                          }
                                        }
                                        """);
        SchemeManager.ApplyFromConfiguration (merged, "Dark");

        Assert.True (SchemeManager.TryGetScheme ("Base", out Scheme? baseScheme));
        Assert.Equal (Color.White, baseScheme!.Normal.Foreground);
        Assert.True (SchemeManager.TryGetScheme ("Error", out Scheme? errorScheme));
        Assert.Equal (Color.Red, errorScheme!.Normal.Foreground);
    }

    [Fact]
    public void ApplyFromConfiguration_ThemeOverlay_MergesPerRoleWithRootScheme ()
    {
        using SchemeSnapshot snapshot = new ();

        JsonObject merged = MergedJson (
                                        """
                                        {
                                          "Schemes": {
                                            "Base": { "Normal": { "Foreground": "White", "Background": "Blue" } }
                                          },
                                          "Themes": {
                                            "Dark": {
                                              "Schemes": {
                                                "Base": { "Focus": { "Foreground": "Black", "Background": "Gray" } }
                                              }
                                            }
                                          }
                                        }
                                        """);
        SchemeManager.ApplyFromConfiguration (merged, "Dark");

        Assert.True (SchemeManager.TryGetScheme ("Base", out Scheme? baseScheme));

        // The overlay's Focus wins; the root's Normal survives (root-then-overlay merge).
        Assert.Equal (Color.White, baseScheme!.Normal.Foreground);
        Assert.Equal (Color.Black, baseScheme.Focus.Foreground);
        Assert.Equal (Color.Gray, baseScheme.Focus.Background);
    }

    [Fact]
    public void ApplyFromConfiguration_InvalidSchemeValue_KeepsHardCodedScheme_AndCollectsError ()
    {
        using SchemeSnapshot snapshot = new ();
        TuiJsonErrors.Print ();

        JsonObject merged = MergedJson (
                                        """
                                        {
                                          "Schemes": {
                                            "Base": { "Normal": { "Foreground": "NotAColor", "Background": "Blue" } }
                                          }
                                        }
                                        """);
        SchemeManager.ApplyFromConfiguration (merged, "Default");

        Scheme hardCodedBase = SchemeManager.GetHardCodedSchemes () ["Base"]!;
        Assert.True (SchemeManager.TryGetScheme ("Base", out Scheme? baseScheme));
        Assert.Equal (hardCodedBase.Normal, baseScheme!.Normal);
        Assert.Contains (TuiJsonErrors.GetErrors (), e => e.Contains ("Base"));
    }

    [Fact]
    public void ApplyFromConfiguration_ConfigSourcedScheme_RemovedWhenNoLongerInConfig ()
    {
        using SchemeSnapshot snapshot = new ();

        JsonObject withScheme = MergedJson (
                                            """
                                            {
                                              "Schemes": {
                                                "FromConfig": { "Normal": { "Foreground": "White", "Background": "Blue" } }
                                              }
                                            }
                                            """);
        SchemeManager.ApplyFromConfiguration (withScheme, "Default");
        Assert.True (SchemeManager.TryGetScheme ("FromConfig", out _));

        SchemeManager.ApplyFromConfiguration (MergedJson ("""{ }"""), "Default");

        Assert.False (SchemeManager.TryGetScheme ("FromConfig", out _));
    }
}
