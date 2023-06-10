using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time.
	/// </summary>
	public class RadioGroup : View {
		int selected = -1;
		int cursor;
		DisplayModeLayout displayMode;
		int horizontalSpace = 2;
		List<(int pos, int length)> horizontal;

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public RadioGroup () : this (radioLabels: new string [] { }) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The index of the item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (string [] radioLabels, int selected = 0) : base ()
		{
			SetInitalProperties (Rect.Empty, radioLabels, selected);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="rect">Boundaries for the radio group.</param>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The index of item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (Rect rect, string [] radioLabels, int selected = 0) : base (rect)
		{
			SetInitalProperties (rect, radioLabels, selected);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// The <see cref="View"/> frame is computed from the provided radio labels.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
		/// <param name="selected">The item to be selected, the value is clamped to the number of items.</param>
		public RadioGroup (int x, int y, string [] radioLabels, int selected = 0) :
			this (MakeRect (x, y, radioLabels != null ? radioLabels.ToList () : null), radioLabels, selected)
		{ }

		void SetInitalProperties (Rect rect, string [] radioLabels, int selected)
		{
			if (radioLabels == null) {
				this.radioLabels = new List<string> ();
			} else {
				this.radioLabels = radioLabels.ToList ();
			}

			this.selected = selected;
			Frame = rect;
			CanFocus = true;
			HotKeySpecifier = new Rune ('_');

			// Things this view knows how to do
			AddCommand (Command.LineUp, () => { MoveUp (); return true; });
			AddCommand (Command.LineDown, () => { MoveDown (); return true; });
			AddCommand (Command.TopHome, () => { MoveHome (); return true; });
			AddCommand (Command.BottomEnd, () => { MoveEnd (); return true; });
			AddCommand (Command.Accept, () => { SelectItem (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.Space, Command.Accept);

			LayoutStarted += RadioGroup_LayoutStarted;
		}

		private void RadioGroup_LayoutStarted (object sender, EventArgs e)
		{
			SetWidthHeight (radioLabels);
		}

		/// <summary>
		/// Gets or sets the <see cref="DisplayModeLayout"/> for this <see cref="RadioGroup"/>.
		/// </summary>
		public DisplayModeLayout DisplayMode {
			get { return displayMode; }
			set {
				if (displayMode != value) {
					displayMode = value;
					SetWidthHeight (radioLabels);
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Gets or sets the horizontal space for this <see cref="RadioGroup"/> if the <see cref="DisplayMode"/> is <see cref="DisplayModeLayout.Horizontal"/>
		/// </summary>
		public int HorizontalSpace {
			get { return horizontalSpace; }
			set {
				if (horizontalSpace != value && displayMode == DisplayModeLayout.Horizontal) {
					horizontalSpace = value;
					SetWidthHeight (radioLabels);
					UpdateTextFormatterText ();
					SetNeedsDisplay ();
				}
			}
		}

		void SetWidthHeight (List<string> radioLabels)
		{
			switch (displayMode) {
			case DisplayModeLayout.Vertical:
				var r = MakeRect (0, 0, radioLabels);
				Bounds = new Rect (Bounds.Location, new Size (r.Width, radioLabels.Count));
				break;

			case DisplayModeLayout.Horizontal:
				CalculateHorizontalPositions ();
				var length = 0;
				foreach (var item in horizontal) {
					length += item.length;
				}
				var hr = new Rect (0, 0, length, 1);
				if (IsAdded && LayoutStyle == LayoutStyle.Computed) {
					Width = hr.Width;
					Height = 1;
				} else {
					Bounds = new Rect (Bounds.Location, new Size (hr.Width, radioLabels.Count));
				}
				break;
			}
		}

		static Rect MakeRect (int x, int y, List<string> radioLabels)
		{
			if (radioLabels == null) {
				return new Rect (x, y, 0, 0);
			}

			int width = 0;

			foreach (var s in radioLabels) {
				width = Math.Max (s.GetColumns () + 2, width);
			}
			return new Rect (x, y, width, radioLabels.Count);
		}

		List<string> radioLabels = new List<string> ();

		/// <summary>
		/// The radio labels to display
		/// </summary>
		/// <value>The radio labels.</value>
		public string [] RadioLabels {
			get => radioLabels.ToArray ();
			set {
				var prevCount = radioLabels.Count;
				radioLabels = value.ToList ();
				if (prevCount != radioLabels.Count) {
					SetWidthHeight (radioLabels);
				}
				SelectedItem = 0;
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		private void CalculateHorizontalPositions ()
		{
			if (displayMode == DisplayModeLayout.Horizontal) {
				horizontal = new List<(int pos, int length)> ();
				int start = 0;
				int length = 0;
				for (int i = 0; i < radioLabels.Count; i++) {
					start += length;
					length = radioLabels [i].GetColumns () + 2 + (i < radioLabels.Count - 1 ? horizontalSpace : 0);
					horizontal.Add ((start, length));
				}
			}
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			base.OnDrawContent (contentArea);

			Driver.SetAttribute (GetNormalColor ());
			for (int i = 0; i < radioLabels.Count; i++) {
				switch (DisplayMode) {
				case DisplayModeLayout.Vertical:
					Move (0, i);
					break;
				case DisplayModeLayout.Horizontal:
					Move (horizontal [i].pos, 0);
					break;
				}
				var rl = radioLabels [i];
				Driver.SetAttribute (GetNormalColor ());
				Driver.AddStr ($"{(i == selected ? CM.Glyphs.Selected : CM.Glyphs.UnSelected)} ");
				TextFormatter.FindHotKey (rl, HotKeySpecifier, true, out int hotPos, out Key hotKey);
				if (hotPos != -1 && (hotKey != Key.Null || hotKey != Key.Unknown)) {
					var rlRunes = rl.ToRunes ();
					for (int j = 0; j < rlRunes.Length; j++) {
						Rune rune = rlRunes [j];
						if (j == hotPos && i == cursor) {
							Application.Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : GetHotNormalColor ());
						} else if (j == hotPos && i != cursor) {
							Application.Driver.SetAttribute (GetHotNormalColor ());
						} else if (HasFocus && i == cursor) {
							Application.Driver.SetAttribute (ColorScheme.Focus);
						}
						if (rune == HotKeySpecifier && j + 1 < rlRunes.Length) {
							j++;
							rune = rlRunes [j];
							if (i == cursor) {
								Application.Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : GetHotNormalColor ());
							} else if (i != cursor) {
								Application.Driver.SetAttribute (GetHotNormalColor ());
							}
						}
						Application.Driver.AddRune (rune);
						Driver.SetAttribute (GetNormalColor ());
					}
				} else {
					DrawHotString (rl, HasFocus && i == cursor, ColorScheme);
				}
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			switch (DisplayMode) {
			case DisplayModeLayout.Vertical:
				Move (0, cursor);
				break;
			case DisplayModeLayout.Horizontal:
				Move (horizontal [cursor].pos, 0);
				break;
			}
		}

		/// <summary>
		/// Invoked when the selected radio label has changed.
		/// </summary>
		public event EventHandler<SelectedItemChangedArgs> SelectedItemChanged;

		/// <summary>
		/// The currently selected item from the list of radio labels
		/// </summary>
		/// <value>The selected.</value>
		public int SelectedItem {
			get => selected;
			set {
				OnSelectedItemChanged (value, SelectedItem);
				cursor = selected;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Allow to invoke the <see cref="SelectedItemChanged"/> after their creation.
		/// </summary>
		public void Refresh ()
		{
			OnSelectedItemChanged (selected, -1);
		}

		/// <summary>
		/// Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.
		/// </summary>
		/// <param name="selectedItem"></param>
		/// <param name="previousSelectedItem"></param>
		public virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem)
		{
			selected = selectedItem;
			SelectedItemChanged?.Invoke (this, new SelectedItemChangedArgs (selectedItem, previousSelectedItem));
		}

		///<inheritdoc/>
		public override bool ProcessColdKey (KeyEvent kb)
		{
			var key = kb.KeyValue;
			if (key < Char.MaxValue && Char.IsLetterOrDigit ((char)key)) {
				int i = 0;
				key = Char.ToUpper ((char)key);
				foreach (var l in radioLabels) {
					bool nextIsHot = false;
					TextFormatter.FindHotKey (l, HotKeySpecifier, true, out _, out Key hotKey);
					foreach (Rune c in l) {
						if (c == HotKeySpecifier) {
							nextIsHot = true;
						} else {
							if ((nextIsHot && Rune.ToUpperInvariant (c).Value == key) || (key == (uint)hotKey)) {
								SelectedItem = i;
								cursor = i;
								if (!HasFocus)
									SetFocus ();
								return true;
							}
							nextIsHot = false;
						}
					}
					i++;
				}
			}
			return false;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (kb);
		}

		void SelectItem ()
		{
			SelectedItem = cursor;
		}

		void MoveEnd ()
		{
			cursor = Math.Max (radioLabels.Count - 1, 0);
		}

		void MoveHome ()
		{
			cursor = 0;
		}

		void MoveDown ()
		{
			if (cursor + 1 < radioLabels.Count) {
				cursor++;
				SetNeedsDisplay ();
			} else if (cursor > 0) {
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		void MoveUp ()
		{
			if (cursor > 0) {
				cursor--;
				SetNeedsDisplay ();
			} else if (radioLabels.Count - 1 > 0) {
				cursor = radioLabels.Count - 1;
				SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked)) {
				return false;
			}
			if (!CanFocus) {
				return false;
			}
			SetFocus ();

			int boundsX = me.X - GetFramesThickness ().Left;
			int boundsY = me.Y - GetFramesThickness ().Top;

			var pos = displayMode == DisplayModeLayout.Horizontal ? boundsX : boundsY;
			var rCount = displayMode == DisplayModeLayout.Horizontal ? horizontal.Last ().pos + horizontal.Last ().length : radioLabels.Count;

			if (pos < rCount) {
				var c = displayMode == DisplayModeLayout.Horizontal ? horizontal.FindIndex ((x) => x.pos <= boundsX && x.pos + x.length - 2 >= boundsX) : boundsY;
				if (c > -1) {
					cursor = SelectedItem = c;
					SetNeedsDisplay ();
				}
			}
			return true;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}

	/// <summary>
	/// Used for choose the display mode of this <see cref="RadioGroup"/>
	/// </summary>
	public enum DisplayModeLayout {
		/// <summary>
		/// Vertical mode display. It's the default.
		/// </summary>
		Vertical,
		/// <summary>
		/// Horizontal mode display.
		/// </summary>
		Horizontal
	}
}
