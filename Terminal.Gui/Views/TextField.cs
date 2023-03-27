//
// TextField.cs: single-line text editor with Emacs keybindings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using NStack;
using Terminal.Gui.Resources;
using Rune = System.Rune;

namespace Terminal.Gui {
	/// <summary>
	///   Single-line text entry <see cref="View"/>
	/// </summary>
	/// <remarks>
	///   The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.
	/// </remarks>
	public class TextField : View {
		List<Rune> text;
		int first, point;
		int selectedStart = -1; // -1 represents there is no text selection.
		ustring selectedText;
		HistoryText historyText = new HistoryText ();
		CultureInfo currentCulture;

		/// <summary>
		/// Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so new input should be appended at the cursor position, rather than clearing the entry
		/// </summary>
		public bool Used { get; set; }

		/// <summary>
		/// If set to true its not allow any changes in the text.
		/// </summary>
		public bool ReadOnly { get; set; } = false;

		/// <summary>
		/// Changing event, raised before the <see cref="Text"/> changes and can be canceled or changing the new text.
		/// </summary>
		public event EventHandler<TextChangingEventArgs> TextChanging;

		/// <summary>
		///   Changed event, raised when the text has changed.
		/// </summary>
		/// <remarks>
		///   This event is raised when the <see cref="Text"/> changes. 
		/// </remarks>
		/// <remarks>
		///   The passed <see cref="EventArgs"/> is a <see cref="NStack.ustring"/> containing the old value. 
		/// </remarks>
		public event EventHandler<TextChangedEventArgs> TextChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (string text) : this (ustring.Make (text)) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextField () : this (string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (ustring text) : base (text)
		{
			Initialize (text, text.RuneCount + 1);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="w">The width.</param>
		/// <param name="text">Initial text contents.</param>
		public TextField (int x, int y, int w, ustring text) : base (new Rect (x, y, w, 1))
		{
			Initialize (text, w);
		}

		void Initialize (ustring text, int w)
		{
			Height = 1;

			if (text == null)
				text = "";

			this.text = TextModel.ToRunes (text.Split ("\n") [0]);
			point = text.RuneCount;
			first = point > w + 1 ? point - w + 1 : 0;
			CanFocus = true;
			Used = true;
			WantMousePositionReports = true;
			savedCursorVisibility = desiredCursorVisibility;

			historyText.ChangeText += HistoryText_ChangeText;

			Initialized += TextField_Initialized;

			// Things this view knows how to do
			AddCommand (Command.DeleteCharRight, () => { DeleteCharRight (); return true; });
			AddCommand (Command.DeleteCharLeft, () => { DeleteCharLeft (); return true; });
			AddCommand (Command.LeftHomeExtend, () => { MoveHomeExtend (); return true; });
			AddCommand (Command.RightEndExtend, () => { MoveEndExtend (); return true; });
			AddCommand (Command.LeftHome, () => { MoveHome (); return true; });
			AddCommand (Command.LeftExtend, () => { MoveLeftExtend (); return true; });
			AddCommand (Command.RightExtend, () => { MoveRightExtend (); return true; });
			AddCommand (Command.WordLeftExtend, () => { MoveWordLeftExtend (); return true; });
			AddCommand (Command.WordRightExtend, () => { MoveWordRightExtend (); return true; });
			AddCommand (Command.Left, () => { MoveLeft (); return true; });
			AddCommand (Command.RightEnd, () => { MoveEnd (); return true; });
			AddCommand (Command.Right, () => { MoveRight (); return true; });
			AddCommand (Command.CutToEndLine, () => { KillToEnd (); return true; });
			AddCommand (Command.CutToStartLine, () => { KillToStart (); return true; });
			AddCommand (Command.Undo, () => { UndoChanges (); return true; });
			AddCommand (Command.Redo, () => { RedoChanges (); return true; });
			AddCommand (Command.WordLeft, () => { MoveWordLeft (); return true; });
			AddCommand (Command.WordRight, () => { MoveWordRight (); return true; });
			AddCommand (Command.KillWordForwards, () => { KillWordForwards (); return true; });
			AddCommand (Command.KillWordBackwards, () => { KillWordBackwards (); return true; });
			AddCommand (Command.ToggleOverwrite, () => { SetOverwrite (!Used); return true; });
			AddCommand (Command.EnableOverwrite, () => { SetOverwrite (true); return true; });
			AddCommand (Command.DisableOverwrite, () => { SetOverwrite (false); return true; });
			AddCommand (Command.Copy, () => { Copy (); return true; });
			AddCommand (Command.Cut, () => { Cut (); return true; });
			AddCommand (Command.Paste, () => { Paste (); return true; });
			AddCommand (Command.SelectAll, () => { SelectAll (); return true; });
			AddCommand (Command.DeleteAll, () => { DeleteAll (); return true; });
			AddCommand (Command.Accept, () => { ShowContextMenu (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.DeleteChar, Command.DeleteCharRight);
			AddKeyBinding (Key.D | Key.CtrlMask, Command.DeleteCharRight);

			AddKeyBinding (Key.Delete, Command.DeleteCharLeft);
			AddKeyBinding (Key.Backspace, Command.DeleteCharLeft);

			AddKeyBinding (Key.Home | Key.ShiftMask, Command.LeftHomeExtend);
			AddKeyBinding (Key.Home | Key.ShiftMask | Key.CtrlMask, Command.LeftHomeExtend);
			AddKeyBinding (Key.A | Key.ShiftMask | Key.CtrlMask, Command.LeftHomeExtend);

			AddKeyBinding (Key.End | Key.ShiftMask, Command.RightEndExtend);
			AddKeyBinding (Key.End | Key.ShiftMask | Key.CtrlMask, Command.RightEndExtend);
			AddKeyBinding (Key.E | Key.ShiftMask | Key.CtrlMask, Command.RightEndExtend);

			AddKeyBinding (Key.Home, Command.LeftHome);
			AddKeyBinding (Key.Home | Key.CtrlMask, Command.LeftHome);
			AddKeyBinding (Key.A | Key.CtrlMask, Command.LeftHome);

			AddKeyBinding (Key.CursorLeft | Key.ShiftMask, Command.LeftExtend);
			AddKeyBinding (Key.CursorUp | Key.ShiftMask, Command.LeftExtend);

			AddKeyBinding (Key.CursorRight | Key.ShiftMask, Command.RightExtend);
			AddKeyBinding (Key.CursorDown | Key.ShiftMask, Command.RightExtend);

			AddKeyBinding (Key.CursorLeft | Key.ShiftMask | Key.CtrlMask, Command.WordLeftExtend);
			AddKeyBinding (Key.CursorUp | Key.ShiftMask | Key.CtrlMask, Command.WordLeftExtend);
			AddKeyBinding ((Key)((int)'B' + Key.ShiftMask | Key.AltMask), Command.WordLeftExtend);

			AddKeyBinding (Key.CursorRight | Key.ShiftMask | Key.CtrlMask, Command.WordRightExtend);
			AddKeyBinding (Key.CursorDown | Key.ShiftMask | Key.CtrlMask, Command.WordRightExtend);
			AddKeyBinding ((Key)((int)'F' + Key.ShiftMask | Key.AltMask), Command.WordRightExtend);

			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.B | Key.CtrlMask, Command.Left);

			AddKeyBinding (Key.End, Command.RightEnd);
			AddKeyBinding (Key.End | Key.CtrlMask, Command.RightEnd);
			AddKeyBinding (Key.E | Key.CtrlMask, Command.RightEnd);

			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.F | Key.CtrlMask, Command.Right);

			AddKeyBinding (Key.K | Key.CtrlMask, Command.CutToEndLine);
			AddKeyBinding (Key.K | Key.AltMask, Command.CutToStartLine);

			AddKeyBinding (Key.Z | Key.CtrlMask, Command.Undo);
			AddKeyBinding (Key.Backspace | Key.AltMask, Command.Undo);

			AddKeyBinding (Key.Y | Key.CtrlMask, Command.Redo);

			AddKeyBinding (Key.CursorLeft | Key.CtrlMask, Command.WordLeft);
			AddKeyBinding (Key.CursorUp | Key.CtrlMask, Command.WordLeft);
			AddKeyBinding ((Key)((int)'B' + Key.AltMask), Command.WordLeft);

			AddKeyBinding (Key.CursorRight | Key.CtrlMask, Command.WordRight);
			AddKeyBinding (Key.CursorDown | Key.CtrlMask, Command.WordRight);
			AddKeyBinding ((Key)((int)'F' + Key.AltMask), Command.WordRight);

			AddKeyBinding (Key.DeleteChar | Key.CtrlMask, Command.KillWordForwards);
			AddKeyBinding (Key.Backspace | Key.CtrlMask, Command.KillWordBackwards);
			AddKeyBinding (Key.InsertChar, Command.ToggleOverwrite);
			AddKeyBinding (Key.C | Key.CtrlMask, Command.Copy);
			AddKeyBinding (Key.X | Key.CtrlMask, Command.Cut);
			AddKeyBinding (Key.V | Key.CtrlMask, Command.Paste);
			AddKeyBinding (Key.T | Key.CtrlMask, Command.SelectAll);

			AddKeyBinding (Key.R | Key.CtrlMask, Command.DeleteAll);
			AddKeyBinding (Key.D | Key.CtrlMask | Key.ShiftMask, Command.DeleteAll);

			currentCulture = Thread.CurrentThread.CurrentUICulture;

			ContextMenu = new ContextMenu (this, BuildContextMenuBarItem ());
			ContextMenu.KeyChanged += ContextMenu_KeyChanged;

			AddKeyBinding (ContextMenu.Key, Command.Accept);
		}

		private MenuBarItem BuildContextMenuBarItem ()
		{
			return new MenuBarItem (new MenuItem [] {
					new MenuItem (Strings.ctxSelectAll, "", () => SelectAll (), null, null, GetKeyFromCommand (Command.SelectAll)),
					new MenuItem (Strings.ctxDeleteAll, "", () => DeleteAll (), null, null, GetKeyFromCommand (Command.DeleteAll)),
					new MenuItem (Strings.ctxCopy, "", () => Copy (), null, null, GetKeyFromCommand (Command.Copy)),
					new MenuItem (Strings.ctxCut, "", () => Cut (), null, null, GetKeyFromCommand (Command.Cut)),
					new MenuItem (Strings.ctxPaste, "", () => Paste (), null, null, GetKeyFromCommand (Command.Paste)),
					new MenuItem (Strings.ctxUndo, "", () => UndoChanges (), null, null, GetKeyFromCommand (Command.Undo)),
					new MenuItem (Strings.ctxRedo, "", () => RedoChanges (), null, null, GetKeyFromCommand (Command.Redo)),
				});
		}

		private void ContextMenu_KeyChanged (object sender, KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
		}

		private void HistoryText_ChangeText (object sender, HistoryText.HistoryTextItem obj)
		{
			if (obj == null)
				return;

			Text = ustring.Make (obj?.Lines [obj.CursorPosition.Y]);
			CursorPosition = obj.CursorPosition.X;
			Adjust ();
		}

		void TextField_Initialized (object sender, EventArgs e)
		{
			Autocomplete.HostControl = this;
			Autocomplete.PopupInsideContainer = false;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (IsInitialized) {
				Application.Driver.SetCursorVisibility (DesiredCursorVisibility);
			}

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (Application.MouseGrabView != null && Application.MouseGrabView == this)
				Application.UngrabMouse ();
			//if (SelectedLength != 0 && !(Application.MouseGrabView is MenuBar))
			//	ClearAllSelection ();

			return base.OnLeave (view);
		}

		/// <summary>
		/// Provides autocomplete context menu based on suggestions at the current cursor
		/// position. Populate <see cref="Autocomplete.AllSuggestions"/> to enable this feature.
		/// </summary>
		public IAutocomplete Autocomplete { get; protected set; } = new TextFieldAutocomplete ();

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;
				Adjust ();
			}
		}

		/// <summary>
		///   Sets or gets the text held by the view.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public new ustring Text {
			get {
				return ustring.Make (text);
			}

			set {
				var oldText = ustring.Make (text);

				if (oldText == value)
					return;

				var newText = OnTextChanging (value.Replace ("\t", "").Split ("\n") [0]);
				if (newText.Cancel) {
					if (point > text.Count) {
						point = text.Count;
					}
					return;
				}
				ClearAllSelection ();
				text = TextModel.ToRunes (newText.NewText);

				if (!Secret && !historyText.IsFromHistory) {
					historyText.Add (new List<List<Rune>> () { oldText.ToRuneList () },
						new Point (point, 0));
					historyText.Add (new List<List<Rune>> () { text }, new Point (point, 0)
						, HistoryText.LineStatus.Replaced);
				}

				TextChanged?.Invoke (this, new TextChangedEventArgs (oldText));

				if (point > text.Count) {
					point = Math.Max (TextModel.DisplaySize (text, 0).size - 1, 0);
				}

				Adjust ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		///   Sets the secret property.
		/// </summary>
		/// <remarks>
		///   This makes the text entry suitable for entering passwords.
		/// </remarks>
		public bool Secret { get; set; }

		/// <summary>
		///    Sets or gets the current cursor position.
		/// </summary>
		public virtual int CursorPosition {
			get { return point; }
			set {
				if (value < 0) {
					point = 0;
				} else if (value > text.Count) {
					point = text.Count;
				} else {
					point = value;
				}
				PrepareSelection (selectedStart, point - selectedStart);
			}
		}

		/// <summary>
		/// Gets the left offset position.
		/// </summary>
		public int ScrollOffset => first;

		/// <summary>
		/// Indicates whatever the text was changed or not.
		/// <see langword="true"/> if the text was changed <see langword="false"/> otherwise.
		/// </summary>
		public bool IsDirty => historyText.IsDirty (Text);

		/// <summary>
		/// Indicates whatever the text has history changes or not.
		/// <see langword="true"/> if the text has history changes <see langword="false"/> otherwise.
		/// </summary>
		public bool HasHistoryChanges => historyText.HasHistoryChanges;

		/// <summary>
		/// Get the <see cref="ContextMenu"/> for this view.
		/// </summary>
		public ContextMenu ContextMenu { get; private set; }

		/// <summary>
		///   Sets the cursor position.
		/// </summary>
		public override void PositionCursor ()
		{
			var col = 0;
			for (int idx = first < 0 ? 0 : first; idx < text.Count; idx++) {
				if (idx == point)
					break;
				var cols = Rune.ColumnWidth (text [idx]);
				TextModel.SetCol (ref col, Frame.Width - 1, cols);
			}
			var pos = point - first + Math.Min (Frame.X, 0);
			var offB = OffSetBackground ();
			var containerFrame = SuperView?.ViewToScreen (SuperView.Bounds) ?? default;
			var thisFrame = ViewToScreen (Bounds);
			if (pos > -1 && col >= pos && pos < Frame.Width + offB
				&& containerFrame.IntersectsWith (thisFrame)) {
				RestoreCursorVisibility ();
				Move (col, 0);
			} else {
				HideCursorVisibility ();
				if (pos < 0) {
					Move (pos, 0, false);
				} else {
					Move (pos - offB, 0, false);
				}
			}
		}

		CursorVisibility savedCursorVisibility;

		void HideCursorVisibility ()
		{
			if (desiredCursorVisibility != CursorVisibility.Invisible) {
				DesiredCursorVisibility = CursorVisibility.Invisible;
			}
		}

		void RestoreCursorVisibility ()
		{
			if (desiredCursorVisibility != savedCursorVisibility) {
				DesiredCursorVisibility = savedCursorVisibility;
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var selColor = new Attribute (ColorScheme.Focus.Background, ColorScheme.Focus.Foreground);
			SetSelectedStartSelectedLength ();

			Driver.SetAttribute (GetNormalColor ());
			Move (0, 0);

			int p = first;
			int col = 0;
			int width = Frame.Width + OffSetBackground ();
			var tcount = text.Count;
			var roc = GetReadOnlyColor ();
			for (int idx = p; idx < tcount; idx++) {
				var rune = text [idx];
				var cols = Rune.ColumnWidth (rune);
				if (idx == point && HasFocus && !Used && length == 0 && !ReadOnly) {
					Driver.SetAttribute (selColor);
				} else if (ReadOnly) {
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? selColor : roc);
				} else if (!HasFocus && Enabled) {
					Driver.SetAttribute (ColorScheme.Focus);
				} else if (!Enabled) {
					Driver.SetAttribute (roc);
				} else {
					Driver.SetAttribute (idx >= start && length > 0 && idx < start + length ? selColor : ColorScheme.Focus);
				}
				if (col + cols <= width) {
					Driver.AddRune ((Rune)(Secret ? '*' : rune));
				}
				if (!TextModel.SetCol (ref col, width, cols)) {
					break;
				}
				if (idx + 1 < tcount && col + Rune.ColumnWidth (text [idx + 1]) > width) {
					break;
				}
			}

			Driver.SetAttribute (ColorScheme.Focus);
			for (int i = col; i < width; i++) {
				Driver.AddRune (' ');
			}

			PositionCursor ();

			if (SelectedLength > 0)
				return;

			// draw autocomplete
			Autocomplete.GenerateSuggestions ();

			var renderAt = new Point (
				CursorPosition - ScrollOffset, 0);

			Autocomplete.RenderOverlay (renderAt);
		}

		/// <inheritdoc/>
		public override Attribute GetNormalColor ()
		{
			return Enabled ? ColorScheme.Focus : ColorScheme.Disabled;
		}

		Attribute GetReadOnlyColor ()
		{
			if (ColorScheme.Disabled.Foreground == ColorScheme.Focus.Background) {
				return new Attribute (ColorScheme.Focus.Foreground, ColorScheme.Focus.Background);
			}
			return new Attribute (ColorScheme.Disabled.Foreground, ColorScheme.Focus.Background);
		}

		void Adjust ()
		{
			if (!IsAdded)
				return;

			int offB = OffSetBackground ();
			if (point < first) {
				first = point;
			} else if (Frame.Width > 0 && (first + point - (Frame.Width + offB) == 0 ||
				  TextModel.DisplaySize (text, first, point).size >= Frame.Width + offB)) {

				first = Math.Max (TextModel.CalculateLeftColumn (text, first,
					point, Frame.Width + offB), 0);
			}
			SetNeedsDisplay ();
		}

		int OffSetBackground ()
		{
			int offB = 0;
			if (SuperView?.Frame.Right - Frame.Right < 0) {
				offB = SuperView.Frame.Right - Frame.Right - 1;
			}

			return offB;
		}

		void SetText (List<Rune> newText)
		{
			Text = ustring.Make (newText);
		}

		void SetText (IEnumerable<Rune> newText)
		{
			SetText (newText.ToList ());
		}

		///<inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set { base.CanFocus = value; }
		}

		void SetClipboard (IEnumerable<Rune> text)
		{
			if (!Secret)
				Clipboard.Contents = ustring.Make (text.ToList ());
		}

		int oldCursorPos;

		/// <summary>
		/// Processes key presses for the <see cref="TextField"/>.
		/// </summary>
		/// <param name="kb"></param>
		/// <returns></returns>
		/// <remarks>
		/// The <see cref="TextField"/> control responds to the following keys:
		/// <list type="table">
		///    <listheader>
		///        <term>Keys</term>
		///        <description>Function</description>
		///    </listheader>
		///    <item>
		///        <term><see cref="Key.Delete"/>, <see cref="Key.Backspace"/></term>
		///        <description>Deletes the character before cursor.</description>
		///    </item>
		/// </list>
		/// </remarks>
		public override bool ProcessKey (KeyEvent kb)
		{
			// remember current cursor position
			// because the new calculated cursor position is needed to be set BEFORE the change event is triggest
			// Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
			oldCursorPos = point;

			// Give autocomplete first opportunity to respond to key presses
			if (SelectedLength == 0 && Autocomplete.ProcessKey (kb)) {
				return true;
			}

			var result = InvokeKeybindings (new KeyEvent (ShortcutHelper.GetModifiersKey (kb),
				new KeyModifiers () { Alt = kb.IsAlt, Ctrl = kb.IsCtrl, Shift = kb.IsShift }));
			if (result != null)
				return (bool)result;

			// Ignore other control characters.
			if (kb.Key < Key.Space || kb.Key > Key.CharMask)
				return false;

			if (ReadOnly)
				return true;

			InsertText (kb);

			return true;
		}

		void InsertText (KeyEvent kb, bool useOldCursorPos = true)
		{
			historyText.Add (new List<List<Rune>> () { text }, new Point (point, 0));

			List<Rune> newText = text;
			if (length > 0) {
				newText = DeleteSelectedText ();
				oldCursorPos = point;
			}
			if (!useOldCursorPos) {
				oldCursorPos = point;
			}
			var kbstr = TextModel.ToRunes (ustring.Make ((uint)kb.Key));
			if (Used) {
				point++;
				if (point == newText.Count + 1) {
					SetText (newText.Concat (kbstr).ToList ());
				} else {
					if (oldCursorPos > newText.Count) {
						oldCursorPos = newText.Count;
					}
					SetText (newText.GetRange (0, oldCursorPos).Concat (kbstr).Concat (newText.GetRange (oldCursorPos, Math.Min (newText.Count - oldCursorPos, newText.Count))));
				}
			} else {
				SetText (newText.GetRange (0, oldCursorPos).Concat (kbstr).Concat (newText.GetRange (Math.Min (oldCursorPos + 1, newText.Count), Math.Max (newText.Count - oldCursorPos - 1, 0))));
				point++;
			}
			Adjust ();
		}

		void SetOverwrite (bool overwrite)
		{
			Used = overwrite;
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Deletes word backwards.
		/// </summary>
		public virtual void KillWordBackwards ()
		{
			ClearAllSelection ();
			int bw = WordBackward (point);
			if (bw != -1) {
				SetText (text.GetRange (0, bw).Concat (text.GetRange (point, text.Count - point)));
				point = bw;
			}
			Adjust ();
		}

		/// <summary>
		/// Deletes word forwards.
		/// </summary>
		public virtual void KillWordForwards ()
		{
			ClearAllSelection ();
			int fw = WordForward (point);
			if (fw != -1) {
				SetText (text.GetRange (0, point).Concat (text.GetRange (fw, text.Count - fw)));
			}
			Adjust ();
		}

		void MoveWordRight ()
		{
			ClearAllSelection ();
			int fw = WordForward (point);
			if (fw != -1)
				point = fw;
			Adjust ();
		}

		void MoveWordLeft ()
		{
			ClearAllSelection ();
			int bw = WordBackward (point);
			if (bw != -1)
				point = bw;
			Adjust ();
		}

		void RedoChanges ()
		{
			if (ReadOnly)
				return;

			historyText.Redo ();

			//if (ustring.IsNullOrEmpty (Clipboard.Contents))
			//	return true;
			//var clip = TextModel.ToRunes (Clipboard.Contents);
			//if (clip == null)
			//	return true;

			//if (point == text.Count) {
			//	point = text.Count;
			//	SetText(text.Concat(clip).ToList());
			//} else {
			//	point += clip.Count;
			//	SetText(text.GetRange(0, oldCursorPos).Concat(clip).Concat(text.GetRange(oldCursorPos, text.Count - oldCursorPos)));
			//}
			//Adjust ();
		}

		void UndoChanges ()
		{
			if (ReadOnly)
				return;

			historyText.Undo ();
		}

		void KillToStart ()
		{
			if (ReadOnly)
				return;

			ClearAllSelection ();
			if (point == 0)
				return;
			SetClipboard (text.GetRange (0, point));
			SetText (text.GetRange (point, text.Count - point));
			point = 0;
			Adjust ();
		}

		void KillToEnd ()
		{
			if (ReadOnly)
				return;

			ClearAllSelection ();
			if (point >= text.Count)
				return;
			SetClipboard (text.GetRange (point, text.Count - point));
			SetText (text.GetRange (0, point));
			Adjust ();
		}

		void MoveRight ()
		{
			ClearAllSelection ();
			if (point == text.Count)
				return;
			point++;
			Adjust ();
		}

		void MoveEnd ()
		{
			ClearAllSelection ();
			point = text.Count;
			Adjust ();
		}

		void MoveLeft ()
		{
			ClearAllSelection ();
			if (point > 0) {
				point--;
				Adjust ();
			}
		}

		void MoveWordRightExtend ()
		{
			if (point < text.Count) {
				int x = start > -1 && start > point ? start : point;
				int sfw = WordForward (x);
				if (sfw != -1)
					point = sfw;
				PrepareSelection (x, sfw - x);
			}
		}

		void MoveWordLeftExtend ()
		{
			if (point > 0) {
				int x = Math.Min (start > -1 && start > point ? start : point, text.Count);
				if (x > 0) {
					int sbw = WordBackward (x);
					if (sbw != -1)
						point = sbw;
					PrepareSelection (x, sbw - x);
				}
			}
		}

		void MoveRightExtend ()
		{
			if (point < text.Count) {
				PrepareSelection (point++, 1);
			}
		}

		void MoveLeftExtend ()
		{
			if (point > 0) {
				PrepareSelection (point--, -1);
			}
		}

		void MoveHome ()
		{
			ClearAllSelection ();
			point = 0;
			Adjust ();
		}

		void MoveEndExtend ()
		{
			if (point <= text.Count) {
				int x = point;
				point = text.Count;
				PrepareSelection (x, point - x);
			}
		}

		void MoveHomeExtend ()
		{
			if (point > 0) {
				int x = point;
				point = 0;
				PrepareSelection (x, point - x);
			}
		}

		/// <summary>
		/// Deletes the left character.
		/// </summary>
		public virtual void DeleteCharLeft (bool useOldCursorPos = true)
		{
			if (ReadOnly)
				return;

			historyText.Add (new List<List<Rune>> () { text }, new Point (point, 0));

			if (length == 0) {
				if (point == 0)
					return;

				if (!useOldCursorPos) {
					oldCursorPos = point;
				}
				point--;
				if (oldCursorPos < text.Count) {
					SetText (text.GetRange (0, oldCursorPos - 1).Concat (text.GetRange (oldCursorPos, text.Count - oldCursorPos)));
				} else {
					SetText (text.GetRange (0, oldCursorPos - 1));
				}
				Adjust ();
			} else {
				var newText = DeleteSelectedText ();
				Text = ustring.Make (newText);
				Adjust ();
			}
		}

		/// <summary>
		/// Deletes the right character.
		/// </summary>
		public virtual void DeleteCharRight ()
		{
			if (ReadOnly)
				return;

			historyText.Add (new List<List<Rune>> () { text }, new Point (point, 0));

			if (length == 0) {
				if (text.Count == 0 || text.Count == point)
					return;

				SetText (text.GetRange (0, point).Concat (text.GetRange (point + 1, text.Count - (point + 1))));
				Adjust ();
			} else {
				var newText = DeleteSelectedText ();
				Text = ustring.Make (newText);
				Adjust ();
			}
		}

		int WordForward (int p)
		{
			if (p >= text.Count)
				return -1;

			int i = p + 1;
			if (i == text.Count)
				return text.Count;

			var ti = text [i];
			if (Rune.IsLetterOrDigit (ti) && Rune.IsWhiteSpace (text [p]))
				return i;

			if (Rune.IsPunctuation (ti) || Rune.IsSymbol (ti) || Rune.IsWhiteSpace (ti)) {
				for (; i < text.Count; i++) {
					if (Rune.IsLetterOrDigit (text [i]))
						return i;
				}
			} else {
				for (; i < text.Count; i++) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
				}
				for (; i < text.Count; i++) {
					if (Rune.IsLetterOrDigit (text [i]) ||
						(Rune.IsPunctuation (text [i]) && Rune.IsWhiteSpace (text [i - 1])))
						break;
				}
			}

			if (i != p)
				return Math.Min (i, text.Count);

			return -1;
		}

		int WordBackward (int p)
		{
			if (p == 0)
				return -1;

			int i = p - 1;
			if (i == 0)
				return 0;

			var ti = text [i];
			var lastValidCol = -1;
			if (Rune.IsPunctuation (ti) || Rune.IsSymbol (ti) || Rune.IsWhiteSpace (ti)) {
				for (; i >= 0; i--) {
					if (Rune.IsLetterOrDigit (text [i])) {
						lastValidCol = i;
						break;
					}
					if (i - 1 > 0 && !Rune.IsWhiteSpace (text [i]) && Rune.IsWhiteSpace (text [i - 1])) {
						return i;
					}
				}
				for (; i >= 0; i--) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
					lastValidCol = i;
				}
				if (lastValidCol > -1) {
					return lastValidCol;
				}
			} else {
				for (; i >= 0; i--) {
					if (!Rune.IsLetterOrDigit (text [i]))
						break;
					lastValidCol = i;
				}
				if (lastValidCol > -1) {
					return lastValidCol;
				}
			}

			if (i != p)
				return Math.Max (i, 0);

			return -1;
		}

		void ShowContextMenu ()
		{
			if (currentCulture != Thread.CurrentThread.CurrentUICulture) {

				currentCulture = Thread.CurrentThread.CurrentUICulture;

				ContextMenu.MenuItems = BuildContextMenuBarItem ();
			}
			ContextMenu.Show ();
		}

		/// <summary>
		/// Selects all text.
		/// </summary>
		public void SelectAll ()
		{
			if (text.Count == 0) {
				return;
			}

			selectedStart = 0;
			MoveEndExtend ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Deletes all text.
		/// </summary>
		public void DeleteAll ()
		{
			if (text.Count == 0) {
				return;
			}

			selectedStart = 0;
			MoveEndExtend ();
			DeleteCharLeft ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Start position of the selected text.
		/// </summary>
		public int SelectedStart {
			get => selectedStart;
			set {
				if (value < -1) {
					selectedStart = -1;
				} else if (value > text.Count) {
					selectedStart = text.Count;
				} else {
					selectedStart = value;
				}
				PrepareSelection (selectedStart, point - selectedStart);
			}
		}

		/// <summary>
		/// Length of the selected text.
		/// </summary>
		public int SelectedLength { get => length; }

		/// <summary>
		/// The selected text.
		/// </summary>
		public ustring SelectedText {
			get => Secret ? null : selectedText;
			private set => selectedText = value;
		}

		int start, length;
		bool isButtonPressed;
		bool isButtonReleased = true;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent ev)
		{
			if (!ev.Flags.HasFlag (MouseFlags.Button1Pressed) && !ev.Flags.HasFlag (MouseFlags.ReportMousePosition) &&
				!ev.Flags.HasFlag (MouseFlags.Button1Released) && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				!ev.Flags.HasFlag (MouseFlags.Button1TripleClicked) && !ev.Flags.HasFlag (ContextMenu.MouseFlags)) {
				return false;
			}

			if (!CanFocus) {
				return true;
			}

			if (!HasFocus && ev.Flags != MouseFlags.ReportMousePosition) {
				SetFocus ();
			}

			// Give autocomplete first opportunity to respond to mouse clicks
			if (SelectedLength == 0 && Autocomplete.MouseEvent (ev, true)) {
				return true;
			}

			if (ev.Flags == MouseFlags.Button1Pressed) {
				EnsureHasFocus ();
				PositionCursor (ev);
				if (isButtonReleased) {
					ClearAllSelection ();
				}
				isButtonReleased = true;
				isButtonPressed = true;
			} else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && isButtonPressed) {
				int x = PositionCursor (ev);
				isButtonReleased = false;
				PrepareSelection (x);
				if (Application.MouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags == MouseFlags.Button1Released) {
				isButtonReleased = true;
				isButtonPressed = false;
				Application.UngrabMouse ();
			} else if (ev.Flags == MouseFlags.Button1DoubleClicked) {
				EnsureHasFocus ();
				int x = PositionCursor (ev);
				int sbw = x;
				if (x == text.Count || (x > 0 && (char)text [x - 1] != ' ')
					|| (x > 0 && (char)text [x] == ' ')) {

					sbw = WordBackward (x);
				}
				if (sbw != -1) {
					x = sbw;
					PositionCursor (x);
				}
				int sfw = WordForward (x);
				ClearAllSelection ();
				if (sfw != -1 && sbw != -1) {
					point = sfw;
				}
				PrepareSelection (sbw, sfw - sbw);
			} else if (ev.Flags == MouseFlags.Button1TripleClicked) {
				EnsureHasFocus ();
				PositionCursor (0);
				ClearAllSelection ();
				PrepareSelection (0, text.Count);
			} else if (ev.Flags == ContextMenu.MouseFlags) {
				ShowContextMenu ();
			}

			SetNeedsDisplay ();
			return true;

			void EnsureHasFocus ()
			{
				if (!HasFocus) {
					SetFocus ();
				}
			}
		}

		int PositionCursor (MouseEvent ev)
		{
			// We could also set the cursor position.
			int x;
			var pX = TextModel.GetColFromX (text, first, ev.X);
			if (text.Count == 0) {
				x = pX - ev.OfX;
			} else {
				x = pX;
			}
			return PositionCursor (x, false);
		}

		int PositionCursor (int x, bool getX = true)
		{
			int pX = x;
			if (getX) {
				pX = TextModel.GetColFromX (text, first, x);
			}
			if (first + pX > text.Count) {
				point = text.Count;
			} else if (first + pX < first) {
				point = 0;
			} else {
				point = first + pX;
			}

			return point;
		}

		void PrepareSelection (int x, int direction = 0)
		{
			x = x + first < -1 ? 0 : x;
			selectedStart = selectedStart == -1 && text.Count > 0 && x >= 0 && x <= text.Count ? x : selectedStart;
			if (selectedStart > -1) {
				length = Math.Abs (x + direction <= text.Count ? x + direction - selectedStart : text.Count - selectedStart);
				SetSelectedStartSelectedLength ();
				if (start > -1 && length > 0) {
					selectedText = length > 0 ? ustring.Make (text).ToString ().Substring (
						start < 0 ? 0 : start, length > text.Count ? text.Count : length) : "";
					if (first > start) {
						first = start;
					}
				} else if (start > -1 && length == 0) {
					selectedText = null;
				}
			} else if (length > 0 || selectedText != null) {
				ClearAllSelection ();
			}
			Adjust ();
		}

		/// <summary>
		/// Clear the selected text.
		/// </summary>
		public void ClearAllSelection ()
		{
			if (selectedStart == -1 && length == 0 && selectedText == "")
				return;

			selectedStart = -1;
			length = 0;
			selectedText = null;
			start = 0;
			length = 0;
			SetNeedsDisplay ();
		}

		void SetSelectedStartSelectedLength ()
		{
			if (SelectedStart > -1 && point < SelectedStart) {
				start = point;
			} else {
				start = SelectedStart;
			}
		}

		/// <summary>
		/// Copy the selected text to the clipboard.
		/// </summary>
		public virtual void Copy ()
		{
			if (Secret || length == 0)
				return;

			Clipboard.Contents = SelectedText;
		}

		/// <summary>
		/// Cut the selected text to the clipboard.
		/// </summary>
		public virtual void Cut ()
		{
			if (ReadOnly || Secret || length == 0)
				return;

			Clipboard.Contents = SelectedText;
			var newText = DeleteSelectedText ();
			Text = ustring.Make (newText);
			Adjust ();
		}

		List<Rune> DeleteSelectedText ()
		{
			ustring actualText = Text;
			SetSelectedStartSelectedLength ();
			int selStart = SelectedStart > -1 ? start : point;
			(var _, var len) = TextModel.DisplaySize (text, 0, selStart, false);
			(var _, var len2) = TextModel.DisplaySize (text, selStart, selStart + length, false);
			(var _, var len3) = TextModel.DisplaySize (text, selStart + length, actualText.RuneCount, false);
			var newText = actualText [0, len] +
				actualText [len + len2, len + len2 + len3];
			ClearAllSelection ();
			point = selStart >= newText.RuneCount ? newText.RuneCount : selStart;
			return newText.ToRuneList ();
		}

		/// <summary>
		/// Paste the selected text from the clipboard.
		/// </summary>
		public virtual void Paste ()
		{
			if (ReadOnly || ustring.IsNullOrEmpty (Clipboard.Contents)) {
				return;
			}

			SetSelectedStartSelectedLength ();
			int selStart = start == -1 ? CursorPosition : start;
			ustring actualText = Text;
			(int _, int len) = TextModel.DisplaySize (text, 0, selStart, false);
			(var _, var len2) = TextModel.DisplaySize (text, selStart, selStart + length, false);
			(var _, var len3) = TextModel.DisplaySize (text, selStart + length, actualText.RuneCount, false);
			ustring cbTxt = Clipboard.Contents.Split ("\n") [0] ?? "";
			Text = actualText [0, len] +
				cbTxt +
				actualText [len + len2, len + len2 + len3];
			point = selStart + cbTxt.RuneCount;
			ClearAllSelection ();
			SetNeedsDisplay ();
			Adjust ();
		}

		/// <summary>
		/// Virtual method that invoke the <see cref="TextChanging"/> event if it's defined.
		/// </summary>
		/// <param name="newText">The new text to be replaced.</param>
		/// <returns>Returns the <see cref="TextChangingEventArgs"/></returns>
		public virtual TextChangingEventArgs OnTextChanging (ustring newText)
		{
			var ev = new TextChangingEventArgs (newText);
			TextChanging?.Invoke (this, ev);
			return ev;
		}

		CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

		/// <summary>
		/// Get / Set the wished cursor when the field is focused
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get => desiredCursorVisibility;
			set {
				if (desiredCursorVisibility != value && HasFocus) {
					Application.Driver.SetCursorVisibility (value);
				}

				desiredCursorVisibility = value;
			}
		}

		/// <summary>
		/// Inserts the given <paramref name="toAdd"/> text at the current cursor position
		/// exactly as if the user had just typed it
		/// </summary>
		/// <param name="toAdd">Text to add</param>
		/// <param name="useOldCursorPos">If uses the <see cref="oldCursorPos"/>.</param>
		public void InsertText (string toAdd, bool useOldCursorPos = true)
		{
			foreach (var ch in toAdd) {

				Key key;

				try {
					key = (Key)ch;
				} catch (Exception) {

					throw new ArgumentException ($"Cannot insert character '{ch}' because it does not map to a Key");
				}

				InsertText (new KeyEvent () { Key = key }, useOldCursorPos);
			}
		}

		/// <summary>
		/// Allows clearing the <see cref="HistoryText.HistoryTextItem"/> items updating the original text.
		/// </summary>
		public void ClearHistoryChanges ()
		{
			historyText.Clear (Text);
		}
	}
	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// An implementation on a TextField.
	/// </summary>
	public class TextFieldAutocomplete : Autocomplete {

		/// <inheritdoc/>
		protected override void DeleteTextBackwards ()
		{
			((TextField)HostControl).DeleteCharLeft (false);
		}

		/// <inheritdoc/>
		protected override string GetCurrentWord (int columnOffset = 0)
		{
			var host = (TextField)HostControl;
			var currentLine = host.Text.ToRuneList ();
			var cursorPosition = Math.Min (host.CursorPosition + columnOffset, currentLine.Count);
			return IdxToWord (currentLine, cursorPosition, columnOffset);
		}

		/// <inheritdoc/>
		protected override void InsertText (string accepted)
		{
			((TextField)HostControl).InsertText (accepted, false);
		}
	}
}
