using System.Globalization;
using System;
using Terminal.Gui;
using System.Linq;
using NStack;
using Terminal.Gui.Configuration;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Frames Demo", Description: "Demonstrates Margin, Border, and Padding on Views.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class Frames : Scenario {

		public class ThicknessEditor : View {
			private Thickness thickness;

			public Thickness Thickness {
				get => thickness;
				set {
					thickness = value;
					ThicknessChanged?.Invoke (this, new ThicknessEventArgs () {  Thickness = Thickness });
				}
			}

			public event EventHandler<ThicknessEventArgs> ThicknessChanged;

			public ThicknessEditor ()
			{
				Margin.Thickness = new Thickness (1);
				BorderFrame.Thickness = new Thickness (1);
			}

			public override void BeginInit ()
			{
				base.BeginInit ();

				var topEdit = new TextField ("") {
					X = Pos.Center (),
					Y = 0,
					Width = 5
				};
				topEdit.TextChanging += (s, e) => {
					try {
						Thickness = new Thickness (Thickness.Left,
							int.Parse (e.NewText.ToString ()), Thickness.Right,
							Thickness.Bottom);
					} catch {
						if (!e.NewText.IsEmpty) {
							e.Cancel = true;
						}
					}
				};
				topEdit.Text = $"{Thickness.Top}";

				Add (topEdit);

				var leftEdit = new TextField ("") {
					X = 0,
					Y = Pos.Bottom (topEdit),
					Width = 5
				};
				leftEdit.TextChanging += (s, e) => {
					try {
						Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
							Thickness.Top, Thickness.Right,
							Thickness.Bottom);
					} catch {
						if (!e.NewText.IsEmpty) {
							e.Cancel = true;
						}
					}
				};
				leftEdit.Text = $"{Thickness.Left}";
				Add (leftEdit);

				var rightEdit = new TextField ("") {
					X = Pos.Right (topEdit),
					Y = Pos.Bottom (topEdit),
					Width = 5
				};
				rightEdit.TextChanging += (s, e) => {
					try {
						Thickness = new Thickness (Thickness.Left,
							Thickness.Top, int.Parse (e.NewText.ToString ()),
							Thickness.Bottom);
					} catch {
						if (!e.NewText.IsEmpty) {
							e.Cancel = true;
						}
					}
				};
				rightEdit.Text = $"{Thickness.Right}";
				Add (rightEdit);

				var bottomEdit = new TextField ("") {
					X = Pos.Center (),
					Y = Pos.Bottom (leftEdit),
					Width = 5
				};
				bottomEdit.TextChanging += (s, e) => {
					try {
						Thickness = new Thickness (Thickness.Left,
							Thickness.Top, Thickness.Right,
							int.Parse (e.NewText.ToString ()));
					} catch {
						if (!e.NewText.IsEmpty) {
							e.Cancel = true;
						}
					}
				};
				bottomEdit.Text = $"{Thickness.Bottom}";
				Add (bottomEdit);

				var copyTop = new Button ("Copy Top") {
					X = Pos.Center (),
					Y = Pos.AnchorEnd (1)
				};
				copyTop.Clicked += (s, e) => {
					Thickness = new Thickness (Thickness.Top);
					if (topEdit.Text.IsEmpty) {
						topEdit.Text = "0";
					}
					bottomEdit.Text = leftEdit.Text = rightEdit.Text = topEdit.Text;
				};
				Add (copyTop);

				LayoutSubviews ();
				Height = Margin.Thickness.Vertical + BorderFrame.Thickness.Vertical + Padding.Thickness.Vertical + 4;
				Width = 20;
			}
		}

		public class FramesEditor : Window {
			public FramesEditor (NStack.ustring title, View viewToEdit)
			{
				viewToEdit.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
				var marginEditor = new ThicknessEditor () {
					X = 20,
					Y = 0,
					Title = "Margin",
					Thickness = viewToEdit.Margin.Thickness,
				};
				marginEditor.ThicknessChanged += (s, a) => {
					viewToEdit.Margin.Thickness = a.Thickness;
				};
				Add (marginEditor);

				viewToEdit.BorderFrame.ColorScheme = Colors.ColorSchemes ["Base"];
				var borderEditor = new ThicknessEditor () {
					X = Pos.Right(marginEditor),
					Y = 0,
					Title = "Border",
					Thickness = viewToEdit.BorderFrame.Thickness,
				};
				borderEditor.ThicknessChanged += (s, a) => {
					viewToEdit.BorderFrame.Thickness = a.Thickness;
				};
				Add (borderEditor);

				viewToEdit.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
				var paddingEditor = new ThicknessEditor () {
					X = Pos.Right (borderEditor),
					Y = 0,
					Title = "Padding",
					Thickness = viewToEdit.Padding.Thickness,
				};
				paddingEditor.ThicknessChanged += (s, a) => {
					viewToEdit.Padding.Thickness = a.Thickness;
				};
				Add (paddingEditor);

				viewToEdit.Y = Pos.Center () + 4;

				Add (new Label ("BorderStyle:"));

				var borderStyleEnum = Enum.GetValues (typeof (BorderStyle)).Cast<BorderStyle> ().ToList ();
				var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
					e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

					X = 2,
					Y = 1,
					SelectedItem = (int)viewToEdit.BorderFrame.BorderStyle
				};
				Add (rbBorderStyle);

				rbBorderStyle.SelectedItemChanged += (s, e) => {
					viewToEdit.BorderFrame.BorderStyle = (BorderStyle)e.SelectedItem;
					viewToEdit.SetNeedsDisplay ();
				};

				//Add (new Label ("Background:") {
				//	Y = 5
				//});

				//var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();
				//var rbBackground = new RadioGroup (colorEnum.Select (
				//	e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				//	X = 2,
				//	Y = 6,
				//	SelectedItem = (int)viewToEdit.Border.BackgroundColor
				//};
				//rbBackground.SelectedItemChanged += (s, e) => {
				//	if (viewToEdit.Border != null) {
				//		viewToEdit.Border.BackgroundColor = (Color)e.SelectedItem;
				//	}
				//};
				//Add (rbBackground);

				//Add (new Label ("BorderBrush:") {
				//	X = Pos.AnchorEnd (20),
				//	Y = 5
				//});

				//var rbBorderBrush = new RadioGroup (colorEnum.Select (
				//	e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				//	X = Pos.AnchorEnd (18),
				//	Y = 6,
				//	SelectedItem = (int)viewToEdit.Border.ForgroundColor
				//};
				//rbBorderBrush.SelectedItemChanged += (s, e) => {
				//	if (viewToEdit.Border != null) {
				//		viewToEdit.Border.ForgroundColor = (Color)e.SelectedItem;
				//	}
				//};
				//Add (rbBorderBrush);

				viewToEdit.X = Pos.Center ();
				viewToEdit.Y = Pos.Bottom (marginEditor);
				viewToEdit.Width = 50;
				viewToEdit.Height = 20;
				Add (viewToEdit);

				LayoutSubviews ();

				Title = title;
			}
		}

		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

			var view = new Window ();
			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += (s, e) => MessageBox.Query (20, 7, "Hi", $"I'm a {view.GetType().Name}?", "Yes", "No");
			var label = new Label ($"I'm a {view.GetType ().Name}") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
			};
			var tf2 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			var tv = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				Text = "1234567890"
			};

			view.Margin.Thickness = new Thickness (3);
			view.Margin.ColorScheme = Colors.ColorSchemes ["Dialog"];

			view.Add (tf1, button, label, tf2, tv);
			view.LayoutComplete += (s, e) => view.Title = view.ToString ();

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