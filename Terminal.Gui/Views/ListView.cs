using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Implement <see cref="IListDataSource"/> to provide custom rendering for a <see cref="ListView"/>.
	/// </summary>
	public interface IListDataSource {
		/// <summary>
		/// Returns the number of elements to display
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Returns the maximum length of elements to display
		/// </summary>
		int Length { get; }

		void PreRender (ListView container, int width);

		/// <summary>
		/// This method is invoked to render a specified item, the method should cover the entire provided width.
		/// </summary>
		/// <returns>The render.</returns>
		/// <param name="container">The list view to render.</param>
		/// <param name="driver">The console driver to render.</param>
		/// <param name="selected">Describes whether the item being rendered is currently selected by the user.</param>
		/// <param name="item">The index of the item to render, zero for the first item and so on.</param>
		/// <param name="col">The column where the rendering will start</param>
		/// <param name="line">The line where the rendering will be done.</param>
		/// <param name="width">The width that must be filled out.</param>
		/// <param name="start">The index of the string to be displayed.</param>
		/// <remarks>
		///   The default color will be set before this method is invoked, and will be based on whether the item is selected or not.
		/// </remarks>
		void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0);

		/// <summary>
		/// Should return whether the specified item is currently marked.
		/// </summary>
		/// <returns><see langword="true"/>, if marked, <see langword="false"/> otherwise.</returns>
		/// <param name="item">Item index.</param>
		bool IsMarked (int item);

		/// <summary>
		/// Flags the item as marked.
		/// </summary>
		/// <param name="item">Item index.</param>
		/// <param name="value">If set to <see langword="true"/> value.</param>
		void SetMark (int item, bool value);

		/// <summary>
		/// Return the source as IList.
		/// </summary>
		/// <returns></returns>
		IList ToList ();
	}

	/// <summary>
	/// ListView <see cref="View"/> renders a scrollable list of data where each item can be activated to perform an action.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The <see cref="ListView"/> displays lists of data and allows the user to scroll through the data.
	///   Items in the can be activated firing an event (with the ENTER key or a mouse double-click). 
	///   If the <see cref="AllowsMarking"/> property is true, elements of the list can be marked by the user.
	/// </para>
	/// <para>
	///   By default <see cref="ListView"/> uses <see cref="object.ToString"/> to render the items of any
	///   <see cref="IList"/> object (e.g. arrays, <see cref="List{T}"/>,
	///   and other collections). Alternatively, an object that implements <see cref="IListDataSource"/>
	///   can be provided giving full control of what is rendered.
	/// </para>
	/// <para>
	///   <see cref="ListView"/> can display any object that implements the <see cref="IList"/> interface.
	///   <see cref="string"/> values are converted into <see cref="string"/> values before rendering, and other values are
	///   converted into <see cref="string"/> by calling <see cref="object.ToString"/> and then converting to <see cref="string"/> .
	/// </para>
	/// <para>
	///   To change the contents of the ListView, set the <see cref="Source"/> property (when 
	///   providing custom rendering via <see cref="IListDataSource"/>) or call <see cref="SetSource"/>
	///   an <see cref="IList"/> is being used.
	/// </para>
	/// <para>
	///   When <see cref="AllowsMarking"/> is set to true the rendering will prefix the rendered items with
	///   [x] or [ ] and bind the SPACE key to toggle the selection. To implement a different
	///   marking style set <see cref="AllowsMarking"/> to false and implement custom rendering.
	/// </para>
	/// <para>
	///   Searching the ListView with the keyboard is supported. Users type the
	///   first characters of an item, and the first item that starts with what the user types will be selected.
	/// </para>
	/// <para>
	///   By default ListViews render items that are one line tall. To change, this set the
	///   ItemHeight property to a larger value. This will allow cells to be rendered with additional
	///   lines. This is typically used when you have a custom renderer that can render cells
	///   larger that one line.
	/// </para>
	/// </remarks>
	public class ListView : View {
		int _top, _left;
		int _selected = -1;
		//int bottomOffset, rightOffset;
		ScrollBarView.ScrollBarType _scrollBarType;
		ScrollBarView _scrollBar;

		public class ColumnHeader {
			public string HeaderName { get; set; }

			public TextAlignment HeaderAlignment { get; set; } = TextAlignment.Centered;

			public int Width { get; set; }

			public string MemberName { get; set; }

			public Type MemberType { get; set; } = typeof (string);

			public TextAlignment MemberAlignment { get; set; } = TextAlignment.Left;

			public string MemberFormat { get; set; }
		}

		IListDataSource _source;
		/// <summary>
		/// Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ListView"/>, enabling custom rendering.
		/// </summary>
		/// <value>The source.</value>
		/// <remarks>
		///  Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.
		/// </remarks>
		public IListDataSource Source {
			get => _source;
			set {
				_source = value;
				KeystrokeNavigator.Collection = _source?.ToList ();
				_top = 0;
				_selected = -1;
				_lastSelectedItem = -1;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Sets the source of the <see cref="ListView"/> to an <see cref="IList"/>.
		/// </summary>
		/// <value>An object implementing the IList interface.</value>
		/// <remarks>
		///  Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custome rendering.
		/// </remarks>
		public void SetSource (IList source)
		{
			if (source == null && (Source == null || !(Source is ListWrapper)))
				Source = null;
			else {
				Source = MakeWrapper (source);
			}
		}

		/// <summary>
		/// Sets the source to an <see cref="IList"/> value asynchronously.
		/// </summary>
		/// <value>An item implementing the IList interface.</value>
		/// <remarks>
		///  Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom rendering.
		/// </remarks>
		public Task SetSourceAsync (IList source)
		{
			return Task.Factory.StartNew (() => {
				if (source == null && (Source == null || !(Source is ListWrapper)))
					Source = null;
				else
					Source = MakeWrapper (source);
				return source;
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		bool _allowsMarking;
		/// <summary>
		/// Gets or sets whether this <see cref="ListView"/> allows items to be marked.
		/// </summary>
		/// <value>Set to <see langword="true"/> to allow marking elements of the list.</value>
		/// <remarks>
		/// If set to <see langword="true"/>, <see cref="ListView"/> will render items marked items with "[x]", and unmarked items with "[ ]"
		/// spaces. SPACE key will toggle marking. The default is <see langword="false"/>.
		/// </remarks>
		public bool AllowsMarking {
			get => _allowsMarking;
			set {
				_allowsMarking = value;
				if (_allowsMarking) {
					AddKeyBinding (Key.Space, Command.ToggleChecked);
				} else {
					ClearKeyBinding (Key.Space);
				}

				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// If set to <see langword="true"/> more than one item can be selected. If <see langword="false"/> selecting
		/// an item will cause all others to be un-selected. The default is <see langword="false"/>.
		/// </summary>
		public bool AllowsMultipleSelection {
			get => _allowsMultipleSelection;
			set {
				_allowsMultipleSelection = value;
				if (Source != null && !_allowsMultipleSelection) {
					// Clear all selections except selected 
					for (int i = 0; i < Source.Count; i++) {
						if (Source.IsMarked (i) && i != _selected) {
							Source.SetMark (i, false);
						}
					}
				}
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the item that is displayed at the top of the <see cref="ListView"/>.
		/// </summary>
		/// <value>The top item.</value>
		public int TopItem {
			get => _top;
			set {
				if (_source == null)
					return;

				if (value < 0 || (_source.Count > 0 && value >= _source.Count))
					throw new ArgumentException ("value");
				_top = Math.Max (value, 0);
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the leftmost column that is currently visible (when scrolling horizontally).
		/// </summary>
		/// <value>The left position.</value>
		public int LeftItem {
			get => _left;
			set {
				if (_source == null)
					return;

				if (value < 0 || (Maxlength > 0 && value >= Maxlength))
					throw new ArgumentException ("value");
				_left = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets the widest item in the list.
		/// </summary>
		public int Maxlength => (_source?.Length) ?? 0;

		/// <summary>
		/// Gets or sets the index of the currently selected item.
		/// </summary>
		/// <value>The selected item.</value>
		public int SelectedItem {
			get => _selected;
			set {
				if (_source == null || _source.Count == 0) {
					return;
				}
				if (value < -1 || value >= _source.Count) {
					throw new ArgumentException ("value");
				}
				_selected = value;
				OnSelectedChanged ();
			}
		}

		/// <summary>
		/// The bottom offset needed to use a horizontal scrollbar or for another reason.
		/// This is only needed with the keyboard navigation.
		/// </summary>
		//public int BottomOffset {
		//	get => bottomOffset;
		//	set {
		//		if (selected == Source.Count - 1 && bottomOffset > 0 && value == 0) {
		//			top = Math.Max (top - bottomOffset, 0);
		//		}
		//		bottomOffset = value;
		//		SetNeedsDisplay ();
		//	}
		//}

		/// <summary>
		/// The right offset needed to use a vertical scrollbar or for another reason.
		/// This is only needed with the keyboard navigation.
		/// </summary>
		//public int RightOffset {
		//	get => rightOffset;
		//	set {
		//		if (left == Maxlength && rightOffset > 0 && value == 0) {
		//			left = Math.Max (left - rightOffset, 0);
		//		}
		//		rightOffset = value;
		//		SetNeedsDisplay ();
		//	}
		//}

		/// <summary>
		/// Get the <see cref="ScrollBarView"/> used by this view or <see langword="null"/> if none are used.
		/// </summary>
		public ScrollBarView ScrollBar { get => _scrollBar; }

		/// <summary>
		/// Gets or sets the <see cref="ScrollBarView.ScrollBarType"/> used by this view.
		/// </summary>
		public ScrollBarView.ScrollBarType ScrollBarType {
			get => _scrollBarType;
			set {
				if (_scrollBarType != value) {
					_scrollBarType = value;
					DisposeScrollBar ();
					if (_scrollBarType != ScrollBarView.ScrollBarType.None) {
						if (!IsAdded) {
							Added += ListView_Added;
						} else {
							CreateScrollBar ();
						}
					}
					SetNeedsDisplay ();
				}
			}
		}

		private void ListView_Added (object sender, SuperViewChangedEventArgs e)
		{
			CreateScrollBar ();
		}

		private void CreateScrollBar ()
		{
			_scrollBar = new ScrollBarView (this, _scrollBarType, false);
			AddHandlers ();
		}

		private void AddHandlers ()
		{
			if (_scrollBar != null) {
				LayoutComplete += ListView_LayoutComplete;
				_scrollBar.ChangedPosition += ScrollBar_ChangedPosition;
				//scrollBar.VisibleChanged += ScrollBar_VisibleChanged;
				//if (scrollBar.IsVertical) {
				//	BottomOffset = 1;
				//} else {
				//	RightOffset = 1;
				//}
				if (_scrollBar.OtherScrollBarView != null) {
					_scrollBar.OtherScrollBarView.ChangedPosition += OtherScrollBarView_ChangedPosition;
					//scrollBar.OtherScrollBarView.VisibleChanged += OtherScrollBarView_VisibleChanged;
					//if (scrollBar.OtherScrollBarView.IsVertical) {
					//	BottomOffset = 1;
					//} else {
					//	RightOffset = 1;
					//}
				}
			}
		}

		private void ListView_LayoutComplete (object sender, LayoutEventArgs e)
		{
			if (_scrollBar != null) {
				_scrollBar.Refresh ();
			}
		}

		private void DisposeScrollBar ()
		{
			if (_scrollBar != null) {
				_scrollBar.ChangedPosition -= ScrollBar_ChangedPosition;
				//scrollBar.VisibleChanged -= ScrollBar_VisibleChanged;
				if (_scrollBar.OtherScrollBarView != null) {
					_scrollBar.OtherScrollBarView.ChangedPosition -= OtherScrollBarView_ChangedPosition;
					//scrollBar.OtherScrollBarView.VisibleChanged -= OtherScrollBarView_VisibleChanged;
				}
				_scrollBar.RemoveAll ();
				_scrollBar = null;
				//BottomOffset = 0;
				//RightOffset = 0;
			}
		}

		private void ScrollBar_ChangedPosition (object sender, EventArgs e)
		{
			TopItem = _scrollBar.Position;
			if (TopItem != _scrollBar.Position) {
				_scrollBar.Position = TopItem;
			}
			SetNeedsDisplay ();
		}

		private void OtherScrollBarView_ChangedPosition (object sender, EventArgs e)
		{
			LeftItem = _scrollBar.OtherScrollBarView.Position;
			if (LeftItem != _scrollBar.OtherScrollBarView.Position) {
				_scrollBar.OtherScrollBarView.Position = LeftItem;
			}
			SetNeedsDisplay ();
		}

		//private void ScrollBar_VisibleChanged ()
		//{
		//	if (scrollBar.Visible && RightOffset == 0) {
		//		RightOffset = 1;
		//	} else if (!scrollBar.Visible && RightOffset == 1) {
		//		RightOffset = 0;
		//	}
		//}

		//private void OtherScrollBarView_VisibleChanged ()
		//{
		//	if (scrollBar.OtherScrollBarView.Visible && BottomOffset == 0) {
		//		BottomOffset = 1;
		//	} else if (!scrollBar.OtherScrollBarView.Visible && BottomOffset == 1) {
		//		BottomOffset = 0;
		//	}
		//}

		static IListDataSource MakeWrapper (IList source)
		{
			return new ListWrapper (source);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListView"/> that will display the 
		/// contents of the object implementing the <see cref="IList"/> interface, 
		/// with relative positioning.
		/// </summary>
		/// <param name="source">An <see cref="IList"/> data source, if the elements are strings or ustrings, 
		/// the string is rendered, otherwise the ToString() method is invoked on the result.</param>
		public ListView (IList source) : this (MakeWrapper (source))
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListView"/> that will display the provided data source, using relative positioning.
		/// </summary>
		/// <param name="source"><see cref="IListDataSource"/> object that provides a mechanism to render the data. 
		/// The number of elements on the collection should not change, if you must change, set 
		/// the "Source" property to reset the internal settings of the ListView.</param>
		public ListView (IListDataSource source) : base ()
		{
			this._source = source;
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListView"/>. Set the <see cref="Source"/> property to display something.
		/// </summary>
		public ListView () : base ()
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListView"/> that will display the contents of the object implementing the <see cref="IList"/> interface with an absolute position.
		/// </summary>
		/// <param name="rect">Frame for the listview.</param>
		/// <param name="source">An IList data source, if the elements of the IList are strings or ustrings, 
		/// the string is rendered, otherwise the ToString() method is invoked on the result.</param>
		public ListView (Rect rect, IList source) : this (rect, MakeWrapper (source))
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ListView"/> with the provided data source and an absolute position
		/// </summary>
		/// <param name="rect">Frame for the listview.</param>
		/// <param name="source">IListDataSource object that provides a mechanism to render the data. 
		/// The number of elements on the collection should not change, if you must change, 
		/// set the "Source" property to reset the internal settings of the ListView.</param>
		public ListView (Rect rect, IListDataSource source) : base (rect)
		{
			this._source = source;
			Initialize ();
		}

		void Initialize ()
		{
			Source = _source;
			CanFocus = true;

			// Things this view knows how to do
			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
			AddCommand (Command.ScrollUp, () => ScrollUp (1));
			AddCommand (Command.ScrollDown, () => ScrollDown (1));
			AddCommand (Command.ScrollLeft, () => ScrollLeft (1));
			AddCommand (Command.ScrollRight, () => ScrollRight (1));
			AddCommand (Command.PageUp, () => MovePageUp ());
			AddCommand (Command.PageDown, () => MovePageDown ());
			AddCommand (Command.TopHome, () => MoveHome ());
			AddCommand (Command.BottomEnd, () => MoveEnd ());
			AddCommand (Command.OpenSelectedItem, () => OnOpenSelectedItem ());
			AddCommand (Command.ToggleChecked, () => MarkUnmarkRow ());
			AddCommand (Command.ScrollRight, () => ScrollRight (1));
			AddCommand (Command.ScrollLeft, () => ScrollLeft (1));

			// Default keybindings for all ListViews
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.P | Key.CtrlMask, Command.LineUp);

			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.N | Key.CtrlMask, Command.LineDown);

			AddKeyBinding (Key.PageUp, Command.PageUp);

			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.V | Key.CtrlMask, Command.PageDown);

			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.Enter, Command.OpenSelectedItem);
			AddKeyBinding (Key.Space, Command.ToggleChecked);
			AddKeyBinding (Key.ShiftMask | Key.CursorRight, Command.ScrollRight);
			AddKeyBinding (Key.ShiftMask | Key.CursorLeft, Command.ScrollLeft);
		}

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			if (_scrollBar != null) {
				_scrollBar.Size = Source.Count - 1;
				_scrollBar.Position = TopItem;
				_scrollBar.Selected = SelectedItem;
				_scrollBar.OtherScrollBarView.Size = Maxlength - 1;
				_scrollBar.OtherScrollBarView.Position = LeftItem;
				_scrollBar.Refresh ();
			}

			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Bounds;
			var item = _top;
			bool focused = HasFocus;
			int col = _allowsMarking ? 2 : 0;
			int row = _showColumnHeaders ? 1 : 0;
			int start = _left;

			if (_showColumnHeaders) {
				current = new Attribute (ColorScheme.Normal.Background, ColorScheme.HotNormal.Foreground);
				Driver.SetAttribute (current);
				if (_source != null) {
					Source.Render (this, Driver, false, item, col, 0, f.Width - col, start);
				}
			}
			for (; row < f.Height; row += _itemHeight, item++) {
				bool isSelected = item == _selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : GetNormalColor ())
					       : (isSelected ? ColorScheme.HotNormal : GetNormalColor ());

				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (_source == null || item >= _source.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune ((Rune)' ');
				} else {
					var rowEventArgs = new ListViewRowEventArgs (item);
					OnRowRender (rowEventArgs);
					if (rowEventArgs.RowAttribute != null && current != rowEventArgs.RowAttribute) {
						current = (Attribute)rowEventArgs.RowAttribute;
						Driver.SetAttribute (current);
					}
					if (_allowsMarking) {
						Driver.AddRune (_source.IsMarked (item) ? (AllowsMultipleSelection ? CM.Glyphs.Checked : CM.Glyphs.Selected) :
						    (AllowsMultipleSelection ? CM.Glyphs.UnChecked : CM.Glyphs.UnSelected));
						Driver.AddRune ((Rune)' ');
					}
					Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
				}
			}
		}

		private int _itemHeight = 1;

		/// <summary>
		/// The number of lines occupied by a an item in the ListView, this defaults to one,
		/// but can be change to a larger value for items that need to use more than one cell,
		/// typically used with a custom IListDataSource that implements a renderer for multiple
		/// lines of content.
		/// </summary>
		public int ItemHeight {
			get {
				return _itemHeight;
			}
			set {
				if (value < 1)
					throw new ArgumentOutOfRangeException ("Must be larger than one");
				_itemHeight = value;
			}
		}

		/// <summary>
		/// The number of visible items that can occupied in the ListView height.
		/// </summary>
		public int VisibleItems {
			get {
				return (Frame.Height - (_showColumnHeaders ? 1 : 0)) / _itemHeight;
			}
		}

		private bool _showColumnHeaders;

		public bool ShowColumnHeaders {
			get { return _showColumnHeaders; }
			set {
				_showColumnHeaders = value;
				SetNeedsDisplay ();
			}
		}

		public int ColumnMargin { get; set; } = 2;

		public TextAlignment HeadersAlignment { get; set; } = TextAlignment.Left;

		public TextAlignment ItemsAlignment { get; set; } = TextAlignment.Left;

		public List<ColumnHeader> ColumnHeaders { get; set; }

		/// <summary>
		/// This event is raised when the selected item in the <see cref="ListView"/> has changed.
		/// </summary>
		public event EventHandler<ListViewItemEventArgs> SelectedItemChanged;

		/// <summary>
		/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
		/// </summary>
		public event EventHandler<ListViewItemEventArgs> OpenSelectedItem;

		/// <summary>
		/// This event is invoked when this <see cref="ListView"/> is being drawn before rendering.
		/// </summary>
		public event EventHandler<ListViewRowEventArgs> RowRender;

		/// <summary>
		/// Gets the <see cref="CollectionNavigator"/> that searches the <see cref="ListView.Source"/> collection as
		/// the user types.
		/// </summary>
		public CollectionNavigator KeystrokeNavigator { get; private set; } = new CollectionNavigator ();

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (_source == null) {
				return base.ProcessKey (kb);
			}

			var result = InvokeKeybindings (kb);
			if (result != null) {
				return (bool)result;
			}

			// Enable user to find & select an item by typing text
			if (CollectionNavigator.IsCompatibleKey (kb)) {
				var newItem = KeystrokeNavigator?.GetNextMatchingItem (SelectedItem, (char)kb.KeyValue);
				if (newItem is int && newItem != -1) {
					SelectedItem = (int)newItem;
					EnsureSelectedItemVisible ();
					SetNeedsDisplay ();
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// If <see cref="AllowsMarking"/> and <see cref="AllowsMultipleSelection"/> are both <see langword="true"/>,
		/// unmarks all marked items other than the currently selected. 
		/// </summary>
		/// <returns><see langword="true"/> if unmarking was successful.</returns>
		public virtual bool AllowsAll ()
		{
			if (!_allowsMarking)
				return false;
			if (!AllowsMultipleSelection) {
				for (int i = 0; i < Source.Count; i++) {
					if (Source.IsMarked (i) && i != _selected) {
						Source.SetMark (i, false);
						return true;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Marks the <see cref="SelectedItem"/> if it is not already marked.
		/// </summary>
		/// <returns><see langword="true"/> if the <see cref="SelectedItem"/> was marked.</returns>
		public virtual bool MarkUnmarkRow ()
		{
			if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Changes the <see cref="SelectedItem"/> to the item at the top of the visible list.
		/// </summary>
		/// <returns></returns>
		public virtual bool MovePageUp ()
		{
			int n = _selected - VisibleItems;
			if (n < 0)
				n = 0;
			if (n != _selected) {
				_selected = n;
				_top = Math.Max (_selected, 0);
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Changes the <see cref="SelectedItem"/> to the item just below the bottom 
		/// of the visible list, scrolling if needed.
		/// </summary>
		/// <returns></returns>
		public virtual bool MovePageDown ()
		{
			var n = (_selected + VisibleItems);
			if (n >= _source.Count)
				n = _source.Count - 1;
			if (n != _selected) {
				_selected = n;
				if (_source.Count * _itemHeight >= _top + VisibleItems)
					_top = Math.Max (_selected, 0);
				else
					_top = 0;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Changes the <see cref="SelectedItem"/> to the next item in the list, 
		/// scrolling the list if needed.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveDown ()
		{
			if (_source.Count == 0) {
				// Do we set lastSelectedItem to -1 here?
				return false; //Nothing for us to move to
			}
			if (_selected >= _source.Count) {
				// If for some reason we are currently outside of the
				// valid values range, we should select the bottommost valid value.
				// This can occur if the backing data source changes.
				_selected = _source.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (_selected + 1 < _source.Count) { //can move by down by one.
				_selected++;

				if (_selected >= _top + VisibleItems) {
					_top++;
				} else if (_selected < _top) {
					_top = Math.Max (_selected, 0);
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (_selected == 0) {
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (_selected >= _top + VisibleItems) {
				_top = Math.Max (_source.Count - VisibleItems, 0);
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Changes the <see cref="SelectedItem"/> to the previous item in the list, 
		/// scrolling the list if needed.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveUp ()
		{
			if (_source.Count == 0) {
				// Do we set lastSelectedItem to -1 here?
				return false; //Nothing for us to move to
			}
			if (_selected >= _source.Count) {
				// If for some reason we are currently outside of the
				// valid values range, we should select the bottommost valid value.
				// This can occur if the backing data source changes.
				_selected = _source.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (_selected > 0) {
				_selected--;
				if (_selected > Source.Count) {
					_selected = Source.Count - 1;
				}
				if (_selected < _top) {
					_top = Math.Max (_selected, 0);
				} else if (_selected > _top + VisibleItems) {
					_top = Math.Max (_selected - VisibleItems + 1, 0);
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (_selected < _top) {
				_top = Math.Max (_selected, 0);
				SetNeedsDisplay ();
			}
			return true;
		}

		/// <summary>
		/// Changes the <see cref="SelectedItem"/> to last item in the list, 
		/// scrolling the list if needed.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveEnd ()
		{
			if (_source.Count > 0 && _selected != _source.Count - 1) {
				_selected = _source.Count - 1;
				if (_top + _selected > VisibleItems - 1) {
					_top = Math.Max (_selected, 0);
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Changes the <see cref="SelectedItem"/> to the first item in the list, 
		/// scrolling the list if needed.
		/// </summary>
		/// <returns></returns>
		public virtual bool MoveHome ()
		{
			if (_selected != 0) {
				_selected = 0;
				_top = Math.Max (_selected, 0);
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		/// <summary>
		/// Scrolls the view down by <paramref name="items"/> items.
		/// </summary>
		/// <param name="items">Number of items to scroll down.</param>
		public virtual bool ScrollDown (int items)
		{
			_top = Math.Max (Math.Min (_top + items * _itemHeight, _source.Count - 1), 0);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Scrolls the view up by <paramref name="items"/> items.
		/// </summary>
		/// <param name="items">Number of items to scroll up.</param>
		public virtual bool ScrollUp (int items)
		{
			_top = Math.Max (_top - items * _itemHeight, 0);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Scrolls the view right.
		/// </summary>
		/// <param name="cols">Number of columns to scroll right.</param>
		public virtual bool ScrollRight (int cols)
		{
			if (ScrollBar?.KeepContentAlwaysInViewport == true) {
				_left = Math.Max (Math.Max (cols, Maxlength - _left), 0);
			} else {
				_left = Math.Max (Math.Min (_left + cols, Maxlength - 1), 0);
			}
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Scrolls the view left.
		/// </summary>
		/// <param name="cols">Number of columns to scroll left.</param>
		public virtual bool ScrollLeft (int cols)
		{
			_left = Math.Max (_left - cols, 0);
			SetNeedsDisplay ();
			return true;
		}

		int _lastSelectedItem = -1;
		private bool _allowsMultipleSelection = true;

		/// <summary>
		/// Invokes the <see cref="SelectedItemChanged"/> event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnSelectedChanged ()
		{
			if (_selected != _lastSelectedItem) {
				var value = _source?.Count > 0 ? _source.ToList () [_selected] : null;
				SelectedItemChanged?.Invoke (this, new ListViewItemEventArgs (_selected, value));
				_lastSelectedItem = _selected;
				EnsureSelectedItemVisible ();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Invokes the <see cref="OpenSelectedItem"/> event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnOpenSelectedItem ()
		{
			if (_source.Count <= _selected || _selected < 0 || OpenSelectedItem == null) {
				return false;
			}

			var value = _source.ToList () [_selected];

			OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (_selected, value));

			return true;
		}

		/// <summary>
		/// Virtual method that will invoke the <see cref="RowRender"/>.
		/// </summary>
		/// <param name="rowEventArgs"></param>
		public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs)
		{
			RowRender?.Invoke (this, rowEventArgs);
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (IsInitialized) {
				Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);
			}
			if (_lastSelectedItem != _selected) {
				EnsureSelectedItemVisible ();
			}

			return base.OnEnter (view);
		}

		/// <summary>
		/// Ensures the selected item is always visible on the screen.
		/// </summary>
		public void EnsureSelectedItemVisible ()
		{
			if (SuperView?.IsInitialized == true) {
				if (_selected < _top) {
					_top = Math.Max (_selected, 0);
				} else if (Frame.Height > 0 && _selected >= _top + Frame.Height) {
					_top = Math.Max (_selected - VisibleItems + 1, 0);
				}
				LayoutStarted -= ListView_LayoutStarted;
			} else {
				LayoutStarted += ListView_LayoutStarted;
			}
		}

		private void ListView_LayoutStarted (object sender, LayoutEventArgs e)
		{
			EnsureSelectedItemVisible ();
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (_allowsMarking)
				Move (0, _selected * _itemHeight - _top);
			else
				Move (Bounds.Width - 1, _selected * _itemHeight - _top);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
			    me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
			    me.Flags != MouseFlags.WheeledRight && me.Flags != MouseFlags.WheeledLeft) {

				return false;
			}

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (_source == null) {
				return false;
			}

			if (me.Flags == MouseFlags.WheeledDown) {
				ScrollDown (1);
				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				ScrollUp (1);
				return true;
			} else if (me.Flags == MouseFlags.WheeledRight) {
				ScrollRight (1);
				return true;
			} else if (me.Flags == MouseFlags.WheeledLeft) {
				ScrollLeft (1);
				return true;
			}

			var meY = _showColumnHeaders ? me.Y - 1 : me.Y;
			var framesThickness = GetFramesThickness ();
			if (meY + _top - framesThickness.Top < 0
			    || meY + _top > _top + VisibleItems - (framesThickness.Top + framesThickness.Bottom)
			    || _top - framesThickness.Top + (meY / _itemHeight) >= _source.Count) {
				return true;
			}

			_selected = _top + meY / _itemHeight;
			if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}
			if (_selected == _top + VisibleItems) {
				_top++;
			}
			OnSelectedChanged ();
			SetNeedsDisplay ();
			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				OnOpenSelectedItem ();
			}

			return true;
		}

		//public override void OnDrawContent (Rect viewport)
		//{
		//	if (source != null) {
		//		source.PreRender (this, Frame.Width - (allowsMarking ? 2 : 0));
		//	}
		//	base.OnDrawContent (viewport);
		//}
	}

	/// <summary>
	/// Provides a default implementation of <see cref="IListDataSource"/> that renders
	/// <see cref="ListView"/> items using <see cref="object.ToString()"/>.
	/// </summary>
	public class ListWrapper : IListDataSource {
		IList _src;
		BitArray _marks;
		int _count, _len, _itemHeight, _width;
		private bool _rendered;
		private List<int> _maxLength, _colsStart, _colsWidth;
		private List<ListView.ColumnHeader> _columnHeaders;

		/// <inheritdoc/>
		public ListWrapper (IList source)
		{
			if (source != null) {
				_count = source.Count;
				_marks = new BitArray (_count);
				_src = source;
				_len = GetMaxLengthItem ();
			}
		}

		/// <inheritdoc/>
		public int Count => _src != null ? _src.Count : 0;

		/// <inheritdoc/>
		public int Length => _len;

		int GetMaxLengthItem (int colStart = 0, int width = 0, int colMargin = 0)
		{
			_colsStart = new List<int> { colStart };
			_colsWidth = new List<int> { width };

			if (_columnHeaders == null && (_src == null || _src?.Count == 0)) {
				return 0;
			}

			var colHeadersCount = Math.Max (_columnHeaders != null ? Math.Max (_columnHeaders.Count, 1) / _itemHeight : 1, 1);
			_maxLength = new List<int> ();
			var colHeadersWidth = 0;
			ListView.ColumnHeader ch = null;
			string us;

			for (int c = 0; c < colHeadersCount; c++) {
				_maxLength.Add (0);

				int colWidth = 0;

				if (_columnHeaders != null) {
					ch = _columnHeaders [c];

					if (c > 0) {
						_colsStart.Add (colStart + colHeadersWidth);
					}

					if (ch.Width == -1) {
						_colsStart [c] = -1;
						continue;
					}

					us = GetColumnName (_columnHeaders, c, ch);

					_maxLength [c] = us.GetColumns ();

					if (ch.Width == 0) {
						if (_src == null) {
							colWidth = Math.Min (width / colHeadersCount, width);
						} else if (c == colHeadersCount - 1) {
							colWidth = width - colHeadersWidth;
						} else {
							colWidth = us.GetColumns ();
						}
					} else {
						colWidth = Math.Min (ch.Width, width);
					}
				}

				for (int i = 0; i < _src?.Count; i++) {
					var t = _src [i];
					int l;
					List<string> lines;
					//if (t == null) {
					//	l = 0;
					//} else {
					//	if (t is string u) {
					//		l = u.ConsoleWidth;
					//	} else if (t is string s) {
					//		us = string.Make (s);
					//		lines = TextFormatter.SplitNewLine (us);
					//		l = lines [c].ConsoleWidth;
					//	} else {
					//		l = t.ToString ().Length;
					//	}
					//}

					if (t is string s) {
						us = s;
						lines = GetFormattedLines (TextFormatter.SplitNewLine (us), _columnHeaders, colHeadersCount, _itemHeight);
						//if (itemHeight > 0) {
						//	lines = TextFormatter.SplitNewLine (us);
						//	if (lines.Count > colHeadersCount) {
						//		us = string.Empty;
						//		for (int line = 0; line < lines.Count / itemHeight; line++) {
						//			var ustr = lines [line];
						//			if (ustr.ConsoleWidth > us.ConsoleWidth) {
						//				us = ustr;
						//			}
						//		}
						//	} else {
						//		us = lines [row];
						//	}
						//}
					} else {
						lines = GetCustomTypesLines (t, _columnHeaders, colHeadersCount, _itemHeight);

						//if (columnHeaders == null) {
						//	us = string.Make (t.ToString ());
						//	//lines = TextFormatter.SplitNewLine (us);
						//} else {
						//	//for (int c = 0; c < colHeadersCount; c++) {
						//	//for (int r = 0; r < itemHeight; r++) {
						//	var ch = columnHeaders [c];
						//	if (!string.IsNullOrEmpty (ch.MemberName)) {
						//		var type = ch.MemberType;
						//		var value = Convert.ChangeType (GetPropertyValue (t, ch.MemberName), type);
						//		if (!string.IsNullOrEmpty (ch.MemberFormat)) {
						//			value = string.Format (ch.MemberFormat, value);
						//		} else {
						//			value.ToString ();
						//		}
						//		us = string.Make (value.ToString ());
						//		//lines.Add (us);
						//	} else {
						//		us = string.Make (t.ToString ());
						//	}
						//	//}
						//	//}
						//}
					}

					us = lines [c];
					//l = lines [c].ConsoleWidth;
					l = us.GetColumns ();

					if (l > _maxLength [c]) {
						_maxLength [c] = l;
						//if (ch != null && ch.Width == 0 && c == colHeadersCount - 1) {
						//	maxLength [c] += colMargin;
						//}
					}
					//else if (colHeadersCount == 1 && maxLength [c] < width) {
					//	maxLength [c] = width;
					//}

					if (ch != null) {
						if (ch.Width == 0) {
							//if (maxLength [c] < colWidth) {
							//	maxLength [c] = colWidth;
							//	if (c < colHeadersCount - 1) {
							//		maxLength [c] += colMargin;
							//	}
							//	//maxLength [c] += colMargin;
							//} else {
							//	colWidth = maxLength [c];
							//}
							if (c < colHeadersCount - 1) {
								colWidth = _maxLength [c];
							}
						} else {
							colWidth = Math.Min (ch.Width, width);
						}
					}
				}

				if (c < colHeadersCount - 1) {
					_maxLength [c] += colStart + colMargin;
					colWidth += colMargin;
				}

				if (ch != null) {
					if (c == 0) {
						_colsWidth [0] = colWidth;
					} else if (c == colHeadersCount - 1) {
						if (ch.Width == 0) {
							if (width - colHeadersWidth > _maxLength [c]) {
								colWidth = width - colHeadersWidth;
							} else {
								colWidth = _maxLength [c];
							}
						}
						_colsWidth.Add (colWidth);
					} else {
						_colsWidth.Add (colWidth);
					}

					colHeadersWidth += colWidth;
				}
			}

			return _maxLength.Sum ();
		}

		public void PreRender (ListView container, int width)
		{
			var itemHeight = container.ItemHeight;
			var colHeaders = container.ColumnHeaders;
			var colStart = container.AllowsMarking ? 2 : 0;
			var colMargin = container.ColumnMargin;

			if (!_rendered || colHeaders != _columnHeaders || itemHeight != this._itemHeight || colStart != _colsStart [0] || width != this._width) {
				_columnHeaders = colHeaders;
				this._itemHeight = itemHeight;
				this._width = width;
				_len = GetMaxLengthItem (colStart, width, colMargin);
				_rendered = true;
			}
		}

		void RenderUstr (ConsoleDriver driver, string ustr, int col, int line, int width, int start = 0)
		{
			var u = TextFormatter.ClipAndJustify (ustr, width, TextAlignment.Left);
			driver.AddStr (u);
			width -= u.GetColumns ();
			while (width-- > 0) {
				driver.AddRune ((Rune)' ');
			}
		}

		private static void RenderUstr (ListView container, ConsoleDriver driver, string ustr, int colStart, int line, int width, int start, ListView.ColumnHeader colHeader)
		{
			int colWidth = width;
			var align = container.ShowColumnHeaders && line == 0 ? container.HeadersAlignment : container.ItemsAlignment;
			if (colHeader == null) {
				RenderUstr (container, driver, ustr, colStart, line, width, start, align);
			} else if (container.ShowColumnHeaders && line == 0) {
				colWidth = CalculateWidth (container, width, colHeader, colHeader.HeaderAlignment);
				RenderUstr (container, driver, ustr, colStart, line, colWidth, start, colHeader.HeaderAlignment);
			} else {
				colWidth = CalculateWidth (container, width, colHeader, colHeader.MemberAlignment);
				RenderUstr (container, driver, ustr, colStart, line, colWidth, start, colHeader.MemberAlignment);
			}
			if (width != colWidth) {
				var us = new String (' ', container.ColumnMargin);
				RenderUstr (container, driver, us, colStart + colWidth, line, container.ColumnMargin, start, colHeader.MemberAlignment);
			}
		}

		private static int CalculateWidth (ListView container, int width, ListView.ColumnHeader colHeader, TextAlignment align)
		{
			//switch (align) {
			//case TextAlignment.Left:
			//case TextAlignment.Centered:
			//	return width;
			//case TextAlignment.Right:
			//case TextAlignment.Justified:
			//	var lastColHeader = container.ColumnHeaders.Last ();
			//	if (colHeader != lastColHeader) {
			//		return width;
			//	} else {
			//		return width - 1;
			//	}
			//}
			return width;
		}

		private static void RenderUstr (ListView container, ConsoleDriver driver, string ustr, int colStart, int line, int width, int start, TextAlignment align)
		{
			var tf = new TextFormatter ();
			tf.Text = ustr;
			tf.Size = new Size (Math.Max (width + start, 0), 1);
			tf.Alignment = align;
			var rect = container.ViewToScreen (new Rect (colStart - start, line, width, 1));
			//var rectContainer = container.ViewToScreen (new Rect (colStart, line, width, 1));
			var rectContainer = container.ViewToScreen (container.Bounds);
			tf.Draw (rect, driver.GetAttribute (), driver.GetAttribute (), rectContainer, true);
		}

		/// <inheritdoc/>
		public void Render (ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width, int start = 0)
		{
			var height = container.ItemHeight;
			//container.Move (col, line);
			var t = _src? [item];
			string us;
			List<string> lines = new List<string> ();
			var colHeaders = container.ColumnHeaders;
			var colHeadersCount = Math.Max (colHeaders != null ? Math.Max (colHeaders.Count, 1) / height : 1, 1);
			//if (line == 0) {
			//	colsStart = new List<int> { col };
			//	colsWidth = new List<int> { width };
			//}
			//if (line == 0) {
			//	if (colsStart [0] != col) {
			//		var dif = col - colsStart [0];
			//		for (int i = 0; i < colsStart.Count; i++) {
			//			colsStart [i] += dif;
			//		}
			//	}
			//	//if (colsWidth.Sum () != Math.Max (width, maxLength [maxLength.Count - 1])) {
			//	//	var dif = width - colsWidth.Sum ();
			//	//	colsWidth [colsWidth.Count - 1] += dif;
			//	//}
			//}

			//if (line == 0) {
			//	var colHeadersWidth = 0;

			//	if (container.AllowsMarking) {
			//		container.Move (0, line);
			//		driver.AddStr ("  ");
			//	}
			//	for (int i = 0; i < colHeadersCount; i++) {
			//		if (colHeaders == null) {
			//			if (container.ShowColumnHeaders) {
			//				us = string.Make ($"Col{i}");
			//				RenderUstr (container, driver, us, colsStart [i], line, width, start, container.HeadersAlignment);
			//			}
			//			break;
			//		}

			//		var ch = colHeaders [i];
			//		int colWidth;

			//		if (i > 0) {
			//			colsStart.Add (colHeadersWidth);
			//		}

			//		if (ch.Width == -1) {
			//			colsStart [i] = -1;
			//			continue;
			//		}

			//		//if (ch.HeaderName != null && !string.IsNullOrEmpty (ch.HeaderName)) {
			//		//	us = string.Make (ch.HeaderName);
			//		//} else if (ch.MemberName != null && !string.IsNullOrEmpty (ch.MemberName)) {
			//		//	us = string.Make (ch.MemberName);
			//		//} else {
			//		//	us = string.Make ($"Col{i}");
			//		//}
			//		us = GetColumnName (colHeaders, i, ch);

			//		if (ch.Width == 0) {
			//			if (src == null) {
			//				colWidth = Math.Min (width - width / colHeadersCount, width);
			//			} else if (maxLength [i] < us.ConsoleWidth + container.ColumnMargin) {
			//				var newMaxLength = us.ConsoleWidth + container.ColumnMargin;
			//				len += newMaxLength - maxLength [i];
			//				colWidth = maxLength [i] = newMaxLength;
			//			} else {
			//				colWidth = maxLength [i];
			//			}
			//		} else {
			//			colWidth = Math.Min (ch.Width, width);
			//		}
			//		if (i == 0) {
			//			colsWidth [0] = colWidth;
			//		} else if (i == colHeadersCount - 1 && colWidth < width - colHeadersWidth) {
			//			colWidth = width - colHeadersWidth;
			//			colsWidth.Add (colWidth);
			//		} else {
			//			colsWidth.Add (colWidth);
			//		}
			//		colHeadersWidth += colWidth;

			//		//RenderUstr (driver, us, colsStart [i], line, colWidth, start);
			//		RenderUstr (container, driver, us, colsStart [i], line, colWidth, start, ch);
			//	}
			//	if (container.ShowColumnHeaders) {
			//		return;
			//	}
			//}

			if (line == 0) {
				if (container.ShowColumnHeaders && container.AllowsMarking) {
					container.Move (0, line);
					driver.AddStr ("  ");
				}
				for (int i = 0; i < colHeadersCount; i++) {
					if (colHeaders == null) {
						if (container.ShowColumnHeaders) {
							us = $"Col{i}";
							RenderUstr (container, driver, us, _colsStart [i], line, width, start, container.HeadersAlignment);
						}
						break;
					}

					var ch = colHeaders [i];
					//int colWidth;

					//if (i > 0) {
					//	colsStart.Add (colHeadersWidth);
					//}

					if (ch.Width == -1) {
						//colsStart [i] = -1;
						continue;
					}

					//if (ch.HeaderName != null && !string.IsNullOrEmpty (ch.HeaderName)) {
					//	us = string.Make (ch.HeaderName);
					//} else if (ch.MemberName != null && !string.IsNullOrEmpty (ch.MemberName)) {
					//	us = string.Make (ch.MemberName);
					//} else {
					//	us = string.Make ($"Col{i}");
					//}
					us = GetColumnName (colHeaders, i, ch);

					//if (ch.Width == 0) {
					//	if (src == null) {
					//		colWidth = Math.Min (width - width / colHeadersCount, width);
					//	} else if (maxLength [i] < us.ConsoleWidth + container.ColumnMargin) {
					//		var newMaxLength = us.ConsoleWidth + container.ColumnMargin;
					//		len += newMaxLength - maxLength [i];
					//		colWidth = maxLength [i] = newMaxLength;
					//	} else {
					//		colWidth = maxLength [i];
					//	}
					//} else {
					//	colWidth = Math.Min (ch.Width, width);
					//}
					//if (i == 0) {
					//	colsWidth [0] = colWidth;
					//} else if (i == colHeadersCount - 1 && colWidth < width - colHeadersWidth) {
					//	colWidth = width - colHeadersWidth;
					//	colsWidth.Add (colWidth);
					//} else {
					//	colsWidth.Add (colWidth);
					//}
					//colHeadersWidth += colWidth;

					//RenderUstr (driver, us, colsStart [i], line, colWidth, start);
					RenderUstr (container, driver, us, _colsStart [i], line, _colsWidth [i], start, ch);
				}
				if (container.ShowColumnHeaders) {
					return;
				}
			}

			if (t == null) {
				us = " ";
				for (int i = 0; i < height; i++) {
					lines.Add (us);
				}
			} else if (t is string u) {
				us = u;
				lines = GetFormattedLines (TextFormatter.SplitNewLine (us), colHeaders, colHeadersCount, height);
			} else if (t is string s) {
				us = s;
				lines = GetFormattedLines (TextFormatter.SplitNewLine (us), colHeaders, colHeadersCount, height);
			} else {
				lines = GetCustomTypesLines (t, colHeaders, colHeadersCount, height);
			}

			int row = 0;
			int cs = colHeaders == null ? col : _colsStart [0];
			int cw = colHeaders == null ? width : _colsWidth [0];
			for (int c = 0; c < colHeadersCount; c++) {
				var ch = colHeaders? [c];

				if (row < colHeadersCount && row == height) {
					cs = _colsStart [c];
					cw = _colsWidth [c];
					var l = lines [c];
					container.Move (cs, line + 1);
					//RenderUstr (driver, lines [row], cs, line, width - cs, start);
					RenderUstr (container, driver, l, cs, line, cw, start, ch);
				}
				for (; row < height; row++) {
					if (row > lines.Count - 1) {
						for (; row < height; row++) {
							if (container.AllowsMarking && row > 0) {
								container.Move (0, line + row);
								driver.AddStr ("  ");
							}
							container.Move (cs, line + row);
							//RenderUstr (driver, "", cs, line + row, width - cs, start);
							if (row > lines.Count - 1) {
								RenderUstr (container, driver, " ", cs, line + row, cw, start, ch);
							} else {
								RenderUstr (container, driver, lines [row], cs, line + row, cw, start, ch);
							}
						}
						break;
					}
					var l = lines [row];
					if (line + row < container.Frame.Height) {
						if (container.AllowsMarking && row > 0) {
							container.Move (0, line + row);
							driver.AddStr ("  ");
						}
						container.Move (cs, line + row);
						//RenderUstr (driver, l, cs, line + row, width, start);
						RenderUstr (container, driver, l, cs, line + row, cw, start, ch);
					}
				}
			}
		}

		private string GetColumnName (List<ListView.ColumnHeader> colHeaders, int index, ListView.ColumnHeader ch)
		{
			string us;
			if (colHeaders == null) {
				us = $"Col{index}";
			} else if (ch.HeaderName != null && !string.IsNullOrEmpty (ch.HeaderName)) {
				us = ch.HeaderName;
			} else if (ch.MemberName != null && !string.IsNullOrEmpty (ch.MemberName)) {
				us = ch.MemberName;
			} else {
				us = $"Col{index}";
			}
			return us;
		}

		private List<string> GetFormattedLines (List<string> lines, List<ListView.ColumnHeader> colHeaders, int colHeadersCount, int height)
		{
			if (colHeaders == null) {
				return lines;
			}

			List<string> fmtLines = new List<string> ();

			for (int c = 0; c < colHeadersCount; c++) {
				for (int r = 0; r < height; r++) {
					if (r < colHeaders.Count) {
						var ch = colHeadersCount > height ? colHeaders [c] : colHeaders [r];
						var us = colHeadersCount > height ? lines [c] : lines [r];
						us = GetFormattedLine (ch, us);
						fmtLines.Add (us);
					} else {
						fmtLines.Add (" ");
					}
				}
			}
			return fmtLines;
		}

		private string GetFormattedLine (ListView.ColumnHeader ch, string us)
		{
			if (!string.IsNullOrEmpty (ch.MemberFormat)) {
				var type = ch.MemberType;
				var value = Convert.ChangeType (us, type);
				value = string.Format (ch.MemberFormat, value);
				return value.ToString ();
			}
			return us;
		}

		private List<string> GetCustomTypesLines (object t, List<ListView.ColumnHeader> colHeaders, int colHeadersCount, int height)
		{
			string us;
			if (colHeaders == null) {
				us = t.ToString ();
				return TextFormatter.SplitNewLine (us);
			}

			List<string> lines = new List<string> ();

			for (int c = 0; c < colHeadersCount; c++) {
				for (int r = 0; r < height; r++) {
					if (r < colHeaders.Count) {
						var ch = colHeadersCount > height ? colHeaders [c] : colHeaders [r];
						if (!string.IsNullOrEmpty (ch.MemberName)) {
							var type = ch.MemberType;
							var value = Convert.ChangeType (GetPropertyValue (t, ch.MemberName), type);
							if (!string.IsNullOrEmpty (ch.MemberFormat)) {
								value = string.Format (ch.MemberFormat, value);
							} else {
								value.ToString ();
							}
							us = value.ToString ();
							lines.Add (us);
						} else {
							lines.Add (" ");
						}
					} else {
						lines.Add (" ");
					}
				}
			}
			return lines;
		}

		private object GetPropertyValue (object source, string propertyName) =>
		    source.GetType ().GetProperty (propertyName).GetValue (source, null);

		private void SetPropertyValue (object source, string propertyName, object value) =>
		    source.GetType ().GetProperty (propertyName).SetValue (source, value);

		/// <summary>
		/// Returns true if the item is marked, false otherwise.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns><c>true</c>If is marked.<c>false</c>otherwise.</returns>
		public bool IsMarked (int item)
		{
			if (item >= 0 && item < _count)
				return _marks [item];
			return false;
		}

		/// <inheritdoc/>
		public void SetMark (int item, bool value)
		{
			if (item >= 0 && item < _count)
				_marks [item] = value;
		}

		/// <inheritdoc/>
		public IList ToList ()
		{
			return _src;
		}

		/// <inheritdoc/>
		public int StartsWith (string search)
		{
			if (_src == null || _src?.Count == 0) {
				return -1;
			}

			for (int i = 0; i < _src.Count; i++) {
				var t = _src [i];
				if (t is string u) {
					if (u.ToUpper ().StartsWith (search.ToUpperInvariant ())) {
						return i;
					}
				} else if (t is string s) {
					if (s.StartsWith (search, StringComparison.InvariantCultureIgnoreCase)) {
						return i;
					}
				}
			}
			return -1;
		}
	}
}
