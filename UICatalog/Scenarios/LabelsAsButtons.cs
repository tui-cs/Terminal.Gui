using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Labels As Buttons", Description: "Illustrates that Button is really just a Label++")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Proof of Concept")]
	public class LabelsAsLabels : Scenario {
		public override void Setup ()
		{
			// Add a label & text field so we can demo IsDefault
			var editLabel = new Label ("TextField (to demo IsDefault):") {
				X = 0,
				Y = 0,
			};
			Win.Add (editLabel);
			// Add a TextField using Absolute layout. 
			var edit = new TextField (31, 0, 15, "");
			Win.Add (edit);

			// This is the default Label (IsDefault = true); if user presses ENTER in the TextField
			// the scenario will quit
			var defaultLabel = new Label ("_Quit") {
				X = Pos.Center (),
				//TODO: Change to use Pos.AnchorEnd()
				Y = Pos.Bottom (Win) - 3,
				//IsDefault = true,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			defaultLabel.Clicked += (s,e) => Application.RequestStop ();
			Win.Add (defaultLabel);

			var swapLabel = new Label (50, 0, "S_wap Default (Absolute Layout)") {
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			swapLabel.Clicked += (s,e) => {
				//defaultLabel.IsDefault = !defaultLabel.IsDefault;
				//swapLabel.IsDefault = !swapLabel.IsDefault;
			};
			Win.Add (swapLabel);

			static void DoMessage (Label Label, string txt)
			{
				Label.Clicked += (s,e) => {
					var btnText = Label.Text;
					MessageBox.Query ("Message", $"Did you click {txt}?", "Yes", "No");
				};
			}

			var colorLabelsLabel = new Label ("Color Labels:") {
				X = 0,
				Y = Pos.Bottom (editLabel) + 1,
			};
			Win.Add (colorLabelsLabel);

			//With this method there is no need to call Application.TopReady += () => Application.TopRedraw (Top.Bounds);
			var x = Pos.Right (colorLabelsLabel) + 2;
			foreach (var colorScheme in Colors.ColorSchemes) {
				var colorLabel = new Label ($"{colorScheme.Key}") {
					ColorScheme = colorScheme.Value,
					X = x,
					Y = Pos.Y (colorLabelsLabel),
					HotKeySpecifier = (Rune)'_',
					CanFocus = true,
				};
				DoMessage (colorLabel, colorLabel.Text);
				Win.Add (colorLabel);
				x += colorLabel.Text.Length + 2;
			}
			Application.Top.Ready += (s,e) => Application.Top.Draw ();

			Label Label;
			Win.Add (Label = new Label ("A super long _Label that will probably expose a bug in clipping or wrapping of text. Will it?") {
				X = 2,
				Y = Pos.Bottom (colorLabelsLabel) + 1,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			});
			DoMessage (Label, Label.Text);

			// Note the 'N' in 'Newline' will be the hotkey
			Win.Add (Label = new Label ("a Newline\nin the Label") {
				X = 2,
				Y = Pos.Bottom (Label) + 1,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
				TextAlignment = TextAlignment.Centered,
				VerticalTextAlignment = VerticalTextAlignment.Middle
			});
			Label.Clicked += (s,e) => MessageBox.Query ("Message", "Question?", "Yes", "No");

			var textChanger = new Label ("Te_xt Changer") {
				X = 2,
				Y = Pos.Bottom (Label) + 1,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			Win.Add (textChanger);
			textChanger.Clicked += (s,e) => textChanger.Text += "!";

			Win.Add (Label = new Label ("Lets see if this will move as \"Text Changer\" grows") {
				X = Pos.Right (textChanger) + 2,
				Y = Pos.Y (textChanger),
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			});

			var removeLabel = new Label ("Remove this Label") {
				X = 2,
				Y = Pos.Bottom (Label) + 1,
				ColorScheme = Colors.Error,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			Win.Add (removeLabel);
			// This in interesting test case because `moveBtn` and below are laid out relative to this one!
			removeLabel.Clicked += (s,e) => {
				// Now this throw a InvalidOperationException on the TopologicalSort method as is expected.
				//Win.Remove (removeLabel);

				removeLabel.Visible = false;
				Win.SetNeedsDisplay ();
			};

			var computedFrame = new FrameView ("Computed Layout") {
				X = 0,
				Y = Pos.Bottom (removeLabel) + 1,
				Width = Dim.Percent (50),
				Height = 5
			};
			Win.Add (computedFrame);

			// Demonstrates how changing the View.Frame property can move Views
			var moveBtn = new Label ("Move This \u263b Label _via Pos") {
				X = 0,
				Y = Pos.Center () - 1,
				Width = 30,
				ColorScheme = Colors.Error,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			moveBtn.Clicked += (s,e) => {
				moveBtn.X = moveBtn.Frame.X + 5;
				// This is already fixed with the call to SetNeedDisplay() in the Pos Dim.
				//computedFrame.LayoutSubviews (); // BUGBUG: This call should not be needed. View.X is not causing relayout correctly
			};
			computedFrame.Add (moveBtn);

			// Demonstrates how changing the View.Frame property can SIZE Views (#583)
			var sizeBtn = new Label ("Size This \u263a Label _via Pos") {
				//var sizeBtn = new Label ("Size This x Label _via Pos") {
				X = 0,
				Y = Pos.Center () + 1,
				Width = 30,
				ColorScheme = Colors.Error,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
				AutoSize = false
			};
			sizeBtn.Clicked += (s,e) => {
				sizeBtn.Width = sizeBtn.Frame.Width + 5;
				//computedFrame.LayoutSubviews (); // FIXED: This call should not be needed. View.X is not causing relayout correctly
			};
			computedFrame.Add (sizeBtn);

			var absoluteFrame = new FrameView ("Absolute Layout") {
				X = Pos.Right (computedFrame),
				Y = Pos.Bottom (removeLabel) + 1,
				Width = Dim.Fill (),
				Height = 5
			};
			Win.Add (absoluteFrame);

			// Demonstrates how changing the View.Frame property can move Views
			var moveBtnA = new Label (0, 0, "Move This Label via Frame") {
				ColorScheme = Colors.Error,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			moveBtnA.Clicked += (s,e) => {
				moveBtnA.Frame = new Rect (moveBtnA.Frame.X + 5, moveBtnA.Frame.Y, moveBtnA.Frame.Width, moveBtnA.Frame.Height);
			};
			absoluteFrame.Add (moveBtnA);

			// Demonstrates how changing the View.Frame property can SIZE Views (#583)
			var sizeBtnA = new Label (0, 2, " ~  s  gui.cs   master ↑10 = Со_хранить") {
				ColorScheme = Colors.Error,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
				AutoSize = false
			};
			sizeBtnA.Clicked += (s,e) => {
				sizeBtnA.Frame = new Rect (sizeBtnA.Frame.X, sizeBtnA.Frame.Y, sizeBtnA.Frame.Width + 5, sizeBtnA.Frame.Height);
			};
			absoluteFrame.Add (sizeBtnA);

			var label = new Label ("Text Alignment (changes the four Labels above): ") {
				X = 2,
				Y = Pos.Bottom (computedFrame) + 1,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			Win.Add (label);

			var radioGroup = new RadioGroup (new string [] { "Left", "Right", "Centered", "Justified" }) {
				X = 4,
				Y = Pos.Bottom (label) + 1,
				SelectedItem = 2,
			};
			Win.Add (radioGroup);

			// Demo changing hotkey
			string MoveHotkey (string txt)
			{
				// Remove the '_'
				var runes = txt.ToRuneList ();

				var i = runes.IndexOf ((Rune)'_');
				string start = "";
				if (i > -1) {
					start = StringExtensions.ToString (runes.GetRange (0, i));
				}
				txt = start + StringExtensions.ToString (runes.GetRange (i + 1, runes.Count - (i + 1)));

				runes = txt.ToRuneList ();

				// Move over one or go to start
				i++;
				if (i >= runes.Count) {
					i = 0;
				}

				// Slip in the '_'
				start = StringExtensions.ToString (runes.GetRange (0, i));
				return start + '_' + StringExtensions.ToString (runes.GetRange (i, runes.Count - i));
			}

			var mhkb = "Click to Change th_is Label's Hotkey";
			var moveHotKeyBtn = new Label (mhkb) {
				X = 2,
				Y = Pos.Bottom (radioGroup) + 1,
				Width = Dim.Width (computedFrame) - 2,
				ColorScheme = Colors.TopLevel,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			moveHotKeyBtn.Clicked += (s,e) => {
				moveHotKeyBtn.Text = MoveHotkey (moveHotKeyBtn.Text);
			};
			Win.Add (moveHotKeyBtn);

			string muhkb = " ~  s  gui.cs   master ↑10 = Сохранить";
			var moveUnicodeHotKeyBtn = new Label (muhkb) {
				X = Pos.Left (absoluteFrame) + 1,
				Y = Pos.Bottom (radioGroup) + 1,
				Width = Dim.Width (absoluteFrame) - 2,
				ColorScheme = Colors.TopLevel,
				HotKeySpecifier = (Rune)'_',
				CanFocus = true,
			};
			moveUnicodeHotKeyBtn.Clicked += (s,e) => {
				moveUnicodeHotKeyBtn.Text = MoveHotkey (moveUnicodeHotKeyBtn.Text);
			};
			Win.Add (moveUnicodeHotKeyBtn);

			radioGroup.SelectedItemChanged += (s,args) => {
				switch (args.SelectedItem) {
				case 0:
					moveBtn.TextAlignment = TextAlignment.Left;
					sizeBtn.TextAlignment = TextAlignment.Left;
					moveBtnA.TextAlignment = TextAlignment.Left;
					sizeBtnA.TextAlignment = TextAlignment.Left;
					moveHotKeyBtn.TextAlignment = TextAlignment.Left;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Left;
					break;
				case 1:
					moveBtn.TextAlignment = TextAlignment.Right;
					sizeBtn.TextAlignment = TextAlignment.Right;
					moveBtnA.TextAlignment = TextAlignment.Right;
					sizeBtnA.TextAlignment = TextAlignment.Right;
					moveHotKeyBtn.TextAlignment = TextAlignment.Right;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Right;
					break;
				case 2:
					moveBtn.TextAlignment = TextAlignment.Centered;
					sizeBtn.TextAlignment = TextAlignment.Centered;
					moveBtnA.TextAlignment = TextAlignment.Centered;
					sizeBtnA.TextAlignment = TextAlignment.Centered;
					moveHotKeyBtn.TextAlignment = TextAlignment.Centered;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Centered;
					break;
				case 3:
					moveBtn.TextAlignment = TextAlignment.Justified;
					sizeBtn.TextAlignment = TextAlignment.Justified;
					moveBtnA.TextAlignment = TextAlignment.Justified;
					sizeBtnA.TextAlignment = TextAlignment.Justified;
					moveHotKeyBtn.TextAlignment = TextAlignment.Justified;
					moveUnicodeHotKeyBtn.TextAlignment = TextAlignment.Justified;
					break;
				}
			};

			Application.Top.Ready += (s,e) => radioGroup.Refresh ();
		}
	}
}