using NStack;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class FrameTests {
		readonly ITestOutputHelper output;

		public FrameTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void GetFramesThickness ()
		{
			var view = new View ();
			Assert.Equal (Thickness.Empty, view.GetFramesThickness ());

			view.Margin.Thickness = new Thickness (1);
			Assert.Equal (new Thickness (1), view.GetFramesThickness ());

			view.Border.Thickness = new Thickness (1);
			Assert.Equal (new Thickness (2), view.GetFramesThickness ());

			view.Padding.Thickness = new Thickness (1);
			Assert.Equal (new Thickness (3), view.GetFramesThickness ());

			view.Padding.Thickness = new Thickness (2);
			Assert.Equal (new Thickness (4), view.GetFramesThickness ());

			view.Padding.Thickness = new Thickness (1, 2, 3, 4);
			Assert.Equal (new Thickness (3, 4, 5, 6), view.GetFramesThickness ());

			view.Margin.Thickness = new Thickness (1, 2, 3, 4);
			Assert.Equal (new Thickness (3, 5, 7, 9), view.GetFramesThickness ());
		}

		[Theory, AutoInitShutdown]
		[InlineData (0)]
		[InlineData (1)]
		[InlineData (2)]
		[InlineData (3)]
		public void Border_With_Title_Size_Height (int height)
		{
			var win = new Window () { 
				Title = "1234",
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
		public void Border_With_Title_Size_Width (int width)
		{
			var win = new Window () {
				Title = "1234",
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

		[Fact, AutoInitShutdown]
		public void NoSuperView ()
		{
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var rs = Application.Begin (win);
			bool firstIteration = false;

			((FakeDriver)Application.Driver).SetBufferSize (3, 3);
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);
			var expected = @"
┌─┐
│ │
└─┘";

			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void HasSuperView ()
		{
			Application.Top.BorderStyle = LineStyle.Double;

			var frame = new FrameView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			Application.Top.Add (frame);
			var rs = Application.Begin (Application.Top);
			bool firstIteration = false;

			((FakeDriver)Application.Driver).SetBufferSize (5, 5);
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);
			var expected = @"
╔═══╗
║┌─┐║
║│ │║
║└─┘║
╚═══╝";

			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void HasSuperView_Title ()
		{
			Application.Top.BorderStyle = LineStyle.Double;

			var frame = new FrameView () {
				Title = "1234",
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			Application.Top.Add (frame);
			var rs = Application.Begin (Application.Top);
			bool firstIteration = false;

			((FakeDriver)Application.Driver).SetBufferSize (10, 4);
			Application.RunMainLoopIteration (ref rs, true, ref firstIteration);
			var expected = @"
╔════════╗
║┌┤1234├┐║
║└──────┘║
╚════════╝";

			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
	}
}
