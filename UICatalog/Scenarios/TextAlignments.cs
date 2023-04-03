using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Simple Text Alignment", Description: "Demonstrates horizontal text alignment")]
	[ScenarioCategory ("Text and Formatting")]
	public class TextAlignments : Scenario {
		public override void Setup ()
		{
			Win.X = 10;
			Win.Width = Dim.Fill (10);

			string txt = "Hello world, how are you today? Pretty neat!";
			string unicodeSampleText = "A Unicode sentence (Ð¿ÑÐ Ð²ÐµÑ) has words.";

			var alignments = Enum.GetValues (typeof (Terminal.Gui.TextAlignment)).Cast<Terminal.Gui.TextAlignment> ().ToList ();
			var singleLines = new Label [alignments.Count];
			var multipleLines = new Label [alignments.Count];

			var multiLineHeight = 5;

			foreach (var alignment in alignments) {
				singleLines [(int)alignment] = new Label (txt) { TextAlignment = alignment, X = 1, Width = Dim.Fill (1), Height = 1, ColorScheme = Colors.Dialog, AutoSize = false };
				multipleLines [(int)alignment] = new Label (txt) { TextAlignment = alignment, X = 1, Width = Dim.Fill (1), Height = multiLineHeight, ColorScheme = Colors.Dialog, AutoSize = false };
			}

			// Add a label & text field so we can demo IsDefault
			var editLabel = new Label ("Text:") {
				X = 0,
				Y = 0,
			};
			Win.Add (editLabel);
			var edit = new TextView () {
				X = Pos.Right (editLabel) + 1,
				Y = Pos.Y (editLabel),
				Width = Dim.Fill ("Text:".Length + "  Unicode Sample".Length + 2),
				Height = 4,
				ColorScheme = Colors.TopLevel,
				Text = txt,
			};
			edit.TextChanged += (s,e) => {
				foreach (var alignment in alignments) {
					singleLines [(int)alignment].Text = edit.Text;
					multipleLines [(int)alignment].Text = edit.Text;
				}
			};
			Win.Add (edit);

			var unicodeSample = new Button ("Unicode Sample") {
				X = Pos.Right (edit) + 1,
				Y = 0,
			};
			unicodeSample.Clicked += (s,e) => {
				edit.Text = unicodeSampleText;
			};
			Win.Add (unicodeSample);

			var update = new Button ("_Update") {
				X = Pos.Right (edit) + 1,
				Y = Pos.Bottom (edit) - 1,

			};
			update.Clicked += (s,e) => {
				foreach (var alignment in alignments) {
					singleLines [(int)alignment].Text = edit.Text;
					multipleLines [(int)alignment].Text = edit.Text;
				}
			};
			Win.Add (update);

			var enableHotKeyCheckBox = new CheckBox ("Enable Hotkey (_)", false) {
				X = 0,
				Y = Pos.Bottom (edit),
			};

			Win.Add (enableHotKeyCheckBox);

			var label = new Label ($"Demonstrating single-line (should clip):") { Y = Pos.Bottom (enableHotKeyCheckBox) + 1 };
			Win.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Win.Add (label);
				singleLines [(int)alignment].Y = Pos.Bottom (label);
				Win.Add (singleLines [(int)alignment]);
				label = singleLines [(int)alignment];
			}

			txt += "\nSecond line\n\nFourth Line.";
			label = new Label ($"Demonstrating multi-line and word wrap:") { Y = Pos.Bottom (label) };
			Win.Add (label);
			foreach (var alignment in alignments) {
				label = new Label ($"{alignment}:") { Y = Pos.Bottom (label) };
				Win.Add (label);
				multipleLines [(int)alignment].Y = Pos.Bottom (label);
				Win.Add (multipleLines [(int)alignment]);
				label = multipleLines [(int)alignment];
			}

			enableHotKeyCheckBox.Toggled += (s,e) => {
				foreach (var alignment in alignments) {
					singleLines [(int)alignment].HotKeySpecifier = e.OldValue == true ? (Rune)0xffff : (Rune)'_';
					multipleLines [(int)alignment].HotKeySpecifier = e.OldValue == true ? (Rune)0xffff : (Rune)'_';
				}
				Win.SetNeedsDisplay ();
				Win.LayoutSubviews ();
			};
		}
	}
}