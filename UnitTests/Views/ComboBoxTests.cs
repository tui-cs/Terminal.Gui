using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
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
			cb.BeginInit ();
			cb.EndInit ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.Null (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 1), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (1, cb.Subviews.Count);
			Assert.True (cb.HideDropdownListOnClick);

			cb = new ComboBox ("Test");
			cb.BeginInit ();
			cb.EndInit ();
			Assert.Equal ("Test", cb.Text);
			Assert.Null (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 1), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (1, cb.Subviews.Count);
			Assert.True (cb.HideDropdownListOnClick);

			cb = new ComboBox (new Rect (1, 2, 10, 20), new List<string> () { "One", "Two", "Three" });
			cb.BeginInit ();
			cb.EndInit ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (1, 2, 10, 20), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (1, cb.Subviews.Count);
			Assert.True (cb.HideDropdownListOnClick);

			cb = new ComboBox (new List<string> () { "One", "Two", "Three" });
			cb.BeginInit ();
			cb.EndInit ();
			Assert.Equal (string.Empty, cb.Text);
			Assert.NotNull (cb.Source);
			Assert.False (cb.AutoSize);
			Assert.Equal (new Rect (0, 0, 0, 1), cb.Frame);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (1, cb.Subviews.Count);
			Assert.True (cb.HideDropdownListOnClick);
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
			Assert.Equal (new Rect (0, 0, 0, 1), cb.Frame);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal (1, cb.Subviews.Count);
			Assert.True (cb.HideDropdownListOnClick);
		}

		[Fact]
		[AutoInitShutdown]
		public void Ensure_Key_Events_Do_Not_Cause_Exceptions ()
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
			Assert.True (cb.HideDropdownListOnClick);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal (string.Empty, cb.Text);
			var opened = false;
			cb.OpenSelectedItem += (s, _) => opened = true;
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (opened);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.w, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (opened);
			Assert.Equal ("Tw", cb.Text);
			cb.Text = "";
			Assert.True (cb.ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.w, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (opened);
			Assert.Equal ("Two", cb.Text);
			Assert.False (cb.IsShow);
			cb.SetSource (null);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			// with no source also expand empty with the minimum size
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			cb.SetSource (source);
			cb.Text = "";
			// collapse
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			// expand
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal ("", cb.Text);
			cb.Collapse ();
			Assert.False (cb.IsShow);
			Assert.True (cb.HasFocus);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			// losing focus
			Assert.False (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.False (cb.HasFocus);
			// Gets focus again
			Application.Top.FocusFirst ();
			Assert.False (cb.IsShow);
			Assert.True (cb.HasFocus);
			cb.Expand ();
			Assert.True (cb.IsShow);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
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
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Begin (Application.Top);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One      ▼
One       
Two       
Three     ", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Application.Begin (Application.Top);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
Three    ▼
Three     ", output);
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
			// The End and Home keys are consumed by the TextField because the ListView never get focus
			// and only the keys that aren't consumed by the TextField are forwarded to the ListView
			Assert.True (cb.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.True (cb.Subviews [0].HasFocus);
			Assert.Equal (3, ((TextField)cb.Subviews [0]).CursorPosition);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.True (cb.Subviews [0].HasFocus);
			Assert.Equal (0, ((TextField)cb.Subviews [0]).CursorPosition);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			// losing focus
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.False (cb.HasFocus);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			// Gets focus again
			Application.Top.FocusFirst ();
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
			cb.SelectedItem = -1;
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.Source.Count);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.Source.Count);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (cb.IsShow);
			// reset the selected item and text
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
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
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1DoubleClicked
			}));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			selected = "";
			// Clear selection
			cb.SelectedItem = -1;
			cb.HideDropdownListOnClick = true;

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
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
			Assert.True (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
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

			Assert.True (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
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
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("One", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
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
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
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
		public void HideDropdownListOnClick_False_OpenSelectedItem_With_Mouse_And_Key_CursorDown_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
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
		public void HideDropdownListOnClick_False_ReadOnly_True_OpenSelectedItem_With_Mouse_And_Key_CursorDown_Enter_And_Esc ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false,
				ReadOnly = true
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
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
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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
			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Two", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_False_Does_Not_OpenSelectedItem_With_Mouse_And_Key_F4 ()
		{
			var selected = "";
			var cb = new ComboBox {
				Height = 4,
				Width = 5,
				HideDropdownListOnClick = false
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("One", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
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
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
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

			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = -1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = -1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Frame.Width,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Equal ("", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);
			Assert.False (Application.Current.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = Application.Current.Subviews [1].Frame.Height,
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
				SelectedItem = 0
			};
			cb.SetSource (new List<string> { "One", "Two", "Three" });
			cb.OpenSelectedItem += (s, e) => selected = e.Value.ToString ();
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			Assert.True (cb.MouseEvent (new MouseEvent {
				X = cb.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼
One   
Two   
Three ", output);

			var attributes = new Attribute [] {
				// 0
				Application.Current.Subviews [1].ColorScheme.Focus,
				// 1
				new Attribute (cb.Subviews [0].ColorScheme.Focus.Background, cb.Subviews [0].ColorScheme.Focus.Foreground),
				// 2
				cb.Subviews [0].ColorScheme.Normal,
				// 3
				cb.Subviews [0].ColorScheme.HotFocus,
			};

			TestHelpers.AssertDriverColorsAre (@"
000000
111112
000002
000002", attributes);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
000000
000002
111112
000002", attributes);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal ("", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
000000
000002
000002
111112", attributes);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (2, cb.SelectedItem);
			Assert.Equal ("Three", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
000000
000002
000002
111112", attributes);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
000000
000002
111112
333332", attributes);

			Assert.True (Application.Current.Subviews [1].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal ("Three", selected);
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
000000
111112
000002
333332", attributes);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.F4, new KeyModifiers ())));
			Assert.Equal ("One", selected);
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
000000
222222
222222
222222", attributes);
		}

		[Fact, AutoInitShutdown]
		public void Expanded_Collapsed_Events ()
		{
			var cb = new ComboBox {
				Height = 4,
				Width = 5
			};
			var list = new List<string> { "One", "Two", "Three" };

			cb.Expanded += (s, e) => cb.SetSource (list);
			cb.Collapsed += (s, e) => cb.Source = null;

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

			Assert.Equal (1, cb1.Subviews.Count);
			Assert.True (cb1.HideDropdownListOnClick);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (2, Application.Current.Subviews.Count);

			Assert.Equal (1, cb2.Subviews.Count);
			Assert.True (cb2.HideDropdownListOnClick);
			Assert.False (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (2, Application.Current.Subviews.Count);

			Assert.True (cb1.MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal (Application.Current.Subviews [2], Application.MouseGrabView);
			Assert.Equal (1, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (3, Application.Current.Subviews.Count);
			Assert.Equal (new Rect (0, 1, 5, 3), Application.Current.Subviews [2].Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);

			var attributes = new Attribute [] {
				// 0
				cb1.Subviews [0].ColorScheme.Focus,
				// 1
				Application.Current.Subviews [2].ColorScheme.Focus,
				// 2
				new Attribute (Application.Current.Subviews [2].ColorScheme.Focus.Background, Application.Current.Subviews [2].ColorScheme.Focus.Foreground),
				// 3
				Application.Top.ColorScheme.Normal
			};

			TestHelpers.AssertDriverColorsAre (@"
0000003300000000
22222
11111
11111", attributes);

			Assert.True (Application.Current.Subviews [2].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition
			}));
			Assert.Equal (Application.Current.Subviews [2], Application.MouseGrabView);
			Assert.Equal (1, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), Application.Current.Subviews [2].Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);
			TestHelpers.AssertDriverColorsAre (@"
0000003300000000
11111
22222
11111", attributes);

			Assert.True (cb2.MouseEvent (new MouseEvent {
				X = cb2.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal (Application.Current.Subviews [2], Application.MouseGrabView);
			Assert.Equal (1, cb1.Subviews.Count);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (3, Application.Current.Subviews.Count);
			Assert.Equal (1, cb2.Subviews.Count);
			Assert.True (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (new Rect (8, 1, 7, 3), Application.Current.Subviews [2].Frame);

			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
        First   
        Second  
        Third   
", output);

			TestHelpers.AssertDriverColorsAre (@"
0000003300000000
333333332222222
333333331111111
333333331111111", attributes);

			Assert.True (Application.Current.Subviews [2].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition
			}));
			Assert.Equal (Application.Current.Subviews [2], Application.MouseGrabView);
			Assert.Equal (1, cb1.Subviews.Count);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (3, Application.Current.Subviews.Count);
			Assert.Equal (1, cb2.Subviews.Count);
			Assert.True (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (new Rect (8, 1, 7, 3), Application.Current.Subviews [2].Frame);

			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
        First   
        Second  
        Third   
", output);

			TestHelpers.AssertDriverColorsAre (@"
000000330000000
333333331111111
333333332222222
333333331111111", attributes);

			Assert.True (cb1.MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal (Application.Current.Subviews [2], Application.MouseGrabView);
			Assert.Equal (1, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), Application.Current.Subviews [2].Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);

			TestHelpers.AssertDriverColorsAre (@"
0000003300000000
22222
11111
11111", attributes);

			Assert.False (Application.Current.Subviews [2].MouseEvent (new MouseEvent {
				X = 50,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Null (Application.MouseGrabView);
			Assert.Equal (1, cb1.Subviews.Count);
			Assert.False (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (2, Application.Current.Subviews.Count);
			Assert.Equal (1, cb2.Subviews.Count);
			Assert.False (cb2.IsShow);
			Assert.Equal (0, cb2.SelectedItem);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (2, Application.Current.Subviews.Count);

			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
", output);

			TestHelpers.AssertDriverColorsAre (@"
0000003300000000", attributes);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_WantMousePositionReports ()
		{
			var cb = new ComboBox ();
			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.WantMousePositionReports);
			cb.Expand ();
			Assert.True (Application.Current.Subviews [0].WantMousePositionReports);

			cb.HideDropdownListOnClick = false;
			Assert.False (cb.HideDropdownListOnClick);
			Assert.False (cb.WantMousePositionReports);
			Assert.True (Application.Current.Subviews [0].WantMousePositionReports);

			cb.HideDropdownListOnClick = true;
			Assert.True (cb.HideDropdownListOnClick);
			Assert.False (cb.WantMousePositionReports);
			Assert.True (Application.Current.Subviews [0].WantMousePositionReports);
			cb.Collapse ();
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
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.False (cb2.HasFocus);
			Assert.Equal ("First", cb2.Text);
			Assert.Equal (0, cb2.SelectedItem);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 8,
					Y = 0,
					Flags = MouseFlags.Button1Clicked
				});

			Assert.Equal (new Rect (0, 0, 6, 1), cb1.Frame);
			Assert.False (cb1.HasFocus);
			Assert.Equal (new Rect (8, 0, 8, 1), cb2.Frame);
			Assert.True (cb2.HasFocus);
		}

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Hide_DropDown_List_If_Click_Is_Handled_By_Another_MouseGrabView ()
		{
			var win = new Window () { Width = 20, Height = 10 };
			var cb = new ComboBox (new List<string> { "One", "Two", "Three" }) {
				Width = 6,
				Height = 4,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};
			win.Add (cb);
			Application.Top.Add (win);
			Application.Begin (Application.Top);

			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = cb.Frame.Right,
					Y = 1,
					Flags = MouseFlags.Button1Pressed
				});
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = cb.Frame.Right,
					Y = 1,
					Flags = MouseFlags.Button1Clicked
				});
			Assert.True (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│One  ▼            │
│One               │
│Two               │
│Three             │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘", output);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 0,
					Y = 0,
					Flags = MouseFlags.Button1Pressed
				});
			Assert.False (cb.IsShow);
			Assert.Equal (0, cb.SelectedItem);
			Assert.Equal ("One", cb.Text);
			Assert.Equal (win, Application.MouseGrabView);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│One  ▼            │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void Text_Not_Equal_To_Search_Text_Not_Valid_On_Enter_Or_OnLeave ()
		{
			var cb = new ComboBox (new List<string> { "One", "Two", "Three" }) {
				Width = 6,
				Height = 4,
			};
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.Equal ("T", ((TextField)cb.Subviews [0]).Text);
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("", ((TextField)cb.Subviews [0]).Text);
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.Equal ("T", ((TextField)cb.Subviews [0]).Text);
			Assert.True (Application.Top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ("", ((TextField)cb.Subviews [0]).Text);
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.t, new KeyModifiers ())));
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.w, new KeyModifiers ())));
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.o, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.Equal ("Two", ((TextField)cb.Subviews [0]).Text);
		}

		[Fact, AutoInitShutdown]
		public void ComboBox_Alone_In_A_Dialog ()
		{
			var dialog = new Dialog () { Width = 20, Height = 8 };
			var cb = new ComboBox (new List<string> { "One", "Two", "Three" });
			dialog.Add (cb);
			var rs = Application.Begin (dialog);

			Assert.Equal (new Rect (30, 8, 20, 8), dialog.Frame);
			Assert.Equal (new Rect (0, 0, 18, 6), cb.Frame);
			Assert.Equal (new Rect (0, 0, 18, 1), cb.Subviews [0].Frame);
			Assert.Equal (new Rect (0, 1, 18, 5), cb.Subviews [1].Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                              ┌──────────────────┐
                              │                  │
                              │One               │
                              │Two               │
                              │Three             │
                              │                  │
                              │                  │
                              └──────────────────┘", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                              ┌──────────────────┐
                              │One               │
                              │One               │
                              │Two               │
                              │Three             │
                              │                  │
                              │                  │
                              └──────────────────┘", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                              ┌──────────────────┐
                              │Two               │
                              │One               │
                              │Two               │
                              │Three             │
                              │                  │
                              │                  │
                              └──────────────────┘", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                              ┌──────────────────┐
                              │Three             │
                              │One               │
                              │Two               │
                              │Three             │
                              │                  │
                              │                  │
                              └──────────────────┘", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);
			Assert.Equal ("Three", cb.Text);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                              ┌──────────────────┐
                              │Three             │
                              │One               │
                              │Two               │
                              │Three             │
                              │                  │
                              │                  │
                              └──────────────────┘", output);

			cb.Text = "";
			cb.SelectedItem = -1;
			Assert.True (cb.Subviews [0].ProcessKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Application.Top.Redraw (Application.Top.Bounds);
			Assert.Equal ("", cb.Text);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                              ┌──────────────────┐
                              │T                 │
                              │Two               │
                              │Three             │
                              │                  │
                              │                  │
                              │                  │
                              └──────────────────┘", output);

			Assert.True (cb.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.True (cb.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			var attributes = new Attribute [] {
				// 0
				new Attribute(Color.Black, Color.Black),
				// 1
				Application.Top.ColorScheme.Normal,
				// 2
				cb.Subviews [0].ColorScheme.Focus,
				// 3
				new Attribute (cb.Subviews [1].ColorScheme.Focus.Background, cb.Subviews [1].ColorScheme.Focus.Foreground),
			};

			// Highlighted three
			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 33,
					Y = 12,
					Flags = MouseFlags.ReportMousePosition
				});

			Assert.Equal (cb.Subviews [1], Application.MouseGrabView);
			Application.Top.Redraw (Application.Top.Bounds);
			var expectedColors = @"
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000000000000000000000000
000000000000000000000000000000111111111111111111110
000000000000000000000000000000122222222222222222210
000000000000000000000000000000111111111111111111110
000000000000000000000000000000111111111111111111110
000000000000000000000000000000133333333333333333310
000000000000000000000000000000111111111111111111110
000000000000000000000000000000111111111111111111110
000000000000000000000000000000111111111111111111110
000000000000000000000000000000000000000000000000000";
			TestHelpers.AssertDriverColorsAre (expectedColors, attributes);

			// Mouse beyond last item highlighted the last item which is three
			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 33,
					Y = 13,
					Flags = MouseFlags.ReportMousePosition
				});

			Assert.Equal (cb.Subviews [1], Application.MouseGrabView);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverColorsAre (expectedColors, attributes);

			Application.End (rs);
		}

		[Fact]
		public void TextChanged_Event ()
		{
			var cb = new ComboBox ("One");
			var oldText = string.Empty;
			cb.TextChanged += (s, e) => oldText = e.OldValue.ToString ();
			Assert.Equal ("One", cb.Text);
			cb.Text = "Two";
			Assert.Equal ("One", oldText);
		}

		[Fact, AutoInitShutdown]
		public void Text_Setter_Does_Not_Show_DropDownList ()
		{
			var cb = new ComboBox (new List<string> { "One", "Two", "Three" }) {
				Width = 5
			};
			Application.Top.Add (cb);
			Application.Begin (Application.Top);

			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("", cb.Text);

			// found and place the right text
			cb.Text = "two";
			Assert.False (cb.IsShow);
			Assert.Equal (1, cb.SelectedItem);
			Assert.Equal ("Two", cb.Text);

			// not found changes selected item to -1
			cb.Text = "five";
			Assert.False (cb.IsShow);
			Assert.Equal (-1, cb.SelectedItem);
			Assert.Equal ("five", cb.Text);
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
			Assert.Equal (cb1.Subviews [1], Application.MouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), cb1.Subviews [1].Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
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

			TestHelpers.AssertDriverColorsAre (@"
0000004400000000
33333
22222
22222", attributes);

			Assert.True (cb1.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition
			}));
			Assert.Equal (cb1.Subviews [1], Application.MouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), cb1.Subviews [1].Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);
			TestHelpers.AssertDriverColorsAre (@"
0000004400000000
11111
33333
22222", attributes);

			Assert.True (cb2.MouseEvent (new MouseEvent {
				X = cb2.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal (cb2.Subviews [1], Application.MouseGrabView);
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
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
        First   
        Second  
        Third   
", output);

			TestHelpers.AssertDriverColorsAre (@"
0000004400000000
444444443333333
444444442222222
444444442222222", attributes);

			Assert.True (cb2.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition
			}));
			Assert.Equal (cb2.Subviews [1], Application.MouseGrabView);
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
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
        First   
        Second  
        Third   
", output);

			TestHelpers.AssertDriverColorsAre (@"
0000004400000000
444444441111111
444444443333333
444444442222222", attributes);

			Assert.True (cb1.MouseEvent (new MouseEvent {
				X = cb1.Bounds.Right - 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Assert.Equal (cb1.Subviews [1], Application.MouseGrabView);
			Assert.Equal (2, cb1.Subviews.Count);
			Assert.True (cb1.IsShow);
			Assert.Equal (0, cb1.SelectedItem);
			Assert.Equal ("One", cb1.Text);
			Assert.Equal (new Rect (0, 1, 5, 3), cb1.Subviews [1].Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
One             
Two             
Three           
", output);

			TestHelpers.AssertDriverColorsAre (@"
0000004400000000
33333
22222
22222", attributes);

			Assert.False (cb1.Subviews [1].MouseEvent (new MouseEvent {
				X = 50,
				Y = 1,
				Flags = MouseFlags.Button1Clicked
			}));
			Assert.Null (Application.MouseGrabView);
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
			TestHelpers.AssertDriverContentsWithFrameAre (@"
One  ▼  First  ▼
", output);

			TestHelpers.AssertDriverColorsAre (@"
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

		[Fact, AutoInitShutdown]
		public void HideDropdownListOnClick_True_Hide_DropDown_List_If_Click_Is_Handled_By_Another_MouseGrabView ()
		{
			var win = new Window ("Window") { Width = 20, Height = 10 };
			var cb = new ComboBox (new List<string> { "One", "Two", "Three" }) {
				Width = 6,
				Height = 4,
				HideDropdownListOnClick = true,
				SelectedItem = 0
			};
			win.Add (cb);
			Application.Top.Add (win);
			Application.Begin (Application.Top);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = cb.Frame.Right,
					Y = 1,
					Flags = MouseFlags.Button1Pressed
				});
			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = cb.Frame.Right,
					Y = 1,
					Flags = MouseFlags.Button1Clicked
				});

			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌ Window ──────────┐
│One  ▼            │
│One               │
│Two               │
│Three             │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
", output);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 0,
					Y = 1,
					Flags = MouseFlags.Button1Pressed
				});

			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌ Window ──────────┐
│One  ▼            │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
", output);

		}
	}
}