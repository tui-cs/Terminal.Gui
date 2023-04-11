using System;
using Xunit;
using Xunit.Abstractions;
using NStack;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class NavigationTests {
		readonly ITestOutputHelper output;

		public NavigationTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void FocusNearestView_Ensure_Focus_Ordered ()
		{
			var top = new Toplevel ();

			var win = new Window ();
			var winSubview = new View ("WindowSubview") {
				CanFocus = true
			};
			win.Add (winSubview);
			top.Add (win);

			var frm = new FrameView ();
			var frmSubview = new View ("FrameSubview") {
				CanFocus = true
			};
			frm.Add (frmSubview);
			top.Add (frm);

			top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
			Assert.Equal ($"WindowSubview", top.MostFocused.Text);
			top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
			Assert.Equal ("FrameSubview", top.MostFocused.Text);
			top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ()));
			Assert.Equal ($"WindowSubview", top.MostFocused.Text);

			top.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
			Assert.Equal ("FrameSubview", top.MostFocused.Text);
			top.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ()));
			Assert.Equal ($"WindowSubview", top.MostFocused.Text);
		}
		

		[Fact]
		public void Subviews_TabIndexes_AreEqual ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.Subviews.IndexOf (v2) == 1);
			Assert.True (r.Subviews.IndexOf (v3) == 2);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);

			Assert.Equal (r.Subviews.IndexOf (v1), r.TabIndexes.IndexOf (v1));
			Assert.Equal (r.Subviews.IndexOf (v2), r.TabIndexes.IndexOf (v2));
			Assert.Equal (r.Subviews.IndexOf (v3), r.TabIndexes.IndexOf (v3));
		}

		[Fact]
		public void BringSubviewToFront_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.BringSubviewToFront (v1);
			Assert.True (r.Subviews.IndexOf (v1) == 2);
			Assert.True (r.Subviews.IndexOf (v2) == 0);
			Assert.True (r.Subviews.IndexOf (v3) == 1);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void BringSubviewForward_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.BringSubviewForward (v1);
			Assert.True (r.Subviews.IndexOf (v1) == 1);
			Assert.True (r.Subviews.IndexOf (v2) == 0);
			Assert.True (r.Subviews.IndexOf (v3) == 2);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void SendSubviewToBack_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.SendSubviewToBack (v3);
			Assert.True (r.Subviews.IndexOf (v1) == 1);
			Assert.True (r.Subviews.IndexOf (v2) == 2);
			Assert.True (r.Subviews.IndexOf (v3) == 0);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void SendSubviewBackwards_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.SendSubviewBackwards (v3);
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.Subviews.IndexOf (v2) == 2);
			Assert.True (r.Subviews.IndexOf (v3) == 1);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_ValidValues ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.TabIndex = 1;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 1);

			v1.TabIndex = 2;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 2);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_HigherValues ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.TabIndex = 3;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 2);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_LowerValues ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.TabIndex = -1;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_False ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.CanFocus = false;
			v1.TabIndex = 0;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.Equal (-1, v1.TabIndex);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_False_To_True ()
		{
			var r = new View ();
			var v1 = new View ();
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.CanFocus = true;
			v1.TabIndex = 1;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 1);
		}

		[Fact]
		public void TabStop_And_CanFocus_Are_All_True ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.True (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.True (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.True (v3.HasFocus);
		}

		[Fact]
		public void TabStop_Are_All_True_And_CanFocus_Are_All_False ()
		{
			var r = new View ();
			var v1 = new View ();
			var v2 = new View ();
			var v3 = new View ();

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
		}

		[Fact]
		public void TabStop_Are_All_False_And_CanFocus_Are_All_True ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true, TabStop = false };
			var v2 = new View () { CanFocus = true, TabStop = false };
			var v3 = new View () { CanFocus = true, TabStop = false };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
		}

		[Fact]
		public void TabStop_And_CanFocus_Mixed_And_BothFalse ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true, TabStop = false };
			var v2 = new View () { CanFocus = false, TabStop = true };
			var v3 = new View () { CanFocus = false, TabStop = false };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
		}

		[Fact]
		public void TabStop_All_True_And_Changing_CanFocus_Later ()
		{
			var r = new View ();
			var v1 = new View ();
			var v2 = new View ();
			var v3 = new View ();

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);

			v1.CanFocus = true;
			r.FocusNext ();
			Assert.True (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v2.CanFocus = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.True (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v3.CanFocus = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.True (v3.HasFocus);
		}

		[Fact]
		public void TabStop_All_False_And_All_True_And_Changing_TabStop_Later ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true, TabStop = false };
			var v2 = new View () { CanFocus = true, TabStop = false };
			var v3 = new View () { CanFocus = true, TabStop = false };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);

			v1.TabStop = true;
			r.FocusNext ();
			Assert.True (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v2.TabStop = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.True (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v3.TabStop = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.True (v3.HasFocus);
		}

		[Fact]
		public void CanFocus_Set_Changes_TabIndex_And_TabStop ()
		{
			var r = new View ();
			var v1 = new View ("1");
			var v2 = new View ("2");
			var v3 = new View ("3");

			r.Add (v1, v2, v3);

			v2.CanFocus = true;
			Assert.Equal (r.TabIndexes.IndexOf (v2), v2.TabIndex);
			Assert.Equal (0, v2.TabIndex);
			Assert.True (v2.TabStop);

			v1.CanFocus = true;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			Assert.True (v1.TabStop);

			v1.TabIndex = 2;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			v3.CanFocus = true;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
			Assert.Equal (2, v3.TabIndex);
			Assert.True (v3.TabStop);

			v2.CanFocus = false;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			Assert.True (v1.TabStop);
			Assert.NotEqual (r.TabIndexes.IndexOf (v2), v2.TabIndex);
			Assert.Equal (-1, v2.TabIndex);
			Assert.False (v2.TabStop);
			Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
			Assert.Equal (2, v3.TabIndex);
			Assert.True (v3.TabStop);
		}

		[Fact]
		[AutoInitShutdown]
		public void CanFocus_Faced_With_Container ()
		{
			var t = new Toplevel ();
			var w = new Window ();
			var f = new FrameView ();
			var v = new View () { CanFocus = true };
			f.Add (v);
			w.Add (f);
			t.Add (w);

			Assert.True (t.CanFocus);
			Assert.True (w.CanFocus);
			Assert.True (f.CanFocus);
			Assert.True (v.CanFocus);

			f.CanFocus = false;
			Assert.False (f.CanFocus);
			Assert.True (v.CanFocus);

			v.CanFocus = false;
			Assert.False (f.CanFocus);
			Assert.False (v.CanFocus);

			v.CanFocus = true;
			Assert.False (f.CanFocus);
			Assert.True (v.CanFocus);
		}

		[Fact]
		public void CanFocus_Faced_With_Container_Before_Run ()
		{
			Application.Init (new FakeDriver ());

			var t = Application.Top;

			var w = new Window ();
			var f = new FrameView ();
			var v = new View () { CanFocus = true };
			f.Add (v);
			w.Add (f);
			t.Add (w);

			Assert.True (t.CanFocus);
			Assert.True (w.CanFocus);
			Assert.True (f.CanFocus);
			Assert.True (v.CanFocus);

			f.CanFocus = false;
			Assert.False (f.CanFocus);
			Assert.True (v.CanFocus);

			v.CanFocus = false;
			Assert.False (f.CanFocus);
			Assert.False (v.CanFocus);

			v.CanFocus = true;
			Assert.False (f.CanFocus);
			Assert.True (v.CanFocus);

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void CanFocus_Faced_With_Container_After_Run ()
		{
			Application.Init (new FakeDriver ());

			var t = Application.Top;

			var w = new Window ();
			var f = new FrameView ();
			var v = new View () { CanFocus = true };
			f.Add (v);
			w.Add (f);
			t.Add (w);

			t.Ready += (s, e) => {
				Assert.True (t.CanFocus);
				Assert.True (w.CanFocus);
				Assert.True (f.CanFocus);
				Assert.True (v.CanFocus);

				f.CanFocus = false;
				Assert.False (f.CanFocus);
				Assert.False (v.CanFocus);

				v.CanFocus = false;
				Assert.False (f.CanFocus);
				Assert.False (v.CanFocus);

				Assert.Throws<InvalidOperationException> (() => v.CanFocus = true);
				Assert.False (f.CanFocus);
				Assert.False (v.CanFocus);

				f.CanFocus = true;
				Assert.True (f.CanFocus);
				Assert.True (v.CanFocus);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void CanFocus_Container_ToFalse_Turns_All_Subviews_ToFalse_Too ()
		{
			Application.Init (new FakeDriver ());

			var t = Application.Top;

			var w = new Window ();
			var f = new FrameView ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			f.Add (v1, v2);
			w.Add (f);
			t.Add (w);

			t.Ready += (s, e) => {
				Assert.True (t.CanFocus);
				Assert.True (w.CanFocus);
				Assert.True (f.CanFocus);
				Assert.True (v1.CanFocus);
				Assert.True (v2.CanFocus);

				w.CanFocus = false;
				Assert.False (w.CanFocus);
				Assert.False (f.CanFocus);
				Assert.False (v1.CanFocus);
				Assert.False (v2.CanFocus);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void CanFocus_Container_Toggling_All_Subviews_To_Old_Value_When_Is_True ()
		{
			Application.Init (new FakeDriver ());

			var t = Application.Top;

			var w = new Window ();
			var f = new FrameView ();
			var v1 = new View ();
			var v2 = new View () { CanFocus = true };
			f.Add (v1, v2);
			w.Add (f);
			t.Add (w);

			t.Ready += (s, e) => {
				Assert.True (t.CanFocus);
				Assert.True (w.CanFocus);
				Assert.True (f.CanFocus);
				Assert.False (v1.CanFocus);
				Assert.True (v2.CanFocus);

				w.CanFocus = false;
				Assert.False (w.CanFocus);
				Assert.False (f.CanFocus);
				Assert.False (v1.CanFocus);
				Assert.False (v2.CanFocus);

				w.CanFocus = true;
				Assert.True (w.CanFocus);
				Assert.True (f.CanFocus);
				Assert.False (v1.CanFocus);
				Assert.True (v2.CanFocus);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}


		[Fact]
		public void Navigation_With_Null_Focused_View ()
		{
			// Non-regression test for #882 (NullReferenceException during keyboard navigation when Focused is null)

			Application.Init (new FakeDriver ());

			Application.Top.Ready += (s, e) => {
				Assert.Null (Application.Top.Focused);
			};

			// Keyboard navigation with tab
			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('\t', ConsoleKey.Tab, false, false, false));

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void Enabled_False_Sets_HasFocus_To_False ()
		{
			var wasClicked = false;
			var view = new Button ("Click Me");
			view.Clicked += (s, e) => wasClicked = !wasClicked;
			Application.Top.Add (view);

			view.ProcessKey (new KeyEvent (Key.Enter, null));
			Assert.True (wasClicked);
			view.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked });
			Assert.False (wasClicked);
			Assert.True (view.Enabled);
			Assert.True (view.CanFocus);
			Assert.True (view.HasFocus);

			view.Enabled = false;
			view.ProcessKey (new KeyEvent (Key.Enter, null));
			Assert.False (wasClicked);
			view.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked });
			Assert.False (wasClicked);
			Assert.False (view.Enabled);
			Assert.True (view.CanFocus);
			Assert.False (view.HasFocus);
			view.SetFocus ();
			Assert.False (view.HasFocus);
		}

		[Fact]
		[AutoInitShutdown]
		public void Enabled_Sets_Also_Sets_Subviews ()
		{
			var wasClicked = false;
			var button = new Button ("Click Me");
			button.Clicked += (s, e) => wasClicked = !wasClicked;
			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (button);
			Application.Top.Add (win);

			var iterations = 0;

			Application.Iteration += () => {
				iterations++;

				button.ProcessKey (new KeyEvent (Key.Enter, null));
				Assert.True (wasClicked);
				button.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked });
				Assert.False (wasClicked);
				Assert.True (button.Enabled);
				Assert.True (button.CanFocus);
				Assert.True (button.HasFocus);
				Assert.True (win.Enabled);
				Assert.True (win.CanFocus);
				Assert.True (win.HasFocus);

				win.Enabled = false;
				button.ProcessKey (new KeyEvent (Key.Enter, null));
				Assert.False (wasClicked);
				button.MouseEvent (new MouseEvent () { Flags = MouseFlags.Button1Clicked });
				Assert.False (wasClicked);
				Assert.False (button.Enabled);
				Assert.True (button.CanFocus);
				Assert.False (button.HasFocus);
				Assert.False (win.Enabled);
				Assert.True (win.CanFocus);
				Assert.False (win.HasFocus);
				button.SetFocus ();
				Assert.False (button.HasFocus);
				Assert.False (win.HasFocus);
				win.SetFocus ();
				Assert.False (button.HasFocus);
				Assert.False (win.HasFocus);

				win.Enabled = true;
				win.FocusFirst ();
				Assert.True (button.HasFocus);
				Assert.True (win.HasFocus);

				Application.RequestStop ();
			};

			Application.Run ();

			Assert.Equal (1, iterations);
		}


		[Fact]
		[AutoInitShutdown]
		public void CanFocus_Sets_To_False_Does_Not_Sets_HasFocus_To_True ()
		{
			var view = new View () { CanFocus = true };
			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);

			Assert.True (view.CanFocus);
			Assert.True (view.HasFocus);

			view.CanFocus = false;
			Assert.False (view.CanFocus);
			Assert.False (view.HasFocus);
			Assert.Null (Application.Current.Focused);
			Assert.Null (Application.Current.MostFocused);
		}

		[Fact]
		[AutoInitShutdown]
		public void CanFocus_Sets_To_False_On_Single_View_Focus_View_On_Another_Toplevel ()
		{
			var view1 = new View () { Id = "view1", Width = 10, Height = 1, CanFocus = true };
			var win1 = new Window () { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
			win1.Add (view1);
			var view2 = new View () { Id = "view2", Width = 20, Height = 2, CanFocus = true };
			var win2 = new Window () { Id = "win2", X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
			win2.Add (view2);
			Application.Top.Add (win1, win2);
			Application.Begin (Application.Top);

			Assert.True (view1.CanFocus);
			Assert.True (view1.HasFocus);
			Assert.True (view2.CanFocus);
			Assert.False (view2.HasFocus);

			view1.CanFocus = false;
			Assert.False (view1.CanFocus);
			Assert.False (view1.HasFocus);
			Assert.Equal (win2, Application.Current.Focused);
			Assert.Equal (view2, Application.Current.MostFocused);
		}

		[Fact]
		[AutoInitShutdown]
		public void CanFocus_Sets_To_False_With_Two_Views_Focus_Another_View_On_The_Same_Toplevel ()
		{
			var view1 = new View () { Id = "view1", Width = 10, Height = 1, CanFocus = true };
			var view12 = new View () { Id = "view12", Y = 5, Width = 10, Height = 1, CanFocus = true };
			var win1 = new Window () { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
			win1.Add (view1, view12);
			var view2 = new View () { Id = "view2", Width = 20, Height = 2, CanFocus = true };
			var win2 = new Window () { Id = "win2", X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
			win2.Add (view2);
			Application.Top.Add (win1, win2);
			Application.Begin (Application.Top);

			Assert.True (view1.CanFocus);
			Assert.True (view1.HasFocus);
			Assert.True (view2.CanFocus);
			Assert.False (view2.HasFocus);

			view1.CanFocus = false;
			Assert.False (view1.CanFocus);
			Assert.False (view1.HasFocus);
			Assert.Equal (win1, Application.Current.Focused);
			Assert.Equal (view12, Application.Current.MostFocused);
		}

		[Fact]
		[AutoInitShutdown]
		public void CanFocus_Sets_To_False_On_Toplevel_Focus_View_On_Another_Toplevel ()
		{
			var view1 = new View () { Id = "view1", Width = 10, Height = 1, CanFocus = true };
			var win1 = new Window () { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
			win1.Add (view1);
			var view2 = new View () { Id = "view2", Width = 20, Height = 2, CanFocus = true };
			var win2 = new Window () { Id = "win2", X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
			win2.Add (view2);
			Application.Top.Add (win1, win2);
			Application.Begin (Application.Top);

			Assert.True (view1.CanFocus);
			Assert.True (view1.HasFocus);
			Assert.True (view2.CanFocus);
			Assert.False (view2.HasFocus);

			win1.CanFocus = false;
			Assert.False (view1.CanFocus);
			Assert.False (view1.HasFocus);
			Assert.False (win1.CanFocus);
			Assert.False (win1.HasFocus);
			Assert.Equal (win2, Application.Current.Focused);
			Assert.Equal (view2, Application.Current.MostFocused);
		}

		[Fact]
		[AutoInitShutdown]
		public void ProcessHotKey_Will_Invoke_ProcessKey_Only_For_The_MostFocused_With_Top_KeyPress_Event ()
		{
			var sbQuiting = false;
			var tfQuiting = false;
			var topQuiting = false;
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => sbQuiting = true )
			});
			var tf = new TextField ();
			tf.KeyPress += Tf_KeyPress;

			void Tf_KeyPress (object sender, KeyEventEventArgs obj)
			{
				if (obj.KeyEvent.Key == (Key.Q | Key.CtrlMask)) {
					obj.Handled = tfQuiting = true;
				}
			}

			var win = new Window ();
			win.Add (sb, tf);
			var top = Application.Top;
			top.KeyPress += Top_KeyPress;

			void Top_KeyPress (object sender, KeyEventEventArgs obj)
			{
				if (obj.KeyEvent.Key == (Key.Q | Key.CtrlMask)) {
					obj.Handled = topQuiting = true;
				}
			}

			top.Add (win);
			Application.Begin (top);

			Assert.False (sbQuiting);
			Assert.False (tfQuiting);
			Assert.False (topQuiting);

			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Assert.False (sbQuiting);
			Assert.True (tfQuiting);
			Assert.False (topQuiting);

			tf.KeyPress -= Tf_KeyPress;
			tfQuiting = false;
			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Application.MainLoop.MainIteration ();
			Assert.True (sbQuiting);
			Assert.False (tfQuiting);
			Assert.False (topQuiting);

			sb.RemoveItem (0);
			sbQuiting = false;
			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Application.MainLoop.MainIteration ();
			Assert.False (sbQuiting);
			Assert.False (tfQuiting);
			Assert.True (topQuiting);
		}

		[Fact]
		[AutoInitShutdown]
		public void ProcessHotKey_Will_Invoke_ProcessKey_Only_For_The_MostFocused_Without_Top_KeyPress_Event ()
		{
			var sbQuiting = false;
			var tfQuiting = false;
			var sb = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => sbQuiting = true )
			});
			var tf = new TextField ();
			tf.KeyPress += Tf_KeyPress;

			void Tf_KeyPress (object sender, KeyEventEventArgs obj)
			{
				if (obj.KeyEvent.Key == (Key.Q | Key.CtrlMask)) {
					obj.Handled = tfQuiting = true;
				}
			}

			var win = new Window ();
			win.Add (sb, tf);
			var top = Application.Top;
			top.Add (win);
			Application.Begin (top);

			Assert.False (sbQuiting);
			Assert.False (tfQuiting);

			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Assert.False (sbQuiting);
			Assert.True (tfQuiting);

			tf.KeyPress -= Tf_KeyPress;
			tfQuiting = false;
			Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
			Application.MainLoop.MainIteration ();
			Assert.True (sbQuiting);
			Assert.False (tfQuiting);
		}

		[Fact]
		[AutoInitShutdown]
		public void WindowDispose_CanFocusProblem ()
		{
			// Arrange
			Application.Init ();
			using var top = Toplevel.Create ();
			using var view = new View (
				x: 0,
				y: 1,
				text: nameof (WindowDispose_CanFocusProblem));
			using var window = new Window ();
			top.Add (window);
			window.Add (view);

			// Act
			Application.Begin (top);
			Application.Shutdown ();

			// Assert does Not throw NullReferenceException
			top.SetFocus ();
		}
		
		[Fact, AutoInitShutdown]
		public void SetHasFocus_Do_Not_Throws_If_OnLeave_Remove_Focused_Changing_To_Null ()
		{
			var view1Leave = false;
			var subView1Leave = false;
			var subView1subView1Leave = false;
			var top = Application.Top;
			var view1 = new View { CanFocus = true };
			var subView1 = new View { CanFocus = true };
			var subView1subView1 = new View { CanFocus = true };
			view1.Leave += (s, e) => {
				view1Leave = true;
			};
			subView1.Leave += (s, e) => {
				subView1.Remove (subView1subView1);
				subView1Leave = true;
			};
			view1.Add (subView1);
			subView1subView1.Leave += (s, e) => {
				// This is never invoked
				subView1subView1Leave = true;
			};
			subView1.Add (subView1subView1);
			var view2 = new View { CanFocus = true };
			top.Add (view1, view2);
			Application.Begin (top);

			view2.SetFocus ();
			Assert.True (view1Leave);
			Assert.True (subView1Leave);
			Assert.False (subView1subView1Leave);
		}
		
		[Fact, AutoInitShutdown]
		public void Remove_Does_Not_Change_Focus ()
		{
			Assert.True (Application.Top.CanFocus);
			Assert.False (Application.Top.HasFocus);

			var container = new View () { Width = 10, Height = 10 };
			var leave = false;
			container.Leave += (s, e) => leave = true;
			Assert.False (container.CanFocus);
			var child = new View () { Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
			container.Add (child);

			Assert.True (container.CanFocus);
			Assert.False (container.HasFocus);
			Assert.True (child.CanFocus);
			Assert.False (child.HasFocus);

			Application.Top.Add (container);
			Application.Begin (Application.Top);

			Assert.True (Application.Top.CanFocus);
			Assert.True (Application.Top.HasFocus);
			Assert.True (container.CanFocus);
			Assert.True (container.HasFocus);
			Assert.True (child.CanFocus);
			Assert.True (child.HasFocus);

			container.Remove (child);
			child.Dispose ();
			child = null;
			Assert.True (Application.Top.HasFocus);
			Assert.True (container.CanFocus);
			Assert.True (container.HasFocus);
			Assert.Null (child);
			Assert.False (leave);
		}

		[Fact, AutoInitShutdown]
		public void SetFocus_View_With_Null_Superview_Does_Not_Throw_Exception ()
		{
			Assert.True (Application.Top.CanFocus);
			Assert.False (Application.Top.HasFocus);

			var exception = Record.Exception (Application.Top.SetFocus);
			Assert.Null (exception);
			Assert.True (Application.Top.CanFocus);
			Assert.True (Application.Top.HasFocus);
		}

		[Fact, AutoInitShutdown]
		public void FocusNext_Does_Not_Throws_If_A_View_Was_Removed_From_The_Collection ()
		{
			var top1 = Application.Top;
			var view1 = new View () { Id = "view1", Width = 10, Height = 5, CanFocus = true };
			var top2 = new Toplevel () { Id = "top2", Y = 1, Width = 10, Height = 5 };
			var view2 = new View () { Id = "view2", Y = 1, Width = 10, Height = 5, CanFocus = true };
			View view3 = null;
			var removed = false;
			view2.Enter += (s, e) => {
				if (!removed) {
					removed = true;
					view3 = new View () { Id = "view3", Y = 1, Width = 10, Height = 5 };
					Application.Current.Add (view3);
					Application.Current.BringSubviewToFront (view3);
					Assert.False (view3.HasFocus);
				}
			};
			view2.Leave += (s, e) => {
				Application.Current.Remove (view3);
				view3.Dispose ();
				view3 = null;
			};
			top2.Add (view2);
			top1.Add (view1, top2);
			Application.Begin (top1);

			Assert.True (top1.HasFocus);
			Assert.True (view1.HasFocus);
			Assert.False (view2.HasFocus);
			Assert.False (removed);
			Assert.Null (view3);

			Assert.True (top1.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers { Ctrl = true })));
			Assert.True (top1.HasFocus);
			Assert.False (view1.HasFocus);
			Assert.True (view2.HasFocus);
			Assert.True (removed);
			Assert.NotNull (view3);

			var exception = Record.Exception (() => top1.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers { Ctrl = true })));
			Assert.Null (exception);
			Assert.True (removed);
			Assert.Null (view3);
		}
	}
}
