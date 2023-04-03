using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Basic Colors", Description: "Show all basic colors.")]
	[ScenarioCategory ("Colors")]
	[ScenarioCategory ("Text and Formatting")]
	public class BasicColors : Scenario {
		public override void Setup ()
		{
			var vx = 30;
			var x = 30;
			var y = 14;
			var colors = System.Enum.GetValues (typeof (Color));

			foreach (Color bg in colors) {
				Attribute attr = new Attribute (bg, colors.Length - 1 - bg);
				var vl = new Label (bg.ToString (), TextDirection.TopBottom_LeftRight) {
					X = vx,
					Y = 0,
					Width = 1,
					Height = 13,
					VerticalTextAlignment = VerticalTextAlignment.Bottom,
					ColorScheme = new ColorScheme () { Normal = attr }
				};
				Win.Add (vl);
				var hl = new Label (bg.ToString ()) {
					X = 15,
					Y = y,
					Width = 13,
					Height = 1,
					TextAlignment = TextAlignment.Right,
					ColorScheme = new ColorScheme () { Normal = attr }
				};
				Win.Add (hl);
				vx++;
				foreach (Color fg in colors) {
					var c = new Attribute (fg, bg);
					var t = x.ToString ();
					var l = new Label (x, y, t [t.Length - 1].ToString ()) {
						ColorScheme = new ColorScheme () { Normal = c }
					};
					Win.Add (l);
					x++;
				}
				x = 30;
				y++;
			}

			Win.Add (new Label ("Mouse over to get the view color:") {
				X = Pos.AnchorEnd (35)
			});
			Win.Add (new Label ("Foreground:") {
				X = Pos.AnchorEnd (34),
				Y = 2
			});

			var lblForeground = new Label () {
				X = Pos.AnchorEnd (20),
				Y = 2
			};
			Win.Add (lblForeground);

			var viewForeground = new View ("  ") {
				X = Pos.AnchorEnd (2),
				Y = 2,
				ColorScheme = new ColorScheme ()
			};
			Win.Add (viewForeground);

			Win.Add (new Label ("Background:") {
				X = Pos.AnchorEnd (34),
				Y = 4
			});

			var lblBackground = new Label () {
				X = Pos.AnchorEnd (20),
				Y = 4
			};
			Win.Add (lblBackground);

			var viewBackground = new View ("  ") {
				X = Pos.AnchorEnd (2),
				Y = 4,
				ColorScheme = new ColorScheme ()
			};
			Win.Add (viewBackground);

			Application.RootMouseEvent = (e) => {
				if (e.View != null) {
					var colorValue = e.View.GetNormalColor ().Value;
					Application.Driver.GetColors (colorValue, out Color fore, out Color back);
					lblForeground.Text = fore.ToString ();
					viewForeground.ColorScheme.Normal = new Attribute (fore, fore);
					lblBackground.Text = back.ToString ();
					viewBackground.ColorScheme.Normal = new Attribute (back, back);
				}
			};
		}
	}
}