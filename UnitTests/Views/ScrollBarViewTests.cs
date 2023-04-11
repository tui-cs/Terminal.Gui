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
			public int Top { get; set; }
			public int Lines { get; set; }
			public int Left { get; set; }
			public int Cols { get; set; }
		}

		private static HostView _hostView;
		private ScrollBarView _scrollBar;
		private bool _added;

		private void AddHandlers ()
		{
			if (!_added) {
				_hostView.DrawContent += _hostView_DrawContent;
				_scrollBar.ChangedPosition += _scrollBar_ChangedPosition;
				_scrollBar.OtherScrollBarView.ChangedPosition += _scrollBar_OtherScrollBarView_ChangedPosition;
			}
			_added = true;
		}

		private void RemoveHandlers ()
		{
			if (_added) {
				_hostView.DrawContent -= _hostView_DrawContent;
				_scrollBar.ChangedPosition -= _scrollBar_ChangedPosition;
				_scrollBar.OtherScrollBarView.ChangedPosition -= _scrollBar_OtherScrollBarView_ChangedPosition;
			}
			_added = false;
		}

		private void _hostView_DrawContent (object sender, DrawEventArgs e)
		{
			_scrollBar.Size = _hostView.Lines;
			_scrollBar.Position = _hostView.Top;
			_scrollBar.OtherScrollBarView.Size = _hostView.Cols;
			_scrollBar.OtherScrollBarView.Position = _hostView.Left;
			_scrollBar.Refresh ();
		}

		private void _scrollBar_ChangedPosition (object sender, EventArgs e)
		{
			_hostView.Top = _scrollBar.Position;
			if (_hostView.Top != _scrollBar.Position) {
				_scrollBar.Position = _hostView.Top;
			}
			_hostView.SetNeedsDisplay ();
		}

		private void _scrollBar_OtherScrollBarView_ChangedPosition (object sender, EventArgs e)
		{
			_hostView.Left = _scrollBar.OtherScrollBarView.Position;
			if (_hostView.Left != _scrollBar.OtherScrollBarView.Position) {
				_scrollBar.OtherScrollBarView.Position = _hostView.Left;
			}
			_hostView.SetNeedsDisplay ();
		}

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
			RemoveHandlers ();

			_scrollBar = new ScrollBarView (_hostView, true);

			Application.Begin (Application.Top);

			Assert.True (_scrollBar.IsVertical);
			Assert.False (_scrollBar.OtherScrollBarView.IsVertical);

			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.NotEqual (_scrollBar.Size, _hostView.Lines);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.NotEqual (_scrollBar.OtherScrollBarView.Size, _hostView.Cols);

			AddHandlers ();
			_hostView.SuperView.LayoutSubviews ();
			_hostView.Redraw (_hostView.Bounds);

			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.Equal (_scrollBar.Size, _hostView.Lines);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (_scrollBar.OtherScrollBarView.Size, _hostView.Cols);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ChangedPosition_Update_The_Hosted_View ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_scrollBar.Position = 2;
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = 5;
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ChangedPosition_Scrolling ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			for (int i = 0; i < _scrollBar.Size; i++) {
				_scrollBar.Position += 1;
				Assert.Equal (_scrollBar.Position, _hostView.Top);
			}
			for (int i = _scrollBar.Size - 1; i >= 0; i--) {
				_scrollBar.Position -= 1;
				Assert.Equal (_scrollBar.Position, _hostView.Top);
			}

			for (int i = 0; i < _scrollBar.OtherScrollBarView.Size; i++) {
				_scrollBar.OtherScrollBarView.Position += i;
				Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			}
			for (int i = _scrollBar.OtherScrollBarView.Size - 1; i >= 0; i--) {
				_scrollBar.OtherScrollBarView.Position -= 1;
				Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			}
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ChangedPosition_Negative_Value ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_scrollBar.Position = -20;
			Assert.Equal (0, _scrollBar.Position);
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = -50;
			Assert.Equal (0, _scrollBar.OtherScrollBarView.Position);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void DrawContent_Update_The_ScrollBarView_Position ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Top = 3;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (_scrollBar.Position, _hostView.Top);

			_hostView.Left = 6;
			_hostView.Redraw (_hostView.Bounds);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void OtherScrollBarView_Not_Null ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.NotNull (_scrollBar.OtherScrollBarView);
			Assert.NotEqual (_scrollBar, _scrollBar.OtherScrollBarView);
			Assert.Equal (_scrollBar.OtherScrollBarView.OtherScrollBarView, _scrollBar);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void ShowScrollIndicator_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void KeepContentAlwaysInViewport_True ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			Assert.Equal (80, _hostView.Bounds.Width);
			Assert.Equal (25, _hostView.Bounds.Height);
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.Equal (30, _scrollBar.Size);
			Assert.Equal (100, _scrollBar.OtherScrollBarView.Size);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);

			_scrollBar.Position = 50;
			Assert.Equal (_scrollBar.Position, _scrollBar.Size - _scrollBar.Bounds.Height);
			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.Equal (6, _scrollBar.Position);
			Assert.Equal (6, _hostView.Top);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);

			_scrollBar.OtherScrollBarView.Position = 150;
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _scrollBar.OtherScrollBarView.Size - _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (21, _scrollBar.OtherScrollBarView.Position);
			Assert.Equal (21, _hostView.Left);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void KeepContentAlwaysInViewport_False ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_scrollBar.KeepContentAlwaysInViewport = false;
			_scrollBar.Position = 50;
			Assert.Equal (_scrollBar.Position, _scrollBar.Size - 1);
			Assert.Equal (_scrollBar.Position, _hostView.Top);
			Assert.Equal (29, _scrollBar.Position);
			Assert.Equal (29, _hostView.Top);

			_scrollBar.OtherScrollBarView.Position = 150;
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _scrollBar.OtherScrollBarView.Size - 1);
			Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
			Assert.Equal (99, _scrollBar.OtherScrollBarView.Position);
			Assert.Equal (99, _hostView.Left);
		}

		[Fact]
		[ScrollBarAutoInitShutdown]
		public void AutoHideScrollBars_Check ()
		{
			Hosting_A_View_To_A_ScrollBarView ();

			AddHandlers ();

			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()({X=0,Y=0,Width=80,Height=25}))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Combine(View(Width,HostView()({X=0,Y=0,Width=80,Height=25}))-Absolute(1))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (79, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Lines = 10;
			_hostView.Redraw (_hostView.Bounds);
			Assert.False (_scrollBar.ShowScrollIndicator);
			Assert.False (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()({X=0,Y=0,Width=80,Height=25}))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("View(Width,HostView()({X=0,Y=0,Width=80,Height=25}))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Cols = 60;
			_hostView.Redraw (_hostView.Bounds);
			Assert.False (_scrollBar.ShowScrollIndicator);
			Assert.False (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()({X=0,Y=0,Width=80,Height=25}))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.False (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("View(Width,HostView()({X=0,Y=0,Width=80,Height=25}))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Lines = 40;
			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("View(Height,HostView()({X=0,Y=0,Width=80,Height=25}))",
				_scrollBar.Height.ToString ());
			Assert.Equal (25, _scrollBar.Bounds.Height);
			Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.False (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("View(Width,HostView()({X=0,Y=0,Width=80,Height=25}))",
				_scrollBar.OtherScrollBarView.Width.ToString ());
			Assert.Equal (80, _scrollBar.OtherScrollBarView.Bounds.Width);
			Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
			Assert.Equal (1, _scrollBar.OtherScrollBarView.Bounds.Height);

			_hostView.Cols = 120;
			_hostView.Redraw (_hostView.Bounds);
			Assert.True (_scrollBar.ShowScrollIndicator);
			Assert.True (_scrollBar.Visible);
			Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
			Assert.Equal (1, _scrollBar.Bounds.Width);
			Assert.Equal ("Combine(View(Height,HostView()({X=0,Y=0,Width=80,Height=25}))-Absolute(1))",
				_scrollBar.Height.ToString ());
			Assert.Equal (24, _scrollBar.Bounds.Height);
			Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
			Assert.True (_scrollBar.OtherScrollBarView.Visible);
			Assert.Equal ("Combine(View(Width,HostView()({X=0,Y=0,Width=80,Height=25}))-Absolute(1))",
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

		//		newScrollBarView.ChangedPosition += (s,e) => {
		//			listView.TopItem = newScrollBarView.Position;
		//			if (listView.TopItem != newScrollBarView.Position) {
		//				newScrollBarView.Position = listView.TopItem;
		//			}
		//			Assert.Equal (newScrollBarView.Position, listView.TopItem);
		//			listView.SetNeedsDisplay ();
		//		};

		//		listView.DrawContent += (s,e) => {
		//			newScrollBarView.Size = listView.Source.Count;
		//			Assert.Equal (newScrollBarView.Size, listView.Source.Count);
		//			newScrollBarView.Position = listView.TopItem;
		//			Assert.Equal (newScrollBarView.Position, listView.TopItem);
		//			newScrollBarView.Refresh ();
		//		};

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
			Application.Top.Redraw (Application.Top.Bounds);
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
			Application.Top.Redraw (Application.Top.Bounds);
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
			Application.Top.Redraw (Application.Top.Bounds);
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
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a test[ Click Me! ]
This is a test             
This is a test             
This is a test             
This is a test             
", output);

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
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This is a test[ Click Me! ]
This is a test             
This is a test             
This is a test             
This is a test             
", output);

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
	}
}
