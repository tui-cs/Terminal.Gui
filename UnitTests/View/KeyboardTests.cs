using System;
using Xunit;
using Xunit.Abstractions;
using NStack;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class KeyboardTests {
		readonly ITestOutputHelper output;

		public KeyboardTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void KeyPress_Handled_To_True_Prevents_Changes ()
		{
			Application.Init (new FakeDriver ());

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('N', ConsoleKey.N, false, false, false));

			var top = Application.Top;

			var text = new TextField ("");
			text.KeyPress += (s, e) => {
				e.Handled = true;
				Assert.True (e.Handled);
				Assert.Equal (Key.N, e.KeyEvent.Key);
			};
			top.Add (text);

			Application.Iteration += () => {
				Console.MockKeyPresses.Push (new ConsoleKeyInfo ('N', ConsoleKey.N, false, false, false));
				Assert.Equal ("", text.Text);

				Application.RequestStop ();
			};

			Application.Run ();

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		
		[Fact, AutoInitShutdown]
		public void KeyDown_And_KeyUp_Events_Must_Called_Before_OnKeyDown_And_OnKeyUp ()
		{
			var keyDown = false;
			var keyPress = false;
			var keyUp = false;

			var view = new DerivedView ();
			view.KeyDown += (s, e) => {
				Assert.Equal (Key.a, e.KeyEvent.Key);
				Assert.False (keyDown);
				Assert.False (view.IsKeyDown);
				e.Handled = true;
				keyDown = true;
			};
			view.KeyPress += (s, e) => {
				Assert.Equal (Key.a, e.KeyEvent.Key);
				Assert.False (keyPress);
				Assert.False (view.IsKeyPress);
				e.Handled = true;
				keyPress = true;
			};
			view.KeyUp += (s, e) => {
				Assert.Equal (Key.a, e.KeyEvent.Key);
				Assert.False (keyUp);
				Assert.False (view.IsKeyUp);
				e.Handled = true;
				keyUp = true;
			};

			Application.Top.Add (view);

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));

			Application.Iteration += () => Application.RequestStop ();

			Assert.True (view.CanFocus);

			Application.Run ();
			Application.Shutdown ();

			Assert.True (keyDown);
			Assert.True (keyPress);
			Assert.True (keyUp);
			Assert.False (view.IsKeyDown);
			Assert.False (view.IsKeyPress);
			Assert.False (view.IsKeyUp);
		}

		public class DerivedView : View {
			public DerivedView ()
			{
				CanFocus = true;
			}

			public bool IsKeyDown { get; set; }
			public bool IsKeyPress { get; set; }
			public bool IsKeyUp { get; set; }
			public override ustring Text { get; set; }

			public override bool OnKeyDown (KeyEvent keyEvent)
			{
				IsKeyDown = true;
				return true;
			}

			public override bool ProcessKey (KeyEvent keyEvent)
			{
				IsKeyPress = true;
				return true;
			}

			public override bool OnKeyUp (KeyEvent keyEvent)
			{
				IsKeyUp = true;
				return true;
			}
		}

		[Theory, AutoInitShutdown]
		[InlineData (true, false, false)]
		[InlineData (true, true, false)]
		[InlineData (true, true, true)]
		public void KeyDown_And_KeyUp_Events_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
		{
			var keyDown = false;
			var keyPress = false;
			var keyUp = false;

			var view = new DerivedView ();
			view.KeyDown += (s, e) => {
				Assert.Equal (-1, e.KeyEvent.KeyValue);
				Assert.Equal (shift, e.KeyEvent.IsShift);
				Assert.Equal (alt, e.KeyEvent.IsAlt);
				Assert.Equal (control, e.KeyEvent.IsCtrl);
				Assert.False (keyDown);
				Assert.False (view.IsKeyDown);
				keyDown = true;
			};
			view.KeyPress += (s, e) => {
				keyPress = true;
			};
			view.KeyUp += (s, e) => {
				Assert.Equal (-1, e.KeyEvent.KeyValue);
				Assert.Equal (shift, e.KeyEvent.IsShift);
				Assert.Equal (alt, e.KeyEvent.IsAlt);
				Assert.Equal (control, e.KeyEvent.IsCtrl);
				Assert.False (keyUp);
				Assert.False (view.IsKeyUp);
				keyUp = true;
			};

			Application.Top.Add (view);

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('\0', (ConsoleKey)'\0', shift, alt, control));

			Application.Iteration += () => Application.RequestStop ();

			Assert.True (view.CanFocus);

			Application.Run ();
			Application.Shutdown ();

			Assert.True (keyDown);
			Assert.False (keyPress);
			Assert.True (keyUp);
			Assert.True (view.IsKeyDown);
			Assert.False (view.IsKeyPress);
			Assert.True (view.IsKeyUp);
		}
	
	}
}
