// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls 
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

using NStack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui;

namespace Terminal.Gui {

	/// <summary>
	/// Interface for all non generic members of <see cref="TreeView{T}"/>.
	/// 
	/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/treeview.html">See TreeView Deep Dive for more information</a>.
	/// </summary>
	public interface ITreeView {
		/// <summary>
		/// Contains options for changing how the tree is rendered.
		/// </summary>
		TreeStyle Style { get; set; }

		/// <summary>
		/// Removes all objects from the tree and clears selection.
		/// </summary>
		void ClearObjects ();

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		void SetNeedsDisplay ();
	}

	/// <summary>
	/// Convenience implementation of generic <see cref="TreeView{T}"/> for any tree were all nodes
	/// implement <see cref="ITreeNode"/>.
	/// 
	/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/treeview.html">See TreeView Deep Dive for more information</a>.
	/// </summary>
	public class TreeView : TreeView<ITreeNode> {

		/// <summary>
		/// Creates a new instance of the tree control with absolute positioning and initialises
		/// <see cref="TreeBuilder{T}"/> with default <see cref="ITreeNode"/> based builder.
		/// </summary>
		public TreeView ()
		{
			TreeBuilder = new TreeNodeBuilder ();
			AspectGetter = o => o == null ? "Null" : (o.Text ?? o?.ToString () ?? "Unamed Node");
		}
	}

	/// <summary>
	/// Hierarchical tree view with expandable branches. Branch objects are dynamically determined
	/// when expanded using a user defined <see cref="ITreeBuilder{T}"/>.
	/// 
	/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/treeview.html">See TreeView Deep Dive for more information</a>.
	/// </summary>
	public class TreeView<T> : View, ITreeView where T : class {
		private int scrollOffsetVertical;
		private int scrollOffsetHorizontal;

		/// <summary>
		/// Determines how sub branches of the tree are dynamically built at runtime as the user
		/// expands root nodes.
		/// </summary>
		/// <value></value>
		public ITreeBuilder<T> TreeBuilder { get; set; }

		/// <summary>
		/// private variable for <see cref="SelectedObject"/>
		/// </summary>
		T selectedObject;

		/// <summary>
		/// Contains options for changing how the tree is rendered.
		/// </summary>
		public TreeStyle Style { get; set; } = new TreeStyle ();

		/// <summary>
		/// True to allow multiple objects to be selected at once.
		/// </summary>
		/// <value></value>
		public bool MultiSelect { get; set; } = true;

		/// <summary>
		/// True makes a letter key press navigate to the next visible branch that begins with
		/// that letter/digit.
		/// </summary>
		/// <value></value>
		public bool AllowLetterBasedNavigation { get; set; } = true;

		/// <summary>
		/// The currently selected object in the tree. When <see cref="MultiSelect"/> is true this
		/// is the object at which the cursor is at.
		/// </summary>
		public T SelectedObject {
			get => selectedObject;
			set {
				var oldValue = selectedObject;
				selectedObject = value;

				if (!ReferenceEquals (oldValue, value)) {
					OnSelectionChanged (new SelectionChangedEventArgs<T> (this, oldValue, value));
				}
			}
		}

		/// <summary>
		/// This event is raised when an object is activated e.g. by double clicking or 
		/// pressing <see cref="ObjectActivationKey"/>.
		/// </summary>
		public event EventHandler<ObjectActivatedEventArgs<T>> ObjectActivated;

		/// <summary>
		/// Key which when pressed triggers <see cref="TreeView{T}.ObjectActivated"/>.
		/// Defaults to Enter.
		/// </summary>
		public Key ObjectActivationKey {
			get => objectActivationKey;
			set {
				if (objectActivationKey != value) {
					ReplaceKeyBinding (ObjectActivationKey, value);
					objectActivationKey = value;
				}
			}
		}

		/// <summary>
		/// Mouse event to trigger <see cref="TreeView{T}.ObjectActivated"/>.
		/// Defaults to double click (<see cref="MouseFlags.Button1DoubleClicked"/>).
		/// Set to null to disable this feature.
		/// </summary>
		/// <value></value>
		public MouseFlags? ObjectActivationButton { get; set; } = MouseFlags.Button1DoubleClicked;

		/// <summary>
		/// Delegate for multi colored tree views. Return the <see cref="ColorScheme"/> to use
		/// for each passed object or null to use the default.
		/// </summary>
		public Func<T, ColorScheme> ColorGetter { get; set; }

		/// <summary>
		/// Secondary selected regions of tree when <see cref="MultiSelect"/> is true.
		/// </summary>
		private Stack<TreeSelection<T>> multiSelectedRegions = new Stack<TreeSelection<T>> ();

		/// <summary>
		/// Cached result of <see cref="BuildLineMap"/>
		/// </summary>
		private IReadOnlyCollection<Branch<T>> cachedLineMap;

		/// <summary>
		/// Error message to display when the control is not properly initialized at draw time 
		/// (nodes added but no tree builder set).
		/// </summary>
		public static ustring NoBuilderError = "ERROR: TreeBuilder Not Set";
		private Key objectActivationKey = Key.Enter;

		/// <summary>
		/// Called when the <see cref="SelectedObject"/> changes.
		/// </summary>
		public event EventHandler<SelectionChangedEventArgs<T>> SelectionChanged;

		/// <summary>
		/// The root objects in the tree, note that this collection is of root objects only.
		/// </summary>
		public IEnumerable<T> Objects { get => roots.Keys; }

		/// <summary>
		/// Map of root objects to the branches under them. All objects have 
		/// a <see cref="Branch{T}"/> even if that branch has no children.
		/// </summary>
		internal Dictionary<T, Branch<T>> roots { get; set; } = new Dictionary<T, Branch<T>> ();

		/// <summary>
		/// The amount of tree view that has been scrolled off the top of the screen (by the user 
		/// scrolling down).
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a offset of 0. To see changes 
		/// in the UI call <see cref="View.SetNeedsDisplay()"/>.</remarks>
		public int ScrollOffsetVertical {
			get => scrollOffsetVertical;
			set {
				scrollOffsetVertical = Math.Max (0, value);
			}
		}

		/// <summary>
		/// The amount of tree view that has been scrolled to the right (horizontally).
		/// </summary>
		/// <remarks>Setting a value of less than 0 will result in a offset of 0. To see changes 
		/// in the UI call <see cref="View.SetNeedsDisplay()"/>.</remarks>
		public int ScrollOffsetHorizontal {
			get => scrollOffsetHorizontal;
			set {
				scrollOffsetHorizontal = Math.Max (0, value);
			}
		}

		/// <summary>
		/// The current number of rows in the tree (ignoring the controls bounds).
		/// </summary>
		public int ContentHeight => BuildLineMap ().Count ();

		/// <summary>
		/// Returns the string representation of model objects hosted in the tree. Default 
		/// implementation is to call <see cref="object.ToString"/>.
		/// </summary>
		/// <value></value>
		public AspectGetterDelegate<T> AspectGetter { get; set; } = (o) => o.ToString () ?? "";

		CursorVisibility desiredCursorVisibility = CursorVisibility.Invisible;

		/// <summary>
		/// Get / Set the wished cursor when the tree is focused.
		/// Only applies when <see cref="MultiSelect"/> is true.
		/// Defaults to <see cref="CursorVisibility.Invisible"/>.
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get {
				return MultiSelect ? desiredCursorVisibility : CursorVisibility.Invisible;
			}
			set {
				if (desiredCursorVisibility != value) {
					desiredCursorVisibility = value;
					if (HasFocus) {
						Application.Driver.SetCursorVisibility (DesiredCursorVisibility);
					}
				}
			}
		}

		/// <summary>
		/// Creates a new tree view with absolute positioning. 
		/// Use <see cref="AddObjects(IEnumerable{T})"/> to set set root objects for the tree.
		/// Children will not be rendered until you set <see cref="TreeBuilder"/>.
		/// </summary>
		public TreeView () : base ()
		{
			CanFocus = true;

			// Things this view knows how to do
			AddCommand (Command.PageUp, () => { MovePageUp (false); return true; });
			AddCommand (Command.PageDown, () => { MovePageDown (false); return true; });
			AddCommand (Command.PageUpExtend, () => { MovePageUp (true); return true; });
			AddCommand (Command.PageDownExtend, () => { MovePageDown (true); return true; });
			AddCommand (Command.Expand, () => { Expand (); return true; });
			AddCommand (Command.ExpandAll, () => { ExpandAll (SelectedObject); return true; });
			AddCommand (Command.Collapse, () => { CursorLeft (false); return true; });
			AddCommand (Command.CollapseAll, () => { CursorLeft (true); return true; });
			AddCommand (Command.LineUp, () => { AdjustSelection (-1, false); return true; });
			AddCommand (Command.LineUpExtend, () => { AdjustSelection (-1, true); return true; });
			AddCommand (Command.LineUpToFirstBranch, () => { AdjustSelectionToBranchStart (); return true; });

			AddCommand (Command.LineDown, () => { AdjustSelection (1, false); return true; });
			AddCommand (Command.LineDownExtend, () => { AdjustSelection (1, true); return true; });
			AddCommand (Command.LineDownToLastBranch, () => { AdjustSelectionToBranchEnd (); return true; });

			AddCommand (Command.TopHome, () => { GoToFirst (); return true; });
			AddCommand (Command.BottomEnd, () => { GoToEnd (); return true; });
			AddCommand (Command.SelectAll, () => { SelectAll (); return true; });

			AddCommand (Command.ScrollUp, () => { ScrollUp (); return true; });
			AddCommand (Command.ScrollDown, () => { ScrollDown (); return true; });
			AddCommand (Command.Accept, () => { ActivateSelectedObjectIfAny (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.PageUp | Key.ShiftMask, Command.PageUpExtend);
			AddKeyBinding (Key.PageDown | Key.ShiftMask, Command.PageDownExtend);
			AddKeyBinding (Key.CursorRight, Command.Expand);
			AddKeyBinding (Key.CursorRight | Key.CtrlMask, Command.ExpandAll);
			AddKeyBinding (Key.CursorLeft, Command.Collapse);
			AddKeyBinding (Key.CursorLeft | Key.CtrlMask, Command.CollapseAll);

			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorUp | Key.ShiftMask, Command.LineUpExtend);
			AddKeyBinding (Key.CursorUp | Key.CtrlMask, Command.LineUpToFirstBranch);

			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.CursorDown | Key.ShiftMask, Command.LineDownExtend);
			AddKeyBinding (Key.CursorDown | Key.CtrlMask, Command.LineDownToLastBranch);

			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.A | Key.CtrlMask, Command.SelectAll);
			AddKeyBinding (ObjectActivationKey, Command.Accept);
		}

		/// <summary>
		/// Initialises <see cref="TreeBuilder"/>.Creates a new tree view with absolute 
		/// positioning. Use <see cref="AddObjects(IEnumerable{T})"/> to set set root 
		/// objects for the tree.
		/// </summary>
		public TreeView (ITreeBuilder<T> builder) : this ()
		{
			TreeBuilder = builder;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

			if (SelectedObject == null && Objects.Any ()) {
				SelectedObject = Objects.First ();
			}

			return base.OnEnter (view);
		}

		/// <summary>
		/// Adds a new root level object unless it is already a root of the tree.
		/// </summary>
		/// <param name="o"></param>
		public void AddObject (T o)
		{
			if (!roots.ContainsKey (o)) {
				roots.Add (o, new Branch<T> (this, null, o));
				InvalidateLineMap ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Removes all objects from the tree and clears <see cref="SelectedObject"/>.
		/// </summary>
		public void ClearObjects ()
		{
			SelectedObject = default (T);
			multiSelectedRegions.Clear ();
			roots = new Dictionary<T, Branch<T>> ();
			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Removes the given root object from the tree
		/// </summary>
		/// <remarks>If <paramref name="o"/> is the currently <see cref="SelectedObject"/> then the
		/// selection is cleared</remarks>.
		/// <param name="o"></param>
		public void Remove (T o)
		{
			if (roots.ContainsKey (o)) {
				roots.Remove (o);
				InvalidateLineMap ();
				SetNeedsDisplay ();

				if (Equals (SelectedObject, o)) {
					SelectedObject = default (T);
				}
			}
		}

		/// <summary>
		/// Adds many new root level objects. Objects that are already root objects are ignored.
		/// </summary>
		/// <param name="collection">Objects to add as new root level objects.</param>.\
		public void AddObjects (IEnumerable<T> collection)
		{
			bool objectsAdded = false;

			foreach (var o in collection) {
				if (!roots.ContainsKey (o)) {
					roots.Add (o, new Branch<T> (this, null, o));
					objectsAdded = true;
				}
			}

			if (objectsAdded) {
				InvalidateLineMap ();
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Refreshes the state of the object <paramref name="o"/> in the tree. This will 
		/// recompute children, string representation etc.
		/// </summary>
		/// <remarks>This has no effect if the object is not exposed in the tree.</remarks>
		/// <param name="o"></param>
		/// <param name="startAtTop">True to also refresh all ancestors of the objects branch 
		/// (starting with the root). False to refresh only the passed node.</param>
		public void RefreshObject (T o, bool startAtTop = false)
		{
			var branch = ObjectToBranch (o);
			if (branch != null) {
				branch.Refresh (startAtTop);
				InvalidateLineMap ();
				SetNeedsDisplay ();
			}

		}

		/// <summary>
		/// Rebuilds the tree structure for all exposed objects starting with the root objects.
		/// Call this method when you know there are changes to the tree but don't know which 
		/// objects have changed (otherwise use <see cref="RefreshObject(T, bool)"/>).
		/// </summary>
		public void RebuildTree ()
		{
			foreach (var branch in roots.Values) {
				branch.Rebuild ();
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Returns the currently expanded children of the passed object. Returns an empty
		/// collection if the branch is not exposed or not expanded.
		/// </summary>
		/// <param name="o">An object in the tree.</param>
		/// <returns></returns>
		public IEnumerable<T> GetChildren (T o)
		{
			var branch = ObjectToBranch (o);

			if (branch == null || !branch.IsExpanded) {
				return new T [0];
			}

			return branch.ChildBranches?.Values?.Select (b => b.Model)?.ToArray () ?? new T [0];
		}
		/// <summary>
		/// Returns the parent object of <paramref name="o"/> in the tree. Returns null if 
		/// the object is not exposed in the tree.
		/// </summary>
		/// <param name="o">An object in the tree.</param>
		/// <returns></returns>
		public T GetParent (T o)
		{
			return ObjectToBranch (o)?.Parent?.Model;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (roots == null) {
				return;
			}

			if (TreeBuilder == null) {
				Move (0, 0);
				Driver.AddStr (NoBuilderError);
				return;
			}

			var map = BuildLineMap ();

			for (int line = 0; line < bounds.Height; line++) {

				var idxToRender = ScrollOffsetVertical + line;

				// Is there part of the tree view to render?
				if (idxToRender < map.Count) {
					// Render the line
					map.ElementAt (idxToRender).Draw (Driver, ColorScheme, line, bounds.Width);
				} else {

					// Else clear the line to prevent stale symbols due to scrolling etc
					Move (0, line);
					Driver.SetAttribute (GetNormalColor ());
					Driver.AddStr (new string (' ', bounds.Width));
				}
			}
		}

		/// <summary>
		/// Returns the index of the object <paramref name="o"/> if it is currently exposed (it's 
		/// parent(s) have been expanded). This can be used with <see cref="ScrollOffsetVertical"/>
		/// and <see cref="View.SetNeedsDisplay()"/> to scroll to a specific object.
		/// </summary>
		/// <remarks>Uses the Equals method and returns the first index at which the object is found
		/// or -1 if it is not found.</remarks>
		/// <param name="o">An object that appears in your tree and is currently exposed.</param>
		/// <returns>The index the object was found at or -1 if it is not currently revealed or
		/// not in the tree at all.</returns>
		public int GetScrollOffsetOf (T o)
		{
			var map = BuildLineMap ();
			for (int i = 0; i < map.Count; i++) {
				if (map.ElementAt (i).Model.Equals (o)) {
					return i;
				}
			}

			//object not found
			return -1;
		}

		/// <summary>
		/// Returns the maximum width line in the tree including prefix and expansion symbols.
		/// </summary>
		/// <param name="visible">True to consider only rows currently visible (based on window
		/// bounds and <see cref="ScrollOffsetVertical"/>. False to calculate the width of 
		/// every exposed branch in the tree.</param>
		/// <returns></returns>
		public int GetContentWidth (bool visible)
		{
			var map = BuildLineMap ();

			if (map.Count == 0) {
				return 0;
			}

			if (visible) {

				//Somehow we managed to scroll off the end of the control
				if (ScrollOffsetVertical >= map.Count) {
					return 0;
				}

				// If control has no height to it then there is no visible area for content
				if (Bounds.Height == 0) {
					return 0;
				}

				return map.Skip (ScrollOffsetVertical).Take (Bounds.Height).Max (b => b.GetWidth (Driver));
			} else {

				return map.Max (b => b.GetWidth (Driver));
			}
		}

		/// <summary>
		/// Calculates all currently visible/expanded branches (including leafs) and outputs them 
		/// by index from the top of the screen.
		/// </summary>
		/// <remarks>Index 0 of the returned array is the first item that should be visible in the
		/// top of the control, index 1 is the next etc.</remarks>
		/// <returns></returns>
		private IReadOnlyCollection<Branch<T>> BuildLineMap ()
		{
			if (cachedLineMap != null) {
				return cachedLineMap;
			}

			List<Branch<T>> toReturn = new List<Branch<T>> ();

			foreach (var root in roots.Values) {
				toReturn.AddRange (AddToLineMap (root));
			}

			cachedLineMap = new ReadOnlyCollection<Branch<T>> (toReturn);

			// Update the collection used for search-typing
			KeystrokeNavigator.Collection = cachedLineMap.Select (b => AspectGetter (b.Model)).ToArray ();
			return cachedLineMap;
		}

		private IEnumerable<Branch<T>> AddToLineMap (Branch<T> currentBranch)
		{
			yield return currentBranch;

			if (currentBranch.IsExpanded) {
				foreach (var subBranch in currentBranch.ChildBranches.Values) {
					foreach (var sub in AddToLineMap (subBranch)) {
						yield return sub;
					}
				}
			}
		}

		/// <summary>
		/// Gets the <see cref="CollectionNavigator"/> that searches the <see cref="Objects"/> collection as
		/// the user types.
		/// </summary>
		public CollectionNavigator KeystrokeNavigator { get; private set; } = new CollectionNavigator ();

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (!Enabled) {
				return false;
			}

			try {
				// First of all deal with any registered keybindings
				var result = InvokeKeybindings (keyEvent);
				if (result != null) {
					return (bool)result;
				}

				// If not a keybinding, is the key a searchable key press?
				if (CollectionNavigator.IsCompatibleKey (keyEvent) && AllowLetterBasedNavigation) {
					IReadOnlyCollection<Branch<T>> map;

					// If there has been a call to InvalidateMap since the last time
					// we need a new one to reflect the new exposed tree state
					map = BuildLineMap ();

					// Find the current selected object within the tree
					var current = map.IndexOf (b => b.Model == SelectedObject);
					var newIndex = KeystrokeNavigator?.GetNextMatchingItem (current, (char)keyEvent.KeyValue);

					if (newIndex is int && newIndex != -1) {
						SelectedObject = map.ElementAt ((int)newIndex).Model;
						EnsureVisible (selectedObject);
						SetNeedsDisplay ();
						return true;
					}
				}
			} finally {
				PositionCursor ();
			}

			return base.ProcessKey (keyEvent);
		}

		/// <summary>
		/// <para>Triggers the <see cref="ObjectActivated"/> event with the <see cref="SelectedObject"/>.</para>
		/// 
		/// <para>This method also ensures that the selected object is visible.</para>
		/// </summary>
		public void ActivateSelectedObjectIfAny ()
		{
			var o = SelectedObject;

			if (o != null) {
				OnObjectActivated (new ObjectActivatedEventArgs<T> (this, o));
				PositionCursor ();
			}
		}

		/// <summary>
		/// <para>
		/// Returns the Y coordinate within the <see cref="View.Bounds"/> of the
		/// tree at which <paramref name="toFind"/> would be displayed or null if
		/// it is not currently exposed (e.g. its parent is collapsed).
		/// </para>
		/// <para>
		/// Note that the returned value can be negative if the TreeView is scrolled
		/// down and the <paramref name="toFind"/> object is off the top of the view.
		/// </para>
		/// </summary>
		/// <param name="toFind"></param>
		/// <returns></returns>
		public int? GetObjectRow (T toFind)
		{
			var idx = BuildLineMap ().IndexOf (o => o.Model.Equals (toFind));

			if (idx == -1)
				return null;

			return idx - ScrollOffsetVertical;
		}

		/// <summary>
		/// <para>Moves the <see cref="SelectedObject"/> to the next item that begins with <paramref name="character"/>.</para>
		/// <para>This method will loop back to the start of the tree if reaching the end without finding a match.</para>
		/// </summary>
		/// <param name="character">The first character of the next item you want selected.</param>
		/// <param name="caseSensitivity">Case sensitivity of the search.</param>
		public void AdjustSelectionToNextItemBeginningWith (char character, StringComparison caseSensitivity = StringComparison.CurrentCultureIgnoreCase)
		{
			// search for next branch that begins with that letter
			var characterAsStr = character.ToString ();
			AdjustSelectionToNext (b => AspectGetter (b.Model).StartsWith (characterAsStr, caseSensitivity));

			PositionCursor ();
		}

		/// <summary>
		/// Moves the selection up by the height of the control (1 page).
		/// </summary>
		/// <param name="expandSelection">True if the navigation should add the covered nodes to the selected current selection.</param>
		/// <exception cref="NotImplementedException"></exception>
		public void MovePageUp (bool expandSelection = false)
		{
			AdjustSelection (-Bounds.Height, expandSelection);
		}

		/// <summary>
		/// Moves the selection down by the height of the control (1 page).
		/// </summary>
		/// <param name="expandSelection">True if the navigation should add the covered nodes to the selected current selection.</param>
		/// <exception cref="NotImplementedException"></exception>
		public void MovePageDown (bool expandSelection = false)
		{
			AdjustSelection (Bounds.Height, expandSelection);
		}

		/// <summary>
		/// Scrolls the view area down a single line without changing the current selection.
		/// </summary>
		public void ScrollDown ()
		{
			if (ScrollOffsetVertical <= ContentHeight - 2) {
				ScrollOffsetVertical++;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Scrolls the view area up a single line without changing the current selection.
		/// </summary>
		public void ScrollUp ()
		{
			if (scrollOffsetVertical > 0) {
				ScrollOffsetVertical--;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Raises the <see cref="ObjectActivated"/> event.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnObjectActivated (ObjectActivatedEventArgs<T> e)
		{
			ObjectActivated?.Invoke (this, e);
		}

		/// <summary>
		/// Returns the object in the tree list that is currently visible.
		/// at the provided row. Returns null if no object is at that location.
		/// <remarks>
		/// </remarks>
		/// If you have screen coordinates then use <see cref="View.ScreenToView(int, int)"/>
		/// to translate these into the client area of the <see cref="TreeView{T}"/>.
		/// </summary>
		/// <param name="row">The row of the <see cref="View.Bounds"/> of the <see cref="TreeView{T}"/>.</param>
		/// <returns>The object currently displayed on this row or null.</returns>
		public T GetObjectOnRow (int row)
		{
			return HitTest (row)?.Model;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			// If it is not an event we care about
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) &&
				!me.Flags.HasFlag (ObjectActivationButton ?? MouseFlags.Button1DoubleClicked) &&
				!me.Flags.HasFlag (MouseFlags.WheeledDown) &&
				!me.Flags.HasFlag (MouseFlags.WheeledUp) &&
				!me.Flags.HasFlag (MouseFlags.WheeledRight) &&
				!me.Flags.HasFlag (MouseFlags.WheeledLeft)) {

				// do nothing
				return false;
			}

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (me.Flags == MouseFlags.WheeledDown) {

				ScrollDown ();

				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				ScrollUp ();

				return true;
			}

			if (me.Flags == MouseFlags.WheeledRight) {

				ScrollOffsetHorizontal++;
				SetNeedsDisplay ();

				return true;
			} else if (me.Flags == MouseFlags.WheeledLeft) {
				ScrollOffsetHorizontal--;
				SetNeedsDisplay ();

				return true;
			}

			if (me.Flags.HasFlag (MouseFlags.Button1Clicked)) {

				// The line they clicked on a branch
				var clickedBranch = HitTest (me.Y);

				if (clickedBranch == null) {
					return false;
				}

				bool isExpandToggleAttempt = clickedBranch.IsHitOnExpandableSymbol (Driver, me.X);

				// If we are already selected (double click)
				if (Equals (SelectedObject, clickedBranch.Model)) {
					isExpandToggleAttempt = true;
				}

				// if they clicked on the +/- expansion symbol
				if (isExpandToggleAttempt) {

					if (clickedBranch.IsExpanded) {
						clickedBranch.Collapse ();
						InvalidateLineMap ();
					} else
					if (clickedBranch.CanExpand ()) {
						clickedBranch.Expand ();
						InvalidateLineMap ();
					} else {
						SelectedObject = clickedBranch.Model; // It is a leaf node
						multiSelectedRegions.Clear ();
					}
				} else {
					// It is a first click somewhere in the current line that doesn't look like an expansion/collapse attempt
					SelectedObject = clickedBranch.Model;
					multiSelectedRegions.Clear ();
				}

				SetNeedsDisplay ();
				return true;
			}

			// If it is activation via mouse (e.g. double click)
			if (ObjectActivationButton.HasValue && me.Flags.HasFlag (ObjectActivationButton.Value)) {
				// The line they clicked on a branch
				var clickedBranch = HitTest (me.Y);

				if (clickedBranch == null) {
					return false;
				}

				// Double click changes the selection to the clicked node as well as triggering
				// activation otherwise it feels wierd
				SelectedObject = clickedBranch.Model;
				SetNeedsDisplay ();

				// trigger activation event				
				OnObjectActivated (new ObjectActivatedEventArgs<T> (this, clickedBranch.Model));

				// mouse event is handled.
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns the branch at the given <paramref name="y"/> client
		/// coordinate e.g. following a click event.
		/// </summary>
		/// <param name="y">Client Y position in the controls bounds.</param>
		/// <returns>The clicked branch or null if outside of tree region.</returns>
		private Branch<T> HitTest (int y)
		{
			var map = BuildLineMap ();

			var idx = y + ScrollOffsetVertical;

			// click is outside any visible nodes
			if (idx < 0 || idx >= map.Count) {
				return null;
			}

			// The line they clicked on
			return map.ElementAt (idx);
		}

		/// <summary>
		/// Positions the cursor at the start of the selected objects line (if visible).
		/// </summary>
		public override void PositionCursor ()
		{
			if (CanFocus && HasFocus && Visible && SelectedObject != null) {

				var map = BuildLineMap ();
				var idx = map.IndexOf (b => b.Model.Equals (SelectedObject));

				// if currently selected line is visible
				if (idx - ScrollOffsetVertical >= 0 && idx - ScrollOffsetVertical < Bounds.Height) {
					Move (0, idx - ScrollOffsetVertical);
				} else {
					base.PositionCursor ();
				}

			} else {
				base.PositionCursor ();
			}
		}

		/// <summary>
		/// Determines systems behaviour when the left arrow key is pressed. Default behaviour is
		/// to collapse the current tree node if possible otherwise changes selection to current 
		/// branches parent.
		/// </summary>
		protected virtual void CursorLeft (bool ctrl)
		{
			if (IsExpanded (SelectedObject)) {

				if (ctrl) {
					CollapseAll (SelectedObject);
				} else {
					Collapse (SelectedObject);
				}
			} else {
				var parent = GetParent (SelectedObject);

				if (parent != null) {
					SelectedObject = parent;
					AdjustSelection (0);
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the first root object and resets 
		/// the <see cref="ScrollOffsetVertical"/> to 0.
		/// </summary>
		public void GoToFirst ()
		{
			ScrollOffsetVertical = 0;
			SelectedObject = roots.Keys.FirstOrDefault ();

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to the last object in the tree and scrolls so
		/// that it is visible.
		/// </summary>
		public void GoToEnd ()
		{
			var map = BuildLineMap ();
			ScrollOffsetVertical = Math.Max (0, map.Count - Bounds.Height + 1);
			SelectedObject = map.LastOrDefault ()?.Model;

			SetNeedsDisplay ();
		}

		/// <summary>
		/// Changes the <see cref="SelectedObject"/> to <paramref name="toSelect"/> and scrolls to ensure
		/// it is visible. Has no effect if <paramref name="toSelect"/> is not exposed in the tree (e.g. 
		/// its parents are collapsed).
		/// </summary>
		/// <param name="toSelect"></param>
		public void GoTo (T toSelect)
		{
			if (ObjectToBranch (toSelect) == null) {
				return;
			}

			SelectedObject = toSelect;
			EnsureVisible (toSelect);
			SetNeedsDisplay ();
		}

		/// <summary>
		/// The number of screen lines to move the currently selected object by. Supports negative values.
		/// <paramref name="offset"/>. Each branch occupies 1 line on screen.
		/// </summary>
		/// <remarks>If nothing is currently selected or the selected object is no longer in the tree
		/// then the first object in the tree is selected instead.</remarks>
		/// <param name="offset">Positive to move the selection down the screen, negative to move it up</param>
		/// <param name="expandSelection">True to expand the selection (assuming 
		/// <see cref="MultiSelect"/> is enabled). False to replace.</param>
		public void AdjustSelection (int offset, bool expandSelection = false)
		{
			// if it is not a shift click or we don't allow multi select
			if (!expandSelection || !MultiSelect) {
				multiSelectedRegions.Clear ();
			}

			if (SelectedObject == null) {
				SelectedObject = roots.Keys.FirstOrDefault ();
			} else {
				var map = BuildLineMap ();

				var idx = map.IndexOf (b => b.Model.Equals (SelectedObject));

				if (idx == -1) {
					// The current selection has disapeared!
					SelectedObject = roots.Keys.FirstOrDefault ();
				} else {
					var newIdx = Math.Min (Math.Max (0, idx + offset), map.Count - 1);

					var newBranch = map.ElementAt (newIdx);

					// If it is a multi selection
					if (expandSelection && MultiSelect) {
						if (multiSelectedRegions.Any ()) {
							// expand the existing head selection
							var head = multiSelectedRegions.Pop ();
							multiSelectedRegions.Push (new TreeSelection<T> (head.Origin, newIdx, map));
						} else {
							// or start a new multi selection region
							multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt (idx), newIdx, map));
						}
					}

					SelectedObject = newBranch.Model;

					EnsureVisible (SelectedObject);
				}
			}
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Moves the selection to the first child in the currently selected level.
		/// </summary>
		public void AdjustSelectionToBranchStart ()
		{
			var o = SelectedObject;
			if (o == null) {
				return;
			}

			var map = BuildLineMap ();

			int currentIdx = map.IndexOf (b => Equals (b.Model, o));

			if (currentIdx == -1) {
				return;
			}

			var currentBranch = map.ElementAt (currentIdx);
			var next = currentBranch;

			for (; currentIdx >= 0; currentIdx--) {
				//if it is the beginning of the current depth of branch
				if (currentBranch.Depth != next.Depth) {

					SelectedObject = currentBranch.Model;
					EnsureVisible (currentBranch.Model);
					SetNeedsDisplay ();
					return;
				}

				// look at next branch up for consideration
				currentBranch = next;
				next = map.ElementAt (currentIdx);
			}

			// We ran all the way to top of tree
			GoToFirst ();
		}

		/// <summary>
		/// Moves the selection to the last child in the currently selected level.
		/// </summary>
		public void AdjustSelectionToBranchEnd ()
		{
			var o = SelectedObject;
			if (o == null) {
				return;
			}

			var map = BuildLineMap ();

			int currentIdx = map.IndexOf (b => Equals (b.Model, o));

			if (currentIdx == -1) {
				return;
			}

			var currentBranch = map.ElementAt (currentIdx);
			var next = currentBranch;

			for (; currentIdx < map.Count; currentIdx++) {
				//if it is the end of the current depth of branch
				if (currentBranch.Depth != next.Depth) {

					SelectedObject = currentBranch.Model;
					EnsureVisible (currentBranch.Model);
					SetNeedsDisplay ();
					return;
				}

				// look at next branch for consideration
				currentBranch = next;
				next = map.ElementAt (currentIdx);
			}
			GoToEnd ();
		}

		/// <summary>
		/// Sets the selection to the next branch that matches the <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate"></param>
		private void AdjustSelectionToNext (Func<Branch<T>, bool> predicate)
		{
			var map = BuildLineMap ();

			// empty map means we can't select anything anyway
			if (map.Count == 0) {
				return;
			}

			// Start searching from the first element in the map
			var idxStart = 0;

			// or the current selected branch
			if (SelectedObject != null) {
				idxStart = map.IndexOf (b => Equals (b.Model, SelectedObject));
			}

			// if currently selected object mysteriously vanished, search from beginning
			if (idxStart == -1) {
				idxStart = 0;
			}

			// loop around all indexes and back to first index
			for (int idxCur = (idxStart + 1) % map.Count; idxCur != idxStart; idxCur = (idxCur + 1) % map.Count) {
				if (predicate (map.ElementAt (idxCur))) {
					SelectedObject = map.ElementAt (idxCur).Model;
					EnsureVisible (map.ElementAt (idxCur).Model);
					SetNeedsDisplay ();
					return;
				}
			}
		}

		/// <summary>
		/// Adjusts the <see cref="ScrollOffsetVertical"/> to ensure the given
		/// <paramref name="model"/> is visible. Has no effect if already visible.
		/// </summary>
		public void EnsureVisible (T model)
		{
			var map = BuildLineMap ();

			var idx = map.IndexOf (b => Equals (b.Model, model));

			if (idx == -1) {
				return;
			}

			/*this -1 allows for possible horizontal scroll bar in the last row of the control*/
			int leaveSpace = Style.LeaveLastRow ? 1 : 0;

			if (idx < ScrollOffsetVertical) {
				//if user has scrolled up too far to see their selection
				ScrollOffsetVertical = idx;
			} else if (idx >= ScrollOffsetVertical + Bounds.Height - leaveSpace) {

				//if user has scrolled off bottom of visible tree
				ScrollOffsetVertical = Math.Max (0, (idx + 1) - (Bounds.Height - leaveSpace));
			}
		}

		/// <summary>
		/// Expands the currently <see cref="SelectedObject"/>.
		/// </summary>
		public void Expand ()
		{
			Expand (SelectedObject);
		}

		/// <summary>
		/// Expands the supplied object if it is contained in the tree (either as a root object or 
		/// as an exposed branch object).
		/// </summary>
		/// <param name="toExpand">The object to expand.</param>
		public void Expand (T toExpand)
		{
			if (toExpand == null) {
				return;
			}

			ObjectToBranch (toExpand)?.Expand ();
			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Expands the supplied object and all child objects.
		/// </summary>
		/// <param name="toExpand">The object to expand.</param>
		public void ExpandAll (T toExpand)
		{
			if (toExpand == null) {
				return;
			}

			ObjectToBranch (toExpand)?.ExpandAll ();
			InvalidateLineMap ();
			SetNeedsDisplay ();
		}
		/// <summary>
		/// Fully expands all nodes in the tree, if the tree is very big and built dynamically this
		/// may take a while (e.g. for file system).
		/// </summary>
		public void ExpandAll ()
		{
			foreach (var item in roots) {
				item.Value.ExpandAll ();
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}
		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and can be
		/// expanded otherwise false.
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool CanExpand (T o)
		{
			return ObjectToBranch (o)?.CanExpand () ?? false;
		}

		/// <summary>
		/// Returns true if the given object <paramref name="o"/> is exposed in the tree and 
		/// expanded otherwise false.
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public bool IsExpanded (T o)
		{
			return ObjectToBranch (o)?.IsExpanded ?? false;
		}

		/// <summary>
		/// Collapses the <see cref="SelectedObject"/>
		/// </summary>
		public void Collapse ()
		{
			Collapse (selectedObject);
		}

		/// <summary>
		/// Collapses the supplied object if it is currently expanded .
		/// </summary>
		/// <param name="toCollapse">The object to collapse.</param>
		public void Collapse (T toCollapse)
		{
			CollapseImpl (toCollapse, false);
		}

		/// <summary>
		/// Collapses the supplied object if it is currently expanded. Also collapses all children
		/// branches (this will only become apparent when/if the user expands it again).
		/// </summary>
		/// <param name="toCollapse">The object to collapse.</param>
		public void CollapseAll (T toCollapse)
		{
			CollapseImpl (toCollapse, true);
		}

		/// <summary>
		/// Collapses all root nodes in the tree.
		/// </summary>
		public void CollapseAll ()
		{
			foreach (var item in roots) {
				item.Value.Collapse ();
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Implementation of <see cref="Collapse(T)"/> and <see cref="CollapseAll(T)"/>. Performs
		/// operation and updates selection if disapeared.
		/// </summary>
		/// <param name="toCollapse"></param>
		/// <param name="all"></param>
		protected void CollapseImpl (T toCollapse, bool all)
		{
			if (toCollapse == null) {
				return;
			}

			var branch = ObjectToBranch (toCollapse);

			// Nothing to collapse
			if (branch == null) {
				return;
			}

			if (all) {
				branch.CollapseAll ();
			} else {
				branch.Collapse ();
			}

			if (SelectedObject != null && ObjectToBranch (SelectedObject) == null) {
				// If the old selection suddenly became invalid then clear it
				SelectedObject = null;
			}

			InvalidateLineMap ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Clears any cached results of <see cref="BuildLineMap"/>
		/// </summary>
		protected void InvalidateLineMap ()
		{
			cachedLineMap = null;
		}

		/// <summary>
		/// Returns the corresponding <see cref="Branch{T}"/> in the tree for
		/// <paramref name="toFind"/>. This will not work for objects hidden
		/// by their parent being collapsed.
		/// </summary>
		/// <param name="toFind"></param>
		/// <returns>The branch for <paramref name="toFind"/> or null if it is not currently 
		/// exposed in the tree.</returns>
		private Branch<T> ObjectToBranch (T toFind)
		{
			return BuildLineMap ().FirstOrDefault (o => o.Model.Equals (toFind));
		}

		/// <summary>
		/// Returns true if the <paramref name="model"/> is either the 
		/// <see cref="SelectedObject"/> or part of a <see cref="MultiSelect"/>.
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public bool IsSelected (T model)
		{
			return Equals (SelectedObject, model) ||
				(MultiSelect && multiSelectedRegions.Any (s => s.Contains (model)));
		}

		/// <summary>
		/// Returns <see cref="SelectedObject"/> (if not null) and all multi selected objects if 
		/// <see cref="MultiSelect"/> is true
		/// </summary>
		/// <returns></returns>
		public IEnumerable<T> GetAllSelectedObjects ()
		{
			var map = BuildLineMap ();

			// To determine multi selected objects, start with the line map, that avoids yielding 
			// hidden nodes that were selected then the parent collapsed e.g. programmatically or
			// with mouse click
			if (MultiSelect) {
				foreach (var m in map.Select (b => b.Model).Where (IsSelected)) {
					yield return m;
				}
			} else {
				if (SelectedObject != null) {
					yield return SelectedObject;
				}
			}
		}

		/// <summary>
		/// Selects all objects in the tree when <see cref="MultiSelect"/> is enabled otherwise 
		/// does nothing.
		/// </summary>
		public void SelectAll ()
		{
			if (!MultiSelect) {
				return;
			}

			multiSelectedRegions.Clear ();

			var map = BuildLineMap ();

			if (map.Count == 0) {
				return;
			}

			multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt (0), map.Count, map));
			SetNeedsDisplay ();

			OnSelectionChanged (new SelectionChangedEventArgs<T> (this, SelectedObject, SelectedObject));
		}

		/// <summary>
		/// Raises the SelectionChanged event.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnSelectionChanged (SelectionChangedEventArgs<T> e)
		{
			SelectionChanged?.Invoke (this, e);
		}
	}

	class TreeSelection<T> where T : class {

		public Branch<T> Origin { get; }

		private HashSet<T> included = new HashSet<T> ();

		/// <summary>
		/// Creates a new selection between two branches in the tree
		/// </summary>
		/// <param name="from"></param>
		/// <param name="toIndex"></param>
		/// <param name="map"></param>
		public TreeSelection (Branch<T> from, int toIndex, IReadOnlyCollection<Branch<T>> map)
		{
			Origin = from;
			included.Add (Origin.Model);

			var oldIdx = map.IndexOf (from);

			var lowIndex = Math.Min (oldIdx, toIndex);
			var highIndex = Math.Max (oldIdx, toIndex);

			// Select everything between the old and new indexes
			foreach (var alsoInclude in map.Skip (lowIndex).Take (highIndex - lowIndex)) {
				included.Add (alsoInclude.Model);
			}

		}
		public bool Contains (T model)
		{
			return included.Contains (model);
		}
	}
}