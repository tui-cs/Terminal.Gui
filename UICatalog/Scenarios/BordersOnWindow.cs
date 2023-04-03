using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders on Window", Description: "Demonstrates Window borders manipulation.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class BordersOnWindow : Scenario {
		public override void Init ()
		{
			Application.Init ();

			var boc = new BordersOnContainers (
				$"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				"Window",
				new Window ());

			Application.Run (boc);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}