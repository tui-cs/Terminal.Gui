using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Terminal.Gui {

	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// </summary>
	public interface IAutocomplete {

		/// <summary>
		/// The host control that will use autocomplete.
		/// </summary>
		View HostControl { get; set; }

		/// <summary>
		/// Gets or sets where the popup will be displayed.
		/// </summary>
		bool PopupInsideContainer { get; set; }

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		int MaxWidth { get; set; }

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		int MaxHeight { get; set; }

		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		bool Visible { get; set; }

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// based on what the user has typed so far.
		/// </summary>
		ReadOnlyCollection<Suggestion> Suggestions { get; set; }

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		int SelectedIdx { get; set; }

		/// <summary>
		/// The colors to use to render the overlay. Accessing this property before
		/// the Application has been initialized will cause an error
		/// </summary>
		ColorScheme ColorScheme { get; set; }

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		Key SelectionKey { get; set; }

		/// <summary>
		/// The key that the user can press to close the currently popped autocomplete menu
		/// </summary>
		Key CloseKey { get; set; }

		/// <summary>
		/// The key that the user can press to reopen the currently popped autocomplete menu
		/// </summary>
		Key Reopen { get; set; }

		/// <summary>
		/// Renders the autocomplete dialog inside the given <see cref="HostControl"/> at the
		/// given point.
		/// </summary>
		/// <param name="renderAt"></param>
		void RenderOverlay (Point renderAt);

		/// <summary>
		/// Handle key events before <see cref="HostControl"/> e.g. to make key events like
		/// up/down apply to the autocomplete control instead of changing the cursor position in
		/// the underlying text view.
		/// </summary>
		/// <param name="kb">The key event.</param>
		/// <returns><c>true</c>if the key can be handled <c>false</c>otherwise.</returns>
		bool ProcessKey (KeyEvent kb);

		/// <summary>
		/// Handle mouse events before <see cref="HostControl"/> e.g. to make mouse events like
		/// report/click apply to the autocomplete control instead of changing the cursor position in
		/// the underlying text view.
		/// </summary>
		/// <param name="me">The mouse event.</param>
		/// <param name="fromHost">If was called from the popup or from the host.</param>
		/// <returns><c>true</c>if the mouse can be handled <c>false</c>otherwise.</returns>
		bool MouseEvent (MouseEvent me, bool fromHost = false);

		/// <summary>
		/// Clears <see cref="Suggestions"/>
		/// </summary>
		void ClearSuggestions ();

		/// <summary>
		/// Gets or Sets the class responsible for generating <see cref="Suggestions"/>
		/// based on a given <see cref="AutocompleteContext"/> of the <see cref="HostControl"/>.
		/// </summary>
		ISuggestionGenerator SuggestionGenerator { get; set; }

		/// <summary>
		/// Populates <see cref="Suggestions"/> with all <see cref="Suggestion"/> 
		/// proposed by <see cref="SuggestionGenerator"/> at the given <paramref name="context"/>
		/// (cursor position)
		/// </summary>
		void GenerateSuggestions (AutocompleteContext context);
	}
}
