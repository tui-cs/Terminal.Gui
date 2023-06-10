using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Line Drawing", Description: "Demonstrates LineCanvas.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Layout")]
	public class LineDrawing : Scenario {

		public override void Setup ()
		{
			var canvas = new DrawingArea {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var tools = new ToolsView () {
				Title = "Tools",
				X = Pos.Right(canvas) - 20,
				Y = 2
			};

			tools.ColorChanged += (c) => canvas.SetColor (c);
			tools.SetStyle += (b) => canvas.LineStyle = b;
			tools.AddLayer += () => canvas.AddLayer ();

			Win.Add (canvas);
			Win.Add (tools);
		}

		class ToolsView : Window {
			public event Action<Color> ColorChanged;
			public event Action<LineStyle> SetStyle;
			public event Action AddLayer;

			private RadioGroup _stylePicker;
			private ColorPicker _colorPicker;
			private Button _addLayerBtn;

			public ToolsView ()
			{
				BorderStyle = LineStyle.Dotted;
				Border.Thickness = new Thickness (1, 2, 1, 1);
				Initialized += ToolsView_Initialized;
			}

			private void ToolsView_Initialized (object sender, EventArgs e)
			{
				LayoutSubviews ();
				Width = Math.Max (_colorPicker.Frame.Width, _stylePicker.Frame.Width) + GetFramesThickness().Horizontal;
				Height = _colorPicker.Frame.Height + _stylePicker.Frame.Height + _addLayerBtn.Frame.Height + GetFramesThickness ().Vertical;
				SuperView.LayoutSubviews ();
			}

			public override void BeginInit ()
			{
				base.BeginInit ();

				_colorPicker = new ColorPicker () {
					X = 0,
					Y = 0,
					BoxHeight = 1,
					BoxWidth = 2
				};

				_colorPicker.ColorChanged += (s, a) => ColorChanged?.Invoke (a.Color);

				_stylePicker = new RadioGroup (Enum.GetNames (typeof (LineStyle)).ToArray ()) {
					X = 0,
					Y = Pos.Bottom (_colorPicker)
				};

				_stylePicker.SelectedItemChanged += (s, a) => {
					SetStyle?.Invoke ((LineStyle)a.SelectedItem);
				};

				_addLayerBtn = new Button () {
					Text = "New Layer",
					X = Pos.Center (),
					Y = Pos.Bottom (_stylePicker),
				};

				_addLayerBtn.Clicked += (s, a) => AddLayer?.Invoke ();
				Add (_colorPicker, _stylePicker, _addLayerBtn);
			}
		}

		class DrawingArea : View {
			List<LineCanvas> _layers = new List<LineCanvas> ();
			LineCanvas _currentLayer;
			Color _currentColor = Color.White;
			Point? _currentLineStart = null;

			public LineStyle LineStyle { get; set; }

			public DrawingArea ()
			{
				AddLayer ();
			}

			internal void AddLayer ()
			{
				_currentLayer = new LineCanvas ();
				_layers.Add (_currentLayer);
			}

			public override void OnDrawContent (Rect contentArea)
			{
				base.OnDrawContent (contentArea);

				foreach (var canvas in _layers) {
					
					foreach (var c in canvas.GetCellMap ()) {
						Driver.SetAttribute (c.Value.Attribute?.Value ?? ColorScheme.Normal);
						this.AddRune (c.Key.X, c.Key.Y, c.Value.Rune.Value);
					}
				}
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)) {
					if (_currentLineStart == null) {
						_currentLineStart = new Point (mouseEvent.X - GetBoundsOffset().X, mouseEvent.Y - GetBoundsOffset ().X);
					}
				} else {
					if (_currentLineStart != null) {

						var start = _currentLineStart.Value;
						var end = new Point (mouseEvent.X - GetBoundsOffset ().X, mouseEvent.Y - GetBoundsOffset ().X);
						var orientation = Orientation.Vertical;
						var length = end.Y - start.Y;

						// if line is wider than it is tall switch to horizontal
						if (Math.Abs (start.X - end.X) > Math.Abs (start.Y - end.Y)) {
							orientation = Orientation.Horizontal;
							length = end.X - start.X;
						}

						if (length > 0) {
							length++;
						} else {
							length--;
						}

						_currentLayer.AddLine (
							start,
							length,
							orientation,
							LineStyle,
							new Attribute (_currentColor, GetNormalColor().Background));

						_currentLineStart = null;
						SetNeedsDisplay ();
					}
				}

				return base.OnMouseEvent (mouseEvent);
			}

			internal void SetColor (Color c)
			{
				_currentColor = c;
			}
		}
	}
}
