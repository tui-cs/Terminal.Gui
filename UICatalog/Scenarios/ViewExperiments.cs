using System;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Configuration;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "_ View Experiments", Description: "v2 View Experiments")]
	[ScenarioCategory ("Controls")]
	public class ViewExperiments : Scenario {

		public class ThicknessEditor : View {
			private Thickness thickness;

			public Thickness Thickness {
				get => thickness;
				set {
					thickness = value;
					ThicknessChanged?.Invoke (this, new ThicknessEventArgs () { Thickness = Thickness });
				}
			}

			public event EventHandler<ThicknessEventArgs> ThicknessChanged;

			public ThicknessEditor ()
			{
				Margin.Thickness = new Thickness (0);
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
					X = 0,
					Y = 0,
					Title = "Margin",
					Thickness = viewToEdit.Margin.Thickness,
				};
				marginEditor.Margin.Thickness = new Thickness (0, 0, 1, 0);
				marginEditor.ThicknessChanged += (s, a) => {
					viewToEdit.Margin.Thickness = a.Thickness;
				};
				Add (marginEditor);

				viewToEdit.BorderFrame.ColorScheme = Colors.ColorSchemes ["Base"];
				var borderEditor = new ThicknessEditor () {
					X = Pos.Right (marginEditor),
					Y = 0,
					Title = "Border",
					Thickness = viewToEdit.BorderFrame.Thickness,
				};
				borderEditor.Margin.Thickness = new Thickness (0, 0, 1, 0);
				borderEditor.ThicknessChanged += (s, a) => {
					viewToEdit.BorderFrame.Thickness = a.Thickness;
				};
				Add (borderEditor);

				var styleLabel = new Label ("BorderStyle: ") {
					X = Pos.Right (borderEditor),
					Y = 0
				};
				Add (styleLabel);

				var borderStyleEnum = Enum.GetValues (typeof (BorderStyle)).Cast<BorderStyle> ().ToList ();
				var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
					e => NStack.ustring.Make (e.ToString ())).ToArray ()) {
					X = Pos.Left (styleLabel),
					Y = Pos.Bottom (styleLabel),
					SelectedItem = (int)viewToEdit.BorderFrame.BorderStyle
				};

				rbBorderStyle.SelectedItemChanged += (s, e) => {
					viewToEdit.BorderFrame.BorderStyle = (BorderStyle)e.SelectedItem;
					viewToEdit.SetNeedsDisplay ();
				};
				Add (rbBorderStyle);

				viewToEdit.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
				var paddingEditor = new ThicknessEditor () {
					X = Pos.Right (styleLabel),
					Y = 0,
					Title = "Padding",
					Thickness = viewToEdit.Padding.Thickness,
				};
				paddingEditor.ThicknessChanged += (s, a) => {
					viewToEdit.Padding.Thickness = a.Thickness;
				};
				Add (paddingEditor);

				viewToEdit.Y = Pos.Center () + 4;



				//rbBorderStyle.SelectedItemChanged += (e) => {
				//	viewToEdit.BorderFrame.BorderStyle = (BorderStyle)e.SelectedItem;
				//	viewToEdit.SetNeedsDisplay ();
				//};

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
				//rbBackground.SelectedItemChanged += (e) => {
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
				//rbBorderBrush.SelectedItemChanged += (e) => {
				//	if (viewToEdit.Border != null) {
				//		viewToEdit.Border.ForgroundColor = (Color)e.SelectedItem;
				//	}
				//};
				//Add (rbBorderBrush);

				Height = 8; 
				Title = title;
			}
		}

		public override void Init ()
		{
			Application.Init ();
			//Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];

		}

		public override void Setup ()
		{
			//ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FramePadding;
			var containerLabel = new Label () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = 3,
			};
			Application.Top.Add (containerLabel);

			var view = new View () {
				X = 2,
				Y = Pos.Bottom (containerLabel),
				Height = Dim.Fill (2),
				Width = Dim.Fill (2),
				Title = "View with 2xMargin, 2xBorder, & 2xPadding",
				ColorScheme = Colors.ColorSchemes ["Base"],
				Id = "DaView"
			};

			//Application.Top.Add (view);

			//view.InitializeFrames ();
			view.Margin.Thickness = new Thickness (2, 2, 2, 2);
			view.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view.Margin.Data = "Margin";
			view.BorderFrame.Thickness = new Thickness (2);
			view.BorderFrame.BorderStyle = BorderStyle.Single;
			view.BorderFrame.ColorScheme = view.ColorScheme;
			view.BorderFrame.Data = "BorderFrame";
			view.Padding.Thickness = new Thickness (2);
			view.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view.Padding.Data = "Padding";

			var view2 = new View () {
				X = 2,
				Y = 3,
				Height = 7,
				Width = 17,
				Title = "View2",
				Text = "View #2",
				TextAlignment = TextAlignment.Centered
			};

			//view2.InitializeFrames ();
			view2.Margin.Thickness = new Thickness (1);
			view2.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view2.Margin.Data = "Margin";
			view2.BorderFrame.Thickness = new Thickness (1);
			view2.BorderFrame.BorderStyle = BorderStyle.Single;
			view2.BorderFrame.ColorScheme = view.ColorScheme;
			view2.BorderFrame.Data = "BorderFrame";
			view2.Padding.Thickness = new Thickness (1);
			view2.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view2.Padding.Data = "Padding";

			view.Add (view2);

			var view3 = new View () {
				X = Pos.Right (view2) + 1,
				Y = 3,
				Height = 5,
				Width = 37,
				Title = "View3",
				Text = "View #3 (Right(view2)+1",
				TextAlignment = TextAlignment.Centered
			};

			//view3.InitializeFrames ();
			view3.Margin.Thickness = new Thickness (1, 1, 0, 0);
			view3.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view3.Margin.Data = "Margin";
			view3.BorderFrame.Thickness = new Thickness (1, 1, 1, 1);
			view3.BorderFrame.BorderStyle = BorderStyle.Single;
			view3.BorderFrame.ColorScheme = view.ColorScheme;
			view3.BorderFrame.Data = "BorderFrame";
			view3.Padding.Thickness = new Thickness (1, 1, 0, 0);
			view3.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view3.Padding.Data = "Padding";

			view.Add (view3);

			var view4 = new View () {
				X = Pos.Right (view3) + 1,
				Y = 3,
				Height = 5,
				Width = 37,
				Title = "View4",
				Text = "View #4 (Right(view3)+1",
				TextAlignment = TextAlignment.Centered
			};

			//view4.InitializeFrames ();
			view4.Margin.Thickness = new Thickness (0, 0, 1, 1);
			view4.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view4.Margin.Data = "Margin";
			view4.BorderFrame.Thickness = new Thickness (1, 1, 1, 1);
			view4.BorderFrame.BorderStyle = BorderStyle.Single;
			view4.BorderFrame.ColorScheme = view.ColorScheme;
			view4.BorderFrame.Data = "BorderFrame";
			view4.Padding.Thickness = new Thickness (0, 0, 1, 1);
			view4.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view4.Padding.Data = "Padding";

			view.Add (view4);

			var view5 = new View () {
				X = Pos.Right (view4) + 1,
				Y = 3,
				Height = Dim.Fill (2),
				Width = Dim.Fill (),
				Title = "View5",
				Text = "View #5 (Right(view4)+1 Fill",
				TextAlignment = TextAlignment.Centered
			};
			//view5.InitializeFrames ();
			view5.Margin.Thickness = new Thickness (0, 0, 0, 0);
			view5.Margin.ColorScheme = Colors.ColorSchemes ["Toplevel"];
			view5.Margin.Data = "Margin";
			view5.BorderFrame.Thickness = new Thickness (1, 1, 1, 1);
			view5.BorderFrame.BorderStyle = BorderStyle.Single;
			view5.BorderFrame.ColorScheme = view.ColorScheme;
			view5.BorderFrame.Data = "BorderFrame";
			view5.Padding.Thickness = new Thickness (0, 0, 0, 0);
			view5.Padding.ColorScheme = Colors.ColorSchemes ["Error"];
			view5.Padding.Data = "Padding";

			view.Add (view5);

			var label = new Label () {
				Text = "AutoSize true; 1;1:",
				AutoSize = true,
				X = 1,
				Y = 1,

			};
			view.Add (label);

			var edit = new TextField () {
				Text = "Right (label)",
				X = Pos.Right (label),
				Y = 1,
				Width = 15,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Right (edit) + 1",
				X = Pos.Right (edit) + 1,
				Y = 1,
				Width = 20,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Center();50%",
				X = Pos.Center (),
				Y = Pos.Percent (50),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "Center() - 1;60%",
				X = Pos.Center () - 1,
				Y = Pos.Percent (60),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "0 + Percent(50);70%",
				X = 0 + Pos.Percent (50),
				Y = Pos.Percent (70),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			edit = new TextField () {
				Text = "AnchorEnd[Right];AnchorEnd (1)",
				Y = Pos.AnchorEnd (1),
				Width = 30,
				Height = 1
			};
			edit.X = Pos.AnchorEnd () - (Pos.Right (edit) - Pos.Left (edit));
			view.Add (edit);

			edit = new TextField () {
				Text = "Left;AnchorEnd (2)",
				X = 0,
				Y = Pos.AnchorEnd (2),
				Width = 30,
				Height = 1
			};
			view.Add (edit);

			view.LayoutComplete += (s, e) => {
				containerLabel.Text = $"Container.Frame: {Application.Top.Frame} .Bounds: {Application.Top.Bounds}\nView.Frame: {view.Frame} .Bounds: {view.Bounds} .BoundsOffset: {view.GetBoundsOffset ()}\n .Padding.Frame: {view.Padding.Frame} .Padding.Bounds: {view.Padding.Bounds}";
			};

			view.X = Pos.Center ();

			var editor = new FramesEditor ($"Frame Editor", view) {
				X = 0,
				Y = Pos.Bottom (containerLabel),
				Width = Dim.Fill (),
			};

			Application.Top.Add (editor);

			view.Y = Pos.Bottom (editor);
			view.Width = Dim.Fill ();
			view.Height = Dim.Fill ();
			Application.Top.Add (view);
		}
	}
}