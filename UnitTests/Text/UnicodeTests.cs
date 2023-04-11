using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.TextTests {
	public class UnicodeTests {
		readonly ITestOutputHelper output;

		public UnicodeTests (ITestOutputHelper output)
		{
			this.output = output;
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
			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (tv);
			Application.Top.Add (win);
			var lbl = new Label ("ワイドルーン。");
			var dg = new Dialog (new Button ("選ぶ")) { Width = 14, Height = 4 };
			dg.Add (lbl);
			Application.Begin (Application.Top);
			Application.Begin (dg);
			((FakeDriver)Application.Driver).SetBufferSize (30, 10);

			var expected = @"
┌────────────────────────────┐
│これは広いルーンラインです。│
│これは広いルーンラインです。│
│これは ┌────────────┐ です。│
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
	}
}