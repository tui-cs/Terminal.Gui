// Claude - Opus 4.8
// Tests that verify MEC binding against the flat dotted-key format used by the shipped config.json.

using System.Text;
using Microsoft.Extensions.Configuration;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;

namespace ConfigurationTests;

/// <summary>
///     Verifies that flat dotted keys (e.g. <c>Driver.Force16Colors</c>) and the scalar <c>Theme</c> key,
///     as used by the shipped <c>config.json</c>, are correctly bound by <see cref="TuiConfigurationBuilder"/>.
/// </summary>
[Collection ("StaticSettingsTests")]
public class MecDottedKeyTests
{
    /// <summary>
    ///     Documents the root cause of CR feedback #1: the MEC JSON provider does NOT interpret a dot in a key
    ///     as a section separator (only <c>:</c> is). A flat key like <c>Driver.Force16Colors</c> is stored
    ///     literally, so <c>GetSection ("Driver")</c> finds nothing — which is why flat-key binding is needed.
    /// </summary>
    [Fact]
    public void JsonProvider_DotInKey_DoesNotCreateSection ()
    {
        string json = """{ "Driver.Force16Colors": true }""";

        IConfiguration config = new ConfigurationBuilder ()
                                .AddTuiRuntimeConfig (json)
                                .Build ();

        IConfigurationSection section = config.GetSection ("Driver");

        Assert.False (section.Exists (), "MEC JSON does not auto-split dotted keys into sections");
    }

    /// <summary>
    ///     Verifies CR feedback #1 fix: a flat dotted bool key binds to its POCO property.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_BindsFlatDottedBoolKey ()
    {
        DriverSettings original = DriverSettings.Defaults;

        try
        {
            TuiConfigurationBuilder builder = new ();
            builder.RuntimeConfig = """{ "Driver.Force16Colors": true }""";
            builder.ApplyToStaticFacades ();

            Assert.True (DriverSettings.Defaults.Force16Colors);
        }
        finally
        {
            DriverSettings.Defaults = original;
        }
    }

    /// <summary>
    ///     Verifies CR feedback #1 fix: multiple flat dotted keys across different sections bind correctly.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_BindsFlatDottedKeysAcrossSections ()
    {
        DriverSettings originalDriver = DriverSettings.Defaults;
        ApplicationSettings originalApp = ApplicationSettings.Defaults;

        try
        {
            string json = """
                          {
                            "Driver.Force16Colors": true,
                            "Application.IsMouseDisabled": true
                          }
                          """;

            TuiConfigurationBuilder builder = new ();
            builder.RuntimeConfig = json;
            builder.ApplyToStaticFacades ();

            Assert.True (DriverSettings.Defaults.Force16Colors);
            Assert.True (ApplicationSettings.Defaults.IsMouseDisabled);
        }
        finally
        {
            DriverSettings.Defaults = originalDriver;
            ApplicationSettings.Defaults = originalApp;
        }
    }

    /// <summary>
    ///     Verifies CR feedback #1 fix: a flat dotted <see cref="System.Text.Rune"/> key (<c>Key.Separator</c>)
    ///     binds via the value converter.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_BindsFlatDottedRuneKey ()
    {
        KeySettings original = KeySettings.Defaults;

        try
        {
            TuiConfigurationBuilder builder = new ();
            builder.RuntimeConfig = """{ "Key.Separator": "-" }""";
            builder.ApplyToStaticFacades ();

            Assert.Equal (new Rune ('-'), KeySettings.Defaults.Separator);
        }
        finally
        {
            KeySettings.Defaults = original;
        }
    }

    /// <summary>
    ///     Verifies CR feedback #1 fix: the active theme is a scalar <c>Theme</c> key, not a nested section,
    ///     and binds to <see cref="ThemeSettings.Theme"/>.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_BindsScalarThemeKey ()
    {
        ThemeSettings original = ThemeSettings.Defaults;

        try
        {
            TuiConfigurationBuilder builder = new ();
            builder.RuntimeConfig = """{ "Theme": "Dark" }""";
            builder.ApplyToStaticFacades ();

            Assert.Equal ("Dark", ThemeSettings.Defaults.Theme);
        }
        finally
        {
            ThemeSettings.Defaults = original;
        }
    }

    /// <summary>
    ///     Verifies that the nested-section format still binds, so existing nested config files keep working.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_StillBindsNestedSectionFormat ()
    {
        DriverSettings original = DriverSettings.Defaults;

        try
        {
            TuiConfigurationBuilder builder = new ();
            builder.RuntimeConfig = """{ "Driver": { "Force16Colors": true } }""";
            builder.ApplyToStaticFacades ();

            Assert.True (DriverSettings.Defaults.Force16Colors);
        }
        finally
        {
            DriverSettings.Defaults = original;
        }
    }

    /// <summary>
    ///     Verifies issue #5561 fix: a flat dotted <see cref="Key"/> key (<c>PopoverMenu.DefaultKey</c>) binds via
    ///     the AOT-safe <see cref="Key"/> fast path — i.e. without the trim-unsafe <c>TypeDescriptor.GetConverter</c>
    ///     fallback that was removed.
    /// </summary>
    [Fact]
    public void ApplyToStaticFacades_BindsFlatDottedKeyTypedProperty ()
    {
        Key original = PopoverMenu.DefaultKey;

        try
        {
            TuiConfigurationBuilder builder = new ();
            builder.RuntimeConfig = """{ "PopoverMenu.DefaultKey": "Ctrl+P" }""";
            builder.ApplyToStaticFacades ();

            Assert.Equal (Key.P.WithCtrl, PopoverMenu.DefaultKey);
        }
        finally
        {
            PopoverMenu.DefaultKey = original;
        }
    }
}
