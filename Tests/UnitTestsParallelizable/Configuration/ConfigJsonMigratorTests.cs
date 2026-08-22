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
}
