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
			Application.Resized = (e) => {
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

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_False_Left_And_Top_Is_Always_Zero (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Assert.False (Application.EnableConsoleScrolling);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			driver.SetWindowPosition (5, 5);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Left_Cannot_Be_Greater_Than_WindowWidth (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (81, 25);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Left_Cannot_Be_Greater_Than_BufferWidth_Minus_WindowWidth (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (81, 25);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			// MockDriver will now be sets to 120x25
			driver.SetBufferSize (120, 25);
			Assert.Equal (120, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);
			Assert.Equal (120, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);
			Assert.Equal (80, Console.WindowWidth);
			Assert.Equal (25, Console.WindowHeight);
			driver.SetWindowPosition (121, 25);
			Assert.Equal (40, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			driver.SetWindowSize (90, 25);
			Assert.Equal (120, Application.Driver.Cols);
			Assert.Equal (25, Application.Driver.Rows);
			Assert.Equal (120, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);
			Assert.Equal (90, Console.WindowWidth);
			Assert.Equal (25, Console.WindowHeight);
			driver.SetWindowPosition (121, 25);
			Assert.Equal (30, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Top_Cannot_Be_Greater_Than_WindowHeight (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (80, 26);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void EnableConsoleScrolling_Is_True_Top_Cannot_Be_Greater_Than_BufferHeight_Minus_WindowHeight (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Application.EnableConsoleScrolling = true;
			Assert.True (Application.EnableConsoleScrolling);

			driver.SetWindowPosition (80, 26);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			// MockDriver will now be sets to 80x40
			driver.SetBufferSize (80, 40);
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (40, Application.Driver.Rows);
			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (40, Console.BufferHeight);
			Assert.Equal (80, Console.WindowWidth);
			Assert.Equal (25, Console.WindowHeight);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);
			driver.SetWindowPosition (80, 40);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (15, Console.WindowTop);

			driver.SetWindowSize (80, 20);
			Assert.Equal (80, Application.Driver.Cols);
			Assert.Equal (40, Application.Driver.Rows);
			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (40, Console.BufferHeight);
			Assert.Equal (80, Console.WindowWidth);
			Assert.Equal (20, Console.WindowHeight);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (15, Console.WindowTop);
			driver.SetWindowPosition (80, 41);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (20, Console.WindowTop);

			Application.Shutdown ();
		}

		[Fact, AutoInitShutdown]
		public void AddRune_On_Clip_Left_Or_Right_Replace_Previous_Or_Next_Wide_Rune_With_Space ()
		{
			var tv = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = @"これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。
これは広いルーンラインです。"
			};
			var win = new Window ("ワイドルーン") { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (tv);
			Application.Top.Add (win);
			var lbl = new Label ("ワイドルーン。");
			var dg = new Dialog ("テスト", 14, 4, new Button ("選ぶ"));
			dg.Add (lbl);
			Application.Begin (Application.Top);
			Application.Begin (dg);
			((FakeDriver)Application.Driver).SetBufferSize (30, 10);

			var expected = @"
┌ ワイドルーン ──────────────┐
│これは広いルーンラインです。│
│これは広いルーンラインです。│
│これは ┌ テスト ────┐ です。│
│これは │ワイドルーン│ です。│
│これは │  [ 選ぶ ]  │ です。│
│これは └────────────┘ です。│
│これは広いルーンラインです。│
│これは広いルーンラインです。│
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 10), pos);
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
					var dlg = new Dialog ("Test", new Button ("Ok"));
					dlg.Add (lbl);
					Application.Begin (dlg);

					var expected = @"
┌──────────────────┐
│┌ Test ─────────┐ │
││  Hello World  │ │
││               │ │
││               │ │
││    [ Ok ]     │ │
│└───────────────┘ │
└──────────────────┘
";

					var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
					Assert.Equal (new Rect (0, 0, 20, 8), pos);

					Assert.True (dlg.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
					dlg.Redraw (dlg.Bounds);

					expected = @"
┌──────────────────┐
│┌ Test ─────────┐ │
││  Hello World  │ │
││               │ │
││               │ │
││    [ Ok ]     │ │
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

		[Theory]
		[InlineData (0x0000001F, 0x241F)]
		[InlineData (0x0000007F, 0x247F)]
		[InlineData (0x0000009F, 0x249F)]
		[InlineData (0x0001001A, 0x1001A)]
		public void MakePrintable_Converts_Control_Chars_To_Proper_Unicode (uint code, uint expected)
		{
			var actual = ConsoleDriver.MakePrintable (code);

			Assert.Equal (expected, actual.Value);
		}

		[Theory]
		[InlineData (0x20)]
		[InlineData (0x7E)]
		[InlineData (0xA0)]
		[InlineData (0x010020)]
		public void MakePrintable_Does_Not_Convert_Ansi_Chars_To_Unicode (uint code)
		{
			var actual = ConsoleDriver.MakePrintable (code);

			Assert.Equal (code, actual.Value);
		}

		private static object packetLock = new object ();

		/// <summary>
		/// Sometimes when using remote tools EventKeyRecord sends 'virtual keystrokes'.
		/// These are indicated with the wVirtualKeyCode of 231. When we see this code
		/// then we need to look to the unicode character (UnicodeChar) instead of the key
		/// when telling the rest of the framework what button was pressed. For full details
		/// see: https://github.com/gui-cs/Terminal.Gui/issues/2008
		/// </summary>
		[Theory, AutoInitShutdown]
		[ClassData (typeof (PacketTest))]
		public void TestVKPacket (uint unicodeCharacter, bool shift, bool alt, bool control, uint initialVirtualKey, uint initialScanCode, Key expectedRemapping, uint expectedVirtualKey, uint expectedScanCode)
		{
			var modifiers = new ConsoleModifiers ();
			if (shift) modifiers |= ConsoleModifiers.Shift;
			if (alt) modifiers |= ConsoleModifiers.Alt;
			if (control) modifiers |= ConsoleModifiers.Control;
			var mappedConsoleKey = ConsoleKeyMapping.GetConsoleKeyFromKey (unicodeCharacter, modifiers, out uint scanCode, out uint outputChar);

			if ((scanCode > 0 || mappedConsoleKey == 0) && mappedConsoleKey == initialVirtualKey) Assert.Equal (mappedConsoleKey, initialVirtualKey);
			else Assert.Equal (mappedConsoleKey, outputChar < 0xff ? outputChar & 0xff | 0xff << 8 : outputChar);
			Assert.Equal (scanCode, initialScanCode);

			var keyChar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (mappedConsoleKey, modifiers, out uint consoleKey, out scanCode);

			//if (scanCode > 0 && consoleKey == keyChar && consoleKey > 48 && consoleKey > 57 && consoleKey < 65 && consoleKey > 91) {
			if (scanCode > 0 && keyChar == 0 && consoleKey == mappedConsoleKey) Assert.Equal (0, (double)keyChar);
			else Assert.Equal (keyChar, unicodeCharacter);
			Assert.Equal (consoleKey, expectedVirtualKey);
			Assert.Equal (scanCode, expectedScanCode);

			var top = Application.Top;

			top.KeyPress += (s, e) => {
				var after = ShortcutHelper.GetModifiersKey (e.KeyEvent);
				Assert.Equal (expectedRemapping, after);
				e.Handled = true;
				Application.RequestStop ();
			};

			var iterations = -1;

			Application.Iteration += () => {
				iterations++;
				if (iterations == 0) Application.Driver.SendKeys ((char)mappedConsoleKey, ConsoleKey.Packet, shift, alt, control);
			};


			lock (packetLock) {
				Application.Run ();
				Application.Shutdown ();
			}
		}

		public class PacketTest : IEnumerable, IEnumerable<object []> {
			public IEnumerator<object []> GetEnumerator ()
			{
				lock (packetLock) {
					yield return new object [] { 'a', false, false, false, 'A', 30, Key.a, 'A', 30 };
					yield return new object [] { 'A', true, false, false, 'A', 30, Key.A | Key.ShiftMask, 'A', 30 };
					yield return new object [] { 'A', true, true, false, 'A', 30, Key.A | Key.ShiftMask | Key.AltMask, 'A', 30 };
					yield return new object [] { 'A', true, true, true, 'A', 30, Key.A | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 'A', 30 };
					yield return new object [] { 'z', false, false, false, 'Z', 44, Key.z, 'Z', 44 };
					yield return new object [] { 'Z', true, false, false, 'Z', 44, Key.Z | Key.ShiftMask, 'Z', 44 };
					yield return new object [] { 'Z', true, true, false, 'Z', 44, Key.Z | Key.ShiftMask | Key.AltMask, 'Z', 44 };
					yield return new object [] { 'Z', true, true, true, 'Z', 44, Key.Z | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 'Z', 44 };
					yield return new object [] { '英', false, false, false, '\0', 0, (Key)'英', '\0', 0 };
					yield return new object [] { '英', true, false, false, '\0', 0, (Key)'英' | Key.ShiftMask, '\0', 0 };
					yield return new object [] { '英', true, true, false, '\0', 0, (Key)'英' | Key.ShiftMask | Key.AltMask, '\0', 0 };
					yield return new object [] { '英', true, true, true, '\0', 0, (Key)'英' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '\0', 0 };
					yield return new object [] { '+', false, false, false, 187, 26, (Key)'+', 187, 26 };
					yield return new object [] { '*', true, false, false, 187, 26, (Key)'*' | Key.ShiftMask, 187, 26 };
					yield return new object [] { '+', true, true, false, 187, 26, (Key)'+' | Key.ShiftMask | Key.AltMask, 187, 26 };
					yield return new object [] { '+', true, true, true, 187, 26, (Key)'+' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 187, 26 };
					yield return new object [] { '1', false, false, false, '1', 2, Key.D1, '1', 2 };
					yield return new object [] { '!', true, false, false, '1', 2, (Key)'!' | Key.ShiftMask, '1', 2 };
					yield return new object [] { '1', true, true, false, '1', 2, Key.D1 | Key.ShiftMask | Key.AltMask, '1', 2 };
					yield return new object [] { '1', true, true, true, '1', 2, Key.D1 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '1', 2 };
					yield return new object [] { '1', false, true, true, '1', 2, Key.D1 | Key.AltMask | Key.CtrlMask, '1', 2 };
					yield return new object [] { '2', false, false, false, '2', 3, Key.D2, '2', 3 };
					yield return new object [] { '"', true, false, false, '2', 3, (Key)'"' | Key.ShiftMask, '2', 3 };
					yield return new object [] { '2', true, true, false, '2', 3, Key.D2 | Key.ShiftMask | Key.AltMask, '2', 3 };
					yield return new object [] { '2', true, true, true, '2', 3, Key.D2 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '2', 3 };
					yield return new object [] { '@', false, true, true, '2', 3, (Key)'@' | Key.AltMask | Key.CtrlMask, '2', 3 };
					yield return new object [] { '3', false, false, false, '3', 4, Key.D3, '3', 4 };
					yield return new object [] { '#', true, false, false, '3', 4, (Key)'#' | Key.ShiftMask, '3', 4 };
					yield return new object [] { '3', true, true, false, '3', 4, Key.D3 | Key.ShiftMask | Key.AltMask, '3', 4 };
					yield return new object [] { '3', true, true, true, '3', 4, Key.D3 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '3', 4 };
					yield return new object [] { '£', false, true, true, '3', 4, (Key)'£' | Key.AltMask | Key.CtrlMask, '3', 4 };
					yield return new object [] { '4', false, false, false, '4', 5, Key.D4, '4', 5 };
					yield return new object [] { '$', true, false, false, '4', 5, (Key)'$' | Key.ShiftMask, '4', 5 };
					yield return new object [] { '4', true, true, false, '4', 5, Key.D4 | Key.ShiftMask | Key.AltMask, '4', 5 };
					yield return new object [] { '4', true, true, true, '4', 5, Key.D4 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '4', 5 };
					yield return new object [] { '§', false, true, true, '4', 5, (Key)'§' | Key.AltMask | Key.CtrlMask, '4', 5 };
					yield return new object [] { '5', false, false, false, '5', 6, Key.D5, '5', 6 };
					yield return new object [] { '%', true, false, false, '5', 6, (Key)'%' | Key.ShiftMask, '5', 6 };
					yield return new object [] { '5', true, true, false, '5', 6, Key.D5 | Key.ShiftMask | Key.AltMask, '5', 6 };
					yield return new object [] { '5', true, true, true, '5', 6, Key.D5 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '5', 6 };
					yield return new object [] { '€', false, true, true, '5', 6, (Key)'€' | Key.AltMask | Key.CtrlMask, '5', 6 };
					yield return new object [] { '6', false, false, false, '6', 7, Key.D6, '6', 7 };
					yield return new object [] { '&', true, false, false, '6', 7, (Key)'&' | Key.ShiftMask, '6', 7 };
					yield return new object [] { '6', true, true, false, '6', 7, Key.D6 | Key.ShiftMask | Key.AltMask, '6', 7 };
					yield return new object [] { '6', true, true, true, '6', 7, Key.D6 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '6', 7 };
					yield return new object [] { '6', false, true, true, '6', 7, Key.D6 | Key.AltMask | Key.CtrlMask, '6', 7 };
					yield return new object [] { '7', false, false, false, '7', 8, Key.D7, '7', 8 };
					yield return new object [] { '/', true, false, false, '7', 8, (Key)'/' | Key.ShiftMask, '7', 8 };
					yield return new object [] { '7', true, true, false, '7', 8, Key.D7 | Key.ShiftMask | Key.AltMask, '7', 8 };
					yield return new object [] { '7', true, true, true, '7', 8, Key.D7 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '7', 8 };
					yield return new object [] { '{', false, true, true, '7', 8, (Key)'{' | Key.AltMask | Key.CtrlMask, '7', 8 };
					yield return new object [] { '8', false, false, false, '8', 9, Key.D8, '8', 9 };
					yield return new object [] { '(', true, false, false, '8', 9, (Key)'(' | Key.ShiftMask, '8', 9 };
					yield return new object [] { '8', true, true, false, '8', 9, Key.D8 | Key.ShiftMask | Key.AltMask, '8', 9 };
					yield return new object [] { '8', true, true, true, '8', 9, Key.D8 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '8', 9 };
					yield return new object [] { '[', false, true, true, '8', 9, (Key)'[' | Key.AltMask | Key.CtrlMask, '8', 9 };
					yield return new object [] { '9', false, false, false, '9', 10, Key.D9, '9', 10 };
					yield return new object [] { ')', true, false, false, '9', 10, (Key)')' | Key.ShiftMask, '9', 10 };
					yield return new object [] { '9', true, true, false, '9', 10, Key.D9 | Key.ShiftMask | Key.AltMask, '9', 10 };
					yield return new object [] { '9', true, true, true, '9', 10, Key.D9 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '9', 10 };
					yield return new object [] { ']', false, true, true, '9', 10, (Key)']' | Key.AltMask | Key.CtrlMask, '9', 10 };
					yield return new object [] { '0', false, false, false, '0', 11, Key.D0, '0', 11 };
					yield return new object [] { '=', true, false, false, '0', 11, (Key)'=' | Key.ShiftMask, '0', 11 };
					yield return new object [] { '0', true, true, false, '0', 11, Key.D0 | Key.ShiftMask | Key.AltMask, '0', 11 };
					yield return new object [] { '0', true, true, true, '0', 11, Key.D0 | Key.ShiftMask | Key.AltMask | Key.CtrlMask, '0', 11 };
					yield return new object [] { '}', false, true, true, '0', 11, (Key)'}' | Key.AltMask | Key.CtrlMask, '0', 11 };
					yield return new object [] { '\'', false, false, false, 219, 12, (Key)'\'', 219, 12 };
					yield return new object [] { '?', true, false, false, 219, 12, (Key)'?' | Key.ShiftMask, 219, 12 };
					yield return new object [] { '\'', true, true, false, 219, 12, (Key)'\'' | Key.ShiftMask | Key.AltMask, 219, 12 };
					yield return new object [] { '\'', true, true, true, 219, 12, (Key)'\'' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 219, 12 };
					yield return new object [] { '«', false, false, false, 221, 13, (Key)'«', 221, 13 };
					yield return new object [] { '»', true, false, false, 221, 13, (Key)'»' | Key.ShiftMask, 221, 13 };
					yield return new object [] { '«', true, true, false, 221, 13, (Key)'«' | Key.ShiftMask | Key.AltMask, 221, 13 };
					yield return new object [] { '«', true, true, true, 221, 13, (Key)'«' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 221, 13 };
					yield return new object [] { 'á', false, false, false, 'á', 0, (Key)'á', 'A', 30 };
					yield return new object [] { 'Á', true, false, false, 'Á', 0, (Key)'Á' | Key.ShiftMask, 'A', 30 };
					yield return new object [] { 'à', false, false, false, 'à', 0, (Key)'à', 'A', 30 };
					yield return new object [] { 'À', true, false, false, 'À', 0, (Key)'À' | Key.ShiftMask, 'A', 30 };
					yield return new object [] { 'é', false, false, false, 'é', 0, (Key)'é', 'E', 18 };
					yield return new object [] { 'É', true, false, false, 'É', 0, (Key)'É' | Key.ShiftMask, 'E', 18 };
					yield return new object [] { 'è', false, false, false, 'è', 0, (Key)'è', 'E', 18 };
					yield return new object [] { 'È', true, false, false, 'È', 0, (Key)'È' | Key.ShiftMask, 'E', 18 };
					yield return new object [] { 'í', false, false, false, 'í', 0, (Key)'í', 'I', 23 };
					yield return new object [] { 'Í', true, false, false, 'Í', 0, (Key)'Í' | Key.ShiftMask, 'I', 23 };
					yield return new object [] { 'ì', false, false, false, 'ì', 0, (Key)'ì', 'I', 23 };
					yield return new object [] { 'Ì', true, false, false, 'Ì', 0, (Key)'Ì' | Key.ShiftMask, 'I', 23 };
					yield return new object [] { 'ó', false, false, false, 'ó', 0, (Key)'ó', 'O', 24 };
					yield return new object [] { 'Ó', true, false, false, 'Ó', 0, (Key)'Ó' | Key.ShiftMask, 'O', 24 };
					yield return new object [] { 'ò', false, false, false, 'Ó', 0, (Key)'ò', 'O', 24 };
					yield return new object [] { 'Ò', true, false, false, 'Ò', 0, (Key)'Ò' | Key.ShiftMask, 'O', 24 };
					yield return new object [] { 'ú', false, false, false, 'ú', 0, (Key)'ú', 'U', 22 };
					yield return new object [] { 'Ú', true, false, false, 'Ú', 0, (Key)'Ú' | Key.ShiftMask, 'U', 22 };
					yield return new object [] { 'ù', false, false, false, 'ù', 0, (Key)'ù', 'U', 22 };
					yield return new object [] { 'Ù', true, false, false, 'Ù', 0, (Key)'Ù' | Key.ShiftMask, 'U', 22 };
					yield return new object [] { 'ö', false, false, false, 'ó', 0, (Key)'ö', 'O', 24 };
					yield return new object [] { 'Ö', true, false, false, 'Ó', 0, (Key)'Ö' | Key.ShiftMask, 'O', 24 };
					yield return new object [] { '<', false, false, false, 226, 86, (Key)'<', 226, 86 };
					yield return new object [] { '>', true, false, false, 226, 86, (Key)'>' | Key.ShiftMask, 226, 86 };
					yield return new object [] { '<', true, true, false, 226, 86, (Key)'<' | Key.ShiftMask | Key.AltMask, 226, 86 };
					yield return new object [] { '<', true, true, true, 226, 86, (Key)'<' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 226, 86 };
					yield return new object [] { 'ç', false, false, false, 192, 39, (Key)'ç', 192, 39 };
					yield return new object [] { 'Ç', true, false, false, 192, 39, (Key)'Ç' | Key.ShiftMask, 192, 39 };
					yield return new object [] { 'ç', true, true, false, 192, 39, (Key)'ç' | Key.ShiftMask | Key.AltMask, 192, 39 };
					yield return new object [] { 'ç', true, true, true, 192, 39, (Key)'ç' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 192, 39 };
					yield return new object [] { '¨', false, true, true, 187, 26, (Key)'¨' | Key.AltMask | Key.CtrlMask, 187, 26 };
					yield return new object [] { (uint)Key.PageUp, false, false, false, 33, 73, Key.PageUp, 33, 73 };
					yield return new object [] { (uint)Key.PageUp, true, false, false, 33, 73, Key.PageUp | Key.ShiftMask, 33, 73 };
					yield return new object [] { (uint)Key.PageUp, true, true, false, 33, 73, Key.PageUp | Key.ShiftMask | Key.AltMask, 33, 73 };
					yield return new object [] { (uint)Key.PageUp, true, true, true, 33, 73, Key.PageUp | Key.ShiftMask | Key.AltMask | Key.CtrlMask, 33, 73 };
				}
			}

			IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
		}
	}
}
