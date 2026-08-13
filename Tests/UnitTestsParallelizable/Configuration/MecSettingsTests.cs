// Copilot - Claude Opus 4.6

using Terminal.Gui.Configuration;
using Microsoft.Extensions.Configuration;

namespace ConfigurationTests;

/// <summary>Tests for the MEC-based settings POCOs and <see cref="TuiConfigurationBuilder"/>.</summary>
[Collection ("StaticSettingsTests")]
public class MecSettingsTests
{
    [Fact]
    public void ApplicationSettings_Defaults_HasCorrectValues ()
    {
        ApplicationSettings settings = new ();

        Assert.Equal (AppModel.FullScreen, settings.AppModel);
        Assert.Equal (string.Empty, settings.ForceDriver);
        Assert.False (settings.IsMouseDisabled);
    }

    [Fact]
    public void DriverSettings_Defaults_HasCorrectValues ()
    {
        DriverSettings settings = new ();

        Assert.False (settings.Force16Colors);
        Assert.Equal (SizeDetectionMode.Polling, settings.SizeDetection);
    }

    [Fact]
    public void ButtonSettings_Defaults_HasCorrectValues ()
    {
        ButtonSettings settings = new ();

        Assert.Equal (ShadowStyles.Opaque, settings.DefaultShadow);
        Assert.Equal (MouseState.In | MouseState.Pressed | MouseState.PressedOutside, settings.DefaultMouseHighlightStates);
    }

    [Fact]
    public void DialogSettings_Defaults_HasCorrectValues ()
    {
        DialogSettings settings = new ();

        Assert.Equal (ShadowStyles.Transparent, settings.DefaultShadow);
        Assert.Equal (LineStyle.Heavy, settings.DefaultBorderStyle);
        Assert.Equal (Alignment.End, settings.DefaultButtonAlignment);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, settings.DefaultButtonAlignmentModes);
    }

    [Fact]
    public void WindowSettings_Defaults_HasCorrectValues ()
    {
        WindowSettings settings = new ();

        Assert.Equal (ShadowStyles.None, settings.DefaultShadow);
        Assert.Equal (LineStyle.Single, settings.DefaultBorderStyle);
    }

    [Fact]
    public void MessageBoxSettings_Defaults_HasCorrectValues ()
    {
        MessageBoxSettings settings = new ();

        Assert.Equal (LineStyle.Heavy, settings.DefaultBorderStyle);
        Assert.Equal (Alignment.Center, settings.DefaultButtonAlignment);
    }

    [Fact]
    public void CheckBoxSettings_Defaults_HasCorrectValues ()
    {
        CheckBoxSettings settings = new ();

        Assert.Equal (MouseState.PressedOutside | MouseState.Pressed | MouseState.In, settings.DefaultMouseHighlightStates);
    }

    [Fact]
    public void StaticFacade_CanBeOverridden ()
    {
        // Save original
        ButtonSettings original = ButtonSettings.Defaults;

        try
        {
            ButtonSettings custom = new () { DefaultShadow = ShadowStyles.None };
            ButtonSettings.Defaults = custom;

            Assert.Equal (ShadowStyles.None, ButtonSettings.Defaults.DefaultShadow);
        }
        finally
        {
            ButtonSettings.Defaults = original;
        }
    }

    [Fact]
    public void TuiConfigurationExtensions_AddTuiRuntimeConfig_BindsJsonToSection ()
    {
        // Arrange
        string json = """
                      {
                        "Application": {
                          "ForceDriver": "ansi",
                          "IsMouseDisabled": true
                        }
                      }
                      """;

        IConfigurationBuilder builder = new ConfigurationBuilder ()
            .AddTuiRuntimeConfig (json);

        IConfiguration config = builder.Build ();

        // Act
        ApplicationSettings settings = new ();
        config.GetSection ("Application").Bind (settings);

        // Assert
        Assert.Equal ("ansi", settings.ForceDriver);
        Assert.True (settings.IsMouseDisabled);
    }

    [Fact]
    public void TuiConfigurationExtensions_AddTuiRuntimeConfig_NullJson_DoesNotThrow ()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder ()
            .AddTuiRuntimeConfig (null);

        IConfiguration config = builder.Build ();

        // Should not throw, and section should be empty
        string? value = config.GetSection ("Application") ["ForceDriver"];
        Assert.Null (value);
    }

    [Fact]
    public void TuiConfigurationBuilder_Build_LoadsLibraryDefaults ()
    {
        // The library's embedded config.json should be loadable
        TuiConfigurationBuilder tuiBuilder = new ();
        IConfiguration config = tuiBuilder.Configuration;

        // The config should have at least the Themes section from the embedded config.json
        IConfigurationSection themesSection = config.GetSection ("Themes");
        Assert.NotNull (themesSection);
    }

    [Fact]
    public void TuiConfigurationBuilder_RuntimeConfig_InvalidatesCache ()
    {
        TuiConfigurationBuilder tuiBuilder = new ();
        IConfiguration first = tuiBuilder.Configuration;

        tuiBuilder.RuntimeConfig = """{ "Driver": { "Force16Colors": true } }""";
        IConfiguration second = tuiBuilder.Configuration;

        Assert.NotSame (first, second);
    }

    [Fact]
    public void TuiConfigurationBuilder_ApplyToStaticFacades_UpdatesDefaults ()
    {
        // Save originals
        ApplicationSettings originalApp = ApplicationSettings.Defaults;
        DriverSettings originalDriver = DriverSettings.Defaults;

        try
        {
            TuiConfigurationBuilder tuiBuilder = new ();

            tuiBuilder.RuntimeConfig = """
                                       {
                                         "Application": { "ForceDriver": "dotnet" },
                                         "Driver": { "Force16Colors": true }
                                       }
                                       """;
            tuiBuilder.ApplyToStaticFacades ();

            Assert.Equal ("dotnet", ApplicationSettings.Defaults.ForceDriver);
            Assert.True (DriverSettings.Defaults.Force16Colors);
        }
        finally
        {
            ApplicationSettings.Defaults = originalApp;
            DriverSettings.Defaults = originalDriver;
        }
    }

    [Fact]
    public void TuiConfigurationExtensions_Precedence_LaterSourceOverridesEarlier ()
    {
        string lowPriority = """{ "Driver": { "Force16Colors": false } }""";
        string highPriority = """{ "Driver": { "Force16Colors": true } }""";

        IConfiguration config = new ConfigurationBuilder ()
                                .AddTuiRuntimeConfig (lowPriority)
                                .AddTuiRuntimeConfig (highPriority)
                                .Build ();

        DriverSettings settings = new ();
        config.GetSection ("Driver").Bind (settings);

        Assert.True (settings.Force16Colors);
    }
}
