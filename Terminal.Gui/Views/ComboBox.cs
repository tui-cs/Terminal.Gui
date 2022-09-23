//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//

using System;
using System.Collections;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Provides a drop-down list of items the user can select from.
	/// </summary>
	public class ComboBox : View {

		private class ComboListView : ListView {
			private int highlighted = -1;
			private bool isFocusing;
			private ComboBox container;
			private bool hideDropdownListOnClick;

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
				if (container == null)
					throw new ArgumentNullException ("ComboBox container cannot be null.", nameof (container));

				this.container = container;
				HideDropdownListOnClick = hideDropdownListOnClick;
			}

			public bool HideDropdownListOnClick {
				get => hideDropdownListOnClick;
				set => hideDropdownListOnClick = WantMousePositionReports = value;
			}

			public override bool MouseEvent (MouseEvent me)
			{
				var res = false;
				var isMousePositionValid = IsMousePositionValid (me);

				if (isMousePositionValid) {
					res = base.MouseEvent (me);
				}

				if (HideDropdownListOnClick && me.Flags == MouseFlags.Button1Clicked) {
					if (!isMousePositionValid && !isFocusing) {
						container.isShow = false;
						container.HideList ();
					} else if (isMousePositionValid) {
						OnOpenSelectedItem ();
					} else {
						isFocusing = false;
					}
					return true;
				} else if (me.Flags == MouseFlags.ReportMousePosition && HideDropdownListOnClick) {
					if (isMousePositionValid) {
						highlighted = Math.Min (TopItem + me.Y, Source.Count);
						SetNeedsDisplay ();
					}
					isFocusing = false;
					return true;
				}

				return res;
			}

			private bool IsMousePositionValid (MouseEvent me)
			{
				if (me.X >= 0 && me.X < Frame.Width && me.Y >= 0 && me.Y < Frame.Height) {
					return true;
				}
				return false;
			}

			public override void Redraw (Rect bounds)
			{
				var current = ColorScheme.Focus;
				Driver.SetAttribute (current);
				Move (0, 0);
				var f = Frame;
				var item = TopItem;
				bool focused = HasFocus;
				int col = AllowsMarking ? 2 : 0;
				int start = LeftItem;

				for (int row = 0; row < f.Height; row++, item++) {
					bool isSelected = item == container.SelectedItem;
					bool isHighlighted = hideDropdownListOnClick && item == highlighted;

					Attribute newcolor;
					if (isHighlighted || (isSelected && !hideDropdownListOnClick)) {
						newcolor = focused ? ColorScheme.Focus : ColorScheme.HotNormal;
					} else if (isSelected && hideDropdownListOnClick) {
						newcolor = focused ? ColorScheme.HotFocus : ColorScheme.HotNormal;
					} else {
						newcolor = focused ? GetNormalColor () : GetNormalColor ();
					}

					if (newcolor != current) {
						Driver.SetAttribute (newcolor);
						current = newcolor;
					}

					Move (0, row);
					if (Source == null || item >= Source.Count) {
						for (int c = 0; c < f.Width; c++)
							Driver.AddRune (' ');
					} else {
						var rowEventArgs = new ListViewRowEventArgs (item);
						OnRowRender (rowEventArgs);
						if (rowEventArgs.RowAttribute != null && current != rowEventArgs.RowAttribute) {
							current = (Attribute)rowEventArgs.RowAttribute;
							Driver.SetAttribute (current);
						}
						if (AllowsMarking) {
							Driver.AddRune (Source.IsMarked (item) ? (AllowsMultipleSelection ? Driver.Checked : Driver.Selected) : (AllowsMultipleSelection ? Driver.UnChecked : Driver.UnSelected));
							Driver.AddRune (' ');
						}
						Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
					}
				}
			}

			public override bool OnMouseEnter (MouseEvent mouseEvent)
			{
				if (hideDropdownListOnClick && HasFocus && Application.mouseGrabView != this) {
					Application.GrabMouse (this);
				}

				return base.OnMouseEnter (mouseEvent);
			}

			public override bool OnEnter (View view)
			{
				if (hideDropdownListOnClick) {
					isFocusing = true;
					highlighted = container.SelectedItem;
					Application.GrabMouse (this);
				}

				return base.OnEnter (view);
			}

			public override bool OnLeave (View view)
			{
				if (hideDropdownListOnClick) {
					isFocusing = false;
					highlighted = container.SelectedItem;
					if (container.IsShow) {
						container.isShow = false;
						container.HideList ();
					}
					if (Application.mouseGrabView == this) {
						Application.UngrabMouse ();
					}
				}

				return base.OnLeave (view);
			}

			public override bool OnSelectedChanged ()
			{
				var res = base.OnSelectedChanged ();

				highlighted = SelectedItem;

				return res;
			}
		}

		IListDataSource source;
		/// <summary>
		/// Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ComboBox"/>, enabling custom rendering.
		/// </summary>
		/// <value>The source.</value>
		/// <remarks>
		///  Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.
		/// </remarks>
		public IListDataSource Source {
			get => source;
			set {
				source = value;

				// Only need to refresh list if its been added to a container view
				if (SuperView != null && SuperView.Subviews.Contains (this)) {
					SelectedItem = -1;
					search.Text = "";
					Search_Changed ("");
					SetNeedsDisplay ();
				}
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
				listview.SetSource (source);
				Source = listview.Source;
			}
		}

		/// <summary>
		/// This event is raised when the selected item in the <see cref="ComboBox"/> has changed.
		/// </summary>
		public event Action<ListViewItemEventArgs> SelectedItemChanged;

		/// <summary>
		/// This event is raised when the drop-down list is expanded.
		/// </summary>
		public event Action Expanded;

		/// <summary>
		/// This event is raised when the drop-down list is collapsed.
		/// </summary>
		public event Action Collapsed;

		/// <summary>
		/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
		/// </summary>
		public event Action<ListViewItemEventArgs> OpenSelectedItem;

		readonly IList searchset = new List<object> ();
		ustring text = "";
		readonly TextField search;
		readonly ComboListView listview;
		bool autoHide = true;
		int minimumHeight = 2;
		private int savedHeight;

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
		public ComboBox (ustring text) : base ()
		{
			search = new TextField ("");
			listview = new ComboListView (this, HideDropdownListOnClick) { LayoutStyle = LayoutStyle.Computed, CanFocus = true, TabStop = false };

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
			search = new TextField ("") { Width = rect.Width };
			listview = new ComboListView (this, rect, source, HideDropdownListOnClick) { LayoutStyle = LayoutStyle.Computed, ColorScheme = Colors.Base };

			Initialize ();
			SetSource (source);
		}

		/// <summary>
		/// Initialize with the source.
		/// </summary>
		/// <param name="source">The source.</param>
		public ComboBox (IList source)
		{
			search = new TextField ("");
			listview = new ComboListView (this, source, HideDropdownListOnClick) { LayoutStyle = LayoutStyle.Computed, ColorScheme = Colors.Base };

			Initialize ();
			SetSource (source);
		}

		private void Initialize ()
		{
			if (Bounds.Height < minimumHeight && (Height == null || Height is Dim.DimAbsolute)) {
				Height = minimumHeight;
			}

			search.TextChanged += Search_Changed;

			listview.Y = Pos.Bottom (search);
			listview.OpenSelectedItem += (ListViewItemEventArgs a) => Selected ();

			this.Add (search, listview);

			// On resize
			LayoutComplete += (LayoutEventArgs a) => {
				if ((!autoHide && Bounds.Width > 0 && search.Frame.Width != Bounds.Width) ||
				    (autoHide && Bounds.Width > 0 && search.Frame.Width != Bounds.Width - 1)) {

					search.Width = listview.Width = autoHide ? Bounds.Width - 1 : Bounds.Width;
					listview.Height = CalculatetHeight ();
					search.SetRelativeLayout (Bounds);
					listview.SetRelativeLayout (Bounds);
				}
			};

			listview.SelectedItemChanged += (ListViewItemEventArgs e) => {

				if (!HideDropdownListOnClick && searchset.Count > 0) {
					SetValue (searchset [listview.SelectedItem]);
				}
			};

			Added += (View v) => {

				// Determine if this view is hosted inside a dialog and is the only control
				for (View view = this.SuperView; view != null; view = view.SuperView) {
					if (view is Dialog && SuperView != null && SuperView.Subviews.Count == 1 && SuperView.Subviews [0] == this) {
						autoHide = false;
						break;
					}
				}

				savedHeight = Bounds.Height;
				if (hideDropdownListOnClick) {
					Height = 1;
				}

				SetNeedsLayout ();
				SetNeedsDisplay ();
				Search_Changed (Text);
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

		private bool isShow = false;
		private int selectedItem = -1;
		private int lastSelectedItem = -1;
		private bool hideDropdownListOnClick;

		/// <summary>
		/// Gets the index of the currently selected item in the <see cref="Source"/>
		/// </summary>
		/// <value>The selected item or -1 none selected.</value>
		public int SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem != value && (value == -1
				    || (source != null && value > -1 && value < source.Count))) {

					selectedItem = lastSelectedItem = value;
					if (selectedItem != -1) {
						SetValue (source.ToList () [selectedItem].ToString (), true);
					} else {
						SetValue ("", true);
					}
					OnSelectedChanged ();
				}
			}
		}

		/// <summary>
		/// Gets the drop down list state, expanded or collapsed.
		/// </summary>
		public bool IsShow => isShow;

		///<inheritdoc/>
		public new ColorScheme ColorScheme {
			get {
				return base.ColorScheme;
			}
			set {
				listview.ColorScheme = value;
				base.ColorScheme = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		///If set to true its not allow any changes in the text.
		/// </summary>
		public bool ReadOnly {
			get => search.ReadOnly;
			set {
				search.ReadOnly = value;
				if (search.ReadOnly) {
					if (search.ColorScheme != null) {
						search.ColorScheme.Normal = search.ColorScheme.Focus;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets if the drop-down list can be hide with a button click event.
		/// </summary>
		public bool HideDropdownListOnClick {
			get => hideDropdownListOnClick;
			set => hideDropdownListOnClick = listview.HideDropdownListOnClick = value;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.X == Bounds.Right - 1 && me.Y == Bounds.Top && me.Flags == MouseFlags.Button1Pressed
			    && autoHide) {

				if (isShow) {
					isShow = false;
					HideList ();
				} else {
					SetSearchSet ();

					isShow = true;
					ShowList ();
					FocusSelectedItem ();
				}

				return true;
			} else if (me.Flags == MouseFlags.Button1Pressed) {
				if (!search.HasFocus) {
					search.SetFocus ();
				}

				return true;
			}

			return false;
		}

		private void FocusSelectedItem ()
		{
			listview.SelectedItem = SelectedItem > -1 ? SelectedItem : 0;
			listview.TabStop = true;
			listview.SetFocus ();
			OnExpanded ();
		}

		/// <summary>
		/// Virtual method which invokes the <see cref="Expanded"/> event.
		/// </summary>
		public virtual void OnExpanded ()
		{
			Expanded?.Invoke ();
		}

		/// <summary>
		/// Virtual method which invokes the <see cref="Collapsed"/> event.
		/// </summary>
		public virtual void OnCollapsed ()
		{
			Collapsed?.Invoke ();
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (!search.HasFocus && !listview.HasFocus) {
				search.SetFocus ();
			}

			search.CursorPosition = search.Text.RuneCount;

			return base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			if (source?.Count > 0 && selectedItem > -1 && selectedItem < source.Count - 1
			    && text != source.ToList () [selectedItem].ToString ()) {

				SetValue (source.ToList () [selectedItem].ToString ());
			}
			if (autoHide && isShow && view != this && view != search && view != listview) {
				isShow = false;
				HideList ();
			} else if (listview.TabStop) {
				listview.TabStop = false;
			}

			return base.OnLeave (view);
		}

		/// <summary>
		/// Invokes the SelectedChanged event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnSelectedChanged ()
		{
			// Note: Cannot rely on "listview.SelectedItem != lastSelectedItem" because the list is dynamic. 
			// So we cannot optimize. Ie: Don't call if not changed
			SelectedItemChanged?.Invoke (new ListViewItemEventArgs (SelectedItem, search.Text));

			return true;
		}

		/// <summary>
		/// Invokes the OnOpenSelectedItem event if it is defined.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnOpenSelectedItem ()
		{
			var value = search.Text;
			lastSelectedItem = SelectedItem;
			OpenSelectedItem?.Invoke (new ListViewItemEventArgs (SelectedItem, value));

			return true;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			if (!autoHide) {
				return;
			}

			Driver.SetAttribute (ColorScheme.Focus);
			Move (Bounds.Right - 1, 0);
			Driver.AddRune (Driver.DownArrow);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent e)
		{
			var result = InvokeKeybindings (e);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (e);
		}

		bool UnixEmulation ()
		{
			// Unix emulation
			Reset ();
			return true;
		}

		bool CancelSelected ()
		{
			search.SetFocus ();
			if (ReadOnly || HideDropdownListOnClick) {
				SelectedItem = lastSelectedItem;
				if (SelectedItem > -1 && listview.Source?.Count > 0) {
					search.Text = text = listview.Source.ToList () [SelectedItem].ToString ();
				}
			} else if (!ReadOnly) {
				search.Text = text = "";
				selectedItem = lastSelectedItem;
				OnSelectedChanged ();
			}
			Collapse ();
			return true;
		}

		bool? MoveEnd ()
		{
			if (!isShow && search.HasFocus) {
				return null;
			}
			if (HasItems ()) {
				listview.MoveEnd ();
			}
			return true;
		}

		bool? MoveHome ()
		{
			if (!isShow && search.HasFocus) {
				return null;
			}
			if (HasItems ()) {
				listview.MoveHome ();
			}
			return true;
		}

		bool PageUp ()
		{
			if (HasItems ()) {
				listview.MovePageUp ();
			}
			return true;
		}

		bool PageDown ()
		{
			if (HasItems ()) {
				listview.MovePageDown ();
			}
			return true;
		}

		bool? MoveUp ()
		{
			if (search.HasFocus) { // stop odd behavior on KeyUp when search has focus
				return true;
			}

			if (listview.HasFocus && listview.SelectedItem == 0 && searchset?.Count > 0) // jump back to search
			{
				search.CursorPosition = search.Text.RuneCount;
				search.SetFocus ();
				return true;
			}
			return null;
		}

		bool? MoveDown ()
		{
			if (search.HasFocus) { // jump to list
				if (searchset?.Count > 0) {
					listview.TabStop = true;
					listview.SetFocus ();
					SetValue (searchset [listview.SelectedItem]);
				} else {
					listview.TabStop = false;
					SuperView?.FocusNext ();
				}
				return true;
			}
			return null;
		}

		/// <summary>
		/// Toggles the expand/collapse state of the sublist in the combo box
		/// </summary>
		/// <returns></returns>
		bool ExpandCollapse ()
		{
			if (search.HasFocus || listview.HasFocus) {
				if (!isShow) {
					return Expand ();
				} else {
					return Collapse ();
				}
			}
			return false;
		}

		bool ActivateSelected ()
		{
			if (HasItems ()) {
				Selected ();
				return true;
			}
			return false;
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
			if (!isShow) {
				return false;
			}

			isShow = false;
			HideList ();
			return true;
		}

		/// <summary>
		/// Expands the drop down list.  Returns true if the state chagned or false
		/// if it was already expanded and no action was taken
		/// </summary>
		public virtual bool Expand ()
		{
			if (isShow) {
				return false;
			}

			SetSearchSet ();
			isShow = true;
			ShowList ();
			FocusSelectedItem ();

			return true;
		}

		/// <summary>
		/// The currently selected list item
		/// </summary>
		public new ustring Text {
			get {
				return text;
			}
			set {
				search.Text = text = value;
			}
		}

		private void SetValue (object text, bool isFromSelectedItem = false)
		{
			search.TextChanged -= Search_Changed;
			this.text = search.Text = text.ToString ();
			search.CursorPosition = 0;
			search.TextChanged += Search_Changed;
			if (!isFromSelectedItem) {
				selectedItem = GetSelectedItemFromSource (this.text);
				OnSelectedChanged ();
			}
		}

		private void Selected ()
		{
			isShow = false;
			listview.TabStop = false;

			if (listview.Source.Count == 0 || (searchset?.Count ?? 0) == 0) {
				text = "";
				HideList ();
				return;
			}

			SetValue (searchset [listview.SelectedItem]);
			search.CursorPosition = search.Text.RuneCount;
			Search_Changed (search.Text);
			OnOpenSelectedItem ();
			Reset (keepSearchText: true);
			HideList ();
		}

		private int GetSelectedItemFromSource (ustring value)
		{
			if (source == null) {
				return -1;
			}
			for (int i = 0; i < source.Count; i++) {
				if (source.ToList () [i].ToString () == value) {
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
				search.Text = text = "";
			}

			ResetSearchSet ();

			listview.SetSource (searchset);
			listview.Height = CalculatetHeight ();

			if (Subviews.Count > 0) {
				search.SetFocus ();
			}
		}

		private void ResetSearchSet (bool noCopy = false)
		{
			searchset.Clear ();

			if (autoHide || noCopy)
				return;
			SetSearchSet ();
		}

		private void SetSearchSet ()
		{
			if (Source == null) { return; }
			// force deep copy
			foreach (var item in Source.ToList ()) {
				searchset.Add (item);
			}
		}

		private void Search_Changed (ustring text)
		{
			if (source == null) { // Object initialization		
				return;
			}

			if (ustring.IsNullOrEmpty (search.Text) && ustring.IsNullOrEmpty (text)) {
				ResetSearchSet ();
			} else if (search.Text != text) {
				isShow = true;
				ResetSearchSet (noCopy: true);

				foreach (var item in source.ToList ()) { // Iterate to preserver object type and force deep copy
					if (item.ToString ().StartsWith (search.Text.ToString (), StringComparison.CurrentCultureIgnoreCase)) {
						searchset.Add (item);
					}
				}
			}

			if (!hideDropdownListOnClick) {
				ShowList ();
			}
		}

		/// <summary>
		/// Show the search list
		/// </summary>
		/// 
		/// Consider making public
		private void ShowList ()
		{
			listview.SetSource (searchset);
			listview.Clear (); // Ensure list shrinks in Dialog as you type
			if (hideDropdownListOnClick) {
				Height = savedHeight;
			}
			listview.Height = CalculatetHeight ();
			this.SuperView?.BringSubviewToFront (this);
		}

		/// <summary>
		/// Hide the search list
		/// </summary>
		/// 
		/// Consider making public
		private void HideList ()
		{
			if (lastSelectedItem != selectedItem) {
				OnOpenSelectedItem ();
			}
			var rect = listview.ViewToScreen (listview.Bounds);
			Reset (SelectedItem > -1);
			listview.Clear (rect);
			listview.TabStop = false;
			if (hideDropdownListOnClick) {
				savedHeight = Bounds.Height;
				Height = 1;
				listview.Height = 0;
			}
			SuperView?.SendSubviewToBack (this);
			SuperView?.SetNeedsDisplay (rect);
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

			return Math.Min (Math.Max (Bounds.Height - 1, minimumHeight - 1), searchset?.Count > 0 ? searchset.Count : isShow ? Math.Max (Bounds.Height - 1, minimumHeight - 1) : 0);
		}
	}
}
