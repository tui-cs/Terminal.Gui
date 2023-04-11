using NStack;
using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "MessageBoxes", Description: "Demonstrates how to use the MessageBox class.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	public class MessageBoxes : Scenario {
		public override void Setup ()
		{
			var frame = new FrameView ("MessageBox Options") {
				X = Pos.Center (),
				Y = 1,
				Width = Dim.Percent (75),
				Height = 12
			};
			Win.Add (frame);

			var label = new Label ("Width:") {
				X = 0,
				Y = 0,
				Width = 15,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var widthEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (widthEdit);

			label = new Label ("Height:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var heightEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (heightEdit);

			frame.Add (new Label ("If height & width are both 0,") {
				X = Pos.Right (widthEdit) + 2,
				Y = Pos.Top (widthEdit),
			});
			frame.Add (new Label ("the MessageBox will be sized automatically.") {
				X = Pos.Right (heightEdit) + 2,
				Y = Pos.Top (heightEdit),
			});

			label = new Label ("Title:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);

			var titleEdit = new TextField ("Title") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill (),
				Height = 1
			};
			frame.Add (titleEdit);

			label = new Label ("Message:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var messageEdit = new TextView () {
				Text = "Message",
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = Dim.Fill (),
				Height = 5,
			};
			frame.Add (messageEdit);

			label = new Label ("Num Buttons:") {
				X = 0,
				Y = Pos.Bottom (messageEdit),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var numButtonsEdit = new TextField ("3") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (numButtonsEdit);

			label = new Label ("Default Button:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);
			var defaultButtonEdit = new TextField ("0") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 5,
				Height = 1
			};
			frame.Add (defaultButtonEdit);

			label = new Label ("Style:") {
				X = 0,
				Y = Pos.Bottom (label),
				Width = Dim.Width (label),
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			frame.Add (label);

			var styleRadioGroup = new RadioGroup (new ustring [] { "_Query", "_Error" }) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
			};
			frame.Add (styleRadioGroup);

			var ckbWrapMessage = new CheckBox ("Wrap Message", true) {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label) + 3
			};
			frame.Add (ckbWrapMessage);

			frame.ForceValidatePosDim = true;
			void Top_Loaded (object sender, EventArgs args)
			{
				frame.Height =
					widthEdit.Frame.Height +
					heightEdit.Frame.Height +
					titleEdit.Frame.Height +
					messageEdit.Frame.Height +
					numButtonsEdit.Frame.Height +
					defaultButtonEdit.Frame.Height +
					styleRadioGroup.Frame.Height +
					2 +
					ckbWrapMessage.Frame.Height;
				Application.Top.Loaded -= Top_Loaded;
			}
			//Application.Top.Loaded += Top_Loaded;

			label = new Label ("Button Pressed:") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 4,
				Height = 1,
				TextAlignment = Terminal.Gui.TextAlignment.Right,
			};
			Win.Add (label);
			var buttonPressedLabel = new Label (" ") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 5,
				Width = 25,
				Height = 1,
				ColorScheme = Colors.Error,
				TextAlignment = Terminal.Gui.TextAlignment.Centered
			};

			//var btnText = new [] { "_Zero", "_One", "T_wo", "_Three", "_Four", "Fi_ve", "Si_x", "_Seven", "_Eight", "_Nine" };

			var showMessageBoxButton = new Button ("Show MessageBox") {
				X = Pos.Center (),
				Y = Pos.Bottom (frame) + 2,
				IsDefault = true,
			};
			showMessageBoxButton.Clicked += (s,e) => {
				try {
					int width = int.Parse (widthEdit.Text.ToString ());
					int height = int.Parse (heightEdit.Text.ToString ());
					int numButtons = int.Parse (numButtonsEdit.Text.ToString ());
					int defaultButton = int.Parse (defaultButtonEdit.Text.ToString ());

					var btns = new List<ustring> ();
					for (int i = 0; i < numButtons; i++) {
						//btns.Add(btnText[i % 10]);
						btns.Add (NumberToWords.Convert (i));
					}
					if (styleRadioGroup.SelectedItem == 0) {
						buttonPressedLabel.Text = $"{MessageBox.Query (width, height, titleEdit.Text.ToString (), messageEdit.Text.ToString (), defaultButton, (bool)ckbWrapMessage.Checked, btns.ToArray ())}";
					} else {
						buttonPressedLabel.Text = $"{MessageBox.ErrorQuery (width, height, titleEdit.Text.ToString (), messageEdit.Text.ToString (), defaultButton, (bool)ckbWrapMessage.Checked, btns.ToArray ())}";
					}
				} catch (FormatException) {
					buttonPressedLabel.Text = "Invalid Options";
				}
			};
			Win.Add (showMessageBoxButton);

			Win.Add (buttonPressedLabel);
		}
	}
}
