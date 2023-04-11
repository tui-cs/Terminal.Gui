using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class ButtonTests {
		readonly ITestOutputHelper output;

		public ButtonTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void Constructors_Defaults ()
		{
			var btn = new Button ();
			Assert.Equal (string.Empty, btn.Text);
			Application.Top.Add (btn);
			var rs = Application.Begin (Application.Top);

			Assert.Equal ("[  ]", btn.TextFormatter.Text);
			Assert.False (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 4, 1), btn.Frame);
			Assert.Equal (Key.Null, btn.HotKey);
			var expected = @"
[  ]
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Application.End (rs);
			
			btn = new Button ("ARGS", true) { Text = "Test" };
			Assert.Equal ("Test", btn.Text);
			Application.Top.Add (btn);
			rs = Application.Begin (Application.Top);

			Assert.Equal ("[◦ Test ◦]", btn.TextFormatter.Text);
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 10, 1), btn.Frame);
			Assert.Equal (Key.T, btn.HotKey);
			expected = @"
[◦ Test ◦]
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Application.End (rs);
			
			btn = new Button (3, 4, "Test", true);
			Assert.Equal ("Test", btn.Text);
			Application.Top.Add (btn);
			rs = Application.Begin (Application.Top);

			Assert.Equal ("[◦ Test ◦]", btn.TextFormatter.Text);
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (3, 4, 10, 1), btn.Frame);
			Assert.Equal (Key.T, btn.HotKey);
			expected = @"
   [◦ Test ◦]
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Application.End (rs);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += (s,e) => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.False (btn.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (clicked);
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
			clicked = false;
			Assert.False (btn.IsDefault);
			Assert.False (btn.ProcessColdKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (clicked);
			btn.IsDefault = true;
			Assert.True (btn.ProcessColdKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessColdKey (new KeyEvent (Key.AltMask | Key.T, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent ((Key)'t', new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space | btn.HotKey, new KeyModifiers ())));
			Assert.True (clicked);
			btn.Text = "Te_st";
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space | btn.HotKey, new KeyModifiers ())));
			Assert.True (clicked);
		}

		[Fact]
		[AutoInitShutdown]
		public void ChangeHotKey ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += (s,e) => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.False (btn.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (clicked);
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
			clicked = false;

			btn.HotKey = Key.E;
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.E | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
		}


		/// <summary>
		/// This test demonstrates how to change the activation key for Button
		/// as described in the README.md keyboard handling section
		/// </summary>
		[Fact]
		[AutoInitShutdown]
		public void KeyBindingExample ()
		{
			int pressed = 0;
			var btn = new Button ("Press Me");
			btn.Clicked += (s,e) => pressed++;

			// The Button class supports the Accept command
			Assert.Contains (Command.Accept, btn.GetSupportedCommands ());

			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			// default keybinding is Enter which results in keypress
			Application.Driver.SendKeys ('\n', ConsoleKey.Enter, false, false, false);
			Assert.Equal (1, pressed);

			// remove the default keybinding (Enter)
			btn.ClearKeybinding (Command.Accept);

			// After clearing the default keystroke the Enter button no longer does anything for the Button
			Application.Driver.SendKeys ('\n', ConsoleKey.Enter, false, false, false);
			Assert.Equal (1, pressed);

			// Set a new binding of b for the click (Accept) event
			btn.AddKeyBinding (Key.b, Command.Accept);

			// now pressing B should call the button click event
			Application.Driver.SendKeys ('b', ConsoleKey.B, false, false, false);
			Assert.Equal (2, pressed);
		}

		[Fact]
		public void TestAssignTextToButton ()
		{
			View b = new Button () { Text = "heya" };
			Assert.Equal ("heya", b.Text);
			Assert.True (b.TextFormatter.Text.Contains ("heya"));
			b.Text = "heyb";
			Assert.Equal ("heyb", b.Text);
			Assert.True (b.TextFormatter.Text.Contains ("heyb"));

			// with cast
			Assert.Equal ("heyb", ((Button)b).Text);
		}

		[Fact]
		public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
		{
			var super = new View ();
			var btn = new Button ("Test");
			super.Add (btn);
			super.BeginInit ();
			super.EndInit ();

			Assert.Equal ("Test", btn.Text);
			Assert.Equal (Key.T, btn.HotKey);

			btn.Text = string.Empty;
			Assert.Equal ("", btn.Text);
			Assert.Equal (Key.Null, btn.HotKey);

			btn.Text = "Te_st";
			Assert.Equal ("Te_st", btn.Text);
			Assert.Equal (Key.S, btn.HotKey);
		}

		[Fact, AutoInitShutdown]
		public void Update_Only_On_Or_After_Initialize ()
		{
			var btn = new Button ("Say Hello 你") {
				X = Pos.Center (),
				Y = Pos.Center ()
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.False (btn.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (btn.IsInitialized);
			Assert.Equal ("Say Hello 你", btn.Text);
			Assert.Equal ("[ Say Hello 你 ]", btn.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 16, 1), btn.Bounds);

			var expected = @"
┌────────────────────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void Update_Parameterless_Only_On_Or_After_Initialize ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你"
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.False (btn.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (btn.IsInitialized);
			Assert.Equal ("Say Hello 你", btn.Text);
			Assert.Equal ("[ Say Hello 你 ]", btn.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 16, 1), btn.Bounds);

			var expected = @"
┌────────────────────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_With_EmptyText ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				AutoSize = true
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			btn.Text = "Say Hello 你";

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌────────────────────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_Center ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你"
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌────────────────────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say Hello 你 changed";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @"
┌────────────────────────────┐
│                            │
│  [ Say Hello 你 changed ]  │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_AnchorEnd ()
		{
			var btn = new Button () {
				Y = Pos.Center (),
				Text = "Say Hello 你",
				AutoSize = true
			};
			btn.X = Pos.AnchorEnd () - Pos.Function (() => TextFormatter.GetTextWidth (btn.TextFormatter.Text));

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌────────────────────────────┐
│                            │
│            [ Say Hello 你 ]│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say Hello 你 changed";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @"
┌────────────────────────────┐
│                            │
│    [ Say Hello 你 changed ]│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_With_Fixed_Width ()
		{
			var tab = new View ();

			var lblWidth = 8;

			var label = new Label ("Find:") {
				Y = 1,
				Width = lblWidth,
				TextAlignment = TextAlignment.Right,
				AutoSize = false
			};
			tab.Add (label);

			var txtToFind = new TextField ("Testing buttons.") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			tab.Add (txtToFind);

			var btnFindNext = new Button ("Find _Next") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (label),
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered,
				IsDefault = true,
				AutoSize = false
			};
			tab.Add (btnFindNext);

			var btnFindPrevious = new Button ("Find _Previous") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindNext) + 1,
				Width = 20,
				Enabled = !txtToFind.Text.IsEmpty,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			};
			tab.Add (btnFindPrevious);

			var btnCancel = new Button ("Cancel") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindPrevious) + 2,
				Width = 20,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			};
			tab.Add (btnCancel);

			var ckbMatchCase = new CheckBox ("Match c_ase") {
				X = 0,
				Y = Pos.Top (txtToFind) + 2,
				Checked = true
			};
			tab.Add (ckbMatchCase);

			var ckbMatchWholeWord = new CheckBox ("Match _whole word") {
				X = 0,
				Y = Pos.Top (ckbMatchCase) + 1,
				Checked = false
			};
			tab.Add (ckbMatchWholeWord);

			var tabView = new TabView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			tabView.AddTab (new TabView.Tab ("Find", tab), true);

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			tab.Width = label.Width + txtToFind.Width + btnFindNext.Width + 2;
			tab.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

			win.Add (tabView);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (54, 11);

			Assert.Equal (new Rect (0, 0, 54, 11), win.Frame);
			Assert.Equal (new Rect (0, 0, 52, 9), tabView.Frame);
			Assert.Equal (new Rect (0, 0, 50, 7), tab.Frame);
			Assert.Equal (new Rect (0, 1, 8, 1), label.Frame);
			Assert.Equal (new Rect (9, 1, 20, 1), txtToFind.Frame);

			Assert.Equal (0, txtToFind.ScrollOffset);
			Assert.Equal (16, txtToFind.CursorPosition);

			Assert.Equal (new Rect (30, 1, 20, 1), btnFindNext.Frame);
			Assert.Equal (new Rect (30, 2, 20, 1), btnFindPrevious.Frame);
			Assert.Equal (new Rect (30, 4, 20, 1), btnCancel.Frame);
			Assert.Equal (new Rect (0, 3, 12, 1), ckbMatchCase.Frame);
			Assert.Equal (new Rect (0, 4, 18, 1), ckbMatchWholeWord.Frame);
			var expected = @"
┌────────────────────────────────────────────────────┐
│┌────┐                                              │
││Find│                                              │
││    └─────────────────────────────────────────────┐│
││                                                  ││
││   Find: Testing buttons.       [◦ Find Next ◦]   ││
││                               [ Find Previous ]  ││
││√ Match case                                      ││
││╴ Match whole word                 [ Cancel ]     ││
│└──────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void Pos_Center_Layout_AutoSize_True ()
		{
			var button = new Button ("Process keys") {
				X = Pos.Center (),
				Y = Pos.Center (),
				IsDefault = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (button);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			Assert.True (button.AutoSize);
			Assert.Equal (new Rect (5, 1, 18, 1), button.Frame);
			var expected = @"
┌────────────────────────────┐
│                            │
│     [◦ Process keys ◦]     │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void Pos_Center_Layout_AutoSize_False ()
		{
			var button = new Button ("Process keys") {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = 20,
				IsDefault = true,
				AutoSize = false
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (button);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			Assert.False (button.AutoSize);
			Assert.Equal (new Rect (4, 1, 20, 1), button.Frame);
			var expected = @"
┌────────────────────────────┐
│                            │
│     [◦ Process keys ◦]     │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void Button_HotKeyChanged_EventFires ()
		{
			var btn = new Button ("Yar");

			object sender = null;
			KeyChangedEventArgs args = null;

			btn.HotKeyChanged += (s, e) =>{
				sender = s;
				args = e;

			};
			
			btn.HotKey = Key.r;
			Assert.Same (btn, sender);
			Assert.Equal (Key.Y, args.OldKey);
			Assert.Equal (Key.r, args.NewKey);

		}
		[Fact, AutoInitShutdown]
		public void Button_HotKeyChanged_EventFires_WithNone ()
		{
			var btn = new Button ();

			object sender = null;
			KeyChangedEventArgs args = null;

			btn.HotKeyChanged += (s, e) => {
				sender = s;
				args = e;

			};

			btn.HotKey = Key.r;
			Assert.Same (btn, sender);
			Assert.Equal (Key.Null, args.OldKey);
			Assert.Equal (Key.r, args.NewKey);

		}
	}
}
