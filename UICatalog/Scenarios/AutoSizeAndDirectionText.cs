using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Text Direction and AutoSize", Description: "Demos TextFormatter Direction and View AutoSize.")]
	[ScenarioCategory ("Text and Formatting")]
	public class AutoSizeAndDirectionText : Scenario {
		public override void Setup ()
		{
			var text = "Hello World";
			var wideText = "Hello World 你";
			var color = Colors.Dialog;

			var labelH = new Label (text, TextDirection.LeftRight_TopBottom) {
				X = 1,
				Y = 1,
				Width = 11,
				Height = 1,
				ColorScheme = color
			};
			Win.Add (labelH);

			var labelV = new Label (text, TextDirection.TopBottom_LeftRight) {
				X = 70,
				Y = 1,
				Width = 1,
				Height = 11,
				ColorScheme = color
			};
			Win.Add (labelV);

			var editText = new TextView () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = 20,
				Height = 5,
				Text = text
			};

			editText.SetFocus ();

			Win.Add (editText);

			var ckbDirection = new CheckBox ("Toggle Direction") {
				X = Pos.Center (),
				Y = Pos.Center () + 3
			};
			ckbDirection.Toggled += (s,e) => {
				if (labelH.TextDirection == TextDirection.LeftRight_TopBottom) {
					labelH.TextDirection = TextDirection.TopBottom_LeftRight;
					labelV.TextDirection = TextDirection.LeftRight_TopBottom;
				} else {
					labelH.TextDirection = TextDirection.LeftRight_TopBottom;
					labelV.TextDirection = TextDirection.TopBottom_LeftRight;
				}
			};
			Win.Add (ckbDirection);

			var ckbAutoSize = new CheckBox ("Auto Size") {
				X = Pos.Center (),
				Y = Pos.Center () + 5,
				Checked = labelH.AutoSize = labelV.AutoSize
			};
			ckbAutoSize.Toggled += (s,e) => labelH.AutoSize = labelV.AutoSize = (bool)ckbAutoSize.Checked;
			Win.Add (ckbAutoSize);

			var ckbPreserveTrailingSpaces = new CheckBox ("Preserve Trailing Spaces") {
				X = Pos.Center (),
				Y = Pos.Center () + 7,
				Checked = labelH.PreserveTrailingSpaces = labelV.PreserveTrailingSpaces
			};
			ckbPreserveTrailingSpaces.Toggled += (s, e) =>
					labelH.PreserveTrailingSpaces = labelV.PreserveTrailingSpaces = (bool)ckbPreserveTrailingSpaces.Checked;
			Win.Add (ckbPreserveTrailingSpaces);

			var ckbWideText = new CheckBox ("Use wide runes") {
				X = Pos.Center (),
				Y = Pos.Center () + 9
			};
			ckbWideText.Toggled += (s, e) => {
				if (ckbWideText.Checked == true) {
					labelH.Text = labelV.Text = editText.Text = wideText;
					labelH.Width = 14;
					labelV.Height = 13;
				} else {
					labelH.Text = labelV.Text = editText.Text = text;
					labelH.Width = 11;
					labelV.Width = 1;
					labelV.Height = 11;
				}
			};
			Win.Add (ckbWideText);

			Win.KeyUp += (s,e) =>
				labelH.Text = labelV.Text = text = editText.Text.ToString ();
		}
	}
}