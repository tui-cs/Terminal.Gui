using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class ScrollBarViewTests {
		readonly ITestOutputHelper output;

		public ScrollBarViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void Horizontal_Default_Draws_Correctly ()
		{
			var width = 40;
			var height = 3;

			var super = new Window () { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
			Application.Top.Add (super);

			var sbv = new ScrollBarView () {
				Id = "sbv",
				Size = width * 2,
				// BUGBUG: ScrollBarView should work if Host is null
				Host = super,
				ShowScrollIndicator = true,
			};
			super.Add (sbv);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width, height);

			var expected = @"
┌──────────────────────────────────────┐
│◄├────────────────┤░░░░░░░░░░░░░░░░░░►│
└──────────────────────────────────────┘";
			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

		}

		[Fact, AutoInitShutdown]
		public void Vertical_Default_Draws_Correctly ()
		{
			var width = 3;
			var height = 40;

			var super = new Window () { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
			Application.Top.Add (super);

			var sbv = new ScrollBarView () {
				Id = "sbv",
				Size = height * 2,
				// BUGBUG: ScrollBarView should work if Host is null
				Host = super,
				ShowScrollIndicator = true,
				IsVertical = true
			};

			super.Add (sbv);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width, height);

			var expected = @"
┌─┐
│▲│
│┬│
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│┴│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│▼│
└─┘";
			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

		}

		[Fact, AutoInitShutdown]
		public void Both_Default_Draws_Correctly ()
		{
			var width = 3;
			var height = 40;

			var super = new Window () { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
			Application.Top.Add (super);

			var horiz = new ScrollBarView () {
				Id = "horiz",
				Size = width * 2,
				// BUGBUG: ScrollBarView should work if Host is null
				Host = super,
				ShowScrollIndicator = true,
				IsVertical = true
			};
			super.Add (horiz);

			var vert = new ScrollBarView () {
				Id = "vert",
				Size = height * 2,
				// BUGBUG: ScrollBarView should work if Host is null
				Host = super,
				ShowScrollIndicator = true,
				IsVertical = true
			};
			super.Add (vert);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (width, height);

			var expected = @"
┌─┐
│▲│
│┬│
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│┴│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│▼│
└─┘";
			_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

		}

		// This class enables test functions annotated with the [InitShutdown] attribute
		// to have a function called before the test function is called and after.
		// 
		// This is necessary because a) Application is a singleton and Init/Shutdown must be called
		// as a pair, and b) all unit test functions should be atomic.
		[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
		public class ScrollBarAutoInitShutdownAttribute : AutoInitShutdownAttribute {

			public override void Before (MethodInfo methodUnderTest)
			{
				base.Before (methodUnderTest);

				ScrollBarViewTests._hostView = new HostView () {
					Width = Dim.Fill (),
					Height = Dim.Fill (),
					Top = 0,
					Lines = 30,
					Left = 0,
					Cols = 100
				};

				Application.Top.Add (ScrollBarViewTests._hostView);
			}

			public override void After (MethodInfo methodUnderTest)
			{
				ScrollBarViewTests._hostView = null;
				base.After (methodUnderTest);
			}
		}

		public class HostView : View {
			ScrollBarView.ScrollBarType _scrollBarType;
			private ScrollBarView _scrollBar;

			public int Top { get; set; }
			public int Lines { get; set; }
			public int Left { get; set; }
			public int Cols { get; set; }
			//public int Bottom { get; set; }
			//public int Right { get; set; }
			public ScrollBarView ScrollBar { get => _scrollBar; }
			public ScrollBarView.ScrollBarType ScrollBarType {
				get => _scrollBarType;
				set {
					if (_scrollBarType != value) {
						_scrollBarType = value;
						DisposeScrollBar ();
						if (_scrollBarType != ScrollBarView.ScrollBarType.None) {
							if (!IsAdded) {
								Added += HostView_Added;
							} else {
								CreateScrollBar ();
							}
						}
						SetNeedsDisplay ();
					}
				}
			}

			private void HostView_Added (object sender, SuperViewChangedEventArgs e)
			{
				CreateScrollBar ();
			}

			private void CreateScrollBar ()
			{
				_scrollBar = new ScrollBarView (this, _scrollBarType, false);
				AddHandlers ();
			}

			private void AddHandlers ()
			{
				if (_scrollBar != null) {
					LayoutComplete += HostView_LayoutComplete;
					_scrollBar.ChangedPosition += ScrollBar_ChangedPosition;
					//_scrollBar.VisibleChanged += ScrollBar_VisibleChanged;
					//if (_scrollBar.IsVertical) {
					//	Bottom = 1;
					//} else {
					//	Right = 1;
					//}
					if (_scrollBar.OtherScrollBarView != null) {
						_scrollBar.OtherScrollBarView.ChangedPosition += OtherScrollBarView_ChangedPosition;
						//_scrollBar.OtherScrollBarView.VisibleChanged += OtherScrollBarView_VisibleChanged;
						//if (_scrollBar.OtherScrollBarView.IsVertical) {
						//	Bottom = 1;
						//} else {
						//	Right = 1;
						//}
					}
				}
			}

			private void HostView_LayoutComplete (object sender, LayoutEventArgs e)
			{
				if (_scrollBar != null) {
					_scrollBar.Refresh ();
				}
			}

			private void DisposeScrollBar ()
			{
				if (_scrollBar != null) {
					_scrollBar.ChangedPosition -= ScrollBar_ChangedPosition;
					//_scrollBar.VisibleChanged -= ScrollBar_VisibleChanged;
					if (_scrollBar.OtherScrollBarView != null) {
						_scrollBar.OtherScrollBarView.ChangedPosition -= OtherScrollBarView_ChangedPosition;
						//_scrollBar.OtherScrollBarView.VisibleChanged -= OtherScrollBarView_VisibleChanged;
					}
					_scrollBar.RemoveAll ();
					_scrollBar = null;
					//Bottom = 0;
					//Right = 0;
				}
			}

			public override void Redraw (Rect bounds)
			{
				if (_scrollBar != null) {
					_scrollBar.Size = Lines;
					_scrollBar.Position = Top;
					_scrollBar.OtherScrollBarView.Size = Cols;
					_scrollBar.OtherScrollBarView.Position = Left;
					_scrollBar.Refresh ();
				}

				base.Redraw (bounds);
			}

			private void ScrollBar_ChangedPosition (object sender, EventArgs e)
			{
				Top = _scrollBar.Position;
				if (Top != _scrollBar.Position) {
					_scrollBar.Position = Top;
				}
				SetNeedsDisplay ();
			}

			private void OtherScrollBarView_ChangedPosition (object sender, EventArgs e)
			{
				Left = _scrollBar.OtherScrollBarView.Position;
				if (Left != _scrollBar.OtherScrollBarView.Position) {
					_scrollBar.OtherScrollBarView.Position = Left;
				}
				SetNeedsDisplay ();
			}

			//private void ScrollBar_VisibleChanged ()
			//{
			//	if (_scrollBar.Visible && Right == 0) {
			//		Right = 1;
			//	} else if (!_scrollBar.Visible && Right == 1) {
			//		Right = 0;
			//	}
			//}

			//private void OtherScrollBarView_VisibleChanged ()
			//{
			//	if (_scrollBar.OtherScrollBarView.Visible && Bottom == 0) {
			//		Bottom = 1;
			//	} else if (!_scrollBar.OtherScrollBarView.Visible && Bottom == 1) {
			//		Bottom = 0;
			//	}
			//}
		}

		public class HostViewAuto : View {
			ScrollBarView.ScrollBarType _scrollBarType;
			private ScrollBarView _scrollBar;

			public int Lines { get; set; }
			public int Cols { get; set; }
			public int Top { get; set; }
			public int Left { get; set; }
			public int Bottom { get => GetScrollOffset (false); }
			public int Right { get => GetScrollOffset (true); }
			public ScrollBarView ScrollBar { get => _scrollBar; }
			public ScrollBarView.ScrollBarType ScrollBarType {
				get => _scrollBarType;
				set {
					if (_scrollBarType != value) {
						_scrollBarType = value;
						DisposeScrollBar ();
						//if (_scrollBarType == ScrollBarView.ScrollBarType.None) {
						//	Bottom = 0;
						//	Right = 0;
						//} else {
						//	if (!IsAdded) {
						//		Added += HostViewAuto_Added;
						//	} else {
						//		CreateScrollBar ();
						//	}
						//}
						if (_scrollBarType != ScrollBarView.ScrollBarType.None) {
							if (!IsAdded) {
								Added += HostViewAuto_Added;
							} else {
								CreateScrollBar ();
							}
						}
						SetNeedsDisplay ();
					}
				}
			}

			private int GetScrollOffset (bool isVertical)
			{
				switch (_scrollBarType) {
				case ScrollBarView.ScrollBarType.Vertical:
					return isVertical ? _scrollBar.ShowScrollIndicator ? 1 : 0 : 0;
				case ScrollBarView.ScrollBarType.Horizontal:
					return !isVertical ? _scrollBar.ShowScrollIndicator ? 1 : 0 : 0;
				case ScrollBarView.ScrollBarType.Both:
					return isVertical ? _scrollBar.ShowScrollIndicator ? 1 : 0
						: _scrollBar.OtherScrollBarView.ShowScrollIndicator ? 1 : 0;
				}
				return 0;
			}

			private void HostViewAuto_Added (object sender, SuperViewChangedEventArgs e)
			{
				CreateScrollBar ();
			}

			private void CreateScrollBar ()
			{
				//switch (_scrollBarType) {
				//case ScrollBarView.ScrollBarType.Vertical:
				//	Bottom = 1;
				//	Right = 0;
				//	break;
				//case ScrollBarView.ScrollBarType.Horizontal:
				//	Bottom = 0;
				//	Right = 1;
				//	break;
				//case ScrollBarView.ScrollBarType.Both:
				//	Bottom = 1;
				//	Right = 1;
				//	break;
				//}
				_scrollBar = new ScrollBarView (this, _scrollBarType, true,
					nameof (Lines), nameof (Cols),
					nameof (Top), nameof (Left));
				//nameof (Bottom), nameof (Right));
			}

			private void DisposeScrollBar ()
			{
				_scrollBar?.RemoveAll ();
				_scrollBar = null;
			}

			public override void Redraw (Rect bounds)
			{
				for (int r = Top; r < Frame.Height; r++) {
					var str = $"Line{r}:";
					int c = Left;
					Driver.Move (c, r);
					if (r < Lines) {
						Driver.AddStr (str);
						c += str.Length;
					}
					Driver.Move (c, r);
					for (; c < Frame.Width; c++) {
						if (c < Cols) {
							Driver.AddStr ($"{c}");
						} else {
							Driver.AddStr (" ");
						}
					}
				}

				base.Redraw (bounds);
			}
		}

		private static HostView _hostView;

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void Hosting_A_Null_View_To_A_ScrollBarView_Throws_ArgumentNullException ()
		{
			Assert.Throws<ArgumentNullException> ("The host parameter can't be null.",
				() => new ScrollBarView (null, true));
			Assert.Throws<ArgumentNullException> ("The host parameter can't be null.",
				() => new ScrollBarView (null, false));
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void Hosting_A_Null_SuperView_View_To_A_ScrollBarView_Throws_ArgumentNullException ()
		{
			Assert.Throws<ArgumentNullException> ("The host SuperView parameter can't be null.",
				() => new ScrollBarView (new View (), true));
			Assert.Throws<ArgumentNullException> ("The host SuperView parameter can't be null.",
				() => new ScrollBarView (new View (), false));
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void Hosting_Two_Vertical_ScrollBarView_Throws_ArgumentException ()
		{
			var top = new Toplevel ();
			var host = new View ();
			top.Add (host);
			var v = new ScrollBarView (host, true);
			var h = new ScrollBarView (host, true);

			Assert.Throws<ArgumentException> (() => v.OtherScrollBarView = h);
			Assert.Throws<ArgumentException> (() => h.OtherScrollBarView = v);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void Hosting_Two_Horizontal_ScrollBarView_Throws_ArgumentException ()
		{
			var top = new Toplevel ();
			var host = new View ();
			top.Add (host);
			var v = new ScrollBarView (host, false);
			var h = new ScrollBarView (host, false);

			Assert.Throws<ArgumentException> (() => v.OtherScrollBarView = h);
			Assert.Throws<ArgumentException> (() => h.OtherScrollBarView = v);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void Scrolling_With_Default_Constructor_Do_Not_Scroll ()
		{
			var sbv = new ScrollBarView {
				Position = 1
			};
			// BUGBUG: v2 - this test makes no sense to me. Why would we un-set Positon?
			//Assert.NotEqual (1, sbv.Position);
			//Assert.Equal (0, sbv.Position);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void Hosting_A_View_To_A_ScrollBarView ()
		{
			_hostView.ScrollBarType = ScrollBarView.ScrollBarType.Both;

			Assert.True (_hostView.ScrollBar.IsVertical);
			Assert.False (_hostView.ScrollBar.OtherScrollBarView.IsVertical);

			Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);
			Assert.NotEqual (_hostView.ScrollBar.Size, _hostView.Lines);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.NotEqual (_hostView.ScrollBar.OtherScrollBarView.Size, _hostView.Cols);

			_hostView.SuperView.LayoutSubviews ();
			_hostView.Draw ();

			Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);
			Assert.Equal (_hostView.ScrollBar.Size, _hostView.Lines);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Size, _hostView.Cols);
			//Assert.Equal (1, _hostView.Bottom);
			//Assert.Equal (1, _hostView.Right);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ChangedPosition_Update_The_Hosted_View ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			_hostView.ScrollBar.Position = 2;
			Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);

			_hostView.ScrollBar.OtherScrollBarView.Position = 5;
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ChangedPosition_Scrolling ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			for (int i = 0; i < _hostView.ScrollBar.Size; i++) {
				_hostView.ScrollBar.Position += 1;
				Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);
			}
			for (int i = _hostView.ScrollBar.Size - 1; i >= 0; i--) {
				_hostView.ScrollBar.Position -= 1;
				Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);
			}

			for (int i = 0; i < _hostView.ScrollBar.OtherScrollBarView.Size; i++) {
				_hostView.ScrollBar.OtherScrollBarView.Position += i;
				Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
			}
			for (int i = _hostView.ScrollBar.OtherScrollBarView.Size - 1; i >= 0; i--) {
				_hostView.ScrollBar.OtherScrollBarView.Position -= 1;
				Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
			}
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ChangedPosition_Negative_Value ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			_hostView.ScrollBar.Position = -20;
			Assert.Equal (0, _hostView.ScrollBar.Position);
			Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);

			_hostView.ScrollBar.OtherScrollBarView.Position = -50;
			Assert.Equal (0, _hostView.ScrollBar.OtherScrollBarView.Position);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void DrawContent_Update_The_ScrollBarView_Position ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			_hostView.Top = 3;
			_hostView.Draw ();
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_hostView.Left = 6;
			_hostView.Draw ();
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void OtherScrollBarView_Not_Null ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			Assert.NotNull (_hostView.ScrollBar.OtherScrollBarView);
			Assert.NotEqual (_hostView.ScrollBar, _hostView.ScrollBar.OtherScrollBarView);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.OtherScrollBarView, _hostView.ScrollBar);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ShowScrollIndicator_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			Assert.True (_hostView.ScrollBar.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void KeepContentAlwaysInViewport_True ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			Assert.Equal (80, _hostView.Bounds.Width);
			Assert.Equal (25, _hostView.Bounds.Height);
			Assert.Equal (79, _hostView.ScrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal (24, _hostView.ScrollBar.Bounds.Height);
			Assert.Equal (30, _hostView.ScrollBar.Size);
			Assert.Equal (100, _hostView.ScrollBar.OtherScrollBarView.Size);
			Assert.True (_hostView.ScrollBar.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.Visible);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.Visible);

			_hostView.ScrollBar.Position = 50;
			Assert.Equal (_hostView.ScrollBar.Position, _hostView.ScrollBar.Size - _hostView.ScrollBar.Bounds.Height);
			Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);
			Assert.Equal (6, _hostView.ScrollBar.Position);
			Assert.Equal (6, _hostView.Top);
			Assert.True (_hostView.ScrollBar.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.Visible);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.Visible);

			_hostView.ScrollBar.OtherScrollBarView.Position = 150;
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.ScrollBar.OtherScrollBarView.Size - _hostView.ScrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (21, _hostView.ScrollBar.OtherScrollBarView.Position);
			Assert.Equal (21, _hostView.Left);
			Assert.True (_hostView.ScrollBar.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_hostView.ScrollBar.Visible);
			Assert.True (_hostView.ScrollBar.OtherScrollBarView.Visible);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void KeepContentAlwaysInViewport_False ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			_hostView.ScrollBar.KeepContentAlwaysInViewport = false;
			_hostView.ScrollBar.Position = 50;
			Assert.Equal (_hostView.ScrollBar.Position, _hostView.ScrollBar.Size - 1);
			Assert.Equal (_hostView.ScrollBar.Position, _hostView.Top);
			Assert.Equal (29, _hostView.ScrollBar.Position);
			Assert.Equal (29, _hostView.Top);

			_hostView.ScrollBar.OtherScrollBarView.Position = 150;
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.ScrollBar.OtherScrollBarView.Size - 1);
			Assert.Equal (_hostView.ScrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (99, _hostView.ScrollBar.OtherScrollBarView.Position);
			Assert.Equal (99, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void AutoHideScrollBars_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Draw ();
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()((0,0,80,25)))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Combine(View(Width,HostView()((0,0,80,25)))-Absolute(1))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Lines = 10;
			_hostView.Draw ();
			Assert.False (_scrollBar.ShowScrollIndicator);
			Assert.False (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()((0,0,80,25)))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("View(Width,HostView()((0,0,80,25)))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Cols = 60;
			_hostView.Draw ();
			Assert.False (_scrollBar.ShowScrollIndicator);
			Assert.False (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()((0,0,80,25)))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.False (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("View(Width,HostView()((0,0,80,25)))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Lines = 40;
			_hostView.Draw ();
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("View(Height,HostView()((0,0,80,25)))",
				_scrollBar.Height.ToString ());
			Assert.Equal (25, _scrollBar.Bounds.Height);
			Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.False (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("View(Width,HostView()((0,0,80,25)))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Cols = 120;
			_hostView.Draw ();
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()((0,0,80,25)))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Combine(View(Width,HostView()((0,0,80,25)))-Absolute(1))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);
		}

		// BUGBUG: v2 - Tig broke these tests; @bdisp help?
		//[Fact]
		//public void Constructor_ShowBothScrollIndicator_False_And_IsVertical_True_Refresh_Does_Not_Throws_An_Object_Null_Exception ()
		//{
		//	var exception = Record.Exception (() => {
		//		Application.Init (new FakeDriver ());

		//		var top = Application.Top;

		//		var win = new Window () {
		//			X = 0,
		//			Y = 0,
		//			Width = Dim.Fill (),
		//			Height = Dim.Fill ()
		//		};

		//		List<string> source = new List<string> ();

		//		for (int i = 0; i < 50; i++) {
		//			source.Add ($"item {i}");
		//		}

		//		var listView = new ListView (source) {
		//			X = 0,
		//			Y = 0,
		//			Width = Dim.Fill (),
		//			Height = Dim.Fill ()
		//		};
		//		win.Add (listView);

		//		var newScrollBarView = new ScrollBarView (listView, true, false) {
		//			KeepContentAlwaysInViewport = true
		//		};
		//		win.Add (newScrollBarView);

				//newScrollBarView.ChangedPosition += (s, e) => {
				//	listView.TopItem = newScrollBarView.Position;
				//	if (listView.TopItem != newScrollBarView.Position) {
				//		newScrollBarView.Position = listView.TopItem;
				//	}
				//	Assert.Equal (newScrollBarView.Position, listView.TopItem);
				//	listView.SetNeedsDisplay ();
				//};

				//listView.DrawContent += (s, e) => {
				//	newScrollBarView.Size = listView.Source.Count;
				//	Assert.Equal (newScrollBarView.Size, listView.Source.Count);
				//	newScrollBarView.Position = listView.TopItem;
				//	Assert.Equal (newScrollBarView.Position, listView.TopItem);
				//	newScrollBarView.Refresh ();
				//};

		//		top.Ready += (s, e) => {
		//			newScrollBarView.Position = 45;
		//			Assert.Equal (newScrollBarView.Position, newScrollBarView.Size - listView.TopItem + (listView.TopItem - listView.Bounds.Height));
		//			Assert.Equal (newScrollBarView.Position, listView.TopItem);
		//			Assert.Equal (27, newScrollBarView.Position);
		//			Assert.Equal (27, listView.TopItem);
		//			Application.RequestStop ();
		//		};

		//		top.Add (win);

		//		Application.Run ();

		//		Application.Shutdown ();
		//	});

		//	Assert.Null (exception);
		//}

		[Fact]
		public void Constructor_ShowBothScrollIndicator_False_And_IsVertical_False_Refresh_Does_Not_Throws_An_Object_Null_Exception ()
		{
			// BUGBUG: v2 - Tig broke these tests; @bdisp help?
			//var exception = Record.Exception (() => {
			Application.Init (new FakeDriver ());

			var top = Application.Top;

			var win = new Window () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			List<string> source = new List<string> ();

			for (int i = 0; i < 50; i++) {
				var text = $"item {i} - ";
				for (int j = 0; j < 160; j++) {
					var col = j.ToString ();
					text += col.Length == 1 ? col [0] : col [1];
				}
				source.Add (text);
			}

			var listView = new ListView (source) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (listView);

			var newScrollBarView = new ScrollBarView (listView, false, false) {
				KeepContentAlwaysInViewport = true
			};
			win.Add (newScrollBarView);

				newScrollBarView.ChangedPosition += (s, e) => {
					listView.LeftItem = newScrollBarView.Position;
					if (listView.LeftItem != newScrollBarView.Position) {
						newScrollBarView.Position = listView.LeftItem;
					}
					Assert.Equal (newScrollBarView.Position, listView.LeftItem);
					listView.SetNeedsDisplay ();
				};

				listView.DrawContent += (s, e) => {
					newScrollBarView.Size = listView.Maxlength;
					Assert.Equal (newScrollBarView.Size, listView.Maxlength);
					newScrollBarView.Position = listView.LeftItem;
					Assert.Equal (newScrollBarView.Position, listView.LeftItem);
					newScrollBarView.Refresh ();
				};

			top.Ready += (s, e) => {
				newScrollBarView.Position = 100;
				//Assert.Equal (newScrollBarView.Position, newScrollBarView.Size - listView.LeftItem + (listView.LeftItem - listView.Bounds.Width));
				Assert.Equal (newScrollBarView.Position, listView.LeftItem);
				//Assert.Equal (92, newScrollBarView.Position);
				//Assert.Equal (92, listView.LeftItem);
				Application.RequestStop ();
			};

			top.Add (win);

			Application.Run ();

			Application.Shutdown ();
			//});

			//Assert.Null (exception);
		}

		[Fact, AutoInitShutdown]
		public void Constructor_ScrollBarType_None_Returns_ArgumentException ()
		{
			Assert.Throws<ArgumentException> (() => new ScrollBarView (new View (), ScrollBarView.ScrollBarType.None, false));
		}

		[Fact]
		[AutoInitShutdown]
		public void Internal_Tests ()
		{
			var top = Application.Top;
			Assert.Equal (new Rect (0, 0, 80, 25), top.Bounds);
			var view = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
			top.Add (view);
			var sbv = new ScrollBarView (view, true);
			top.Add (sbv);
			Assert.Equal (view, sbv.Host);
			sbv.Size = 40;
			sbv.Position = 0;
			sbv.OtherScrollBarView.Size = 100;
			sbv.OtherScrollBarView.Position = 0;

			// Host bounds is empty.
			Assert.False (sbv.CanScroll (10, out int max, sbv.IsVertical));
			Assert.Equal (0, max);
			Assert.False (sbv.OtherScrollBarView.CanScroll (10, out max, sbv.OtherScrollBarView.IsVertical));
			Assert.Equal (0, max);

			Application.Begin (top);

			// They aren't visible so they aren't drawn.
			Assert.False (sbv.Visible);
			Assert.False (sbv.OtherScrollBarView.Visible);
			top.LayoutSubviews ();
			// Now the host bounds is not empty.
			Assert.True (sbv.CanScroll (10, out max, sbv.IsVertical));
			Assert.Equal (10, max);
			Assert.True (sbv.OtherScrollBarView.CanScroll (10, out max, sbv.OtherScrollBarView.IsVertical));
			Assert.Equal (10, max);
			Assert.True (sbv.CanScroll (50, out max, sbv.IsVertical));
			Assert.Equal (40, sbv.Size);
			Assert.Equal (15, max); // 15+25=40
			Assert.True (sbv.OtherScrollBarView.CanScroll (150, out max, sbv.OtherScrollBarView.IsVertical));
			Assert.Equal (100, sbv.OtherScrollBarView.Size);
			Assert.Equal (20, max); // 20+80=100
			Assert.False (sbv.Visible);
			Assert.False (sbv.OtherScrollBarView.Visible);
			sbv.KeepContentAlwaysInViewport = false;
			sbv.OtherScrollBarView.KeepContentAlwaysInViewport = false;
			Assert.True (sbv.CanScroll (50, out max, sbv.IsVertical));
			Assert.Equal (39, max);
			Assert.True (sbv.OtherScrollBarView.CanScroll (150, out max, sbv.OtherScrollBarView.IsVertical));
			Assert.Equal (99, max);
			Assert.True (sbv.Visible);
			Assert.True (sbv.OtherScrollBarView.Visible);
		}

		[Fact, AutoInitShutdown]
		public void Hosting_ShowBothScrollIndicator_Invisible ()
		{
			var textView = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = "This is the help text for the Second Step.\n\nPress the button to see a message box.\n\nEnter name too."
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (textView);

			var scrollBar = new ScrollBarView (textView, true);

			scrollBar.ChangedPosition += (s, e) => {
				textView.TopRow = scrollBar.Position;
				if (textView.TopRow != scrollBar.Position) {
					scrollBar.Position = textView.TopRow;
				}
				textView.SetNeedsDisplay ();
			};

			scrollBar.OtherScrollBarView.ChangedPosition += (s, e) => {
				textView.LeftColumn = scrollBar.OtherScrollBarView.Position;
				if (textView.LeftColumn != scrollBar.OtherScrollBarView.Position) {
					scrollBar.OtherScrollBarView.Position = textView.LeftColumn;
				}
				textView.SetNeedsDisplay ();
			};

			scrollBar.VisibleChanged += (s, e) => {
				if (scrollBar.Visible && textView.RightOffset == 0) {
					textView.RightOffset = 1;
				} else if (!scrollBar.Visible && textView.RightOffset == 1) {
					textView.RightOffset = 0;
				}
			};

			scrollBar.OtherScrollBarView.VisibleChanged += (s, e) => {
				if (scrollBar.OtherScrollBarView.Visible && textView.BottomOffset == 0) {
					textView.BottomOffset = 1;
				} else if (!scrollBar.OtherScrollBarView.Visible && textView.BottomOffset == 1) {
					textView.BottomOffset = 0;
				}
			};

			textView.LayoutComplete += (s, e) => {
				scrollBar.Size = textView.Lines;
				scrollBar.Position = textView.TopRow;
				if (scrollBar.OtherScrollBarView != null) {
					scrollBar.OtherScrollBarView.Size = textView.Maxlength;
					scrollBar.OtherScrollBarView.Position = textView.LeftColumn;
				}
				scrollBar.LayoutSubviews ();
				scrollBar.Refresh ();
			};
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (45, 20);

			Assert.True (scrollBar.AutoHideScrollBars);
			Assert.False (scrollBar.ShowScrollIndicator);
			Assert.False (scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.Equal (5, textView.Lines);
			Assert.Equal (42, textView.Maxlength);
			Assert.Equal (0, textView.LeftColumn);
			Assert.Equal (0, scrollBar.Position);
			Assert.Equal (0, scrollBar.OtherScrollBarView.Position);
			var expected = @"
┌───────────────────────────────────────────┐
│This is the help text for the Second Step. │
│                                           │
│Press the button to see a message box.     │
│                                           │
│Enter name too.                            │
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
└───────────────────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 45, 20), pos);

			textView.WordWrap = true;
			((FakeDriver)Application.Driver).SetBufferSize (26, 20);
			Application.Refresh ();

			Assert.True (textView.WordWrap);
			Assert.True (scrollBar.AutoHideScrollBars);
			Assert.Equal (7, textView.Lines);
			Assert.Equal (22, textView.Maxlength);
			Assert.Equal (0, textView.LeftColumn);
			Assert.Equal (0, scrollBar.Position);
			Assert.Equal (0, scrollBar.OtherScrollBarView.Position);
			expected = @"
┌────────────────────────┐
│This is the help text   │
│for the Second Step.    │
│                        │
│Press the button to     │
│see a message box.      │
│                        │
│Enter name too.         │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
└────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 26, 20), pos);

			((FakeDriver)Application.Driver).SetBufferSize (10, 10);
			Application.Refresh ();

			Assert.True (textView.WordWrap);
			Assert.True (scrollBar.AutoHideScrollBars);
			Assert.Equal (20, textView.Lines);
			Assert.Equal (7, textView.Maxlength);
			Assert.Equal (0, textView.LeftColumn);
			Assert.Equal (0, scrollBar.Position);
			Assert.Equal (0, scrollBar.OtherScrollBarView.Position);
			Assert.True (scrollBar.ShowScrollIndicator);
			expected = @"
┌────────┐
│This   ▲│
│is the ┬│
│help   ││
│text   ┴│
│for    ░│
│the    ░│
│Second ░│
│Step.  ▼│
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 10), pos);
		}



		[Fact, AutoInitShutdown]
		public void ContentBottomRightCorner_Not_Redraw_If_Both_Size_Equal_To_Zero ()
		{
			var text = "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
			var label = new Label (text);
			Application.Top.Add (label);

			var sbv = new ScrollBarView (label, true, true) {
				Size = 100,
			};
			sbv.OtherScrollBarView.Size = 100;
			Application.Begin (Application.Top);

			Assert.Equal (100, sbv.Size);
			Assert.Equal (100, sbv.OtherScrollBarView.Size);
			Assert.True (sbv.ShowScrollIndicator);
			Assert.True (sbv.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (sbv.Visible);
			Assert.True (sbv.OtherScrollBarView.Visible);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes▼
◄├─┤░░░░░░░░► 
", output);

			sbv.Size = 0;
			sbv.OtherScrollBarView.Size = 0;
			Assert.Equal (0, sbv.Size);
			Assert.Equal (0, sbv.OtherScrollBarView.Size);
			Assert.False (sbv.ShowScrollIndicator);
			Assert.False (sbv.OtherScrollBarView.ShowScrollIndicator);
			Assert.False (sbv.Visible);
			Assert.False (sbv.OtherScrollBarView.Visible);
			Application.Top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a test
This is a test
This is a test
This is a test
This is a test
This is a test
", output);

			sbv.Size = 50;
			sbv.OtherScrollBarView.Size = 50;
			Assert.Equal (50, sbv.Size);
			Assert.Equal (50, sbv.OtherScrollBarView.Size);
			Assert.True (sbv.ShowScrollIndicator);
			Assert.True (sbv.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (sbv.Visible);
			Assert.True (sbv.OtherScrollBarView.Visible);
			Application.Top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes▼
◄├──┤░░░░░░░► 
", output);

		}

		[Fact, AutoInitShutdown]
		public void ContentBottomRightCorner_Not_Redraw_If_One_Size_Equal_To_Zero ()
		{
			var text = "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
			var label = new Label (text);
			Application.Top.Add (label);

			var sbv = new ScrollBarView (label, true, false) {
				Size = 100,
			};
			Application.Begin (Application.Top);

			Assert.Equal (100, sbv.Size);
			Assert.Null (sbv.OtherScrollBarView);
			Assert.True (sbv.ShowScrollIndicator);
			Assert.True (sbv.Visible);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes░
This is a tes▼
", output);

			sbv.Size = 0;
			Assert.Equal (0, sbv.Size);
			Assert.False (sbv.ShowScrollIndicator);
			Assert.False (sbv.Visible);
			Application.Top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a test
This is a test
This is a test
This is a test
This is a test
This is a test
", output);
		}

		[Fact, AutoInitShutdown]
		public void ShowScrollIndicator_False_Must_Also_Set_Visible_To_False_To_Not_Respond_To_Events ()
		{
			var clicked = false;
			var text = "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
			var label = new Label (text) { Width = 14, Height = 5 };
			var btn = new Button (14, 0, "Click Me!");
			btn.Clicked += (s, e) => clicked = true;
			Application.Top.Add (label, btn);

			var sbv = new ScrollBarView (label, true, false) {
				Size = 5,
			};
			Application.Begin (Application.Top);

			Assert.Equal (5, sbv.Size);
			Assert.Null (sbv.OtherScrollBarView);
			Assert.False (sbv.ShowScrollIndicator);
			Assert.False (sbv.Visible);
			TestHelpers.AssertDriverContentsWithFrameAre (@$"
This is a test{CM.Glyphs.LeftBracket} Click Me! {CM.Glyphs.RightBracket}
This is a test             
This is a test             
This is a test             
This is a test             ", output);

			ReflectionTools.InvokePrivate (
			    typeof (Application),
			    "ProcessMouseEvent",
			    new MouseEvent () {
				    X = 15,
				    Y = 0,
				    Flags = MouseFlags.Button1Clicked
			    });

			Assert.Null (Application.MouseGrabView);
			Assert.True (clicked);

			clicked = false;

			sbv.Visible = true;
			Assert.Equal (5, sbv.Size);
			Assert.False (sbv.ShowScrollIndicator);
			Assert.True (sbv.Visible);
			Application.Top.Draw ();
			Assert.False (sbv.Visible);
			TestHelpers.AssertDriverContentsWithFrameAre (@$"
This is a test{CM.Glyphs.LeftBracket} Click Me! {CM.Glyphs.RightBracket}
This is a test             
This is a test             
This is a test             
This is a test             ", output);

			ReflectionTools.InvokePrivate (
			    typeof (Application),
			    "ProcessMouseEvent",
			    new MouseEvent () {
				    X = 15,
				    Y = 0,
				    Flags = MouseFlags.Button1Clicked
			    });

			Assert.Null (Application.MouseGrabView);
			Assert.True (clicked);
			Assert.Equal (5, sbv.Size);
			Assert.False (sbv.ShowScrollIndicator);
			Assert.False (sbv.Visible);
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
			Application.Top.Draw ();
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
			Application.Top.Draw ();
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

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void VisibleChanged_Update_The_BottomOffset_Or_RightOffset ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			Assert.Equal (30, _hostView.Lines);
			Assert.Equal (100, _hostView.Cols);
			Assert.Equal (0, _hostView.Top);
			Assert.Equal (0, _hostView.Left);
			//Assert.Equal (1, _hostView.Bottom);
			//Assert.Equal (1, _hostView.Right);

			_hostView.Lines = 20;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (0, _hostView.Top);
			Assert.Equal (0, _hostView.Left);
			//Assert.Equal (1, _hostView.Bottom);
			//Assert.Equal (0, _hostView.Right);

			_hostView.Cols = 70;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (0, _hostView.Top);
			Assert.Equal (0, _hostView.Left);
			//Assert.Equal (0, _hostView.Bottom);
			//Assert.Equal (0, _hostView.Right);
		}

		[Fact, AutoInitShutdown]
		public void ScrollBarType_BottomOffset_RightOffset ()
		{
			Assert.Throws<ArgumentException> (() => new ScrollBarView (new View (), ScrollBarView.ScrollBarType.None, false));

			var hostView = new HostView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Lines = 30,
				Cols = 100
			};
			Application.Top.Add (hostView);

			Assert.Equal (ScrollBarView.ScrollBarType.None, hostView.ScrollBarType);
			Assert.Single (hostView.SuperView.Subviews);
			Assert.Null (hostView.ScrollBar);
			//Assert.Equal (0, hostView.Bottom);
			//Assert.Equal (0, hostView.Right);

			hostView.ScrollBarType = ScrollBarView.ScrollBarType.Vertical;
			Assert.Equal (3, hostView.SuperView.Subviews.Count);
			Assert.NotNull (hostView.ScrollBar);
			Assert.True (hostView.ScrollBar.IsVertical);
			Assert.Null (hostView.ScrollBar.OtherScrollBarView);
			//Assert.Equal (1, hostView.Bottom);
			//Assert.Equal (0, hostView.Right);

			hostView.ScrollBarType = ScrollBarView.ScrollBarType.Horizontal;
			Assert.Equal (3, hostView.SuperView.Subviews.Count);
			Assert.NotNull (hostView.ScrollBar);
			Assert.False (hostView.ScrollBar.IsVertical);
			Assert.Null (hostView.ScrollBar.OtherScrollBarView);
			//Assert.Equal (0, hostView.Bottom);
			//Assert.Equal (1, hostView.Right);

			hostView.ScrollBarType = ScrollBarView.ScrollBarType.Both;
			Assert.Equal (4, hostView.SuperView.Subviews.Count);
			Assert.NotNull (hostView.ScrollBar);
			Assert.True (hostView.ScrollBar.IsVertical);
			Assert.NotNull (hostView.ScrollBar.OtherScrollBarView);
			Assert.False (hostView.ScrollBar.OtherScrollBarView.IsVertical);
			//Assert.Equal (1, hostView.Bottom);
			//Assert.Equal (1, hostView.Right);

			hostView.ScrollBarType = ScrollBarView.ScrollBarType.None;
			Assert.Single (hostView.SuperView.Subviews);
			Assert.Null (hostView.ScrollBar);
			//Assert.Equal (0, hostView.Bottom);
			//Assert.Equal (0, hostView.Right);
		}

		[Fact, AutoInitShutdown]
		public void Hosting_A_View_To_A_ScrollBarView_Built_In ()
		{
			var hostViewAuto = new HostViewAuto () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Lines = 30,
				Cols = 100
			};
			Application.Top.Add (hostViewAuto);

			hostViewAuto.ScrollBarType = ScrollBarView.ScrollBarType.Both;

			Assert.True (hostViewAuto.ScrollBar.IsVertical);
			Assert.False (hostViewAuto.ScrollBar.OtherScrollBarView.IsVertical);

			Assert.Equal (hostViewAuto.ScrollBar.Position, hostViewAuto.Top);
			Assert.NotEqual (hostViewAuto.ScrollBar.Size, hostViewAuto.Lines);
			Assert.Equal (hostViewAuto.ScrollBar.OtherScrollBarView.Position, hostViewAuto.Left);
			Assert.NotEqual (hostViewAuto.ScrollBar.OtherScrollBarView.Size, hostViewAuto.Cols);

			hostViewAuto.SuperView.LayoutSubviews ();
			hostViewAuto.Redraw (hostViewAuto.Bounds);

			Assert.Equal (hostViewAuto.ScrollBar.Position, hostViewAuto.Top);
			Assert.Equal (hostViewAuto.ScrollBar.Size, hostViewAuto.Lines);
			Assert.Equal (hostViewAuto.ScrollBar.OtherScrollBarView.Position, hostViewAuto.Left);
			Assert.Equal (hostViewAuto.ScrollBar.OtherScrollBarView.Size, hostViewAuto.Cols);
			Assert.Equal (1, hostViewAuto.Bottom);
			Assert.Equal (1, hostViewAuto.Right);

			hostViewAuto.Lines = 20;
			hostViewAuto.Redraw (hostViewAuto.Bounds);
		}

		[Fact, AutoInitShutdown]
		public void VisibleChanged_Update_The_BottomOffset_Or_RightOffset_Built_In ()
		{
			var hostViewAuto = new HostViewAuto () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ScrollBarType = ScrollBarView.ScrollBarType.Both,
				Lines = 30,
				Cols = 100
			};
			Application.Top.Add (hostViewAuto);
			Application.Begin (Application.Top);

			Assert.Equal (30, hostViewAuto.Lines);
			Assert.Equal (100, hostViewAuto.Cols);
			Assert.Equal (0, hostViewAuto.Top);
			Assert.Equal (0, hostViewAuto.Left);
			Assert.Equal (1, hostViewAuto.Bottom);
			Assert.Equal (1, hostViewAuto.Right);

			hostViewAuto.Lines = 20;
			hostViewAuto.Redraw (hostViewAuto.Bounds);
			Assert.Equal (0, hostViewAuto.Top);
			Assert.Equal (0, hostViewAuto.Left);
			Assert.Equal (1, hostViewAuto.Bottom);
			Assert.Equal (0, hostViewAuto.Right);

			hostViewAuto.Cols = 70;
			hostViewAuto.Redraw (hostViewAuto.Bounds);
			Assert.Equal (0, hostViewAuto.Top);
			Assert.Equal (0, hostViewAuto.Left);
			Assert.Equal (0, hostViewAuto.Bottom);
			Assert.Equal (0, hostViewAuto.Right);
		}

		[Fact, AutoInitShutdown]
		public void ScrollBarType_BottomOffset_RightOffset_Built_In ()
		{
			Assert.Throws<ArgumentException> (() => new ScrollBarView (new View (), ScrollBarView.ScrollBarType.None, true));

			var hostViewAuto = new HostViewAuto () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Lines = 30,
				Cols = 100
			};
			Application.Top.Add (hostViewAuto);
			Application.Begin (Application.Top);

			Assert.Single (hostViewAuto.SuperView.Subviews);

			Assert.Equal (ScrollBarView.ScrollBarType.None, hostViewAuto.ScrollBarType);
			Assert.Single (hostViewAuto.SuperView.Subviews);
			Assert.Null (hostViewAuto.ScrollBar);
			Assert.Equal (0, hostViewAuto.Bottom);
			Assert.Equal (0, hostViewAuto.Right);

			hostViewAuto.ScrollBarType = ScrollBarView.ScrollBarType.Vertical;
			Assert.Equal (3, hostViewAuto.SuperView.Subviews.Count);
			Assert.NotNull (hostViewAuto.ScrollBar);
			Assert.True (hostViewAuto.ScrollBar.IsVertical);
			Assert.Null (hostViewAuto.ScrollBar.OtherScrollBarView);
			Assert.Equal (0, hostViewAuto.Bottom);
			Assert.Equal (1, hostViewAuto.Right);

			hostViewAuto.ScrollBarType = ScrollBarView.ScrollBarType.Horizontal;
			Assert.Equal (3, hostViewAuto.SuperView.Subviews.Count);
			Assert.NotNull (hostViewAuto.ScrollBar);
			Assert.False (hostViewAuto.ScrollBar.IsVertical);
			Assert.Null (hostViewAuto.ScrollBar.OtherScrollBarView);
			Assert.Equal (1, hostViewAuto.Bottom);
			Assert.Equal (0, hostViewAuto.Right);

			hostViewAuto.ScrollBarType = ScrollBarView.ScrollBarType.Both;
			Assert.Equal (4, hostViewAuto.SuperView.Subviews.Count);
			Assert.NotNull (hostViewAuto.ScrollBar);
			Assert.True (hostViewAuto.ScrollBar.IsVertical);
			Assert.NotNull (hostViewAuto.ScrollBar.OtherScrollBarView);
			Assert.False (hostViewAuto.ScrollBar.OtherScrollBarView.IsVertical);
			Assert.Equal (1, hostViewAuto.Bottom);
			Assert.Equal (1, hostViewAuto.Right);

			hostViewAuto.ScrollBarType = ScrollBarView.ScrollBarType.None;
			Assert.Single (hostViewAuto.SuperView.Subviews);
			Assert.Null (hostViewAuto.ScrollBar);
			Assert.Equal (0, hostViewAuto.Bottom);
			Assert.Equal (0, hostViewAuto.Right);
		}

		[Fact, AutoInitShutdown]
		public void Width_Equal_To_Zero_Or_Height_Equal_To_Zero_Disable_Both_ShowScrollIndicator ()
		{
			var hostView = new HostView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Lines = 30,
				Cols = 100,
				ScrollBarType = ScrollBarView.ScrollBarType.Both
			};
			Application.Top.Add (hostView);
			Application.Begin (Application.Top);

			Assert.True (hostView.ScrollBar.ShowScrollIndicator);
			Assert.True (hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);

			hostView.Width = 0;
			Application.Refresh ();
			Assert.False (hostView.ScrollBar.ShowScrollIndicator);
			Assert.False (hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);

			hostView.Width = Dim.Fill ();
			Application.Refresh ();
			Assert.True (hostView.ScrollBar.ShowScrollIndicator);
			Assert.True (hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);

			hostView.Height = 0;
			Application.Refresh ();
			Assert.False (hostView.ScrollBar.ShowScrollIndicator);
			Assert.False (hostView.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
		}

		[Fact, AutoInitShutdown]
		public void Width_Equal_To_Zero_Or_Height_Equal_To_Zero_Disable_Both_ShowScrollIndicator_Built_In ()
		{
			var hostViewAuto = new HostViewAuto () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Lines = 30,
				Cols = 100,
				ScrollBarType = ScrollBarView.ScrollBarType.Both
			};
			Application.Top.Add (hostViewAuto);
			Application.Begin (Application.Top);

			Assert.True (hostViewAuto.ScrollBar.ShowScrollIndicator);
			Assert.True (hostViewAuto.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Line0:6789101112131415161718192021222324252627282930313233343536373839404142434▲
Line1:6789101112131415161718192021222324252627282930313233343536373839404142434┬
Line2:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line3:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line4:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line5:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line6:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line7:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line8:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line9:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line10:789101112131415161718192021222324252627282930313233343536373839404142434│
Line11:789101112131415161718192021222324252627282930313233343536373839404142434│
Line12:789101112131415161718192021222324252627282930313233343536373839404142434│
Line13:789101112131415161718192021222324252627282930313233343536373839404142434│
Line14:789101112131415161718192021222324252627282930313233343536373839404142434│
Line15:789101112131415161718192021222324252627282930313233343536373839404142434│
Line16:789101112131415161718192021222324252627282930313233343536373839404142434│
Line17:789101112131415161718192021222324252627282930313233343536373839404142434│
Line18:789101112131415161718192021222324252627282930313233343536373839404142434┴
Line19:789101112131415161718192021222324252627282930313233343536373839404142434░
Line20:789101112131415161718192021222324252627282930313233343536373839404142434░
Line21:789101112131415161718192021222324252627282930313233343536373839404142434░
Line22:789101112131415161718192021222324252627282930313233343536373839404142434░
Line23:789101112131415161718192021222324252627282930313233343536373839404142434▼
◄├───────────────────────────────────────────────────────────┤░░░░░░░░░░░░░░░░► 
", output);

			hostViewAuto.Width = 0;
			Application.Refresh ();
			Assert.False (hostViewAuto.ScrollBar.ShowScrollIndicator);
			Assert.False (hostViewAuto.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
", output);

			hostViewAuto.Width = Dim.Fill ();
			Application.Refresh ();
			Assert.True (hostViewAuto.ScrollBar.ShowScrollIndicator);
			Assert.True (hostViewAuto.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Line0:6789101112131415161718192021222324252627282930313233343536373839404142434▲
Line1:6789101112131415161718192021222324252627282930313233343536373839404142434┬
Line2:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line3:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line4:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line5:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line6:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line7:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line8:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line9:6789101112131415161718192021222324252627282930313233343536373839404142434│
Line10:789101112131415161718192021222324252627282930313233343536373839404142434│
Line11:789101112131415161718192021222324252627282930313233343536373839404142434│
Line12:789101112131415161718192021222324252627282930313233343536373839404142434│
Line13:789101112131415161718192021222324252627282930313233343536373839404142434│
Line14:789101112131415161718192021222324252627282930313233343536373839404142434│
Line15:789101112131415161718192021222324252627282930313233343536373839404142434│
Line16:789101112131415161718192021222324252627282930313233343536373839404142434│
Line17:789101112131415161718192021222324252627282930313233343536373839404142434│
Line18:789101112131415161718192021222324252627282930313233343536373839404142434┴
Line19:789101112131415161718192021222324252627282930313233343536373839404142434░
Line20:789101112131415161718192021222324252627282930313233343536373839404142434░
Line21:789101112131415161718192021222324252627282930313233343536373839404142434░
Line22:789101112131415161718192021222324252627282930313233343536373839404142434░
Line23:789101112131415161718192021222324252627282930313233343536373839404142434▼
◄├───────────────────────────────────────────────────────────┤░░░░░░░░░░░░░░░░► 
", output);

			hostViewAuto.Height = 0;
			Application.Refresh ();
			Assert.False (hostViewAuto.ScrollBar.ShowScrollIndicator);
			Assert.False (hostViewAuto.ScrollBar.OtherScrollBarView.ShowScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
", output);
		}
	}
}
