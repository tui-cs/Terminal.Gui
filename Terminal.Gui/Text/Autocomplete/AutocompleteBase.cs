using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// Abstract implementation of <see cref="IAutocomplete"/> allows
	/// for tailoring how autocomplete is rendered/interacted with.
	/// </summary>
	public abstract class AutocompleteBase : IAutocomplete {

		/// <inheritdoc/>
		public abstract View HostControl { get; set; }
		/// <inheritdoc/>
		public bool PopupInsideContainer { get; set; }

		/// <inheritdoc/>
		public ISuggestionGenerator SuggestionGenerator { get; set; } = new SingleWordSuggestionGenerator ();

		/// <inheritdoc/>
		public virtual int MaxWidth { get; set; } = 10;

		/// <inheritdoc/>
		public virtual int MaxHeight { get; set; } = 6;

		/// <inheritdoc/>

		/// <inheritdoc/>
		public virtual bool Visible { get; set; }

		/// <inheritdoc/>
		public virtual ReadOnlyCollection<Suggestion> Suggestions { get; set; } = new ReadOnlyCollection<Suggestion> (new Suggestion [0]);


		/// <inheritdoc/>
		public virtual int SelectedIdx { get; set; }

		/// <inheritdoc/>
		public abstract ColorScheme ColorScheme { get; set; }

		/// <inheritdoc/>
		public virtual Key SelectionKey { get; set; } = Key.Enter;

		/// <inheritdoc/>
		public virtual Key CloseKey { get; set; } = Key.Esc;

		/// <inheritdoc/>
		public virtual Key Reopen { get; set; } = Key.Space | Key.CtrlMask | Key.AltMask;

		/// <inheritdoc/>
		public abstract bool MouseEvent (MouseEvent me, bool fromHost = false);

		/// <inheritdoc/>
		public abstract bool ProcessKey (KeyEvent kb);
		/// <inheritdoc/>
		public abstract void RenderOverlay (Point renderAt);

		/// <inheritdoc/>>
		public virtual void ClearSuggestions ()
		{
			Suggestions = Enumerable.Empty<Suggestion> ().ToList ().AsReadOnly ();
		}

		/// <inheritdoc/>
		public virtual void GenerateSuggestions (AutocompleteContext context)
		{
			Suggestions = SuggestionGenerator.GenerateSuggestions (context).ToList ().AsReadOnly ();

			EnsureSelectedIdxIsValid ();
		}

		/// <summary>
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		public virtual void EnsureSelectedIdxIsValid ()
		{
			SelectedIdx = Math.Max (0, Math.Min (Suggestions.Count - 1, SelectedIdx));
		}
	}
}

