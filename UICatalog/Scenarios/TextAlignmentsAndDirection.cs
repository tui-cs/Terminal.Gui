using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Text Alignment and Direction", Description: "Demos horiztonal and vertical text alignment and text direction.")]
	[ScenarioCategory ("Text and Formatting")]
	public class TextAlignmentsAndDirections : Scenario {

		public override void Setup ()
		{
			// string txt = ".\n...\n.....\nHELLO\n.....\n...\n.";
			// string txt = "┌──┴──┐\n┤HELLO├\n└──┬──┘";
			string txt = "HELLO WORLD";

			var color1 = new ColorScheme { Normal = Application.Driver.MakeAttribute (Color.Black, Color.Gray) };
			var color2 = new ColorScheme { Normal = Application.Driver.MakeAttribute (Color.Black, Color.DarkGray) };

			var txts = new List<Label> (); // single line
			var mtxts = new List<Label> (); // multi line

			// Horizontal Single-Line 

			var labelHL = new Label ("Left") { X = 1, Y = 1, Width = 9, Height = 1, TextAlignment = TextAlignment.Right, ColorScheme = Colors.ColorSchemes ["Dialog"] };
			var labelHC = new Label ("Centered") { X = 1, Y = 2, Width = 9, Height = 1, TextAlignment = TextAlignment.Right, ColorScheme = Colors.ColorSchemes ["Dialog"] };
			var labelHR = new Label ("Right") { X = 1, Y = 3, Width = 9, Height = 1, TextAlignment = TextAlignment.Right, ColorScheme = Colors.ColorSchemes ["Dialog"] };
			var labelHJ = new Label ("Justified") { X = 1, Y = 4, Width = 9, Height = 1, TextAlignment = TextAlignment.Right, ColorScheme = Colors.ColorSchemes ["Dialog"] };

			var txtLabelHL = new Label (txt) { X = Pos.Right (labelHL) + 1, Y = Pos.Y (labelHL), Width = Dim.Fill (1) - 9, Height = 1, ColorScheme = color1, TextAlignment = TextAlignment.Left };
			var txtLabelHC = new Label (txt) { X = Pos.Right (labelHC) + 1, Y = Pos.Y (labelHC), Width = Dim.Fill (1) - 9, Height = 1, ColorScheme = color2, TextAlignment = TextAlignment.Centered };
			var txtLabelHR = new Label (txt) { X = Pos.Right (labelHR) + 1, Y = Pos.Y (labelHR), Width = Dim.Fill (1) - 9, Height = 1, ColorScheme = color1, TextAlignment = TextAlignment.Right };
			var txtLabelHJ = new Label (txt) { X = Pos.Right (labelHJ) + 1, Y = Pos.Y (labelHJ), Width = Dim.Fill (1) - 9, Height = 1, ColorScheme = color2, TextAlignment = TextAlignment.Justified };

			txts.Add (txtLabelHL); txts.Add (txtLabelHC); txts.Add (txtLabelHR); txts.Add (txtLabelHJ);

			Win.Add (labelHL); Win.Add (txtLabelHL);
			Win.Add (labelHC); Win.Add (txtLabelHC);
			Win.Add (labelHR); Win.Add (txtLabelHR);
			Win.Add (labelHJ); Win.Add (txtLabelHJ);

			// Vertical Single-Line

			var labelVT = new Label ("Top") { X = Pos.AnchorEnd (8), Y = 1, Width = 2, Height = 9, ColorScheme = color1, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };
			var labelVM = new Label ("Middle") { X = Pos.AnchorEnd (6), Y = 1, Width = 2, Height = 9, ColorScheme = color1, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };
			var labelVB = new Label ("Bottom") { X = Pos.AnchorEnd (4), Y = 1, Width = 2, Height = 9, ColorScheme = color1, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };
			var labelVJ = new Label ("Justified") { X = Pos.AnchorEnd (2), Y = 1, Width = 1, Height = 9, ColorScheme = color1, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };

			var txtLabelVT = new Label (txt) { X = Pos.X (labelVT), Y = Pos.Bottom (labelVT) + 1, Width = 1, Height = Dim.Fill (1), ColorScheme = color1, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Top };
			var txtLabelVM = new Label (txt) { X = Pos.X (labelVM), Y = Pos.Bottom (labelVM) + 1, Width = 1, Height = Dim.Fill (1), ColorScheme = color2, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Middle };
			var txtLabelVB = new Label (txt) { X = Pos.X (labelVB), Y = Pos.Bottom (labelVB) + 1, Width = 1, Height = Dim.Fill (1), ColorScheme = color1, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };
			var txtLabelVJ = new Label (txt) { X = Pos.X (labelVJ), Y = Pos.Bottom (labelVJ) + 1, Width = 1, Height = Dim.Fill (1), ColorScheme = color2, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Justified };

			txts.Add (txtLabelVT); txts.Add (txtLabelVM); txts.Add (txtLabelVB); txts.Add (txtLabelVJ);

			Win.Add (labelVT); Win.Add (txtLabelVT);
			Win.Add (labelVM); Win.Add (txtLabelVM);
			Win.Add (labelVB); Win.Add (txtLabelVB);
			Win.Add (labelVJ); Win.Add (txtLabelVJ);

			// Multi-Line

			var container = new View () { X = 0, Y = Pos.Bottom (txtLabelHJ), Width = Dim.Fill (31), Height = Dim.Fill (6), ColorScheme = color2 };

			var txtLabelTL = new Label (txt) { X = 1 /*                    */, Y = 1, Width = Dim.Percent (100f / 3f), Height = Dim.Percent (100f / 3f), TextAlignment = TextAlignment.Left, VerticalTextAlignment = VerticalTextAlignment.Top, ColorScheme = color1 };
			var txtLabelTC = new Label (txt) { X = Pos.Right (txtLabelTL) + 2, Y = 1, Width = Dim.Percent (100f / 3f), Height = Dim.Percent (100f / 3f), TextAlignment = TextAlignment.Centered, VerticalTextAlignment = VerticalTextAlignment.Top, ColorScheme = color1 };
			var txtLabelTR = new Label (txt) { X = Pos.Right (txtLabelTC) + 2, Y = 1, Width = Dim.Percent (100f, true), Height = Dim.Percent (100f / 3f), TextAlignment = TextAlignment.Right, VerticalTextAlignment = VerticalTextAlignment.Top, ColorScheme = color1 };

			var txtLabelML = new Label (txt) { X = Pos.X (txtLabelTL)/*    */, Y = Pos.Bottom (txtLabelTL) + 1, Width = Dim.Width (txtLabelTL), Height = Dim.Percent (100f / 3f), TextAlignment = TextAlignment.Left, VerticalTextAlignment = VerticalTextAlignment.Middle, ColorScheme = color1 };
			var txtLabelMC = new Label (txt) { X = Pos.X (txtLabelTC)/*    */, Y = Pos.Bottom (txtLabelTC) + 1, Width = Dim.Width (txtLabelTC), Height = Dim.Percent (100f / 3f), TextAlignment = TextAlignment.Centered, VerticalTextAlignment = VerticalTextAlignment.Middle, ColorScheme = color1 };
			var txtLabelMR = new Label (txt) { X = Pos.X (txtLabelTR)/*    */, Y = Pos.Bottom (txtLabelTR) + 1, Width = Dim.Percent (100f, true), Height = Dim.Percent (100f / 3f), TextAlignment = TextAlignment.Right, VerticalTextAlignment = VerticalTextAlignment.Middle, ColorScheme = color1 };

			var txtLabelBL = new Label (txt) { X = Pos.X (txtLabelML)/*    */, Y = Pos.Bottom (txtLabelML) + 1, Width = Dim.Width (txtLabelML), Height = Dim.Percent (100f, true), TextAlignment = TextAlignment.Left, VerticalTextAlignment = VerticalTextAlignment.Bottom, ColorScheme = color1 };
			var txtLabelBC = new Label (txt) { X = Pos.X (txtLabelMC)/*    */, Y = Pos.Bottom (txtLabelMC) + 1, Width = Dim.Width (txtLabelMC), Height = Dim.Percent (100f, true), TextAlignment = TextAlignment.Centered, VerticalTextAlignment = VerticalTextAlignment.Bottom, ColorScheme = color1 };
			var txtLabelBR = new Label (txt) { X = Pos.X (txtLabelMR)/*    */, Y = Pos.Bottom (txtLabelMR) + 1, Width = Dim.Percent (100f, true), Height = Dim.Percent (100f, true), TextAlignment = TextAlignment.Right, VerticalTextAlignment = VerticalTextAlignment.Bottom, ColorScheme = color1 };

			mtxts.Add (txtLabelTL); mtxts.Add (txtLabelTC); mtxts.Add (txtLabelTR);
			mtxts.Add (txtLabelML); mtxts.Add (txtLabelMC); mtxts.Add (txtLabelMR);
			mtxts.Add (txtLabelBL); mtxts.Add (txtLabelBC); mtxts.Add (txtLabelBR);

			// Save Alignments in Data
			foreach (var t in mtxts) {
				t.Data = new { h = t.TextAlignment, v = t.VerticalTextAlignment };
			}

			container.Add (txtLabelTL);
			container.Add (txtLabelTC);
			container.Add (txtLabelTR);

			container.Add (txtLabelML);
			container.Add (txtLabelMC);
			container.Add (txtLabelMR);

			container.Add (txtLabelBL);
			container.Add (txtLabelBC);
			container.Add (txtLabelBR);

			Win.Add (container);

			// Edit Text

			var editText = new TextView () {
				X = 1,
				Y = Pos.Bottom (container) + 1,
				Width = Dim.Fill (10),
				Height = Dim.Fill (1),
				ColorScheme = Colors.TopLevel,
				Text = txt
			};

			editText.MouseClick += (s, m) => {
				foreach (var v in txts) {
					v.Text = editText.Text;
				}
				foreach (var v in mtxts) {
					v.Text = editText.Text;
				}
			};

			Win.KeyUp += (s, m) => {
				foreach (var v in txts) {
					v.Text = editText.Text;
				}
				foreach (var v in mtxts) {
					v.Text = editText.Text;
				}
			};

			editText.SetFocus ();

			Win.Add (editText);

			// JUSTIFY CHECKBOX

			var justifyCheckbox = new CheckBox ("Justify") {
				X = Pos.Right (container) + 1,
				Y = Pos.Y (container) + 1,
				Width = Dim.Fill (10),
				Height = 1
			};

			justifyCheckbox.Toggled += (s,e) => {
				if (e.OldValue == true) {
					foreach (var t in mtxts) {
						t.TextAlignment = (TextAlignment)((dynamic)t.Data).h;
						t.VerticalTextAlignment = (VerticalTextAlignment)((dynamic)t.Data).v;
					}
				} else {
					foreach (var t in mtxts) {
						if (TextFormatter.IsVerticalDirection (t.TextDirection)) {
							t.VerticalTextAlignment = VerticalTextAlignment.Justified;
							t.TextAlignment = ((dynamic)t.Data).h;
						} else {
							t.TextAlignment = TextAlignment.Justified;
							t.VerticalTextAlignment = ((dynamic)t.Data).v;
						}
					}
				}
			};

			Win.Add (justifyCheckbox);

			// Direction Options

			var directionsEnum = Enum.GetValues (typeof (Terminal.Gui.TextDirection)).Cast<Terminal.Gui.TextDirection> ().ToList ();

			var directionOptions = new RadioGroup (directionsEnum.Select (e => NStack.ustring.Make (e.ToString ())).ToArray ()) {
				X = Pos.Right (container) + 1,
				Y = Pos.Bottom (justifyCheckbox) + 1,
				Width = Dim.Fill (10),
				Height = Dim.Fill (1),
				HotKeySpecifier = '\xffff'
			};

			directionOptions.SelectedItemChanged += (s, ev) => {
				foreach (var v in mtxts) {
					v.TextDirection = (TextDirection)ev.SelectedItem;
				}
			};

			Win.Add (directionOptions);
		}
	}
}
