using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui.Graphs {


	/// <summary>
	/// Facilitates box drawing and line intersection detection
	/// and rendering.  Does not support diagonal lines.
	/// </summary>
	public class LineCanvas {



		private List<StraightLine> lines = new List<StraightLine> ();

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
		/// Add a new line to the canvas starting at <paramref name="from"/>.
		/// Use positive <paramref name="length"/> for Right and negative for Left
		/// when <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>.
		/// Use positive <paramref name="length"/> for Down and negative for Up
		/// when <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>.
		/// </summary>
		/// <param name="from">Starting point.</param>
		/// <param name="length">Length of line.  0 for a dot.  
		/// Positive for Down/Right.  Negative for Up/Left.</param>
		/// <param name="orientation">Direction of the line.</param>
		/// <param name="style">The style of line to use</param>
		public void AddLine (Point from, int length, Orientation orientation, BorderStyle style)
		{
			lines.Add (new StraightLine (from, length, orientation, style));
		}
		/// <summary>
		/// Evaluate all currently defined lines that lie within 
		/// <paramref name="inArea"/> and map that
		/// shows what characters (if any) should be rendered at each
		/// point so that all lines connect up correctly with appropriate
		/// intersection symbols.
		/// <returns></returns>
		/// </summary>
		/// <param name="inArea"></param>
		/// <returns>Mapping of all the points within <paramref name="inArea"/> to
		/// line or intersection runes which should be drawn there.</returns>
		public Dictionary<Point,Rune> GenerateImage (Rect inArea)
		{
			var map = new Dictionary<Point,Rune>();

			// walk through each pixel of the bitmap
			for (int y = inArea.Y; y < inArea.Y + inArea.Height; y++) {
				for (int x = inArea.X; x < inArea.X + inArea.Width; x++) {

					var intersects = lines
						.Select (l => l.Intersects (x, y))
						.Where (i => i != null)
						.ToArray ();

					// TODO: use Driver and LineStyle to map
					var rune = GetRuneForIntersects (Application.Driver, intersects);

					if(rune != null)
					{
						map.Add(new Point(x,y),rune.Value);
					}
				}
			}

			return map;
		}

		private abstract class IntersectionRuneResolver {
			readonly Rune round;
			readonly Rune doubleH;
			readonly Rune doubleV;
			readonly Rune doubleBoth;
			readonly Rune normal;

			public IntersectionRuneResolver (Rune round, Rune doubleH, Rune doubleV, Rune doubleBoth, Rune normal)
			{
				this.round = round;
				this.doubleH = doubleH;
				this.doubleV = doubleV;
				this.doubleBoth = doubleBoth;
				this.normal = normal;
			}

			public Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition [] intersects)
			{
				var useRounded = intersects.Any (i => i.Line.Style == BorderStyle.Rounded && i.Line.Length != 0);

				bool doubleHorizontal = intersects.Any (l => l.Line.Orientation == Orientation.Horizontal && l.Line.Style == BorderStyle.Double);
				bool doubleVertical = intersects.Any (l => l.Line.Orientation == Orientation.Vertical && l.Line.Style == BorderStyle.Double);


				if (doubleHorizontal) {
					return doubleVertical ? doubleBoth : doubleH;
				}

				if (doubleVertical) {
					return doubleV;
				}

				return useRounded ? round : normal;
			}
		}

		private class ULIntersectionRuneResolver : IntersectionRuneResolver {
			public ULIntersectionRuneResolver () :
				base ('╭', '╒', '╓', '╔', '┌')
			{

			}
		}
		private class URIntersectionRuneResolver : IntersectionRuneResolver {

			public URIntersectionRuneResolver () :
				base ('╮', '╕', '╖', '╗', '┐')
			{

			}
		}
		private class LLIntersectionRuneResolver : IntersectionRuneResolver {

			public LLIntersectionRuneResolver () :
				base ('╰', '╘', '╙', '╚', '└')
			{

			}
		}
		private class LRIntersectionRuneResolver : IntersectionRuneResolver {
			public LRIntersectionRuneResolver () :
				base ('╯', '╛', '╜', '╝', '┘')
			{

			}
		}

		private class TopTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public TopTeeIntersectionRuneResolver () :
				base ('┬', '╤', '╥', '╦', '┬')
			{

			}
		}
		private class LeftTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public LeftTeeIntersectionRuneResolver () :
				base ('├', '╞', '╟', '╠', '├')
			{

			}
		}
		private class RightTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public RightTeeIntersectionRuneResolver () :
				base ('┤', '╡', '╢', '╣', '┤')
			{

			}
		}
		private class BottomTeeIntersectionRuneResolver : IntersectionRuneResolver {
			public BottomTeeIntersectionRuneResolver () :
				base ('┴', '╧', '╨', '╩', '┴')
			{

			}
		}
		private class CrosshairIntersectionRuneResolver : IntersectionRuneResolver {
			public CrosshairIntersectionRuneResolver () :
				base ('┼', '╪', '╫', '╬', '┼')
			{

			}
		}

		private Rune? GetRuneForIntersects (ConsoleDriver driver, IntersectionDefinition [] intersects)
		{
			if (!intersects.Any ())
				return null;

			var runeType = GetRuneTypeForIntersects (intersects);

			if (runeResolvers.ContainsKey (runeType)) {
				return runeResolvers [runeType].GetRuneForIntersects (driver, intersects);
			}

			// TODO: Remove these two once we have all of the below ported to IntersectionRuneResolvers
			var useDouble = intersects.Any (i => i.Line.Style == BorderStyle.Double);
			var useRounded = intersects.Any (i => i.Line.Style == BorderStyle.Rounded);
			// TODO: Support ruler
			//var useRuler = intersects.Any (i => i.Line.Style == BorderStyle.Ruler && i.Line.Length != 0);

			// TODO: maybe make these resolvers to for simplicity?
			// or for dotted lines later on or that kind of thing?
			switch (runeType) {
			case IntersectionRuneType.None:
				return null;
			case IntersectionRuneType.Dot:
				return (Rune)'.';
			case IntersectionRuneType.HLine:
				return useDouble ? driver.HDLine : driver.HLine;
			case IntersectionRuneType.VLine:
				return useDouble ? driver.VDLine : driver.VLine;
			default: throw new Exception ("Could not find resolver or switch case for " + nameof (runeType) + ":" + runeType);
			}
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

		class IntersectionDefinition {
			/// <summary>
			/// The point at which the intersection happens
			/// </summary>
			public Point Point { get; }

			/// <summary>
			/// Defines how <see cref="Line"/> position relates
			/// to <see cref="Point"/>.
			/// </summary>
			public IntersectionType Type { get; }

			/// <summary>
			/// The line that intersects <see cref="Point"/>
			/// </summary>
			public StraightLine Line { get; }

			public IntersectionDefinition (Point point, IntersectionType type, StraightLine line)
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
		enum IntersectionRuneType {
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

		enum IntersectionType {
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

		class StraightLine {
			public Point Start { get; }
			public int Length { get; }
			public Orientation Orientation { get; }
			public BorderStyle Style { get; }

			public StraightLine (Point start, int length, Orientation orientation, BorderStyle style)
			{
				this.Start = start;
				this.Length = length;
				this.Orientation = orientation;
				this.Style = style;
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
							GetTypeByLength(IntersectionType.StartLeft, IntersectionType.PassOverHorizontal,IntersectionType.StartRight),
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
							GetTypeByLength(IntersectionType.StartUp, IntersectionType.PassOverVertical, IntersectionType.StartDown),
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
				if (Orientation == Orientation.Horizontal) {
					return Start.X + Length == x && Start.Y == y;
				}

				return Start.X == x && Start.Y + Length == y;
			}

			private bool StartsAt (int x, int y)
			{
				return Start.X == x && Start.Y == y;
			}
		}
	}
}
