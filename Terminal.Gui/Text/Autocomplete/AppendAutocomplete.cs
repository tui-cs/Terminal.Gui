using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Terminal.Gui {

	/// <summary>
	/// Autocomplete for a <see cref="TextField"/> which shows suggestions within the box.
	/// Displayed suggestions can be completed using the tab key.
	/// </summary>
	public class AppendAutocomplete : AutocompleteBase {

		private TextField textField;

		/// <inheritdoc/>
		public override View HostControl { get => textField; set => textField = (TextField)value; }

		/// <summary>
		/// The color used for rendering the appended text. Note that only
		/// <see cref="ColorScheme.Normal"/> is used and then only <see cref="Attribute.Foreground"/>
		/// (Background comes from <see cref="HostControl"/>).
		/// </summary>
		public override ColorScheme ColorScheme { get; set; }

		/// <summary>
		///	Creates a new instance of the <see cref="AppendAutocomplete"/> class.
		/// </summary>
		public AppendAutocomplete (TextField textField)
		{
			this.textField = textField;
			SelectionKey = Key.Tab;

			ColorScheme = new ColorScheme {
				Normal = new Attribute (Color.DarkGray, 0),
				Focus = new Attribute (Color.DarkGray, 0),
				HotNormal = new Attribute (Color.DarkGray, 0),
				HotFocus = new Attribute (Color.DarkGray, 0),
				Disabled = new Attribute (Color.DarkGray, 0),
			};
		}

		/// <inheritdoc/>
		public override void ClearSuggestions ()
		{
			base.ClearSuggestions ();
			textField.SetNeedsDisplay ();
		}

		/// <inheritdoc/>
		public override bool MouseEvent (MouseEvent me, bool fromHost = false)
		{
			return false;
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			var key = kb.Key;
			if (key == SelectionKey) {
				return this.AcceptSelectionIfAny ();
			} else
			if (key == Key.CursorUp) {
				return this.CycleSuggestion (1);
			} else
			if (key == Key.CursorDown) {
				return this.CycleSuggestion (-1);
			} else if (key == CloseKey && Suggestions.Any ()) {
				ClearSuggestions ();
				_suspendSuggestions = true;
				return true;
			}

			if (char.IsLetterOrDigit ((char)kb.KeyValue)) {
				_suspendSuggestions = false;
			}

			return false;
		}
		bool _suspendSuggestions = false;

		/// <inheritdoc/>
		public override void GenerateSuggestions (AutocompleteContext context)
		{
			if (_suspendSuggestions) {
				return;
			}
			base.GenerateSuggestions (context);
		}

		/// <summary>
		/// Renders the current suggestion into the <see cref="TextField"/>
		/// </summary>
		public override void RenderOverlay (Point renderAt)
		{
			if (!this.MakingSuggestion ()) {
				return;
			}

			// draw it like its selected even though its not
			Application.Driver.SetAttribute (new Attribute (ColorScheme.Normal.Foreground, textField.ColorScheme.Focus.Background));
			textField.Move (textField.Text.Length, 0);

			var suggestion = this.Suggestions.ElementAt (this.SelectedIdx);
			var fragment = suggestion.Replacement.Substring (suggestion.Remove);

			int spaceAvailable = textField.Bounds.Width - textField.Text.GetColumns ();
			int spaceRequired = fragment.EnumerateRunes ().Sum (c => c.GetColumns ());

			if (spaceAvailable < spaceRequired) {
				fragment = new string (
					fragment.TakeWhile (c => (spaceAvailable -= ((Rune)c).GetColumns ()) >= 0)
					.ToArray ()
				);
			}

			Application.Driver.AddStr (fragment);
		}

		/// <summary>
		/// Accepts the current autocomplete suggestion displaying in the text box.
		/// Returns true if a valid suggestion was being rendered and acceptable or
		/// false if no suggestion was showing.
		/// </summary>
		/// <returns></returns>
		internal bool AcceptSelectionIfAny ()
		{
			if (this.MakingSuggestion ()) {

				var insert = this.Suggestions.ElementAt (this.SelectedIdx);
				var newText = textField.Text;
				newText = newText.Substring (0, newText.Length - insert.Remove);
				newText += insert.Replacement;
				textField.Text = newText;

				this.textField.MoveEnd ();

				this.ClearSuggestions ();
				return true;
			}

			return false;
		}

		internal void SetTextTo (FileSystemInfo fileSystemInfo)
		{
			var newText = fileSystemInfo.FullName;
			if (fileSystemInfo is DirectoryInfo) {
				newText += System.IO.Path.DirectorySeparatorChar;
			}
			textField.Text = newText;
			textField.MoveEnd ();
		}

		/// <summary>
		/// Returns true if there is a suggestion that can be made and the control
		/// is in a state where user would expect to see auto-complete (i.e. focused and
		/// cursor in right place).
		/// </summary>
		/// <returns></returns>
		private bool MakingSuggestion ()
		{
			return Suggestions.Any () && this.SelectedIdx != -1 && textField.HasFocus && textField.CursorIsAtEnd ();
		}

		private bool CycleSuggestion (int direction)
		{
			if (this.Suggestions.Count <= 1) {
				return false;
			}

			this.SelectedIdx = (this.SelectedIdx + direction) % this.Suggestions.Count;

			if (this.SelectedIdx < 0) {
				this.SelectedIdx = this.Suggestions.Count () - 1;
			}
			textField.SetNeedsDisplay ();
			return true;
		}
	}
}
