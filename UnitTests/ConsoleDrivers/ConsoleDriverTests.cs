using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class ConsoleDriverTests {
		readonly ITestOutputHelper output;

		public ConsoleDriverTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		//[InlineData (typeof (NetDriver))]
		//[InlineData (typeof (CursesDriver))]
		//[InlineData (typeof (WindowsDriver))]
		public void Init_Inits (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);
			driver.Init (() => { });

			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);

			// MockDriver is always 80x25
			Assert.Equal (Console.BufferWidth, driver.Cols);
			Assert.Equal (Console.BufferHeight, driver.Rows);
			driver.End ();

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		//[InlineData (typeof (NetDriver))]
		//[InlineData (typeof (CursesDriver))]
		//[InlineData (typeof (WindowsDriver))]
		public void End_Cleans_Up (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);
			driver.Init (() => { });

			Console.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

			Console.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);
			driver.Move (2, 3);
			Assert.Equal (2, Console.CursorLeft);
			Assert.Equal (3, Console.CursorTop);

			driver.End ();
			Assert.Equal (0, Console.CursorLeft);
			Assert.Equal (0, Console.CursorTop);
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void FakeDriver_Only_Sends_Keystrokes_Through_MockKeyPresses (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			var top = Application.Top;
			var view = new View ();
			var count = 0;
			var wasKeyPressed = false;

			view.KeyPress += (s, e) => {
				wasKeyPressed = true;
			};
			top.Add (view);

			Application.Iteration += () => {
				count++;
				if (count == 10) Application.RequestStop ();
			};

			Application.Run ();

			Assert.False (wasKeyPressed);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void FakeDriver_MockKeyPresses (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			var text = "MockKeyPresses";
			var mKeys = new Stack<ConsoleKeyInfo> ();
			foreach (var r in text.Reverse ()) {
				var ck = char.IsLetter (r) ? (ConsoleKey)char.ToUpper (r) : (ConsoleKey)r;
				var cki = new ConsoleKeyInfo (r, ck, false, false, false);
				mKeys.Push (cki);
			}
			Console.MockKeyPresses = mKeys;

			var top = Application.Top;
			var view = new View ();
			var rText = "";
			var idx = 0;

			view.KeyPress += (s, e) => {
				Assert.Equal (text [idx], (char)e.KeyEvent.Key);
				rText += (char)e.KeyEvent.Key;
				Assert.Equal (rText, text.Substring (0, idx + 1));
				e.Handled = true;
				idx++;
			};
			top.Add (view);

			Application.Iteration += () => {
				if (mKeys.Count == 0) Application.RequestStop ();
			};

			Application.Run ();

			Assert.Equal ("MockKeyPresses", rText);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		//[Theory]
		//[InlineData (typeof (FakeDriver))]
		//public void FakeDriver_MockKeyPresses_Press_AfterTimeOut (Type driverType)
		//{
		//	var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		//	Application.Init (driver);

		//	// Simulating pressing of QuitKey after a short period of time
		//	uint quitTime = 100;
		//	Func<MainLoop, bool> closeCallback = (MainLoop loop) => {
		//		// Prove the scenario is using Application.QuitKey correctly
		//		output.WriteLine ($"  {quitTime}ms elapsed; Simulating keypresses...");
		//		FakeConsole.PushMockKeyPress (Key.F);
		//		FakeConsole.PushMockKeyPress (Key.U);
		//		FakeConsole.PushMockKeyPress (Key.C);
		//		FakeConsole.PushMockKeyPress (Key.K);
		//		return false;
		//	};
		//	output.WriteLine ($"Add timeout to simulate key presses after {quitTime}ms");
		//	_ = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (quitTime), closeCallback);

		//	// If Top doesn't quit within abortTime * 5 (500ms), this will force it
		//	uint abortTime = quitTime * 5;
		//	Func<MainLoop, bool> forceCloseCallback = (MainLoop loop) => {
		//		Application.RequestStop ();
		//		Assert.Fail ($"  failed to Quit after {abortTime}ms. Force quit.");
		//		return false;
		//	};
		//	output.WriteLine ($"Add timeout to force quit after {abortTime}ms");
		//	_ = Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (abortTime), forceCloseCallback);

		//	Key key = Key.Unknown;
			
		//	Application.Top.KeyPress += (e) => {
		//		key = e.KeyEvent.Key;
		//		output.WriteLine ($"  Application.Top.KeyPress: {key}");
		//		e.Handled = true;
				
		//	};

		//	int iterations = 0;
		//	Application.Iteration += () => {
		//		output.WriteLine ($"  iteration {++iterations}");

		//		if (Console.MockKeyPresses.Count == 0) {
		//			output.WriteLine ($"    No more MockKeyPresses; RequestStop");
		//			Application.RequestStop ();
		//		}
		//	};

		//	Application.Run ();

		//	// Shutdown must be called to safely clean up Application if Init has been called
		//	Application.Shutdown ();
		//}
		
		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void TerminalResized_Simulation (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);
			var wasTerminalResized = false;
			Application.TerminalResized = (e) => {
				wasTerminalResized = true;
				Assert.Equal (120, e.Cols);
				Assert.Equal (40, e.Rows);
			};

			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);

			// MockDriver is by default 80x25
			Assert.Equal (Console.BufferWidth, driver.Cols);
			Assert.Equal (Console.BufferHeight, driver.Rows);
			Assert.False (wasTerminalResized);

			// MockDriver will now be sets to 120x40
			driver.SetBufferSize (120, 40);
			Assert.Equal (120, Application.Driver.Cols);
			Assert.Equal (40, Application.Driver.Rows);
			Assert.True (wasTerminalResized);

			// MockDriver will still be 120x40
			wasTerminalResized = false;
			Application.EnableConsoleScrolling = true;
			driver.SetWindowSize (40, 20);
			Assert.Equal (120, Application.Driver.Cols);
			Assert.Equal (40, Application.Driver.Rows);
			Assert.Equal (120, Console.BufferWidth);
			Assert.Equal (40, Console.BufferHeight);
			Assert.Equal (40, Console.WindowWidth);
			Assert.Equal (20, Console.WindowHeight);
			Assert.True (wasTerminalResized);

			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void Write_Do_Not_Change_On_ProcessKey ()
		{
			var win = new Window ();
			Application.Begin (win);
			((FakeDriver)Application.Driver).SetBufferSize (20, 8);

			System.Threading.Tasks.Task.Run (() => {
				System.Threading.Tasks.Task.Delay (500).Wait ();
				Application.MainLoop.Invoke (() => {
					var lbl = new Label ("Hello World") { X = Pos.Center () };
					var dlg = new Dialog ();
					dlg.Add (lbl);
					Application.Begin (dlg);

					var expected = @"
┌──────────────────┐
│┌───────────────┐ │
││  Hello World  │ │
││               │ │
││               │ │
││               │ │
│└───────────────┘ │
└──────────────────┘
";

					var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
					Assert.Equal (new Rect (0, 0, 20, 8), pos);

					Assert.True (dlg.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
					dlg.Draw ();

					expected = @"
┌──────────────────┐
│┌───────────────┐ │
││  Hello World  │ │
││               │ │
││               │ │
││               │ │
│└───────────────┘ │
└──────────────────┘
";

					pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
					Assert.Equal (new Rect (0, 0, 20, 8), pos);

					win.RequestStop ();
				});
			});

			Application.Run (win);
			Application.Shutdown ();
		}
	}
}
