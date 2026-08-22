// Grok - grok-4.6
using System.Text.Json.Nodes;
using Terminal.Gui.Tools.MigrateConfig;

namespace ConfigurationTests;

/// <summary>Tests for the standalone config.json migrator used by Tools/MigrateConfig.</summary>
public class ConfigJsonMigratorTests
{
    [Fact]
    public void MigrateObject_EmptyThemesArray_BecomesEmptyDictionary ()
    {
        JsonObject src = JsonNode.Parse ("""{ "Themes": [] }""")!.AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.IsType<JsonObject> (migrated ["Themes"]);
        Assert.Empty (migrated ["Themes"]!.AsObject ());
    }

    [Fact]
    public void MigrateObject_EmptySchemesArray_BecomesEmptyDictionary ()
    {
        JsonObject src = JsonNode.Parse ("""{ "Schemes": [] }""")!.AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.IsType<JsonObject> (migrated ["Schemes"]);
        Assert.Empty (migrated ["Schemes"]!.AsObject ());
    }

    [Fact]
    public void MigrateObject_ArrayThemes_BecomeDictionary ()
    {
        JsonObject src = JsonNode.Parse (
                             """
                             {
                               "Themes": [
                                 { "Dark": { "Button.DefaultShadow": "None" } }
                               ]
                             }
                             """)!
                         .AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.Equal ("None", migrated ["Themes"]? ["Dark"]? ["Button"]? ["DefaultShadow"]?.GetValue<string> ());
    }

    [Fact]
    public void IsLegacyConfigShape_DottedTopLevelKey_IsTrue ()
    {
        Assert.True (TuiConfigurationExtensions.IsLegacyConfigShape ("""{ "Button.DefaultShadow": "None" }"""));
    }

    [Fact]
    public void IsLegacyConfigShape_ArrayThemes_IsTrue ()
    {
        Assert.True (TuiConfigurationExtensions.IsLegacyConfigShape ("""{ "Themes": [ { "Dark": {} } ] }"""));
    }

    [Fact]
    public void IsLegacyConfigShape_NestedMec_IsFalse ()
    {
        Assert.False (TuiConfigurationExtensions.IsLegacyConfigShape ("""{ "Button": { "DefaultShadow": "None" }, "Themes": { "Dark": {} } }"""));
    }

    // Claude - Fable 5
    [Fact]
    public void MigrateObject_DottedKeyThenPlainSection_MergesBoth ()
    {
        JsonObject src = JsonNode.Parse (
                             """
                             {
                               "Button.DefaultShadow": "Opaque",
                               "Button": { "DefaultMouseHighlightStates": "In" }
                             }
                             """)!
                         .AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.Equal ("Opaque", migrated ["Button"]? ["DefaultShadow"]?.GetValue<string> ());
        Assert.Equal ("In", migrated ["Button"]? ["DefaultMouseHighlightStates"]?.GetValue<string> ());
    }

    // Claude - Fable 5
    [Fact]
    public void MigrateObject_PlainSectionThenDottedKey_MergesBoth ()
    {
        JsonObject src = JsonNode.Parse (
                             """
                             {
                               "Button": { "DefaultMouseHighlightStates": "In" },
                               "Button.DefaultShadow": "Opaque"
                             }
                             """)!
                         .AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.Equal ("Opaque", migrated ["Button"]? ["DefaultShadow"]?.GetValue<string> ());
        Assert.Equal ("In", migrated ["Button"]? ["DefaultMouseHighlightStates"]?.GetValue<string> ());
    }

    // Claude - Fable 5
    [Fact]
    public void MigrateObject_TwoDottedKeysDeepPath_MergesBoth ()
    {
        JsonObject src = JsonNode.Parse (
                             """
                             {
                               "FileDialogStyle.ColorProviderName": "Ext",
                               "FileDialogStyle.UseColors": true
                             }
                             """)!
                         .AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.Equal ("Ext", migrated ["FileDialogStyle"]? ["ColorProviderName"]?.GetValue<string> ());
        Assert.True (migrated ["FileDialogStyle"]? ["UseColors"]?.GetValue<bool> ());
    }

    // Claude - Fable 5
    [Fact]
    public void MigrateObject_NestedObjectsUnderSameKey_DeepMerges ()
    {
        JsonObject src = JsonNode.Parse (
                             """
                             {
                               "Themes.Dark.Button": { "DefaultShadow": "None" },
                               "Themes": { "Dark": { "Dialog": { "DefaultShadow": "Opaque" } } }
                             }
                             """)!
                         .AsObject ();
        JsonObject migrated = ConfigJsonMigrator.MigrateObject (src);

        Assert.Equal ("None", migrated ["Themes"]? ["Dark"]? ["Button"]? ["DefaultShadow"]?.GetValue<string> ());
        Assert.Equal ("Opaque", migrated ["Themes"]? ["Dark"]? ["Dialog"]? ["DefaultShadow"]?.GetValue<string> ());
    }
}
