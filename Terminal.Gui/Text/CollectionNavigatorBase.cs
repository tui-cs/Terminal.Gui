using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Navigates a collection of items using keystrokes. The keystrokes are used to build a search string. 
	/// The <see cref="SearchString"/> is used to find the next item in the collection that matches the search string
	/// when <see cref="GetNextMatchingItem(int, char)"/> is called.
	/// <para>
	/// If the user types keystrokes that can't be found in the collection, 
	/// the search string is cleared and the next item is found that starts with the last keystroke.
	/// </para>
	/// <para>
	/// If the user pauses keystrokes for a short time (see <see cref="TypingDelay"/>), the search string is cleared.
	/// </para>
	/// </summary>
	public abstract class CollectionNavigatorBase {

		DateTime lastKeystroke = DateTime.Now;
		/// <summary>
		/// Gets or sets the number of milliseconds to delay before clearing the search string. The delay is
		/// reset on each call to <see cref="GetNextMatchingItem(int, char)"/>. The default is 500ms.
		/// </summary>
		public int TypingDelay { get; set; } = 500;

		/// <summary>
		/// The compararer function to use when searching the collection.
		/// </summary>
		public StringComparer Comparer { get; set; } = StringComparer.InvariantCultureIgnoreCase;

		/// <summary>
		/// This event is invoked when <see cref="SearchString"/>  changes. Useful for debugging.
		/// </summary>
		public event EventHandler<KeystrokeNavigatorEventArgs> SearchStringChanged;

		private string _searchString = "";
		/// <summary>
		/// Gets the current search string. This includes the set of keystrokes that have been pressed
		/// since the last unsuccessful match or after <see cref="TypingDelay"/>) milliseconds. Useful for debugging.
		/// </summary>
		public string SearchString {
			get => _searchString;
			private set {
				_searchString = value;
				OnSearchStringChanged (new KeystrokeNavigatorEventArgs (value));
			}
		}

		/// <summary>
		/// Invoked when the <see cref="SearchString"/> changes. Useful for debugging. Invokes the <see cref="SearchStringChanged"/> event.
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnSearchStringChanged (KeystrokeNavigatorEventArgs e)
		{
			SearchStringChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Gets the index of the next item in the collection that matches the current <see cref="SearchString"/> plus the provided character (typically
		/// from a key press).
		/// </summary>
		/// <param name="currentIndex">The index in the collection to start the search from.</param>
		/// <param name="keyStruck">The character of the key the user pressed.</param>
		/// <returns>The index of the item that matches what the user has typed. 
		/// Returns <see langword="-1"/> if no item in the collection matched.</returns>
		public int GetNextMatchingItem (int currentIndex, char keyStruck)
		{
			if (!char.IsControl (keyStruck)) {

				// maybe user pressed 'd' and now presses 'd' again.
				// a candidate search is things that begin with "dd"
				// but if we find none then we must fallback on cycling
				// d instead and discard the candidate state
				string candidateState = "";

				// is it a second or third (etc) keystroke within a short time
				if (SearchString.Length > 0 && DateTime.Now - lastKeystroke < TimeSpan.FromMilliseconds (TypingDelay)) {
					// "dd" is a candidate
					candidateState = SearchString + keyStruck;
				} else {
					// its a fresh keystroke after some time
					// or its first ever key press
					SearchString = new string (keyStruck, 1);
				}

				var idxCandidate = GetNextMatchingItem (currentIndex, candidateState,
					// prefer not to move if there are multiple characters e.g. "ca" + 'r' should stay on "car" and not jump to "cart"
					candidateState.Length > 1);

				if (idxCandidate != -1) {
					// found "dd" so candidate searchstring is accepted
					lastKeystroke = DateTime.Now;
					SearchString = candidateState;
					return idxCandidate;
				}

				//// nothing matches "dd" so discard it as a candidate
				//// and just cycle "d" instead
				lastKeystroke = DateTime.Now;
				idxCandidate = GetNextMatchingItem (currentIndex, candidateState);

				// if a match wasn't found, the user typed a 'wrong' key in their search ("can" + 'z'
				// instead of "can" + 'd').
				if (SearchString.Length > 1 && idxCandidate == -1) {
					// ignore it since we're still within the typing delay
					// don't add it to SearchString either
					return currentIndex;
				}

				// if no changes to current state manifested
				if (idxCandidate == currentIndex || idxCandidate == -1) {
					// clear history and treat as a fresh letter
					ClearSearchString ();

					// match on the fresh letter alone
					SearchString = new string (keyStruck, 1);
					idxCandidate = GetNextMatchingItem (currentIndex, SearchString);
					return idxCandidate == -1 ? currentIndex : idxCandidate;
				}

				// Found another "d" or just leave index as it was
				return idxCandidate;

			} else {
				// clear state because keypress was a control char
				ClearSearchString ();

				// control char indicates no selection
				return -1;
			}
		}

		/// <summary>
		/// Gets the index of the next item in the collection that matches <paramref name="search"/>. 
		/// </summary>
		/// <param name="currentIndex">The index in the collection to start the search from.</param>
		/// <param name="search">The search string to use.</param>
		/// <param name="minimizeMovement">Set to <see langword="true"/> to stop the search on the first match
		/// if there are multiple matches for <paramref name="search"/>.
		/// e.g. "ca" + 'r' should stay on "car" and not jump to "cart". If <see langword="false"/> (the default), 
		/// the next matching item will be returned, even if it is above in the collection.
		/// </param>
		/// <returns>The index of the next matching item or <see langword="-1"/> if no match was found.</returns>
		internal int GetNextMatchingItem (int currentIndex, string search, bool minimizeMovement = false)
		{
			if (string.IsNullOrEmpty (search)) {
				return -1;
			}

			var collectionLength = GetCollectionLength ();

			if (currentIndex != -1 && currentIndex < collectionLength && IsMatch (search, ElementAt (currentIndex))) {
				// we are already at a match
				if (minimizeMovement) {
					// if we would rather not jump around (e.g. user is typing lots of text to get this match)
					return currentIndex;
				}

				for (int i = 1; i < collectionLength; i++) {
					//circular
					var idxCandidate = (i + currentIndex) % collectionLength;
					if (IsMatch (search, ElementAt (idxCandidate))) {
						return idxCandidate;
					}
				}

				// nothing else starts with the search term
				return currentIndex;
			} else {
				// search terms no longer match the current selection or there is none
				for (int i = 0; i < collectionLength; i++) {
					if (IsMatch (search, ElementAt (i))) {
						return i;
					}
				}

				// Nothing matches
				return -1;
			}
		}

		/// <summary>
		/// Return the number of elements in the collection
		/// </summary>
		protected abstract int GetCollectionLength ();

		private bool IsMatch (string search, object value)
		{
			return value?.ToString ().StartsWith (search, StringComparison.InvariantCultureIgnoreCase) ?? false;
		}

		/// <summary>
		/// Returns the collection being navigated element at <paramref name="idx"/>.
		/// </summary>
		/// <returns></returns>
		protected abstract object ElementAt (int idx);

		private void ClearSearchString ()
		{
			SearchString = "";
			lastKeystroke = DateTime.Now;
		}

		/// <summary>
		/// Returns true if <paramref name="kb"/> is a searchable key
		/// (e.g. letters, numbers, etc) that are valid to pass to this
		/// class for search filtering.
		/// </summary>
		/// <param name="kb"></param>
		/// <returns></returns>
		public static bool IsCompatibleKey (KeyEvent kb)
		{
			return !kb.IsAlt && !kb.IsCtrl;
		}
	}
}
