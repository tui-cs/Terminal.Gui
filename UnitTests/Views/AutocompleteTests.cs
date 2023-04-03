using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class AutocompleteTests {
		readonly ITestOutputHelper output;

		public AutocompleteTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Test_GenerateSuggestions_Simple ()
		{
			var ac = new TextViewAutocomplete ();
			((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions = new List<string> {
				"fish",
				"const",
				"Cobble" };

			var tv = new TextView ();
			tv.InsertText ("co");

			ac.HostControl = tv;
			ac.GenerateSuggestions (
				new AutocompleteContext(
				tv.Text.ToRuneList(),2));

			Assert.Equal (2, ac.Suggestions.Count);
			Assert.Equal ("const", ac.Suggestions [0].Title);
			Assert.Equal ("Cobble", ac.Suggestions [1].Title);
		}

		[Fact]
		[AutoInitShutdown]
		public void TestSettingColorSchemeOnAutocomplete ()
		{
			var tv = new TextView ();

			// to begin with we should be using the default menu color scheme
			Assert.Same (Colors.Menu, tv.Autocomplete.ColorScheme);

			// allocate a new custom scheme
			tv.Autocomplete.ColorScheme = new ColorScheme () {
				Normal = Application.Driver.MakeAttribute (Color.Black, Color.Blue),
				Focus = Application.Driver.MakeAttribute (Color.Black, Color.Cyan),
			};

			// should be separate instance
			Assert.NotSame (Colors.Menu, tv.Autocomplete.ColorScheme);

			// with the values we set on it
			Assert.Equal (Color.Black, tv.Autocomplete.ColorScheme.Normal.Foreground);
			Assert.Equal (Color.Blue, tv.Autocomplete.ColorScheme.Normal.Background);

			Assert.Equal (Color.Black, tv.Autocomplete.ColorScheme.Focus.Foreground);
			Assert.Equal (Color.Cyan, tv.Autocomplete.ColorScheme.Focus.Background);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var tv = new TextView () {
				Width = 10,
				Height = 2,
				Text = " Fortunately super feature."
			};
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);

			Assert.Equal (Point.Empty, tv.CursorPosition);
			Assert.NotNull (tv.Autocomplete);
			var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;

			Assert.Empty (g.AllSuggestions);
			g.AllSuggestions = Regex.Matches (tv.Text.ToString (), "\\w+")
				.Select (s => s.Value)
				.Distinct ().ToList ();
			Assert.Equal (3, g.AllSuggestions.Count);
			Assert.Equal ("Fortunately", g.AllSuggestions [0]);
			Assert.Equal ("super", g.AllSuggestions [1]);
			Assert.Equal ("feature", g.AllSuggestions [^1]);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.F, new KeyModifiers ())));
			top.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[^1].Replacement);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[tv.Autocomplete.SelectedIdx].Replacement);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			top.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[^1].Replacement);
			Assert.Equal (1, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[tv.Autocomplete.SelectedIdx].Replacement);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			top.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[^1].Replacement);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[tv.Autocomplete.SelectedIdx].Replacement);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			top.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[^1].Replacement);
			Assert.Equal (1, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[tv.Autocomplete.SelectedIdx].Replacement);
			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			top.Redraw (tv.Bounds);
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[^1].Replacement);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[tv.Autocomplete.SelectedIdx].Replacement);
			Assert.True (tv.Autocomplete.Visible);
			top.Redraw (tv.Bounds);
			Assert.True (tv.ProcessKey (new KeyEvent (tv.Autocomplete.CloseKey, new KeyModifiers ())));
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.Equal (3, g.AllSuggestions.Count);
			Assert.False (tv.Autocomplete.Visible);
			top.Redraw (tv.Bounds);
			Assert.True (tv.ProcessKey (new KeyEvent (tv.Autocomplete.Reopen, new KeyModifiers ())));
			Assert.Equal ($"F Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (1, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal (3, g.AllSuggestions.Count);
			Assert.True (tv.ProcessKey (new KeyEvent (tv.Autocomplete.SelectionKey, new KeyModifiers ())));
			Assert.Equal ($"Fortunately Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);
			Assert.Equal (2, tv.Autocomplete.Suggestions.Count);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[0].Replacement);
			Assert.Equal ("feature", tv.Autocomplete.Suggestions[^1].Replacement);
			Assert.Equal (0, tv.Autocomplete.SelectedIdx);
			Assert.Equal ("Fortunately", tv.Autocomplete.Suggestions[tv.Autocomplete.SelectedIdx].Replacement);
			Assert.True (tv.ProcessKey (new KeyEvent (tv.Autocomplete.CloseKey, new KeyModifiers ())));
			Assert.Equal ($"Fortunately Fortunately super feature.", tv.Text);
			Assert.Equal (new Point (11, 0), tv.CursorPosition);
			Assert.Empty (tv.Autocomplete.Suggestions);
			Assert.Equal (3, g.AllSuggestions.Count);
		}

		[Fact, AutoInitShutdown]
		public void CursorLeft_CursorRight_Mouse_Button_Pressed_Does_Not_Show_Popup ()
		{
			var tv = new TextView () {
				Width = 50,
				Height = 5,
				Text = "This a long line and against TextView."
			};

			var g = (SingleWordSuggestionGenerator)tv.Autocomplete.SuggestionGenerator;
			g.AllSuggestions = Regex.Matches (tv.Text.ToString (), "\\w+")
					.Select (s => s.Value)
					.Distinct ().ToList ();
			var top = Application.Top;
			top.Add (tv);
			Application.Begin (top);


			for (int i = 0; i < 7; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
				Application.Refresh ();
				TestHelpers.AssertDriverContentsWithFrameAre (@"
This a long line and against TextView.", output);
			}

			Assert.True (tv.MouseEvent (new MouseEvent () {
				X = 6,
				Y = 0,
				Flags = MouseFlags.Button1Pressed
			}));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This a long line and against TextView.", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.g, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This ag long line and against TextView.
       against                         ", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This ag long line and against TextView.
      against                          ", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This ag long line and against TextView.
     against                           ", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This ag long line and against TextView.", output);

			for (int i = 0; i < 3; i++) {
				Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
				Application.Refresh ();
				TestHelpers.AssertDriverContentsWithFrameAre (@"
This ag long line and against TextView.", output);
			}

			Assert.True (tv.ProcessKey (new KeyEvent (Key.Backspace, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This a long line and against TextView.", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.n, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This an long line and against TextView.
       and                             ", output);

			Assert.True (tv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
This an long line and against TextView.", output);
		}
	}
}