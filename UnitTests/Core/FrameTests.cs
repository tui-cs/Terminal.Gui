using NStack;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.CoreTests {
	public class FrameTests {
		readonly ITestOutputHelper output;

		public FrameTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory, AutoInitShutdown]
		[InlineData (0)]
		[InlineData (1)]
		[InlineData (2)]
		[InlineData (3)]
		public void BorderFrame_With_Title_Size_Height (int height)
		{
			var win = new Window ("1234") {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var rs = Application.Begin (win);
			bool firstIteration = false;

			((FakeDriver)Application.Driver).SetBufferSize (20, height);
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);
			var expected = string.Empty;

			switch (height) {
			case 0:
				//Assert.Equal (new Rect (0, 0, 17, 0), subview.Frame);
				expected = @"
";
				break;
			case 1:
				//Assert.Equal (new Rect (0, 0, 17, 0), subview.Frame);
				expected = @"
────────────────────";
				break;
			case 2:
				//Assert.Equal (new Rect (0, 0, 17, 1), subview.Frame);
				expected = @"
┌┤1234├────────────┐
└──────────────────┘
";
				break;
			case 3:
				//Assert.Equal (new Rect (0, 0, 17, 2), subview.Frame);
				expected = @"
┌┤1234├────────────┐
│                  │
└──────────────────┘
";
				break;
			}
			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

		}
		
		[Theory, AutoInitShutdown]
		[InlineData (0)]
		[InlineData (1)]
		[InlineData (2)]
		[InlineData (3)]
		[InlineData (4)]
		[InlineData (5)]
		[InlineData (6)]
		[InlineData (7)]
		[InlineData (8)]
		[InlineData (9)]
		[InlineData (10)]
		public void BorderFrame_With_Title_Size_Width (int width)
		{
			var win = new Window ("1234") {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var rs = Application.Begin (win);
			bool firstIteration = false;

			((FakeDriver)Application.Driver).SetBufferSize (width, 3);
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);
			var expected = string.Empty;

			switch (width) {
			case 1:
				//Assert.Equal (new Rect (0, 0, 17, 0), subview.Frame);
				expected = @"
│
│
│";
				break;
			case 2:
				//Assert.Equal (new Rect (0, 0, 17, 1), subview.Frame);
				expected = @"
┌┐
││
└┘";
				break;
			case 3:
				//Assert.Equal (new Rect (0, 0, 17, 2), subview.Frame);
				expected = @"
┌─┐
│ │
└─┘
";
				break;
			case 4:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤├┐
│  │
└──┘";
				break;
			case 5:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤1├┐
│   │
└───┘";
				break;
			case 6:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤12├┐
│    │
└────┘";
				break;
			case 7:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤123├┐
│     │
└─────┘";
				break;
			case 8:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤1234├┐
│      │
└──────┘";
				break;
			case 9:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤1234├─┐
│       │
└───────┘";
				break;
			case 10:
				//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
				expected = @"
┌┤1234├──┐
│        │
└────────┘";
				break;
			}
			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

		}
	}
}
