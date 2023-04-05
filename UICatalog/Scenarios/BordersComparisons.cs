using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders Comparisons", Description: "Compares Window, Toplevel and FrameView borders.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class BordersComparisons : Scenario {
		public override void Init ()
		{
			Application.Init ();

			var borderStyle = BorderStyle.Double;
			var borderThickness = new Thickness (1, 2, 3, 4);
			var padding = 1;

			Application.Top.Text = $"Border Thickness: {borderThickness}\nPadding: {padding}";

			var win = new Window (new Rect (5, 5, 40, 20), "Window",
				padding: padding,
				border: new Border () {
					BorderStyle = borderStyle,
					BorderThickness = borderThickness
				});

			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += (s,e) => MessageBox.Query (20, 7, "Hi", "I'm a Window?", "Yes", "No");
			var label = new Label ("I'm a Window") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
			};
			var tv = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				Text = "1234567890"
			};
			var tf2 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			win.Add (tf1, button, label, tv, tf2);
			Application.Top.Add (win);

			var topLevel = new Toplevel (new Rect (50, 5, 40, 20));
			//topLevel.BorderFrame.Thickness = borderThickness;
			//topLevel.BorderFrame.BorderStyle = borderStyle;
			//topLevel.Padding.Thickness = paddingThickness;

			var tf3 = new TextField ("1234567890") { Width = 10 };

			var button2 = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button2.Clicked += (s,e) => MessageBox.Query (20, 7, "Hi", "I'm a Toplevel?", "Yes", "No");
			var label2 = new Label ("I'm a Toplevel") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
			};
			var tv2 = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				Text = "1234567890"
			};
			var tf4 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			topLevel.Add (tf3, button2, label2, tv2, tf4);
			Application.Top.Add (topLevel);

			var frameView = new FrameView (new Rect (95, 5, 40, 20), "FrameView", null,
				border: new Border () {
					BorderStyle = borderStyle,
					BorderThickness = borderThickness
				}
			);
			//frameView.BorderFrame.Thickness = borderThickness;
			//frameView.BorderFrame.BorderStyle = borderStyle;
			//frameView.Padding.Thickness = paddingThickness;

			var tf5 = new TextField ("1234567890") { Width = 10 };

			var button3 = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button3.Clicked += (s,e) => MessageBox.Query (20, 7, "Hi", "I'm a FrameView?", "Yes", "No");
			var label3 = new Label ("I'm a FrameView") {
				X = Pos.Center (),
				Y = Pos.Center () - 1,
			};
			var tv3 = new TextView () {
				Y = Pos.AnchorEnd (2),
				Width = 10,
				Height = Dim.Fill (),
				Text = "1234567890"
			};
			var tf6 = new TextField ("1234567890") {
				X = Pos.AnchorEnd (10),
				Y = Pos.AnchorEnd (1),
				Width = 10
			};
			frameView.Add (tf5, button3, label3, tv3, tf6);
			Application.Top.Add (frameView);

			Application.Run ();
		}

		public override void Run ()
		{
			// Do nothing
		}
	}
}