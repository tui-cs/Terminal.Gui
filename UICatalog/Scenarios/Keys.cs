using System.Text;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Keys", Description: "Shows how to handle keyboard input")]
	[ScenarioCategory ("Mouse and Keyboard")]
	public class Keys : Scenario {

		class TestWindow : Window {
			public List<string> _processKeyList = new List<string> ();
			public List<string> _processHotKeyList = new List<string> ();
			public List<string> _processColdKeyList = new List<string> ();

			public override bool ProcessKey (KeyEvent keyEvent)
			{
				_processKeyList.Add (keyEvent.ToString ());
				return base.ProcessKey (keyEvent);
			}

			public override bool ProcessHotKey (KeyEvent keyEvent)
			{
				_processHotKeyList.Add (keyEvent.ToString ());
				return base.ProcessHotKey (keyEvent);
			}

			public override bool ProcessColdKey (KeyEvent keyEvent)
			{
				_processColdKeyList.Add (keyEvent.ToString ());

				return base.ProcessColdKey (keyEvent);
			}
		}

		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			
			Win = new TestWindow () {
				Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = Colors.ColorSchemes [TopLevelColorScheme],
			};
			Application.Top.Add (Win);
		}

		public override void Setup ()
		{
			// Type text here: ______
			var editLabel = new Label ("Type text here:") {
				X = 0,
				Y = 0,
			};
			Win.Add (editLabel);
			var edit = new TextField ("") {
				X = Pos.Right (editLabel) + 1,
				Y = Pos.Top (editLabel),
				Width = Dim.Fill (2),
			};
			Win.Add (edit);

			// Last KeyPress: ______
			var keyPressedLabel = new Label ("Last KeyPress:") {
				X = Pos.Left (editLabel),
				Y = Pos.Top (editLabel) + 2,
			};
			Win.Add (keyPressedLabel);
			var labelKeypress = new Label ("") {
				X = Pos.Left (edit),
				Y = Pos.Top (keyPressedLabel),
				TextAlignment = Terminal.Gui.TextAlignment.Centered,
				ColorScheme = Colors.Error,
				AutoSize = true
			};
			Win.Add (labelKeypress);

			Win.KeyPress += (s,e) => labelKeypress.Text = e.KeyEvent.ToString ();

			// Key stroke log:
			var keyLogLabel = new Label ("Key stroke log:") {
				X = Pos.Left (editLabel),
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (keyLogLabel);
			var fakeKeyPress = new KeyEvent (Key.CtrlMask | Key.A, new KeyModifiers () {
				Alt = true,
				Ctrl = true,
				Shift = true
			});
			var maxLogEntry = $"Key{"",-5}: {fakeKeyPress}".Length;
			var yOffset = (Application.Top == Application.Top ? 1 : 6);
			var keyStrokelist = new List<string> ();
			var keyStrokeListView = new ListView (keyStrokelist) {
				X = 0,
				Y = Pos.Top (keyLogLabel) + yOffset,
				Width = Dim.Percent (30),
				Height = Dim.Fill (),
			};
			keyStrokeListView.ColorScheme = Colors.TopLevel;
			Win.Add (keyStrokeListView);

			// ProcessKey log:
			var processKeyLogLabel = new Label ("ProcessKey log:") {
				X = Pos.Right (keyStrokeListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processKeyLogLabel);

			maxLogEntry = $"{fakeKeyPress}".Length;
			yOffset = (Application.Top == Application.Top ? 1 : 6);
			var processKeyListView = new ListView (((TestWindow)Win)._processKeyList) {
				X = Pos.Left (processKeyLogLabel),
				Y = Pos.Top (processKeyLogLabel) + yOffset,
				Width = Dim.Percent(30),
				Height = Dim.Fill (),
			};
			processKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processKeyListView);

			// ProcessHotKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processHotKeyLogLabel = new Label ("ProcessHotKey log:") {
				X = Pos.Right (processKeyListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processHotKeyLogLabel);

			yOffset = (Application.Top == Application.Top ? 1 : 6);
			var processHotKeyListView = new ListView (((TestWindow)Win)._processHotKeyList) {
				X = Pos.Left (processHotKeyLogLabel),
				Y = Pos.Top (processHotKeyLogLabel) + yOffset,
				Width = Dim.Percent (20),
				Height = Dim.Fill (),
			};
			processHotKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processHotKeyListView);

			// ProcessColdKey log:
			// BUGBUG: Label is not positioning right with Pos, so using TextField instead
			var processColdKeyLogLabel = new Label ("ProcessColdKey log:") {
				X = Pos.Right (processHotKeyListView) + 1,
				Y = Pos.Top (editLabel) + 4,
			};
			Win.Add (processColdKeyLogLabel);

			yOffset = (Application.Top == Application.Top ? 1 : 6);
			var processColdKeyListView = new ListView (((TestWindow)Win)._processColdKeyList) {
				X = Pos.Left (processColdKeyLogLabel),
				Y = Pos.Top (processColdKeyLogLabel) + yOffset,
				Width = Dim.Percent (20),
				Height = Dim.Fill (),
			};

			Win.KeyDown += (s,a) => KeyDownPressUp (a.KeyEvent, "Down");
			Win.KeyPress += (s, a) => KeyDownPressUp (a.KeyEvent, "Press");
			Win.KeyUp += (s, a) => KeyDownPressUp (a.KeyEvent, "Up");

			void KeyDownPressUp (KeyEvent keyEvent, string updown)
			{
				var msg = $"Key{updown,-5}: {keyEvent}";
				keyStrokelist.Add (msg);
				keyStrokeListView.MoveDown ();
				processKeyListView.MoveDown ();
				processColdKeyListView.MoveDown ();
				processHotKeyListView.MoveDown ();
			}

			processColdKeyListView.ColorScheme = Colors.TopLevel;
			Win.Add (processColdKeyListView);
		}
	}
}