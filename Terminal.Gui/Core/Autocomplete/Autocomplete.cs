using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Rune = System.Rune;

namespace Terminal.Gui {

	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// </summary>
	public abstract class Autocomplete : IAutocomplete {

		private class Popup : View {
			Autocomplete autocomplete;

			public Popup (Autocomplete autocomplete)
			{
				this.autocomplete = autocomplete;
				CanFocus = true;
				WantMousePositionReports = true;
			}

			public override Rect Frame {
				get => base.Frame;
				set {
					base.Frame = value;
					X = value.X;
					Y = value.Y;
					Width = value.Width;
					Height = value.Height;
				}
			}

			public override void Redraw (Rect bounds)
			{
				if (autocomplete.LastPopupPos == null) {
					return;
				}

				autocomplete.RenderOverlay ((Point)autocomplete.LastPopupPos);
			}

			public override bool MouseEvent (MouseEvent mouseEvent)
			{
				return autocomplete.MouseEvent (mouseEvent);
			}
		}

		private View top, popup;
		private bool closed;
		int toRenderLength;

		private Point? LastPopupPos { get; set; }

		private ColorScheme colorScheme;
		private View hostControl;

		/// <summary>
		/// The host control to handle.
		/// </summary>
		public virtual View HostControl {
			get => hostControl;
			set {
				hostControl = value;
				top = hostControl.SuperView;
				if (top != null) {
					top.DrawContent += Top_DrawContent;
					top.DrawContentComplete += Top_DrawContentComplete;
					top.Removed += Top_Removed;
				}
			}
		}

		private void Top_Removed (object sender, SuperViewChangedEventArgs e)
		{
			Visible = false;
			ManipulatePopup ();
		}

		private void Top_DrawContentComplete (object sender, DrawEventArgs e)
		{
			ManipulatePopup ();
		}

		private void Top_DrawContent (object sender, DrawEventArgs e)
		{
			if (!closed) {
				ReopenSuggestions ();
			}
			ManipulatePopup ();
			if (Visible) {
				top.BringSubviewToFront (popup);
			}
		}

		private void ManipulatePopup ()
		{
			if (Visible && popup == null) {
				popup = new Popup (this) {
					Frame = Rect.Empty
				};
				top?.Add (popup);
			}

			if (!Visible && popup != null) {
				top?.Remove (popup);
				popup.Dispose ();
				popup = null;
			}
		}

		/// <summary>
		/// Gets or sets If the popup is displayed inside or outside the host limits.
		/// </summary>
		public bool PopupInsideContainer { get; set; } = true;

		/// <summary>
		/// The maximum width of the autocomplete dropdown
		/// </summary>
		public virtual int MaxWidth { get; set; } = 10;

		/// <summary>
		/// The maximum number of visible rows in the autocomplete dropdown to render
		/// </summary>
		public virtual int MaxHeight { get; set; } = 6;

		/// <summary>
		/// True if the autocomplete should be considered open and visible
		/// </summary>
		public virtual bool Visible { get; set; }

		/// <summary>
		/// The strings that form the current list of suggestions to render
		/// based on what the user has typed so far.
		/// </summary>
		public virtual ReadOnlyCollection<string> Suggestions { get; set; } = new ReadOnlyCollection<string> (new string [0]);

		/// <summary>
		/// The full set of all strings that can be suggested.
		/// </summary>
		/// <returns></returns>
		public virtual List<string> AllSuggestions { get; set; } = new List<string> ();

		/// <summary>
		/// The currently selected index into <see cref="Suggestions"/> that the user has highlighted
		/// </summary>
		public virtual int SelectedIdx { get; set; }

		/// <summary>
		/// When more suggestions are available than can be rendered the user
		/// can scroll down the dropdown list.  This indicates how far down they
		/// have gone
		/// </summary>
		public virtual int ScrollOffset { get; set; }

		/// <summary>
		/// The colors to use to render the overlay.  Accessing this property before
		/// the Application has been initialized will cause an error
		/// </summary>
		public virtual ColorScheme ColorScheme {
			get {
				if (colorScheme == null) {
					colorScheme = Colors.Menu;
				}
				return colorScheme;
			}
			set {
				colorScheme = value;
			}
		}

		/// <summary>
		/// The key that the user must press to accept the currently selected autocomplete suggestion
		/// </summary>
		public virtual Key SelectionKey { get; set; } = Key.Enter;

		/// <summary>
		/// The key that the user can press to close the currently popped autocomplete menu
		/// </summary>
		public virtual Key CloseKey { get; set; } = Key.Esc;

		/// <summary>
		/// The key that the user can press to reopen the currently popped autocomplete menu
		/// </summary>
		public virtual Key Reopen { get; set; } = Key.Space | Key.CtrlMask | Key.AltMask;

		/// <summary>
		/// Renders the autocomplete dialog inside or outside the given <see cref="HostControl"/> at the
		/// given point.
		/// </summary>
		/// <param name="renderAt"></param>
		public virtual void RenderOverlay (Point renderAt)
		{
			if (!Visible || HostControl?.HasFocus == false || Suggestions.Count == 0) {
				LastPopupPos = null;
				Visible = false;
				return;
			}

			LastPopupPos = renderAt;

			int height, width;

			if (PopupInsideContainer) {
				// don't overspill vertically
				height = Math.Min (HostControl.Bounds.Height - renderAt.Y, MaxHeight);
				// There is no space below, lets see if can popup on top
				if (height < Suggestions.Count && HostControl.Bounds.Height - renderAt.Y >= height) {
					// Verifies that the upper limit available is greater than the lower limit
					if (renderAt.Y > HostControl.Bounds.Height - renderAt.Y) {
						renderAt.Y = Math.Max (renderAt.Y - Math.Min (Suggestions.Count + 1, MaxHeight + 1), 0);
						height = Math.Min (Math.Min (Suggestions.Count, MaxHeight), LastPopupPos.Value.Y - 1);
					}
				}
			} else {
				// don't overspill vertically
				height = Math.Min (Math.Min (top.Bounds.Height - HostControl.Frame.Bottom, MaxHeight), Suggestions.Count);
				// There is no space below, lets see if can popup on top
				if (height < Suggestions.Count && HostControl.Frame.Y - top.Frame.Y >= height) {
					// Verifies that the upper limit available is greater than the lower limit
					if (HostControl.Frame.Y > top.Bounds.Height - HostControl.Frame.Y) {
						renderAt.Y = Math.Max (HostControl.Frame.Y - Math.Min (Suggestions.Count, MaxHeight), 0);
						height = Math.Min (Math.Min (Suggestions.Count, MaxHeight), HostControl.Frame.Y);
					}
				} else {
					renderAt.Y = HostControl.Frame.Bottom;
				}
			}

			if (ScrollOffset > Suggestions.Count - height) {
				ScrollOffset = 0;
			}
			var toRender = Suggestions.Skip (ScrollOffset).Take (height).ToArray ();
			toRenderLength = toRender.Length;

			if (toRender.Length == 0) {
				return;
			}

			width = Math.Min (MaxWidth, toRender.Max (s => s.Length));

			if (PopupInsideContainer) {
				// don't overspill horizontally, let's see if can be displayed on the left
				if (width > HostControl.Bounds.Width - renderAt.X) {
					// Verifies that the left limit available is greater than the right limit
					if (renderAt.X > HostControl.Bounds.Width - renderAt.X) {
						renderAt.X -= Math.Min (width, LastPopupPos.Value.X);
						width = Math.Min (width, LastPopupPos.Value.X);
					} else {
						width = Math.Min (width, HostControl.Bounds.Width - renderAt.X);
					}
				}
			} else {
				// don't overspill horizontally, let's see if can be displayed on the left
				if (width > top.Bounds.Width - (renderAt.X + HostControl.Frame.X)) {
					// Verifies that the left limit available is greater than the right limit
					if (renderAt.X + HostControl.Frame.X > top.Bounds.Width - (renderAt.X + HostControl.Frame.X)) {
						renderAt.X -= Math.Min (width, LastPopupPos.Value.X);
						width = Math.Min (width, LastPopupPos.Value.X);
					} else {
						width = Math.Min (width, top.Bounds.Width - renderAt.X);
					}
				}
			}

			if (PopupInsideContainer) {
				popup.Frame = new Rect (
					new Point (HostControl.Frame.X + renderAt.X, HostControl.Frame.Y + renderAt.Y),
					new Size (width, height));
			} else {
				popup.Frame = new Rect (
					new Point (HostControl.Frame.X + renderAt.X, renderAt.Y),
					new Size (width, height));
			}

			popup.Move (0, 0);

			for (int i = 0; i < toRender.Length; i++) {

				if (i == SelectedIdx - ScrollOffset) {
					Application.Driver.SetAttribute (ColorScheme.Focus);
				} else {
					Application.Driver.SetAttribute (ColorScheme.Normal);
				}

				popup.Move (0, i);

				var text = TextFormatter.ClipOrPad (toRender [i], width);

				Application.Driver.AddStr (text);
			}
		}

		/// <summary>
		/// Updates <see cref="SelectedIdx"/> to be a valid index within <see cref="Suggestions"/>
		/// </summary>
		public virtual void EnsureSelectedIdxIsValid ()
		{
			SelectedIdx = Math.Max (0, Math.Min (Suggestions.Count - 1, SelectedIdx));

			// if user moved selection up off top of current scroll window
			if (SelectedIdx < ScrollOffset) {
				ScrollOffset = SelectedIdx;
			}

			// if user moved selection down past bottom of current scroll window
			while (toRenderLength > 0 && SelectedIdx >= ScrollOffset + toRenderLength) {
				ScrollOffset++;
			}
		}

		/// <summary>
		/// Handle key events before <see cref="HostControl"/> e.g. to make key events like
		/// up/down apply to the autocomplete control instead of changing the cursor position in
		/// the underlying text view.
		/// </summary>
		/// <param name="kb">The key event.</param>
		/// <returns><c>true</c>if the key can be handled <c>false</c>otherwise.</returns>
		public virtual bool ProcessKey (KeyEvent kb)
		{
			if (IsWordChar ((char)kb.Key)) {
				Visible = true;
				ManipulatePopup ();
				closed = false;
				return false;
			}

			if (kb.Key == Reopen) {
				return ReopenSuggestions ();
			}

			if (closed || Suggestions.Count == 0) {
				Visible = false;
				if (!closed) {
					Close ();
				}
				return false;
			}

			if (kb.Key == Key.CursorDown) {
				MoveDown ();
				return true;
			}

			if (kb.Key == Key.CursorUp) {
				MoveUp ();
				return true;
			}

			if (kb.Key == Key.CursorLeft || kb.Key == Key.CursorRight) {
				GenerateSuggestions (kb.Key == Key.CursorLeft ? -1 : 1);
				if (Suggestions.Count == 0) {
					Visible = false;
					if (!closed) {
						Close ();
					}
				}
				return false;
			}

			if (kb.Key == SelectionKey) {
				return Select ();
			}

			if (kb.Key == CloseKey) {
				Close ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Handle mouse events before <see cref="HostControl"/> e.g. to make mouse events like
		/// report/click apply to the autocomplete control instead of changing the cursor position in
		/// the underlying text view.
		/// </summary>
		/// <param name="me">The mouse event.</param>
		/// <param name="fromHost">If was called from the popup or from the host.</param>
		/// <returns><c>true</c>if the mouse can be handled <c>false</c>otherwise.</returns>
		public virtual bool MouseEvent (MouseEvent me, bool fromHost = false)
		{
			if (fromHost) {
				if (!Visible) {
					return false;
				}
				GenerateSuggestions ();
				if (Visible && Suggestions.Count == 0) {
					Visible = false;
					HostControl?.SetNeedsDisplay ();
					return true;
				} else if (!Visible && Suggestions.Count > 0) {
					Visible = true;
					HostControl?.SetNeedsDisplay ();
					Application.UngrabMouse ();
					return false;
				} else {
					// not in the popup
					if (Visible && HostControl != null) {
						Visible = false;
						closed = false;
					}
					HostControl?.SetNeedsDisplay ();
				}
				return false;
			}

			if (popup == null || Suggestions.Count == 0) {
				ManipulatePopup ();
				return false;
			}

			if (me.Flags == MouseFlags.ReportMousePosition) {
				RenderSelectedIdxByMouse (me);
				return true;
			}

			if (me.Flags == MouseFlags.Button1Clicked) {
				SelectedIdx = me.Y - ScrollOffset;
				return Select ();
			}

			if (me.Flags == MouseFlags.WheeledDown) {
				MoveDown ();
				return true;
			}

			if (me.Flags == MouseFlags.WheeledUp) {
				MoveUp ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Render the current selection in the Autocomplete context menu by the mouse reporting.
		/// </summary>
		/// <param name="me"></param>
		protected void RenderSelectedIdxByMouse (MouseEvent me)
		{
			if (SelectedIdx != me.Y - ScrollOffset) {
				SelectedIdx = me.Y - ScrollOffset;
				if (LastPopupPos != null) {
					RenderOverlay ((Point)LastPopupPos);
				}
			}
		}

		/// <summary>
		/// Clears <see cref="Suggestions"/>
		/// </summary>
		public virtual void ClearSuggestions ()
		{
			Suggestions = Enumerable.Empty<string> ().ToList ().AsReadOnly ();
		}


		/// <summary>
		/// Populates <see cref="Suggestions"/> with all strings in <see cref="AllSuggestions"/> that
		/// match with the current cursor position/text in the <see cref="HostControl"/>
		/// </summary>
		/// <param name="columnOffset">The column offset.</param>
		public virtual void GenerateSuggestions (int columnOffset = 0)
		{
			// if there is nothing to pick from
			if (AllSuggestions.Count == 0) {
				ClearSuggestions ();
				return;
			}

			var currentWord = GetCurrentWord (columnOffset);

			if (string.IsNullOrWhiteSpace (currentWord)) {
				ClearSuggestions ();
			} else {
				Suggestions = AllSuggestions.Where (o =>
				o.StartsWith (currentWord, StringComparison.CurrentCultureIgnoreCase) &&
				!o.Equals (currentWord, StringComparison.CurrentCultureIgnoreCase)
				).ToList ().AsReadOnly ();

				EnsureSelectedIdxIsValid ();
			}
		}


		/// <summary>
		/// Return true if the given symbol should be considered part of a word
		/// and can be contained in matches.  Base behavior is to use <see cref="char.IsLetterOrDigit(char)"/>
		/// </summary>
		/// <param name="rune"></param>
		/// <returns></returns>
		public virtual bool IsWordChar (Rune rune)
		{
			return Char.IsLetterOrDigit ((char)rune);
		}

		/// <summary>
		/// Completes the autocomplete selection process.  Called when user hits the <see cref="SelectionKey"/>.
		/// </summary>
		/// <returns></returns>
		protected bool Select ()
		{
			if (SelectedIdx >= 0 && SelectedIdx < Suggestions.Count) {
				var accepted = Suggestions [SelectedIdx];

				return InsertSelection (accepted);

			}

			return false;
		}

		/// <summary>
		/// Called when the user confirms a selection at the current cursor location in
		/// the <see cref="HostControl"/>.  The <paramref name="accepted"/> string
		/// is the full autocomplete word to be inserted.  Typically a host will have to
		/// remove some characters such that the <paramref name="accepted"/> string 
		/// completes the word instead of simply being appended.
		/// </summary>
		/// <param name="accepted"></param>
		/// <returns>True if the insertion was possible otherwise false</returns>
		protected virtual bool InsertSelection (string accepted)
		{
			var typedSoFar = GetCurrentWord () ?? "";

			if (typedSoFar.Length < accepted.Length) {

				// delete the text
				for (int i = 0; i < typedSoFar.Length; i++) {
					DeleteTextBackwards ();
				}

				InsertText (accepted);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the currently selected word from the <see cref="HostControl"/>.
		/// <para>
		/// When overriding this method views can make use of <see cref="IdxToWord(List{Rune}, int, int)"/>
		/// </para>
		/// </summary>
		/// <param name="columnOffset">The column offset.</param>
		/// <returns></returns>
		protected abstract string GetCurrentWord (int columnOffset = 0);

		/// <summary>
		/// <para>
		/// Given a <paramref name="line"/> of characters, returns the word which ends at <paramref name="idx"/> 
		/// or null.  Also returns null if the <paramref name="idx"/> is positioned in the middle of a word.
		/// </para>
		/// 
		/// <para>
		/// Use this method to determine whether autocomplete should be shown when the cursor is at
		/// a given point in a line and to get the word from which suggestions should be generated.
		/// Use the <paramref name="columnOffset"/> to indicate if search the word at left (negative),
		/// at right (positive) or at the current column (zero) which is the default.
		/// </para>
		/// </summary>
		/// <param name="line"></param>
		/// <param name="idx"></param>
		/// <param name="columnOffset"></param>
		/// <returns></returns>
		protected virtual string IdxToWord (List<Rune> line, int idx, int columnOffset = 0)
		{
			StringBuilder sb = new StringBuilder ();
			var endIdx = idx;

			// get the ending word index
			while (endIdx < line.Count) {
				if (IsWordChar (line [endIdx])) {
					endIdx++;
				} else {
					break;
				}
			}

			// It isn't a word char then there is no way to autocomplete that word
			if (endIdx == idx && columnOffset != 0) {
				return null;
			}

			// we are at the end of a word.  Work out what has been typed so far
			while (endIdx-- > 0) {
				if (IsWordChar (line [endIdx])) {
					sb.Insert (0, (char)line [endIdx]);
				} else {
					break;
				}
			}
			return sb.ToString ();
		}

		/// <summary>
		/// Deletes the text backwards before insert the selected text in the <see cref="HostControl"/>.
		/// </summary>
		protected abstract void DeleteTextBackwards ();

		/// <summary>
		/// Inser the selected text in the <see cref="HostControl"/>.
		/// </summary>
		/// <param name="accepted"></param>
		protected abstract void InsertText (string accepted);

		/// <summary>
		/// Closes the Autocomplete context menu if it is showing and <see cref="ClearSuggestions"/>
		/// </summary>
		protected void Close ()
		{
			ClearSuggestions ();
			Visible = false;
			closed = true;
			HostControl?.SetNeedsDisplay ();
			ManipulatePopup ();
		}

		/// <summary>
		/// Moves the selection in the Autocomplete context menu up one
		/// </summary>
		protected void MoveUp ()
		{
			SelectedIdx--;
			if (SelectedIdx < 0) {
				SelectedIdx = Suggestions.Count - 1;
			}
			EnsureSelectedIdxIsValid ();
			HostControl?.SetNeedsDisplay ();
		}

		/// <summary>
		/// Moves the selection in the Autocomplete context menu down one
		/// </summary>
		protected void MoveDown ()
		{
			SelectedIdx++;
			if (SelectedIdx > Suggestions.Count - 1) {
				SelectedIdx = 0;
			}
			EnsureSelectedIdxIsValid ();
			HostControl?.SetNeedsDisplay ();
		}

		/// <summary>
		/// Reopen the popup after it has been closed.
		/// </summary>
		/// <returns></returns>
		protected bool ReopenSuggestions ()
		{
			GenerateSuggestions ();
			if (Suggestions.Count > 0) {
				Visible = true;
				closed = false;
				HostControl?.SetNeedsDisplay ();
				return true;
			}
			return false;
		}
	}
}
