using NStack;
using System;
using System.Collections.Generic;
using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.CoreTests {
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
			Assert.Equal ("View()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
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
			Assert.Equal ("View()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
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
			Assert.Equal ("View()({X=1,Y=2,Width=3,Height=4})", r.ToString ());
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
			Assert.Equal ("View()({X=0,Y=0,Width=1,Height=13})", r.ToString ());
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
			Assert.Empty (r.Id);
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
		public void Added_Removed ()
		{
			var v = new View (new Rect (0, 0, 10, 24));
			var t = new View ();

			v.Added += (s,e) => {
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
		public void Initialized_Event_Comparing_With_Added_Event ()
		{
			Application.Init (new FakeDriver ());

			var t = new Toplevel () { Id = "0", };

			var w = new Window () { Id = "t", Width = Dim.Fill (), Height = Dim.Fill () };
			var v1 = new View () { Id = "v1", Width = Dim.Fill (), Height = Dim.Fill () };
			var v2 = new View () { Id = "v2", Width = Dim.Fill (), Height = Dim.Fill () };
			var sv1 = new View () { Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill () };

			int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

			w.Added += (s,e) => {
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

			var w = new Window ("w");
			var f = new FrameView ("f");
			var v = new View ("v") { CanFocus = true };
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

			var w = new Window ("w");
			var f = new FrameView ("f");
			var v = new View ("v") { CanFocus = true };
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

			var w = new Window ("w");
			var f = new FrameView ("f");
			var v1 = new View ("v1") { CanFocus = true };
			var v2 = new View ("v2") { CanFocus = true };
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

			var w = new Window ("w");
			var f = new FrameView ("f");
			var v1 = new View ("v1");
			var v2 = new View ("v2") { CanFocus = true };
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

		[Fact]
		public void KeyPress_Handled_To_True_Prevents_Changes ()
		{
			Application.Init (new FakeDriver ());

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('N', ConsoleKey.N, false, false, false));

			var top = Application.Top;

			var text = new TextField ("");
			text.KeyPress += (s, e) => {
				e.Handled = true;
				Assert.True (e.Handled);
				Assert.Equal (Key.N, e.KeyEvent.Key);
			};
			top.Add (text);

			Application.Iteration += () => {
				Console.MockKeyPresses.Push (new ConsoleKeyInfo ('N', ConsoleKey.N, false, false, false));
				Assert.Equal ("", text.Text);

				Application.RequestStop ();
			};

			Application.Run ();

			// Shutdown must be called to safely clean up Application if Init has been called
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
				Application.Top.Redraw (Application.Top.Bounds);

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
			Assert.Equal (new Rect (new Point (0, 0), rect.Size), view.NeedDisplay);
			Assert.True (view.LayoutNeeded);
			Assert.False (view.ChildNeedsDisplay);
			Assert.False (view.addingView);
			view.addingView = true;
			Assert.True (view.addingView);
			view.ViewToScreen (0, 0, out int rcol, out int rrow);
			Assert.Equal (1, rcol);
			Assert.Equal (1, rrow);
			Assert.Equal (rect, view.ViewToScreen (view.Bounds));
			Assert.Equal (top.Bounds, view.ScreenClip (top.Bounds));
			view.Width = Dim.Fill ();
			view.Height = Dim.Fill ();
			Assert.Equal (10, view.Bounds.Width);
			Assert.Equal (1, view.Bounds.Height);
			view.SetRelativeLayout (top.Bounds);
			Assert.Equal (79, view.Bounds.Width);
			Assert.Equal (24, view.Bounds.Height);
			view.X = 0;
			view.Y = 0;
			view.SetRelativeLayout (top.Bounds);
			Assert.Equal (80, view.Bounds.Width);
			Assert.Equal (25, view.Bounds.Height);
			bool layoutStarted = false;
			view.LayoutStarted += (s,e) => layoutStarted = true;
			view.OnLayoutStarted (null);
			Assert.True (layoutStarted);
			view.LayoutComplete += (s,e) => layoutStarted = false;
			view.OnLayoutComplete (null);
			Assert.False (layoutStarted);
			view.X = Pos.Center () - 41;
			view.Y = Pos.Center () - 13;
			view.SetRelativeLayout (top.Bounds);
			view.ViewToScreen (0, 0, out rcol, out rrow);
			Assert.Equal (-41, rcol);
			Assert.Equal (-13, rrow);
		}

		[Fact]
		[AutoInitShutdown]
		public void Enabled_False_Sets_HasFocus_To_False ()
		{
			var wasClicked = false;
			var view = new Button ("Click Me");
			view.Clicked += (s,e) => wasClicked = !wasClicked;
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
			button.Clicked += (s,e) => wasClicked = !wasClicked;
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
			var view1 = new View () { Width = 10, Height = 1, CanFocus = true };
			var win1 = new Window () { Width = Dim.Percent (50), Height = Dim.Fill () };
			win1.Add (view1);
			var view2 = new View () { Width = 20, Height = 2, CanFocus = true };
			var win2 = new Window () { X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
			win2.Add (view2);
			Application.Top.Add (win1, win2);
			Application.Begin (Application.Top);

			Assert.True (view1.CanFocus);
			Assert.True (view1.HasFocus);
			Assert.True (view2.CanFocus);
			Assert.True (view2.HasFocus);

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
			var view1 = new View () { Width = 10, Height = 1, CanFocus = true };
			var view12 = new View () { Y = 5, Width = 10, Height = 1, CanFocus = true };
			var win1 = new Window () { Width = Dim.Percent (50), Height = Dim.Fill () };
			win1.Add (view1, view12);
			var view2 = new View () { Width = 20, Height = 2, CanFocus = true };
			var win2 = new Window () { X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
			win2.Add (view2);
			Application.Top.Add (win1, win2);
			Application.Begin (Application.Top);

			Assert.True (view1.CanFocus);
			Assert.True (view1.HasFocus);
			Assert.True (view2.CanFocus);
			Assert.True (view2.HasFocus);

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
			var view1 = new View () { Width = 10, Height = 1, CanFocus = true };
			var win1 = new Window () { Width = Dim.Percent (50), Height = Dim.Fill () };
			win1.Add (view1);
			var view2 = new View () { Width = 20, Height = 2, CanFocus = true };
			var win2 = new Window () { X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
			win2.Add (view2);
			Application.Top.Add (win1, win2);
			Application.Begin (Application.Top);

			Assert.True (view1.CanFocus);
			Assert.True (view1.HasFocus);
			Assert.True (view2.CanFocus);
			Assert.True (view2.HasFocus);

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
		public void DrawFrame_With_Positive_Positions ()
		{
			var view = new View (new Rect (0, 0, 8, 4));

			view.DrawContent += (s, e) => view.DrawFrame (view.Bounds, 0, true);

			Assert.Equal (Point.Empty, new Point (view.Frame.X, view.Frame.Y));
			Assert.Equal (new Size (8, 4), new Size (view.Frame.Width, view.Frame.Height));

			Application.Top.Add (view);
			Application.Begin (Application.Top);

			var expected = @"
┌──────┐
│      │
│      │
└──────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 8, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_With_Minimum_Size ()
		{
			var view = new View (new Rect (0, 0, 2, 2));

			view.DrawContent += (s, e) => view.DrawFrame (view.Bounds, 0, true);

			Assert.Equal (Point.Empty, new Point (view.Frame.X, view.Frame.Y));
			Assert.Equal (new Size (2, 2), new Size (view.Frame.Width, view.Frame.Height));

			Application.Top.Add (view);
			Application.Begin (Application.Top);

			var expected = @"
┌┐
└┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 2, 2), pos);
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_With_Negative_Positions ()
		{
			var view = new View (new Rect (-1, 0, 8, 4));

			view.DrawContent += (s, e) => view.DrawFrame (view.Bounds, 0, true);

			Assert.Equal (new Point (-1, 0), new Point (view.Frame.X, view.Frame.Y));
			Assert.Equal (new Size (8, 4), new Size (view.Frame.Width, view.Frame.Height));

			Application.Top.Add (view);
			Application.Begin (Application.Top);

			var expected = @"
──────┐
      │
      │
──────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 7, 4), pos);

			view.Frame = new Rect (-1, -1, 8, 4);
			Application.Refresh ();

			expected = @"
      │
      │
──────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (6, 0, 7, 3), pos);

			view.Frame = new Rect (0, 0, 8, 4);
			((FakeDriver)Application.Driver).SetBufferSize (7, 4);

			expected = @"
┌──────
│      
│      
└──────
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 7, 4), pos);

			view.Frame = new Rect (0, 0, 8, 4);
			((FakeDriver)Application.Driver).SetBufferSize (7, 3);
		}

		[Fact, AutoInitShutdown]
		public void DrawTextFormatter_Respects_The_Clip_Bounds ()
		{
			var view = new View (new Rect (0, 0, 20, 20));
			view.Add (new Label ("0123456789abcdefghij"));
			view.Add (new Label (0, 1, "1\n2\n3\n4\n5\n6\n7\n8\n9\n0"));
			view.Add (new Button (1, 1, "Press me!"));
			var scrollView = new ScrollView (new Rect (1, 1, 15, 10)) {
				ContentSize = new Size (40, 40),
				ShowHorizontalScrollIndicator = true,
				ShowVerticalScrollIndicator = true
			};
			scrollView.Add (view);
			var win = new Window (new Rect (1, 1, 20, 14), "Test");
			win.Add (scrollView);
			Application.Top.Add (win);
			Application.Begin (Application.Top);

			var expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 0123456789abcd▲  │
 │ 1[ Press me! ]┬  │
 │ 2             │  │
 │ 3             ┴  │
 │ 4             ░  │
 │ 5             ░  │
 │ 6             ░  │
 │ 7             ░  │
 │ 8             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 123456789abcde▲  │
 │ [ Press me! ] ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 23456789abcdef▲  │
 │  Press me! ]  ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄├────┤░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 3456789abcdefg▲  │
 │ Press me! ]   ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄├────┤░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 456789abcdefgh▲  │
 │ ress me! ]    ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░├───┤░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 56789abcdefghi▲  │
 │ ess me! ]     ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░├────┤░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 6789abcdefghij▲  │
 │ ss me! ]      ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░├────┤░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 789abcdefghij ▲  │
 │ s me! ]       ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░░├───┤░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Home, new KeyModifiers ())));
			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 1[ Press me! ]▲  │
 │ 2             ┬  │
 │ 3             │  │
 │ 4             ┴  │
 │ 5             ░  │
 │ 6             ░  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 2             ▲  │
 │ 3             ┬  │
 │ 4             │  │
 │ 5             ┴  │
 │ 6             ░  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ░  │
 │ 0             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌ Test ────────────┐
 │                  │
 │ 3             ▲  │
 │ 4             ┬  │
 │ 5             │  │
 │ 6             ┴  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ░  │
 │ 0             ░  │
 │               ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);
		}

		[Fact, AutoInitShutdown]
		public void Clear_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
		{
			var view = new View () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			view.DrawContent += (s,e) => {
				view.DrawFrame (view.Bounds);
				var savedClip = Application.Driver.Clip;
				Application.Driver.Clip = new Rect (1, 1, view.Bounds.Width - 2, view.Bounds.Height - 2);
				for (int row = 0; row < view.Bounds.Height - 2; row++) {
					Application.Driver.Move (1, row + 1);
					for (int col = 0; col < view.Bounds.Width - 2; col++) {
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

			view.Clear ();

			expected = @"
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (Rect.Empty, pos);
		}

		[Fact, AutoInitShutdown]
		public void Clear_Bounds_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
		{
			var view = new View () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			view.DrawContent += (s,e) => {
				view.DrawFrame (view.Bounds);
				var savedClip = Application.Driver.Clip;
				Application.Driver.Clip = new Rect (1, 1, view.Bounds.Width - 2, view.Bounds.Height - 2);
				for (int row = 0; row < view.Bounds.Height - 2; row++) {
					Application.Driver.Move (1, row + 1);
					for (int col = 0; col < view.Bounds.Width - 2; col++) {
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

			view.Clear (view.Bounds);

			expected = @"
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (Rect.Empty, pos);
		}

		[Fact]
		public void GetTextFormatterBoundsSize_GetSizeNeededForText_HotKeySpecifier ()
		{
			var text = "Say Hello 你";
			var horizontalView = new View () { Text = text, AutoSize = true, HotKeySpecifier = '_' };
			var verticalView = new View () {
				Text = text,
				AutoSize = true,
				HotKeySpecifier = '_',
				TextDirection = TextDirection.TopBottom_LeftRight
			};

			Assert.True (horizontalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 12, 1), horizontalView.Frame);
			Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
			Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (horizontalView.TextFormatter.Size, horizontalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

			Assert.True (verticalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, 11), verticalView.Frame);
			Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
			Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (verticalView.TextFormatter.Size, verticalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());

			text = "Say He_llo 你";
			horizontalView.Text = text;
			verticalView.Text = text;

			Assert.True (horizontalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 12, 1), horizontalView.Frame);
			Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
			Assert.Equal (new Size (13, 1), horizontalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (horizontalView.TextFormatter.Size, horizontalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

			Assert.True (verticalView.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, 11), verticalView.Frame);
			Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
			Assert.Equal (new Size (2, 12), verticalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (verticalView.TextFormatter.Size, verticalView.GetSizeNeededForTextAndHotKey ());
			Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());
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
		public void ClearOnVisibleFalse_Gets_Sets ()
		{
			var text = "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
			var label = new Label (text);
			Application.Top.Add (label);

			var sbv = new ScrollBarView (label, true, false) {
				Size = 100,
				ClearOnVisibleFalse = false
			};
			Application.Begin (Application.Top);

			Assert.True (sbv.Visible);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes░
This is a tes▼
", output);

			sbv.Visible = false;
			Assert.False (sbv.Visible);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a test
This is a test
This is a test
This is a test
This is a test
This is a test
", output);

			sbv.Visible = true;
			Assert.True (sbv.Visible);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes░
This is a tes▼
", output);

			sbv.ClearOnVisibleFalse = true;
			sbv.Visible = false;
			Assert.False (sbv.Visible);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a tes
This is a tes
This is a tes
This is a tes
This is a tes
This is a tes
", output);
		}

		[Fact, AutoInitShutdown]
		public void DrawContentComplete_Event_Is_Always_Called ()
		{
			var viewCalled = false;
			var tvCalled = false;

			var view = new View ("View") { Width = 10, Height = 10 };
			view.DrawContentComplete += (s,e) => viewCalled = true;
			var tv = new TextView () { Y = 11, Width = 10, Height = 10 };
			tv.DrawContentComplete += (s, e) => tvCalled = true;

			Application.Top.Add (view, tv);
			Application.Begin (Application.Top);

			Assert.True (viewCalled);
			Assert.True (tvCalled);
		}

		[Fact, AutoInitShutdown]
		public void KeyDown_And_KeyUp_Events_Must_Called_Before_OnKeyDown_And_OnKeyUp ()
		{
			var keyDown = false;
			var keyPress = false;
			var keyUp = false;

			var view = new DerivedView ();
			view.KeyDown += (s, e) => {
				Assert.Equal (Key.a, e.KeyEvent.Key);
				Assert.False (keyDown);
				Assert.False (view.IsKeyDown);
				e.Handled = true;
				keyDown = true;
			};
			view.KeyPress += (s,e) => {
				Assert.Equal (Key.a, e.KeyEvent.Key);
				Assert.False (keyPress);
				Assert.False (view.IsKeyPress);
				e.Handled = true;
				keyPress = true;
			};
			view.KeyUp += (s, e) => {
				Assert.Equal (Key.a, e.KeyEvent.Key);
				Assert.False (keyUp);
				Assert.False (view.IsKeyUp);
				e.Handled = true;
				keyUp = true;
			};

			Application.Top.Add (view);

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false));

			Application.Iteration += () => Application.RequestStop ();

			Assert.True (view.CanFocus);

			Application.Run ();
			Application.Shutdown ();

			Assert.True (keyDown);
			Assert.True (keyPress);
			Assert.True (keyUp);
			Assert.False (view.IsKeyDown);
			Assert.False (view.IsKeyPress);
			Assert.False (view.IsKeyUp);
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

		[Theory, AutoInitShutdown]
		[InlineData (true, false, false)]
		[InlineData (true, true, false)]
		[InlineData (true, true, true)]
		public void KeyDown_And_KeyUp_Events_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
		{
			var keyDown = false;
			var keyPress = false;
			var keyUp = false;

			var view = new DerivedView ();
			view.KeyDown += (s,e) => {
				Assert.Equal (-1, e.KeyEvent.KeyValue);
				Assert.Equal (shift, e.KeyEvent.IsShift);
				Assert.Equal (alt, e.KeyEvent.IsAlt);
				Assert.Equal (control, e.KeyEvent.IsCtrl);
				Assert.False (keyDown);
				Assert.False (view.IsKeyDown);
				keyDown = true;
			};
			view.KeyPress += (s, e) => {
				keyPress = true;
			};
			view.KeyUp += (s, e) => {
				Assert.Equal (-1, e.KeyEvent.KeyValue);
				Assert.Equal (shift, e.KeyEvent.IsShift);
				Assert.Equal (alt, e.KeyEvent.IsAlt);
				Assert.Equal (control, e.KeyEvent.IsCtrl);
				Assert.False (keyUp);
				Assert.False (view.IsKeyUp);
				keyUp = true;
			};

			Application.Top.Add (view);

			Console.MockKeyPresses.Push (new ConsoleKeyInfo ('\0', (ConsoleKey)'\0', shift, alt, control));

			Application.Iteration += () => Application.RequestStop ();

			Assert.True (view.CanFocus);

			Application.Run ();
			Application.Shutdown ();

			Assert.True (keyDown);
			Assert.False (keyPress);
			Assert.True (keyUp);
			Assert.True (view.IsKeyDown);
			Assert.False (view.IsKeyPress);
			Assert.True (view.IsKeyUp);
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
			view1.Leave += (s,e) => {
				view1Leave = true;
			};
			subView1.Leave += (s,e) => {
				subView1.Remove (subView1subView1);
				subView1Leave = true;
			};
			view1.Add (subView1);
			subView1subView1.Leave += (s,e) => {
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
					Width = Dim.Fill ()
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
222222222222222222220", attributes);
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
222222222222222222220", attributes);
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
			Assert.Equal (new Rect (0, 0, 10, 1), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 30, 2), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 10, 1), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 30, 2), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 10, 1), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 30, 2), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 10, 1), view.NeedDisplay);
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
			Assert.Equal (new Rect (0, 0, 30, 2), view.NeedDisplay);
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
		public void Frame_Set_After_Initialze_Update_NeededDisplay ()
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

			Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
			Assert.Equal (new Rect (0, 0, 40, 8), frame.Frame);
			Assert.Equal (new Rect (1, 1, 0, 0), frame.Subviews [0].Frame);
			Assert.Equal ("ContentView()({X=1,Y=1,Width=0,Height=0})", frame.Subviews [0].ToString ());
			Assert.Equal (new Rect (0, 0, 40, 8), new Rect (
				frame.Frame.Left, frame.Frame.Top,
				frame.Frame.Right, frame.Frame.Bottom));
			Assert.Equal (new Rect (0, 0, 30, 1), label.Frame);
			Assert.Equal (new Rect (0, 0, 13, 1), button.Frame);

			Assert.Equal (new Rect (0, 0, 80, 25), top.NeedDisplay);
			Assert.Equal (new Rect (0, 0, 40, 8), frame.NeedDisplay);
			Assert.Equal (Rect.Empty, frame.Subviews [0].NeedDisplay);
			Assert.Equal (new Rect (0, 0, 40, 8), new Rect (
				frame.NeedDisplay.Left, frame.NeedDisplay.Top,
				frame.NeedDisplay.Right, frame.NeedDisplay.Bottom));
			Assert.Equal (new Rect (0, 0, 30, 1), label.NeedDisplay);
			Assert.Equal (new Rect (0, 0, 13, 1), button.NeedDisplay);

			top.LayoutComplete += (s,e) => {
				Assert.Equal (new Rect (0, 0, 80, 25), top.NeedDisplay);
			};

			frame.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 40, 8), frame.NeedDisplay);
			};

			frame.Subviews [0].LayoutComplete += (s, e) => {
				if (top.IsLoaded) {
					Assert.Equal (new Rect (0, 0, 38, 6), frame.Subviews [0].NeedDisplay);
				} else {
					Assert.Equal (new Rect (0, 0, 38, 6), frame.Subviews [0].NeedDisplay);
				}
			};

			label.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 38, 1), label.NeedDisplay);
			};

			button.LayoutComplete += (s, e) => {
				Assert.Equal (new Rect (0, 0, 13, 1), button.NeedDisplay);
			};

			Application.Begin (top);

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
			Assert.Equal (new Rect (20, 8, 40, 8), frame.Frame);
			Assert.Equal (new Rect (1, 1, 38, 6), frame.Subviews [0].Frame);
			Assert.Equal ("ContentView()({X=1,Y=1,Width=38,Height=6})", frame.Subviews [0].ToString ());
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
	}
}
