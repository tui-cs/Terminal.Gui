using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class LabelTests {
		readonly ITestOutputHelper output;

		public LabelTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void Constructors_Defaults ()
		{
			var label = new Label ();
			Assert.Equal (string.Empty, label.Text);
			Application.Top.Add (label);
			var rs = Application.Begin (Application.Top);
			
			Assert.Equal (TextAlignment.Left, label.TextAlignment);
			Assert.True (label.AutoSize);
			Assert.False (label.CanFocus);
			Assert.Equal (new Rect (0, 0, 0, 1), label.Frame);
			Assert.Equal (Key.Null, label.HotKey);
			var expected = @"";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Application.End (rs);
			
			label = new Label ("ARGS", true) { Text = "Test" };
			Assert.True (label.AutoSize);
			Assert.Equal ("Test", label.Text);
			Application.Top.Add (label);
			rs = Application.Begin (Application.Top);

			Assert.Equal ("Test", label.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 4, 1), label.Frame);
			expected = @"
Test
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Application.End (rs);
			
			label = new Label (3, 4, "Test", true);
			Assert.Equal ("Test", label.Text);
			Application.Top.Add (label);
			rs = Application.Begin (Application.Top);

			Assert.Equal ("Test", label.TextFormatter.Text);
			Assert.Equal (new Rect (3, 4, 4, 1), label.Frame);
			expected = @"
   Test
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Application.End (rs);
		}

		[Fact]
		public void TestAssignTextToLabel ()
		{
			View b = new Label () { Text = "heya" };
			Assert.Equal ("heya", b.Text);
			Assert.True (b.TextFormatter.Text.Contains ("heya"));
			b.Text = "heyb";
			Assert.Equal ("heyb", b.Text);
			Assert.True (b.TextFormatter.Text.Contains ("heyb"));

			// with cast
			Assert.Equal ("heyb", ((Label)b).Text);
		}


		[Fact, AutoInitShutdown]
		public void Update_Only_On_Or_After_Initialize ()
		{
			var label = new Label ("Say Hello 你") {
				X = Pos.Center (),
				Y = Pos.Center ()
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (label);
			Application.Top.Add (win);

			Assert.False (label.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (label.IsInitialized);
			Assert.Equal ("Say Hello 你", label.Text);
			Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 12, 1), label.Bounds);

			var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";
			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void Update_Parameterless_Only_On_Or_After_Initialize ()
		{
			var label = new Label () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你",
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (label);
			Application.Top.Add (win);

			Assert.False (label.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (label.IsInitialized);
			Assert.Equal ("Say Hello 你", label.Text);
			Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 12, 1), label.Bounds);

			var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_With_EmptyText ()
		{
			var label = new Label () {
				X = Pos.Center (),
				Y = Pos.Center (),
				AutoSize = true
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (label);
			Application.Top.Add (win);

			Assert.True (label.AutoSize);

			label.Text = "Say Hello 你";

			Assert.True (label.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_Center ()
		{
			var label = new Label () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你"
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (label);
			Application.Top.Add (win);

			Assert.True (label.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (label.AutoSize);
			label.Text = "Say Hello 你 changed";
			Assert.True (label.AutoSize);
			Application.Refresh ();
			expected = @"
┌────────────────────────────┐
│                            │
│    Say Hello 你 changed    │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_AnchorEnd ()
		{
			var label = new Label () {
				Y = Pos.Center (),
				Text = "Say Hello 你",
				AutoSize = true
			};
			label.X = Pos.AnchorEnd () - Pos.Function (() => TextFormatter.GetTextWidth (label.TextFormatter.Text));

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (label);
			Application.Top.Add (win);

			Assert.True (label.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌────────────────────────────┐
│                            │
│                Say Hello 你│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (label.AutoSize);
			label.Text = "Say Hello 你 changed";
			Assert.True (label.AutoSize);
			Application.Refresh ();
			expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你 changed│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		
		[Fact, AutoInitShutdown]
		public void Pos_Center_Layout_AutoSize_True ()
		{
			var Label = new Label ("012345678901") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (Label);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			Assert.True (Label.AutoSize);
			//Assert.Equal (new Rect (5, 1, 18, 1), Label.Frame);
			var expected = @"
┌────────────────────────────┐
│                            │
│        012345678901        │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void Pos_Center_Layout_AutoSize_False ()
		{                               
			var Label = new Label ("012345678901") {
				X = Pos.Center (),
				Y = Pos.Center (),
				AutoSize = false,
				Width = 20,
				TextAlignment = TextAlignment.Centered
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (Label);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			Assert.False (Label.AutoSize);
			Assert.Equal (new Rect (4, 1, 20, 1), Label.Frame);
			var expected = @"
┌────────────────────────────┐
│                            │
│        012345678901        │
│                            │
└────────────────────────────┘
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		
		//[Fact, AutoInitShutdown]
		//public void Label_HotKeyChanged_EventFires ()
		//{
		//	var label = new Label ("Yar");

		//	object sender = null;
		//	KeyChangedEventArgs args = null;

		//	label.HotKeyChanged += (s, e) =>{
		//		sender = s;
		//		args = e;

		//	};
			
		//	label.HotKey = Key.r;
		//	Assert.Same (label, sender);
		//	Assert.Equal (Key.Y, args.OldKey);
		//	Assert.Equal (Key.r, args.NewKey);

		//}
		
		[Fact, AutoInitShutdown]
		public void Label_HotKeyChanged_EventFires_WithNone ()
		{
			var label = new Label ();

			object sender = null;
			KeyChangedEventArgs args = null;

			label.HotKeyChanged += (s, e) => {
				sender = s;
				args = e;

			};

			label.HotKey = Key.r;
			Assert.Same (label, sender);
			Assert.Equal (Key.Null, args.OldKey);
			Assert.Equal (Key.r, args.NewKey);
		}


		[Fact, AutoInitShutdown]
		public void Label_WordWrap_PreserveTrailingSpaces_Horizontal_With_Simple_Runes ()
		{
			var text = "A sentence has words.";
			var width = 3;
			var height = 8;
			var wrappedLines = TextFormatter.WordWrapText (text, width, true);
			var breakLines = "";
			foreach (var line in wrappedLines) breakLines += $"{line}{Environment.NewLine}";
			var label = new Label (breakLines) { Width = Dim.Fill (), Height = Dim.Fill () };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (label);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, width, height + 1), label.Frame);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

			var expected = @"
┌───┐
│A  │
│sen│
│ten│
│ce │
│has│
│   │
│wor│
│ds.│
└───┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_WordWrap_PreserveTrailingSpaces_Vertical_With_Simple_Runes ()
		{
			var text = "A sentence has words.";
			var width = 8;
			var height = 3;
			var wrappedLines = TextFormatter.WordWrapText (text, height, true);
			var breakLines = "";
			for (int i = 0; i < wrappedLines.Count; i++) breakLines += $"{wrappedLines [i]}{(i < wrappedLines.Count - 1 ? Environment.NewLine : string.Empty)}";
			var label = new Label (breakLines) {
				TextDirection = TextDirection.TopBottom_LeftRight,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (label);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, width, height), label.Frame);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

			var expected = @"
┌────────┐
│Astch wd│
│ eeea os│
│ nn s r.│
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_WordWrap_PreserveTrailingSpaces_Horizontal_With_Wide_Runes ()
		{
			var text = "文に は言葉 があり ます。";
			var width = 6;
			var height = 8;
			var wrappedLines = TextFormatter.WordWrapText (text, width, true);
			var breakLines = "";
			foreach (var line in wrappedLines) breakLines += $"{line}{Environment.NewLine}";
			var label = new Label (breakLines) { Width = Dim.Fill (), Height = Dim.Fill () };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (label);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, width, height), label.Frame);
			Assert.Equal (new Size (width, height), label.TextFormatter.Size);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

			var expected = @"
┌──────┐
│文に  │
│は言葉│
│ があ │
│り ま │
│す。  │
│      │
│      │
│      │
└──────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_WordWrap_PreserveTrailingSpaces_Vertical_With_Wide_Runes ()
		{
			var text = "文に は言葉 があり ます。";
			var width = 8;
			var height = 4;
			var wrappedLines = TextFormatter.WordWrapText (text, width, true);
			var breakLines = "";
			for (int i = 0; i < wrappedLines.Count; i++) breakLines += $"{wrappedLines [i]}{(i < wrappedLines.Count - 1 ? Environment.NewLine : string.Empty)}";
			var label = new Label (breakLines) {
				TextDirection = TextDirection.TopBottom_LeftRight,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (label);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, width, height), label.Frame);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

			var expected = @"
┌────────┐
│文言あす│
│に葉り。│
│        │
│はがま  │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
		}


		[Fact, AutoInitShutdown]
		public void Label_Draw_Horizontal_Simple_TextAlignments_Justified ()
		{
			var text = "01234 01234";
			var width = 20;
			var lblJust = new Label (text) { Y = 0, Width = width, TextAlignment = TextAlignment.Justified };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (lblJust);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, 3);

			var expected = @"
┌────────────────────┐
│01234          01234│
└────────────────────┘
";
			Assert.Equal (new Rect (0, 0, width, 1), lblJust.Frame);

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, 3), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Horizontal_Simple_Runes ()
		{
			var label = new Label ("Demo Simple Rune");
			Application.Top.Add (label);
			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 16, 1), label.Frame);

			var expected = @"
Demo Simple Rune
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 16, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Vertical_Simple_Runes ()
		{
			var label = new Label ("Demo Simple Rune") {
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			Application.Top.Add (label);
			Application.Begin (Application.Top);

			Assert.NotNull (label.Width);
			Assert.NotNull (label.Height);

			var expected = @"
D
e
m
o
 
S
i
m
p
l
e
 
R
u
n
e
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 1, 16), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Horizontal_Wide_Runes ()
		{
			var label = new Label ("デモエムポンズ");
			Application.Top.Add (label);
			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 14, 1), label.Frame);

			var expected = @"
デモエムポンズ
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 14, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Vertical_Wide_Runes ()
		{
			var label = new Label ("デモエムポンズ") {
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			Application.Top.Add (label);
			Application.Begin (Application.Top);

			var expected = @"
デ
モ
エ
ム
ポ
ン
ズ
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 2, 7), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Vertical_Wide_Runes_With_ForceValidatePosDim ()
		{
			var label = new Label ("デモエムポンズ") {
				Width = Dim.Fill (),
				Height = Dim.Percent (50f),
				TextDirection = TextDirection.TopBottom_LeftRight,
				ForceValidatePosDim = true
			};
			Application.Top.Add (label);
			Application.Begin (Application.Top);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 80, 12), label.Frame);

			var expected = @"
デ
モ
エ
ム
ポ
ン
ズ
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 2, 7), pos);
		}


		[Fact, AutoInitShutdown]
		public void Label_Draw_Horizontal_Simple_TextAlignments ()
		{
			var text = "Hello World";
			var width = 20;
			var lblLeft = new Label (text) { Width = width };
			var lblCenter = new Label (text) { Y = 1, Width = width, TextAlignment = TextAlignment.Centered };
			var lblRight = new Label (text) { Y = 2, Width = width, TextAlignment = TextAlignment.Right };
			var lblJust = new Label (text) { Y = 3, Width = width, TextAlignment = TextAlignment.Justified };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (lblLeft, lblCenter, lblRight, lblJust);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

			Assert.True (lblLeft.AutoSize);
			Assert.True (lblCenter.AutoSize);
			Assert.True (lblRight.AutoSize);
			Assert.True (lblJust.AutoSize);
			Assert.Equal (new Rect (0, 0, width, 1), lblLeft.Frame);
			Assert.Equal (new Rect (0, 1, width, 1), lblCenter.Frame);
			Assert.Equal (new Rect (0, 2, width, 1), lblRight.Frame);
			Assert.Equal (new Rect (0, 3, width, 1), lblJust.Frame);
			Assert.Equal (new Rect (0, 0, width + 2, 6), frame.Frame);

			var expected = @"
┌────────────────────┐
│Hello World         │
│    Hello World     │
│         Hello World│
│Hello          World│
└────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, 6), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Vertical_Simple_TextAlignments ()
		{
			var text = "Hello World";
			var height = 20;
			var lblLeft = new Label (text, direction: TextDirection.TopBottom_LeftRight) { Height = height };
			var lblCenter = new Label (text, direction: TextDirection.TopBottom_LeftRight) { X = 2, Height = height, VerticalTextAlignment = VerticalTextAlignment.Middle };
			var lblRight = new Label (text, direction: TextDirection.TopBottom_LeftRight) { X = 4, Height = height, VerticalTextAlignment = VerticalTextAlignment.Bottom };
			var lblJust = new Label (text, direction: TextDirection.TopBottom_LeftRight) { X = 6, Height = height, VerticalTextAlignment = VerticalTextAlignment.Justified };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (lblLeft, lblCenter, lblRight, lblJust);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (9, height + 2);

			Assert.True (lblLeft.AutoSize);
			Assert.True (lblCenter.AutoSize);
			Assert.True (lblRight.AutoSize);
			Assert.True (lblJust.AutoSize);
			Assert.Equal (new Rect (0, 0, 1, height), lblLeft.Frame);
			Assert.Equal (new Rect (2, 0, 1, height), lblCenter.Frame);
			Assert.Equal (new Rect (4, 0, 1, height), lblRight.Frame);
			Assert.Equal (new Rect (6, 0, 1, height), lblJust.Frame);
			Assert.Equal (new Rect (0, 0, 9, height + 2), frame.Frame);

			var expected = @"
┌───────┐
│H     H│
│e     e│
│l     l│
│l     l│
│o H   o│
│  e    │
│W l    │
│o l    │
│r o    │
│l   H  │
│d W e  │
│  o l  │
│  r l  │
│  l o  │
│  d    │
│    W W│
│    o o│
│    r r│
│    l l│
│    d d│
└───────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 9, height + 2), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Horizontal_Wide_TextAlignments ()
		{
			var text = "こんにちは 世界";
			var width = 25;
			var lblLeft = new Label (text) { Width = width };
			var lblCenter = new Label (text) { Y = 1, Width = width, TextAlignment = TextAlignment.Centered };
			var lblRight = new Label (text) { Y = 2, Width = width, TextAlignment = TextAlignment.Right };
			var lblJust = new Label (text) { Y = 3, Width = width, TextAlignment = TextAlignment.Justified };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (lblLeft, lblCenter, lblRight, lblJust);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

			Assert.True (lblLeft.AutoSize);
			Assert.True (lblCenter.AutoSize);
			Assert.True (lblRight.AutoSize);
			Assert.True (lblJust.AutoSize);
			Assert.Equal (new Rect (0, 0, width, 1), lblLeft.Frame);
			Assert.Equal (new Rect (0, 1, width, 1), lblCenter.Frame);
			Assert.Equal (new Rect (0, 2, width, 1), lblRight.Frame);
			Assert.Equal (new Rect (0, 3, width, 1), lblJust.Frame);
			Assert.Equal (new Rect (0, 0, width + 2, 6), frame.Frame);

			var expected = @"
┌─────────────────────────┐
│こんにちは 世界          │
│     こんにちは 世界     │
│          こんにちは 世界│
│こんにちは           世界│
└─────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, width + 2, 6), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Vertical_Wide_TextAlignments ()
		{
			var text = "こんにちは 世界";
			var height = 23;
			var lblLeft = new Label (text) { Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight };
			var lblCenter = new Label (text) { X = 3, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Middle };
			var lblRight = new Label (text) { X = 6, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };
			var lblJust = new Label (text) { X = 9, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Justified };
			var frame = new FrameView () { Width = Dim.Fill (), Height = Dim.Fill () };

			frame.Add (lblLeft, lblCenter, lblRight, lblJust);
			Application.Top.Add (frame);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (13, height + 2);

			// All AutoSize are false because the Frame.Height != TextFormatter.Size.Height
			Assert.True (lblLeft.AutoSize);
			Assert.True (lblCenter.AutoSize);
			Assert.True (lblRight.AutoSize);
			Assert.True (lblJust.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, height), lblLeft.Frame);
			Assert.Equal (new Rect (3, 0, 2, height), lblCenter.Frame);
			Assert.Equal (new Rect (6, 0, 2, height), lblRight.Frame);
			Assert.Equal (new Rect (9, 0, 2, height), lblJust.Frame);
			Assert.Equal (new Rect (0, 0, 13, height + 2), frame.Frame);

			var expected = @"
┌───────────┐
│こ       こ│
│ん       ん│
│に       に│
│ち       ち│
│は       は│
│           │
│世         │
│界 こ      │
│   ん      │
│   に      │
│   ち      │
│   は      │
│           │
│   世      │
│   界      │
│      こ   │
│      ん   │
│      に   │
│      ち   │
│      は   │
│           │
│      世 世│
│      界 界│
└───────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 13, height + 2), pos);
		}

		[Fact, AutoInitShutdown]
		public void Label_Draw_Fill_Remaining ()
		{
			var view = new View ("This view needs to be cleared before rewritten.");

			var tf1 = new TextFormatter ();
			tf1.Text = "This TextFormatter (tf1) without fill will not be cleared on rewritten.";
			var tf1Size = tf1.Size;

			var tf2 = new TextFormatter ();
			tf2.Text = "This TextFormatter (tf2) with fill will be cleared on rewritten.";
			var tf2Size = tf2.Size;

			Application.Top.Add (view);
			Application.Begin (Application.Top);

			tf1.Draw (new Rect (new Point (0, 1), tf1Size), view.GetNormalColor (), view.ColorScheme.HotNormal, default, false);

			tf2.Draw (new Rect (new Point (0, 2), tf2Size), view.GetNormalColor (), view.ColorScheme.HotNormal);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
This view needs to be cleared before rewritten.                        
This TextFormatter (tf1) without fill will not be cleared on rewritten.
This TextFormatter (tf2) with fill will be cleared on rewritten.       
", output);

			view.Text = "This view is rewritten.";
			view.Redraw (view.Bounds);

			tf1.Text = "This TextFormatter (tf1) is rewritten.";
			tf1.Draw (new Rect (new Point (0, 1), tf1Size), view.GetNormalColor (), view.ColorScheme.HotNormal, default, false);

			tf2.Text = "This TextFormatter (tf2) is rewritten.";
			tf2.Draw (new Rect (new Point (0, 2), tf2Size), view.GetNormalColor (), view.ColorScheme.HotNormal);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
This view is rewritten.                                                
This TextFormatter (tf1) is rewritten.will not be cleared on rewritten.
This TextFormatter (tf2) is rewritten.                                 
", output);
		}

	}
}
