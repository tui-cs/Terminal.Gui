using Terminal.Gui.Drawing;

namespace Terminal.Gui.Configuration;

/// <summary>
///     POCO that represents a single named theme in the MEC-bound configuration tree.
/// </summary>
/// <remarks>
///     <para>
///         A <see cref="ThemeDefinition"/> contains an optional dictionary of <see cref="Scheme"/>s plus an optional
///         per-component override for each of the 18 <c>ThemeScope</c>-flavored settings POCOs bound by
///         <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/>.
///     </para>
///     <para>
///         <b>Null = no theme-level override.</b> Each override property is nullable. When a property is <see langword="null"/>,
///         the theme does not contribute a value for that component and the root-level <c>*Settings</c> section continues to
///         supply the effective default. When a property is non-<see langword="null"/>, the theme contributes a
///         <em>fully-populated</em> replacement POCO; how the consumer combines it with the root defaults
///         (wholesale-replace vs. property-level merge) is a manager-rewire concern and is not encoded here.
///     </para>
///     <para>
///         <b>Why nullable subsections (not "missing dictionary entry" or "explicit empty object"):</b> using nullability on
///         strongly-typed properties keeps the binder honest — MEC populates a property iff the JSON section is present, and
///         a consumer can ask <c>theme.Button is null</c> without reflecting over a generic bag. A "missing entry in
///         dictionary" alternative would force a stringly-typed lookup; an "explicit empty object" alternative would make
///         "I appear in JSON but override nothing" indistinguishable from "I appear in JSON to override defaults to their
///         own values" — both ambiguities are avoided by nullability.
///     </para>
///     <para>
///         This type is the typed bind target for a nested <c>Themes:{name}</c> object. Runtime overlay is applied
///         per section by <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/>; this type is used by
///         source-generation and binding tests.
///     </para>
/// </remarks>
public class ThemeDefinition
{
    /// <summary>
    ///     Gets or sets the dictionary of named <see cref="Scheme"/>s contributed by this theme.
    ///     <see langword="null"/> means the theme contributes no schemes.
    /// </summary>
    public Dictionary<string, Scheme>? Schemes { get; set; }

    /// <summary>Per-theme override for <see cref="ButtonSettings"/>. <see langword="null"/> = no override.</summary>
    public ButtonSettings? Button { get; set; }

    /// <summary>Per-theme override for <see cref="CheckBoxSettings"/>. <see langword="null"/> = no override.</summary>
    public CheckBoxSettings? CheckBox { get; set; }

    /// <summary>Per-theme override for <see cref="CharMapSettings"/>. <see langword="null"/> = no override.</summary>
    public CharMapSettings? CharMap { get; set; }

    /// <summary>Per-theme override for <see cref="DialogSettings"/>. <see langword="null"/> = no override.</summary>
    public DialogSettings? Dialog { get; set; }

    /// <summary>Per-theme override for <see cref="FrameViewSettings"/>. <see langword="null"/> = no override.</summary>
    public FrameViewSettings? FrameView { get; set; }

    /// <summary>Per-theme override for <see cref="HexViewSettings"/>. <see langword="null"/> = no override.</summary>
    public HexViewSettings? HexView { get; set; }

    /// <summary>Per-theme override for <see cref="LinearRangeSettings"/>. <see langword="null"/> = no override.</summary>
    public LinearRangeSettings? LinearRange { get; set; }

    /// <summary>Per-theme override for <see cref="MenuBarSettings"/>. <see langword="null"/> = no override.</summary>
    public MenuBarSettings? MenuBar { get; set; }

    /// <summary>Per-theme override for <see cref="MenuSettings"/>. <see langword="null"/> = no override.</summary>
    public MenuSettings? Menu { get; set; }

    /// <summary>Per-theme override for <see cref="MessageBoxSettings"/>. <see langword="null"/> = no override.</summary>
    public MessageBoxSettings? MessageBox { get; set; }

    /// <summary>Per-theme override for <see cref="NerdFontsSettings"/>. <see langword="null"/> = no override.</summary>
    public NerdFontsSettings? NerdFonts { get; set; }

    /// <summary>Per-theme override for <see cref="PopoverMenuSettings"/>. <see langword="null"/> = no override.</summary>
    public PopoverMenuSettings? PopoverMenu { get; set; }

    /// <summary>Per-theme override for <see cref="SelectorBaseSettings"/>. <see langword="null"/> = no override.</summary>
    public SelectorBaseSettings? SelectorBase { get; set; }

    /// <summary>Per-theme override for <see cref="StatusBarSettings"/>. <see langword="null"/> = no override.</summary>
    public StatusBarSettings? StatusBar { get; set; }

    /// <summary>Per-theme override for <see cref="TextFieldSettings"/>. <see langword="null"/> = no override.</summary>
    public TextFieldSettings? TextField { get; set; }

    /// <summary>Per-theme override for <see cref="TextViewSettings"/>. <see langword="null"/> = no override.</summary>
    public TextViewSettings? TextView { get; set; }

    /// <summary>Per-theme override for <see cref="WindowSettings"/>. <see langword="null"/> = no override.</summary>
    public WindowSettings? Window { get; set; }

    /// <summary>
    ///     Per-theme override for <see cref="GlyphSettings"/>. <see langword="null"/> = no override.
    ///     <b>Section name in JSON is <c>Glyphs</c></b> (matching <see cref="TuiConfigurationBuilder.ApplyToStaticFacades"/>).
    /// </summary>
    public GlyphSettings? Glyphs { get; set; }
}
