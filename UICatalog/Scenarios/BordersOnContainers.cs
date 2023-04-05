using System;
using System.Globalization;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	public class BordersOnContainers : Window {
		public BordersOnContainers (NStack.ustring title, string typeName, View smartView)
		{
			var borderStyle = BorderStyle.Double;
			var drawMarginFrame = false;
			var borderThickness = new Thickness (1, 2, 3, 4);
			var borderBrush = Colors.Base.HotFocus.Foreground;
			var padding = new Thickness (1, 2, 3, 4);
			var background = Colors.Base.HotNormal.Foreground;
			var effect3D = true;

			smartView.X = Pos.Center ();
			smartView.Y = 0;
			smartView.Width = 40;
			smartView.Height = 20;
			smartView.Border = new Border () {
				BorderStyle = borderStyle,
				DrawMarginFrame = drawMarginFrame,
				BorderThickness = borderThickness,
				ForgroundColor = borderBrush,
				PaddingThickness = padding,
				BackgroundColor = background,
				Effect3D = effect3D,
				//Title = typeName
			};
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
					smartView.Border.PaddingThickness = new Thickness (smartView.Border.PaddingThickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Border.PaddingThickness.Right,
						smartView.Border.PaddingThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingTopEdit.Text = $"{smartView.Border.PaddingThickness.Top}";

			Add (paddingTopEdit);

			var paddingLeftEdit = new TextField ("") {
				X = Pos.Center () - 30,
				Y = 2,
				Width = 5
			};
			paddingLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.PaddingThickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Border.PaddingThickness.Top, smartView.Border.PaddingThickness.Right,
						smartView.Border.PaddingThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingLeftEdit.Text = $"{smartView.Border.PaddingThickness.Left}";
			Add (paddingLeftEdit);

			var paddingRightEdit = new TextField ("") {
				X = Pos.Center () - 15,
				Y = 2,
				Width = 5
			};
			paddingRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.PaddingThickness = new Thickness (smartView.Border.PaddingThickness.Left,
						smartView.Border.PaddingThickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Border.PaddingThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingRightEdit.Text = $"{smartView.Border.PaddingThickness.Right}";
			Add (paddingRightEdit);

			var paddingBottomEdit = new TextField ("") {
				X = Pos.Center () - 22,
				Y = 3,
				Width = 5
			};
			paddingBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.PaddingThickness = new Thickness (smartView.Border.PaddingThickness.Left,
						smartView.Border.PaddingThickness.Top, smartView.Border.PaddingThickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			paddingBottomEdit.Text = $"{smartView.Border.PaddingThickness.Bottom}";
			Add (paddingBottomEdit);

			var replacePadding = new Button ("Replace all based on top") {
				X = Pos.Left (paddingLeftEdit),
				Y = 5
			};
			replacePadding.Clicked += (s, e) => {
				smartView.Border.PaddingThickness = new Thickness (smartView.Border.PaddingThickness.Top);
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
					smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Left,
						int.Parse (e.NewText.ToString ()), smartView.Border.BorderThickness.Right,
						smartView.Border.BorderThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderTopEdit.Text = $"{smartView.Border.BorderThickness.Top}";

			Add (borderTopEdit);

			var borderLeftEdit = new TextField ("") {
				X = Pos.Center () + 5,
				Y = 2,
				Width = 5
			};
			borderLeftEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (int.Parse (e.NewText.ToString ()),
						smartView.Border.BorderThickness.Top, smartView.Border.BorderThickness.Right,
						smartView.Border.BorderThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderLeftEdit.Text = $"{smartView.Border.BorderThickness.Left}";
			Add (borderLeftEdit);

			var borderRightEdit = new TextField ("") {
				X = Pos.Center () + 19,
				Y = 2,
				Width = 5
			};
			borderRightEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Left,
						smartView.Border.BorderThickness.Top, int.Parse (e.NewText.ToString ()),
						smartView.Border.BorderThickness.Bottom);
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderRightEdit.Text = $"{smartView.Border.BorderThickness.Right}";
			Add (borderRightEdit);

			var borderBottomEdit = new TextField ("") {
				X = Pos.Center () + 12,
				Y = 3,
				Width = 5
			};
			borderBottomEdit.TextChanging += (s, e) => {
				try {
					smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Left,
						smartView.Border.BorderThickness.Top, smartView.Border.BorderThickness.Right,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty) {
						e.Cancel = true;
					}
				}
			};
			borderBottomEdit.Text = $"{smartView.Border.BorderThickness.Bottom}";
			Add (borderBottomEdit);

			var replaceBorder = new Button ("Replace all based on top") {
				X = Pos.Left (borderLeftEdit),
				Y = 5
			};
			replaceBorder.Clicked += (s, e) => {
				smartView.Border.BorderThickness = new Thickness (smartView.Border.BorderThickness.Top);
				if (borderTopEdit.Text.IsEmpty) {
					borderTopEdit.Text = "0";
				}
				borderBottomEdit.Text = borderLeftEdit.Text = borderRightEdit.Text = borderTopEdit.Text;
			};
			Add (replaceBorder);

			smartView.Y = Pos.Center () + 4;

			Add (new Label ("BorderStyle:"));

			var borderStyleEnum = Enum.GetValues (typeof (BorderStyle)).Cast<BorderStyle> ().ToList ();
			var rbBorderStyle = new RadioGroup (borderStyleEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 1,
				SelectedItem = (int)smartView.Border.BorderStyle
			};
			Add (rbBorderStyle);

			var cbDrawMarginFrame = new CheckBox ("Draw Margin Frame", smartView.Border.DrawMarginFrame) {
				X = Pos.AnchorEnd (20),
				Y = 0,
				Width = 5
			};
			cbDrawMarginFrame.Toggled += (s, e) => {
				try {
					smartView.Border.DrawMarginFrame = (bool)cbDrawMarginFrame.Checked;
					if (cbDrawMarginFrame.Checked != smartView.Border.DrawMarginFrame) {
						cbDrawMarginFrame.Checked = smartView.Border.DrawMarginFrame;
					}
				} catch { }
			};
			Add (cbDrawMarginFrame);

			rbBorderStyle.SelectedItemChanged += (s, e) => {
				smartView.Border.BorderStyle = (BorderStyle)e.SelectedItem;
				smartView.SetNeedsDisplay ();
				if (cbDrawMarginFrame.Checked != smartView.Border.DrawMarginFrame) {
					cbDrawMarginFrame.Checked = smartView.Border.DrawMarginFrame;
				}
			};

			var cbEffect3D = new CheckBox ("Draw 3D effects", smartView.Border.Effect3D) {
				X = Pos.AnchorEnd (20),
				Y = 1,
				Width = 5
			};
			Add (cbEffect3D);

			Add (new Label ("Effect3D Offset:") {
				X = Pos.AnchorEnd (20),
				Y = 2
			});
			Add (new Label ("X:") {
				X = Pos.AnchorEnd (19),
				Y = 3
			});

			var effect3DOffsetX = new TextField ("") {
				X = Pos.AnchorEnd (16),
				Y = 3,
				Width = 5
			};
			effect3DOffsetX.TextChanging += (s, e) => {
				try {
					smartView.Border.Effect3DOffset = new Point (int.Parse (e.NewText.ToString ()),
						smartView.Border.Effect3DOffset.Y);
				} catch {
					if (!e.NewText.IsEmpty && e.NewText != CultureInfo.CurrentCulture.NumberFormat.NegativeSign) {
						e.Cancel = true;
					}
				}
			};
			effect3DOffsetX.Text = $"{smartView.Border.Effect3DOffset.X}";
			Add (effect3DOffsetX);

			Add (new Label ("Y:") {
				X = Pos.AnchorEnd (10),
				Y = 3
			});

			var effect3DOffsetY = new TextField ("") {
				X = Pos.AnchorEnd (7),
				Y = 3,
				Width = 5
			};
			effect3DOffsetY.TextChanging += (s, e) => {
				try {
					smartView.Border.Effect3DOffset = new Point (smartView.Border.Effect3DOffset.X,
						int.Parse (e.NewText.ToString ()));
				} catch {
					if (!e.NewText.IsEmpty && e.NewText != CultureInfo.CurrentCulture.NumberFormat.NegativeSign) {
						e.Cancel = true;
					}
				}
			};
			effect3DOffsetY.Text = $"{smartView.Border.Effect3DOffset.Y}";
			Add (effect3DOffsetY);

			cbEffect3D.Toggled += (s, e) => {
				try {
					smartView.Border.Effect3D = effect3DOffsetX.Enabled =
						effect3DOffsetY.Enabled = (bool)cbEffect3D.Checked;
				} catch { }
			};

			Add (new Label ("Background:") {
				Y = 5
			});

			var colorEnum = Enum.GetValues (typeof (Color)).Cast<Color> ().ToList ();
			var rbBackground = new RadioGroup (colorEnum.Select (
				e => NStack.ustring.Make (e.ToString ())).ToArray ()) {

				X = 2,
				Y = 6,
				SelectedItem = (int)smartView.Border.BackgroundColor
			};
			rbBackground.SelectedItemChanged += (s, e) => {
				smartView.Border.BackgroundColor = (Color)e.SelectedItem;
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
				SelectedItem = (int)smartView.Border.ForgroundColor
			};
			rbBorderBrush.SelectedItemChanged += (s, e) => {
				smartView.Border.ForgroundColor = (Color)e.SelectedItem;
			};
			Add (rbBorderBrush);

			Add (smartView);

			Title = title;
		}
	}
}
