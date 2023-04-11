using System;

namespace Terminal.Gui {
	
	/// <summary>
	/// A straight line control either horizontal or vertical
	/// </summary>
	public class LineView : View {

		/// <summary>
		/// The rune to display at the start of the line (left end of horizontal line or top end of vertical)
		/// If not specified then <see cref="LineRune"/> is used
		/// </summary>
		public Rune? StartingAnchor { get; set; }

		/// <summary>
		/// The rune to display at the end of the line (right end of horizontal line or bottom end of vertical).
		/// If not specified then <see cref="LineRune"/> is used
		/// </summary>
		public Rune? EndingAnchor { get; set; }

		/// <summary>
		/// The symbol to use for drawing the line
		/// </summary>
		public Rune LineRune { get; set; }

		/// <summary>
		/// The direction of the line.  If you change this you will need to manually update the Width/Height
		/// of the control to cover a relevant area based on the new direction.
		/// </summary>
		public Orientation Orientation { get; set; }

		/// <summary>
		/// Creates a horizontal line
		/// </summary>
		public LineView () : this(Orientation.Horizontal)
		{

		}

		/// <summary>
		/// Creates a horizontal or vertical line based on <paramref name="orientation"/>
		/// </summary>
		public LineView (Orientation orientation)
		{
			CanFocus = false;

			switch (orientation) {
			case Orientation.Horizontal:
				Height = 1;
				Width = Dim.Fill ();
				LineRune = Driver.HLine;

				break;
			case Orientation.Vertical:
				Height = Dim.Fill ();
				Width = 1;
				LineRune = Driver.VLine;
				break;
			default:
				throw new ArgumentException ($"Unknown Orientation {orientation}");
			}
			Orientation = orientation;
		}

		/// <summary>
		/// Draws the line including any starting/ending anchors
		/// </summary>
		/// <param name="bounds"></param>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);
			
			Move (0, 0);
			Driver.SetAttribute (GetNormalColor ());

			var hLineWidth = Math.Max (1, Rune.ColumnWidth (Driver.HLine));

			var dEnd = Orientation == Orientation.Horizontal ?
				bounds.Width :
				bounds.Height;

			for (int d = 0; d < dEnd; d += hLineWidth) {
				
				if(Orientation == Orientation.Horizontal) {
					Move (d, 0);
				}
				else {
					Move (0,d);
				}

				Rune rune = LineRune;

				if(d == 0) {
					rune = StartingAnchor ?? LineRune;
				} else
				if (d == dEnd - 1) {
					rune = EndingAnchor ?? LineRune;
				}

				Driver.AddRune (rune);
			}
		}
	}
}
