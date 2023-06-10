using static Terminal.Gui.ConfigurationManager;
using System.Text.Json.Serialization;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Defines the standard set of glyphs used to draw checkboxes, lines, borders, etc...
	/// </summary>
	/// <remarks>
	/// <para>
	/// Access with <see cref="CM.Glyphs"/> (which is a global using alias for <see cref="ConfigurationManager.Glyphs"/>).
	/// </para>
	/// <para>
	/// The default glyphs can be changed via the <see cref="ConfigurationManager"/>. Within a <c>config.json</c> file 
	/// The Json property name is the property name prefixed with "Glyphs.". 
	/// </para>
	/// <para>
	/// The JSon property can be either a decimal number or a string. The string may be one of:
	/// - A unicode char (e.g. "☑")
	/// - A hex value in U+ format (e.g. "U+2611")
	/// - A hex value in UTF-16 format (e.g. "\\u2611")
	/// </para>
	/// </remarks>
	public class GlyphDefinitions {
		#region ----------------- Single Glyphs -----------------

		/// <summary>
		/// Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).
		/// </summary>
		public Rune Checked { get; set; } = (Rune)'☑';

		/// <summary>
		/// Not Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).
		/// </summary>
		public Rune UnChecked { get; set; } = (Rune)'☐';

		/// <summary>
		/// Null Checked indicator (e.g. for <see cref="ListView"/> and <see cref="CheckBox"/>).
		/// </summary>
		public Rune NullChecked { get; set; } = (Rune)'☒';

		/// <summary>
		/// Selected indicator  (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).
		/// </summary>
		public Rune Selected { get; set; } = (Rune)'◉';

		/// <summary>
		/// Not Selected indicator (e.g. for <see cref="ListView"/> and <see cref="RadioGroup"/>).
		/// </summary>
		public Rune UnSelected { get; set; } = (Rune)'○';

		/// <summary>
		/// Horizontal arrow.
		/// </summary>
		public Rune RightArrow { get; set; } = (Rune)'►';

		/// <summary>
		/// Left arrow.
		/// </summary>
		public Rune LeftArrow { get; set; } = (Rune)'◄';

		/// <summary>
		/// Down arrow.
		/// </summary>
		public Rune DownArrow { get; set; } = (Rune)'▼';

		/// <summary>
		/// Vertical arrow.
		/// </summary>
		public Rune UpArrow { get; set; } = (Rune)'▲';

		/// <summary>
		/// Left default indicator (e.g. for <see cref="Button"/>.
		/// </summary>
		public Rune LeftDefaultIndicator { get; set; } = (Rune)'►';

		/// <summary>
		/// Horizontal default indicator (e.g. for <see cref="Button"/>.
		/// </summary>
		public Rune RightDefaultIndicator { get; set; } = (Rune)'◄';

		/// <summary>
		/// Left Bracket (e.g. for <see cref="Button"/>. Default is (U+005B) - [.
		/// </summary>
		public Rune LeftBracket { get; set; } = (Rune)'⟦';

		/// <summary>
		/// Horizontal Bracket (e.g. for <see cref="Button"/>. Default is (U+005D) - ].
		/// </summary>
		public Rune RightBracket { get; set; } = (Rune)'⟧';

		/// <summary>
		/// Half block meter segment (e.g. for <see cref="ProgressBar"/>).
		/// </summary>
		public Rune BlocksMeterSegment { get; set; } = (Rune)'▌';

		/// <summary>
		/// Continuous block meter segment (e.g. for <see cref="ProgressBar"/>).
		/// </summary>
		public Rune ContinuousMeterSegment { get; set; } = (Rune)'█';

		/// <summary>
		/// Stipple pattern (e.g. for <see cref="ScrollBarView"/>). Default is Light Shade (U+2591) - ░.
		/// </summary>
		public Rune Stipple { get; set; } = (Rune)'░';

		/// <summary>
		/// Diamond (e.g. for <see cref="ScrollBarView"/>. Default is Lozenge (U+25CA) - ◊.
		/// </summary>
		public Rune Diamond { get; set; } = (Rune)'◊';

		/// <summary>
		/// Close. Default is Heavy Ballot X (U+2718) - ✘.
		/// </summary>
		public Rune Close { get; set; } = (Rune)'✘';

		/// <summary>
		/// Minimize. Default is Lower Horizontal Shadowed White Circle (U+274F) - ❏.
		/// </summary>
		public Rune Minimize { get; set; } = (Rune)'❏';

		/// <summary>
		/// Maximize. Default is Upper Horizontal Shadowed White Circle (U+273D) - ✽.
		/// </summary>
		public Rune Maximize { get; set; } = (Rune)'✽';

		/// <summary>
		/// Dot. Default is (U+2219) - ∙.
		/// </summary>
		public Rune Dot { get; set; } = (Rune)'∙';

		/// <summary>
		/// Expand (e.g. for <see cref="TreeView"/>.
		/// </summary>
		public Rune Expand { get; set; } = (Rune)'+';

		/// <summary>
		/// Expand (e.g. for <see cref="TreeView"/>.
		/// </summary>
		public Rune Collapse { get; set; } = (Rune)'-';

		/// <summary>
		/// Apple (non-BMP). Because snek. And because it's an example of a non-BMP surrogate pair. See Issue #2610.
		/// </summary>
		public Rune Apple { get; set; } = "🍎".ToRunes () [0]; // nonBMP

		/// <summary>
		/// Apple (BMP). Because snek. See Issue #2610.
		/// </summary>
		public Rune AppleBMP { get; set; } = (Rune)'❦';

		///// <summary>
		///// A nonprintable (low surrogate) that should fail to ctor.
		///// </summary>
		//public Rune InvalidGlyph { get; set; } = (Rune)'\ud83d';

		#endregion

		/// <summary>
		/// Folder icon.  Defaults to ꤉ (Kayah Li Digit Nine)
		/// </summary>
		public Rune Folder { get; set; } = (Rune)'꤉';

		/// <summary>
		/// File icon.  Defaults to ☰ (Trigram For Heaven)
		/// </summary>
		public Rune File { get; set; } = (Rune)'☰';

		#region ----------------- Lines -----------------
		/// <summary>
		/// Box Drawings Horizontal Line - Light (U+2500) - ─
		/// </summary>
		public Rune HLine { get; set; } = (Rune)'─';

		/// <summary>
		/// Box Drawings Vertical Line - Light (U+2502) - │
		/// </summary>
		public Rune VLine { get; set; } = (Rune)'│';

		/// <summary>
		/// Box Drawings Double Horizontal (U+2550) - ═
		/// </summary>
		public Rune HLineDbl { get; set; } = (Rune)'═';

		/// <summary>
		/// Box Drawings Double Vertical (U+2551) - ║
		/// </summary>
		public Rune VLineDbl { get; set; } = (Rune)'║';

		/// <summary>
		/// Box Drawings Heavy Double Dash Horizontal (U+254D) - ╍
		/// </summary>
		public Rune HLineHvDa2 { get; set; } = (Rune)'╍';

		/// <summary>
		/// Box Drawings Heavy Triple Dash Vertical (U+2507) - ┇
		/// </summary>
		public Rune VLineHvDa3 { get; set; } = (Rune)'┇';

		/// <summary>
		/// Box Drawings Heavy Triple Dash Horizontal (U+2505) - ┅
		/// </summary>
		public Rune HLineHvDa3 { get; set; } = (Rune)'┅';

		/// <summary>
		/// Box Drawings Heavy Quadruple Dash Horizontal (U+2509) - ┉
		/// </summary>
		public Rune HLineHvDa4 { get; set; } = (Rune)'┉';

		/// <summary>
		/// Box Drawings Heavy Double Dash Vertical (U+254F) - ╏
		/// </summary>
		public Rune VLineHvDa2 { get; set; } = (Rune)'╏';

		/// <summary>
		/// Box Drawings Heavy Quadruple Dash Vertical (U+250B) - ┋
		/// </summary>
		public Rune VLineHvDa4 { get; set; } = (Rune)'┋';

		/// <summary>
		/// Box Drawings Light Double Dash Horizontal (U+254C) - ╌
		/// </summary>
		public Rune HLineDa2 { get; set; } = (Rune)'╌';

		/// <summary>
		/// Box Drawings Light Triple Dash Vertical (U+2506) - ┆
		/// </summary>
		public Rune VLineDa3 { get; set; } = (Rune)'┆';

		/// <summary>
		/// Box Drawings Light Triple Dash Horizontal (U+2504) - ┄
		/// </summary>
		public Rune HLineDa3 { get; set; } = (Rune)'┄';

		/// <summary>
		/// Box Drawings Light Quadruple Dash Horizontal (U+2508) - ┈
		/// </summary>
		public Rune HLineDa4 { get; set; } = (Rune)'┈';

		/// <summary>
		/// Box Drawings Light Double Dash Vertical (U+254E) - ╎
		/// </summary>
		public Rune VLineDa2 { get; set; } = (Rune)'╎';

		/// <summary>
		/// Box Drawings Light Quadruple Dash Vertical (U+250A) - ┊
		/// </summary>
		public Rune VLineDa4 { get; set; } = (Rune)'┊';

		/// <summary>
		/// Box Drawings Heavy Horizontal (U+2501) - ━
		/// </summary>
		public Rune HLineHv { get; set; } = (Rune)'━';

		/// <summary>
		/// Box Drawings Heavy Vertical (U+2503) - ┃
		/// </summary>
		public Rune VLineHv { get; set; } = (Rune)'┃';

		/// <summary>
		/// Box Drawings Light Left (U+2574) - ╴
		/// </summary>
		public Rune HalfLeftLine { get; set; } = (Rune)'╴';

		/// <summary>
		/// Box Drawings Light Vertical (U+2575) - ╵
		/// </summary>
		public Rune HalfTopLine { get; set; } = (Rune)'╵';

		/// <summary>
		/// Box Drawings Light Horizontal (U+2576) - ╶
		/// </summary>
		public Rune HalfRightLine { get; set; } = (Rune)'╶';

		/// <summary>
		/// Box Drawings Light Down (U+2577) - ╷
		/// </summary>
		public Rune HalfBottomLine { get; set; } = (Rune)'╷';

		/// <summary>
		/// Box Drawings Heavy Left (U+2578) - ╸
		/// </summary>
		public Rune HalfLeftLineHv { get; set; } = (Rune)'╸';

		/// <summary>
		/// Box Drawings Heavy Vertical (U+2579) - ╹
		/// </summary>
		public Rune HalfTopLineHv { get; set; } = (Rune)'╹';

		/// <summary>
		/// Box Drawings Heavy Horizontal (U+257A) - ╺
		/// </summary>
		public Rune HalfRightLineHv { get; set; } = (Rune)'╺';

		/// <summary>
		/// Box Drawings Light Vertical and Horizontal (U+257B) - ╻
		/// </summary>
		public Rune HalfBottomLineLt { get; set; } = (Rune)'╻';

		/// <summary>
		/// Box Drawings Light Horizontal and Heavy Horizontal (U+257C) - ╼
		/// </summary>
		public Rune RightSideLineLtHv { get; set; } = (Rune)'╼';

		/// <summary>
		/// Box Drawings Light Vertical and Heavy Horizontal (U+257D) - ╽
		/// </summary>
		public Rune BottomSideLineLtHv { get; set; } = (Rune)'╽';

		/// <summary>
		/// Box Drawings Heavy Left and Light Horizontal (U+257E) - ╾
		/// </summary>
		public Rune LeftSideLineHvLt { get; set; } = (Rune)'╾';

		/// <summary>
		/// Box Drawings Heavy Vertical and Light Horizontal (U+257F) - ╿
		/// </summary>
		public Rune TopSideLineHvLt { get; set; } = (Rune)'╿';
		#endregion

		#region ----------------- Upper Left Corners -----------------
		/// <summary>
		/// Box Drawings Upper Left Corner - Light Vertical and Light Horizontal (U+250C) - ┌
		/// </summary>
		public Rune ULCorner { get; set; } = (Rune)'┌';

		/// <summary>
		/// Box Drawings Upper Left Corner -  Double (U+2554) - ╔
		/// </summary>
		public Rune ULCornerDbl { get; set; } = (Rune)'╔';

		/// <summary>
		/// Box Drawings Upper Left Corner - Light Arc Down and Horizontal (U+256D) - ╭
		/// </summary>
		public Rune ULCornerR { get; set; } = (Rune)'╭';

		/// <summary>
		/// Box Drawings Heavy Down and Horizontal (U+250F) - ┏
		/// </summary>
		public Rune ULCornerHv { get; set; } = (Rune)'┏';

		/// <summary>
		/// Box Drawings Down Heavy and Horizontal Light (U+251E) - ┎
		/// </summary>
		public Rune ULCornerHvLt { get; set; } = (Rune)'┎';

		/// <summary>
		/// Box Drawings Down Light and Horizontal Heavy (U+250D) - ┎
		/// </summary>
		public Rune ULCornerLtHv { get; set; } = (Rune)'┍';

		/// <summary>
		/// Box Drawings Double Down and Single Horizontal (U+2553) - ╓
		/// </summary>
		public Rune ULCornerDblSingle { get; set; } = (Rune)'╓';

		/// <summary>
		/// Box Drawings Single Down and Double Horizontal (U+2552) - ╒
		/// </summary>
		public Rune ULCornerSingleDbl { get; set; } = (Rune)'╒';
		#endregion

		#region ----------------- Lower Left Corners -----------------
		/// <summary>
		/// Box Drawings Lower Left Corner - Light Vertical and Light Horizontal (U+2514) - └
		/// </summary>
		public Rune LLCorner { get; set; } = (Rune)'└';

		/// <summary>
		/// Box Drawings Heavy Vertical and Horizontal (U+2517) - ┗
		/// </summary>
		public Rune LLCornerHv { get; set; } = (Rune)'┗';

		/// <summary>
		/// Box Drawings Heavy Vertical and Horizontal Light (U+2516) - ┖
		/// </summary>
		public Rune LLCornerHvLt { get; set; } = (Rune)'┖';

		/// <summary>
		/// Box Drawings Vertical Light and Horizontal Heavy (U+2511) - ┕
		/// </summary>
		public Rune LLCornerLtHv { get; set; } = (Rune)'┕';

		/// <summary>
		/// Box Drawings Double Vertical and Double Left (U+255A) - ╚
		/// </summary>
		public Rune LLCornerDbl { get; set; } = (Rune)'╚';

		/// <summary>
		/// Box Drawings Single Vertical and Double Left (U+2558) - ╘
		/// </summary>
		public Rune LLCornerSingleDbl { get; set; } = (Rune)'╘';

		/// <summary>
		/// Box Drawings Double Down and Single Left (U+2559) - ╙
		/// </summary>
		public Rune LLCornerDblSingle { get; set; } = (Rune)'╙';

		/// <summary>
		/// Box Drawings Upper Left Corner - Light Arc Down and Left (U+2570) - ╰
		/// </summary>
		public Rune LLCornerR { get; set; } = (Rune)'╰';

		#endregion

		#region ----------------- Upper Right Corners -----------------
		/// <summary>
		/// Box Drawings Upper Horizontal Corner - Light Vertical and Light Horizontal (U+2510) - ┐
		/// </summary>
		public Rune URCorner { get; set; } = (Rune)'┐';

		/// <summary>
		/// Box Drawings Upper Horizontal Corner - Double Vertical and Double Horizontal (U+2557) - ╗
		/// </summary>
		public Rune URCornerDbl { get; set; } = (Rune)'╗';

		/// <summary>
		/// Box Drawings Upper Horizontal Corner - Light Arc Vertical and Horizontal (U+256E) - ╮
		/// </summary>
		public Rune URCornerR { get; set; } = (Rune)'╮';

		/// <summary>
		/// Box Drawings Heavy Down and Left (U+2513) - ┓
		/// </summary>
		public Rune URCornerHv { get; set; } = (Rune)'┓';

		/// <summary>
		/// Box Drawings Heavy Vertical and Left Down Light (U+2511) - ┑
		/// </summary>
		public Rune URCornerHvLt { get; set; } = (Rune)'┑';

		/// <summary>
		/// Box Drawings Down Light and Horizontal Heavy (U+2514) - ┒
		/// </summary>
		public Rune URCornerLtHv { get; set; } = (Rune)'┒';

		/// <summary>
		/// Box Drawings Double Vertical and Single Left (U+2556) - ╖
		/// </summary>
		public Rune URCornerDblSingle { get; set; } = (Rune)'╖';

		/// <summary>
		/// Box Drawings Single Vertical and Double Left (U+2555) - ╕
		/// </summary>
		public Rune URCornerSingleDbl { get; set; } = (Rune)'╕';
		#endregion

		#region ----------------- Lower Right Corners -----------------
		/// <summary>
		/// Box Drawings Lower Right Corner - Light (U+2518) - ┘
		/// </summary>
		public Rune LRCorner { get; set; } = (Rune)'┘';

		/// <summary>
		/// Box Drawings Lower Right Corner - Double (U+255D) - ╝
		/// </summary>
		public Rune LRCornerDbl { get; set; } = (Rune)'╝';

		/// <summary>
		/// Box Drawings Lower Right Corner - Rounded (U+256F) - ╯
		/// </summary>
		public Rune LRCornerR { get; set; } = (Rune)'╯';

		/// <summary>
		/// Box Drawings Lower Right Corner - Heavy (U+251B) - ┛
		/// </summary>
		public Rune LRCornerHv { get; set; } = (Rune)'┛';

		/// <summary>
		/// Box Drawings Lower Right Corner - Double Vertical and Single Horizontal (U+255C) - ╜
		/// </summary>
		public Rune LRCornerDblSingle { get; set; } = (Rune)'╜';

		/// <summary>
		/// Box Drawings Lower Right Corner - Single Vertical and Double Horizontal (U+255B) - ╛
		/// </summary>
		public Rune LRCornerSingleDbl { get; set; } = (Rune)'╛';

		/// <summary>
		/// Box Drawings Lower Right Corner - Light Vertical and Heavy Horizontal (U+2519) - ┙
		/// </summary>
		public Rune LRCornerLtHv { get; set; } = (Rune)'┙';

		/// <summary>
		/// Box Drawings Lower Right Corner - Heavy Vertical and Light Horizontal (U+251A) - ┚
		/// </summary>
		public Rune LRCornerHvLt { get; set; } = (Rune)'┚';
		#endregion

		#region ----------------- Tees -----------------
		/// <summary>
		/// Box Drawings Left Tee - Single Vertical and Single Horizontal (U+251C) - ├
		/// </summary>
		public Rune LeftTee { get; set; } = (Rune)'├';

		/// <summary>
		/// Box Drawings Left Tee - Single Vertical and Double Horizontal (U+255E) - ╞
		/// </summary>
		public Rune LeftTeeDblH { get; set; } = (Rune)'╞';

		/// <summary>
		/// Box Drawings Left Tee - Double Vertical and Single Horizontal (U+255F) - ╟
		/// </summary>
		public Rune LeftTeeDblV { get; set; } = (Rune)'╟';

		/// <summary>
		/// Box Drawings Left Tee - Double Vertical and Double Horizontal (U+2560) - ╠
		/// </summary>
		public Rune LeftTeeDbl { get; set; } = (Rune)'╠';

		/// <summary>
		/// Box Drawings Left Tee - Heavy Horizontal and Light Vertical (U+2523) - ┝
		/// </summary>
		public Rune LeftTeeHvH { get; set; } = (Rune)'┝';

		/// <summary>
		/// Box Drawings Left Tee - Light Horizontal and Heavy Vertical (U+252B) - ┠
		/// </summary>
		public Rune LeftTeeHvV { get; set; } = (Rune)'┠';

		/// <summary>
		/// Box Drawings Left Tee - Heavy Vertical and Heavy Horizontal (U+2527) - ┣
		/// </summary>
		public Rune LeftTeeHvDblH { get; set; } = (Rune)'┣';

		/// <summary>
		/// Box Drawings Righ Tee - Single Vertical and Single Horizontal (U+2524) - ┤
		/// </summary>
		public Rune RightTee { get; set; } = (Rune)'┤';

		/// <summary>
		/// Box Drawings Right Tee - Single Vertical and Double Horizontal (U+2561) - ╡
		/// </summary>
		public Rune RightTeeDblH { get; set; } = (Rune)'╡';

		/// <summary>
		/// Box Drawings Right Tee - Double Vertical and Single Horizontal (U+2562) - ╢
		/// </summary>
		public Rune RightTeeDblV { get; set; } = (Rune)'╢';

		/// <summary>
		/// Box Drawings Right Tee - Double Vertical and Double Horizontal (U+2563) - ╣
		/// </summary>
		public Rune RightTeeDbl { get; set; } = (Rune)'╣';

		/// <summary>
		/// Box Drawings Right Tee - Heavy Horizontal and Light Vertical (U+2528) - ┥
		/// </summary>
		public Rune RightTeeHvH { get; set; } = (Rune)'┥';

		/// <summary>
		/// Box Drawings Right Tee - Light Horizontal and Heavy Vertical (U+2530) - ┨
		/// </summary>
		public Rune RightTeeHvV { get; set; } = (Rune)'┨';

		/// <summary>
		/// Box Drawings Right Tee - Heavy Vertical and Heavy Horizontal (U+252C) - ┫
		/// </summary>
		public Rune RightTeeHvDblH { get; set; } = (Rune)'┫';

		/// <summary>
		/// Box Drawings Top Tee - Single Vertical and Single Horizontal (U+252C) - ┬
		/// </summary>
		public Rune TopTee { get; set; } = (Rune)'┬';

		/// <summary>
		/// Box Drawings Top Tee - Single Vertical and Double Horizontal (U+2564) - ╤
		/// </summary>
		public Rune TopTeeDblH { get; set; } = (Rune)'╤';

		/// <summary>
		/// Box Drawings Top Tee - Double Vertical and Single Horizontal  (U+2565) - ╥
		/// </summary>
		public Rune TopTeeDblV { get; set; } = (Rune)'╥';

		/// <summary>
		/// Box Drawings Top Tee - Double Vertical and Double Horizontal (U+2566) - ╦
		/// </summary>
		public Rune TopTeeDbl { get; set; } = (Rune)'╦';

		/// <summary>
		/// Box Drawings Top Tee - Heavy Horizontal and Light Vertical (U+252F) - ┯
		/// </summary>
		public Rune TopTeeHvH { get; set; } = (Rune)'┯';

		/// <summary>
		/// Box Drawings Top Tee - Light Horizontal and Heavy Vertical (U+2537) - ┰
		/// </summary>
		public Rune TopTeeHvV { get; set; } = (Rune)'┰';

		/// <summary>
		/// Box Drawings Top Tee - Heavy Vertical and Heavy Horizontal (U+2533) - ┳
		/// </summary>
		public Rune TopTeeHvDblH { get; set; } = (Rune)'┳';

		/// <summary>
		/// Box Drawings Bottom Tee - Single Vertical and Single Horizontal (U+2534) - ┴
		/// </summary>
		public Rune BottomTee { get; set; } = (Rune)'┴';

		/// <summary>
		/// Box Drawings Bottom Tee - Single Vertical and Double Horizontal (U+2567) - ╧
		/// </summary>
		public Rune BottomTeeDblH { get; set; } = (Rune)'╧';

		/// <summary>
		/// Box Drawings Bottom Tee - Double Vertical and Single Horizontal (U+2568) - ╨
		/// </summary>
		public Rune BottomTeeDblV { get; set; } = (Rune)'╨';

		/// <summary>
		/// Box Drawings Bottom Tee - Double Vertical and Double Horizontal (U+2569) - ╩
		/// </summary>
		public Rune BottomTeeDbl { get; set; } = (Rune)'╩';

		/// <summary>
		/// Box Drawings Bottom Tee - Heavy Horizontal and Light Vertical (U+2535) - ┷
		/// </summary>
		public Rune BottomTeeHvH { get; set; } = (Rune)'┷';

		/// <summary>
		/// Box Drawings Bottom Tee - Light Horizontal and Heavy Vertical (U+253D) - ┸
		/// </summary>
		public Rune BottomTeeHvV { get; set; } = (Rune)'┸';

		/// <summary>
		/// Box Drawings Bottom Tee - Heavy Vertical and Heavy Horizontal (U+2539) - ┻
		/// </summary>
		public Rune BottomTeeHvDblH { get; set; } = (Rune)'┻';

		#endregion

		#region ----------------- Crosses -----------------
		/// <summary>
		/// Box Drawings Cross - Single Vertical and Single Horizontal (U+253C) - ┼
		/// </summary>
		public Rune Cross { get; set; } = (Rune)'┼';

		/// <summary>
		/// Box Drawings Cross - Single Vertical and Double Horizontal (U+256A) - ╪
		/// </summary>
		public Rune CrossDblH { get; set; } = (Rune)'╪';

		/// <summary>
		/// Box Drawings Cross - Double Vertical and Single Horizontal (U+256B) - ╫
		/// </summary>
		public Rune CrossDblV { get; set; } = (Rune)'╫';

		/// <summary>
		/// Box Drawings Cross - Double Vertical and Double Horizontal (U+256C) - ╬
		/// </summary>
		public Rune CrossDbl { get; set; } = (Rune)'╬';

		/// <summary>
		/// Box Drawings Cross - Heavy Horizontal and Light Vertical (U+253F) - ┿
		/// </summary>
		public Rune CrossHvH { get; set; } = (Rune)'┿';

		/// <summary>
		/// Box Drawings Cross - Light Horizontal and Heavy Vertical (U+2541) - ╂
		/// </summary>
		public Rune CrossHvV { get; set; } = (Rune)'╂';

		/// <summary>
		/// Box Drawings Cross - Heavy Vertical and Heavy Horizontal (U+254B) - ╋
		/// </summary>
		public Rune CrossHv { get; set; } = (Rune)'╋';
		#endregion
	}
}
