using System;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;
using NStack;
using Terminal.Gui;

namespace Terminal.Gui.TopLevelTests {
	public class WindowTests {
		readonly ITestOutputHelper output;

		public WindowTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void New_Initializes ()
		{
			// Parameterless
			var r = new Window ();
			Assert.NotNull (r);
			Assert.Equal (ustring.Empty, r.Title);
			Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);
			Assert.Equal ("Window()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
			Assert.True (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.NotNull (r.ColorScheme);
			Assert.Equal (Dim.Fill (0), r.Width);
			Assert.Equal (Dim.Fill (0), r.Height);
			Assert.Null (r.X);
			Assert.Null (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.NotEmpty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Empty Rect
			r = new Window (Rect.Empty, "title");
			Assert.NotNull (r);
			Assert.Equal ("title", r.Title);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("Window()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
			Assert.True (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.NotNull (r.ColorScheme);
			Assert.Null (r.Width);       // All view Dim are initialized now in the IsAdded setter,
			Assert.Null (r.Height);      // avoiding Dim errors.
			Assert.Null (r.X);           // All view Pos are initialized now in the IsAdded setter,
			Assert.Null (r.Y);           // avoiding Pos errors.
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.NotEmpty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Rect with values
			r = new Window (new Rect (1, 2, 3, 4), "title");
			Assert.Equal ("title", r.Title);
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("Window()({X=1,Y=2,Width=3,Height=4})", r.ToString ());
			Assert.True (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 3, 4), r.Bounds);
			Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
			Assert.Null (r.Focused);
			Assert.NotNull (r.ColorScheme);
			Assert.Null (r.Width);
			Assert.Null (r.Height);
			Assert.Null (r.X);
			Assert.Null (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.NotEmpty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
			r.Dispose ();
		}

		[Fact]
		public void Set_Title_Fires_TitleChanging ()
		{
			var r = new Window ();
			Assert.Equal (ustring.Empty, r.Title);

			string expectedOld = null;
			string expectedDuring = null;
			string expectedAfter = null;
			bool cancel = false;
			r.TitleChanging += (s, args) => {
				Assert.Equal (expectedOld, args.OldTitle);
				Assert.Equal (expectedDuring, args.NewTitle);
				args.Cancel = cancel;
			};

			expectedOld = string.Empty;
			r.Title = expectedDuring = expectedAfter = "title";
			Assert.Equal (expectedAfter, r.Title.ToString ());

			expectedOld = r.Title.ToString ();
			r.Title = expectedDuring = expectedAfter = "a different title";
			Assert.Equal (expectedAfter, r.Title.ToString ());

			// Now setup cancelling the change and change it back to "title"
			cancel = true;
			expectedOld = r.Title.ToString ();
			r.Title = expectedDuring = "title";
			Assert.Equal (expectedAfter, r.Title.ToString ());
			r.Dispose ();

		}

		[Fact]
		public void Set_Title_Fires_TitleChanged ()
		{
			var r = new Window ();
			Assert.Equal (ustring.Empty, r.Title);

			string expectedOld = null;
			string expected = null;
			r.TitleChanged += (s,args) => {
				Assert.Equal (expectedOld, args.OldTitle);
				Assert.Equal (r.Title, args.NewTitle);
			};

			expected = "title";
			expectedOld = r.Title.ToString ();
			r.Title = expected;
			Assert.Equal (expected, r.Title.ToString ());

			expected = "another title";
			expectedOld = r.Title.ToString ();
			r.Title = expected;
			Assert.Equal (expected, r.Title.ToString ());
			r.Dispose ();
		}

		[Fact, AutoInitShutdown]
		public void MenuBar_And_StatusBar_Inside_Window ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("Open", "", null),
					new MenuItem ("Quit", "", null),
				}),
				new MenuBarItem ("Edit", new MenuItem [] {
					new MenuItem ("Copy", "", null),
				})
			});

			var sb = new StatusBar (new StatusItem [] {
				new StatusItem (Key.CtrlMask | Key.Q, "~^Q~ Quit", null),
				new StatusItem (Key.CtrlMask | Key.O, "~^O~ Open", null),
				new StatusItem (Key.CtrlMask | Key.C, "~^C~ Copy", null),
			});

			var fv = new FrameView ("Frame View") {
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (1)
			};
			var win = new Window ();
			win.Add (menu, sb, fv);
			var top = Application.Top;
			top.Add (win);
			Application.Begin (top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│ File  Edit       │
│┌ Frame View ────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│ ^Q Quit │ ^O Open│
└──────────────────┘", output);

			((FakeDriver)Application.Driver).SetBufferSize (40, 20);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌ Frame View ────────────────────────┐│
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
││                                    ││
│└────────────────────────────────────┘│
│ ^Q Quit │ ^O Open │ ^C Copy          │
└──────────────────────────────────────┘", output);

			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│ File  Edit       │
│┌ Frame View ────┐│
││                ││
││                ││
││                ││
││                ││
│└────────────────┘│
│ ^Q Quit │ ^O Open│
└──────────────────┘", output);

		}
	}
}
