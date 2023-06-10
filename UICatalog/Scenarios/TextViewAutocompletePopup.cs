using System.Linq;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "TextView Autocomplete Popup", Description: "Shows five TextView Autocomplete Popup effects")]
	[ScenarioCategory ("TextView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Mouse and Keyboard")]
	public class TextViewAutocompletePopup : Scenario {

		TextView textViewTopLeft;
		TextView textViewTopRight;
		TextView textViewBottomLeft;
		TextView textViewBottomRight;
		TextView textViewCentered;
		MenuItem miMultiline;
		MenuItem miWrap;
		StatusItem siMultiline;
		StatusItem siWrap;
		int height = 10;

		public override void Setup ()
		{
			Win.Title = GetName ();
			var width = 20;
			var text = " jamp jemp jimp jomp jump";


			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					miMultiline =  new MenuItem ("_Multiline", "", () => Multiline()){CheckType = MenuItemCheckStyle.Checked},
					miWrap =  new MenuItem ("_Word Wrap", "", () => WordWrap()){CheckType = MenuItemCheckStyle.Checked},
					new MenuItem ("_Quit", "", () => Quit())
				})
			});
			Application.Top.Add (menu);

			textViewTopLeft = new TextView () {
				Width = width,
				Height = height,
				Text = text
			};
			textViewTopLeft.DrawContent += TextViewTopLeft_DrawContent;
			Win.Add (textViewTopLeft);

			textViewTopRight = new TextView () {
				X = Pos.AnchorEnd (width),
				Width = width,
				Height = height,
				Text = text
			};
			textViewTopRight.DrawContent += TextViewTopRight_DrawContent;
			Win.Add (textViewTopRight);

			textViewBottomLeft = new TextView () {
				Y = Pos.AnchorEnd (height),
				Width = width,
				Height = height,
				Text = text
			};
			textViewBottomLeft.DrawContent += TextViewBottomLeft_DrawContent;
			Win.Add (textViewBottomLeft);

			textViewBottomRight = new TextView () {
				X = Pos.AnchorEnd (width),
				Y = Pos.AnchorEnd (height),
				Width = width,
				Height = height,
				Text = text
			};
			textViewBottomRight.DrawContent += TextViewBottomRight_DrawContent;
			Win.Add (textViewBottomRight);

			textViewCentered = new TextView () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = width,
				Height = height,
				Text = text
			};
			textViewCentered.DrawContent += TextViewCentered_DrawContent;
			Win.Add (textViewCentered);

			miMultiline.Checked = textViewTopLeft.Multiline;
			miWrap.Checked = textViewTopLeft.WordWrap;

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
				siMultiline = new StatusItem(Key.Null, "", null),
				siWrap = new StatusItem(Key.Null, "", null)
			});
			Application.Top.Add (statusBar);

			Win.LayoutStarted += Win_LayoutStarted;
		}

		private void Win_LayoutStarted (object sender, LayoutEventArgs obj)
		{
			miMultiline.Checked = textViewTopLeft.Multiline;
			miWrap.Checked = textViewTopLeft.WordWrap;
			SetMultilineStatusText ();
			SetWrapStatusText ();

			if (miMultiline.Checked == true) {
				height = 10;
			} else {
				height = 1;
			}
			textViewBottomLeft.Y = textViewBottomRight.Y = Pos.AnchorEnd (height);
		}

		private void SetMultilineStatusText ()
		{
			siMultiline.Title = $"Multiline: {miMultiline.Checked}";
		}

		private void SetWrapStatusText ()
		{
			siWrap.Title = $"WordWrap: {miWrap.Checked}";
		}

		private void SetAllSuggestions (TextView view)
		{
			((SingleWordSuggestionGenerator)view.Autocomplete.SuggestionGenerator).AllSuggestions = Regex.Matches (view.Text, "\\w+")
				.Select (s => s.Value)
				.Distinct ().ToList ();
		}

		private void TextViewCentered_DrawContent (object sender, DrawEventArgs e)
		{
			SetAllSuggestions (textViewCentered);
		}

		private void TextViewBottomRight_DrawContent (object sender, DrawEventArgs e)
		{
			SetAllSuggestions (textViewBottomRight);
		}

		private void TextViewBottomLeft_DrawContent (object sender, DrawEventArgs e)
		{
			SetAllSuggestions (textViewBottomLeft);
		}

		private void TextViewTopRight_DrawContent (object sender, DrawEventArgs e)
		{
			SetAllSuggestions (textViewTopRight);
		}

		private void TextViewTopLeft_DrawContent (object sender, DrawEventArgs e)
		{
			SetAllSuggestions (textViewTopLeft);
		}

		private void Multiline ()
		{
			miMultiline.Checked = !miMultiline.Checked;
			SetMultilineStatusText ();
			textViewTopLeft.Multiline = (bool)miMultiline.Checked;
			textViewTopRight.Multiline = (bool)miMultiline.Checked;
			textViewBottomLeft.Multiline = (bool)miMultiline.Checked;
			textViewBottomRight.Multiline = (bool)miMultiline.Checked;
			textViewCentered.Multiline = (bool)miMultiline.Checked;
		}

		private void WordWrap ()
		{
			miWrap.Checked = !miWrap.Checked;
			textViewTopLeft.WordWrap = (bool)miWrap.Checked;
			textViewTopRight.WordWrap = (bool)miWrap.Checked;
			textViewBottomLeft.WordWrap = (bool)miWrap.Checked;
			textViewBottomRight.WordWrap = (bool)miWrap.Checked;
			textViewCentered.WordWrap = (bool)miWrap.Checked;
			miWrap.Checked = textViewTopLeft.WordWrap;
			SetWrapStatusText ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
