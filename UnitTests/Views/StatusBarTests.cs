using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class StatusBarTests {
		readonly ITestOutputHelper output;

		public StatusBarTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void StatusItem_Constructor ()
		{
			var si = new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", null);
			Assert.Equal (Key.CtrlMask | Key.Q, si.Shortcut);
			Assert.Equal ($"{Application.QuitKey} to Quit", si.Title);
			Assert.Null (si.Action);
			si = new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", () => { });
			Assert.NotNull (si.Action);
		}

		[Fact]
		public void StatusBar_Contructor_Default ()
		{
			var sb = new StatusBar ();

			Assert.Empty (sb.Items);
			Assert.False (sb.CanFocus);
			Assert.Equal (Colors.Menu, sb.ColorScheme);
			Assert.Equal (0, sb.X);
			Assert.Equal ("AnchorEnd(1)", sb.Y.ToString ());
			Assert.Equal (Dim.Fill (), sb.Width);
			Assert.Equal (1, sb.Height);

			var driver = new FakeDriver ();
			Application.Init (driver);

			sb = new StatusBar ();

			driver.SetCursorVisibility (CursorVisibility.Default);
			driver.GetCursorVisibility (out CursorVisibility cv);
			Assert.Equal (CursorVisibility.Default, cv);
			Assert.True (FakeConsole.CursorVisible);

			Application.Iteration += () => {
				Assert.Equal (24, sb.Frame.Y);

				driver.SetWindowSize (driver.Cols, 15);

				Assert.Equal (14, sb.Frame.Y);

				sb.OnEnter (null);
				driver.GetCursorVisibility (out cv);
				Assert.Equal (CursorVisibility.Invisible, cv);
				Assert.False (FakeConsole.CursorVisible);

				Application.RequestStop ();
			};

			Application.Top.Add (sb);

			Application.Run ();

			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void Run_Action_With_Key_And_Mouse ()
		{
			var msg = "";
			var sb = new StatusBar (new StatusItem [] { new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", () => msg = "Quiting...") });
			Application.Top.Add (sb);

			var iteration = 0;

			Application.Iteration += () => {
				if (iteration == 0) {
					Assert.Equal ("", msg);
					sb.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.Q, null));
				} else if (iteration == 1) {
					Assert.Equal ("Quiting...", msg);
					msg = "";
					sb.MouseEvent (new MouseEvent () { X = 1, Y = 24, Flags = MouseFlags.Button1Clicked });
				} else {
					Assert.Equal ("Quiting...", msg);

					Application.RequestStop ();
				}
				iteration++;
			};

			Application.Run ();
		}

		[Fact]
		[AutoInitShutdown]
		public void Redraw_Output ()
		{
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (Key.CtrlMask | Key.O, "~^O~ Open", null),
				new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit!", null)
			});
			Application.Top.Add (sb);

			sb.OnDrawContent (sb.Bounds);

			string expected = @$"
^O Open {CM.Glyphs.VLine} Q, CtrlMask to Quit!
";
			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact]
		[AutoInitShutdown]
		public void Redraw_Output_CTRLQ ()
		{
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (Key.CtrlMask | Key.O, "~CTRL-O~ Open", null),
				new StatusItem (Key.CtrlMask | Key.Q, "~CTRL-Q~ Quit", null)
			});
			Application.Top.Add (sb);
			sb.OnDrawContent (sb.Bounds);

			string expected = @$"
CTRL-O Open {CM.Glyphs.VLine} CTRL-Q Quit
";

			TestHelpers.AssertDriverContentsAre (expected, output);
		}

		[Fact]
		public void AddItemAt_RemoveItem_Replacing ()
		{
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (Key.CtrlMask | Key.Q, "~^O~ Open", null),
				new StatusItem (Key.CtrlMask | Key.Q, "~^S~ Save", null),
				new StatusItem (Key.CtrlMask | Key.Q, "~^Q~ Quit", null)
			});

			sb.AddItemAt (2, new StatusItem (Key.CtrlMask | Key.Q, "~^C~ Close", null));

			Assert.Equal ("~^O~ Open", sb.Items [0].Title);
			Assert.Equal ("~^S~ Save", sb.Items [1].Title);
			Assert.Equal ("~^C~ Close", sb.Items [2].Title);
			Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);

			Assert.Equal ("~^S~ Save", sb.RemoveItem (1).Title);

			Assert.Equal ("~^O~ Open", sb.Items [0].Title);
			Assert.Equal ("~^C~ Close", sb.Items [1].Title);
			Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);

			sb.Items [1] = new StatusItem (Key.CtrlMask | Key.A, "~^A~ Save As", null);

			Assert.Equal ("~^O~ Open", sb.Items [0].Title);
			Assert.Equal ("~^A~ Save As", sb.Items [1].Title);
			Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);
		}
	}
}
