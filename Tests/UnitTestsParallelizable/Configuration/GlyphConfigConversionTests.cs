// Claude - Fable 5
using System.Text;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

/// <summary>
///     Tests for <see cref="Rune"/> conversion when glyph values arrive through MEC configuration.
///     A single-character string is always the glyph itself; multi-digit strings are legacy
///     JSON-number codepoints flattened to strings by MEC.
/// </summary>
[Collection ("StaticSettingsTests")]
public class GlyphConfigConversionTests
{
    [Fact]
    public void ApplyToStaticFacades_SingleDigitGlyphString_IsTheGlyphNotACodepoint ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """{ "Glyphs": { "CheckStateChecked": "6" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (new Rune ('6'), GlyphSettings.Current.CheckStateChecked);
    }

    [Fact]
    public void ApplyToStaticFacades_MultiDigitGlyphString_IsALegacyDecimalCodepoint ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        // Legacy CM allowed JSON numbers (e.g. 9733 for '★'); MEC flattens them to strings.
        tuiBuilder.RuntimeConfig = """{ "Glyphs": { "CheckStateChecked": 9733 } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (new Rune ('★'), GlyphSettings.Current.CheckStateChecked);
    }

    [Fact]
    public void ApplyToStaticFacades_UPlusHexGlyphString_Binds ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """{ "Glyphs": { "CheckStateChecked": "U+2611" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (new Rune ('☑'), GlyphSettings.Current.CheckStateChecked);
    }

    [Fact]
    public void ApplyToStaticFacades_PlainGlyphString_Binds ()
    {
        using SettingsFacadeSnapshot snapshot = new ();
        TuiConfigurationBuilder tuiBuilder = new ();

        tuiBuilder.RuntimeConfig = """{ "Glyphs": { "CheckStateChecked": "X" } }""";
        tuiBuilder.ApplyToStaticFacades ();

        Assert.Equal (new Rune ('X'), GlyphSettings.Current.CheckStateChecked);
    }
}
