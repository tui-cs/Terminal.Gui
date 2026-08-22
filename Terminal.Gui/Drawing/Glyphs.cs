
namespace Terminal.Gui.Drawing;

/// <summary>Defines the standard set of glyphs used to draw checkboxes, lines, borders, etc...</summary>
/// <remarks>
///     <para>
///         Access with <see cref="Glyphs"/> (which is a global using alias for
///         <see cref="Glyphs"/>).
///     </para>
///     <para>
///         The default glyphs can be changed per-theme overlay in <see cref="TuiConfigurationBuilder"/>. Within a <c>config.json</c>
///         file the Json property name is the property name prefixed with "Glyphs.".
///     </para>
///     <para>
///         The Json property can be one of:
///         - unicode glyph in a string (e.g. "â˜‘")
///         - U+hex format in a string  (e.g. "U+2611")
///         - \u format in a string (e.g. "\\u2611")
///         - A decimal number (e.g. 97 for "a")
///     </para>
/// </remarks>
public class Glyphs
{
    // The default glyph values live on the GlyphSettings record's `init` defaults.
    // Resources/config.json is the source of truth for the runtime glyphs; the embedded
    // config is loaded and applied via TuiConfigurationBuilder.ApplyToStaticFacades, with
    // theme overlays composed under "Themes:<active>:Glyphs".

    /// <summary>Unicode replacement character; used by Drivers when rendering in cases where a wide glyph can't
    /// be output because it would be clipped. Defaults to ' ' (Space).</summary>
    public static Rune WideGlyphReplacement => GlyphSettings.Current.WideGlyphReplacement;

    /// <summary>File icon.  Defaults to â˜° (Trigram For Heaven)</summary>
    public static Rune File => GlyphSettings.Current.File;

    /// <summary>Folder icon.  Defaults to ê¤‰ (Kayah Li Digit Nine)</summary>
    public static Rune Folder => GlyphSettings.Current.Folder;

    /// <summary>Horizontal Ellipsis - â€¦ U+2026</summary>
    public static Rune HorizontalEllipsis => GlyphSettings.Current.HorizontalEllipsis;

    /// <summary>Vertical Four Dots - âž U+205e</summary>
    public static Rune VerticalFourDots => GlyphSettings.Current.VerticalFourDots;

    #region ----------------- Single Glyphs -----------------

    /// <summary>Null symbol ('â€')</summary>
    public static Rune Null => GlyphSettings.Current.Null;

    /// <summary>Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    public static Rune CheckStateChecked => GlyphSettings.Current.CheckStateChecked;

    /// <summary>Not Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    public static Rune CheckStateUnChecked => GlyphSettings.Current.CheckStateUnChecked;

    /// <summary>Null Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).</summary>
    public static Rune CheckStateNone => GlyphSettings.Current.CheckStateNone;

    /// <summary>Selected indicator  (e.g. for <see cref="ListView"/> and <see cref="OptionSelector"/>).</summary>
    public static Rune Selected => GlyphSettings.Current.Selected;

    /// <summary>Not Selected indicator (e.g. for <see cref="ListView"/> and <see cref="OptionSelector"/>).</summary>
    public static Rune UnSelected => GlyphSettings.Current.UnSelected;

    /// <summary>Horizontal arrow.</summary>
    public static Rune RightArrow => GlyphSettings.Current.RightArrow;

    /// <summary>Left arrow.</summary>
    public static Rune LeftArrow => GlyphSettings.Current.LeftArrow;

    /// <summary>Down arrow.</summary>
    public static Rune DownArrow => GlyphSettings.Current.DownArrow;

    /// <summary>Vertical arrow.</summary>
    public static Rune UpArrow => GlyphSettings.Current.UpArrow;

    /// <summary>Left default indicator (e.g. for <see cref="Button"/>.</summary>
    public static Rune LeftDefaultIndicator => GlyphSettings.Current.LeftDefaultIndicator;

    /// <summary>Horizontal default indicator (e.g. for <see cref="Button"/>.</summary>
    public static Rune RightDefaultIndicator => GlyphSettings.Current.RightDefaultIndicator;

    /// <summary>Left Bracket (e.g. for <see cref="Button"/>. Default is (U+005B) - [.</summary>
    public static Rune LeftBracket => GlyphSettings.Current.LeftBracket;

    /// <summary>Horizontal Bracket (e.g. for <see cref="Button"/>. Default is (U+005D) - ].</summary>
    public static Rune RightBracket => GlyphSettings.Current.RightBracket;

    /// <summary>Half block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    public static Rune BlocksMeterSegment => GlyphSettings.Current.BlocksMeterSegment;

    /// <summary>Continuous block meter segment (e.g. for <see cref="ProgressBar"/>).</summary>
    public static Rune ContinuousMeterSegment => GlyphSettings.Current.ContinuousMeterSegment;

    /// <summary>Stipple pattern (e.g. for <see cref="ScrollBar"/>). Default is Light Shade (U+2591) - â–‘.</summary>
    public static Rune Stipple => GlyphSettings.Current.Stipple;

    /// <summary>Diamond. Default is Lozenge (U+25CA) - â—Š.</summary>
    public static Rune Diamond => GlyphSettings.Current.Diamond;

    /// <summary>Close. Default is Heavy Ballot X (U+2718) - âœ˜.</summary>
    public static Rune Close => GlyphSettings.Current.Close;

    /// <summary>Minimize. Default is Lower Horizontal Shadowed White Circle (U+274F) - â.</summary>
    public static Rune Minimize => GlyphSettings.Current.Minimize;

    /// <summary>Maximize. Default is Upper Horizontal Shadowed White Circle (U+273D) - âœ½.</summary>
    public static Rune Maximize => GlyphSettings.Current.Maximize;

    /// <summary>Dot. Default is (U+2219) - âˆ™.</summary>
    public static Rune Dot => GlyphSettings.Current.Dot;

    /// <summary>Dotted Square - â¬š U+02b1aâ”</summary>
    public static Rune DottedSquare => GlyphSettings.Current.DottedSquare;

    /// <summary>Black Circle . Default is (U+025cf) - â—.</summary>
    public static Rune BlackCircle => GlyphSettings.Current.BlackCircle;

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    public static Rune Expand => GlyphSettings.Current.Expand;

    /// <summary>Expand (e.g. for <see cref="TreeView"/>.</summary>
    public static Rune Collapse => GlyphSettings.Current.Collapse;

    /// <summary>Identical To (U+226)</summary>
    public static Rune IdenticalTo => GlyphSettings.Current.IdenticalTo;

    /// <summary>Move indicator. Default is Lozenge (U+25CA) - â—Š.</summary>
    public static Rune Move => GlyphSettings.Current.Move;

    /// <summary>Size Horizontally indicator. Default is â”¥Left Right Arrow - â†” U+02194</summary>
    public static Rune SizeHorizontal => GlyphSettings.Current.SizeHorizontal;

    /// <summary>Size Vertical indicator. Default Up Down Arrow - â†• U+02195</summary>
    public static Rune SizeVertical => GlyphSettings.Current.SizeVertical;

    /// <summary>Size Top Left indicator. North West Arrow - â†– U+02196</summary>
    public static Rune SizeTopLeft => GlyphSettings.Current.SizeTopLeft;

    /// <summary>Size Top Right indicator. North East Arrow - â†— U+02197</summary>
    public static Rune SizeTopRight => GlyphSettings.Current.SizeTopRight;

    /// <summary>Size Bottom Right indicator. South East Arrow - â†˜ U+02198</summary>
    public static Rune SizeBottomRight => GlyphSettings.Current.SizeBottomRight;

    /// <summary>Size Bottom Left indicator. South West Arrow - â†™ U+02199</summary>
    public static Rune SizeBottomLeft => GlyphSettings.Current.SizeBottomLeft;

    /// <summary>Apple (non-BMP). Because snek. And because it's an example of a non-BMP surrogate pair. See Issue #2610.</summary>
    public static Rune Apple => GlyphSettings.Current.Apple;

    /// <summary>Apple (BMP). Because snek. See Issue #2610.</summary>
    public static Rune AppleBMP => GlyphSettings.Current.AppleBMP;

    /// <summary>Copy indicator. Two Joined Squares - â§‰ U+29C9. Used for code block copy buttons.</summary>
    public static Rune Copy => GlyphSettings.Current.Copy;

    #endregion

    #region ----------------- Lines -----------------

    /// <summary>Box Drawings Horizontal Line - Light (U+2500) - â”€</summary>
    public static Rune HLine => GlyphSettings.Current.HLine;

    /// <summary>Box Drawings Vertical Line - Light (U+2502) - â”‚</summary>
    public static Rune VLine => GlyphSettings.Current.VLine;

    /// <summary>Box Drawings Double Horizontal (U+2550) - â•</summary>
    public static Rune HLineDbl => GlyphSettings.Current.HLineDbl;

    /// <summary>Box Drawings Double Vertical (U+2551) - â•‘</summary>

    public static Rune VLineDbl => GlyphSettings.Current.VLineDbl;

    /// <summary>Box Drawings Heavy Double Dash Horizontal (U+254D) - â•</summary>

    public static Rune HLineHvDa2 => GlyphSettings.Current.HLineHvDa2;

    /// <summary>Box Drawings Heavy Triple Dash Vertical (U+2507) - â”‡</summary>
    public static Rune VLineHvDa3 => GlyphSettings.Current.VLineHvDa3;

    /// <summary>Box Drawings Heavy Triple Dash Horizontal (U+2505) - â”…</summary>

    public static Rune HLineHvDa3 => GlyphSettings.Current.HLineHvDa3;

    /// <summary>Box Drawings Heavy Quadruple Dash Horizontal (U+2509) - â”‰</summary>

    public static Rune HLineHvDa4 => GlyphSettings.Current.HLineHvDa4;

    /// <summary>Box Drawings Heavy Double Dash Vertical (U+254F) - â•</summary>

    public static Rune VLineHvDa2 => GlyphSettings.Current.VLineHvDa2;

    /// <summary>Box Drawings Heavy Quadruple Dash Vertical (U+250B) - â”‹</summary>

    public static Rune VLineHvDa4 => GlyphSettings.Current.VLineHvDa4;

    /// <summary>Box Drawings Light Double Dash Horizontal (U+254C) - â•Œ</summary>

    public static Rune HLineDa2 => GlyphSettings.Current.HLineDa2;

    /// <summary>Box Drawings Light Triple Dash Vertical (U+2506) - â”†</summary>

    public static Rune VLineDa3 => GlyphSettings.Current.VLineDa3;

    /// <summary>Box Drawings Light Triple Dash Horizontal (U+2504) - â”„</summary>

    public static Rune HLineDa3 => GlyphSettings.Current.HLineDa3;

    /// <summary>Box Drawings Light Quadruple Dash Horizontal (U+2508) - â”ˆ</summary>

    public static Rune HLineDa4 => GlyphSettings.Current.HLineDa4;

    /// <summary>Box Drawings Light Double Dash Vertical (U+254E) - â•Ž</summary>

    public static Rune VLineDa2 => GlyphSettings.Current.VLineDa2;

    /// <summary>Box Drawings Light Quadruple Dash Vertical (U+250A) - â”Š</summary>

    public static Rune VLineDa4 => GlyphSettings.Current.VLineDa4;

    /// <summary>Box Drawings Heavy Horizontal (U+2501) - â”</summary>

    public static Rune HLineHv => GlyphSettings.Current.HLineHv;

    /// <summary>Box Drawings Heavy Vertical (U+2503) - â”ƒ</summary>

    public static Rune VLineHv => GlyphSettings.Current.VLineHv;

    /// <summary>Box Drawings Light Left (U+2574) - â•´</summary>

    public static Rune HalfLeftLine => GlyphSettings.Current.HalfLeftLine;

    /// <summary>Box Drawings Light Vertical (U+2575) - â•µ</summary>

    public static Rune HalfTopLine => GlyphSettings.Current.HalfTopLine;

    /// <summary>Box Drawings Light Horizontal (U+2576) - â•¶</summary>

    public static Rune HalfRightLine => GlyphSettings.Current.HalfRightLine;

    /// <summary>Box Drawings Light Down (U+2577) - â•·</summary>

    public static Rune HalfBottomLine => GlyphSettings.Current.HalfBottomLine;

    /// <summary>Box Drawings Heavy Left (U+2578) - â•¸</summary>

    public static Rune HalfLeftLineHv => GlyphSettings.Current.HalfLeftLineHv;

    /// <summary>Box Drawings Heavy Vertical (U+2579) - â•¹</summary>

    public static Rune HalfTopLineHv => GlyphSettings.Current.HalfTopLineHv;

    /// <summary>Box Drawings Heavy Horizontal (U+257A) - â•º</summary>

    public static Rune HalfRightLineHv => GlyphSettings.Current.HalfRightLineHv;

    /// <summary>Box Drawings Light Vertical and Horizontal (U+257B) - â•»</summary>

    public static Rune HalfBottomLineLt => GlyphSettings.Current.HalfBottomLineLt;

    /// <summary>Box Drawings Light Horizontal and Heavy Horizontal (U+257C) - â•¼</summary>

    public static Rune RightSideLineLtHv => GlyphSettings.Current.RightSideLineLtHv;

    /// <summary>Box Drawings Light Vertical and Heavy Horizontal (U+257D) - â•½</summary>

    public static Rune BottomSideLineLtHv => GlyphSettings.Current.BottomSideLineLtHv;

    /// <summary>Box Drawings Heavy Left and Light Horizontal (U+257E) - â•¾</summary>

    public static Rune LeftSideLineHvLt => GlyphSettings.Current.LeftSideLineHvLt;

    /// <summary>Box Drawings Heavy Vertical and Light Horizontal (U+257F) - â•¿</summary>

    public static Rune TopSideLineHvLt => GlyphSettings.Current.TopSideLineHvLt;

    #endregion

    #region ----------------- Upper Left Corners -----------------

    /// <summary>Box Drawings Upper Left Corner - Light Vertical and Light Horizontal (U+250C) - â”Œ</summary>

    public static Rune ULCorner => GlyphSettings.Current.ULCorner;

    /// <summary>Box Drawings Upper Left Corner -  Double (U+2554) - â•”</summary>

    public static Rune ULCornerDbl => GlyphSettings.Current.ULCornerDbl;

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Horizontal (U+256D) - â•­</summary>

    public static Rune ULCornerR => GlyphSettings.Current.ULCornerR;

    /// <summary>Box Drawings Heavy Down and Horizontal (U+250F) - â”</summary>

    public static Rune ULCornerHv => GlyphSettings.Current.ULCornerHv;

    /// <summary>Box Drawings Down Heavy and Horizontal Light (U+251E) - â”Ž</summary>

    public static Rune ULCornerHvLt => GlyphSettings.Current.ULCornerHvLt;

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+250D) - â”Ž</summary>

    public static Rune ULCornerLtHv => GlyphSettings.Current.ULCornerLtHv;

    /// <summary>Box Drawings Double Down and Single Horizontal (U+2553) - â•“</summary>

    public static Rune ULCornerDblSingle => GlyphSettings.Current.ULCornerDblSingle;

    /// <summary>Box Drawings Single Down and Double Horizontal (U+2552) - â•’</summary>

    public static Rune ULCornerSingleDbl => GlyphSettings.Current.ULCornerSingleDbl;

    #endregion

    #region ----------------- Lower Left Corners -----------------

    /// <summary>Box Drawings Lower Left Corner - Light Vertical and Light Horizontal (U+2514) - â””</summary>

    public static Rune LLCorner => GlyphSettings.Current.LLCorner;

    /// <summary>Box Drawings Heavy Vertical and Horizontal (U+2517) - â”—</summary>

    public static Rune LLCornerHv => GlyphSettings.Current.LLCornerHv;

    /// <summary>Box Drawings Heavy Vertical and Horizontal Light (U+2516) - â”–</summary>

    public static Rune LLCornerHvLt => GlyphSettings.Current.LLCornerHvLt;

    /// <summary>Box Drawings Vertical Light and Horizontal Heavy (U+2511) - â”•</summary>

    public static Rune LLCornerLtHv => GlyphSettings.Current.LLCornerLtHv;

    /// <summary>Box Drawings Double Vertical and Double Left (U+255A) - â•š</summary>

    public static Rune LLCornerDbl => GlyphSettings.Current.LLCornerDbl;

    /// <summary>Box Drawings Single Vertical and Double Left (U+2558) - â•˜</summary>

    public static Rune LLCornerSingleDbl => GlyphSettings.Current.LLCornerSingleDbl;

    /// <summary>Box Drawings Double Down and Single Left (U+2559) - â•™</summary>

    public static Rune LLCornerDblSingle => GlyphSettings.Current.LLCornerDblSingle;

    /// <summary>Box Drawings Upper Left Corner - Light Arc Down and Left (U+2570) - â•°</summary>

    public static Rune LLCornerR => GlyphSettings.Current.LLCornerR;

    #endregion

    #region ----------------- Upper Right Corners -----------------

    /// <summary>Box Drawings Upper Horizontal Corner - Light Vertical and Light Horizontal (U+2510) - â”</summary>

    public static Rune URCorner => GlyphSettings.Current.URCorner;

    /// <summary>Box Drawings Upper Horizontal Corner - Double Vertical and Double Horizontal (U+2557) - â•—</summary>

    public static Rune URCornerDbl => GlyphSettings.Current.URCornerDbl;

    /// <summary>Box Drawings Upper Horizontal Corner - Light Arc Vertical and Horizontal (U+256E) - â•®</summary>

    public static Rune URCornerR => GlyphSettings.Current.URCornerR;

    /// <summary>Box Drawings Heavy Down and Left (U+2513) - â”“</summary>

    public static Rune URCornerHv => GlyphSettings.Current.URCornerHv;

    /// <summary>Box Drawings Heavy Vertical and Left Down Light (U+2511) - â”‘</summary>

    public static Rune URCornerHvLt => GlyphSettings.Current.URCornerHvLt;

    /// <summary>Box Drawings Down Light and Horizontal Heavy (U+2514) - â”’</summary>

    public static Rune URCornerLtHv => GlyphSettings.Current.URCornerLtHv;

    /// <summary>Box Drawings Double Vertical and Single Left (U+2556) - â•–</summary>

    public static Rune URCornerDblSingle => GlyphSettings.Current.URCornerDblSingle;

    /// <summary>Box Drawings Single Vertical and Double Left (U+2555) - â••</summary>

    public static Rune URCornerSingleDbl => GlyphSettings.Current.URCornerSingleDbl;

    #endregion

    #region ----------------- Lower Right Corners -----------------

    /// <summary>Box Drawings Lower Right Corner - Light (U+2518) - â”˜</summary>

    public static Rune LRCorner => GlyphSettings.Current.LRCorner;

    /// <summary>Box Drawings Lower Right Corner - Double (U+255D) - â•</summary>

    public static Rune LRCornerDbl => GlyphSettings.Current.LRCornerDbl;

    /// <summary>Box Drawings Lower Right Corner - Rounded (U+256F) - â•¯</summary>

    public static Rune LRCornerR => GlyphSettings.Current.LRCornerR;

    /// <summary>Box Drawings Lower Right Corner - Heavy (U+251B) - â”›</summary>

    public static Rune LRCornerHv => GlyphSettings.Current.LRCornerHv;

    /// <summary>Box Drawings Lower Right Corner - Double Vertical and Single Horizontal (U+255C) - â•œ</summary>

    public static Rune LRCornerDblSingle => GlyphSettings.Current.LRCornerDblSingle;

    /// <summary>Box Drawings Lower Right Corner - Single Vertical and Double Horizontal (U+255B) - â•›</summary>

    public static Rune LRCornerSingleDbl => GlyphSettings.Current.LRCornerSingleDbl;

    /// <summary>Box Drawings Lower Right Corner - Light Vertical and Heavy Horizontal (U+2519) - â”™</summary>

    public static Rune LRCornerLtHv => GlyphSettings.Current.LRCornerLtHv;

    /// <summary>Box Drawings Lower Right Corner - Heavy Vertical and Light Horizontal (U+251A) - â”š</summary>

    public static Rune LRCornerHvLt => GlyphSettings.Current.LRCornerHvLt;

    #endregion

    #region ----------------- Tees -----------------

    /// <summary>Box Drawings Left Tee - Single Vertical and Single Horizontal (U+251C) - â”œ</summary>

    public static Rune LeftTee => GlyphSettings.Current.LeftTee;

    /// <summary>Box Drawings Left Tee - Single Vertical and Double Horizontal (U+255E) - â•ž</summary>

    public static Rune LeftTeeDblH => GlyphSettings.Current.LeftTeeDblH;

    /// <summary>Box Drawings Left Tee - Double Vertical and Single Horizontal (U+255F) - â•Ÿ</summary>

    public static Rune LeftTeeDblV => GlyphSettings.Current.LeftTeeDblV;

    /// <summary>Box Drawings Left Tee - Double Vertical and Double Horizontal (U+2560) - â• </summary>

    public static Rune LeftTeeDbl => GlyphSettings.Current.LeftTeeDbl;

    /// <summary>Box Drawings Left Tee - Heavy Horizontal and Light Vertical (U+2523) - â”</summary>

    public static Rune LeftTeeHvH => GlyphSettings.Current.LeftTeeHvH;

    /// <summary>Box Drawings Left Tee - Light Horizontal and Heavy Vertical (U+252B) - â” </summary>

    public static Rune LeftTeeHvV => GlyphSettings.Current.LeftTeeHvV;

    /// <summary>Box Drawings Left Tee - Heavy Vertical and Heavy Horizontal (U+2527) - â”£</summary>

    public static Rune LeftTeeHvDblH => GlyphSettings.Current.LeftTeeHvDblH;

    /// <summary>Box Drawings Right Tee - Single Vertical and Single Horizontal (U+2524) - â”¤</summary>

    public static Rune RightTee => GlyphSettings.Current.RightTee;

    /// <summary>Box Drawings Right Tee - Single Vertical and Double Horizontal (U+2561) - â•¡</summary>

    public static Rune RightTeeDblH => GlyphSettings.Current.RightTeeDblH;

    /// <summary>Box Drawings Right Tee - Double Vertical and Single Horizontal (U+2562) - â•¢</summary>

    public static Rune RightTeeDblV => GlyphSettings.Current.RightTeeDblV;

    /// <summary>Box Drawings Right Tee - Double Vertical and Double Horizontal (U+2563) - â•£</summary>

    public static Rune RightTeeDbl => GlyphSettings.Current.RightTeeDbl;

    /// <summary>Box Drawings Right Tee - Heavy Horizontal and Light Vertical (U+2528) - â”¥</summary>

    public static Rune RightTeeHvH => GlyphSettings.Current.RightTeeHvH;

    /// <summary>Box Drawings Right Tee - Light Horizontal and Heavy Vertical (U+2530) - â”¨</summary>

    public static Rune RightTeeHvV => GlyphSettings.Current.RightTeeHvV;

    /// <summary>Box Drawings Right Tee - Heavy Vertical and Heavy Horizontal (U+252C) - â”«</summary>

    public static Rune RightTeeHvDblH => GlyphSettings.Current.RightTeeHvDblH;

    /// <summary>Box Drawings Top Tee - Single Vertical and Single Horizontal (U+252C) - â”¬</summary>

    public static Rune TopTee => GlyphSettings.Current.TopTee;

    /// <summary>Box Drawings Top Tee - Single Vertical and Double Horizontal (U+2564) - â•¤</summary>

    public static Rune TopTeeDblH => GlyphSettings.Current.TopTeeDblH;

    /// <summary>Box Drawings Top Tee - Double Vertical and Single Horizontal  (U+2565) - â•¥</summary>

    public static Rune TopTeeDblV => GlyphSettings.Current.TopTeeDblV;

    /// <summary>Box Drawings Top Tee - Double Vertical and Double Horizontal (U+2566) - â•¦</summary>

    public static Rune TopTeeDbl => GlyphSettings.Current.TopTeeDbl;

    /// <summary>Box Drawings Top Tee - Heavy Horizontal and Light Vertical (U+252F) - â”¯</summary>

    public static Rune TopTeeHvH => GlyphSettings.Current.TopTeeHvH;

    /// <summary>Box Drawings Top Tee - Light Horizontal and Heavy Vertical (U+2537) - â”°</summary>

    public static Rune TopTeeHvV => GlyphSettings.Current.TopTeeHvV;

    /// <summary>Box Drawings Top Tee - Heavy Vertical and Heavy Horizontal (U+2533) - â”³</summary>

    public static Rune TopTeeHvDblH => GlyphSettings.Current.TopTeeHvDblH;

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Single Horizontal (U+2534) - â”´</summary>

    public static Rune BottomTee => GlyphSettings.Current.BottomTee;

    /// <summary>Box Drawings Bottom Tee - Single Vertical and Double Horizontal (U+2567) - â•§</summary>

    public static Rune BottomTeeDblH => GlyphSettings.Current.BottomTeeDblH;

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Single Horizontal (U+2568) - â•¨</summary>

    public static Rune BottomTeeDblV => GlyphSettings.Current.BottomTeeDblV;

    /// <summary>Box Drawings Bottom Tee - Double Vertical and Double Horizontal (U+2569) - â•©</summary>

    public static Rune BottomTeeDbl => GlyphSettings.Current.BottomTeeDbl;

    /// <summary>Box Drawings Bottom Tee - Heavy Horizontal and Light Vertical (U+2535) - â”·</summary>

    public static Rune BottomTeeHvH => GlyphSettings.Current.BottomTeeHvH;

    /// <summary>Box Drawings Bottom Tee - Light Horizontal and Heavy Vertical (U+253D) - â”¸</summary>

    public static Rune BottomTeeHvV => GlyphSettings.Current.BottomTeeHvV;

    /// <summary>Box Drawings Bottom Tee - Heavy Vertical and Heavy Horizontal (U+2539) - â”»</summary>

    public static Rune BottomTeeHvDblH => GlyphSettings.Current.BottomTeeHvDblH;

    #endregion

    #region ----------------- Crosses -----------------

    /// <summary>Box Drawings Cross - Single Vertical and Single Horizontal (U+253C) - â”¼</summary>

    public static Rune Cross => GlyphSettings.Current.Cross;

    /// <summary>Box Drawings Cross - Single Vertical and Double Horizontal (U+256A) - â•ª</summary>

    public static Rune CrossDblH => GlyphSettings.Current.CrossDblH;

    /// <summary>Box Drawings Cross - Double Vertical and Single Horizontal (U+256B) - â•«</summary>

    public static Rune CrossDblV => GlyphSettings.Current.CrossDblV;

    /// <summary>Box Drawings Cross - Double Vertical and Double Horizontal (U+256C) - â•¬</summary>

    public static Rune CrossDbl => GlyphSettings.Current.CrossDbl;

    /// <summary>Box Drawings Cross - Heavy Horizontal and Light Vertical (U+253F) - â”¿</summary>

    public static Rune CrossHvH => GlyphSettings.Current.CrossHvH;

    /// <summary>Box Drawings Cross - Light Horizontal and Heavy Vertical (U+2541) - â•‚</summary>

    public static Rune CrossHvV => GlyphSettings.Current.CrossHvV;

    /// <summary>Box Drawings Cross - Heavy Vertical and Heavy Horizontal (U+254B) - â•‹</summary>

    public static Rune CrossHv => GlyphSettings.Current.CrossHv;

    #endregion

    #region ----------------- ShadowStyle -----------------

    /// <summary>Shadow - Vertical Start - Left Half Block - â–Œ U+0258c</summary>

    public static Rune ShadowVerticalStart => GlyphSettings.Current.ShadowVerticalStart;

    /// <summary>Shadow - Vertical - Left Half Block - â–Œ U+0258c</summary>

    public static Rune ShadowVertical => GlyphSettings.Current.ShadowVertical;

    /// <summary>Shadow - Horizontal Start - Upper Half Block - â–€ U+02580</summary>

    public static Rune ShadowHorizontalStart => GlyphSettings.Current.ShadowHorizontalStart;

    /// <summary>Shadow - Horizontal - Upper Half Block - â–€ U+02580</summary>

    public static Rune ShadowHorizontal => GlyphSettings.Current.ShadowHorizontal;

    /// <summary>Shadow - Horizontal End - Quadrant Upper Left - â–˜ U+02598</summary>

    public static Rune ShadowHorizontalEnd => GlyphSettings.Current.ShadowHorizontalEnd;

    #endregion
}