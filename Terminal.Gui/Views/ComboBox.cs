//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Provides a drop-down list of items the user can select from.
	/// </summary>
	public class ComboBox : View {

		private class ComboListView : ListView {
			internal ComboBox _container;

			public ComboListView (ComboBox container, bool hideDropdownListOnClick)
			{
				Initialize (container, hideDropdownListOnClick);
			}

			public ComboListView (ComboBox container, Rect rect, IList source, bool hideDropdownListOnClick) : base (rect, source)
			{
				Initialize (container, hideDropdownListOnClick);
			}

			public ComboListView (ComboBox container, IList source, bool hideDropdownListOnClick) : base (source)
			{
				Initialize (container, hideDropdownListOnClick);
			}

			private void Initialize (ComboBox container, bool hideDropdownListOnClick)
			{
				this._container = container ?? throw new ArgumentNullException ("ComboBox container cannot be null.", nameof (container));
				HideDropdownListOnClick = hideDropdownListOnClick;
				WantMousePositionReports = true;
				CanFocus = false;
			}

			public bool HideDropdownListOnClick { get; set; }

			public int Highlighted { get; set; } = -1;

			public override bool MouseEvent (MouseEvent me)
			{
				if (me.View == _container) {
					var newXY = new MouseEvent {
						X = me.X,
						Y = 0,
						Flags = me.Flags,
						View = me.View
					};
					if (_container.MouseEvent (newXY)) {
						return me.View != _container._search;
					}
				}

				var res = false;
				var isListViewPositionValid = IsListViewPositionValid (me);

				if (isListViewPositionValid) {
					if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1Released) {
						return true;
					}
					res = base.MouseEvent (me);
				}

				if (HideDropdownListOnClick && !isListViewPositionValid && me.View != _container && me.View != _container._search
					&& me.Flags != MouseFlags.ReportMousePosition && me.Flags != MouseFlags.WheeledDown
					&& me.Flags != MouseFlags.WheeledLeft && me.Flags != MouseFlags.WheeledRight && me.Flags != MouseFlags.WheeledUp) {

					_container._isShow = false;
					_container.HideList ();
					Application.RootKeyEvent = null;
					return false;
				} else if (HideDropdownListOnClick && isListViewPositionValid && me.Flags == MouseFlags.Button1Clicked) {
					OnOpenSelectedItem ();
					return true;
				} else if (me.Flags == MouseFlags.ReportMousePosition) {
					if (isListViewPositionValid) {
						Highlighted = Math.Min (TopItem + me.Y, Source.Count - 1);
						SetNeedsDisplay ();
					}
					return true;
				}

				return res;
			}

			private bool IsListViewPositionValid (MouseEvent me)
			{
				return me.X >= 0 && me.X < Frame.Width && me.Y >= 0 && me.Y < Frame.Height;
			}

			public override bool ProcessKey (KeyEvent kb)
			{
				if (kb.Key == Key.Esc || (_container._autoHide && kb.Key == Key.F4)) {
					return _container.ProcessKey (kb);
				}
				return base.ProcessKey (kb);
			}

			public override bool MoveUp ()
			{
				if (!_container.ReadOnly && HasFocus && SelectedItem == 0 && _container._searchset?.Count > 0) {
					_container._search.CursorPosition = _container._search.Text.EnumerateRunes ().Count ();
					_container._search.SetFocus ();
					return true;
				}
				return base.MoveUp ();
			}

			public override void OnDrawContent (Rect contentArea)
			{
				var currentColor = ColorScheme.Focus;
				Driver.SetAttribute (currentColor);
				Move (0, 0);
				var f = Frame;
				var item = TopItem;
				int col = AllowsMarking ? 2 : 0;
				int start = LeftItem;

				for (int row = 0; row < f.Height; row++, item++) {
					bool isSelected = item == _container.GetOpenSelectItem (item);
					if (Highlighted > Source?.Count - 1) {
						Highlighted = Source.Count - 1;
					}
					bool isHighlighted = item == Highlighted;

					Attribute newcolor;
					if (isHighlighted) {
						//newcolor = new Attribute (ColorScheme.Focus.Background, ColorScheme.Focus.Foreground);
						newcolor = new Attribute (ColorScheme.Normal.Background, ColorScheme.Normal.Foreground);
					} else if (isSelected) {
						//newcolor = ColorScheme.HotFocus;
						newcolor = ColorScheme.HotFocus;
					} else {
						//newcolor = GetFocusColor ();
						newcolor = new Attribute (ColorScheme.Normal.Foreground, ColorScheme.HotFocus.Foreground);
					}

					if (newcolor != currentColor) {
						Driver.SetAttribute (newcolor);
						currentColor = newcolor;
					}

					Move (0, row);
					if (Source == null || item >= Source.Count) {
						for (int c = 0; c < f.Width; c++)
							Driver.AddRune ((Rune)' ');
					} else {
						var rowEventArgs = new ListViewRowEventArgs (item);
						OnRowRender (rowEventArgs);
						if (rowEventArgs.RowAttribute != null && currentColor != rowEventArgs.RowAttribute) {
							currentColor = (Attribute)rowEventArgs.RowAttribute;
							Driver.SetAttribute (currentColor);
						}
						if (AllowsMarking) {
							Driver.AddRune (Source.IsMarked (item) ? (AllowsMultipleSelection ? CM.Glyphs.Checked : CM.Glyphs.Selected) : (AllowsMultipleSelection ? CM.Glyphs.UnChecked : CM.Glyphs.UnSelected));
							Driver.AddRune ((Rune)' ');
						}
						Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
					}
				}
			}

			public override bool OnMouseEnter (MouseEvent mouseEvent)
			{
				if (Application.MouseGrabView != this) {
					Application.GrabMouse (this);
				}

				return base.OnMouseEnter (mouseEvent);
			}

			//public override bool OnEnter (View view)
			//{
			//	ProcessEnter ();

			//	return base.OnEnter (view);
			//}

			//private void ProcessEnter ()
			//{
			//	Highlighted = SelectedItem;
			//	Application.GrabMouse (this);
			//	_container._prevFocused = this;
			//}

			//public override bool OnLeave (View view)
			//{
			//	Highlighted = _container.SelectedItem;
			//	if (_container.IsShow) {
			//		_container._isShow = false;
			//		_container.HideList ();
			//	}
			//	if (Application.MouseGrabView == this) {
			//		Application.UngrabMouse ();
			//	}

			//	return base.OnLeave (view);
			//}

			public override bool OnSelectedChanged ()
			{
				var res = base.OnSelectedChanged ();

				Highlighted = SelectedItem;

				return res;
			}
		}

		IListDataSource _source;
		/// <summary>
		/// Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ComboBox"/>, enabling custom rendering.
		/// </summary>
		/// <value>The source.</value>
		/// <remarks>
		///  Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.
		/// </remarks>
		public IListDataSource Source {
			get => _source;
			set {
				_source = value;

				// Only need to refresh list if its been added to a container view
				//if (SuperView?.Subviews.Contains (this) == true) {
				if (_selectedItem > -1 && _source != null && _selectedItem < _source.Count) {
					SetValue (_source.ToList () [_selectedItem].ToString ());
				} else {
					SelectedItem = -1;
					SetValue ("");
					//Search_Changed (this, new TextChangedEventArgs (""));
					//SetNeedsDisplay ();
				}
				_lastSelectedItem = _selectedItem;
			}
		}

		/// <summary>
		/// Sets the source of the <see cref="ComboBox"/> to an <see cref="IList"/>.
		/// </summary>
		/// <value>An object implementing the IList interface.</value>
		/// <remarks>
		///  Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custome rendering.
		/// </remarks>
		public void SetSource (IList source)
		{
			if (source == null) {
				Source = null;
			} else {
				if (_listView != null) {
					_listView.SetSource (source);
					Source = _listView.Source;
				} else {
					Source = new ListWrapper (source);
				}
			}
		}

		/// <summary>
		/// This event is raised when the selected item in the <see cref="ComboBox"/> has changed.
		/// </summary>
		public event EventHandler<ListViewItemEventArgs> SelectedItemChanged;

		/// <summary>
		/// This event is raised when the drop-down list is expanded.
		/// </summary>
		public event EventHandler Expanded;

		/// <summary>
		/// This event is raised when the drop-down list is collapsed.
		/// </summary>
		public event EventHandler Collapsed;

		/// <summary>
		/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
		/// </summary>
		public event EventHandler<ListViewItemEventArgs> OpenSelectedItem;

		/// <summary>
		/// This event is raised when the text has changed.
		/// </summary>
		public event EventHandler<TextChangedEventArgs> TextChanged;

		readonly IList _searchset = new List<object> ();
		string _text = "";
		readonly TextField _search;
		ComboListView _listView;
		bool _autoHide = true;
		int _minimumHeight = 1;

		string _savedText = "";

		/// <summary>
		/// Public constructor
		/// </summary>
		public ComboBox () : this (string.Empty)
		{
		}

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="text"></param>
		public ComboBox (string text) : base ()
		{
			_search = new TextField ("");

			Initialize ();
			Text = text;
		}

		/// <summary>
		/// Public constructor
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="source"></param>
		public ComboBox (Rect rect, IList source) : base (rect)
		{
			_search = new TextField ("") { Width = rect.Width };

			Initialize ();
			SetSource (source);
		}

		/// <summary>
		/// Initialize with the source.
		/// </summary>
		/// <param name="source">The source.</param>
		public ComboBox (IList source)
		{
			_search = new TextField ("");

			Initialize ();
			SetSource (source);
		}

		private void Initialize ()
		{
			_search.Width = Dim.Fill (1);
			if (Bounds.Height < _search.Frame.Height && (Height == null || Height is Dim.DimAbsolute)) {
				Height = 1;
			}

			_search.TextChanged += Search_Changed;
			_search.KeyPress += Search_KeyPress;
			_search.Enter += (s, e) => _prevFocused = _search;

			this.Add (_search);

			// On resize
			LayoutComplete += (object sender, LayoutEventArgs a) => {
				//if (_autoHide && Bounds.Width > 0 && _search.Frame.Width != Bounds.Width - 1) {
				//	_search.Width = _listview.Width = _autoHide ? Bounds.Width - 1 : Bounds.Width;
				//	_listview.Height = CalculatetHeight ();
				//	_search.SetRelativeLayout (Bounds);
				//	_listview.SetRelativeLayout (Bounds);
				//}
				if (_autoHide && Bounds.Width > 0 && _search.Frame.Width != Bounds.Width - 1) {
					_search.Width = Dim.Fill (1);
				}
				if (_autoHide && Bounds.Height > 1) {
					Height = 1;
				}
			};

			Added += (s, e) => {

				// Determine if this view is hosted inside a dialog and is the only control
				for (View view = this.SuperView; view != null; view = view.SuperView) {
					if (view is Dialog && SuperView?.Subviews.Count == 1 && SuperView.Subviews [0] == this) {
						_autoHide = false;
						_listView = new ComboListView (this, HideDropdownListOnClick) { LayoutStyle = LayoutStyle.Computed, ColorScheme = Colors.Base };

						_listView.OpenSelectedItem += (object sender, ListViewItemEventArgs a) => Selected ();
						_listView.SelectedItemChanged += (object sender, ListViewItemEventArgs a) => SelectedChanged ();

						_listView.Y = Pos.Bottom (_search);
						this.Width = Dim.Fill ();
						this.Height = Dim.Fill ();
						_search.Width = Dim.Fill ();
						_listView.Width = Dim.Fill ();
						_listView.Height = Dim.Fill ();
						this.Add (_listView);
						Search_Changed (this, new TextChangedEventArgs (Text));
						break;
					}
				}

				SetNeedsLayout ();
				SetNeedsDisplay ();
			};

			// Things this view knows how to do
			AddCommand (Command.Accept, () => ActivateSelected ());
			AddCommand (Command.ToggleExpandCollapse, () => ExpandCollapse ());
			AddCommand (Command.Expand, () => Expand ());
			AddCommand (Command.Collapse, () => Collapse ());
			AddCommand (Command.LineDown, () => MoveDown ());
			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.PageDown, () => PageDown ());
			AddCommand (Command.PageUp, () => PageUp ());
			AddCommand (Command.TopHome, () => MoveHome ());
			AddCommand (Command.BottomEnd, () => MoveEnd ());
			AddCommand (Command.Cancel, () => CancelSelected ());
			AddCommand (Command.UnixEmulation, () => UnixEmulation ());

			// Default keybindings for this view
			AddKeyBinding (Key.Enter, Command.Accept);
			AddKeyBinding (Key.F4, Command.ToggleExpandCollapse);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.Esc, Command.Cancel);
			AddKeyBinding (Key.U | Key.CtrlMask, Command.UnixEmulation);
		}

		private bool _isShow;
		private int _selectedItem = -1;
		private int _lastSelectedItem = -1;
		private bool _hideDropdownListOnClick = true;

		/// <summary>
		/// Gets the index of the currently selected item in the <see cref="Source"/>
		/// </summary>
		/// <value>The selected item or -1 none selected.</value>
		public int SelectedItem {
			get => _selectedItem;
			set {
				if (_selectedItem != value || (_text != _search.Text && value == -1)
				    || (_source != null && value > -1 && value < _source.Count)) {

					if (value == -1 && _selectedItem > -1 && IsValidSelection ()) {
						_savedText = "";
					}
					_selectedItem = value;
					if (_selectedItem != -1 && _source != null) {
						SetValue (_source.ToList () [_selectedItem].ToString ());
					} else if (ReadOnly) {
						SetValue ("");
					} else {
						//SetValue (_search.Text);
						SetValue (_savedText);
					}
					OnSelectedChanged ();
				}
			}
		}

		/// <summary>
		/// Gets the drop down list state, expanded or collapsed.
		/// </summary>
		public bool IsShow => _isShow;

		///<inheritdoc/>
		public new ColorScheme ColorScheme {
			get {
				return base.ColorScheme;
			}
			set {
				_listView.ColorScheme = value;
				base.ColorScheme = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		///If set to true its not allow any changes in the text.
		/// </summary>
		public bool ReadOnly {
			get => _search.ReadOnly;
			set {
				_search.ReadOnly = value;
			}
		}

		/// <summary>
		/// Gets or sets if the drop-down list can be hide with a button click event.
		/// </summary>
		public bool HideDropdownListOnClick {
			get => _hideDropdownListOnClick;
			set {
				_hideDropdownListOnClick = value;
				if (_listView != null) {
					_listView.HideDropdownListOnClick = _hideDropdownListOnClick;
				}
			}
		}

		public int DropDownHeight { get; set; } = 8;

		public int DropDownWidth { get; set; }

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!_search.HasFocus) {
				_search.SetFocus ();
			}
			if (me.X == Bounds.Right - 1 && me.Y == Bounds.Top && me.Flags == MouseFlags.Button1Pressed
			    && _autoHide) {

				if (_isShow) {
					_isShow = false;
					HideList ();
				} else {
					SetSearchSet ();

					_isShow = true;
					ShowList ();
					FocusSelectedItem ();
				}

				return true;
			}
			//else if (me.View == _search && me.Flags != MouseFlags.ReportMousePosition) {
			//	if (!_search.HasFocus) {
			//		_search.SetFocus ();
			//	}

			//	return true;
			//}

			return base.MouseEvent (me);
		}

		private void FocusSelectedItem ()
		{
			//_listview.SelectedItem = Math.Max (GetOpenSelectItem (SelectedItem), 0);
			_listView.SelectedItem = GetOpenSelectItem (SelectedItem);
			//_listview.SetFocus ();
			OnExpanded ();
		}

		private int GetOpenSelectItem (int item)
		{
			if (item > -1 && _lastSelectedItem > -1 && Source?.Count > 0 && Source?.Count > item
				&& _searchset?.Count > 0 && _searchset?.Count > item) {

				var openItem = Source.ToList () [_lastSelectedItem].ToString ();
				var searchsetItem = _searchset [item].ToString ();
				if (openItem == searchsetItem) {
					return item;
				}
			}
			return -1;
		}

		/// <summary>
		/// Virtual method which invokes the <see cref="Expanded"/> event.
		/// </summary>
		public virtual void OnExpanded ()
		{
			Expanded?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Virtual method which invokes the <see cref="Collapsed"/> event.
		/// </summary>
		public virtual void OnCollapsed ()
		{
			Collapsed?.Invoke (this, EventArgs.Empty);
		}

		View _prevFocused;

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (_listView == null || !_autoHide && !_search.HasFocus && _prevFocused == _listView) {
				_search.SetFocus ();
			} else if (!_autoHide && _listView != null && _prevFocused == _search) {
				_listView.SetFocus ();
			}

			if (ReadOnly) {
				_search.CursorPosition = 0;
			} else {
				_search.SelectAll ();
			}

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			//if (_autoHide && view != _listview && IsDirty ()) {
			//	ValidateSelection ();
			//} else if (_source?.Count > 0 && view != _listview && _lastSelectedItem > -1) {
			//	SetValue (_source.ToList () [_lastSelectedItem].ToString ());
			//}
			//if (_autoHide && _isShow && view != this && view != _search && view != _listview) {
			//	_isShow = false;
			//	HideList ();
			//}
			if (_isShow) {
				HideList ();
				_isShow = false;
			}

			return base.OnLeave (view);
		}

		/// <summary>
		/// Invokes the SelectedChanged event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnSelectedChanged ()
		{
			//if (_selectedItem != _lastSelectedItem || _text != _savedText) {
			SelectedItemChanged?.Invoke (this, new ListViewItemEventArgs (SelectedItem, _search.Text));
			if (!IsInitialized) {
				_lastSelectedItem = _selectedItem;
			}
			return true;
			//}

			//return false;
		}

		/// <summary>
		/// Invokes the OnOpenSelectedItem event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnOpenSelectedItem ()
		{
			//var value = _search.Text;
			var value = _savedText = _text = _search.Text;
			_lastSelectedItem = _selectedItem = GetSelectedItemFromSource (value);
			if (_selectedItem > -1) {
				value = _savedText = _text = _search.Text = _source.ToList () [_selectedItem].ToString ();
			}
			OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (SelectedItem, value));

			return true;
		}

		/// <summary>
		/// Virtual method that invoke the <see cref="TextChanged"/> event if it's subscribed.
		/// </summary>
		/// <param name="oldValue">The old text replaced.</param>
		public virtual void OnTextChanged (string oldValue)
		{
			TextChanged?.Invoke (this, new TextChangedEventArgs (oldValue));
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			base.OnDrawContent (contentArea);

			if (!_autoHide) {
				return;
			}

			Driver.SetAttribute (ColorScheme.Focus);
			Move (Bounds.Right - 1, 0);
			Driver.AddRune (CM.Glyphs.DownArrow);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent ke)
		{
			var result = InvokeKeybindings (ke);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (ke);
		}

		bool UnixEmulation ()
		{
			// Unix emulation
			Reset ();
			return true;
		}

		bool CancelSelected ()
		{
			if (!IsDirty ()) {
				if (!_search.HasFocus) {
					_search.SetFocus ();
				}
				Collapse ();
				return false;
			}
			if (!ReadOnly && _search.Text != _savedText) {
				SetValue (_savedText);
				_search.SelectAll ();
			}
			SelectedItem = _lastSelectedItem;
			if (ReadOnly && HideDropdownListOnClick && SelectedItem > -1) {
				SetSearchSet ();
				_search.Text = _text = Source.ToList () [_lastSelectedItem].ToString ();
				_search.CursorPosition = 0;
			}
			if (!_search.HasFocus) {
				_search.SetFocus ();
			}
			Collapse ();
			return true;
		}

		bool IsDirty ()
		{
			//return _isShow && _search.Text != _savedText || (_lastSelectedItem >= -1 && !string.IsNullOrEmpty (_search.Text)
			//	&& _selectedItem != _lastSelectedItem);
			//return _isShow && _search.Text != _savedText;
			return _search.Text != _savedText;
		}

		bool? MoveEnd ()
		{
			if (!_isShow && _search.HasFocus) {
				return null;
			}
			if (HasItems ()) {
				_listView?.MoveEnd ();
			}
			return true;
		}

		bool? MoveHome ()
		{
			if (!_isShow && _search.HasFocus) {
				return null;
			}
			if (HasItems ()) {
				_listView?.MoveHome ();
			}
			return true;
		}

		bool PageUp ()
		{
			if (HasItems ()) {
				_listView?.MovePageUp ();
			}
			return true;
		}

		bool PageDown ()
		{
			if (HasItems ()) {
				_listView?.MovePageDown ();
			}
			return true;
		}

		bool? MoveUp ()
		{
			// stop odd behavior on KeyUp when search has focus
			if (_listView == null && _search.HasFocus) {
				return SuperView?.FocusPrev ();
			}
			// jump back to search
			if (_listView?.HasFocus == true && _listView?.SelectedItem == 0 && _searchset?.Count > 0) {
				_search.CursorPosition = _search.Text.EnumerateRunes ().Count ();
				_search.SetFocus ();
				return true;
				//} else if (_listview?.HasFocus == true) {
			} else if (_listView != null) {
				return _listView.MoveUp ();
			}
			return null;
		}

		bool? MoveDown ()
		{
			// jump to list
			if (_search.HasFocus) {
				if (_listView != null) {
					//if (_searchset?.Count > 0) {
					//	_listview.SetFocus ();
					//	if (_listview.SelectedItem == -1) {
					//		_listview.SelectedItem = 0;
					//	}

					//}
					return _listView.MoveDown ();
				} else {
					return SuperView?.FocusNext ();
				}
				//return true;
			} else if (_listView?.HasFocus == true) {
				return _listView.MoveDown ();
			}
			return null;
		}

		/// <summary>
		/// Toggles the expand/collapse state of the sublist in the combo box
		/// </summary>
		/// <returns></returns>
		bool ExpandCollapse ()
		{
			if (_search.HasFocus || _listView?.HasFocus == true) {
				if (!_isShow) {
					return Expand ();
				} else {
					return Collapse ();
				}
			}
			return false;
		}

		bool ActivateSelected ()
		{
			//if (!ReadOnly || HasItems ()) {
			//if (HasItems ()) {
			Selected ();
			return true;
			//}
			//return false;
		}

		bool HasItems ()
		{
			return Source?.Count > 0;
		}

		/// <summary>
		/// Collapses the drop down list.  Returns true if the state chagned or false
		/// if it was already collapsed and no action was taken
		/// </summary>
		public virtual bool Collapse ()
		{
			if (!_isShow) {
				return false;
			}

			HideList ();
			_isShow = false;
			return true;
		}

		/// <summary>
		/// Expands the drop down list.  Returns true if the state chagned or false
		/// if it was already expanded and no action was taken
		/// </summary>
		public virtual bool Expand ()
		{
			if (_isShow) {
				return false;
			}

			SetSearchSet ();
			_isShow = true;
			ShowList ();
			FocusSelectedItem ();

			return true;
		}

		/// <summary>
		/// The currently selected list item
		/// </summary>
		public new string Text {
			get {
				return _text;
			}
			set {
				var srcItem = GetSelectedItemFromSource (value);
				if (srcItem > -1) {
					SelectedItem = srcItem;
					_savedText = _source.ToList () [_selectedItem].ToString ();
				} else {
					SelectedItem = -1;
					_savedText = value;
					SetValue (value);
				}
			}
		}

		private void SetValue (object text)
		{
			_search.TextChanged -= Search_Changed;
			var oldValue = _search.Text;
			this._text = _search.Text = text.ToString ();
			_search.CursorPosition = 0;
			OnTextChanged (oldValue);
			_search.TextChanged += Search_Changed;
		}

		//private void SetValue (object text, bool allText = true)
		//{
		//	_search.TextChanged -= Search_Changed;
		//	if (allText) {
		//		this._text = _search.Text = text.ToString ();
		//	} else {
		//		_search.Text = text.ToString ();
		//	}
		//	_search.CursorPosition = 0;
		//	_search.TextChanged += Search_Changed;
		//}

		private void Selected ()
		{
			if (_autoHide && !_isShow) {
				return;
			}

			if (ReadOnly && (_listView?.Source.Count == 0 || (_searchset?.Count ?? 0) == 0)) {
				_text = "";
				HideList ();
				_isShow = false;
				return;
			}
			//ValidateSelection ();
			Reset (keepSearchText: true);
			if (_autoHide) {
				HideList ();
				_isShow = false;
			} else {
				OnOpenSelectedItem ();
			}
		}

		private void ValidateSelection ()
		{
			//if (!IsValidSelection (out string text)) {
			if (!IsValidSelection ()) {
				if (ReadOnly) {
					SelectedItem = _lastSelectedItem;
				} else {
					SelectedItem = _lastSelectedItem = -1;
				}
			} else if (_listView.SelectedItem > -1) {
				SetValue (_searchset [_listView.SelectedItem]);
			} else {
				SetValue (_savedText);
			}
			var itemFromSource = GetSelectedItemFromSource (this._text);
			if (SelectedItem != itemFromSource) {
				SelectedItem = itemFromSource;
			}
			OnOpenSelectedItem ();
		}

		private bool IsValidSelection ()
		{
			if (_source?.Count > 0 && _selectedItem > -1 && !string.IsNullOrEmpty (_savedText)) {
				var srcList = _source.ToList ();
				var scrText = srcList [_selectedItem].ToString ();
				if (string.Equals (scrText, _savedText.ToString (), StringComparison.InvariantCultureIgnoreCase)) {
					return true;
				}
			}
			return false;
		}

		//private bool IsValidSelection ()
		//{
		//	if (_source?.Count > 0 && _searchset?.Count > 0 && !string.IsNullOrEmpty (_search.Text) && _listview.SelectedItem > -1) {
		//		var srcList = _source.ToList ();
		//		var srcItem = GetSelectedItemFromSource (_searchset [_listview.SelectedItem].ToString ());
		//		if (srcItem == -1) {
		//			return false;
		//		}
		//		var scrText = srcList [srcItem].ToString ();
		//		if (string.Equals (scrText, _search.Text.ToString (), StringComparison.InvariantCultureIgnoreCase)) {
		//			return true;
		//		}
		//	}
		//	return false;
		//}

		//private bool IsValidSelection (out string text)
		//{
		//	text = string.Empty;
		//	if (_source?.Count > 0 && _searchset?.Count > 0 && (!string.IsNullOrEmpty (_search.Text) || _listview.SelectedItem > -1)) {
		//		var srcList = _source.ToList ();
		//		text = _listview.SelectedItem > -1 ? _searchset [_listview.SelectedItem].ToString () : _search.Text;
		//		var srcItem = GetSelectedItemFromSource (text);
		//		if (srcItem == -1) {
		//			return false;
		//		}
		//		var scrText = srcList [srcItem].ToString ();
		//		if (string.Equals (scrText, _search.Text.ToString (), StringComparison.InvariantCultureIgnoreCase)) {
		//			text = scrText;
		//			return true;
		//		}
		//	}
		//	return false;
		//}

		private void SelectedChanged ()
		{
			if (_searchset?.Count > 0) {
				//SetValue (_searchset [_listview.SelectedItem], false);
				SetValue (_searchset [_listView.SelectedItem]);
				_search.CursorPosition = _search.Text.EnumerateRunes ().Count ();
				_selectedItem = GetSelectedItemFromSource (_search.Text);
			}
		}

		private int GetSelectedItemFromSource (string value)
		{
			if (_source == null) {
				return -1;
			}
			var sourceList = _source.ToList ();
			for (int i = 0; i < _source.Count; i++) {
				if (string.Equals (sourceList [i].ToString (), value.ToString (), StringComparison.InvariantCultureIgnoreCase)) {
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Reset to full original list
		/// </summary>
		private void Reset (bool keepSearchText = false)
		{
			if (!keepSearchText) {
				_search.Text = _text = "";
			}

			ResetSearchSet ();

			if (_listView != null) {
				_listView.SetSource (_searchset);
				if (_autoHide) {
					_listView.Height = CalculatetHeight ();
				}
			}

			if (HasFocus && Subviews.Count > 0 && !_search.HasFocus) {
				_search.SetFocus ();
			}
		}

		private void ResetSearchSet (bool noCopy = false)
		{
			_searchset.Clear ();

			if (_autoHide || noCopy)
				return;
			SetSearchSet ();
		}

		private void SetSearchSet ()
		{
			if (Source == null) { return; }
			// force deep copy
			foreach (var item in Source.ToList ()) {
				_searchset.Add (item);
			}
		}

		private void Search_KeyPress (object sender, KeyEventEventArgs e)
		{
			if (e.KeyEvent.Key == Key.Esc) {
				e.Handled = ProcessKey (e.KeyEvent);
			} else if (ReadOnly && !e.KeyEvent.IsCtrl && !e.KeyEvent.IsAlt && char.IsLetter ((char)e.KeyEvent.KeyValue)) {
				if (!_isShow) {
					_searchset.Clear ();
					SetSearchSet ();
					_isShow = true;
					ShowList ();
				}
				_listView.ProcessKey (e.KeyEvent);
			}
		}

		private void Search_Changed (object sender, TextChangedEventArgs e)
		{
			if (_source == null) {
				return;
			}

			if (string.IsNullOrEmpty (_search.Text) && string.IsNullOrEmpty (e.OldValue)) {
				_isShow = true;
				ResetSearchSet ();
			} else if (_search.Text != e.OldValue) {
				PopulateSearchset (_search.Text.ToString ());
			}

			ShowList ();
			//if (_searchset?.Count > 0) {
			//	ShowList ();
			//} else if (_isShow) {
			//	HideList ();
			//	_isShow = false;
			//}
			//if (_autoHide && _listview != null) {
			//	Application.RootMouseEvent += Application_RootMouseEvent;
			//}
		}

		private void PopulateSearchset (string searchText)
		{
			_isShow = true;
			ResetSearchSet (noCopy: true);

			// Iterate to preserver object type and force deep copy
			foreach (var item in _source.ToList ()) {
				if (item.ToString ().StartsWith (searchText, StringComparison.CurrentCultureIgnoreCase)) {
					_searchset.Add (item);
				}
			}
		}

		/// <summary>
		/// Show the search list
		/// </summary>
		/// 
		/// Consider making public
		private void ShowList ()
		{
			if (_autoHide) {
				// Close first any other open ComboListView
				if (_listView == null) {
					ComboBox prevListViewConatiner;
					try {
						prevListViewConatiner = ((ComboListView)Application.Top.Subviews.First (v => v is ComboListView))?._container;
					} catch (Exception) {
						prevListViewConatiner = null;
					}
					if (prevListViewConatiner != null) {
						prevListViewConatiner.HideList ();
						prevListViewConatiner._isShow = false;
					}

					System.Diagnostics.Debug.Assert (Application.Top.Subviews.Sum (x => x is ComboListView comboList && comboList._container == this ? 1 : 0) == 0);
					_listView = new ComboListView (this, HideDropdownListOnClick) { LayoutStyle = LayoutStyle.Computed };
					if (SuperView != null) {
						_listView.ColorScheme = SuperView.ColorScheme;
					}

					_listView.OpenSelectedItem += (object sender, ListViewItemEventArgs a) => Selected ();
					_listView.SelectedItemChanged += (object sender, ListViewItemEventArgs a) => SelectedChanged ();

					var viewToScreen = ViewToScreen (Application.Top.Frame);
					_listView.X = viewToScreen.X;
					_listView.Y = viewToScreen.Y + 1;
					_listView.Width = Math.Max (DropDownWidth == 0 ? Bounds.Width - 1 : DropDownWidth, 0);
					_listView.SetSource (_searchset);
					//_listview.SelectedItem = _selectedItem;
					_listView.Height = CalculatetHeight ();

					Application.GrabMouse (_listView);

					//Application.IterationComplete += Application_IterationComplete;
					Application.Current.DrawContentComplete += Current_DrawContentComplete;
					Application.RootMouseEvent += Application_RootMouseEvent;
					Application.GrabbedMouse += Application_GrabbedMouse;
					Application.UnGrabbingMouse += Application_UnGrabbingMouse;
					Application.Top.Add (_listView);
				} else {
					// listview it's open and only need be resized
					_listView.Height = CalculatetHeight ();
				}
				//Application.Top.BringSubviewToFront (_listView);
			} else {
				// Ensure list shrinks as you type
				_listView.Clear ();
				_listView.SetSource (_searchset);
				_listView.SetNeedsDisplay ();
			}
		}

		private void Current_DrawContentComplete (object sender, DrawEventArgs e)
		{
			var viewToScreen = ViewToScreen (Application.Top.Frame);
			var listX = viewToScreen.X;
			var listY = viewToScreen.Y + 1;
			var listWidth = CalculateWidth ();
			var listHeight = CalculatetHeight ();
			if (_autoHide && _listView != null) {
				if (listX != _listView.Frame.X || listY != _listView.Frame.Y
					|| listWidth != _listView.Frame.Width || listHeight != _listView.Frame.Height) {

					_listView.X = listX;
					_listView.Y = listY;
					_listView.Width = listWidth;
					_listView.Height = listHeight;
					//_search.SetRelativeLayout (Bounds);
					//_listview.SetRelativeLayout (Bounds);
					//_listView.SetRelativeLayout (Application.Top.Bounds);
				}
				//Application.Top.SetNeedsDisplay ();
				//_listView.SetNeedsDisplay ();
				//Application.Top.Redraw (Application.Top.Bounds);
				var savedClip = Driver.Clip;
				Driver.Clip = _listView.Frame;
				_listView.Draw ();
				//Driver.UpdateScreen ();
				//_search.PositionCursor ();
				Driver.Clip = savedClip;
			}
		}

		private void Application_RootMouseEvent (MouseEvent me)
		{
			if (_listView != null) {
				//var screenToView = _listview.ScreenToView (me.X, me.Y);
				var view = View.FindDeepestView (_listView, me.X, me.Y, out int rx, out int ry);
				if (view == _listView) {
					var nme = new MouseEvent () {
						X = rx,
						Y = ry,
						Flags = me.Flags,
						View = view
					};
					if (_listView.MouseEvent (nme)) {
						me.Handled = true;
					}
				}
			}
		}

		private void Application_GrabbedMouse (object sender, ViewEventArgs e)
		{
			if (_autoHide && _listView != null && _listView != e.View) {
				HideList ();
				_isShow = false;
			}
		}

		private void Application_UnGrabbingMouse (object sender, GrabMouseEventArgs e)
		{
			if (_autoHide && _listView != null && _listView != e.View) {

			}
		}

		//private void Application_IterationComplete ()
		//{
		//	var viewToScreen = ViewToScreen (Application.Top.Frame);
		//	var listX = viewToScreen.X;
		//	var listY = viewToScreen.Y + 1;
		//	var listWidth = CalculateWidth ();
		//	var listHeight = CalculatetHeight ();
		//	if (_autoHide && _listView != null) {
		//		if (listX != _listView.Frame.X || listY != _listView.Frame.Y
		//			|| listWidth != _listView.Frame.Width || listHeight != _listView.Frame.Height) {

		//			_listView.X = listX;
		//			_listView.Y = listY;
		//			_listView.Width = listWidth;
		//			_listView.Height = listHeight;
		//			//_search.SetRelativeLayout (Bounds);
		//			//_listview.SetRelativeLayout (Bounds);
		//			//_listView.SetRelativeLayout (Application.Top.Bounds);
		//		}
		//		//Application.Top.SetNeedsDisplay ();
		//		//_listView.SetNeedsDisplay ();
		//		//Application.Top.Redraw (Application.Top.Bounds);
		//		//var savedClip = Driver.Clip;
		//		//Driver.Clip = _listView.Frame;
		//		_listView.Redraw (_listView.Bounds);
		//		Driver.UpdateScreen ();
		//		_search.PositionCursor ();
		//		//Driver.Clip = savedClip;
		//	}
		//}

		/// <summary>
		/// Hide the search list
		/// </summary>
		/// 
		/// Consider making public
		private void HideList ()
		{
			//if (_autoHide && IsDirty () && IsValidSelection (out string text)) {
			//if (_autoHide && IsDirty () && IsValidSelection () && _text != _savedText) {
			//	_text = _search.Text = _savedText;
			//	_selectedItem = _lastSelectedItem;
			//	_search.SelectAll ();
			//	OnOpenSelectedItem ();
			//}
			if (_autoHide && IsDirty () && _search.Text != _savedText) {
				OnOpenSelectedItem ();
			}
			if (!_autoHide) {
				Reset (_lastSelectedItem > -1);
				var rect = _listView.ViewToScreen (_listView.Bounds);
				_listView.Clear (rect);
				SuperView?.SetNeedsDisplay (rect);
			}
			if (_autoHide && _listView != null) {
				System.Diagnostics.Debug.Assert (Application.Top.Subviews.Sum (x => x is ComboListView comboList && comboList._container == this ? 1 : 0) == 1);
				Application.RootMouseEvent -= Application_RootMouseEvent;
				//Application.IterationComplete -= Application_IterationComplete;
				Application.Current.DrawContentComplete -= Current_DrawContentComplete;
				Application.GrabbedMouse -= Application_GrabbedMouse;
				Application.UnGrabbingMouse -= Application_UnGrabbingMouse;
				_listView.OpenSelectedItem -= (object sender, ListViewItemEventArgs a) => Selected ();
				_listView.SelectedItemChanged -= (object sender, ListViewItemEventArgs a) => SelectedChanged ();
				Application.Top.Remove (_listView);
				if (Application.MouseGrabView == _listView) {
					Application.UngrabMouse ();
				}
				_listView.Dispose ();
				_listView = null;
				Reset (!ReadOnly || _lastSelectedItem > -1);
			}
			OnCollapsed ();
		}

		/// <summary>
		/// Internal height of dynamic search list
		/// </summary>
		/// <returns></returns>
		private int CalculatetHeight ()
		{
			if (Bounds.Height == 0)
				return 0;

			return Math.Min (DropDownHeight, _searchset?.Count > 0 ? _searchset.Count : _isShow ? _minimumHeight : 0);
		}

		private int CalculateWidth ()
		{
			if (Bounds.Width == 0)
				return 0;

			return DropDownWidth == 0 ? Bounds.Width - 1 : DropDownWidth;
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			if (_listView != null && Application.MouseGrabView == _listView) {
				Application.UngrabMouse ();
			}
			base.Dispose (disposing);
		}
	}
}
