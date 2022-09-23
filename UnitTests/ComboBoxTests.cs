using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.Views {
	public class ComboBoxTests {
		ITestOutputHelper output;

		public ComboBoxTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var cb = new ComboBox ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.Null (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (2, cb.Subviews.Count);

			cb = new ComboBox ("Test");
			Assert.Equal ("Test", cb.Text);
			Assert.Null (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (2, cb.Subviews.Count);

			cb = new ComboBox (new Rect (1, 2, 10, 20), new List<string> () { "One", "Two", "Three" });
			Assert.Equal (string.Empty, cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (1, 2, 10, 20), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (2, cb.Subviews.Count);

			cb = new ComboBox (new List<string> () { "One", "Two", "Three" });
			Assert.Equal (string.Empty, cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (2, cb.Subviews.Count);
		}

		[Fact]
		[AutoInitShutdown]
		public void Constructor_With_Source_Initialize_With_The_Passed_SelectedItem ()
		{
			var cb = new ComboBox (new List<string> () { "One", "Two", "Three" }) {
				SelectedItem = 1
			};
			Assert.Equal ("Two", cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 2), cb.Frame);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal (2, cb.Subviews.Count);
		}

		[Fact]
		[AutoInitShutdown]
		public void EnsureKeyEventsDoNotCauseExceptions ()
		{
			var comboBox = new ComboBox ("0");

			var source = Enumerable.Range (0, 15).Select (x => x.ToString ()).ToArray ();
			comboBox.SetSource (source);

			Application.Top.Add (comboBox);

			foreach (var key in (Key [])Enum.GetValues (typeof (Key))) {
				Assert.Null (Record.Exception (() => comboBox.ProcessKey (new KeyEvent (key, new KeyModifiers ()))));
			}
		}


		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			List<string> source = new List<string> () { "One", "Two", "Three" };
			ComboBox cb = new ComboBox () { Width = 10 };
			cb.SetSource (source);
			Application.Top.Add (cb);
			Application.Top.FocusFirst ();
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (string.Empty, cb.Text);
			var opened = false;
			cb.OpenSelectedItem += (_) => opened = true;
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (opened);
			cb.Text = "Tw";
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (opened);
			Assert.Equal ("Two", cb.Text);
			cb.SetSource (null);
			Assert.False (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ()))); // with no source also expand empty
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			cb.SetSource (source);
			cb.Text = "";
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ()))); // collapse
			Assert.False (cb.IsShow);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ()))); // expand
			Assert.True (cb.IsShow);
			cb.Collapse ();
			Assert.False (cb.IsShow);
			Assert.True (cb.HasFocus);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()))); // losing focus
			Assert.False (cb.IsShow);
			Assert.False (cb.HasFocus);
			Application.Top.FocusFirst (); // Gets focus again
			Assert.False (cb.IsShow);
			Assert.True (cb.HasFocus);
			cb.Expand ();
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Begin (Application.Top);
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One      ▼
One       
", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Application.Begin (Application.Top);
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
Two      ▼
Two       
", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Application.Begin (Application.Top);
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
Three    ▼
Three     
", output);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()))); // losing focus
			Assert.False (cb.HasFocus);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Top.FocusFirst (); // Gets focus again
			Assert.True (cb.HasFocus);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.U | Key.CtrlMask, new KeyModifiers ())));
			Assert.True (cb.HasFocus);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.Equal (3, cb.Source.Count);
		}

		[Fact]
		[AutoInitShutdown]
		public void Source_Equal_Null_Or_Count_Equal_Zero_Sets_SelectedItem_Equal_To_Minus_One ()
		{
			var cb = new ComboBox ();
			Application.Top.Add (cb);
			Application.Top.FocusFirst ();
			Assert.Null (cb.Source);
			Assert.Equal (-1, cb.SelectedItem);
			var source = new List<string> ();
			cb.SetSource (source);
			Assert.NotNull (cb.Source);
			Assert.Equal (0, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			source.Add ("One");
			Assert.Equal (1, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			source.Add ("Two");
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			cb.Text = "T";
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("T", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.Source.Count);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem); // retains last accept selected item
			Assert.Equal ("", cb.Text); // clear text
			cb.SetSource (new List<string> ());
			Assert.Equal (0, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_Gets_Sets ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			cb.HideDropdownListOnClick = true;

			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 2,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 2,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("One", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_And_Mouse ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.ReadOnly);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_ReadOnly_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false,
				ReadOnly = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.True (cb.ReadOnly);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_OpenSelectedItem_With_Mouse_And_Key_F4 ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_F4 ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Colapse_On_Click_Outside_Frame ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = -1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = -1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Frame.Width,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = cb.Frame.Height,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Highlight_Current_Item ()
		{
			var selected = "";
			var cb = new ComboBox {
				Width = 6,
				Height = 4,
				HideDropdownListOnClick = true,
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			cb.Redraw (cb.Bounds);
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
     ▼
One   
Two   
Three ", output);

			var attributes = new Attribute [] {
				// 0
				cb.Subviews [0].ColorScheme.Focus,
				// 1
				cb.Subviews [1].ColorScheme.HotFocus,
				// 2
				cb.Subviews [1].GetNormalColor ()
			};

			GraphViewTests.AssertDriverColorsAre (@"
000000
00000
22222
22222", attributes);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			cb.Redraw (cb.Bounds);
			GraphViewTests.AssertDriverColorsAre (@"
000000
22222
00000
22222", attributes);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			cb.Redraw (cb.Bounds);
			GraphViewTests.AssertDriverColorsAre (@"
000000
22222
22222
00000", attributes);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			cb.Redraw (cb.Bounds);
			GraphViewTests.AssertDriverColorsAre (@"
000000
22222
22222
00000", attributes);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			cb.Redraw (cb.Bounds);
			GraphViewTests.AssertDriverColorsAre (@"
000000
22222
00000
11111", attributes);

			Assert.True (cb.Subviews [1].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			cb.Redraw (cb.Bounds);
			GraphViewTests.AssertDriverColorsAre (@"
000000
00000
22222
11111", attributes);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void Expanded_Collapsed_Events ()
		{
			var cb = new ComboBox {
				Height = 4,
				Width = 5
			};
			var list = new List<string> { "One", "Two", "Three" };

			cb.Expanded += () => cb.SetSource (list);
			cb.Collapsed += () => cb.Source = null;

			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.Null (cb.Source);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.NotNull (cb.Source);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Null (cb.Source);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_With_Two_ComboBox_Highlight_On_Mouse_Move ()
		{
			var cb1 = new ComboBox (new List<string> { "One", "Two", "Three" }) {
				Width = 6,
				Height = 4,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};

			var cb2 = new ComboBox (new List<string> { "First", "Second", "Third" }) {
				X = Pos.Right (cb1) + 2,
				Width = 8,
				Height = 4,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};

			Application.Top.Add (cb1, cb2);
			Application.Begin (Application.Top);

			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.HideDropdownListOnClick);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 0), cb1.Subviews [1].Frame);

			Assert.Equal (2, cb2.Subviews.Count);
			Assert.True (cb2.HideDropdownListOnClick);
			Assert.False (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (new Rect (0, 1, 7, 0), cb2.Subviews [1].Frame);

			Assert.True (cb1.MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb1.Subviews [1].MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal (cb1.Subviews [1], Application.mouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), cb1.Subviews [1].Frame);
			Application.Refresh ();
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);

			var attributes = new Attribute [] {
				// 0
				cb1.Subviews [0].ColorScheme.Focus,
				// 1
				cb1.Subviews [1].ColorScheme.HotFocus,
				// 2
				cb1.Subviews [1].GetNormalColor (),
				// 3
				cb1.Subviews [1].ColorScheme.Focus,
				// 4
				Application.Top.ColorScheme.Normal
			};

			GraphViewTests.AssertDriverColorsAre (@"
0000004400000000
33333
22222
22222", attributes);

			Assert.True (cb1.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition
			}));
			Assert.Equal (cb1.Subviews [1], Application.mouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), cb1.Subviews [1].Frame);
			Application.Refresh ();
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);
			GraphViewTests.AssertDriverColorsAre (@"
0000004400000000
11111
33333
22222", attributes);

			Assert.True (cb2.MouseEvent (new MouseEvent {
				X = cb2.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb2.Subviews [1].MouseEvent (new MouseEvent {
				X = cb2.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal (cb2.Subviews [1], Application.mouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 0), cb1.Subviews [1].Frame);
			Assert.Equal (2, cb2.Subviews.Count);
			Assert.True (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (new Rect (0, 1, 7, 3), cb2.Subviews [1].Frame);

			Application.Refresh ();
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
        First   
        Second  
        Third   
", output);

			GraphViewTests.AssertDriverColorsAre (@"
0000004400000000
444444443333333
444444442222222
444444442222222", attributes);

			Assert.True (cb2.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition
			}));
			Assert.Equal (cb2.Subviews [1], Application.mouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 0), cb1.Subviews [1].Frame);
			Assert.Equal (2, cb2.Subviews.Count);
			Assert.True (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (new Rect (0, 1, 7, 3), cb2.Subviews [1].Frame);

			Application.Refresh ();
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
        First   
        Second  
        Third   
", output);

			GraphViewTests.AssertDriverColorsAre (@"
0000004400000000
444444441111111
444444443333333
444444442222222", attributes);

			Assert.True (cb1.MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.True (cb1.Subviews [1].MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal (cb1.Subviews [1], Application.mouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), cb1.Subviews [1].Frame);
			Application.Refresh ();
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);

			GraphViewTests.AssertDriverColorsAre (@"
0000004400000000
33333
22222
22222", attributes);

			Assert.True (cb1.Subviews [1].MouseEvent (new MouseEvent {
				X = 50,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Null (Application.mouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 0), cb1.Subviews [1].Frame);
			Assert.Equal (2, cb2.Subviews.Count);
			Assert.False (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (new Rect (0, 1, 7, 0), cb2.Subviews [1].Frame);

			Application.Refresh ();
			GraphViewTests.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
", output);

			GraphViewTests.AssertDriverColorsAre (@"
0000004400000000", attributes);
		}

		[Fact]
		public void HideDropdownListOnClick_WantMousePositionReports ()
		{
			var cb = new ComboBox ();
			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.WantMousePositionReports);
			Assert.False (cb.Subviews [1].WantMousePositionReports);

			cb.HideDropdownListOnClick = true;
			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.WantMousePositionReports);
			Assert.True (cb.Subviews [1].WantMousePositionReports);

			cb.HideDropdownListOnClick = false;
			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.WantMousePositionReports);
			Assert.False (cb.Subviews [1].WantMousePositionReports);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Height_Is_Zero ()
		{
			var cb1 = new ComboBox (new List<string> { "One", "Two", "Three" }) {
				Width = 6,
				Height = 4,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};

			var cb2 = new ComboBox (new List<string> { "First", "Second", "Third" }) {
				X = Pos.Right (cb1) + 2,
				Width = 8,
				Height = 4,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};

			Application.Top.Add (cb1, cb2);
			Application.Begin (Application.Top);

			Assert.True (cb1.HasFocus);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 8,
					Y = 1,
					Flags = MouseFlags.Button1Clicked
				});

			Assert.False (cb2.HasFocus);
		}
	}
}