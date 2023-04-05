using NStack;
using System;
using System.Reflection.Emit;
using Terminal.Gui;
using Terminal.Gui.Configuration;
using Label = Terminal.Gui.Label;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Generic", Description: "Generic sample - A template for creating new Scenarios")]
	[ScenarioCategory ("Controls")]
	public class MyScenario : Scenario {
		public override void Init ()
		{
			// The base `Scenario.Init` implementation:
			//  - Calls `Application.Init ()`
			//  - Adds a full-screen Window to Application.Top with a title
			//    that reads "Press <hotkey> to Quit". Access this Window with `this.Win`.
			//  - Sets the Theme & the ColorScheme property of `this.Win` to `colorScheme`.
			// To override this, implement an override of `Init`.
			
			//base.Init ();
			
			// A common, alternate, implementation where `this.Win` is not used is below. This code
			// leverages ConfigurationManager to borrow the color scheme settings from UICatalog:
			
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
		}

		public override void Setup ()
		{
			// Put scenario code here (in a real app, this would be the code
			// that would setup the app before `Application.Run` is called`).
			// With a Scenario, after UI Catalog calls `Scenario.Setup` it calls
			// `Scenario.Run` which calls `Application.Run`. Example:

			var button = new Button ("Press me!") {
				AutoSize = false,
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			Application.Top.Add (button);

		}
	}
}