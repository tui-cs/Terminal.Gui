namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for <see cref="Drawing.Glyphs"/> defaults (ThemeScope).
/// </summary>
public sealed record GlyphSettings
{
    /// <summary>Unicode replacement character; used when a wide glyph can't be output because it would be clipped.</summary>
    public Rune WideGlyphReplacement { get; init; } = (Rune)' ';

    /// <summary>File icon.</summary>
    public Rune File { get; init; } = (Rune)'☰';

    /// <summary>Folder icon.</summary>
    public Rune Folder { get; init; } = (Rune)'꤉';

    /// <summary>Horizontal Ellipsis.</summary>
    public Rune HorizontalEllipsis { get; init; } = (Rune)'…';

    /// <summary>Vertical Four Dots.</summary>
    public Rune VerticalFourDots { get; init; } = (Rune)'⁞';

    /// <summary>Null symbol.</summary>
    public Rune Null { get; init; } = (Rune)'␀';

    /// <summary>Checked indicator.</summary>
    public Rune CheckStateChecked { get; init; } = (Rune)'☑';

    /// <summary>Not Checked indicator.</summary>
    public Rune CheckStateUnChecked { get; init; } = (Rune)'☐';

    /// <summary>Null Checked indicator.</summary>
    public Rune CheckStateNone { get; init; } = (Rune)'⬛';

    /// <summary>Selected indicator.</summary>
    public Rune Selected { get; init; } = (Rune)'◉';

    /// <summary>Not Selected indicator.</summary>
    public Rune UnSelected { get; init; } = (Rune)'○';

    /// <summary>Right arrow.</summary>
    public Rune RightArrow { get; init; } = (Rune)'►';

    /// <summary>Left arrow.</summary>
    public Rune LeftArrow { get; init; } = (Rune)'◄';

    /// <summary>Down arrow.</summary>
    public Rune DownArrow { get; init; } = (Rune)'▼';

    /// <summary>Up arrow.</summary>
    public Rune UpArrow { get; init; } = (Rune)'▲';

    /// <summary>Left default indicator.</summary>
    public Rune LeftDefaultIndicator { get; init; } = (Rune)'►';

    /// <summary>Right default indicator.</summary>
    public Rune RightDefaultIndicator { get; init; } = (Rune)'◄';

    /// <summary>Left Bracket.</summary>
    public Rune LeftBracket { get; init; } = (Rune)'⟦';

    /// <summary>Right Bracket.</summary>
    public Rune RightBracket { get; init; } = (Rune)'⟧';

    /// <summary>Half block meter segment.</summary>
    public Rune BlocksMeterSegment { get; init; } = (Rune)'▌';

    /// <summary>Continuous block meter segment.</summary>
    public Rune ContinuousMeterSegment { get; init; } = (Rune)'█';

    /// <summary>Stipple pattern.</summary>
    public Rune Stipple { get; init; } = (Rune)'░';

    /// <summary>Diamond.</summary>
    public Rune Diamond { get; init; } = (Rune)'◊';

    /// <summary>Close.</summary>
    public Rune Close { get; init; } = (Rune)'✘';

    /// <summary>Minimize.</summary>
    public Rune Minimize { get; init; } = (Rune)'❏';

    /// <summary>Maximize.</summary>
    public Rune Maximize { get; init; } = (Rune)'✽';

    /// <summary>Dot.</summary>
    public Rune Dot { get; init; } = (Rune)'∙';

    /// <summary>Dotted Square.</summary>
    public Rune DottedSquare { get; init; } = (Rune)'⬚';

    /// <summary>Black Circle.</summary>
    public Rune BlackCircle { get; init; } = (Rune)'●';

    /// <summary>Expand.</summary>
    public Rune Expand { get; init; } = (Rune)'+';

    /// <summary>Collapse.</summary>
    public Rune Collapse { get; init; } = (Rune)'-';

    /// <summary>Identical To.</summary>
    public Rune IdenticalTo { get; init; } = (Rune)'≡';

    /// <summary>Move indicator.</summary>
    public Rune Move { get; init; } = (Rune)'◊';

    /// <summary>Size Horizontally indicator.</summary>
    public Rune SizeHorizontal { get; init; } = (Rune)'↔';

    /// <summary>Size Vertical indicator.</summary>
    public Rune SizeVertical { get; init; } = (Rune)'↕';

    /// <summary>Size Top Left indicator.</summary>
    public Rune SizeTopLeft { get; init; } = (Rune)'↖';

    /// <summary>Size Top Right indicator.</summary>
    public Rune SizeTopRight { get; init; } = (Rune)'↗';

    /// <summary>Size Bottom Right indicator.</summary>
    public Rune SizeBottomRight { get; init; } = (Rune)'↘';

    /// <summary>Size Bottom Left indicator.</summary>
    public Rune SizeBottomLeft { get; init; } = (Rune)'↙';

    /// <summary>Apple (non-BMP).</summary>
    public Rune Apple { get; init; } = "🍎".ToRunes () [0];

    /// <summary>Apple (BMP).</summary>
    public Rune AppleBMP { get; init; } = (Rune)'❦';

    /// <summary>Copy indicator.</summary>
    public Rune Copy { get; init; } = (Rune)'⧉';

    /// <summary>Box Drawings Horizontal Line - Light.</summary>
    public Rune HLine { get; init; } = (Rune)'─';

    /// <summary>Box Drawings Vertical Line - Light.</summary>
    public Rune VLine { get; init; } = (Rune)'│';

    /// <summary>Box Drawings Double Horizontal.</summary>
    public Rune HLineDbl { get; init; } = (Rune)'═';

    /// <summary>Box Drawings Double Vertical.</summary>
    public Rune VLineDbl { get; init; } = (Rune)'║';

    /// <summary>Box Drawings Heavy Double Dash Horizontal.</summary>
    public Rune HLineHvDa2 { get; init; } = (Rune)'╍';

    /// <summary>Box Drawings Heavy Triple Dash Vertical.</summary>
    public Rune VLineHvDa3 { get; init; } = (Rune)'┇';

    /// <summary>Box Drawings Heavy Triple Dash Horizontal.</summary>
    public Rune HLineHvDa3 { get; init; } = (Rune)'┅';

    /// <summary>Box Drawings Heavy Quadruple Dash Horizontal.</summary>
    public Rune HLineHvDa4 { get; init; } = (Rune)'┉';

    /// <summary>Box Drawings Heavy Double Dash Vertical.</summary>
    public Rune VLineHvDa2 { get; init; } = (Rune)'╏';

    /// <summary>Box Drawings Heavy Quadruple Dash Vertical.</summary>
    public Rune VLineHvDa4 { get; init; } = (Rune)'┋';

    /// <summary>Box Drawings Light Double Dash Horizontal.</summary>
    public Rune HLineDa2 { get; init; } = (Rune)'╌';

    /// <summary>Box Drawings Light Triple Dash Vertical.</summary>
    public Rune VLineDa3 { get; init; } = (Rune)'┆';

    /// <summary>Box Drawings Light Triple Dash Horizontal.</summary>
    public Rune HLineDa3 { get; init; } = (Rune)'┄';

    /// <summary>Box Drawings Light Quadruple Dash Horizontal.</summary>
    public Rune HLineDa4 { get; init; } = (Rune)'┈';

    /// <summary>Box Drawings Light Double Dash Vertical.</summary>
    public Rune VLineDa2 { get; init; } = (Rune)'╎';

    /// <summary>Box Drawings Light Quadruple Dash Vertical.</summary>
    public Rune VLineDa4 { get; init; } = (Rune)'┊';

    /// <summary>Box Drawings Heavy Horizontal.</summary>
    public Rune HLineHv { get; init; } = (Rune)'━';

    /// <summary>Box Drawings Heavy Vertical.</summary>
    public Rune VLineHv { get; init; } = (Rune)'┃';

    /// <summary>Box Drawings Light Left.</summary>
    public Rune HalfLeftLine { get; init; } = (Rune)'╴';

    /// <summary>Box Drawings Light Up.</summary>
    public Rune HalfTopLine { get; init; } = (Rune)'╵';

    /// <summary>Box Drawings Light Right.</summary>
    public Rune HalfRightLine { get; init; } = (Rune)'╶';

    /// <summary>Box Drawings Light Down.</summary>
    public Rune HalfBottomLine { get; init; } = (Rune)'╷';

    /// <summary>Box Drawings Heavy Left.</summary>
    public Rune HalfLeftLineHv { get; init; } = (Rune)'╸';

    /// <summary>Box Drawings Heavy Up.</summary>
    public Rune HalfTopLineHv { get; init; } = (Rune)'╹';

    /// <summary>Box Drawings Heavy Right.</summary>
    public Rune HalfRightLineHv { get; init; } = (Rune)'╺';

    /// <summary>Box Drawings Light Down Heavy.</summary>
    public Rune HalfBottomLineLt { get; init; } = (Rune)'╻';

    /// <summary>Box Drawings Light Horizontal and Heavy Horizontal.</summary>
    public Rune RightSideLineLtHv { get; init; } = (Rune)'╼';

    /// <summary>Box Drawings Light Vertical and Heavy Horizontal.</summary>
    public Rune BottomSideLineLtHv { get; init; } = (Rune)'╽';

    /// <summary>Box Drawings Heavy Left and Light Horizontal.</summary>
    public Rune LeftSideLineHvLt { get; init; } = (Rune)'╾';

    /// <summary>Box Drawings Heavy Vertical and Light Horizontal.</summary>
    public Rune TopSideLineHvLt { get; init; } = (Rune)'╿';

    /// <summary>Box Drawings Upper Left Corner - Light.</summary>
    public Rune ULCorner { get; init; } = (Rune)'┌';

    /// <summary>Box Drawings Upper Left Corner - Double.</summary>
    public Rune ULCornerDbl { get; init; } = (Rune)'╔';

    /// <summary>Box Drawings Upper Left Corner - Rounded.</summary>
    public Rune ULCornerR { get; init; } = (Rune)'╭';

    /// <summary>Box Drawings Upper Left Corner - Heavy.</summary>
    public Rune ULCornerHv { get; init; } = (Rune)'┏';

    /// <summary>Box Drawings Upper Left Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune ULCornerHvLt { get; init; } = (Rune)'┎';

    /// <summary>Box Drawings Upper Left Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune ULCornerLtHv { get; init; } = (Rune)'┍';

    /// <summary>Box Drawings Upper Left Corner - Double Down Single Horizontal.</summary>
    public Rune ULCornerDblSingle { get; init; } = (Rune)'╓';

    /// <summary>Box Drawings Upper Left Corner - Single Down Double Horizontal.</summary>
    public Rune ULCornerSingleDbl { get; init; } = (Rune)'╒';

    /// <summary>Box Drawings Lower Left Corner - Light.</summary>
    public Rune LLCorner { get; init; } = (Rune)'└';

    /// <summary>Box Drawings Lower Left Corner - Heavy.</summary>
    public Rune LLCornerHv { get; init; } = (Rune)'┗';

    /// <summary>Box Drawings Lower Left Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune LLCornerHvLt { get; init; } = (Rune)'┖';

    /// <summary>Box Drawings Lower Left Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune LLCornerLtHv { get; init; } = (Rune)'┕';

    /// <summary>Box Drawings Lower Left Corner - Double.</summary>
    public Rune LLCornerDbl { get; init; } = (Rune)'╚';

    /// <summary>Box Drawings Lower Left Corner - Single Vertical Double Horizontal.</summary>
    public Rune LLCornerSingleDbl { get; init; } = (Rune)'╘';

    /// <summary>Box Drawings Lower Left Corner - Double Vertical Single Horizontal.</summary>
    public Rune LLCornerDblSingle { get; init; } = (Rune)'╙';

    /// <summary>Box Drawings Lower Left Corner - Rounded.</summary>
    public Rune LLCornerR { get; init; } = (Rune)'╰';

    /// <summary>Box Drawings Upper Right Corner - Light.</summary>
    public Rune URCorner { get; init; } = (Rune)'┐';

    /// <summary>Box Drawings Upper Right Corner - Double.</summary>
    public Rune URCornerDbl { get; init; } = (Rune)'╗';

    /// <summary>Box Drawings Upper Right Corner - Rounded.</summary>
    public Rune URCornerR { get; init; } = (Rune)'╮';

    /// <summary>Box Drawings Upper Right Corner - Heavy.</summary>
    public Rune URCornerHv { get; init; } = (Rune)'┓';

    /// <summary>Box Drawings Upper Right Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune URCornerHvLt { get; init; } = (Rune)'┑';

    /// <summary>Box Drawings Upper Right Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune URCornerLtHv { get; init; } = (Rune)'┒';

    /// <summary>Box Drawings Upper Right Corner - Double Vertical Single Horizontal.</summary>
    public Rune URCornerDblSingle { get; init; } = (Rune)'╖';

    /// <summary>Box Drawings Upper Right Corner - Single Vertical Double Horizontal.</summary>
    public Rune URCornerSingleDbl { get; init; } = (Rune)'╕';

    /// <summary>Box Drawings Lower Right Corner - Light.</summary>
    public Rune LRCorner { get; init; } = (Rune)'┘';

    /// <summary>Box Drawings Lower Right Corner - Double.</summary>
    public Rune LRCornerDbl { get; init; } = (Rune)'╝';

    /// <summary>Box Drawings Lower Right Corner - Rounded.</summary>
    public Rune LRCornerR { get; init; } = (Rune)'╯';

    /// <summary>Box Drawings Lower Right Corner - Heavy.</summary>
    public Rune LRCornerHv { get; init; } = (Rune)'┛';

    /// <summary>Box Drawings Lower Right Corner - Double Vertical Single Horizontal.</summary>
    public Rune LRCornerDblSingle { get; init; } = (Rune)'╜';

    /// <summary>Box Drawings Lower Right Corner - Single Vertical Double Horizontal.</summary>
    public Rune LRCornerSingleDbl { get; init; } = (Rune)'╛';

    /// <summary>Box Drawings Lower Right Corner - Light Vertical Heavy Horizontal.</summary>
    public Rune LRCornerLtHv { get; init; } = (Rune)'┙';

    /// <summary>Box Drawings Lower Right Corner - Heavy Vertical Light Horizontal.</summary>
    public Rune LRCornerHvLt { get; init; } = (Rune)'┚';

    /// <summary>Box Drawings Left Tee - Light.</summary>
    public Rune LeftTee { get; init; } = (Rune)'├';

    /// <summary>Box Drawings Left Tee - Single Vertical Double Horizontal.</summary>
    public Rune LeftTeeDblH { get; init; } = (Rune)'╞';

    /// <summary>Box Drawings Left Tee - Double Vertical Single Horizontal.</summary>
    public Rune LeftTeeDblV { get; init; } = (Rune)'╟';

    /// <summary>Box Drawings Left Tee - Double.</summary>
    public Rune LeftTeeDbl { get; init; } = (Rune)'╠';

    /// <summary>Box Drawings Left Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune LeftTeeHvH { get; init; } = (Rune)'┝';

    /// <summary>Box Drawings Left Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune LeftTeeHvV { get; init; } = (Rune)'┠';

    /// <summary>Box Drawings Left Tee - Heavy.</summary>
    public Rune LeftTeeHvDblH { get; init; } = (Rune)'┣';

    /// <summary>Box Drawings Right Tee - Light.</summary>
    public Rune RightTee { get; init; } = (Rune)'┤';

    /// <summary>Box Drawings Right Tee - Single Vertical Double Horizontal.</summary>
    public Rune RightTeeDblH { get; init; } = (Rune)'╡';

    /// <summary>Box Drawings Right Tee - Double Vertical Single Horizontal.</summary>
    public Rune RightTeeDblV { get; init; } = (Rune)'╢';

    /// <summary>Box Drawings Right Tee - Double.</summary>
    public Rune RightTeeDbl { get; init; } = (Rune)'╣';

    /// <summary>Box Drawings Right Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune RightTeeHvH { get; init; } = (Rune)'┥';

    /// <summary>Box Drawings Right Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune RightTeeHvV { get; init; } = (Rune)'┨';

    /// <summary>Box Drawings Right Tee - Heavy.</summary>
    public Rune RightTeeHvDblH { get; init; } = (Rune)'┫';

    /// <summary>Box Drawings Top Tee - Light.</summary>
    public Rune TopTee { get; init; } = (Rune)'┬';

    /// <summary>Box Drawings Top Tee - Single Vertical Double Horizontal.</summary>
    public Rune TopTeeDblH { get; init; } = (Rune)'╤';

    /// <summary>Box Drawings Top Tee - Double Vertical Single Horizontal.</summary>
    public Rune TopTeeDblV { get; init; } = (Rune)'╥';

    /// <summary>Box Drawings Top Tee - Double.</summary>
    public Rune TopTeeDbl { get; init; } = (Rune)'╦';

    /// <summary>Box Drawings Top Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune TopTeeHvH { get; init; } = (Rune)'┯';

    /// <summary>Box Drawings Top Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune TopTeeHvV { get; init; } = (Rune)'┰';

    /// <summary>Box Drawings Top Tee - Heavy.</summary>
    public Rune TopTeeHvDblH { get; init; } = (Rune)'┳';

    /// <summary>Box Drawings Bottom Tee - Light.</summary>
    public Rune BottomTee { get; init; } = (Rune)'┴';

    /// <summary>Box Drawings Bottom Tee - Single Vertical Double Horizontal.</summary>
    public Rune BottomTeeDblH { get; init; } = (Rune)'╧';

    /// <summary>Box Drawings Bottom Tee - Double Vertical Single Horizontal.</summary>
    public Rune BottomTeeDblV { get; init; } = (Rune)'╨';

    /// <summary>Box Drawings Bottom Tee - Double.</summary>
    public Rune BottomTeeDbl { get; init; } = (Rune)'╩';

    /// <summary>Box Drawings Bottom Tee - Heavy Horizontal Light Vertical.</summary>
    public Rune BottomTeeHvH { get; init; } = (Rune)'┷';

    /// <summary>Box Drawings Bottom Tee - Light Horizontal Heavy Vertical.</summary>
    public Rune BottomTeeHvV { get; init; } = (Rune)'┸';

    /// <summary>Box Drawings Bottom Tee - Heavy.</summary>
    public Rune BottomTeeHvDblH { get; init; } = (Rune)'┻';

    /// <summary>Box Drawings Cross - Light.</summary>
    public Rune Cross { get; init; } = (Rune)'┼';

    /// <summary>Box Drawings Cross - Single Vertical Double Horizontal.</summary>
    public Rune CrossDblH { get; init; } = (Rune)'╪';

    /// <summary>Box Drawings Cross - Double Vertical Single Horizontal.</summary>
    public Rune CrossDblV { get; init; } = (Rune)'╫';

    /// <summary>Box Drawings Cross - Double.</summary>
    public Rune CrossDbl { get; init; } = (Rune)'╬';

    /// <summary>Box Drawings Cross - Heavy Horizontal Light Vertical.</summary>
    public Rune CrossHvH { get; init; } = (Rune)'┿';

    /// <summary>Box Drawings Cross - Light Horizontal Heavy Vertical.</summary>
    public Rune CrossHvV { get; init; } = (Rune)'╂';

    /// <summary>Box Drawings Cross - Heavy.</summary>
    public Rune CrossHv { get; init; } = (Rune)'╋';

    /// <summary>Shadow - Vertical Start.</summary>
    public Rune ShadowVerticalStart { get; init; } = (Rune)'▖';

    /// <summary>Shadow - Vertical.</summary>
    public Rune ShadowVertical { get; init; } = (Rune)'▌';

    /// <summary>Shadow - Horizontal Start.</summary>
    public Rune ShadowHorizontalStart { get; init; } = (Rune)'▝';

    /// <summary>Shadow - Horizontal.</summary>
    public Rune ShadowHorizontal { get; init; } = (Rune)'▀';

    /// <summary>Shadow - Horizontal End.</summary>
    public Rune ShadowHorizontalEnd { get; init; } = (Rune)'▘';

    /// <summary>The compile-time-known defaults.</summary>
    public static GlyphSettings Default { get; } = new ();

    /// <summary>The currently effective values, updated atomically by <see cref="MecThemeManager"/>.</summary>
    public static GlyphSettings Current
    {
        get => Volatile.Read (ref _current);
        internal set => Volatile.Write (ref _current, value);
    }

    private static GlyphSettings _current = Default;
}
