using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class RadioGroupTests {
		readonly ITestOutputHelper output;

		public RadioGroupTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var rg = new RadioGroup ();
			Assert.True (rg.CanFocus);
			Assert.Empty (rg.RadioLabels);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (Rect.Empty, rg.Frame);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (new NStack.ustring [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (new Rect (0, 0, 7, 1), rg.Frame);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (new Rect (1, 2, 20, 5), new NStack.ustring [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (LayoutStyle.Absolute, rg.LayoutStyle);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (new Rect (1, 2, 20, 5), rg.Frame);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (1, 2, new NStack.ustring [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (LayoutStyle.Absolute, rg.LayoutStyle);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (new Rect (1, 2, 7, 1), rg.Frame);
			Assert.Equal (0, rg.SelectedItem);
		}

		[Fact]
		public void Initialize_SelectedItem_With_Minus_One ()
		{
			var rg = new RadioGroup (new NStack.ustring [] { "Test" }, -1);
			Assert.Equal (-1, rg.SelectedItem);
			Assert.True (rg.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.Equal (0, rg.SelectedItem);
		}

		[Fact, AutoInitShutdown]
		public void DisplayMode_Width_Height_Vertical_Horizontal_Space ()
		{
			var rg = new RadioGroup (new NStack.ustring [] { "Test", "New Test 你" });
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (rg);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.Equal (DisplayModeLayout.Vertical, rg.DisplayMode);
			Assert.Equal (2, rg.RadioLabels.Length);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (14, rg.Width);
			Assert.Equal (2, rg.Height);
			var expected = @"
┌ Test Demo 你 ──────────────┐
│● Test                      │
│◌ New Test 你               │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			rg.DisplayMode = DisplayModeLayout.Horizontal;
			Application.Refresh ();

			Assert.Equal (DisplayModeLayout.Horizontal, rg.DisplayMode);
			Assert.Equal (2, rg.HorizontalSpace);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (21, rg.Width);
			Assert.Equal (1, rg.Height);

			expected = @"
┌ Test Demo 你 ──────────────┐
│● Test  ◌ New Test 你       │
│                            │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			rg.HorizontalSpace = 4;
			Application.Refresh ();

			Assert.Equal (DisplayModeLayout.Horizontal, rg.DisplayMode);
			Assert.Equal (4, rg.HorizontalSpace);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (23, rg.Width);
			Assert.Equal (1, rg.Height);
			expected = @"
┌ Test Demo 你 ──────────────┐
│● Test    ◌ New Test 你     │
│                            │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact]
		public void SelectedItemChanged_Event ()
		{
			var previousSelectedItem = -1;
			var selectedItem = -1;
			var rg = new RadioGroup (new NStack.ustring [] { "Test", "New Test" });
			rg.SelectedItemChanged += (s,e) => {
				previousSelectedItem = e.PreviousSelectedItem;
				selectedItem = e.SelectedItem;
			};

			rg.SelectedItem = 1;
			Assert.Equal (0, previousSelectedItem);
			Assert.Equal (selectedItem, rg.SelectedItem);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			var rg = new RadioGroup (new NStack.ustring [] { "Test", "New Test" });

			Assert.True (rg.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (rg.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.Equal (1, rg.SelectedItem);
		}

		[Fact]
		public void ProcessColdKey_HotKey ()
		{
			var rg = new RadioGroup (new NStack.ustring [] { "Left", "Right", "Cen_tered", "Justified" });

			Assert.True (rg.ProcessColdKey (new KeyEvent (Key.t, new KeyModifiers ())));
			Assert.Equal (2, rg.SelectedItem);
			Assert.True (rg.ProcessColdKey (new KeyEvent (Key.L, new KeyModifiers ())));
			Assert.Equal (0, rg.SelectedItem);
			Assert.True (rg.ProcessColdKey (new KeyEvent (Key.J, new KeyModifiers ())));
			Assert.Equal (3, rg.SelectedItem);
			Assert.True (rg.ProcessColdKey (new KeyEvent (Key.R, new KeyModifiers ())));
			Assert.Equal (1, rg.SelectedItem);
		}
	}
}
