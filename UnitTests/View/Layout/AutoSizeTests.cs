using System.Text;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class AutoSizeTests {
		readonly ITestOutputHelper output;

		public AutoSizeTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_GetAutoSize_Horizontal ()
		{
			var text = "text";
			var view = new View () {
				Text = text,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 1), size);

			view.Text = $"{text}\n{text}";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 2), size);

			view.Text = $"{text}\n{text}\n{text}+";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length + 1, 3), size);

			text = string.Empty;
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (0, 0), size);

			text = "1";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (1, 1), size);

			text = "界";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (2, 1), size);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_GetAutoSize_Vertical()
		{
			var text = "text";
			var view = new View () {
				Text = text,
				TextDirection = TextDirection.TopBottom_LeftRight,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var size = view.GetAutoSize ();
			Assert.Equal (new Size (1, text.Length), size);

			view.Text = $"{text}\n{text}";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (2, text.Length), size);

			view.Text = $"{text}\n{text}\n{text}+";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (3, text.Length + 1), size);

			text = string.Empty;
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (0, 0), size);

			text = "1";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (1, 1), size);

			text = "界";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (2, 1), size);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_GetAutoSize_Left()
		{
			var text = "This is some text.";
			var view = new View () {
				Text = text,
				TextAlignment = TextAlignment.Left,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 1), size);

			view.Text = $"{text}\n{text}";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 2), size);

			view.Text = $"{text}\n{text}\n{text}+";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length + 1, 3), size);

			text = string.Empty;
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (0, 0), size);

			text = "1";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (1, 1), size);

			text = "界";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (2, 1), size);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_GetAutoSize_Right ()
		{
			var text = "This is some text.";
			var view = new View () {
				Text = text,
				TextAlignment = TextAlignment.Right,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 1), size);

			view.Text = $"{text}\n{text}";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 2), size);

			view.Text = $"{text}\n{text}\n{text}+";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length + 1, 3), size);

			text = string.Empty;
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (0, 0), size);

			text = "1";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (1, 1), size);

			text = "界";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (2, 1), size);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_GetAutoSize_Centered ()
		{
			var text = "This is some text.";
			var view = new View () {
				Text = text,
				TextAlignment = TextAlignment.Centered,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 1), size);

			view.Text = $"{text}\n{text}";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length, 2), size);

			view.Text = $"{text}\n{text}\n{text}+";
			size = view.GetAutoSize ();
			Assert.Equal (new Size (text.Length + 1, 3), size);

			text = string.Empty;
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (0, 0), size);

			text = "1";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (1, 1), size);

			text = "界";
			view.Text = text;
			size = view.GetAutoSize ();
			Assert.Equal (new Size (2, 1), size);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_View_IsEmpty_False_Return_Null_Lines ()
		{
			var text = "Views";
			var view = new View () {
				Width = Dim.Fill () - text.Length,
				Height = 1,
				Text = text
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			Assert.Equal (5, text.Length);
			Assert.False (view.AutoSize);
			Assert.Equal (new Rect (0, 0, 3, 1), view.Frame);
			Assert.Equal (new Size (3, 1), view.TextFormatter.Size);
			Assert.Equal (new List<string> () { "Vie" }, view.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
			Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
			var expected = @"
┌────────┐
│Vie     │
│        │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			view.Width = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 0, 1), view.Frame);
			Assert.Equal (new Size (0, 1), view.TextFormatter.Size);
			Assert.Equal (new List<string> () { string.Empty }, view.TextFormatter.Lines);
			expected = @"
┌────────┐
│        │
│        │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_View_IsEmpty_True_Minimum_Height ()
		{
			var text = "Views";
			var view = new View () {
				Width = Dim.Fill () - text.Length,
				Text = text
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			Assert.Equal (5, text.Length);
			Assert.False (view.AutoSize);
			Assert.Equal (new Rect (0, 0, 3, 1), view.Frame);
			Assert.Equal (new Size (3, 1), view.TextFormatter.Size);
			Assert.Single (view.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
			Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
			var expected = @"
┌────────┐
│Vie     │
│        │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			view.Width = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 0, 1), view.Frame);
			Assert.Equal (new Size (0, 1), view.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Equal (new List<string> () { string.Empty }, view.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌────────┐
│        │
│        │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_Label_IsEmpty_False_Never_Return_Null_Lines ()
		{
			var text = "Label";
			var label = new Label () {
				Width = Dim.Fill () - text.Length,
				Height = 1,
				Text = text
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (label);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			Assert.Equal (5, text.Length);
			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
			Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
			Assert.Equal (new List<string> () { "Label" }, label.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
			Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
			var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			label.Width = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
			Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
			Assert.Single (label.TextFormatter.Lines);
			expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_Label_IsEmpty_True_Return_Null_Lines ()
		{
			var text = "Label";
			var label = new Label () {
				Width = Dim.Fill () - text.Length,
				Height = 1,
				Text = text,
				AutoSize = false
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (label);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			Assert.Equal (5, text.Length);
			Assert.False (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 3, 1), label.Frame);
			Assert.Equal (new Size (3, 1), label.TextFormatter.Size);
			Assert.Equal (new List<string> () { "Lab" }, label.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
			Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
			var expected = @"
┌────────┐
│Lab     │
│        │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			label.Width = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.False (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 1), label.Frame);
			Assert.Equal (new Size (0, 1), label.TextFormatter.Size);
			Assert.Equal (new List<string> { string.Empty }, label.TextFormatter.Lines);
			expected = @"
┌────────┐
│        │
│        │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_Label_IsEmpty_False_Minimum_Height ()
		{
			var text = "Label";
			var label = new Label () {
				Width = Dim.Fill () - text.Length,
				Text = text
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (label);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			Assert.Equal (5, text.Length);
			Assert.True (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
			Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
			Assert.Equal (new List<string> () { "Label" }, label.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
			Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
			var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			label.Width = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
			Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Single (label.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_Label_Height_Zero_Returns_Minimum_Height ()
		{
			var text = "Label";
			var label = new Label () {
				Width = Dim.Fill () - text.Length,
				Text = text,
				AutoSize = false
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (label);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			Assert.Equal (5, text.Length);
			Assert.False (label.AutoSize);
			Assert.Equal (new Rect (0, 0, 3, 1), label.Frame);
			Assert.Equal (new Size (3, 1), label.TextFormatter.Size);
			Assert.Single (label.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
			Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
			var expected = @"
┌────────┐
│Lab     │
│        │
└────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			label.Width = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 0, 1), label.Frame);
			Assert.Equal (new Size (0, 1), label.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Equal (new List<string> () { string.Empty }, label.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌────────┐
│        │
│        │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 10, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_View_IsEmpty_False_Minimum_Width ()
		{
			var text = "Views";
			var view = new View () {
				TextDirection = TextDirection.TopBottom_LeftRight,
				Height = Dim.Fill () - text.Length,
				Text = text,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (4, 10);

			Assert.Equal (5, text.Length);
			Assert.True (view.AutoSize);
			Assert.Equal (new Rect (0, 0, 1, 5), view.Frame);
			Assert.Equal (new Size (1, 5), view.TextFormatter.Size);
			Assert.Equal (new List<string> () { "Views" }, view.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
			Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
			var expected = @"
┌──┐
│V │
│i │
│e │
│w │
│s │
│  │
│  │
│  │
└──┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			view.Height = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 1, 5), view.Frame);
			Assert.Equal (new Size (1, 5), view.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Single (view.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌──┐
│V │
│i │
│e │
│w │
│s │
│  │
│  │
│  │
└──┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_View_Width_Null_Returns_Host_Frame_Width ()
		{
			var text = "Views";
			var view = new View () {
				TextDirection = TextDirection.TopBottom_LeftRight,
				Height = Dim.Fill () - text.Length,
				Text = text
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (4, 10);

			Assert.Equal (5, text.Length);
			Assert.False (view.AutoSize);
			Assert.Equal (new Rect (0, 0, 1, 3), view.Frame);
			Assert.Equal (new Size (1, 3), view.TextFormatter.Size);
			Assert.Single (view.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
			Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
			var expected = @"
┌──┐
│V │
│i │
│e │
│  │
│  │
│  │
│  │
│  │
└──┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			view.Height = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 1, 0), view.Frame);
			Assert.Equal (new Size (1, 0), view.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Equal (new List<string> () { string.Empty }, view.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌──┐
│  │
│  │
│  │
│  │
│  │
│  │
│  │
│  │
└──┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_True_View_IsEmpty_False_Minimum_Width_Wide_Rune ()
		{
			var text = "界View";
			var view = new View () {
				TextDirection = TextDirection.TopBottom_LeftRight,
				Height = Dim.Fill () - text.Length,
				Text = text,
				AutoSize = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (4, 10);

			Assert.Equal (5, text.Length);
			Assert.True (view.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, 5), view.Frame);
			Assert.Equal (new Size (2, 5), view.TextFormatter.Size);
			Assert.Equal (new List<string> () { "界View" }, view.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
			Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
			var expected = @"
┌──┐
│界│
│V │
│i │
│e │
│w │
│  │
│  │
│  │
└──┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			view.Height = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 2, 5), view.Frame);
			Assert.Equal (new Size (2, 5), view.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Equal (new List<string> () { "界View" }, view.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌──┐
│界│
│V │
│i │
│e │
│w │
│  │
│  │
│  │
└──┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_View_Width_Zero_Returns_Minimum_Width_With_Wide_Rune ()
		{
			var text = "界View";
			var view = new View () {
				TextDirection = TextDirection.TopBottom_LeftRight,
				Height = Dim.Fill () - text.Length,
				Text = text
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (view);
			Application.Top.Add (win);
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (4, 10);

			Assert.Equal (5, text.Length);
			Assert.False (view.AutoSize);
			Assert.Equal (new Rect (0, 0, 2, 3), view.Frame);
			Assert.Equal (new Size (2, 3), view.TextFormatter.Size);
			Assert.Single (view.TextFormatter.Lines);
			Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
			Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
			var expected = @"
┌──┐
│界│
│V │
│i │
│  │
│  │
│  │
│  │
│  │
└──┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);

			text = "0123456789";
			Assert.Equal (10, text.Length);
			view.Height = Dim.Fill () - text.Length;
			Application.Refresh ();

			Assert.Equal (new Rect (0, 0, 2, 0), view.Frame);
			Assert.Equal (new Size (2, 0), view.TextFormatter.Size);
			var exception = Record.Exception (() => Assert.Equal (new List<string> () { string.Empty }, view.TextFormatter.Lines));
			Assert.Null (exception);
			expected = @"
┌──┐
│  │
│  │
│  │
│  │
│  │
│  │
│  │
│  │
└──┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 4, 10), pos);
		}
	}
}
