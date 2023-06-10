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
using System.Text;
using Terminal.Gui.Resources;


namespace Terminal.Gui {
	/// <summary>
	///   Single-line text entry <see cref="View"/>
	/// </summary>
	/// <remarks>
	///   The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.
	/// </remarks>
	public class TextField : View {
		List<Rune> _text;
		int _first, _point;
		int _selectedStart = -1; // -1 represents there is no text selection.
		string _selectedText;
		HistoryText _historyText = new HistoryText ();
		CultureInfo _currentCulture;

		/// <summary>
		/// Gets or sets the text to render in control when no value has 
		/// been entered yet and the <see cref="View"/> does not yet have
		/// input focus.
		/// </summary>
		public string Caption { get; set; }

		/// <summary>
		/// Gets or sets the foreground <see cref="Color"/> to use when 
		/// rendering <see cref="Caption"/>.
		/// </summary>
		public Color CaptionColor { get; set; } = Color.DarkGray;

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
		///   The passed <see cref="EventArgs"/> is a <see cref="string"/> containing the old value. 
		/// </remarks>
		public event EventHandler<TextChangedEventArgs> TextChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public TextField () : this (string.Empty) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="text">Initial text contents.</param>
		public TextField (string text) : base (text)
		{
			SetInitialProperties (text, text.GetRuneCount () + 1);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="w">The width.</param>
		/// <param name="text">Initial text contents.</param>
		public TextField (int x, int y, int w, string text) : base (new Rect (x, y, w, 1))
		{
			SetInitialProperties (text, w);
		}

		void SetInitialProperties (string text, int w)
		{
			Height = 1;

			if (text == null)
				text = "";

			this._text = TextModel.ToRunes (text.Split ("\n") [0]);
			_point = text.GetRuneCount ();
			_first = _point > w + 1 ? _point - w + 1 : 0;
			CanFocus = true;
			Used = true;
			WantMousePositionReports = true;
			_savedCursorVisibility = _desiredCursorVisibility;

			_historyText.ChangeText += HistoryText_ChangeText;

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

			_currentCulture = Thread.CurrentThread.CurrentUICulture;

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

			Text = StringExtensions.ToString (obj?.Lines [obj.CursorPosition.Y]);
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
		/// position. Configure <see cref="ISuggestionGenerator"/> to enable this feature.
		/// </summary>
		public IAutocomplete Autocomplete { get; set; } = new TextFieldAutocomplete ();

		///<inheritdoc/>
		public override Rect Frame {
			get => base.Frame;
			set {
				if (value.Height > 1) {
					base.Frame = new Rect (value.X, value.Y, value.Width, 1);
					Height = 1;
				} else {
					base.Frame = value;
				}
				Adjust ();
			}
		}

		/// <summary>
		///   Sets or gets the text held by the view.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public new string Text {
			get {
				return StringExtensions.ToString (_text);
			}

			set {
				var oldText = StringExtensions.ToString (_text);

				if (oldText == value)
					return;

				var newText = OnTextChanging (value.Replace ("\t", "").Split ("\n") [0]);
				if (newText.Cancel) {
					if (_point > _text.Count) {
						_point = _text.Count;
					}
					return;
				}
				ClearAllSelection ();
				_text = TextModel.ToRunes (newText.NewText);

				if (!Secret && !_historyText.IsFromHistory) {
					_historyText.Add (new List<List<Rune>> () { oldText.ToRuneList () },
						new Point (_point, 0));
					_historyText.Add (new List<List<Rune>> () { _text }, new Point (_point, 0)
						, HistoryText.LineStatus.Replaced);
				}

				TextChanged?.Invoke (this, new TextChangedEventArgs (oldText));

				if (_point > _text.Count) {
					_point = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
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
			get { return _point; }
			set {
				if (value < 0) {
					_point = 0;
				} else if (value > _text.Count) {
					_point = _text.Count;
				} else {
					_point = value;
				}
				PrepareSelection (_selectedStart, _point - _selectedStart);
			}
		}

		/// <summary>
		/// Gets the left offset position.
		/// </summary>
		public int ScrollOffset => _first;

		/// <summary>
		/// Indicates whatever the text was changed or not.
		/// <see langword="true"/> if the text was changed <see langword="false"/> otherwise.
		/// </summary>
		public bool IsDirty => _historyText.IsDirty (Text);

		/// <summary>
		/// Indicates whatever the text has history changes or not.
		/// <see langword="true"/> if the text has history changes <see langword="false"/> otherwise.
		/// </summary>
		public bool HasHistoryChanges => _historyText.HasHistoryChanges;

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
			for (int idx = _first < 0 ? 0 : _first; idx < _text.Count; idx++) {
				if (idx == _point)
					break;
				var cols = _text [idx].GetColumns ();
				TextModel.SetCol (ref col, Frame.Width - 1, cols);
			}
			var pos = _point - _first + Math.Min (Frame.X, 0);
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

		CursorVisibility _savedCursorVisibility;

		void HideCursorVisibility ()
		{
			if (_desiredCursorVisibility != CursorVisibility.Invisible) {
				DesiredCursorVisibility = CursorVisibility.Invisible;
			}
		}

		CursorVisibility _visibility;

		void RestoreCursorVisibility ()
		{
			Application.Driver.GetCursorVisibility (out _visibility);
			if (_desiredCursorVisibility != _savedCursorVisibility || _visibility != _savedCursorVisibility) {
				DesiredCursorVisibility = _savedCursorVisibility;
			}
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			var selColor = new Attribute (ColorScheme.Focus.Background, ColorScheme.Focus.Foreground);
			SetSelectedStartSelectedLength ();

			Driver.SetAttribute (GetNormalColor ());
			Move (0, 0);

			int p = _first;
			int col = 0;
			int width = Frame.Width + OffSetBackground ();
			var tcount = _text.Count;
			var roc = GetReadOnlyColor ();
			for (int idx = p; idx < tcount; idx++) {
				var rune = _text [idx];
				var cols = ((Rune)rune).GetColumns ();
				if (idx == _point && HasFocus && !Used && _length == 0 && !ReadOnly) {
					Driver.SetAttribute (selColor);
				} else if (ReadOnly) {
					Driver.SetAttribute (idx >= _start && _length > 0 && idx < _start + _length ? selColor : roc);
				} else if (!HasFocus && Enabled) {
					Driver.SetAttribute (ColorScheme.Focus);
				} else if (!Enabled) {
					Driver.SetAttribute (roc);
				} else {
					Driver.SetAttribute (idx >= _start && _length > 0 && idx < _start + _length ? selColor : ColorScheme.Focus);
				}
				if (col + cols <= width) {
					Driver.AddRune ((Rune)(Secret ? CM.Glyphs.Dot : rune));
				}
				if (!TextModel.SetCol (ref col, width, cols)) {
					break;
				}
				if (idx + 1 < tcount && col + _text [idx + 1].GetColumns () > width) {
					break;
				}
			}

			Driver.SetAttribute (ColorScheme.Focus);
			for (int i = col; i < width; i++) {
				Driver.AddRune ((Rune)' ');
			}

			PositionCursor ();

			RenderCaption ();

			if (SelectedLength > 0)
				return;

			// draw autocomplete
			GenerateSuggestions ();

			var renderAt = new Point (
				CursorPosition - ScrollOffset, 0);

			Autocomplete.RenderOverlay (renderAt);
		}

		private void RenderCaption ()
		{

			if (HasFocus || Caption == null || Caption.Length == 0
				|| Text?.Length > 0) {
				return;
			}

			var color = new Attribute (CaptionColor, GetNormalColor ().Background);
			Driver.SetAttribute (color);

			Move (0, 0);
			var render = Caption;

			if (render.GetColumns () > Bounds.Width) {
				render = render [..Bounds.Width];
			}

			Driver.AddStr (render);
		}

		private void GenerateSuggestions ()
		{
			var currentLine = Text.ToRuneList ();
			var cursorPosition = Math.Min (this.CursorPosition, currentLine.Count);

			Autocomplete.GenerateSuggestions (
				new AutocompleteContext (currentLine, cursorPosition)
				);
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
			if (_point < _first) {
				_first = _point;
			} else if (Frame.Width > 0 && (_first + _point - (Frame.Width + offB) == 0 ||
				  TextModel.DisplaySize (_text, _first, _point).size >= Frame.Width + offB)) {

				_first = Math.Max (TextModel.CalculateLeftColumn (_text, _first,
					_point, Frame.Width + offB), 0);
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
			Text = StringExtensions.ToString (newText);
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
				Clipboard.Contents = StringExtensions.ToString (text.ToList ());
		}

		int _oldCursorPos;

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
			_oldCursorPos = _point;

			// Give autocomplete first opportunity to respond to key presses
			if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (kb)) {
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
			_historyText.Add (new List<List<Rune>> () { _text }, new Point (_point, 0));

			List<Rune> newText = _text;
			if (_length > 0) {
				newText = DeleteSelectedText ();
				_oldCursorPos = _point;
			}
			if (!useOldCursorPos) {
				_oldCursorPos = _point;
			}
			var kbstr = TextModel.ToRunes (((Rune)(uint)kb.Key).ToString ());
			if (Used) {
				_point++;
				if (_point == newText.Count + 1) {
					SetText (newText.Concat (kbstr).ToList ());
				} else {
					if (_oldCursorPos > newText.Count) {
						_oldCursorPos = newText.Count;
					}
					SetText (newText.GetRange (0, _oldCursorPos).Concat (kbstr).Concat (newText.GetRange (_oldCursorPos, Math.Min (newText.Count - _oldCursorPos, newText.Count))));
				}
			} else {
				SetText (newText.GetRange (0, _oldCursorPos).Concat (kbstr).Concat (newText.GetRange (Math.Min (_oldCursorPos + 1, newText.Count), Math.Max (newText.Count - _oldCursorPos - 1, 0))));
				_point++;
			}
			Adjust ();
		}

		void SetOverwrite (bool overwrite)
		{
			Used = overwrite;
			SetNeedsDisplay ();
		}

		TextModel GetModel ()
		{
			var model = new TextModel ();
			model.LoadString (Text);
			return model;
		}

		/// <summary>
		/// Deletes word backwards.
		/// </summary>
		public virtual void KillWordBackwards ()
		{
			ClearAllSelection ();
			var newPos = GetModel ().WordBackward (_point, 0);
			if (newPos == null) return;
			if (newPos.Value.col != -1) {
				SetText (_text.GetRange (0, newPos.Value.col).Concat (_text.GetRange (_point, _text.Count - _point)));
				_point = newPos.Value.col;
			}
			Adjust ();
		}

		/// <summary>
		/// Deletes word forwards.
		/// </summary>
		public virtual void KillWordForwards ()
		{
			ClearAllSelection ();
			var newPos = GetModel ().WordForward (_point, 0);
			if (newPos == null) return;
			if (newPos.Value.col != -1) {
				SetText (_text.GetRange (0, _point).Concat (_text.GetRange (newPos.Value.col, _text.Count - newPos.Value.col)));
			}
			Adjust ();
		}

		void MoveWordRight ()
		{
			ClearAllSelection ();
			var newPos = GetModel ().WordForward (_point, 0);
			if (newPos == null) return;
			if (newPos.Value.col != -1)
				_point = newPos.Value.col;
			Adjust ();
		}

		void MoveWordLeft ()
		{
			ClearAllSelection ();
			var newPos = GetModel ().WordBackward (_point, 0);
			if (newPos == null) return;
			if (newPos.Value.col != -1)
				_point = newPos.Value.col;
			Adjust ();
		}

		void RedoChanges ()
		{
			if (ReadOnly)
				return;

			_historyText.Redo ();

			//if (string.IsNullOrEmpty (Clipboard.Contents))
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

			_historyText.Undo ();
		}

		void KillToStart ()
		{
			if (ReadOnly)
				return;

			ClearAllSelection ();
			if (_point == 0)
				return;
			SetClipboard (_text.GetRange (0, _point));
			SetText (_text.GetRange (_point, _text.Count - _point));
			_point = 0;
			Adjust ();
		}

		void KillToEnd ()
		{
			if (ReadOnly)
				return;

			ClearAllSelection ();
			if (_point >= _text.Count)
				return;
			SetClipboard (_text.GetRange (_point, _text.Count - _point));
			SetText (_text.GetRange (0, _point));
			Adjust ();
		}

		void MoveRight ()
		{
			ClearAllSelection ();
			if (_point == _text.Count)
				return;
			_point++;
			Adjust ();
		}

		/// <summary>
		/// Moves cursor to the end of the typed text.
		/// </summary>
		public void MoveEnd ()
		{
			ClearAllSelection ();
			_point = _text.Count;
			Adjust ();
		}

		void MoveLeft ()
		{
			ClearAllSelection ();
			if (_point > 0) {
				_point--;
				Adjust ();
			}
		}

		void MoveWordRightExtend ()
		{
			if (_point < _text.Count) {
				int x = _start > -1 && _start > _point ? _start : _point;
				var newPos = GetModel ().WordForward (x, 0);
				if (newPos == null) return;
				if (newPos.Value.col != -1)
					_point = newPos.Value.col;
				PrepareSelection (x, newPos.Value.col - x);
			}
		}

		void MoveWordLeftExtend ()
		{
			if (_point > 0) {
				int x = Math.Min (_start > -1 && _start > _point ? _start : _point, _text.Count);
				if (x > 0) {
					var newPos = GetModel ().WordBackward (x, 0);
					if (newPos == null) return;
					if (newPos.Value.col != -1)
						_point = newPos.Value.col;
					PrepareSelection (x, newPos.Value.col - x);
				}
			}
		}

		void MoveRightExtend ()
		{
			if (_point < _text.Count) {
				PrepareSelection (_point++, 1);
			}
		}

		void MoveLeftExtend ()
		{
			if (_point > 0) {
				PrepareSelection (_point--, -1);
			}
		}

		void MoveHome ()
		{
			ClearAllSelection ();
			_point = 0;
			Adjust ();
		}

		void MoveEndExtend ()
		{
			if (_point <= _text.Count) {
				int x = _point;
				_point = _text.Count;
				PrepareSelection (x, _point - x);
			}
		}

		void MoveHomeExtend ()
		{
			if (_point > 0) {
				int x = _point;
				_point = 0;
				PrepareSelection (x, _point - x);
			}
		}

		/// <summary>
		/// Deletes the left character.
		/// </summary>
		public virtual void DeleteCharLeft (bool useOldCursorPos = true)
		{
			if (ReadOnly)
				return;

			_historyText.Add (new List<List<Rune>> () { _text }, new Point (_point, 0));

			if (_length == 0) {
				if (_point == 0)
					return;

				if (!useOldCursorPos) {
					_oldCursorPos = _point;
				}
				_point--;
				if (_oldCursorPos < _text.Count) {
					SetText (_text.GetRange (0, _oldCursorPos - 1).Concat (_text.GetRange (_oldCursorPos, _text.Count - _oldCursorPos)));
				} else {
					SetText (_text.GetRange (0, _oldCursorPos - 1));
				}
				Adjust ();
			} else {
				var newText = DeleteSelectedText ();
				Text = StringExtensions.ToString (newText);
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

			_historyText.Add (new List<List<Rune>> () { _text }, new Point (_point, 0));

			if (_length == 0) {
				if (_text.Count == 0 || _text.Count == _point)
					return;

				SetText (_text.GetRange (0, _point).Concat (_text.GetRange (_point + 1, _text.Count - (_point + 1))));
				Adjust ();
			} else {
				var newText = DeleteSelectedText ();
				Text = StringExtensions.ToString (newText);
				Adjust ();
			}
		}

		void ShowContextMenu ()
		{
			if (_currentCulture != Thread.CurrentThread.CurrentUICulture) {

				_currentCulture = Thread.CurrentThread.CurrentUICulture;

				ContextMenu.MenuItems = BuildContextMenuBarItem ();
			}
			ContextMenu.Show ();
		}

		/// <summary>
		/// Selects all text.
		/// </summary>
		public void SelectAll ()
		{
			if (_text.Count == 0) {
				return;
			}

			_selectedStart = 0;
			MoveEndExtend ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Deletes all text.
		/// </summary>
		public void DeleteAll ()
		{
			if (_text.Count == 0) {
				return;
			}

			_selectedStart = 0;
			MoveEndExtend ();
			DeleteCharLeft ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Start position of the selected text.
		/// </summary>
		public int SelectedStart {
			get => _selectedStart;
			set {
				if (value < -1) {
					_selectedStart = -1;
				} else if (value > _text.Count) {
					_selectedStart = _text.Count;
				} else {
					_selectedStart = value;
				}
				PrepareSelection (_selectedStart, _point - _selectedStart);
			}
		}

		/// <summary>
		/// Length of the selected text.
		/// </summary>
		public int SelectedLength { get => _length; }

		/// <summary>
		/// The selected text.
		/// </summary>
		public string SelectedText {
			get => Secret ? null : _selectedText;
			private set => _selectedText = value;
		}

		int _start, _length;
		bool _isButtonPressed;
		bool _isButtonReleased = true;

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
				if (_isButtonReleased) {
					ClearAllSelection ();
				}
				_isButtonReleased = true;
				_isButtonPressed = true;
			} else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && _isButtonPressed) {
				int x = PositionCursor (ev);
				_isButtonReleased = false;
				PrepareSelection (x);
				if (Application.MouseGrabView == null) {
					Application.GrabMouse (this);
				}
			} else if (ev.Flags == MouseFlags.Button1Released) {
				_isButtonReleased = true;
				_isButtonPressed = false;
				Application.UngrabMouse ();
			} else if (ev.Flags == MouseFlags.Button1DoubleClicked) {
				EnsureHasFocus ();
				int x = PositionCursor (ev);
				int sbw = x;
				if (x == _text.Count || (x > 0 && (char)_text [x - 1].Value != ' ')
					|| (x > 0 && (char)_text [x].Value == ' ')) {

					var newPosBw = GetModel ().WordBackward (x, 0);
					if (newPosBw == null) return true;
					sbw = newPosBw.Value.col;
				}
				if (sbw != -1) {
					x = sbw;
					PositionCursor (x);
				}
				var newPosFw = GetModel ().WordForward (x, 0);
				if (newPosFw == null) return true;
				ClearAllSelection ();
				if (newPosFw.Value.col != -1 && sbw != -1) {
					_point = newPosFw.Value.col;
				}
				PrepareSelection (sbw, newPosFw.Value.col - sbw);
			} else if (ev.Flags == MouseFlags.Button1TripleClicked) {
				EnsureHasFocus ();
				PositionCursor (0);
				ClearAllSelection ();
				PrepareSelection (0, _text.Count);
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
			var pX = TextModel.GetColFromX (_text, _first, ev.X);
			if (_text.Count == 0) {
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
				pX = TextModel.GetColFromX (_text, _first, x);
			}
			if (_first + pX > _text.Count) {
				_point = _text.Count;
			} else if (_first + pX < _first) {
				_point = 0;
			} else {
				_point = _first + pX;
			}

			return _point;
		}

		void PrepareSelection (int x, int direction = 0)
		{
			x = x + _first < -1 ? 0 : x;
			_selectedStart = _selectedStart == -1 && _text.Count > 0 && x >= 0 && x <= _text.Count ? x : _selectedStart;
			if (_selectedStart > -1) {
				_length = Math.Abs (x + direction <= _text.Count ? x + direction - _selectedStart : _text.Count - _selectedStart);
				SetSelectedStartSelectedLength ();
				if (_start > -1 && _length > 0) {
					_selectedText = _length > 0 ? StringExtensions.ToString (_text.GetRange (
						_start < 0 ? 0 : _start, _length > _text.Count ? _text.Count : _length)) : "";
					if (_first > _start) {
						_first = _start;
					}
				} else if (_start > -1 && _length == 0) {
					_selectedText = null;
				}
			} else if (_length > 0 || _selectedText != null) {
				ClearAllSelection ();
			}
			Adjust ();
		}

		/// <summary>
		/// Clear the selected text.
		/// </summary>
		public void ClearAllSelection ()
		{
			if (_selectedStart == -1 && _length == 0 && _selectedText == "")
				return;

			_selectedStart = -1;
			_length = 0;
			_selectedText = null;
			_start = 0;
			_length = 0;
			SetNeedsDisplay ();
		}

		void SetSelectedStartSelectedLength ()
		{
			if (SelectedStart > -1 && _point < SelectedStart) {
				_start = _point;
			} else {
				_start = SelectedStart;
			}
		}

		/// <summary>
		/// Copy the selected text to the clipboard.
		/// </summary>
		public virtual void Copy ()
		{
			if (Secret || _length == 0)
				return;

			Clipboard.Contents = SelectedText;
		}

		/// <summary>
		/// Cut the selected text to the clipboard.
		/// </summary>
		public virtual void Cut ()
		{
			if (ReadOnly || Secret || _length == 0)
				return;

			Clipboard.Contents = SelectedText;
			var newText = DeleteSelectedText ();
			Text = StringExtensions.ToString (newText);
			Adjust ();
		}

		List<Rune> DeleteSelectedText ()
		{
			string actualText = Text;
			SetSelectedStartSelectedLength ();
			int selStart = SelectedStart > -1 ? _start : _point;
			(var size, var _) = TextModel.DisplaySize (_text, 0, selStart, false);
			(var size2, var _) = TextModel.DisplaySize (_text, selStart, selStart + _length, false);
			(var size3, var _) = TextModel.DisplaySize (_text, selStart + _length, actualText.GetRuneCount (), false);
			var newText = actualText [..size] +
				actualText.Substring (size + size2, size3);
			ClearAllSelection ();
			_point = selStart >= newText.GetRuneCount () ? newText.GetRuneCount () : selStart;
			return newText.ToRuneList ();
		}

		/// <summary>
		/// Paste the selected text from the clipboard.
		/// </summary>
		public virtual void Paste ()
		{
			if (ReadOnly || string.IsNullOrEmpty (Clipboard.Contents)) {
				return;
			}

			SetSelectedStartSelectedLength ();
			int selStart = _start == -1 ? CursorPosition : _start;
			string actualText = Text;
			(int size, int _) = TextModel.DisplaySize (_text, 0, selStart, false);
			(var size2, var _) = TextModel.DisplaySize (_text, selStart, selStart + _length, false);
			(var size3, var _) = TextModel.DisplaySize (_text, selStart + _length, actualText.GetRuneCount (), false);
			string cbTxt = Clipboard.Contents.Split ("\n") [0] ?? "";
			Text = actualText [..size] +
				cbTxt +
				actualText.Substring (size + size2, size3);
			_point = selStart + cbTxt.GetRuneCount ();
			ClearAllSelection ();
			SetNeedsDisplay ();
			Adjust ();
		}

		/// <summary>
		/// Virtual method that invoke the <see cref="TextChanging"/> event if it's defined.
		/// </summary>
		/// <param name="newText">The new text to be replaced.</param>
		/// <returns>Returns the <see cref="TextChangingEventArgs"/></returns>
		public virtual TextChangingEventArgs OnTextChanging (string newText)
		{
			var ev = new TextChangingEventArgs (newText);
			TextChanging?.Invoke (this, ev);
			return ev;
		}

		CursorVisibility _desiredCursorVisibility = CursorVisibility.Default;

		/// <summary>
		/// Get / Set the wished cursor when the field is focused
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get => _desiredCursorVisibility;
			set {
				if ((_desiredCursorVisibility != value || _visibility != value) && HasFocus) {
					Application.Driver.SetCursorVisibility (value);
				}

				_desiredCursorVisibility = _visibility = value;
			}
		}

		/// <summary>
		/// Inserts the given <paramref name="toAdd"/> text at the current cursor position
		/// exactly as if the user had just typed it
		/// </summary>
		/// <param name="toAdd">Text to add</param>
		/// <param name="useOldCursorPos">If uses the <see cref="_oldCursorPos"/>.</param>
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
			_historyText.Clear (Text);
		}

		/// <summary>
		/// Returns <see langword="true"/> if the current cursor position is
		/// at the end of the <see cref="Text"/>. This includes when it is empty.
		/// </summary>
		/// <returns></returns>
		internal bool CursorIsAtEnd ()
		{
			return CursorPosition == Text.Length;
		}

		/// <summary>
		/// Returns <see langword="true"/> if the current cursor position is
		/// at the start of the <see cref="TextField"/>.
		/// </summary>
		/// <returns></returns>
		internal bool CursorIsAtStart ()
		{
			return CursorPosition <= 0;
		}
	}
	/// <summary>
	/// Renders an overlay on another view at a given point that allows selecting
	/// from a range of 'autocomplete' options.
	/// An implementation on a TextField.
	/// </summary>
	public class TextFieldAutocomplete : PopupAutocomplete {

		/// <inheritdoc/>
		protected override void DeleteTextBackwards ()
		{
			((TextField)HostControl).DeleteCharLeft (false);
		}

		/// <inheritdoc/>
		protected override void InsertText (string accepted)
		{
			((TextField)HostControl).InsertText (accepted, false);
		}
	}
}
