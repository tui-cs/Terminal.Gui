// Grok - grok-4.6
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace ConfigurationTests;

/// <summary>
///     Nested MEC key-binding dictionaries overlay hard-coded defaults.
/// </summary>
[Collection ("StaticSettingsTests")]
public class KeyBindingConfigurationTests
{
    [Fact]
    public void ApplyToStaticFacades_OverlaysApplicationDefaultKeyBindings ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = """
                                {
                                  "Application": {
                                    "DefaultKeyBindings": {
                                      "Quit": { "All": ["Ctrl+Q"] }
                                    }
                                  }
                                }
                                """;
        builder.ApplyToStaticFacades ();

        Assert.NotNull (Application.DefaultKeyBindings);
        Assert.Equal (Key.Q.WithCtrl, Application.DefaultKeyBindings [Command.Quit].All! [0]);
        Assert.True (Application.DefaultKeyBindings.ContainsKey (Command.Arrange));
    }

    [Fact]
    public void ApplyToStaticFacades_OverlaysViewDefaultKeyBindings ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = """
                                {
                                  "View": {
                                    "DefaultKeyBindings": {
                                      "Copy": { "All": ["Ctrl+Insert"] }
                                    }
                                  }
                                }
                                """;
        builder.ApplyToStaticFacades ();

        Assert.NotNull (View.DefaultKeyBindings);
        Assert.Equal (Key.InsertChar.WithCtrl, View.DefaultKeyBindings [Command.Copy].All! [0]);
        Assert.True (View.DefaultKeyBindings.ContainsKey (Command.Paste));
    }

    [Fact]
    public void ApplyToStaticFacades_OverlaysViewKeyBindingsByType ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = """
                                {
                                  "View": {
                                    "ViewKeyBindings": {
                                      "TextField": {
                                        "CutToEndOfLine": { "All": ["Ctrl+K"] }
                                      }
                                    }
                                  }
                                }
                                """;
        builder.ApplyToStaticFacades ();

        Assert.NotNull (View.ViewKeyBindings);
        Assert.True (View.ViewKeyBindings.ContainsKey ("TextField"));
        Assert.Equal (Key.K.WithCtrl, View.ViewKeyBindings ["TextField"] [Command.CutToEndOfLine].All! [0]);
    }

    [Fact]
    public void ApplyToStaticFacades_MigratedFlatKeyBindingJson_Binds ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        string nested = """
                        {
                          "Application": {
                            "DefaultKeyBindings": {
                              "Quit": { "All": ["Esc", "Ctrl+Q"] }
                            }
                          }
                        }
                        """;

        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = nested;
        builder.ApplyToStaticFacades ();

        Key [] quitKeys = Application.DefaultKeyBindings! [Command.Quit].All!;
        Assert.Equal (2, quitKeys.Length);
        Assert.Equal (Key.Esc, quitKeys [0]);
        Assert.Equal (Key.Q.WithCtrl, quitKeys [1]);
    }

    [Fact]
    public void ToJson_RebuildsMecIndexChildrenAsJsonArrays ()
    {
        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = """
                                {
                                  "Application": {
                                    "DefaultKeyBindings": {
                                      "Quit": { "All": ["Esc", "Ctrl+Q"] }
                                    }
                                  }
                                }
                                """;

        JsonNode? node = ConfigurationSectionJson.ToJson (
                                                         builder.Configuration.GetSection ("Application").GetSection ("DefaultKeyBindings"));

        Assert.NotNull (node);
        Assert.Equal (JsonValueKind.Array, node ["Quit"]! ["All"]!.GetValueKind ());
        Assert.Equal (2, node ["Quit"]! ["All"]!.AsArray ().Count);
        Assert.Equal ("Esc", node ["Quit"]! ["All"]! [0]!.GetValue<string> ());
        Assert.Equal ("Ctrl+Q", node ["Quit"]! ["All"]! [1]!.GetValue<string> ());
    }

    // Claude - Fable 5
    // A binding removed from configuration must regain its default on re-apply — "unmentioned
    // commands keep hard-coded defaults" also holds across Reload, not just at first apply.
    [Fact]
    public void ApplyToStaticFacades_RemovedBinding_RestoresDefault ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = """
                                {
                                  "Application": {
                                    "DefaultKeyBindings": {
                                      "Quit": { "All": ["Ctrl+X"] }
                                    }
                                  }
                                }
                                """;
        builder.ApplyToStaticFacades ();
        Assert.Equal (Key.X.WithCtrl, Application.DefaultKeyBindings! [Command.Quit].All! [0]);

        builder.RuntimeConfig = "{ }";
        builder.ApplyToStaticFacades ();

        Assert.Equal (Key.Esc, Application.DefaultKeyBindings! [Command.Quit].All! [0]);
    }

    // Claude - Fable 5
    // A higher-priority source's array must atomically replace a lower-priority source's array;
    // MEC's per-index merge would otherwise keep the longer array's tail elements bound.
    [Fact]
    public void ApplyToStaticFacades_ShorterArrayInHigherPrioritySource_ReplacesWholesale ()
    {
        using SettingsFacadeSnapshot snapshot = new ();

        string root = Directory.CreateTempSubdirectory ("tui-kb-test-").FullName;
        string tuiDir = Path.Combine (root, TuiConfigurationExtensions.TUI_CONFIG_FOLDER);
        Directory.CreateDirectory (tuiDir);

        File.WriteAllText (
                           Path.Combine (tuiDir, TuiConfigurationExtensions.CONFIG_FILENAME),
                           """
                           {
                             "Application": {
                               "DefaultKeyBindings": {
                                 "Quit": { "All": ["Esc", "Ctrl+Q"] }
                               }
                             }
                           }
                           """);

        try
        {
            TuiConfigurationBuilder builder = new (appName: null, currentDirectory: root);
            builder.RuntimeConfig = """
                                    {
                                      "Application": {
                                        "DefaultKeyBindings": {
                                          "Quit": { "All": ["Ctrl+C"] }
                                        }
                                      }
                                    }
                                    """;
            builder.ApplyToStaticFacades ();

            Key [] quitKeys = Application.DefaultKeyBindings! [Command.Quit].All!;
            Assert.Single (quitKeys);
            Assert.Equal (Key.C.WithCtrl, quitKeys [0]);
        }
        finally
        {
            Directory.Delete (root, recursive: true);
        }
    }

    [Fact]
    public void ToJson_NonContiguousNumericKeys_StayObject ()
    {
        TuiConfigurationBuilder builder = new (null, null, includeUserSources: false);
        builder.RuntimeConfig = """
                                {
                                  "Probe": {
                                    "10": "Esc",
                                    "2": "Ctrl+Q"
                                  }
                                }
                                """;

        JsonNode? node = ConfigurationSectionJson.ToJson (builder.Configuration.GetSection ("Probe"));

        Assert.NotNull (node);
        Assert.Equal (JsonValueKind.Object, node.GetValueKind ());
        Assert.Equal ("Esc", node ["10"]!.GetValue<string> ());
        Assert.Equal ("Ctrl+Q", node ["2"]!.GetValue<string> ());
    }
}
