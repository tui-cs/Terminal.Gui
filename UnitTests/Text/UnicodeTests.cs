using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.TextTests;
public class UnicodeTests {
	readonly ITestOutputHelper _output;

	public UnicodeTests (ITestOutputHelper output)
	{
		this._output = output;
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

		var expected = @$"
┌────────────────────────────┐
│これは広いルーンラインです。│
│これは広いルーンラインです。│
│これは ┌────────────┐ です。│
│これは │ワイドルーン│ です。│
│これは │  {CM.Glyphs.LeftBracket} 選ぶ {CM.Glyphs.RightBracket}  │ です。│
│これは └────────────┘ です。│
│これは広いルーンラインです。│
│これは広いルーンラインです。│
└────────────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 30, 10), pos);
	}
}
