using System;
using Terminal.Gui;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Tile View Experiments", Description: "Experiments with Tile View")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("LineView")]
	public class TileViewExperiment : Scenario {


		public override void Init ()
		{
			Application.Init ();
		}

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			//var menu = new MenuBar (new MenuBarItem [] {
			//new MenuBarItem ("_File", new MenuItem [] {
			//	new MenuItem ("_Quit", "", () => Application.RequestStop()),
			//}) });

			//Application.Top.Add (menu);

			var frame1 = new FrameView () {
				Title = "frame1",
				X = 0,
				Y = 0,
				Width = 70, //Dim.Fill (),
				Height = 15, //Dim.Fill (),
					     //IgnoreBorderPropertyOnRedraw = true

			};
			frame1.BorderStyle = LineStyle.Double;

			//var frame2 = new FrameView () {
			//	Title = "frame2",
			//	X = 0,
			//	Y = Pos.Bottom (frame1) + 1,
			//	Width = 20, //Dim.Fill (),
			//	Height = 15, //Dim.Fill (),
			//		     //IgnoreBorderPropertyOnRedraw = true

			//};
			//frame2.Border.BorderStyle = BorderStyle.Single;

			//ConsoleDriver.Diagnostics ^= ConsoleDriver.DiagnosticFlags.FrameRuler;

			Application.Top.Add (frame1);
			//Application.Top.Add (frame2);

			var view1 = new View () {
				AutoSize = false,
				Title = "view1",
				Text = "View1 30%/50% Single",
				X = 0,
				Y = 0,
				Width = 30, //Dim.Percent (30) - 5,
				Height = 10, //Dim.Percent (50) - 5,
				ColorScheme = Colors.ColorSchemes ["Dialog"],
				BorderStyle = LineStyle.Single,
			};
			view1.Padding.Thickness = new Thickness (1);

			frame1.Add (view1);

			//var view12splitter = new SplitterEventArgs

			var view2 = new FrameView () {
				Title = "view2",
				Text = "View2 right of view1, 30%/70% Single.",
				X = Pos.Right (view1) - 1,
				Y = 0,
				Width = Dim.Percent (30),
				Height = Dim.Percent (70),
				ColorScheme = Colors.ColorSchemes ["Error"]
			};

			frame1.Add (view2);

			//var view3 = new FrameView () {
			//	Title = "View 3",
			//	Text = "View3 right of View2 Fill/Fill Single",
			//	X = Pos.Right (view2) - 1,
			//	Y = -1,
			//	Width = Dim.Fill (-1),
			//	Height = Dim.Fill (-1),
			//	ColorScheme = Colors.ColorSchemes ["Menu"],
			//	Border = new Border () { BorderStyle = BorderStyle.Single }
			//};

			//frame.Add (view3);

			//var view4 = new FrameView () {
			//	Title = "View 4",
			//	Text = "View4 below View2 view2.Width/5 Single",
			//	X = Pos.Left (view2),
			//	Y = Pos.Bottom (view2) - 1,
			//	Width = view2.Width,
			//	Height = 5,
			//	ColorScheme = Colors.ColorSchemes ["TopLevel"],
			//	Border = new Border () { BorderStyle = BorderStyle.Single }
			//};

			//frame.Add (view4);

			//var view5 = new FrameView () {
			//	Title = "View 5",
			//	Text = "View5 below View4 view4.Width/5 Double",
			//	X = Pos.Left (view2),
			//	Y = Pos.Bottom (view4) - 1,
			//	Width = view4.Width,
			//	Height = 5,
			//	ColorScheme = Colors.ColorSchemes ["TopLevel"],
			//	Border = new Border () { BorderStyle = BorderStyle.Double }
			//};

			//frame.Add (view5);
		}
	}
}