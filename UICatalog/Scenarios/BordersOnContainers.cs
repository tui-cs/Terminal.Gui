using System;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	public class BordersOnContainers : Window {
		public BordersOnContainers (NStack.ustring title, string typeName, View smartView)
		{
			var borderStyle = LineStyle.Double;
			var borderThickness = new Thickness (1, 2, 3, 4);
			var borderBrush = Colors.Base.HotFocus.Foreground;
			var padding = new Thickness (1, 2, 3, 4);
			var background = Colors.Base.HotNormal.Foreground;

			smartView.X = Pos.Center ();
			smartView.Y = 0;
			smartView.Width = 40;
			smartView.Height = 20;
			smartView.BorderStyle = borderStyle;
			
			smartView.ColorScheme = Colors.TopLevel;

			var tf1 = new TextField ("1234567890") { Width = 10 };

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += (s, e) => MessageBox.Query (20, 7, "Hi", $"I'm a {typeName}?", "Yes", "No");
			var label = new Label ($"I'm a {typeName}") {
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
			smartView.Add (tf1, button, label, tf2, tv);

			Add (new Label ("Padding:") {
				X = Pos.Center () - 23,
			});

			var paddingTopEdit = new TextField ("") {
				X = Pos.Center () - 22,
				Y = 1,
				Width = 5
			};
			paddingTopEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Padding.Thickness.Right,
						smartView.Padding.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingTopEdit.Text = $"{smartView.Padding.Thickness.Top}";

			Add (paddingTopEdit);

			var paddingLeftEdit = new TextField ("") {
				X = Pos.Center () - 30,
				Y = 2,
				Width = 5
			};
			paddingLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Padding.Thickness.Top, smartView.Padding.Thickness.Right,
						smartView.Padding.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingLeftEdit.Text = $"{smartView.Padding.Thickness.Left}";
			Add (paddingLeftEdit);

			var paddingRightEdit = new TextField ("") {
				X = Pos.Center () - 15,
				Y = 2,
				Width = 5
			};
			paddingRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Left,
						smartView.Padding.Thickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Padding.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingRightEdit.Text = $"{smartView.Padding.Thickness.Right}";
			Add (paddingRightEdit);

			var paddingBottomEdit = new TextField ("") {
				X = Pos.Center () - 22,
				Y = 3,
				Width = 5
			};
			paddingBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Left,
						smartView.Padding.Thickness.Top, smartView.Padding.Thickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingBottomEdit.Text = $"{smartView.Padding.Thickness.Bottom}";
			Add (paddingBottomEdit);

			var replacePadding = new Button ("Replace all based on top") {
				X = Pos.Left (paddingLeftEdit),
				Y = 5
			};
			replacePadding.Clicked += (s, e) => {
				smartView.Padding.Thickness = new Thickness (smartView.Padding.Thickness.Top);
				if (paddingTopEdit.Text.IsEmpty) {
					paddingTopEdit.Text = "0";
				}
				paddingBottomEdit.Text = paddingLeftEdit.Text = paddingRightEdit.Text = paddingTopEdit.Text;
			};
			Add (replacePadding);

			Add (new Label ("Border:") {
				X = Pos.Center () + 11,
			});

			var borderTopEdit = new TextField ("") {
				X = Pos.Center () + 12,
				Y = 1,
				Width = 5
			};
			borderTopEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Border.Thickness.Right,
						smartView.Border.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderTopEdit.Text = $"{smartView.Border.Thickness.Top}";

			Add (borderTopEdit);

			var borderLeftEdit = new TextField ("") {
				X = Pos.Center () + 5,
				Y = 2,
				Width = 5
			};
			borderLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Border.Thickness.Top, smartView.Border.Thickness.Right,
						smartView.Border.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderLeftEdit.Text = $"{smartView.Border.Thickness.Left}";
			Add (borderLeftEdit);

			var borderRightEdit = new TextField ("") {
				X = Pos.Center () + 19,
				Y = 2,
				Width = 5
			};
			borderRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Left,
						smartView.Border.Thickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Border.Thickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderRightEdit.Text = $"{smartView.Border.Thickness.Right}";
			Add (borderRightEdit);

			var borderBottomEdit = new TextField ("") {
				X = Pos.Center () + 12,
				Y = 3,
				Width = 5
			};
			borderBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Left,
						smartView.Border.Thickness.Top, smartView.Border.Thickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderBottomEdit.Text = $"{smartView.Border.Thickness.Bottom}";
			Add (borderBottomEdit);

			var replaceBorder = new Button ("Replace all based on top") {
				X = Pos.Left (borderLeftEdit),
				Y = 5
			};
			replaceBorder.Clicked += (s, e) => {
				smartView.Border.Thickness = new Thickness (smartView.Border.Thickness.Top);
				if (borderTopEdit.Text.IsEmpty) {
					borderTopEdit.Text = "0";
				}
				borderBottomEdit.Text = borderLeftEdit.Text = borderRightEdit.Text = borderTopEdit.Text;
			};
			Add (replaceBorder);

			smartView.Y = Pos.Center () + 4;

			Add (new Label ("BorderStyle:"));

			var borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();
			var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 1,
				SelectedItem = (int)smartView.BorderStyle
			};
			Add (rbBorderStyle);

			rbBorderStyle.SelectedItemChanged += (s, e) => {
				smartView.BorderStyle = (LineStyle)e.SelectedItem;
				smartView.SetNeedsDisplay ();
			};

			Add (new Label ("Background:") {
				Y = 12
			});

			var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();
			var rbBackground = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 13,
				//SelectedItem = (int)smartView.BorderFrame.BackgroundColor
			};
			rbBackground.SelectedItemChanged += (s, e) => {
//				smartView.Border.BackgroundColor = (Color)e.SelectedItem;
				smartView.Border.ColorScheme = new ColorScheme() { 
					Normal = new Terminal.Gui.Attribute(Color.Red, Color.White),
					HotNormal = new Terminal.Gui.Attribute (Color.Magenta, Color.White),
					Disabled = new Terminal.Gui.Attribute (Color.Gray, Color.White),
					Focus = new Terminal.Gui.Attribute (Color.Blue, Color.White),
					HotFocus = new Terminal.Gui.Attribute (Color.BrightBlue, Color.White),	
				};
			};
			Add (rbBackground);

			Add (new Label ("BorderBrush:") {
				X = Pos.AnchorEnd (20),
				Y = 5
			});

			var rbBorderBrush = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = Pos.AnchorEnd (18),
				Y = 6,
				//SelectedItem = (int)smartView.Border.ForgroundColor
			};
			rbBorderBrush.SelectedItemChanged += (s, e) => {
				//smartView.Border.ForgroundColor = (Color)e.SelectedItem;
			};
			Add (rbBorderBrush);

			Add (smartView);

			Title = title;
		}
	}
}
