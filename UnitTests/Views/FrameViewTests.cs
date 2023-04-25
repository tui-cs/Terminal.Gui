using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class FrameViewTests {
		readonly ITestOutputHelper output;

		public FrameViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constuctors_Defaults ()
		{
			var fv = new FrameView ();
			Assert.Equal (string.Empty, fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			Assert.Equal (LineStyle.Single, fv.BorderStyle);

			fv = new FrameView ("Test");
			Assert.Equal ("Test", fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			Assert.Equal (LineStyle.Single, fv.BorderStyle);

			fv = new FrameView (new Rect (1, 2, 10, 20), "Test");
			Assert.Equal ("Test", fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			fv.BeginInit ();
			fv.EndInit ();
			Assert.Equal (LineStyle.Single, fv.BorderStyle);
			Assert.Equal (new Rect (1, 2, 10, 20), fv.Frame);
		}

		[Fact, AutoInitShutdown]
		public void Draw_Defaults ()
		{
			((FakeDriver)Application.Driver).SetBufferSize (10, 10);
			var fv = new FrameView ();
			Assert.Equal (string.Empty, fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			Application.Top.Add (fv);
			Application.Begin (Application.Top);
			Assert.Equal (new Rect (0, 0, 0, 0), fv.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

			fv.Height = 5;
			fv.Width = 5;
			Assert.Equal (new Rect (0, 0, 5, 5), fv.Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌───┐
│   │
│   │
│   │
└───┘", output);

			fv.X = 1;
			fv.Y = 2;
			Assert.Equal (new Rect (1, 2, 5, 5), fv.Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 ┌───┐
 │   │
 │   │
 │   │
 └───┘", output);

			fv.X = -1;
			fv.Y = -2;
			Assert.Equal (new Rect (-1, -2, 5, 5), fv.Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
   │
   │
───┘", output);

			fv.X = 7;
			fv.Y = 8;
			Assert.Equal (new Rect (7, 8, 5, 5), fv.Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
       ┌──
       │  ", output);
		}
	}
}
