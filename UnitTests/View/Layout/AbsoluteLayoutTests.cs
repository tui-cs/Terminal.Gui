using System.Text;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class AbsoluteLayoutTests {
		readonly ITestOutputHelper output;

		public AbsoluteLayoutTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void AbsoluteLayout_Constructor ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var v = new View (frame);
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (frame, v.Frame);
			Assert.Equal (new Rect(0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame, "v");
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (frame, v.Frame);
			Assert.Equal (new Rect (0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame.X, frame.Y, "v");
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			// BUGBUG: v2 - I think the default size should be 0,0
			Assert.Equal (new Rect(frame.X, frame.Y, 1, 1), v.Frame);
			Assert.Equal (new Rect (0, 0, 1, 1), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);
		}

		[Fact]
		public void AbsoluteLayout_Change_Frame ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var newFrame = new Rect (1, 2, 30, 40);

			var v = new View (frame);
			v.Frame = newFrame;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame.X, frame.Y, "v");
			v.Frame = newFrame;
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			newFrame = new Rect (10, 20, 30, 40);
			v = new View (frame);
			v.Frame = newFrame;
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

			v = new View (frame.X, frame.Y, "v");
			v.Frame = newFrame;
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Null (v.Height);
			Assert.Null (v.Width);

		}

		[Fact]
		public void AbsoluteLayout_Change_Height_or_Width_Absolute ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var newFrame = new Rect (1, 2, 30, 40);

			var v = new View (frame);
			v.Height = newFrame.Height;
			v.Width = newFrame.Width;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Null (v.X);
			Assert.Null (v.Y);
			Assert.Equal ($"Absolute({newFrame.Height})", v.Height.ToString());
			Assert.Equal ($"Absolute({newFrame.Width})", v.Width.ToString ());
		}

		[Fact]
		public void AbsoluteLayout_Change_Height_or_Width_NotAbsolute ()
		{
			var v = new View (Rect.Empty);
			v.Height = Dim.Fill ();
			v.Width = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);  // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle
		}

		[Fact]
		public void AbsoluteLayout_Change_Height_or_Width_Null ()
		{
			var v = new View (Rect.Empty);
			v.Height = null;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		}

		[Fact]
		public void AbsoluteLayout_Change_X_or_Y_Absolute ()
		{
			var frame = new Rect (1, 2, 3, 4);
			var newFrame = new Rect (10, 20, 3, 4);

			var v = new View (frame);
			v.X = newFrame.X;
			v.Y = newFrame.Y;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
			Assert.Equal (newFrame, v.Frame);
			Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
			Assert.Equal ($"Absolute({newFrame.X})", v.X.ToString ());
			Assert.Equal ($"Absolute({newFrame.Y})", v.Y.ToString ());
			Assert.Null (v.Height);
			Assert.Null (v.Width);
		}

		[Fact]
		public void AbsoluteLayout_Change_X_or_Y_NotAbsolute ()
		{
			var v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle
		}

		[Fact]
		public void AbsoluteLayout_Change_X_or_Y_Null ()
		{
			var v = new View (Rect.Empty);
			v.X = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			v.X = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

			v = new View (Rect.Empty);
			v.Y = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

			v = new View (Rect.Empty);
			v.Y = Pos.Center ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			v.Y = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		}

		[Fact]
		public void AbsoluteLayout_Change_X_Y_Height_Width_Absolute ()
		{
			var v = new View (Rect.Empty);
			v.X = 1;
			v.Y = 2;
			v.Height = 3;
			v.Width = 4;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			v.Width = Dim.Fill ();
			v.Height = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			// BUGBUG: v2 - If all of X, Y, Width, and Height are null or Absolute(n), isn't that the same as LayoutStyle.Absoulte?
			v.X = null;
			v.Y = null;
			v.Height = null;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // We never automatically change to Absolute from Computed??

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			v.Width = Dim.Fill ();
			v.Height = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			// BUGBUG: v2 - If all of X, Y, Width, and Height are null or Absolute(n), isn't that the same as LayoutStyle.Absoulte?
			v.X = 1;
			v.Y = null;
			v.Height = null;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // We never automatically change to Absolute from Computed??

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			v.Width = Dim.Fill ();
			v.Height = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			// BUGBUG: v2 - If all of X, Y, Width, and Height are null or Absolute(n), isn't that the same as LayoutStyle.Absoulte?
			v.X = null;
			v.Y = 2;
			v.Height = null;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // We never automatically change to Absolute from Computed??

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			v.Width = Dim.Fill ();
			v.Height = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			// BUGBUG: v2 - If all of X, Y, Width, and Height are null or Absolute(n), isn't that the same as LayoutStyle.Absoulte?
			v.X = null;
			v.Y = null;
			v.Height = 3;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // We never automatically change to Absolute from Computed??

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			v.Width = Dim.Fill ();
			v.Height = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			// BUGBUG: v2 - If all of X, Y, Width, and Height are null or Absolute(n), isn't that the same as LayoutStyle.Absoulte?
			v.X = null;
			v.Y = null;
			v.Height = null;
			v.Width = 4;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // We never automatically change to Absolute from Computed??
		}

		[Fact]
		public void AbsoluteLayout_Change_X_Y_Height_Width_Null ()
		{
			var v = new View (Rect.Empty);
			v.X = null;
			v.Y = null;
			v.Height = null;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

			v = new View (Rect.Empty);
			v.X = Pos.Center ();
			v.Y = Pos.Center ();
			v.Width = Dim.Fill ();
			v.Height = Dim.Fill ();
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // BUGBUG: v2 - Changing the Height or Width should change the LayoutStyle

			// BUGBUG: v2 - If all of X, Y, Width, and Height are null or Absolute(n), isn't that the same as LayoutStyle.Absoulte?
			v.X = null;
			v.Y = null;
			v.Height = null;
			v.Width = null;
			Assert.True (v.LayoutStyle == LayoutStyle.Absolute); // We never automatically change to Absolute from Computed??
		}

		[Fact]
		public void AbsoluteLayout_Layout ()
		{
			var superRect = new Rect (0, 0, 100, 100);
			var super = new View (superRect, "super");
			Assert.True (super.LayoutStyle == LayoutStyle.Absolute);
			var v1 = new View () {
				X = 0,
				Y = 0,
				Width = 10,
				Height = 10
			};
			// BUGBUG: v2 - This should be LayoutStyle.Absolute
			Assert.True (v1.LayoutStyle == LayoutStyle.Computed);

			var v2 = new View () {
				X = 10,
				Y = 10,
				Width = 10,
				Height = 10
			};
			// BUGBUG: v2 - This should be LayoutStyle.Absolute
			Assert.True (v1.LayoutStyle == LayoutStyle.Computed);

			super.Add (v1, v2);
			super.LayoutSubviews ();
			Assert.Equal (new Rect (0, 0, 10, 10), v1.Frame);
			Assert.Equal (new Rect (10, 10, 10, 10), v2.Frame);
		}
	}
}
