// Copilot

using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>Tests for the app-developer <see cref="TuiConfigurationBuilder.BindAppSettings{T}"/> workflow.</summary>
[Collection ("StaticSettingsTests")]
public class MecAppSettingsTests
{
    /// <summary>Example app settings POCO for testing.</summary>
    private class SampleAppSettings
    {
        public string Title { get; set; } = "Default Title";
        public int MaxItems { get; set; } = 100;
        public bool EnableFeatureX { get; set; }
        public static SampleAppSettings Defaults { get; set; } = new ();
    }

    [Fact]
    public void BindAppSettings_BindsFromJson ()
    {
        // Arrange
        string json = """{"SampleApp": {"Title": "My App", "MaxItems": 50, "EnableFeatureX": true}}""";
        TuiConfigurationBuilder builder = new () { RuntimeConfig = json };

        // Act
        builder.BindAppSettings<SampleAppSettings> ("SampleApp", s => SampleAppSettings.Defaults = s);

        // Assert
        Assert.Equal ("My App", SampleAppSettings.Defaults.Title);
        Assert.Equal (50, SampleAppSettings.Defaults.MaxItems);
        Assert.True (SampleAppSettings.Defaults.EnableFeatureX);

        // Cleanup
        SampleAppSettings.Defaults = new ();
    }

    [Fact]
    public void BindAppSettings_UsesDefaults_WhenSectionMissing ()
    {
        // Arrange - empty config
        TuiConfigurationBuilder builder = new () { RuntimeConfig = "{}" };

        // Act
        builder.BindAppSettings<SampleAppSettings> ("SampleApp", s => SampleAppSettings.Defaults = s);

        // Assert - defaults from POCO
        Assert.Equal ("Default Title", SampleAppSettings.Defaults.Title);
        Assert.Equal (100, SampleAppSettings.Defaults.MaxItems);
        Assert.False (SampleAppSettings.Defaults.EnableFeatureX);

        // Cleanup
        SampleAppSettings.Defaults = new ();
    }

    [Fact]
    public void BindAppSettings_PartialJson_MergesWithDefaults ()
    {
        // Arrange - only Title specified
        string json = """{"SampleApp": {"Title": "Custom"}}""";
        TuiConfigurationBuilder builder = new () { RuntimeConfig = json };

        // Act
        builder.BindAppSettings<SampleAppSettings> ("SampleApp", s => SampleAppSettings.Defaults = s);

        // Assert
        Assert.Equal ("Custom", SampleAppSettings.Defaults.Title);
        Assert.Equal (100, SampleAppSettings.Defaults.MaxItems);
        Assert.False (SampleAppSettings.Defaults.EnableFeatureX);

        // Cleanup
        SampleAppSettings.Defaults = new ();
    }

    [Fact]
    public void BindAppSettings_ReturnsBuilder_ForChaining ()
    {
        // Arrange
        TuiConfigurationBuilder builder = new () { RuntimeConfig = "{}" };

        // Act
        TuiConfigurationBuilder result = builder.BindAppSettings<SampleAppSettings> ("SampleApp", s => SampleAppSettings.Defaults = s);

        // Assert
        Assert.Same (builder, result);

        // Cleanup
        SampleAppSettings.Defaults = new ();
    }
}
