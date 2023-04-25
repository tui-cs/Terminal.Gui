using System;
using Terminal.Gui;
using System.Linq;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "LineCanvas Experiments", Description: "Experiments with LineCanvas")]
	[ScenarioCategory ("LineCanvas")]
	public class LineCanvasExperiment : Scenario {

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
				Title = "LineCanvas Experiments",
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = Colors.ColorSchemes ["Base"],
			};
			frame1.BorderStyle = LineStyle.Double;

			//ConsoleDriver.Diagnostics ^= ConsoleDriver.DiagnosticFlags.FrameRuler;

			Application.Top.Add (frame1);

			var win1 = new Window () {
				AutoSize = false,
				Title = "win1",
				Text = "Win1 30%/50% Heavy",
				X = 20,
				Y = 0,
				Width = 30, //Dim.Percent (30) - 5,
				Height = 10, //Dim.Percent (50) - 5,
				//ColorScheme = Colors.ColorSchemes ["Base"],
				BorderStyle = LineStyle.Heavy,
				SuperViewRendersLineCanvas = true
			};
			win1.Padding.Thickness = new Thickness (1);

			frame1.Add (win1);

			var win2 = new Window () {
				Title = "win2",
				Text = "Win2 right of win1, 30%/70% Single.",
				X = Pos.Right (win1) - 1,
				Y = 0,
				Width = Dim.Percent (30),
				Height = Dim.Percent (70),
				//ColorScheme = Colors.ColorSchemes ["Error"],
				SuperViewRendersLineCanvas = true
			};

			frame1.Add (win2);

			var view3 = new FrameView () {
				Title = "View 3",
				Text = "View3 right of win2 Fill/Fill Single",
				X = Pos.Right (win2) - 1,
				Y = 0,
				Width = Dim.Fill (-1),
				Height = Dim.Fill (-1),
				SuperViewRendersLineCanvas = true,
				//ColorScheme = Colors.ColorSchemes ["Menu"],
			};

			frame1.Add (view3);

			var view4 = new FrameView () {
				Title = "View 4",
				Text = "View4 below win2 win2.Width/5 Single",
				X = Pos.Left (win2),
				Y = Pos.Bottom (win2) - 1,
				Width = win2.Width,
				Height = 5,
				SuperViewRendersLineCanvas = true,
				//ColorScheme = Colors.ColorSchemes ["TopLevel"],
			};

			frame1.Add (view4);

			var win5 = new Window () {
				Title = "Win 5",
				Text = "win5 below View4 view4.Width/5 Double",
				X = Pos.Left (win2),
				Y = Pos.Bottom (view4) - 1,
				Width = view4.Width,
				Height = 5,
				//ColorScheme = Colors.ColorSchemes ["TopLevel"],
				SuperViewRendersLineCanvas = true,
				BorderStyle = LineStyle.Double 
			};

			frame1.Add (win5);

			var line = new Line () {
				X = 1,
				Y = 1,
				Width = 10,
				Height = 1,
				Orientation = Orientation.Horizontal,
				SuperViewRendersLineCanvas = true
			};
			frame1.Add (line);

			var marginWindow = new Window () {
				Title = "Positive Margin",
				X = 0,
				Y = 8,
				Width = 25,
				Height = 10,
				//ColorScheme = Colors.Error,
				SuperViewRendersLineCanvas = true
			};
			marginWindow.Margin.ColorScheme = Colors.Dialog;
			marginWindow.Margin.Thickness = new Thickness (1);
			marginWindow.Border.Thickness = new Thickness (1,2,1,1);

			frame1.Add (marginWindow);
		}
	}
}