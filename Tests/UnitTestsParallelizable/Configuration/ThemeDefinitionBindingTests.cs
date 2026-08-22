// Copilot - Claude Opus 4.7

using Microsoft.Extensions.Configuration;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;

namespace ConfigurationTests;

/// <summary>
///     Binding tests for <see cref="ThemeDefinition"/> against in-memory MEC providers.
/// </summary>
/// <remarks>
///     <para>
///         These tests validate the bind-target shape. Production overlay uses
///         <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/> per-section binds, not a single
///         <see cref="ThemeDefinition"/> graph. Tests use <c>AddJsonStream</c> against in-memory JSON.
///     </para>
/// </remarks>
public class ThemeDefinitionBindingTests
{
    /// <summary>
    ///     A nested JSON sample with two themes — one fully-populated, one partial-override — binds to
    ///     <c>Dictionary&lt;string, ThemeDefinition&gt;</c>. The partial theme has <see langword="null"/>s in every
    ///     subsection it did not mention.
    /// </summary>
    [Fact]
    public void Bind_FullAndPartialThemes_PartialHasNullsInOmittedSubsections ()
    {
        string json = """
                      {
                        "Themes": {
                          "Full": {
                            "Button":     { "DefaultShadow": "None" },
                            "CheckBox":   { },
                            "Dialog":     { "DefaultBorderStyle": "Single" },
                            "FrameView":  { },
                            "HexView":    { },
                            "LinearRange":{ },
                            "MenuBar":    { },
                            "Menu":       { },
                            "MessageBox": { "DefaultBorderStyle": "Double" },
                            "NerdFonts":  { },
                            "PopoverMenu":{ },
                            "SelectorBase":{ },
                            "StatusBar":  { },
                            "TextField":  { },
                            "TextView":   { },
                            "Window":     { "DefaultBorderStyle": "Heavy" },
                            "Glyphs":     { },
                            "CharMap":    { }
                          },
                          "Partial": {
                            "Button":     { "DefaultShadow": "Transparent" }
                          }
                        }
                      }
                      """;

            IConfigurationBuilder builder = new ConfigurationBuilder ()
                                            .AddJsonStream (JsonStream (json));
            IConfiguration config = builder.Build ();

        Dictionary<string, ThemeDefinition> themes = new ();
        config.GetSection ("Themes").Bind (themes);

        Assert.Equal (2, themes.Count);
        Assert.True (themes.ContainsKey ("Full"));
        Assert.True (themes.ContainsKey ("Partial"));

        ThemeDefinition full = themes ["Full"];
        Assert.NotNull (full.Button);
        Assert.Equal (ShadowStyles.None, full.Button!.DefaultShadow);
        Assert.NotNull (full.Dialog);
        Assert.Equal (LineStyle.Single, full.Dialog!.DefaultBorderStyle);
        Assert.NotNull (full.MessageBox);
        Assert.Equal (LineStyle.Double, full.MessageBox!.DefaultBorderStyle);
        Assert.NotNull (full.Window);
        Assert.Equal (LineStyle.Heavy, full.Window!.DefaultBorderStyle);

        ThemeDefinition partial = themes ["Partial"];
        Assert.NotNull (partial.Button);
        Assert.Equal (ShadowStyles.Transparent, partial.Button!.DefaultShadow);

        // Every subsection the partial theme did not mention must be null.
        Assert.Null (partial.CheckBox);
        Assert.Null (partial.CharMap);
        Assert.Null (partial.Dialog);
        Assert.Null (partial.FrameView);
        Assert.Null (partial.HexView);
        Assert.Null (partial.LinearRange);
        Assert.Null (partial.MenuBar);
        Assert.Null (partial.Menu);
        Assert.Null (partial.MessageBox);
        Assert.Null (partial.NerdFonts);
        Assert.Null (partial.PopoverMenu);
        Assert.Null (partial.SelectorBase);
        Assert.Null (partial.StatusBar);
        Assert.Null (partial.TextField);
        Assert.Null (partial.TextView);
        Assert.Null (partial.Window);
        Assert.Null (partial.Glyphs);
        Assert.Null (partial.Schemes);
    }

    /// <summary>
    ///     A nested JSON sample with <c>Schemes</c> as a dictionary inside a <see cref="ThemeDefinition"/> binds. This
    ///     test surfaces whether MEC's reflection-based binder can populate the immutable <see cref="Scheme"/> via its
    ///     parameterless constructor and <c>init</c>-only <see cref="Scheme.Normal"/> property. If this test fails it
    ///     signals that the manager-rewire commit will need a <c>SchemeDefinition</c> DTO wrapper to mediate binding.
    /// </summary>
    [Fact]
    public void Bind_SchemesDictionaryInsideTheme_PopulatesSchemes ()
    {
        string json = """
                      {
                        "Themes": {
                          "Test": {
                            "Schemes": {
                              "Base":    { },
                              "Toplevel":{ }
                            }
                          }
                        }
                      }
                      """;

            IConfigurationBuilder builder = new ConfigurationBuilder ()
                                            .AddJsonStream (JsonStream (json));
            IConfiguration config = builder.Build ();

        Dictionary<string, ThemeDefinition> themes = new ();
        config.GetSection ("Themes").Bind (themes);

        Assert.True (themes.ContainsKey ("Test"));
        ThemeDefinition test = themes ["Test"];

        Assert.NotNull (test.Schemes);
        Assert.Equal (2, test.Schemes!.Count);
        Assert.True (test.Schemes.ContainsKey ("Base"));
        Assert.True (test.Schemes.ContainsKey ("Toplevel"));
        Assert.NotNull (test.Schemes ["Base"]);
        Assert.NotNull (test.Schemes ["Toplevel"]);
    }

    /// <summary>
    ///     An empty <c>Themes</c> section binds to an empty dictionary without throwing.
    /// </summary>
    [Fact]
    public void Bind_EmptyThemesSection_ProducesEmptyDictionary ()
    {
        string json = """{ "Themes": { } }""";

            IConfigurationBuilder builder = new ConfigurationBuilder ()
                                            .AddJsonStream (JsonStream (json));
            IConfiguration config = builder.Build ();

        Dictionary<string, ThemeDefinition> themes = new ();
        config.GetSection ("Themes").Bind (themes);

        Assert.Empty (themes);
    }

    private static Stream JsonStream (string json)
    {
        MemoryStream stream = new ();
        StreamWriter writer = new (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return stream;
    }
}
