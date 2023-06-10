using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Frames Demo", Description: "Demonstrates Margin, Border, and Padding on Views.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class Frames : Scenario {
		public class FrameEditor : View {
			private Thickness _thickness;
			private TextField _topEdit;
			private TextField _leftEdit;
			private TextField _rightEdit;
			private TextField _bottomEdit;
			private bool _isUpdating;

			private ColorPicker _foregroundColorPicker;
			private ColorPicker _backgroundColorPicker;

			public Terminal.Gui.Attribute Color { get; set; }

			public Thickness Thickness {
				get => _thickness;
				set {
					if (_isUpdating) {
						return;
					}
					_thickness = value;
					ThicknessChanged?.Invoke (this, new ThicknessEventArgs () { Thickness = Thickness });
					if (IsInitialized) {
						_isUpdating = true;
						if (_topEdit.Text != _thickness.Top.ToString ()) {
							_topEdit.Text = _thickness.Top.ToString ();
						}
						if (_leftEdit.Text != _thickness.Left.ToString ()) {
							_leftEdit.Text = _thickness.Left.ToString ();
						}
						if (_rightEdit.Text != _thickness.Right.ToString ()) {
							_rightEdit.Text = _thickness.Right.ToString ();
						}
						if (_bottomEdit.Text != _thickness.Bottom.ToString ()) {
							_bottomEdit.Text = _thickness.Bottom.ToString ();
						}
						_isUpdating = false;
					}
				}
			}

			public event EventHandler<ThicknessEventArgs> ThicknessChanged;
			public event EventHandler<Terminal.Gui.Attribute> AttributeChanged;

			public FrameEditor ()
			{
				Margin.Thickness = new Thickness (0);
				BorderStyle = LineStyle.Double;
				Initialized += FrameEditor_Initialized; ;
			}

			void FrameEditor_Initialized (object sender, EventArgs e)
			{
				var editWidth = 3;

				_topEdit = new TextField ("") {
					X = Pos.Center (),
					Y = 0,
					Width = editWidth
				};
				_topEdit.TextChanging += Edit_TextChanging;
				Add (_topEdit);

				_leftEdit = new TextField ("") {
					X = Pos.Left (_topEdit) - editWidth,
					Y = Pos.Bottom (_topEdit),
					Width = editWidth
				};
				_leftEdit.TextChanging += Edit_TextChanging;
				Add (_leftEdit);

				_rightEdit = new TextField ("") {
					X = Pos.Right (_topEdit),
					Y = Pos.Bottom (_topEdit),
					Width = editWidth
				};
				_rightEdit.TextChanging += Edit_TextChanging;
				Add (_rightEdit);

				_bottomEdit = new TextField ("") {
					X = Pos.Center (),
					Y = Pos.Bottom (_leftEdit),
					Width = editWidth
				};
				_bottomEdit.TextChanging += Edit_TextChanging;
				Add (_bottomEdit);

				var copyTop = new Button ("Copy Top") {
					X = Pos.Center () + 1,
					Y = Pos.Bottom (_bottomEdit)
				};
				copyTop.Clicked += (s, e) => {
					Thickness = new Thickness (Thickness.Top);
					if (string.IsNullOrEmpty (_topEdit.Text)) {
						_topEdit.Text = "0";
					}
					_bottomEdit.Text = _leftEdit.Text = _rightEdit.Text = _topEdit.Text;
				};
				Add (copyTop);

				// Foreground ColorPicker.
				_foregroundColorPicker = new ColorPicker () {
					Title = "FG",
					BoxWidth = 1,
					BoxHeight = 1,
					X = -1,
					Y = Pos.Bottom (copyTop) + 1,
					BorderStyle = LineStyle.Single,
					SuperViewRendersLineCanvas = true
				};
				_foregroundColorPicker.ColorChanged += (o, a) =>
					AttributeChanged?.Invoke (this,
						new Terminal.Gui.Attribute (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor));
				Add (_foregroundColorPicker);

				// Background ColorPicker.
				_backgroundColorPicker = new ColorPicker () {
					Title = "BG",
					BoxWidth = 1,
					BoxHeight = 1,
					X = Pos.Right (_foregroundColorPicker) - 1,
					Y = Pos.Top (_foregroundColorPicker),
					BorderStyle = LineStyle.Single,
					SuperViewRendersLineCanvas = true
				};

				_backgroundColorPicker.ColorChanged += (o, a) =>
					AttributeChanged?.Invoke (this,
						new Terminal.Gui.Attribute (
							_foregroundColorPicker.SelectedColor,
							_backgroundColorPicker.SelectedColor));
				Add (_backgroundColorPicker);

				_topEdit.Text = $"{Thickness.Top}";
				_leftEdit.Text = $"{Thickness.Left}";
				_rightEdit.Text = $"{Thickness.Right}";
				_bottomEdit.Text = $"{Thickness.Bottom}";

				LayoutSubviews ();
				Height = GetFramesThickness ().Vertical + 4 + 4;
				Width = GetFramesThickness ().Horizontal + _foregroundColorPicker.Frame.Width * 2 - 3;
			}

			private void Edit_TextChanging (object sender, TextChangingEventArgs e)
			{
				try {
					if (string.IsNullOrEmpty (e.NewText.ToString ())) {
						e.Cancel = true;
						((TextField)sender).Text = "0";
						return;
					}
					switch (sender.ToString ()) {
					case var s when s == _topEdit.ToString ():
						Thickness = new Thickness (Thickness.Left,
							int.Parse (e.NewText), Thickness.Right,
							Thickness.Bottom);
						break;
					case var s when s == _leftEdit.ToString ():
						Thickness = new Thickness (int.Parse (e.NewText),
							Thickness.Top, Thickness.Right,
							Thickness.Bottom);
						break;
					case var s when s == _rightEdit.ToString ():
						Thickness = new Thickness (Thickness.Left,
							Thickness.Top, int.Parse (e.NewText),
							Thickness.Bottom);
						break;
					case var s when s == _bottomEdit.ToString ():
						Thickness = new Thickness (Thickness.Left,
							Thickness.Top, Thickness.Right,
							int.Parse (e.NewText));
						break;
					}
				} catch {
					if (!string.IsNullOrEmpty(e.NewText)) {
						e.Cancel = true;
					}
				}
			}
		}

		public class FramesEditor : Window {
			private View _viewToEdit;
			private FrameEditor _marginEditor;
			private FrameEditor _borderEditor;
			private FrameEditor _paddingEditor;

			public FramesEditor (string title, View viewToEdit)
			{
				this._viewToEdit = viewToEdit;

				viewToEdit.Margin.ColorScheme = new ColorScheme (Colors.ColorSchemes ["Toplevel"]);
				_marginEditor = new FrameEditor () {
					X = 0,
					Y = 0,
					Title = "Margin",
					Thickness = viewToEdit.Margin.Thickness,
					SuperViewRendersLineCanvas = true
				};
				_marginEditor.ThicknessChanged += Editor_ThicknessChanged;
				_marginEditor.AttributeChanged += Editor_AttributeChanged; ;
				Add (_marginEditor);

				viewToEdit.Border.ColorScheme = new ColorScheme (Colors.ColorSchemes ["Base"]);
				_borderEditor = new FrameEditor () {
					X = Pos.Left (_marginEditor),
					Y = Pos.Bottom (_marginEditor),
					Title = "Border",
					Thickness = viewToEdit.Border.Thickness,
					SuperViewRendersLineCanvas = true
				};
				_borderEditor.ThicknessChanged += Editor_ThicknessChanged;
				_borderEditor.AttributeChanged += Editor_AttributeChanged;
				Add (_borderEditor);

				viewToEdit.Padding.ColorScheme = new ColorScheme (Colors.ColorSchemes ["Error"]);
				var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();

				var borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();
				var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
					e => e.ToString ()).ToArray ()) {

					X = Pos.Right (_borderEditor) - 1,
					Y = Pos.Top (_borderEditor),
					SelectedItem = (int)viewToEdit.Border.BorderStyle,
					BorderStyle = LineStyle.Double,
					Title = "Border Style",
					SuperViewRendersLineCanvas = true
				};
				Add (rbBorderStyle);

				rbBorderStyle.SelectedItemChanged += (s, e) => {
					var prevBorderStyle = viewToEdit.BorderStyle;
					viewToEdit.Border.BorderStyle = (LineStyle)e.SelectedItem;
					if (viewToEdit.Border.BorderStyle == LineStyle.None) {
						viewToEdit.Border.Thickness = new Thickness (0);
					} else if (prevBorderStyle == LineStyle.None && viewToEdit.Border.BorderStyle != LineStyle.None) {
						viewToEdit.Border.Thickness = new Thickness (1);
					}
					_borderEditor.Thickness = new Thickness (viewToEdit.Border.Thickness.Left, viewToEdit.Border.Thickness.Top,
						viewToEdit.Border.Thickness.Right, viewToEdit.Border.Thickness.Bottom);
					viewToEdit.SetNeedsDisplay ();
				};

				var ckbTitle = new CheckBox ("Show Title") {
					BorderStyle = LineStyle.Double,
					X = Pos.Left (_borderEditor),
					Y = Pos.Bottom (_borderEditor) - 1,
					Width = Dim.Width (_borderEditor),
					Checked = true,
					SuperViewRendersLineCanvas = true
				};
				Add (ckbTitle);

				_paddingEditor = new FrameEditor () {
					X = Pos.Left (_borderEditor),
					Y = Pos.Bottom (rbBorderStyle),
					Title = "Padding",
					Thickness = viewToEdit.Padding.Thickness,
					SuperViewRendersLineCanvas = true
				};
				_paddingEditor.ThicknessChanged += Editor_ThicknessChanged;
				_paddingEditor.AttributeChanged += Editor_AttributeChanged;
				Add (_paddingEditor);

				viewToEdit.X = Pos.Right (rbBorderStyle);
				viewToEdit.Y = 0;
				viewToEdit.Width = Dim.Fill ();
				viewToEdit.Height = Dim.Fill ();
				Add (viewToEdit);

				viewToEdit.LayoutComplete += (s, e) => {
					if (ckbTitle.Checked == true) {
						viewToEdit.Title = viewToEdit.ToString ();
					} else {
						viewToEdit.Title = string.Empty;
					}
				};

				Title = title;
			}

			private void Editor_AttributeChanged (object sender, Terminal.Gui.Attribute attr)
			{
				switch (sender.ToString ()) {
				case var s when s == _marginEditor.ToString ():
					_viewToEdit.Margin.ColorScheme = new ColorScheme (_viewToEdit.Margin.ColorScheme) { Normal = attr };
					break;
				case var s when s == _borderEditor.ToString ():
					_viewToEdit.Border.ColorScheme = new ColorScheme (_viewToEdit.Border.ColorScheme) { Normal = attr };
					break;
				case var s when s == _paddingEditor.ToString ():
					_viewToEdit.Padding.ColorScheme = new ColorScheme (_viewToEdit.Padding.ColorScheme) { Normal = attr };
					break;
				}
			}

			private void Editor_ThicknessChanged (object sender, ThicknessEventArgs e)
			{
				try {
					switch (sender.ToString ()) {
					case var s when s == _marginEditor.ToString ():
						_viewToEdit.Margin.Thickness = e.Thickness;
						break;
					case var s when s == _borderEditor.ToString ():
						_viewToEdit.Border.Thickness = e.Thickness;
						break;
					case var s when s == _paddingEditor.ToString ():
						_viewToEdit.Padding.Thickness = e.Thickness;
						break;
					}
				} catch {
					switch (sender.ToString ()) {
					case var s when s == _marginEditor.ToString ():
						_viewToEdit.Margin.Thickness = e.PreviousThickness;
						break;
					case var s when s == _borderEditor.ToString ():
						_viewToEdit.Border.Thickness = e.PreviousThickness;
						break;
					case var s when s == _paddingEditor.ToString ():
						_viewToEdit.Padding.Thickness = e.PreviousThickness;
						break;
					}
				}
			}
		}

		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

			var view = new Window ();
			var tf1 = new TextField ("TextField") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += (s, e) => MessageBox.Query (20, 7, "Hi", $"Am I a {view.GetType ().Name}?", "Yes", "No");
			var label = new Label ($"I'm a {view.GetType ().Name}") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
			};
			var tf2 = new Button ("Button") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv = new Label () {
				Y = Pos.AnchorEnd (2),
				Width = 25,
				Height = Dim.Fill (),
				Text = "Label\nY=AnchorEnd(2),Height=Dim.Fill()"
			};

			view.Margin.Thickness = new Thickness (3);
			view.Padding.Thickness = new Thickness (1);

			view.Add (tf1, button, label, tf2, tv);

			var editor = new FramesEditor (
				$"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				view);

			Application.Run (editor);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}