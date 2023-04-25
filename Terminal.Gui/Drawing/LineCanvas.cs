using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {

	/// <summary>
	/// Defines the style of lines for a <see cref="LineCanvas"/>.
	/// </summary>
	public enum LineStyle {
		/// <summary>
		/// No border is drawn.
		/// </summary>
		None,
		/// <summary>
		/// The border is drawn using thin line glyphs.
		/// </summary>
		Single,
		/// <summary>
		/// The border is drawn using thin line glyphs with dashed (double and triple) straight lines.
		/// </summary>
		Dashed,
		/// <summary>
		/// The border is drawn using thin line glyphs with short dashed (triple and quadruple) straight lines.
		/// </summary>
		Dotted,
		/// <summary>
		/// The border is drawn using thin double line glyphs.
		/// </summary>
		Double,
		/// <summary>
		/// The border is drawn using heavy line glyphs.
		/// </summary>
		Heavy,
		/// <summary>
		/// The border is drawn using heavy line glyphs with dashed (double and triple) straight lines.
		/// </summary>
		HeavyDashed,
		/// <summary>
		/// The border is drawn using heavy line glyphs with short dashed (triple and quadruple) straight lines.
		/// </summary>
		HeavyDotted,
		/// <summary>
		/// The border is drawn using thin line glyphs with rounded corners.
		/// </summary>
		Rounded,
		/// <summary>
		/// The border is drawn using thin line glyphs with rounded corners and dashed (double and triple) straight lines.
		/// </summary>
		RoundedDashed,
		/// <summary>
		/// The border is drawn using thin line glyphs with rounded corners and short dashed (triple and quadruple) straight lines.
		/// </summary>
		RoundedDotted,
		// TODO: Support Ruler
		///// <summary> 
		///// The border is drawn as a diagnostic ruler ("|123456789...").
		///// </summary>
		//Ruler
	}

	/// <summary>
	/// Facilitates box drawing and line intersection detection
	/// and rendering.  Does not support diagonal lines.
	/// </summary>
	public class LineCanvas {
		private List<StraightLine> _lines = new List<StraightLine> ();

		Dictionary<IntersectionRuneType, IntersectionRuneResolver> runeResolvers = new Dictionary<IntersectionRuneType, IntersectionRuneResolver> {
			{IntersectionRuneType.ULCorner,new ULIntersectionRuneResolver()},
			{IntersectionRuneType.URCorner,new URIntersectionRuneResolver()},
			{IntersectionRuneType.LLCorner,new LLIntersectionRuneResolver()},
			{IntersectionRuneType.LRCorner,new LRIntersectionRuneResolver()},

			{IntersectionRuneType.TopTee,new TopTeeIntersectionRuneResolver()},
			{IntersectionRuneType.LeftTee,new LeftTeeIntersectionRuneResolver()},
			{IntersectionRuneType.RightTee,new RightTeeIntersectionRuneResolver()},
			{IntersectionRuneType.BottomTee,new BottomTeeIntersectionRuneResolver()},

			{IntersectionRuneType.Crosshair,new CrosshairIntersectionRuneResolver()},
			// TODO: Add other resolvers
		};

		/// <summary>
		/// <para>
		/// Adds a new <paramref name="length"/> long line to the canvas starting at <paramref name="start"/>.
		/// </para>
		/// <para>
		/// Use positive <paramref name="length"/> for the line to extend Right and negative for Left
		/// when <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>.
		/// </para>
		/// <para>
		/// Use positive <paramref name="length"/> for the line to extend Down and negative for Up
		/// when <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>.
		/// </para>
		/// </summary>
		/// <param name="start">Starting point.</param>
		/// <param name="length">The length of line. 0 for an intersection (cross or T). Positive for Down/Right. Negative for Up/Left.</param>
		/// <param name="orientation">The direction of the line.</param>
		/// <param name="style">The style of line to use</param>
		/// <param name="attribute"></param>
		public void AddLine (Point start, int length, Orientation orientation, LineStyle style, Attribute? attribute = default)
		{
			_cachedBounds = Rect.Empty;
			_lines.Add (new StraightLine (start, length, orientation, style, attribute));
		}

		private void AddLine (StraightLine line)
		{
			_cachedBounds = Rect.Empty;
			_lines.Add (line);
		}

		/// <summary>
		/// Clears all lines from the LineCanvas.
		/// </summary>
		public void Clear ()
		{
			_cachedBounds = Rect.Empty;
			_lines.Clear ();
		}

		private Rect _cachedBounds;

		/// <summary>
		/// Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the 
		/// line that is furthest left/top and Size is defined by the line that extends the furthest
		/// right/bottom.
		/// </summary>
		public Rect Bounds {
			get {
				if (_cachedBounds.IsEmpty) {
					if (_lines.Count == 0) {
						return _cachedBounds;
					}

					Rect bounds = _lines [0].Bounds;

					for (var i = 1; i < _lines.Count; i++) {
						var line = _lines [i];
						var lineBounds = line.Bounds;
						bounds = Rect.Union (bounds, lineBounds);
					}

					if (bounds.Width == 0) {
						bounds.Width = 1;
					}

					if (bounds.Height == 0) {
						bounds.Height = 1;
					}
					_cachedBounds = new Rect (bounds.X, bounds.Y, bounds.Width, bounds.Height);
				}

				return _cachedBounds;
			}
		}

		// TODO: Unless there's an obvious use case for this API we should delete it in favor of the
		// simpler version that doensn't take an area.
		/// <summary>
		/// Evaluates the lines that have been added to the canvas and returns a map containing
		/// the glyphs and their locations. The glyphs are the characters that should be rendered
		/// so that all lines connect up with the appropriate intersection symbols. 
		/// </summary>
		/// <param name="inArea">A rectangle to constrain the search by.</param>
		/// <returns>A map of the points within the canvas that intersect with <paramref name="inArea"/>.</returns>
		public Dictionary<Point, Rune> GetMap (Rect inArea)
		{
			var map = new Dictionary<Point, Rune> ();

			// walk through each pixel of the bitmap
			for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++) {
				for (int x = inArea.X; x < inArea.X + inArea.Width; x++) {

					var intersects = _lines
						.Select (l => l.Intersects (x, y))
						.Where (i => i != null)
						.ToArray ();

					var rune = GetRuneForIntersects (Application.Driver, intersects);

					if (rune != null) {
						map.Add (new Point (x, y), rune.Value);
					}
				}
			}

			return map;
		}

		/// <summary>
		/// Evaluates the lines that have been added to the canvas and returns a map containing
		/// the glyphs and their locations. The glyphs are the characters that should be rendered
		/// so that all lines connect up with the appropriate intersection symbols. 
		/// </summary>
		/// <returns>A map of all the points within the canvas.</returns>
		public Dictionary<Point, Cell> GetCellMap ()
		{
			var map = new Dictionary<Point, Cell> ();

			// walk through each pixel of the bitmap
			for (int y = Bounds.Y; y < Bounds.Y + Bounds.Height; y++) {
				for (int x = Bounds.X; x < Bounds.X + Bounds.Width; x++) {

					var intersects = _lines
						.Select (l => l.Intersects (x, y))
						.Where (i => i != null)
						.ToArray ();

					var cell = GetCellForIntersects (Application.Driver, intersects);

					if (cell != null) {
						map.Add (new Point (x, y), cell);
					}
				}
			}

			return map;
		}

		/// <summary>
		/// Evaluates the lines that have been added to the canvas and returns a map containing
		/// the glyphs and their locations. The glyphs are the characters that should be rendered
		/// so that all lines connect up with the appropriate intersection symbols. 
		/// </summary>
		/// <returns>A map of all the points within the canvas.</returns>
		public Dictionary<Point, Rune> GetMap () => GetMap (Bounds);

		/// <summary>
		/// Returns the contents of the line canvas rendered to a string. The string
		/// will include all columns and rows, even if <see cref="Bounds"/> has negative coordinates. 
		/// For example, if the canvas contains a single line that starts at (-1,-1) with a length of 2, the
		/// rendered string will have a length of 2.
		/// </summary>
		/// <returns>The canvas rendered to a string.</returns>
		public override string ToString ()
		{
			if (Bounds.IsEmpty) {
				return string.Empty;
			}

			// Generate the rune map for the entire canvas
			var runeMap = GetMap ();

			// Create the rune canvas
			Rune [,] canvas = new Rune [Bounds.Height, Bounds.Width];

			// Copy the rune map to the canvas, adjusting for any negative coordinates
			foreach (var kvp in runeMap) {
				int x = kvp.Key.X - Bounds.X;
				int y = kvp.Key.Y - Bounds.Y;
				canvas [y, x] = kvp.Value;
			}

			// Convert the canvas to a string
			StringBuilder sb = new StringBuilder ();
			for (int y = 0; y < canvas.GetLength (0); y++) {
				for (int x = 0; x < canvas.GetLength (1); x++) {
					Rune r = canvas [y, x];
					sb.Append (r.Value == 0 ? ' ' : r.ToString ());
				}
				if (y < canvas.GetLength (0) - 1) {
					sb.AppendLine ();
				}
			}

			return sb.ToString ();
		}

		private abstract class IntersectionRuneResolver {
			readonly Rune round;
			readonly Rune doubleH;
			readonly Rune doubleV;
			readonly Rune doubleBoth;
			readonly Rune thickH;
			readonly Rune thickV;
			readonly Rune thickBoth;
			readonly Rune normal;

			public IntersectionRuneResolver (Rune round, Rune doubleH, Rune doubleV, Rune doubleBoth, Rune thickH, Rune thickV, Rune thickBoth, Rune normal)
			{
				this.round = round;
				this.doubleH = doubleH;
				this.doubleV = doubleV;
				this.doubleBoth = doubleBoth;
				this.thickH = thickH;
				this.thickV = thickV;
				this.thickBoth = thickBoth;
				this.normal = normal;
			}

			public Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition [] intersects)
			{
				var useRounded = intersects.Any (i => i.Line.Length != 0 && (
					i.Line.Style == LineStyle.Rounded || i.Line.Style == LineStyle.RoundedDashed || i.Line.Style == LineStyle.RoundedDotted));

				// Note that there aren't any glyphs for intersections of double lines with heavy lines

				bool doubleHorizontal = intersects.Any (l => l.Line.Orientation == Orientation.Horizontal && l.Line.Style == LineStyle.Double);
				bool doubleVertical = intersects.Any (l => l.Line.Orientation == Orientation.Vertical && l.Line.Style == LineStyle.Double);

				bool thickHorizontal = intersects.Any (l => l.Line.Orientation == Orientation.Horizontal && (
					l.Line.Style == LineStyle.Heavy || l.Line.Style == LineStyle.HeavyDashed || l.Line.Style == LineStyle.HeavyDotted));
				bool thickVertical = intersects.Any (l => l.Line.Orientation == Orientation.Vertical && (
					l.Line.Style == LineStyle.Heavy || l.Line.Style == LineStyle.HeavyDashed || l.Line.Style == LineStyle.HeavyDotted));

				if (doubleHorizontal) {
					return doubleVertical ? doubleBoth : doubleH;
				}
				if (doubleVertical) {
					return doubleV;
				}

				if (thickHorizontal) {
					return thickVertical ? thickBoth : thickH;
				}
				if (thickVertical) {
					return thickV;
				}

				return useRounded ? round : normal;
			}
		}

		private class ULIntersectionRuneResolver : IntersectionRuneResolver {
			public ULIntersectionRuneResolver () :
				base ('╭', '╒', '╓', '╔', '┍', '┎', '┏', '┌')
			{

			}
		}
		private class URIntersectionRuneResolver : IntersectionRuneResolver {

			public URIntersectionRuneResolver () :
				base ('╮', '╕', '╖', '╗', '┑', '┒', '┓', '┐')
			{

			}
		}
		private class LLIntersectionRuneResolver : IntersectionRuneResolver {

			public LLIntersectionRuneResolver () :
				base ('╰', '╘', '╙', '╚', '┕', '┖', '┗', '└')
			{

			}
		}
		private class LRIntersectionRuneResolver : IntersectionRuneResolver {
			public LRIntersectionRuneResolver () :
				base ('╯', '╛', '╜', '╝', '┙', '┚', '┛', '┘')
			{

			}
		}

		private class TopTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public TopTeeIntersectionRuneResolver () :
				base ('┬', '╤', '╥', '╦', '┯', '┰', '┳', '┬')
			{

			}
		}
		private class LeftTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public LeftTeeIntersectionRuneResolver () :
				base ('├', '╞', '╟', '╠', '┝', '┠', '┣', '├')
			{

			}
		}
		private class RightTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public RightTeeIntersectionRuneResolver () :
				base ('┤', '╡', '╢', '╣', '┥', '┨', '┫', '┤')
			{

			}
		}
		private class BottomTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public BottomTeeIntersectionRuneResolver () :
				base ('┴', '╧', '╨', '╩', '┷', '┸', '┻', '┴')
			{

			}
		}
		private class CrosshairIntersectionRuneResolver : IntersectionRuneResolver {
			public CrosshairIntersectionRuneResolver () :
				base ('┼', '╪', '╫', '╬', '┿', '╂', '╋', '┼')
			{

			}
		}

		private Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition [] intersects)
		{
			if (!intersects.Any ()) {
				return null;
			}

			var runeType = GetRuneTypeForIntersects (intersects);

			if (runeResolvers.ContainsKey (runeType)) {
				return runeResolvers [runeType].GetRuneForIntersects (driver, intersects);
			}

			// TODO: Remove these once we have all of the below ported to IntersectionRuneResolvers
			var useDouble = intersects.Any (i => i.Line.Style == LineStyle.Double);
			var useDashed = intersects.Any (i => i.Line.Style == LineStyle.Dashed || i.Line.Style == LineStyle.RoundedDashed);
			var useDotted = intersects.Any (i => i.Line.Style == LineStyle.Dotted || i.Line.Style == LineStyle.RoundedDotted);
			// horiz and vert lines same as Single for Rounded
			var useThick = intersects.Any (i => i.Line.Style == LineStyle.Heavy);
			var useThickDashed = intersects.Any (i => i.Line.Style == LineStyle.HeavyDashed);
			var useThickDotted = intersects.Any (i => i.Line.Style == LineStyle.HeavyDotted);
			// TODO: Support ruler
			//var useRuler = intersects.Any (i => i.Line.Style == LineStyle.Ruler && i.Line.Length != 0);

			// TODO: maybe make these resolvers too for simplicity?
			switch (runeType) {
			case IntersectionRuneType.None:
				return null;
			case IntersectionRuneType.Dot:
				return (Rune)'.';
			case IntersectionRuneType.HLine:
				if (useDouble) {
					return driver.HDbLine;
				}
				if (useDashed) {
					return driver.HDsLine;
				}
				if (useDotted) {
					return driver.HDtLine;
				}
				return useThick ? driver.HThLine : (useThickDashed ? driver.HThDsLine : (useThickDotted ? driver.HThDtLine : driver.HLine));
			case IntersectionRuneType.VLine:
				if (useDouble) {
					return driver.VDbLine;
				}
				if (useDashed) {
					return driver.VDsLine;
				}
				if (useDotted) {
					return driver.VDtLine;
				}
				return useThick ? driver.VThLine : (useThickDashed ? driver.VThDsLine : (useThickDotted ? driver.VThDtLine : driver.VLine));

			default: throw new Exception ("Could not find resolver or switch case for " + nameof (runeType) + ":" + runeType);
			}
		}

		private Attribute? GetAttributeForIntersects (IntersectionDefinition [] intersects)
		{
			var set = new List<IntersectionDefinition> (intersects.Where (i => i.Line.Attribute?.HasValidColors ?? false));

			if (set.Count == 0) {
				return null;
			}

			return set [0].Line.Attribute;

		}

		/// <summary>
		/// Represents a single row/column within the <see cref="LineCanvas"/>. Includes the glyph and the foreground/background colors.
		/// </summary>
		public class Cell
		{
			/// <summary>
			/// The glyph to draw.
			/// </summary>
			public Rune? Rune { get; set; }

			/// <summary>
			/// The foreground color to draw the glyph with.
			/// </summary>
			public Attribute? Attribute { get; set; }

		}

		private Cell GetCellForIntersects (ConsoleDriver driver, IntersectionDefinition [] intersects)
		{
			if (!intersects.Any ()) {
				return null;
			}

			var cell = new Cell ();
			cell.Rune = GetRuneForIntersects (driver, intersects);
			cell.Attribute = GetAttributeForIntersects (intersects);
			return cell;
		}

		private IntersectionRuneType GetRuneTypeForIntersects (IntersectionDefinition [] intersects)
		{
			var set = new HashSet<IntersectionType> (intersects.Select (i => i.Type));

			#region Crosshair Conditions
			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.PassOverVertical
				)) {
				return IntersectionRuneType.Crosshair;
			}

			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartLeft,
				IntersectionType.StartRight
				)) {
				return IntersectionRuneType.Crosshair;
			}

			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartUp,
				IntersectionType.StartDown
				)) {
				return IntersectionRuneType.Crosshair;
			}

			if (Has (set,
				IntersectionType.StartLeft,
				IntersectionType.StartRight,
				IntersectionType.StartUp,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.Crosshair;
			}
			#endregion

			#region Corner Conditions
			if (Exactly (set,
				IntersectionType.StartRight,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.ULCorner;
			}

			if (Exactly (set,
				IntersectionType.StartLeft,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.URCorner;
			}

			if (Exactly (set,
				IntersectionType.StartUp,
				IntersectionType.StartLeft)) {
				return IntersectionRuneType.LRCorner;
			}

			if (Exactly (set,
				IntersectionType.StartUp,
				IntersectionType.StartRight)) {
				return IntersectionRuneType.LLCorner;
			}
			#endregion Corner Conditions

			#region T Conditions
			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.TopTee;
			}
			if (Has (set,
				IntersectionType.StartRight,
				IntersectionType.StartLeft,
				IntersectionType.StartDown)) {
				return IntersectionRuneType.TopTee;
			}

			if (Has (set,
				IntersectionType.PassOverHorizontal,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.BottomTee;
			}
			if (Has (set,
				IntersectionType.StartRight,
				IntersectionType.StartLeft,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.BottomTee;
			}

			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartRight)) {
				return IntersectionRuneType.LeftTee;
			}
			if (Has (set,
				IntersectionType.StartRight,
				IntersectionType.StartDown,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.LeftTee;
			}

			if (Has (set,
				IntersectionType.PassOverVertical,
				IntersectionType.StartLeft)) {
				return IntersectionRuneType.RightTee;
			}
			if (Has (set,
				IntersectionType.StartLeft,
				IntersectionType.StartDown,
				IntersectionType.StartUp)) {
				return IntersectionRuneType.RightTee;
			}
			#endregion

			if (All (intersects, Orientation.Horizontal)) {
				return IntersectionRuneType.HLine;
			}

			if (All (intersects, Orientation.Vertical)) {
				return IntersectionRuneType.VLine;
			}

			return IntersectionRuneType.Dot;
		}

		private bool All (IntersectionDefinition [] intersects, Orientation orientation)
		{
			return intersects.All (i => i.Line.Orientation == orientation);
		}

		/// <summary>
		/// Returns true if the <paramref name="intersects"/> collection has all the <paramref name="types"/>
		/// specified (i.e. AND).
		/// </summary>
		/// <param name="intersects"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		private bool Has (HashSet<IntersectionType> intersects, params IntersectionType [] types)
		{
			return types.All (t => intersects.Contains (t));
		}

		/// <summary>
		/// Returns true if all requested <paramref name="types"/> appear in <paramref name="intersects"/>
		/// and there are no additional <see cref="IntersectionRuneType"/>
		/// </summary>
		/// <param name="intersects"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		private bool Exactly (HashSet<IntersectionType> intersects, params IntersectionType [] types)
		{
			return intersects.SetEquals (types);
		}

		/// <summary>
		/// Merges one line canvas into this one.
		/// </summary>
		/// <param name="lineCanvas"></param>
		public void Merge (LineCanvas lineCanvas)
		{
			foreach (var line in lineCanvas._lines) {
				AddLine (line);
			}
		}

		internal class IntersectionDefinition {
			/// <summary>
			/// The point at which the intersection happens
			/// </summary>
			internal Point Point { get; }

			/// <summary>
			/// Defines how <see cref="Line"/> position relates
			/// to <see cref="Point"/>.
			/// </summary>
			internal IntersectionType Type { get; }

			/// <summary>
			/// The line that intersects <see cref="Point"/>
			/// </summary>
			internal StraightLine Line { get; }

			internal IntersectionDefinition (Point point, IntersectionType type, StraightLine line)
			{
				Point = point;
				Type = type;
				Line = line;
			}
		}

		/// <summary>
		/// The type of Rune that we will use before considering
		/// double width, curved borders etc
		/// </summary>
		internal enum IntersectionRuneType {
			None,
			Dot,
			ULCorner,
			URCorner,
			LLCorner,
			LRCorner,
			TopTee,
			BottomTee,
			RightTee,
			LeftTee,
			Crosshair,
			HLine,
			VLine,
		}

		internal enum IntersectionType {
			/// <summary>
			/// There is no intersection
			/// </summary>
			None,

			/// <summary>
			///  A line passes directly over this point traveling along
			///  the horizontal axis
			/// </summary>
			PassOverHorizontal,

			/// <summary>
			///  A line passes directly over this point traveling along
			///  the vertical axis
			/// </summary>
			PassOverVertical,

			/// <summary>
			/// A line starts at this point and is traveling up
			/// </summary>
			StartUp,

			/// <summary>
			/// A line starts at this point and is traveling right
			/// </summary>
			StartRight,

			/// <summary>
			/// A line starts at this point and is traveling down
			/// </summary>
			StartDown,

			/// <summary>
			/// A line starts at this point and is traveling left
			/// </summary>
			StartLeft,

			/// <summary>
			/// A line exists at this point who has 0 length
			/// </summary>
			Dot
		}

		// TODO: Add events that notify when StraightLine changes to enable dynamic layout
		internal class StraightLine {
			public Point Start { get; }
			public int Length { get; }
			public Orientation Orientation { get; }
			public LineStyle Style { get; }
			public Attribute? Attribute { get; set; }

			internal StraightLine (Point start, int length, Orientation orientation, LineStyle style, Attribute? attribute = default)
			{
				this.Start = start;
				this.Length = length;
				this.Orientation = orientation;
				this.Style = style;
				this.Attribute = attribute;
			}

			internal IntersectionDefinition Intersects (int x, int y)
			{
				switch (Orientation) {
				case Orientation.Horizontal: return IntersectsHorizontally (x, y);
				case Orientation.Vertical: return IntersectsVertically (x, y);
				default: throw new ArgumentOutOfRangeException (nameof (Orientation));
				}

			}

			private IntersectionDefinition IntersectsHorizontally (int x, int y)
			{
				if (Start.Y != y) {
					return null;
				} else {
					if (StartsAt (x, y)) {

						return new IntersectionDefinition (
							Start,
							GetTypeByLength (IntersectionType.StartLeft, IntersectionType.PassOverHorizontal, IntersectionType.StartRight),
							this
							);

					}

					if (EndsAt (x, y)) {

						return new IntersectionDefinition (
							Start,
							Length < 0 ? IntersectionType.StartRight : IntersectionType.StartLeft,
							this
							);

					} else {
						var xmin = Math.Min (Start.X, Start.X + Length);
						var xmax = Math.Max (Start.X, Start.X + Length);

						if (xmin < x && xmax > x) {
							return new IntersectionDefinition (
							new Point (x, y),
							IntersectionType.PassOverHorizontal,
							this
							);
						}
					}

					return null;
				}
			}

			private IntersectionDefinition IntersectsVertically (int x, int y)
			{
				if (Start.X != x) {
					return null;
				} else {
					if (StartsAt (x, y)) {

						return new IntersectionDefinition (
							Start,
							GetTypeByLength (IntersectionType.StartUp, IntersectionType.PassOverVertical, IntersectionType.StartDown),
							this
							);

					}

					if (EndsAt (x, y)) {

						return new IntersectionDefinition (
							Start,
							Length < 0 ? IntersectionType.StartDown : IntersectionType.StartUp,
							this
							);

					} else {
						var ymin = Math.Min (Start.Y, Start.Y + Length);
						var ymax = Math.Max (Start.Y, Start.Y + Length);

						if (ymin < y && ymax > y) {
							return new IntersectionDefinition (
							new Point (x, y),
							IntersectionType.PassOverVertical,
							this
							);
						}
					}

					return null;
				}
			}

			private IntersectionType GetTypeByLength (IntersectionType typeWhenNegative, IntersectionType typeWhenZero, IntersectionType typeWhenPositive)
			{
				if (Length == 0) {
					return typeWhenZero;
				}

				return Length < 0 ? typeWhenNegative : typeWhenPositive;
			}

			private bool EndsAt (int x, int y)
			{
				var sub = (Length == 0) ? 0 : (Length > 0) ? 1 : -1;
				if (Orientation == Orientation.Horizontal) {
					return Start.X + Length - sub == x && Start.Y == y;
				}

				return Start.X == x && Start.Y + Length - sub == y;
			}

			private bool StartsAt (int x, int y)
			{
				return Start.X == x && Start.Y == y;
			}

			/// <summary>
			/// Gets the rectangle that describes the bounds of the canvas. Location is the coordinates of the 
			/// line that is furthest left/top and Size is defined by the line that extends the furthest
			/// right/bottom.
			/// </summary>
			internal Rect Bounds {
				get {

					// 0 and 1/-1 Length means a size (width or height) of 1
					var size = Math.Max (1, Math.Abs (Length));

					// How much to offset x or y to get the start of the line
					var offset = Math.Abs (Length < 0 ? Length + 1 : 0);
					var x = Start.X - (Orientation == Orientation.Horizontal ? offset : 0);
					var y = Start.Y - (Orientation == Orientation.Vertical ? offset : 0);
					var width = Orientation == Orientation.Horizontal ? size : 1;
					var height = Orientation == Orientation.Vertical ? size : 1;

					return new Rect (x, y, width, height);
				}
			}

			/// <summary>
			/// Formats the Line as a string in (Start.X,Start.Y,Length,Orientation) notation.
			/// </summary>
			public override string ToString ()
			{
				return $"({Start.X},{Start.Y},{Length},{Orientation})";
			}
		}
	}
}
