using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Invert Colors", Description: "Invert the foreground and the background colors.")]
	[ScenarioCategory ("Colors")]
	[ScenarioCategory ("Text and Formatting")]
	public class InvertColors : Scenario {
		public override void Setup ()
		{
			Win.ColorScheme = Colors.TopLevel;

			List<Label> labels = new List<Label> ();
			var foreColors = Enum.GetValues (typeof (Color)).Cast<Color> ().ToArray ();
			for (int y = 0; y < foreColors.Length; y++) {

				var fore = foreColors [y];
				var back = foreColors [(y + 1) % foreColors.Length];
				var color = Application.Driver.MakeAttribute (fore, back);

				var label = new Label ($"{fore} on {back}") {
					ColorScheme = new ColorScheme (),
					Y = y
				};
				label.ColorScheme.Normal = color;
				Win.Add (label);
				labels.Add (label);
			}

			var button = new Button ("Invert color!") {
				X = Pos.Center (),
				Y = foreColors.Length + 1,
			};
			button.Clicked += (s,e) => {

				foreach (var label in labels) {
					var color = label.ColorScheme.Normal;
					color = Application.Driver.MakeAttribute (color.Background, color.Foreground);

					label.ColorScheme.Normal = color;
					label.Text = $"{color.Foreground} on {color.Background}";
					label.SetNeedsDisplay ();

				}
			};
			Win.Add (button);
		}
	}
}