using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NStack;

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
	///   <see cref="string"/> values are converted into <see cref="NStack.ustring"/> values before rendering, and other values are
	///   converted into <see cref="string"/> by calling <see cref="object.ToString"/> and then converting to <see cref="NStack.ustring"/> .
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
	/// </remarks>
	public class ListView : View {
		int top, left;
		int selected;

		IListDataSource source;
		/// <summary>
		/// Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ListView"/>, enabling custom rendering.
		/// </summary>
		/// <value>The source.</value>
		/// <remarks>
		///  Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.
		/// </remarks>
		public IListDataSource Source {
			get => source;
			set {
				source = value;
				KeystrokeNavigator.Collection = source?.ToList ()?.Cast<object> ();
				top = 0;
				selected = 0;
				lastSelectedItem = -1;
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

		bool allowsMarking;
		/// <summary>
		/// Gets or sets whether this <see cref="ListView"/> allows items to be marked.
		/// </summary>
		/// <value>Set to <see langword="true"/> to allow marking elements of the list.</value>
		/// <remarks>
		/// If set to <see langword="true"/>, <see cref="ListView"/> will render items marked items with "[x]", and unmarked items with "[ ]"
		/// spaces. SPACE key will toggle marking. The default is <see langword="false"/>.
		/// </remarks>
		public bool AllowsMarking {
			get => allowsMarking;
			set {
				allowsMarking = value;
				if (allowsMarking) {
					AddKeyBinding (Key.Space, Command.ToggleChecked);
				} else {
					ClearKeybinding (Key.Space);
				}

				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// If set to <see langword="true"/> more than one item can be selected. If <see langword="false"/> selecting
		/// an item will cause all others to be un-selected. The default is <see langword="false"/>.
		/// </summary>
		public bool AllowsMultipleSelection {
			get => allowsMultipleSelection;
			set {
				allowsMultipleSelection = value;
				if (Source != null && !allowsMultipleSelection) {
					// Clear all selections except selected 
					for (int i = 0; i < Source.Count; i++) {
						if (Source.IsMarked (i) && i != selected) {
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
			get => top;
			set {
				if (source == null)
					return;

				if (value < 0 || (source.Count > 0 && value >= source.Count))
					throw new ArgumentException ("value");
				top = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets or sets the leftmost column that is currently visible (when scrolling horizontally).
		/// </summary>
		/// <value>The left position.</value>
		public int LeftItem {
			get => left;
			set {
				if (source == null)
					return;

				if (value < 0 || (Maxlength > 0 && value >= Maxlength))
					throw new ArgumentException ("value");
				left = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Gets the widest item in the list.
		/// </summary>
		public int Maxlength => (source?.Length) ?? 0;

		/// <summary>
		/// Gets or sets the index of the currently selected item.
		/// </summary>
		/// <value>The selected item.</value>
		public int SelectedItem {
			get => selected;
			set {
				if (source == null || source.Count == 0) {
					return;
				}
				if (value < 0 || value >= source.Count) {
					throw new ArgumentException ("value");
				}
				selected = value;
				OnSelectedChanged ();
			}
		}

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
			this.source = source;
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
			this.source = source;
			Initialize ();
		}

		void Initialize ()
		{
			Source = source;
			CanFocus = true;

			// Things this view knows how to do
			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
			AddCommand (Command.ScrollUp, () => ScrollUp (1));
			AddCommand (Command.ScrollDown, () => ScrollDown (1));
			AddCommand (Command.PageUp, () => MovePageUp ());
			AddCommand (Command.PageDown, () => MovePageDown ());
			AddCommand (Command.TopHome, () => MoveHome ());
			AddCommand (Command.BottomEnd, () => MoveEnd ());
			AddCommand (Command.OpenSelectedItem, () => OnOpenSelectedItem ());
			AddCommand (Command.ToggleChecked, () => MarkUnmarkRow ());

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
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = top;
			bool focused = HasFocus;
			int col = allowsMarking ? 2 : 0;
			int start = left;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : GetNormalColor ())
						       : (isSelected ? ColorScheme.HotNormal : GetNormalColor ());

				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (source == null || item >= source.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
				} else {
					var rowEventArgs = new ListViewRowEventArgs (item);
					OnRowRender (rowEventArgs);
					if (rowEventArgs.RowAttribute != null && current != rowEventArgs.RowAttribute) {
						current = (Attribute)rowEventArgs.RowAttribute;
						Driver.SetAttribute (current);
					}
					if (allowsMarking) {
						Driver.AddRune (source.IsMarked (item) ? (AllowsMultipleSelection ? Driver.Checked : Driver.Selected) :
							(AllowsMultipleSelection ? Driver.UnChecked : Driver.UnSelected));
						Driver.AddRune (' ');
					}
					Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
				}
			}
		}

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
			if (source == null) {
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
			if (!allowsMarking)
				return false;
			if (!AllowsMultipleSelection) {
				for (int i = 0; i < Source.Count; i++) {
					if (Source.IsMarked (i) && i != selected) {
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
			int n = (selected - Frame.Height);
			if (n < 0)
				n = 0;
			if (n != selected) {
				selected = n;
				top = selected;
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
			var n = (selected + Frame.Height);
			if (n >= source.Count)
				n = source.Count - 1;
			if (n != selected) {
				selected = n;
				if (source.Count >= Frame.Height)
					top = selected;
				else
					top = 0;
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
			if (source.Count == 0) {
				// Do we set lastSelectedItem to -1 here?
				return false; //Nothing for us to move to
			}
			if (selected >= source.Count) {
				// If for some reason we are currently outside of the
				// valid values range, we should select the bottommost valid value.
				// This can occur if the backing data source changes.
				selected = source.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected + 1 < source.Count) { //can move by down by one.
				selected++;

				if (selected >= top + Frame.Height) {
					top++;
				} else if (selected < top) {
					top = selected;
				} else if (selected < top) {
					top = selected;
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected == 0) {
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected >= top + Frame.Height) {
				top = source.Count - Frame.Height;
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
			if (source.Count == 0) {
				// Do we set lastSelectedItem to -1 here?
				return false; //Nothing for us to move to
			}
			if (selected >= source.Count) {
				// If for some reason we are currently outside of the
				// valid values range, we should select the bottommost valid value.
				// This can occur if the backing data source changes.
				selected = source.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected > 0) {
				selected--;
				if (selected > Source.Count) {
					selected = Source.Count - 1;
				}
				if (selected < top) {
					top = selected;
				} else if (selected > top + Frame.Height) {
					top = Math.Max (selected - Frame.Height + 1, 0);
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected < top) {
				top = selected;
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
			if (source.Count > 0 && selected != source.Count - 1) {
				selected = source.Count - 1;
				if (top + selected > Frame.Height - 1) {
					top = selected;
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
			if (selected != 0) {
				selected = 0;
				top = selected;
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
			top = Math.Max (Math.Min (top + items, source.Count - 1), 0);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Scrolls the view up by <paramref name="items"/> items.
		/// </summary>
		/// <param name="items">Number of items to scroll up.</param>
		public virtual bool ScrollUp (int items)
		{
			top = Math.Max (top - items, 0);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Scrolls the view right.
		/// </summary>
		/// <param name="cols">Number of columns to scroll right.</param>
		public virtual bool ScrollRight (int cols)
		{
			left = Math.Max (Math.Min (left + cols, Maxlength - 1), 0);
			SetNeedsDisplay ();
			return true;
		}

		/// <summary>
		/// Scrolls the view left.
		/// </summary>
		/// <param name="cols">Number of columns to scroll left.</param>
		public virtual bool ScrollLeft (int cols)
		{
			left = Math.Max (left - cols, 0);
			SetNeedsDisplay ();
			return true;
		}

		int lastSelectedItem = -1;
		private bool allowsMultipleSelection = true;

		/// <summary>
		/// Invokes the <see cref="SelectedItemChanged"/> event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnSelectedChanged ()
		{
			if (selected != lastSelectedItem) {
				var value = source?.Count > 0 ? source.ToList () [selected] : null;
				SelectedItemChanged?.Invoke (this, new ListViewItemEventArgs (selected, value));
				if (HasFocus) {
					lastSelectedItem = selected;
				}
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
			if (source.Count <= selected || selected < 0 || OpenSelectedItem == null) {
				return false;
			}

			var value = source.ToList () [selected];

			OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (selected, value));

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
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			if (lastSelectedItem == -1) {
				EnsureSelectedItemVisible ();
			}

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (lastSelectedItem > -1) {
				lastSelectedItem = -1;
			}

			return base.OnLeave (view);
		}

		/// <summary>
		/// Ensures the selected item is always visible on the screen.
		/// </summary>
		public void EnsureSelectedItemVisible ()
		{
			SuperView?.LayoutSubviews ();
			if (selected < top) {
				top = selected;
			} else if (Frame.Height > 0 && selected >= top + Frame.Height) {
				top = Math.Max (selected - Frame.Height + 1, 0);
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (allowsMarking)
				Move (0, selected - top);
			else
				Move (Bounds.Width - 1, selected - top);
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledRight && me.Flags != MouseFlags.WheeledLeft)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (source == null) {
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

			if (me.Y + top >= source.Count) {
				return true;
			}

			selected = top + me.Y;
			if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}
			OnSelectedChanged ();
			SetNeedsDisplay ();
			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				OnOpenSelectedItem ();
			}

			return true;
		}
	}

	/// <summary>
	/// Provides a default implementation of <see cref="IListDataSource"/> that renders
	/// <see cref="ListView"/> items using <see cref="object.ToString()"/>.
	/// </summary>
	public class ListWrapper : IListDataSource {
		IList src;
		BitArray marks;
		int count, len;

		/// <inheritdoc/>
		public ListWrapper (IList source)
		{
			if (source != null) {
				count = source.Count;
				marks = new BitArray (count);
				src = source;
				len = GetMaxLengthItem ();
			}
		}

		/// <inheritdoc/>
		public int Count => src != null ? src.Count : 0;

		/// <inheritdoc/>
		public int Length => len;

		int GetMaxLengthItem ()
		{
			if (src == null || src?.Count == 0) {
				return 0;
			}

			int maxLength = 0;
			for (int i = 0; i < src.Count; i++) {
				var t = src [i];
				int l;
				if (t is ustring u) {
					l = TextFormatter.GetTextWidth (u);
				} else if (t is string s) {
					l = s.Length;
				} else {
					l = t.ToString ().Length;
				}

				if (l > maxLength) {
					maxLength = l;
				}
			}

			return maxLength;
		}

		void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
		{
			var u = TextFormatter.ClipAndJustify (ustr, width, TextAlignment.Left);
			driver.AddStr (u);
			width -= TextFormatter.GetTextWidth (u);
			while (width-- > 0) {
				driver.AddRune (' ');
			}
		}

		/// <inheritdoc/>
		public void Render (ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width, int start = 0)
		{
			container.Move (col, line);
			var t = src? [item];
			if (t == null) {
				RenderUstr (driver, ustring.Make (""), col, line, width);
			} else {
				if (t is ustring u) {
					RenderUstr (driver, u, col, line, width, start);
				} else if (t is string s) {
					RenderUstr (driver, s, col, line, width, start);
				} else {
					RenderUstr (driver, t.ToString (), col, line, width, start);
				}
			}
		}

		/// <inheritdoc/>
		public bool IsMarked (int item)
		{
			if (item >= 0 && item < count)
				return marks [item];
			return false;
		}

		/// <inheritdoc/>
		public void SetMark (int item, bool value)
		{
			if (item >= 0 && item < count)
				marks [item] = value;
		}

		/// <inheritdoc/>
		public IList ToList ()
		{
			return src;
		}

		/// <inheritdoc/>
		public int StartsWith (string search)
		{
			if (src == null || src?.Count == 0) {
				return -1;
			}

			for (int i = 0; i < src.Count; i++) {
				var t = src [i];
				if (t is ustring u) {
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
