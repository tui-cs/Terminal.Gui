using System;
using Xunit;
using Xunit.Abstractions;
using NStack;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class ViewTests {
		readonly ITestOutputHelper output;

		public ViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void New_Initializes ()
		{
			// Parameterless
			var r = new View ();
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);
			Assert.Equal ("View()((0,0,0,0))", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Null (r.Width);
			Assert.Null (r.Height);
			Assert.Null (r.X);
			Assert.Null (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Empty Rect
			r = new View (Rect.Empty);
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("View()((0,0,0,0))", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Null (r.Width);       // All view Dim are initialized now in the IsAdded setter,
			Assert.Null (r.Height);      // avoiding Dim errors.
			Assert.Null (r.X);           // All view Pos are initialized now in the IsAdded setter,
			Assert.Null (r.Y);           // avoiding Pos errors.
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Rect with values
			r = new View (new Rect (1, 2, 3, 4));
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("View()((1,2,3,4))", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 3, 4), r.Bounds);
			Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Null (r.Width);
			Assert.Null (r.Height);
			Assert.Null (r.X);
			Assert.Null (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Initializes a view with a vertical direction
			r = new View ("Vertical View", TextDirection.TopBottom_LeftRight);
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);
			Assert.Equal ("View(Vertical View)((0,0,1,13))", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 1, 13), r.Bounds);
			Assert.Equal (new Rect (0, 0, 1, 13), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Null (r.Width);       // All view Dim are initialized now in the IsAdded setter,
			Assert.Null (r.Height);      // avoiding Dim errors.
			Assert.Null (r.X);           // All view Pos are initialized now in the IsAdded setter,
			Assert.Null (r.Y);           // avoiding Pos errors.
			Assert.False (r.IsCurrentTop);
			Assert.Equal ("Vertical View", r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.TopBottom_LeftRight, r.TextDirection);

		}

		[Fact]
		public void New_Methods_Return_False ()
		{
			var r = new View ();

			Assert.False (r.ProcessKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.ProcessHotKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.ProcessColdKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.OnKeyDown (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.OnKeyUp (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.MouseEvent (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnMouseEnter (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnMouseLeave (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnEnter (new View ()));
			Assert.False (r.OnLeave (new View ()));

			// TODO: Add more
		}

		[Fact]
		public void View_With_No_Difference_Between_An_Object_Initializer_And_A_Constructor ()
		{
			// Object Initializer
			var view = new View () {
				X = 1,
				Y = 2,
				Width = 3,
				Height = 4
			};
			var super = new View (new Rect (0, 0, 10, 10));
			super.Add (view);
			super.BeginInit ();
			super.EndInit ();
			super.LayoutSubviews ();

			Assert.Equal (1, view.X);
			Assert.Equal (2, view.Y);
			Assert.Equal (3, view.Width);
			Assert.Equal (4, view.Height);
			Assert.False (view.Frame.IsEmpty);
			Assert.Equal (new Rect (1, 2, 3, 4), view.Frame);
			Assert.False (view.Bounds.IsEmpty);
			Assert.Equal (new Rect (0, 0, 3, 4), view.Bounds);

			view.LayoutSubviews ();

			Assert.Equal (1, view.X);
			Assert.Equal (2, view.Y);
			Assert.Equal (3, view.Width);
			Assert.Equal (4, view.Height);
			Assert.False (view.Frame.IsEmpty);
			Assert.False (view.Bounds.IsEmpty);

			// Default Constructor
			view = new View ();
			Assert.Null (view.X);
			Assert.Null (view.Y);
			Assert.Null (view.Width);
			Assert.Null (view.Height);
			Assert.True (view.Frame.IsEmpty);
			Assert.True (view.Bounds.IsEmpty);

			// Constructor
			view = new View (1, 2, "");
			Assert.Null (view.X);
			Assert.Null (view.Y);
			Assert.Null (view.Width);
			Assert.Null (view.Height);
			Assert.False (view.Frame.IsEmpty);
			Assert.True (view.Bounds.IsEmpty);

			// Default Constructor and post assignment equivalent to Object Initializer
			view = new View ();
			view.X = 1;
			view.Y = 2;
			view.Width = 3;
			view.Height = 4;
			super = new View (new Rect (0, 0, 10, 10));
			super.Add (view);
			super.BeginInit ();
			super.EndInit ();
			super.LayoutSubviews ();
			Assert.Equal (1, view.X);
			Assert.Equal (2, view.Y);
			Assert.Equal (3, view.Width);
			Assert.Equal (4, view.Height);
			Assert.False (view.Frame.IsEmpty);
			Assert.Equal (new Rect (1, 2, 3, 4), view.Frame);
			Assert.False (view.Bounds.IsEmpty);
			Assert.Equal (new Rect (0, 0, 3, 4), view.Bounds);
		}

		[Fact]
		public void Added_Removed ()
		{
			var v = new View (new Rect (0, 0, 10, 24));
			var t = new View ();

			v.Added += (s, e) => {
				Assert.Same (v.SuperView, e.Parent);
				Assert.Same (t, e.Parent);
				Assert.Same (v, e.Child);
			};

			v.Removed += (s, e) => {
				Assert.Same (t, e.Parent);
				Assert.Same (v, e.Child);
				Assert.True (v.SuperView == null);
			};

			t.Add (v);
			Assert.True (t.Subviews.Count == 1);

			t.Remove (v);
			Assert.True (t.Subviews.Count == 0);
		}

		[Fact]
		public void Initialized_Event_Comparing_With_Added_Event ()
		{
			Application.Init (new FakeDriver ());

			var t = new Toplevel () { Id = "0", };

			var w = new Window () { Id = "t", Width = Dim.Fill (), Height = Dim.Fill () };
			var v1 = new View () { Id = "v1", Width = Dim.Fill (), Height = Dim.Fill () };
			var v2 = new View () { Id = "v2", Width = Dim.Fill (), Height = Dim.Fill () };
			var sv1 = new View () { Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill () };

			int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

			w.Added += (s, e) => {
				Assert.Equal (e.Parent.Frame.Width, w.Frame.Width);
				Assert.Equal (e.Parent.Frame.Height, w.Frame.Height);
			};
			v1.Added += (s, e) => {
				Assert.Equal (e.Parent.Frame.Width, v1.Frame.Width);
				Assert.Equal (e.Parent.Frame.Height, v1.Frame.Height);
			};
			v2.Added += (s, e) => {
				Assert.Equal (e.Parent.Frame.Width, v2.Frame.Width);
				Assert.Equal (e.Parent.Frame.Height, v2.Frame.Height);
			};
			sv1.Added += (s, e) => {
				Assert.Equal (e.Parent.Frame.Width, sv1.Frame.Width);
				Assert.Equal (e.Parent.Frame.Height, sv1.Frame.Height);
			};

			t.Initialized += (s, e) => {
				tc++;
				Assert.Equal (1, tc);
				Assert.Equal (1, wc);
				Assert.Equal (1, v1c);
				Assert.Equal (1, v2c);
				Assert.Equal (1, sv1c);

				Assert.True (t.CanFocus);
				Assert.True (w.CanFocus);
				Assert.False (v1.CanFocus);
				Assert.False (v2.CanFocus);
				Assert.False (sv1.CanFocus);

				Application.Refresh ();
			};
			w.Initialized += (s, e) => {
				wc++;
				Assert.Equal (t.Frame.Width, w.Frame.Width);
				Assert.Equal (t.Frame.Height, w.Frame.Height);
			};
			v1.Initialized += (s, e) => {
				v1c++;
				Assert.Equal (t.Frame.Width, v1.Frame.Width);
				Assert.Equal (t.Frame.Height, v1.Frame.Height);
			};
			v2.Initialized += (s, e) => {
				v2c++;
				Assert.Equal (t.Frame.Width, v2.Frame.Width);
				Assert.Equal (t.Frame.Height, v2.Frame.Height);
			};
			sv1.Initialized += (s, e) => {
				sv1c++;
				Assert.Equal (t.Frame.Width, sv1.Frame.Width);
				Assert.Equal (t.Frame.Height, sv1.Frame.Height);
				Assert.False (sv1.CanFocus);
				Assert.Throws<InvalidOperationException> (() => sv1.CanFocus = true);
				Assert.False (sv1.CanFocus);
			};

			v1.Add (sv1);
			w.Add (v1, v2);
			t.Add (w);

			Application.Iteration = () => {
				Application.Refresh ();
				t.Running = false;
			};

			Application.Run (t);
			Application.Shutdown ();

			Assert.Equal (1, tc);
			Assert.Equal (1, wc);
			Assert.Equal (1, v1c);
			Assert.Equal (1, v2c);
			Assert.Equal (1, sv1c);

			Assert.True (t.CanFocus);
			Assert.True (w.CanFocus);
			Assert.False (v1.CanFocus);
			Assert.False (v2.CanFocus);
			Assert.False (sv1.CanFocus);

			v1.CanFocus = true;
			Assert.False (sv1.CanFocus); // False because sv1 was disposed and it isn't a subview of v1.
		}

		[Fact]
		public void Initialized_Event_Will_Be_Invoked_When_Added_Dynamically ()
		{
			Application.Init (new FakeDriver ());

			var t = new Toplevel () { Id = "0", };

			var w = new Window () { Id = "t", Width = Dim.Fill (), Height = Dim.Fill () };
			var v1 = new View () { Id = "v1", Width = Dim.Fill (), Height = Dim.Fill () };
			var v2 = new View () { Id = "v2", Width = Dim.Fill (), Height = Dim.Fill () };

			int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

			t.Initialized += (s, e) => {
				tc++;
				Assert.Equal (1, tc);
				Assert.Equal (1, wc);
				Assert.Equal (1, v1c);
				Assert.Equal (1, v2c);
				Assert.Equal (0, sv1c); // Added after t in the Application.Iteration.

				Assert.True (t.CanFocus);
				Assert.True (w.CanFocus);
				Assert.False (v1.CanFocus);
				Assert.False (v2.CanFocus);

				Application.Refresh ();
			};
			w.Initialized += (s, e) => {
				wc++;
				Assert.Equal (t.Frame.Width, w.Frame.Width);
				Assert.Equal (t.Frame.Height, w.Frame.Height);
			};
			v1.Initialized += (s, e) => {
				v1c++;
				Assert.Equal (t.Frame.Width, v1.Frame.Width);
				Assert.Equal (t.Frame.Height, v1.Frame.Height);
			};
			v2.Initialized += (s, e) => {
				v2c++;
				Assert.Equal (t.Frame.Width, v2.Frame.Width);
				Assert.Equal (t.Frame.Height, v2.Frame.Height);
			};
			w.Add (v1, v2);
			t.Add (w);

			Application.Iteration = () => {
				var sv1 = new View () { Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill () };

				sv1.Initialized += (s, e) => {
					sv1c++;
					Assert.NotEqual (t.Frame.Width, sv1.Frame.Width);
					Assert.NotEqual (t.Frame.Height, sv1.Frame.Height);
					Assert.False (sv1.CanFocus);
					Assert.Throws<InvalidOperationException> (() => sv1.CanFocus = true);
					Assert.False (sv1.CanFocus);
				};

				v1.Add (sv1);

				Application.Refresh ();
				t.Running = false;
			};

			Application.Run (t);
			Application.Shutdown ();

			Assert.Equal (1, tc);
			Assert.Equal (1, wc);
			Assert.Equal (1, v1c);
			Assert.Equal (1, v2c);
			Assert.Equal (1, sv1c);

			Assert.True (t.CanFocus);
			Assert.True (w.CanFocus);
			Assert.False (v1.CanFocus);
			Assert.False (v2.CanFocus);
		}

		[Fact]
		public void Multi_Thread_Toplevels ()
		{
			Application.Init (new FakeDriver ());

			var t = Application.Top;
			var w = new Window ();
			t.Add (w);

			int count = 0, count1 = 0, count2 = 0;
			bool log = false, log1 = false, log2 = false;
			bool fromTopStillKnowFirstIsRunning = false;
			bool fromTopStillKnowSecondIsRunning = false;
			bool fromFirstStillKnowSecondIsRunning = false;

			Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), (_) => {
				count++;
				if (count1 == 5) {
					log1 = true;
				}
				if (count1 == 14 && count2 == 10 && count == 15) { // count2 is already stopped
					fromTopStillKnowFirstIsRunning = true;
				}
				if (count1 == 7 && count2 == 7 && count == 8) {
					fromTopStillKnowSecondIsRunning = true;
				}
				if (count == 30) {
					Assert.Equal (30, count);
					Assert.Equal (20, count1);
					Assert.Equal (10, count2);

					Assert.True (log);
					Assert.True (log1);
					Assert.True (log2);

					Assert.True (fromTopStillKnowFirstIsRunning);
					Assert.True (fromTopStillKnowSecondIsRunning);
					Assert.True (fromFirstStillKnowSecondIsRunning);

					Application.RequestStop ();
					return false;
				}
				return true;
			});

			t.Ready += FirstDialogToplevel;

			void FirstDialogToplevel (object sender, EventArgs args)
			{
				var od = new OpenDialog ();
				od.Ready += SecondDialogToplevel;

				Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), (_) => {
					count1++;
					if (count2 == 5) {
						log2 = true;
					}
					if (count2 == 4 && count1 == 5 && count == 5) {
						fromFirstStillKnowSecondIsRunning = true;
					}
					if (count1 == 20) {
						Assert.Equal (20, count1);
						Application.RequestStop ();
						return false;
					}
					return true;
				});

				Application.Run (od);
			}

			void SecondDialogToplevel (object sender, EventArgs args)
			{
				var d = new Dialog ();

				Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (100), (_) => {
					count2++;
					if (count < 30) {
						log = true;
					}
					if (count2 == 10) {
						Assert.Equal (10, count2);
						Application.RequestStop ();
						return false;
					}
					return true;
				});

				Application.Run (d);
			}

			Application.Run ();
			Application.Shutdown ();
		}

		[Theory]
		[InlineData (1)]
		[InlineData (2)]
		[InlineData (3)]
		public void LabelChangeText_RendersCorrectly_Constructors (int choice)
		{
			var driver = new FakeDriver ();
			Application.Init (driver);

			try {
				// Create a label with a short text 
				Label lbl;
				var text = "test";

				if (choice == 1) {
					// An object initializer should call the default constructor.
					lbl = new Label { Text = text };
				} else if (choice == 2) {
					// Calling the default constructor followed by the object initializer.
					lbl = new Label () { Text = text };
				} else {
					// Calling the Text constructor.
					lbl = new Label (text);
				}
				Application.Top.Add (lbl);
				Application.Begin (Application.Top);

				// should have the initial text
				Assert.Equal ('t', driver.Contents [0, 0, 0]);
				Assert.Equal ('e', driver.Contents [0, 1, 0]);
				Assert.Equal ('s', driver.Contents [0, 2, 0]);
				Assert.Equal ('t', driver.Contents [0, 3, 0]);
				Assert.Equal (' ', driver.Contents [0, 4, 0]);
			} finally {
				Application.Shutdown ();
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void Internal_Tests ()
		{
			Assert.Equal (new [] { View.Direction.Forward, View.Direction.Backward },
				Enum.GetValues (typeof (View.Direction)));

			var rect = new Rect (1, 1, 10, 1);
			var view = new View (rect);
			var top = Application.Top;
			top.Add (view);

			Assert.Equal (View.Direction.Forward, view.FocusDirection);
			view.FocusDirection = View.Direction.Backward;
			Assert.Equal (View.Direction.Backward, view.FocusDirection);
			Assert.Empty (view.InternalSubviews);
			// BUGBUG: v2 - _needsDisplay needs debugging - test disabled for now.
			//Assert.Equal (new Rect (new Point (0, 0), rect.Size), view._needsDisplay);
			Assert.True (view.LayoutNeeded);
			Assert.False (view._childNeedsDisplay);
			Assert.False (view._addingView);
			view._addingView = true;
			Assert.True (view._addingView);
			view.ViewToScreen (0, 0, out int rcol, out int rrow);
			Assert.Equal (1, rcol);
			Assert.Equal (1, rrow);
			Assert.Equal (rect, view.ViewToScreen (view.Bounds));
			Assert.Equal (top.Bounds, view.ScreenClip (top.Bounds));
			Assert.True (view.LayoutStyle == LayoutStyle.Absolute);

			Application.Begin (top);

			view.Width = Dim.Fill ();
			view.Height = Dim.Fill ();
			Assert.Equal (10, view.Bounds.Width);
			Assert.Equal (1, view.Bounds.Height);
			view.LayoutStyle = LayoutStyle.Computed;
			view.SetRelativeLayout (top.Bounds);
			Assert.Equal (1, view.Frame.X);
			Assert.Equal (1, view.Frame.Y);
			Assert.Equal (79, view.Frame.Width);
			Assert.Equal (24, view.Frame.Height);
			Assert.Equal (0, view.Bounds.X);
			Assert.Equal (0, view.Bounds.Y);
			Assert.Equal (79, view.Bounds.Width);
			Assert.Equal (24, view.Bounds.Height);

			view.X = 0;
			view.Y = 0;
			Assert.Equal ("Absolute(0)", view.X.ToString ());
			Assert.Equal ("Fill(0)", view.Width.ToString ());
			view.SetRelativeLayout (top.Bounds);
			Assert.Equal (0, view.Frame.X);
			Assert.Equal (0, view.Frame.Y);
			Assert.Equal (80, view.Frame.Width);
			Assert.Equal (25, view.Frame.Height);
			Assert.Equal (0, view.Bounds.X);
			Assert.Equal (0, view.Bounds.Y);
			Assert.Equal (80, view.Bounds.Width);
			Assert.Equal (25, view.Bounds.Height);
			bool layoutStarted = false;
			view.LayoutStarted += (s, e) => layoutStarted = true;
			view.OnLayoutStarted (null);
			Assert.True (layoutStarted);
			view.LayoutComplete += (s, e) => layoutStarted = false;
			view.OnLayoutComplete (null);
			Assert.False (layoutStarted);
			view.X = Pos.Center () - 41;
			view.Y = Pos.Center () - 13;
			view.SetRelativeLayout (top.Bounds);
			top.LayoutSubviews (); // BUGBUG: v2 - ??
			view.ViewToScreen (0, 0, out rcol, out rrow);
			Assert.Equal (-41, rcol);
			Assert.Equal (-13, rrow);
		}

		[Fact]
		[AutoInitShutdown]
		public void Visible_Sets_Also_Sets_Subviews ()
		{
			var button = new Button ("Click Me");
			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (button);
			var top = Application.Top;
			top.Add (win);

			var iterations = 0;

			Application.Iteration += () => {
				iterations++;

				Assert.True (button.Visible);
				Assert.True (button.CanFocus);
				Assert.True (button.HasFocus);
				Assert.True (win.Visible);
				Assert.True (win.CanFocus);
				Assert.True (win.HasFocus);
				Assert.True (RunesCount () > 0);

				win.Visible = false;
				Assert.True (button.Visible);
				Assert.True (button.CanFocus);
				Assert.False (button.HasFocus);
				Assert.False (win.Visible);
				Assert.True (win.CanFocus);
				Assert.False (win.HasFocus);
				button.SetFocus ();
				Assert.False (button.HasFocus);
				Assert.False (win.HasFocus);
				win.SetFocus ();
				Assert.False (button.HasFocus);
				Assert.False (win.HasFocus);
				top.Redraw (top.Bounds);
				Assert.True (RunesCount () == 0);

				win.Visible = true;
				win.FocusFirst ();
				Assert.True (button.HasFocus);
				Assert.True (win.HasFocus);
				top.Redraw (top.Bounds);
				Assert.True (RunesCount () > 0);

				Application.RequestStop ();
			};

			Application.Run ();

			Assert.Equal (1, iterations);

			int RunesCount ()
			{
				var contents = ((FakeDriver)Application.Driver).Contents;
				var runesCount = 0;

				for (int i = 0; i < Application.Driver.Rows; i++) {
					for (int j = 0; j < Application.Driver.Cols; j++) {
						if (contents [i, j, 0] != ' ') {
							runesCount++;
						}
					}
				}
				return runesCount;
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void GetTopSuperView_Test ()
		{
			var v1 = new View ();
			var fv1 = new FrameView ();
			fv1.Add (v1);
			var tf1 = new TextField ();
			var w1 = new Window ();
			w1.Add (fv1, tf1);
			var top1 = new Toplevel ();
			top1.Add (w1);

			var v2 = new View ();
			var fv2 = new FrameView ();
			fv2.Add (v2);
			var tf2 = new TextField ();
			var w2 = new Window ();
			w2.Add (fv2, tf2);
			var top2 = new Toplevel ();
			top2.Add (w2);

			Assert.Equal (top1, v1.GetTopSuperView ());
			Assert.Equal (top2, v2.GetTopSuperView ());
		}

		

		[Fact, AutoInitShutdown]
		public void Clear_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
		{
			var view = new FrameView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			view.DrawContent += (s, e) => {
				var savedClip = Application.Driver.Clip;
				Application.Driver.Clip = new Rect (1, 1, view.Bounds.Width, view.Bounds.Height);
				for (int row = 0; row < view.Bounds.Height; row++) {
					Application.Driver.Move (1, row + 1);
					for (int col = 0; col < view.Bounds.Width; col++) {
						Application.Driver.AddStr ($"{col}");
					}
				}
				Application.Driver.Clip = savedClip;
			};
			Application.Top.Add (view);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 20, 10), pos);

			view.Clear (view.Frame);

			expected = @"
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (Rect.Empty, pos);
		}

		[Fact, AutoInitShutdown]
		public void Clear_Bounds_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
		{
			var view = new FrameView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			view.DrawContent += (s, e) => {
				var savedClip = Application.Driver.Clip;
				Application.Driver.Clip = new Rect (1, 1, view.Bounds.Width, view.Bounds.Height);
				for (int row = 0; row < view.Bounds.Height; row++) {
					Application.Driver.Move (1, row + 1);
					for (int col = 0; col < view.Bounds.Width; col++) {
						Application.Driver.AddStr ($"{col}");
					}
				}
				Application.Driver.Clip = savedClip;
			};
			Application.Top.Add (view);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 20, 10), pos);

			view.Clear (view.Frame);

			expected = @"
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (Rect.Empty, pos);
		}

		[Fact, AutoInitShutdown]
		public void GetTextFormatterBoundsSize_GetSizeNeededForText_HotKeySpecifier ()
		{
			var text = "Say Hello 你";
			var horizontalView = new View () {
				Text = text,
				AutoSize = true,
				HotKeySpecifier = '_'
			};

			var verticalView = new View () {
				Text = text,
				AutoSize = true,
				HotKeySpecifier = '_',
				TextDirection = TextDirection.TopBottom_LeftRight
			};
			Application.Top.Add (horizontalView, verticalView);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (50, 50);

			Assert.True (horizontalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 12, 1), horizontalView.Frame);
			Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
			//Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextAndHotKey ());
			//Assert.Equal (horizontalView.TextFormatter.Size, horizontalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

			Assert.True (verticalView.AutoSize);
			// BUGBUG: v2 - Autosize is broken; disabling this test
			//Assert.Equal (new Rect (0, 0, 2, 11), verticalView.Frame);
			//Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
			//Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextAndHotKey ());
			//Assert.Equal (verticalView.TextFormatter.Size, verticalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());

			text = "Say He_llo 你";
			horizontalView.Text = text;
			verticalView.Text = text;

			Assert.True (horizontalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 12, 1), horizontalView.Frame);
			Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
			//Assert.Equal (new Size (13, 1), horizontalView.GetSizeNeededForTextAndHotKey ());
			//Assert.Equal (horizontalView.TextFormatter.Size, horizontalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

			Assert.True (verticalView.AutoSize);
			// BUGBUG: v2 - Autosize is broken; disabling this test
			//Assert.Equal (new Rect (0, 0, 2, 11), verticalView.Frame);
			//Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
			//Assert.Equal (new Size (2, 12), verticalView.GetSizeNeededForTextAndHotKey ());
			//Assert.Equal (verticalView.TextFormatter.Size, verticalView.GetSizeNeededForTextAndHotKey ());
			//Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());
		}

		[Fact]
		public void IsAdded_Added_Removed ()
		{
			var top = new Toplevel ();
			var view = new View ();
			Assert.False (view.IsAdded);
			top.Add (view);
			Assert.True (view.IsAdded);
			top.Remove (view);
			Assert.False (view.IsAdded);
		}

		[Fact, AutoInitShutdown]
		public void Visible_Clear_The_View_Output ()
		{
			var label = new Label ("Testing visibility.");
			var win = new Window ();
			win.Add (label);
			var top = Application.Top;
			top.Add (win);
			Application.Begin (top);

			Assert.True (label.Visible);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────────────────────────┐
│Testing visibility.         │
│                            │
│                            │
└────────────────────────────┘
", output);

			label.Visible = false;
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────────────────────────┐
│                            │
│                            │
│                            │
└────────────────────────────┘
", output);
		}

		[Fact, AutoInitShutdown]
		public void DrawContentComplete_Event_Is_Always_Called ()
		{
			var viewCalled = false;
			var tvCalled = false;

			var view = new View ("View") { Width = 10, Height = 10 };
			view.DrawContentComplete += (s, e) => viewCalled = true;
			var tv = new TextView () { Y = 11, Width = 10, Height = 10 };
			tv.DrawContentComplete += (s, e) => tvCalled = true;

			Application.Top.Add (view, tv);
			Application.Begin (Application.Top);

			Assert.True (viewCalled);
			Assert.True (tvCalled);
		}

		[Fact, AutoInitShutdown]
		public void GetNormalColor_ColorScheme ()
		{
			var view = new View { ColorScheme = Colors.Base };

			Assert.Equal (view.ColorScheme.Normal, view.GetNormalColor ());

			view.Enabled = false;
			Assert.Equal (view.ColorScheme.Disabled, view.GetNormalColor ());
		}

		[Fact, AutoInitShutdown]
		public void GetHotNormalColor_ColorScheme ()
		{
			var view = new View { ColorScheme = Colors.Base };

			Assert.Equal (view.ColorScheme.HotNormal, view.GetHotNormalColor ());

			view.Enabled = false;
			Assert.Equal (view.ColorScheme.Disabled, view.GetHotNormalColor ());
		}

		[Theory, AutoInitShutdown]
		[InlineData (true)]
		[InlineData (false)]
		public void Clear_Does_Not_Spillover_Its_Parent (bool label)
		{
			var root = new View () { Width = 20, Height = 10, ColorScheme = Colors.Base };

			var v = label == true ?
				new Label (new string ('c', 100)) {
					Width = Dim.Fill (),
				} :
				(View)new TextView () {
					Height = 1,
					Text = new string ('c', 100),
					Width = Dim.Fill ()
				};

			root.Add (v);

			Application.Top.Add (root);
			Application.Begin (Application.Top);

			if (label) {
				Assert.True (v.AutoSize);
				Assert.False (v.CanFocus);
				Assert.Equal (new Rect (0, 0, 100, 1), v.Frame);
			} else {
				Assert.False (v.AutoSize);
				Assert.True (v.CanFocus);
				Assert.Equal (new Rect (0, 0, 20, 1), v.Frame);
			}

			TestHelpers.AssertDriverContentsWithFrameAre (@"
cccccccccccccccccccc", output);

			var attributes = new Attribute [] {
				Colors.TopLevel.Normal,
				Colors.Base.Normal,
				Colors.Base.Focus
			};
			if (label) {
				TestHelpers.AssertDriverColorsAre (@"
111111111111111111110
111111111111111111110", attributes);
			} else {
				TestHelpers.AssertDriverColorsAre (@"
222222222222222222220
111111111111111111110", attributes);
			}

			if (label) {
				root.CanFocus = true;
				v.CanFocus = true;
				Assert.False (v.HasFocus);
				v.SetFocus ();
				Assert.True (v.HasFocus);
				Application.Refresh ();
				TestHelpers.AssertDriverColorsAre (@"
222222222222222222220
111111111111111111110", attributes);
			}
		}

		public class DerivedView : View {
			public DerivedView ()
			{
				CanFocus = true;
			}

			public bool IsKeyDown { get; set; }
			public bool IsKeyPress { get; set; }
			public bool IsKeyUp { get; set; }
			public override ustring Text { get; set; }

			public override bool OnKeyDown (KeyEvent keyEvent)
			{
				IsKeyDown = true;
				return true;
			}

			public override bool ProcessKey (KeyEvent keyEvent)
			{
				IsKeyPress = true;
				return true;
			}

			public override bool OnKeyUp (KeyEvent keyEvent)
			{
				IsKeyUp = true;
				return true;
			}

			public override void Redraw (Rect bounds)
			{
				var idx = 0;
				// BUGBUG: v2 - this should use Boudns, not Frame
				for (int r = 0; r < Frame.Height; r++) {
					for (int c = 0; c < Frame.Width; c++) {
						if (idx < Text.Length) {
							var rune = Text [idx];
							if (rune != '\n') {
								AddRune (c, r, Text [idx]);
							}
							idx++;
							if (rune == '\n') {
								break;
							}
						}
					}
				}
				ClearLayoutNeeded ();
				ClearNeedsDisplay ();
			}
		}

		[Fact, AutoInitShutdown]
		public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Frame ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.Frame = new Rect (1, 1, 10, 1);
			Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
			Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
			view.LayoutStyle = LayoutStyle.Absolute;
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplay);
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0     
 A text wit", output);
		}

		[Fact, AutoInitShutdown]
		public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Pos_Dim ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.X = 1;
			view.Y = 1;
			view.Width = 10;
			view.Height = 1;
			Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplay);
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0     
 A text wit", output);
		}

		[Fact, AutoInitShutdown]
		public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Frame ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.Frame = new Rect (1, 1, 10, 1);
			Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
			Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
			view.LayoutStyle = LayoutStyle.Absolute;
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplay);
			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
 A text wit                  
  A text with some long width
   and also with two lines.  ", output);
		}

		[Fact, AutoInitShutdown]
		public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Pos_Dim ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.X = 1;
			view.Y = 1;
			view.Width = 10;
			view.Height = 1;
			Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplay);
			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
 A text wit                  
  A text with some long width
   and also with two lines.  ", output);
		}

		[Fact, AutoInitShutdown]
		public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Frame ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.Frame = new Rect (3, 3, 10, 1);
			Assert.Equal (new Rect (3, 3, 10, 1), view.Frame);
			Assert.Equal (LayoutStyle.Computed, view.LayoutStyle);
			view.LayoutStyle = LayoutStyle.Absolute;
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplay);
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0       
             
             
   A text wit", output);
		}

		[Fact, AutoInitShutdown]
		public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Pos_Dim ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.X = 3;
			view.Y = 3;
			view.Width = 10;
			view.Height = 1;
			Assert.Equal (new Rect (3, 3, 10, 1), view.Frame);
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplay);
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0       
             
             
   A text wit", output);
		}

		[Fact, AutoInitShutdown]
		public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Frame ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.Frame = new Rect (3, 3, 10, 1);
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplay);
			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   A text witith two lines.  ", output);
		}

		[Fact, AutoInitShutdown]
		public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Pos_Dim ()
		{
			var label = new Label ("At 0,0");
			var view = new DerivedView () {
				X = 2,
				Y = 2,
				Width = 30,
				Height = 2,
				Text = "A text with some long width\n and also with two lines."
			};
			var top = Application.Top;
			top.Add (label, view);
			Application.Begin (top);

			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ", output);

			view.X = 3;
			view.Y = 3;
			view.Width = 10;
			view.Height = 1;
			Assert.Equal (new Rect (3, 3, 10, 1), view.Frame);
			Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
			Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplay);
			view.Redraw (view.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
At 0,0                       
                             
  A text with some long width
   A text witith two lines.  ", output);
		}

		[Fact, AutoInitShutdown]
		public void Test_Nested_Views_With_Height_Equal_To_One ()
		{
			var v = new View () { Width = 11, Height = 3, ColorScheme = new ColorScheme () };

			var top = new View () { Width = Dim.Fill (), Height = 1 };
			var bottom = new View () { Width = Dim.Fill (), Height = 1, Y = 2 };

			top.Add (new Label ("111"));
			v.Add (top);
			v.Add (new LineView (Orientation.Horizontal) { Y = 1 });
			bottom.Add (new Label ("222"));
			v.Add (bottom);

			v.LayoutSubviews ();
			v.Redraw (v.Bounds);

			string looksLike =
@"    
111
───────────
222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact]
		[AutoInitShutdown]
		public void Frame_Set_After_Initialize_Update_NeededDisplay ()
		{
			var frame = new FrameView ();

			var label = new Label ("This should be the first line.") {
				TextAlignment = Terminal.Gui.TextAlignment.Centered,
				ColorScheme = Colors.Menu,
				Width = Dim.Fill (),
				X = Pos.Center (),
				Y = Pos.Center () - 2  // center minus 2 minus two lines top and bottom borders equal to zero (4-2-2=0)
			};

			var button = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center ()
			};

			frame.Add (label, button);

			frame.X = Pos.Center ();
			frame.Y = Pos.Center ();
			frame.Width = 40;
			frame.Height = 8;

			var top = Application.Top;

			top.Add (frame);

			Application.Begin (top);

			top.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 80, 25), top._needsDisplay);
			};

			frame.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 40, 8), frame._needsDisplay);
			};

			label.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 38, 1), label._needsDisplay);
			};

			button.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 13, 1), button._needsDisplay);
			};

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
			Assert.Equal (new Rect (20, 8, 40, 8), frame.Frame);
			Assert.Equal (new Rect (20, 8, 60, 16), new Rect (
				frame.Frame.Left, frame.Frame.Top,
				frame.Frame.Right, frame.Frame.Bottom));
			Assert.Equal (new Rect (0, 0, 38, 1), label.Frame);
			Assert.Equal (new Rect (12, 2, 13, 1), button.Frame);
			var expected = @"
                    ┌──────────────────────────────────────┐
                    │    This should be the first line.    │
                    │                                      │
                    │            [ Press me! ]             │
                    │                                      │
                    │                                      │
                    │                                      │
                    └──────────────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact]
		public void Dispose_View ()
		{
			var view = new View ();
			Assert.NotNull (view.Margin);
			Assert.NotNull (view.Border);
			Assert.NotNull (view.Padding);

			view.Dispose ();
			Assert.Null (view.Margin);
			Assert.Null (view.Border);
			Assert.Null (view.Padding);
		}
	}
}
