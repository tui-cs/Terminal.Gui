using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Windows & FrameViews", Description: "Stress Tests Windows, sub-Windows, and FrameViews.")]
	[ScenarioCategory ("Layout")]
	public class WindowsAndFrameViews : Scenario {
		
		public override void Setup ()
		{
			static int About ()
			{
				return MessageBox.Query ("About UI Catalog", "UI Catalog is a comprehensive sample library for Terminal.Gui", "Ok");
			}

			int margin = 2;
			int padding = 1;
			int contentHeight = 7;

			// list of Windows we create
			var listWin = new List<View> ();

			//Ignore the Win that UI Catalog created and create a new one
			Application.Top.Remove (Win);
			Win?.Dispose ();

			Win = new Window () {
				Title = $"{listWin.Count} - Scenario: {GetName ()}",
				X = Pos.Center (),
				Y = 1,
				Width = Dim.Fill (15),
				Height = 10,
				ColorScheme = Colors.Dialog
			};
			Win.Padding.Thickness = new Thickness (padding);
			Win.Margin.Thickness = new Thickness (margin);

			var paddingButton = new Button ($"Padding of container is {padding}") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = Colors.Error,
			};
			paddingButton.Clicked += (s,e) => About ();
			Win.Add (paddingButton);
			Win.Add (new Button ("Press ME! (Y = Pos.AnchorEnd(1))") {
				X = Pos.Center (),
				Y = Pos.AnchorEnd (1),
				ColorScheme = Colors.Error
			});
			Application.Top.Add (Win);
			
			// add it to our list
			listWin.Add (Win);

			// create 3 more Windows in a loop, adding them Application.Top
			// Each with a
			//	button
			//  sub Window with
			//		TextField
			//	sub FrameView with
			// 
			for (var pad = 0; pad < 3; pad++) {
				Window win = null;
				win = new Window () {
					Title = $"{listWin.Count} - Window Loop - padding = {pad}",
					X = margin,
					Y = Pos.Bottom (listWin.Last ()) + (margin),
					Width = Dim.Fill (margin),
					Height = contentHeight + (pad * 2) + 2,
				};
				win.Padding.Thickness = new Thickness (pad);
				
				win.ColorScheme = Colors.Dialog;
				var pressMeButton = new Button ("Press me! (Y = 0)") {
					X = Pos.Center (),
					Y = 0,
					ColorScheme = Colors.Error,
				};
				pressMeButton.Clicked += (s,e) =>
					MessageBox.ErrorQuery (win.Title, "Neat?", "Yes", "No");
				win.Add (pressMeButton);
				var subWin = new Window () {
					Title = "Sub Window",
					X = Pos.Percent (0),
					Y = 1,
					Width = Dim.Percent (50),
					Height = 5,
					ColorScheme = Colors.Base,
					Text = "The Text in the Window",
				};
				subWin.Add (new TextField ("Edit me! " + win.Title) {
					Y = 1,
					ColorScheme = Colors.Error
				});
				win.Add (subWin);
				var frameView = new FrameView ("This is a Sub-FrameView") {
					X = Pos.Percent (50),
					Y = 1,
					Width = Dim.Percent (100, true), // Or Dim.Percent (50)
					Height = 5,
					ColorScheme = Colors.Base,
					Text = "The Text in the FrameView",

				};
				frameView.Add (new TextField ("Edit Me!") {
					Y = 1,
				});
				win.Add (frameView);

				Application.Top.Add (win);
				listWin.Add (win);
			}

			// Add a FrameView (frame) to Application.Top
			// Position it at Bottom, using the list of Windows we created above.
			// Fill it with
			//   a label
			//   a SubWindow containing (subWinofFV)
			//      a TextField
			//	    two checkboxes
			//   a Sub FrameView containing (subFrameViewofFV)
			//      a TextField
			//      two CheckBoxes	
			//   a checkbox
			//   a checkbox
			FrameView frame = null;
			frame = new FrameView ($"This is a FrameView") {
				X = margin,
				Y = Pos.Bottom (listWin.Last ()) + (margin / 2),
				Width = Dim.Fill (margin),
				Height = contentHeight + 2,  // 2 for default padding
			};
			frame.ColorScheme = Colors.Dialog;
			frame.Add (new Label ("This is a Label! (Y = 0)") {
				X = Pos.Center (),
				Y = 0,
				ColorScheme = Colors.Error,
				//Clicked = () => MessageBox.ErrorQuery (frame.Title, "Neat?", "Yes", "No")
			});
			var subWinofFV = new Window () {
				Title = "This is a Sub-Window",
				X = Pos.Percent (0),
				Y = 1,
				Width = Dim.Percent (50),
				Height = Dim.Fill () - 1,
				ColorScheme = Colors.Base,
				Text = "The Text in the Window",
			};
			subWinofFV.Add (new TextField ("Edit Me") {
				ColorScheme = Colors.Error
			});

			subWinofFV.Add (new CheckBox (0, 1, "Check me"));
			subWinofFV.Add (new CheckBox (0, 2, "Or, Check me"));

			frame.Add (subWinofFV);
			var subFrameViewofFV = new FrameView ("this is a Sub-FrameView") {
				X = Pos.Percent (50),
				Y = 1,
				Width = Dim.Percent (100),
				Height = Dim.Fill () - 1,
				ColorScheme = Colors.Base,
				Text = "The Text in the FrameView",
			};
			subFrameViewofFV.Add (new TextField (0, 0, 15, "Edit Me"));

			subFrameViewofFV.Add (new CheckBox (0, 1, "Check me"));
			// BUGBUG: This checkbox is not shown even though frameViewFV has 3 rows in 
			// its client area. #522
			subFrameViewofFV.Add (new CheckBox (0, 2, "Or, Check me"));

			frame.Add (new CheckBox ("Btn1 (Y = Pos.AnchorEnd (1))") {
				X = 0,
				Y = Pos.AnchorEnd (1),
			});
			CheckBox c = new CheckBox ("Btn2 (Y = Pos.AnchorEnd (1))") {
				Y = Pos.AnchorEnd (1),
			};
			c.X = Pos.AnchorEnd () - (Pos.Right (c) - Pos.Left (c));
			frame.Add (c);

			frame.Add (subFrameViewofFV);

			Application.Top.Add (frame);
			listWin.Add (frame);

			Application.Top.ColorScheme = Colors.Base;
		}
	}
}