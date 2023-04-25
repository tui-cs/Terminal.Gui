using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Line Drawing", Description: "Demonstrates LineCanvas.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	public class LineDrawing : Scenario {

		public override void Setup ()
		{
			var toolsWidth = 12;

			var canvas = new DrawingArea {
				Width = Dim.Fill (toolsWidth + 1),
				Height = Dim.Fill (),
				BorderStyle = LineStyle.Single
			};

			var tools = new ToolsView (toolsWidth) {
				Y = 1,
				X = Pos.AnchorEnd (toolsWidth + 1),
				Height = Dim.Fill (),
				Width = Dim.Fill ()
			};

			tools.ColorChanged += (c) => canvas.SetColor (c);
			tools.SetStyle += (b) => canvas.LineStyle = b;

			Win.Add (canvas);
			Win.Add (tools);
			Win.Add (new Label (" -Tools-") { X = Pos.AnchorEnd (toolsWidth + 1) });
		}

		class ToolsView : View {

			LineCanvas grid;
			public event Action<Color> ColorChanged;
			public event Action<LineStyle> SetStyle;

			Dictionary<Point, Color> swatches = new Dictionary<Point, Color> {
				{ new Point(1,1),Color.Red},
				{ new Point(3,1),Color.Green},
				{ new Point(5,1),Color.BrightBlue},
				{ new Point(7,1),Color.Black},
				{ new Point(9,1),Color.DarkGray},
				{ new Point(11,1),Color.White},
			};

			public ToolsView (int width)
			{
				grid = new LineCanvas ();

				grid.AddLine (new Point (0, 0), 7, Orientation.Vertical, LineStyle.Single);
				grid.AddLine (new Point (0, 0), width + 1, Orientation.Horizontal, LineStyle.Single);
				grid.AddLine (new Point (width, 0), 7, Orientation.Vertical, LineStyle.Single);
				grid.AddLine (new Point (0, 6), width + 1, Orientation.Horizontal, LineStyle.Single);

				grid.AddLine (new Point (2, 0), 7, Orientation.Vertical, LineStyle.Single);
				grid.AddLine (new Point (4, 0), 7, Orientation.Vertical, LineStyle.Single);
				grid.AddLine (new Point (6, 0), 7, Orientation.Vertical, LineStyle.Single);
				grid.AddLine (new Point (8, 0), 7, Orientation.Vertical, LineStyle.Single);
				grid.AddLine (new Point (10, 0), 7, Orientation.Vertical, LineStyle.Single);

				grid.AddLine (new Point (0, 2), width + 1, Orientation.Horizontal, LineStyle.Single);
				grid.AddLine (new Point (0, 4), width + 1, Orientation.Horizontal, LineStyle.Single);
			}
			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				Driver.SetAttribute (new Terminal.Gui.Attribute (Color.DarkGray, ColorScheme.Normal.Background));
				
				
				foreach(var p in grid.GetMap(bounds))
				{
					this.AddRune(p.Key.X,p.Key.Y,p.Value);
				}

				foreach (var swatch in swatches) {
					Driver.SetAttribute (new Terminal.Gui.Attribute (swatch.Value, ColorScheme.Normal.Background));
					AddRune (swatch.Key.X, swatch.Key.Y, '█');
				}

				Driver.SetAttribute (new Terminal.Gui.Attribute (ColorScheme.Normal.Foreground, ColorScheme.Normal.Background));
				AddRune (1, 3, Application.Driver.HLine);
				AddRune (3, 3, Application.Driver.HDsLine);
				AddRune (5, 3, Application.Driver.HDtLine);
				AddRune (7, 3, Application.Driver.ULRCorner);
				AddRune (9, 3, Application.Driver.HDsLine);
				AddRune (11, 3, Application.Driver.HDtLine);
				AddRune (1, 5, Application.Driver.HThLine);
				AddRune (3, 5, Application.Driver.HThDsLine);
				AddRune (5, 5, Application.Driver.HThDtLine);
				AddRune (7, 5, Application.Driver.HDbLine);
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {
					foreach (var swatch in swatches) {
						if (mouseEvent.X == swatch.Key.X && mouseEvent.Y == swatch.Key.Y) {

							ColorChanged?.Invoke (swatch.Value);
							return true;
						}
					}

					if (mouseEvent.X == 1 && mouseEvent.Y == 3) {

						SetStyle?.Invoke (LineStyle.Single);
						return true;
					}
					if (mouseEvent.X == 3 && mouseEvent.Y == 3) {

						SetStyle?.Invoke (LineStyle.Dashed);
						return true;
					}
					if (mouseEvent.X == 5 && mouseEvent.Y == 3) {

						SetStyle?.Invoke (LineStyle.Dotted);
						return true;
					}
					if (mouseEvent.X == 7 && mouseEvent.Y == 3) {

						SetStyle?.Invoke (LineStyle.Rounded);
						return true;
					}
					if (mouseEvent.X == 9 && mouseEvent.Y == 3) {

						SetStyle?.Invoke (LineStyle.RoundedDashed);
						return true;
					}
					if (mouseEvent.X == 11 && mouseEvent.Y == 3) {

						SetStyle?.Invoke (LineStyle.RoundedDotted);
						return true;
					}
					if (mouseEvent.X == 1 && mouseEvent.Y == 5) {

						SetStyle?.Invoke (LineStyle.Heavy);
						return true;
					}
					if (mouseEvent.X == 3 && mouseEvent.Y == 5) {

						SetStyle?.Invoke (LineStyle.HeavyDashed);
						return true;
					}
					if (mouseEvent.X == 5 && mouseEvent.Y == 5) {

						SetStyle?.Invoke (LineStyle.HeavyDotted);
						return true;
					}
					if (mouseEvent.X == 7 && mouseEvent.Y == 5) {

						SetStyle?.Invoke (LineStyle.Double);
						return true;
					}
				}

				return base.OnMouseEvent (mouseEvent);
			}
		}

		class DrawingArea : View {
			/// <summary>
			/// Index into <see cref="canvases"/> by color.
			/// </summary>
			Dictionary<Color, int> colorLayers = new Dictionary<Color, int> ();
			List<LineCanvas> canvases = new List<LineCanvas> ();
			int currentColor;

			Point? currentLineStart = null;
			public LineStyle LineStyle { get; set; }

			public DrawingArea ()
			{
				AddCanvas (Color.White);
			}

			private void AddCanvas (Color c)
			{
				if (colorLayers.ContainsKey (c)) {
					return;
				}

				canvases.Add (new LineCanvas ());
				colorLayers.Add (c, canvases.Count - 1);
				currentColor = canvases.Count - 1;
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				foreach (var kvp in colorLayers) {

					Driver.SetAttribute (new Terminal.Gui.Attribute (kvp.Key, ColorScheme.Normal.Background));

					var canvas = canvases [kvp.Value];

					foreach(var p in canvas.GetMap(bounds))
					{
						this.AddRune(p.Key.X,p.Key.Y,p.Value);
					}
				}
			}
			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{

				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {
					if (currentLineStart == null) {
						currentLineStart = new Point (mouseEvent.X - 1, mouseEvent.Y - 1);
					}
				} else {
					if (currentLineStart != null) {

						var start = currentLineStart.Value;
						var end = new Point (mouseEvent.X - 1, mouseEvent.Y - 1);
						var orientation = Orientation.Vertical;
						var length = end.Y - start.Y;

						// if line is wider than it is tall switch to horizontal
						if (Math.Abs (start.X - end.X) > Math.Abs (start.Y - end.Y)) {
							orientation = Orientation.Horizontal;
							length = end.X - start.X;
						}

						if(length > 0) {
							length++;
						}
						else {
							length--;
						}
						

						canvases [currentColor].AddLine (
							start,
							length,
							orientation,
							LineStyle);

						currentLineStart = null;
						SetNeedsDisplay ();
					}
				}

				return base.OnMouseEvent (mouseEvent);
			}

			internal void SetColor (Color c)
			{
				AddCanvas (c);
				currentColor = colorLayers [c];
			}

		}
	}
}
