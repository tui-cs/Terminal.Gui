using Terminal.Gui;
using NStack;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DrawingTests {
	public class ThicknessTests {

		readonly ITestOutputHelper output;

		public ThicknessTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact ()]
		public void Constructor_Defaults ()
		{
			var t = new Thickness ();
			Assert.Equal (0, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (0, t.Bottom);
		}

		[Fact ()]
		public void Empty_Is_empty ()
		{
			var t = Thickness.Empty;
			Assert.Equal (0, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (0, t.Bottom);
		}

		[Fact ()]
		public void Constructor_Width ()
		{
			var t = new Thickness (1);
			Assert.Equal (1, t.Left);
			Assert.Equal (1, t.Top);
			Assert.Equal (1, t.Right);
			Assert.Equal (1, t.Bottom);
		}


		[Fact ()]
		public void Constructor_params ()
		{
			var t = new Thickness (1, 2, 3, 4);
			Assert.Equal (1, t.Left);
			Assert.Equal (2, t.Top);
			Assert.Equal (3, t.Right);
			Assert.Equal (4, t.Bottom);

			t = new Thickness (0, 0, 0, 0);
			Assert.Equal (0, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (0, t.Bottom);

			t = new Thickness (-1, 0, 0, 0);
			Assert.Equal (-1, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (0, t.Bottom);
		}

		[Fact ()]
		public void Vertical_get ()
		{
			var t = new Thickness (1, 2, 3, 4);
			Assert.Equal (6, t.Vertical);

			t = new Thickness (0);
			Assert.Equal (0, t.Vertical);
		}

		[Fact ()]
		public void Horizontal_get ()
		{
			var t = new Thickness (1, 2, 3, 4);
			Assert.Equal (4, t.Horizontal);

			t = new Thickness (0);
			Assert.Equal (0, t.Horizontal);
		}

		[Fact ()]
		public void Vertical_set ()
		{
			var t = new Thickness ();
			t.Vertical = 10;
			Assert.Equal (10, t.Vertical);
			Assert.Equal (0, t.Left);
			Assert.Equal (5, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (5, t.Bottom);
			Assert.Equal (0, t.Horizontal);

			t.Vertical = 11;
			Assert.Equal (10, t.Vertical);
			Assert.Equal (0, t.Left);
			Assert.Equal (5, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (5, t.Bottom);
			Assert.Equal (0, t.Horizontal);

			t.Vertical = 1;
			Assert.Equal (0, t.Vertical);
			Assert.Equal (0, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (0, t.Bottom);
			Assert.Equal (0, t.Horizontal);
		}

		[Fact ()]
		public void Horizontal_set ()
		{
			var t = new Thickness ();
			t.Horizontal = 10;
			Assert.Equal (10, t.Horizontal);
			Assert.Equal (5, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (5, t.Right);
			Assert.Equal (0, t.Bottom);
			Assert.Equal (0, t.Vertical);

			t.Horizontal = 11;
			Assert.Equal (10, t.Horizontal);
			Assert.Equal (5, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (5, t.Right);
			Assert.Equal (0, t.Bottom);
			Assert.Equal (0, t.Vertical);

			t.Horizontal = 1;
			Assert.Equal (0, t.Horizontal);
			Assert.Equal (0, t.Left);
			Assert.Equal (0, t.Top);
			Assert.Equal (0, t.Right);
			Assert.Equal (0, t.Bottom);
			Assert.Equal (0, t.Vertical);

		}

		[Fact ()]
		public void GetInsideTests_Zero_Thickness ()
		{
			var t = new Thickness (0, 0, 0, 0);
			var r = Rect.Empty;
			var inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (0, 0, 0, 0);
			r = new Rect (0, 0, 1, 1);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (1, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (0, 0, 0, 0);
			r = new Rect (1, 1, 1, 1);
			inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (1, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (0, 0, 0, 0);
			r = new Rect (0, 0, 1, 0);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (1, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (0, 0, 0, 0);
			r = new Rect (0, 0, 0, 1);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (0, 0, 0, 0);
			r = new Rect (-1, -1, 0, 1);
			inside = t.GetInside (r);
			Assert.Equal (-1, inside.X);
			Assert.Equal (-1, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (1, inside.Height);
		}

		[Fact ()]
		public void GetInsideTests_Positive_Thickness_Too_Small_Rect_Means_Empty_Size ()
		{
			var t = new Thickness (1, 1, 1, 1);
			var r = Rect.Empty;
			var inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (0, 0, 1, 1);
			inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (1, 1, 1, 1);
			inside = t.GetInside (r);
			Assert.Equal (2, inside.X);
			Assert.Equal (2, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (0, 0, 1, 0);
			inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (0, 0, 0, 1);
			inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (-1, -1, 0, 1);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (0, 0, 2, 2);
			inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (-1, -1, 2, 2);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (1, 1, 2, 2);
			inside = t.GetInside (r);
			Assert.Equal (2, inside.X);
			Assert.Equal (2, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);

			t = new Thickness (1, 2, 3, 4);
			r = new Rect (-1, 0, 4, 6);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (2, inside.Y);
			Assert.Equal (0, inside.Width);
			Assert.Equal (0, inside.Height);
		}

		[Fact ()]
		public void GetInsideTests_Positive_Thickness_Non_Empty_Size()
		{

			var t = new Thickness (1, 1, 1, 1);
			var r = new Rect (0, 0, 3, 3);
			var inside = t.GetInside (r);
			Assert.Equal (1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (1, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (-1, -1, 3, 3);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (1, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (1, 1, 1, 1);
			r = new Rect (1, 1, 3, 3);
			inside = t.GetInside (r);
			Assert.Equal (2, inside.X);
			Assert.Equal (2, inside.Y);
			Assert.Equal (1, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (1, 2, 3, 4);
			r = new Rect (-1, 0, 50, 60);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (2, inside.Y);
			Assert.Equal (46, inside.Width);
			Assert.Equal (54, inside.Height);
		}

		[Fact ()]
		public void GetInsideTests_Negative_Thickness_Non_Empty_Size ()
		{
			var t = new Thickness (-1, -1, -1, -1);
			var r = new Rect (0, 0, 3, 3);
			var inside = t.GetInside (r);
			Assert.Equal (-1, inside.X);
			Assert.Equal (-1, inside.Y);
			Assert.Equal (5, inside.Width);
			Assert.Equal (5, inside.Height);

			t = new Thickness (-1, -1, -1, -1);
			r = new Rect (-1, -1, 3, 3);
			inside = t.GetInside (r);
			Assert.Equal (-2, inside.X);
			Assert.Equal (-2, inside.Y);
			Assert.Equal (5, inside.Width);
			Assert.Equal (5, inside.Height);

			t = new Thickness (-1, -1, -1, -1);
			r = new Rect (1, 1, 3, 3);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (5, inside.Width);
			Assert.Equal (5, inside.Height);

			t = new Thickness (-1, -2, -3, -4);
			r = new Rect (-1, 0, 50, 60);
			inside = t.GetInside (r);
			Assert.Equal (-2, inside.X);
			Assert.Equal (-2, inside.Y);
			Assert.Equal (54, inside.Width);
			Assert.Equal (66, inside.Height);
		}


		[Fact ()]
		public void GetInsideTests_Mixed_Pos_Neg_Thickness_Non_Empty_Size ()
		{
			var t = new Thickness (-1, 1, -1, 1);
			var r = new Rect (0, 0, 3, 3);
			var inside = t.GetInside (r);
			Assert.Equal (-1, inside.X);
			Assert.Equal (1, inside.Y);
			Assert.Equal (5, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (-1, 1, -1, 1);
			r = new Rect (-1, -1, 3, 3);
			inside = t.GetInside (r);
			Assert.Equal (-2, inside.X);
			Assert.Equal (0, inside.Y);
			Assert.Equal (5, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (-1, 1, -1, 1);
			r = new Rect (1, 1, 3, 3);
			inside = t.GetInside (r);
			Assert.Equal (0, inside.X);
			Assert.Equal (2, inside.Y);
			Assert.Equal (5, inside.Width);
			Assert.Equal (1, inside.Height);

			t = new Thickness (-2, -1, 0, 1);
			r = new Rect (-1, 0, 50, 60);
			inside = t.GetInside (r);
			Assert.Equal (-3, inside.X);
			Assert.Equal (-1, inside.Y);
			Assert.Equal (52, inside.Width);
			Assert.Equal (60, inside.Height);
		}

		[Fact (), AutoInitShutdown]
		public void DrawTests ()
		{
			((FakeDriver)Application.Driver).SetBufferSize (60, 60);
			var t = new Thickness (0, 0, 0, 0);
			var r = new Rect (5, 5, 40, 15);
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FramePadding;
			Application.Driver.FillRect (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), ' ');
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
       Test (Left=0,Top=0,Right=0,Bottom=0)", output);


			t = new Thickness (1, 1, 1, 1);
			r = new Rect (5, 5, 40, 15);
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FramePadding;
			Application.Driver.FillRect (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), ' ');
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     TTTest (Left=1,Top=1,Right=1,Bottom=1)TT", output);

			t = new Thickness (1, 2, 3, 4);
			r = new Rect (5, 5, 40, 15);
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FramePadding;
			Application.Driver.FillRect (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), ' ');
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTest (Left=1,Top=2,Right=3,Bottom=4)TT", output);


			t = new Thickness (-1, 1, 1, 1);
			r = new Rect (5, 5, 40, 15);
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FramePadding;
			Application.Driver.FillRect (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), ' ');
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
     TTest (Left=-1,Top=1,Right=1,Bottom=1)TT", output);

		}

		[Fact (), AutoInitShutdown]
		public void DrawTests_Ruler ()
		{
			// Add a frame so we can see the ruler
			var f = new FrameView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};


			Application.Top.Add (f);
			Application.Begin (Application.Top);
			
			((FakeDriver)Application.Driver).SetBufferSize (45, 20);
			var t = new Thickness (0, 0, 0, 0);
			var r = new Rect (2, 2, 40, 15);
			Application.Refresh ();
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FrameRuler;
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsAre (@"
┌───────────────────────────────────────────┐
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘", output);


			t = new Thickness (1, 1, 1, 1);
			r = new Rect (1, 1, 40, 15);
			Application.Refresh ();
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FrameRuler;
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsAre (@"
┌───────────────────────────────────────────┐
│|123456789|123456789|123456789|123456789   │
│1                                      1   │
│2                                      2   │
│3                                      3   │
│4                                      4   │
│5                                      5   │
│6                                      6   │
│7                                      7   │
│8                                      8   │
│9                                      9   │
│-                                      -   │
│1                                      1   │
│2                                      2   │
│3                                      3   │
│|123456789|123456789|123456789|123456789   │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘", output);

			t = new Thickness (1, 2, 3, 4);
			r = new Rect (2, 2, 40, 15);
			Application.Refresh ();
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FrameRuler;
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌───────────────────────────────────────────┐
│                                           │
│ |123456789|123456789|123456789|123456789  │
│ 1                                      1  │
│ 2                                      2  │
│ 3                                      3  │
│ 4                                      4  │
│ 5                                      5  │
│ 6                                      6  │
│ 7                                      7  │
│ 8                                      8  │
│ 9                                      9  │
│ -                                      -  │
│ 1                                      1  │
│ 2                                      2  │
│ 3                                      3  │
│ |123456789|123456789|123456789|123456789  │
│                                           │
│                                           │
└───────────────────────────────────────────┘", output);


			t = new Thickness (-1, 1, 1, 1);
			r = new Rect (5, 5, 40, 15);
			Application.Refresh ();
			ConsoleDriver.Diagnostics |= ConsoleDriver.DiagnosticFlags.FrameRuler;
			t.Draw (r, "Test");
			ConsoleDriver.Diagnostics = ConsoleDriver.DiagnosticFlags.Off;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌───────────────────────────────────────────┐
│                                           │
│                                           │
│                                           │
│                                           │
│    |123456789|123456789|123456789|123456789
│                                           1
│                                           2
│                                           3
│                                           4
│                                           5
│                                           6
│                                           7
│                                           8
│                                           9
│                                           -
│                                           1
│                                           2
│                                           3
└────|123456789|123456789|123456789|123456789", output);

		}
		[Fact ()]
		public void EqualsTest ()
		{
			var t = new Thickness (1, 2, 3, 4);
			var t2 = new Thickness (1, 2, 3, 4);
			Assert.True (t.Equals (t2));
			Assert.True (t == t2);
			Assert.False (t != t2);
		}

		[Fact ()]
		public void ToStringTest ()
		{
			var t = new Thickness (1, 2, 3, 4);
			Assert.Equal ("(Left=1,Top=2,Right=3,Bottom=4)", t.ToString ());
		}

		[Fact ()]
		public void GetHashCodeTest ()
		{
			var t = new Thickness (1, 2, 3, 4);
			Assert.Equal (t.GetHashCode (), t.GetHashCode ());
		}
	}
}


