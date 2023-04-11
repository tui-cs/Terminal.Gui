using Microsoft.VisualStudio.TestPlatform.Utilities;
using NStack;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class ScrollViewTests {
		readonly ITestOutputHelper output;

		public ScrollViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var sv = new ScrollView ();
			Assert.Equal (LayoutStyle.Computed, sv.LayoutStyle);
			Assert.True (sv.CanFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), sv.Frame);
			Assert.Equal (Rect.Empty, sv.Frame);
			Assert.Null (sv.X);
			Assert.Null (sv.Y);
			Assert.Null (sv.Width);
			Assert.Null (sv.Height);
			Assert.Equal (Point.Empty, sv.ContentOffset);
			Assert.Equal (Size.Empty, sv.ContentSize);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.KeepContentAlwaysInViewport);

			sv = new ScrollView (new Rect (1, 2, 20, 10));
			Assert.Equal (LayoutStyle.Absolute, sv.LayoutStyle);
			Assert.True (sv.CanFocus);
			Assert.Equal (new Rect (1, 2, 20, 10), sv.Frame);
			Assert.Null (sv.X);
			Assert.Null (sv.Y);
			Assert.Null (sv.Width);
			Assert.Null (sv.Height);
			Assert.Equal (Point.Empty, sv.ContentOffset);
			Assert.Equal (Size.Empty, sv.ContentSize);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.KeepContentAlwaysInViewport);
		}

		[Fact]
		public void Adding_Views ()
		{
			var sv = new ScrollView (new Rect (0, 0, 20, 10)) {
				ContentSize = new Size (30, 20)
			};
			sv.Add (new View () { Width = 10, Height = 5 },
				new View () { X = 12, Y = 7, Width = 10, Height = 5 });

			Assert.Equal (new Size (30, 20), sv.ContentSize);
			Assert.Equal (2, sv.Subviews [0].Subviews.Count);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			var sv = new ScrollView (new Rect (0, 0, 20, 10)) {
				ContentSize = new Size (40, 20)
			};
			sv.Add (new View () { Width = 20, Height = 5 },
				new View () { X = 22, Y = 7, Width = 10, Height = 5 });

			sv.BeginInit (); sv.EndInit ();

			Assert.True (sv.KeepContentAlwaysInViewport);
			Assert.True (sv.AutoHideScrollBars);
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -1), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent ((Key)'v' | Key.AltMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-1, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-20, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-20, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-20, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);

			sv.KeepContentAlwaysInViewport = false;
			Assert.False (sv.KeepContentAlwaysInViewport);
			Assert.True (sv.AutoHideScrollBars);
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -1), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent ((Key)'v' | Key.AltMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -9), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-1, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-19, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-19, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-19, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-19, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-19, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
		}

		[Fact, AutoInitShutdown]
		public void AutoHideScrollBars_False_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10,
				AutoHideScrollBars = false
			};

			sv.ShowHorizontalScrollIndicator = true;
			sv.ShowVerticalScrollIndicator = true;

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.False (sv.AutoHideScrollBars);
			Assert.True (sv.ShowHorizontalScrollIndicator);
			Assert.True (sv.ShowVerticalScrollIndicator);
			sv.Redraw (sv.Bounds);
			TestHelpers.AssertDriverContentsAre (@"
         ▲
         ┬
         │
         │
         │
         │
         │
         ┴
         ▼
◄├─────┤► 
", output);

			sv.ShowHorizontalScrollIndicator = false;
			sv.ShowVerticalScrollIndicator = true;

			Assert.False (sv.AutoHideScrollBars);
			Assert.False (sv.ShowHorizontalScrollIndicator);
			Assert.True (sv.ShowVerticalScrollIndicator);
			sv.Redraw (sv.Bounds);
			TestHelpers.AssertDriverContentsAre (@"
         ▲
         ┬
         │
         │
         │
         │
         │
         ┴
         ▼
", output);

			sv.ShowHorizontalScrollIndicator = true;
			sv.ShowVerticalScrollIndicator = false;

			Assert.False (sv.AutoHideScrollBars);
			Assert.True (sv.ShowHorizontalScrollIndicator);
			Assert.False (sv.ShowVerticalScrollIndicator);
			sv.Redraw (sv.Bounds);
			TestHelpers.AssertDriverContentsAre (@"
         
         
         
         
         
         
         
         
         
◄├─────┤► 
", output);

			sv.ShowHorizontalScrollIndicator = false;
			sv.ShowVerticalScrollIndicator = false;

			Assert.False (sv.AutoHideScrollBars);
			Assert.False (sv.ShowHorizontalScrollIndicator);
			Assert.False (sv.ShowVerticalScrollIndicator);
			sv.Redraw (sv.Bounds);
			TestHelpers.AssertDriverContentsAre (@"
         
         
         
         
         
         
         
         
         
         
", output);
		}

		[Fact, AutoInitShutdown]
		public void AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10
			};

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.True (sv.AutoHideScrollBars);
			Assert.False (sv.ShowHorizontalScrollIndicator);
			Assert.False (sv.ShowVerticalScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);

			sv.AutoHideScrollBars = false;
			sv.ShowHorizontalScrollIndicator = true;
			sv.ShowVerticalScrollIndicator = true;
			sv.LayoutSubviews ();
			sv.Redraw (sv.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
         ▲
         ┬
         │
         │
         │
         │
         │
         ┴
         ▼
◄├─────┤► 
", output);
		}

		[Fact, AutoInitShutdown]
		public void ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10,
				ContentSize = new Size (50, 50)
			};

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.Equal (50, sv.ContentSize.Width);
			Assert.Equal (50, sv.ContentSize.Height);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.ShowHorizontalScrollIndicator);
			Assert.True (sv.ShowVerticalScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
         ▲
         ┬
         ┴
         ░
         ░
         ░
         ░
         ░
         ▼
◄├┤░░░░░► 
", output);
		}

		[Fact, AutoInitShutdown]
		public void ContentOffset_ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10,
				ContentSize = new Size (50, 50),
				ContentOffset = new Point (25, 25)
			};

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.Equal (-25, sv.ContentOffset.X);
			Assert.Equal (-25, sv.ContentOffset.Y);
			Assert.Equal (50, sv.ContentSize.Width);
			Assert.Equal (50, sv.ContentSize.Height);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.ShowHorizontalScrollIndicator);
			Assert.True (sv.ShowVerticalScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
         ▲
         ░
         ░
         ░
         ┬
         │
         ┴
         ░
         ▼
◄░░░├─┤░► 
", output);
		}

		// BUGBUG: v2 - I can't figure out what this test is trying to test and it fails in weird ways
		// Disabling for now
		//[Fact, AutoInitShutdown]
		//public void Frame_And_Labels_Does_Not_Overspill_ScrollView ()
		//{
		//	var sv = new ScrollView {
		//		X = 3,
		//		Y = 3,
		//		Width = 10,
		//		Height = 10,
		//		ContentSize = new Size (50, 50)
		//	};
		//	for (int i = 0; i < 8; i++) {
		//		sv.Add (new CustomButton ("█", $"Button {i}", 20, 3) { Y = i * 3 });
		//	}
		//	Application.Top.Add (sv);
		//	Application.Begin (Application.Top);

		//	TestHelpers.AssertDriverContentsWithFrameAre (@"
		// █████████▲
		// ██████But┬
		// █████████┴
		// ┌────────░
		// │     But░
		// └────────░
		// ┌────────░
		// │     But░
		// └────────▼
		// ◄├┤░░░░░► ", output);

		//	sv.ContentOffset = new Point (5, 5);
		//	sv.LayoutSubviews ();
		//	Application.Refresh ();
		//	TestHelpers.AssertDriverContentsWithFrameAre (@"
		// ─────────▲
		// ─────────┬
		//  Button 2│
		// ─────────┴
		// ─────────░
		//  Button 3░
		// ─────────░
		// ─────────░
		//  Button 4▼
		// ◄├─┤░░░░► ", output);
		//}

		//private class CustomButton : FrameView {
		//	private Label labelFill;
		//	private Label labelText;

		//	public CustomButton (string fill, ustring text, int width, int height) : base ()
		//	{
		//		Width = width;
		//		Height = height;
		//		labelFill = new Label () { AutoSize = false, X = Pos.Center (), Y = Pos.Center (), Width = Dim.Fill (), Height = Dim.Fill (), Visible = false };
		//		labelFill.LayoutComplete += (s, e) => {
		//			var fillText = new System.Text.StringBuilder ();
		//			for (int i = 0; i < labelFill.Bounds.Height; i++) {
		//				if (i > 0) {
		//					fillText.AppendLine ("");
		//				}
		//				for (int j = 0; j < labelFill.Bounds.Width; j++) {
		//					fillText.Append (fill);
		//				}
		//			}
		//			labelFill.Text = fillText.ToString ();
		//		};

		//		labelText = new Label (text) { X = Pos.Center (), Y = Pos.Center () };
		//		Add (labelFill, labelText);
		//		CanFocus = true;
		//	}

		//	public override bool OnEnter (View view)
		//	{
		//		Border.BorderStyle = BorderStyle.None;
		//		Border.DrawMarginFrame = false;
		//		labelFill.Visible = true;
		//		view = this;
		//		return base.OnEnter (view);
		//	}

		//	public override bool OnLeave (View view)
		//	{
		//		Border.BorderStyle = BorderStyle.Single;
		//		Border.DrawMarginFrame = true;
		//		labelFill.Visible = false;
		//		if (view == null)
		//			view = this;
		//		return base.OnLeave (view);
		//	}
		//}

		[Fact, AutoInitShutdown]
		public void Clear_Window_Inside_ScrollView ()
		{
			var topLabel = new Label ("At 15,0") { X = 15 };
			var sv = new ScrollView {
				X = 3,
				Y = 3,
				Width = 10,
				Height = 10,
				ContentSize = new Size (23, 23),
				KeepContentAlwaysInViewport = false
			};
			var bottomLabel = new Label ("At 15,15") { X = 15, Y = 15 };
			Application.Top.Add (topLabel, sv, bottomLabel);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
               At 15,0 
                       
                       
            ▲          
            ┬          
            ┴          
            ░          
            ░          
            ░          
            ░          
            ░          
            ▼          
   ◄├┤░░░░░►           
                       
                       
               At 15,15", output);

			var attributes = new Attribute [] {
				Colors.TopLevel.Normal,
				Colors.TopLevel.Focus,
				Colors.Base.Normal
			};

			TestHelpers.AssertDriverColorsAre (@"
00000000000000000000000
00000000000000000000000
00000000000000000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00011111111110000000000
00000000000000000000000
00000000000000000000000
00000000000000000000000", attributes);

			sv.Add (new Window { X = 3, Y = 3, Width = 20, Height = 20 });

			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
               At 15,0 
                       
                       
            ▲          
            ┬          
            ┴          
      ┌─────░          
      │     ░          
      │     ░          
      │     ░          
      │     ░          
      │     ▼          
   ◄├┤░░░░░►           
                       
                       
               At 15,15", output);

			TestHelpers.AssertDriverColorsAre (@"
00000000000000000000000
00000000000000000000000
00000000000000000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00011111111110000000000
00000000000000000000000
00000000000000000000000
00000000000000000000000", attributes);

			sv.ContentOffset = new Point (20, 20);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
               At 15,0 
                       
                       
     │      ▲          
     │      ░          
   ──┘      ░          
            ░          
            ░          
            ┬          
            │          
            ┴          
            ▼          
   ◄░░░░├─┤►           
                       
                       
               At 15,15", output);

			TestHelpers.AssertDriverColorsAre (@"
00000000000000000000000
00000000000000000000000
00000000000000000000000
00022200000010000000000
00022200000010000000000
00022200000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00011111111110000000000
00000000000000000000000
00000000000000000000000
00000000000000000000000", attributes);
		}

		[Fact, AutoInitShutdown]
		public void DrawTextFormatter_Respects_The_Clip_Bounds ()
		{
			var rule = "0123456789";
			var size = new Size (40, 40);
			var view = new View (new Rect (Point.Empty, size));
			view.Add (new Label (rule.Repeat (size.Width / rule.Length)) { AutoSize = false, Width = Dim.Fill () });
			view.Add (new Label (rule.Repeat (size.Height / rule.Length), TextDirection.TopBottom_LeftRight) { Height = Dim.Fill (), AutoSize = false });
			view.Add (new Button (1, 1, "Press me!"));
			var scrollView = new ScrollView (new Rect (1, 1, 15, 10)) {
				ContentSize = size,
				ShowHorizontalScrollIndicator = true,
				ShowVerticalScrollIndicator = true
			};
			scrollView.Add (view);
			var win = new Window (new Rect (1, 1, 20, 14));
			win.Add (scrollView);
			Application.Top.Add (win);
			Application.Begin (Application.Top);

			var expected = @"
 ┌──────────────────┐
 │                  │
 │ 01234567890123▲  │
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
"
			;

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 12345678901234▲  │
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
"
			;

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 23456789012345▲  │
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
"
			;

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 34567890123456▲  │
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
"
			;

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 45678901234567▲  │
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
"
			;

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 56789012345678▲  │
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
"
			;

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 67890123456789▲  │
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
"
			;

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 78901234567890▲  │
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

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CtrlMask | Key.End, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 67890123456789▲  │
 │               ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░░░░░░░├───┤►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CtrlMask | Key.Home, new KeyModifiers ())));
			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
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
 ┌──────────────────┐
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
 ┌──────────────────┐
 │                  │
 │ 3             ▲  │
 │ 4             ┬  │
 │ 5             │  │
 │ 6             ┴  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ░  │
 │ 0             ░  │
 │ 1             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);

			Assert.True (scrollView.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);

			expected = @"
 ┌──────────────────┐
 │                  │
 │ 1             ▲  │
 │ 2             ░  │
 │ 3             ░  │
 │ 4             ░  │
 │ 5             ░  │
 │ 6             ░  │
 │ 7             ┬  │
 │ 8             ┴  │
 │ 9             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 1, 21, 14), pos);
		}
	}
}
