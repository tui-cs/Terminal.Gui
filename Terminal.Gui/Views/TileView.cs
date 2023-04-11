using NStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// A <see cref="View"/> consisting of a moveable bar that divides
	/// the display area into resizeable <see cref="Tiles"/>.
	/// </summary>
	public class TileView : View {
		TileView parentTileView;

		/// <summary>
		/// The keyboard key that the user can press to toggle resizing
		/// of splitter lines.  Mouse drag splitting is always enabled.
		/// </summary>
		public Key ToggleResizable { get; set; } = Key.CtrlMask | Key.F10;

		/// <summary>
		/// A single <see cref="ContentView"/> presented in a <see cref="TileView"/>. To create
		/// new instances use <see cref="TileView.RebuildForTileCount(int)"/> 
		/// or <see cref="TileView.InsertTile(int)"/>.
		/// </summary>
		public class Tile {
			/// <summary>
			/// The <see cref="ContentView"/> that is contained in this <see cref="TileView"/>.
			/// Add new child views to this member for multiple 
			/// <see cref="ContentView"/>s within the <see cref="Tile"/>.
			/// </summary>
			public View ContentView { get; internal set; }

			/// <summary>
			/// Gets or Sets the minimum size you to allow when splitter resizing along
			/// parent <see cref="TileView.Orientation"/> direction.
			/// </summary>
			public int MinSize { get; set; }

			/// <summary>
			/// The text that should be displayed above the <see cref="ContentView"/>. This 
			/// will appear over the splitter line or border (above the view client area).
			/// </summary>
			/// <remarks>
			/// Title are not rendered for root level tiles 
			/// <see cref="Gui.LineStyle"/> is <see cref="LineStyle.None"/>.
			///</remarks>
			public string Title {
				get => _title;
				set {
					if (!OnTitleChanging (_title, value)) {
						var old = _title;
						_title = value;
						OnTitleChanged (old, _title);
						return;
					}
					_title = value;
				}
			}

			private string _title = string.Empty;

			/// <summary>
			/// Called before the <see cref="Title"/> changes. Invokes the <see cref="TitleChanging"/> event, which can be cancelled.
			/// </summary>
			/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
			/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
			/// <returns><c>true</c> if an event handler cancelled the Title change.</returns>
			public virtual bool OnTitleChanging (ustring oldTitle, ustring newTitle)
			{
				var args = new TitleEventArgs (oldTitle, newTitle);
				TitleChanging?.Invoke (this, args);
				return args.Cancel;
			}

			/// <summary>
			/// Event fired when the <see cref="Title"/> is changing. Set <see cref="TitleEventArgs.Cancel"/> to 
			/// <c>true</c> to cancel the Title change.
			/// </summary>
			public event EventHandler<TitleEventArgs> TitleChanging;

			/// <summary>
			/// Called when the <see cref="Title"/> has been changed. Invokes the <see cref="TitleChanged"/> event.
			/// </summary>
			/// <param name="oldTitle">The <see cref="Title"/> that is/has been replaced.</param>
			/// <param name="newTitle">The new <see cref="Title"/> to be replaced.</param>
			public virtual void OnTitleChanged (ustring oldTitle, ustring newTitle)
			{
				var args = new TitleEventArgs (oldTitle, newTitle);
				TitleChanged?.Invoke (this, args);
			}

			/// <summary>
			/// Event fired after the <see cref="Title"/> has been changed. 
			/// </summary>
			public event EventHandler<TitleEventArgs> TitleChanged;

			/// <summary>
			/// Creates a new instance of the <see cref="Tile"/> class.
			/// </summary>
			public Tile ()
			{
				ContentView = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
#if DEBUG_IDISPOSABLE
				ContentView.Data = "Tile.ContentView";
#endif
				Title = string.Empty;
				MinSize = 0;
			}
		}

		List<Tile> tiles;
		private List<Pos> splitterDistances;
		private List<TileViewLineView> splitterLines;

		/// <summary>
		/// The sub sections hosted by the view
		/// </summary>
		public IReadOnlyCollection<Tile> Tiles => tiles.AsReadOnly ();

		/// <summary>
		/// The splitter locations. Note that there will be N-1 splitters where
		/// N is the number of <see cref="Tiles"/>.
		/// </summary>
		public IReadOnlyCollection<Pos> SplitterDistances => splitterDistances.AsReadOnly ();

		private Orientation orientation = Orientation.Vertical;

		/// <summary>
		/// Creates a new instance of the <see cref="TileView"/> class with 
		/// 2 tiles (i.e. left and right).
		/// </summary>
		public TileView () : this (2)
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="TileView"/> class with 
		/// <paramref name="tiles"/> number of tiles.
		/// </summary>
		/// <param name="tiles"></param>
		public TileView (int tiles)
		{
			RebuildForTileCount (tiles);
		}

		/// <summary>
		/// Invoked when any of the <see cref="SplitterDistances"/> is changed.
		/// </summary>
		public event SplitterEventHandler SplitterMoved;

		/// <summary>
		/// Raises the <see cref="SplitterMoved"/> event
		/// </summary>
		protected virtual void OnSplitterMoved (int idx)
		{
			SplitterMoved?.Invoke (this, new SplitterEventArgs (this, idx, splitterDistances [idx]));
		}

		/// <summary>
		/// Scraps all <see cref="Tiles"/> and creates <paramref name="count"/> new tiles
		/// in orientation <see cref="Orientation"/>
		/// </summary>
		/// <param name="count"></param>
		public void RebuildForTileCount (int count)
		{
			tiles = new List<Tile> ();
			splitterDistances = new List<Pos> ();
			if (splitterLines != null) {
				foreach (var sl in splitterLines) {
					sl.Dispose ();
				}
			}
			splitterLines = new List<TileViewLineView> ();

			RemoveAll ();
			foreach (var tile in tiles) {
				tile.ContentView.Dispose ();
				tile.ContentView = null;
			}
			tiles.Clear ();
			splitterDistances.Clear ();

			if (count == 0) {
				return;
			}

			for (int i = 0; i < count; i++) {
				if (i > 0) {
					var currentPos = Pos.Percent ((100 / count) * i);
					splitterDistances.Add (currentPos);
					var line = new TileViewLineView (this, i - 1);
					Add (line);
					splitterLines.Add (line);
				}

				var tile = new Tile ();
				tiles.Add (tile);
				Add (tile.ContentView);
				tile.TitleChanged += (s,e) => SetNeedsDisplay ();
			}

			LayoutSubviews ();
		}

		/// <summary>
		/// Adds a new <see cref="Tile"/> to the collection at <paramref name="idx"/>.
		/// This will also add another splitter line
		/// </summary>
		/// <param name="idx"></param>
		public Tile InsertTile (int idx)
		{
			var oldTiles = Tiles.ToArray ();
			RebuildForTileCount (oldTiles.Length + 1);

			Tile toReturn = null;

			for (int i = 0; i < tiles.Count; i++) {

				if (i != idx) {
					var oldTile = oldTiles [i > idx ? i - 1 : i];

					// remove the new empty View
					Remove (tiles [i].ContentView);
					tiles [i].ContentView.Dispose ();
					tiles [i].ContentView = null;

					// restore old Tile and View
					tiles [i] = oldTile;
					Add (tiles [i].ContentView);
				} else {
					toReturn = tiles [i];
				}
			}
			SetNeedsDisplay ();
			LayoutSubviews ();

			return toReturn;
		}

		/// <summary>
		/// Removes a <see cref="Tiles"/> at the provided <paramref name="idx"/> from
		/// the view. Returns the removed tile or null if already empty.
		/// </summary>
		/// <param name="idx"></param>
		/// <returns></returns>
		public Tile RemoveTile (int idx)
		{
			var oldTiles = Tiles.ToArray ();

			if (idx < 0 || idx >= oldTiles.Length) {
				return null;
			}

			var removed = Tiles.ElementAt (idx);

			RebuildForTileCount (oldTiles.Length - 1);

			for (int i = 0; i < tiles.Count; i++) {

				int oldIdx = i >= idx ? i + 1 : i;
				var oldTile = oldTiles [oldIdx];

				// remove the new empty View
				Remove (tiles [i].ContentView);
				tiles [i].ContentView.Dispose ();
				tiles [i].ContentView = null;

				// restore old Tile and View
				tiles [i] = oldTile;
				Add (tiles [i].ContentView);

			}
			SetNeedsDisplay ();
			LayoutSubviews ();

			return removed;
		}

		///<summary>
		/// Returns the index of the first <see cref="Tile"/> in
		/// <see cref="Tiles"/> which contains <paramref name="toFind"/>.
		///</summary>
		public int IndexOf (View toFind, bool recursive = false)
		{
			for (int i = 0; i < tiles.Count; i++) {
				var v = tiles [i].ContentView;

				if (v == toFind) {
					return i;
				}

				if (v.Subviews.Contains (toFind)) {
					return i;
				}

				if (recursive) {
					if (RecursiveContains (v.Subviews, toFind)) {
						return i;
					}
				}
			}

			return -1;
		}

		private bool RecursiveContains (IEnumerable<View> haystack, View needle)
		{
			foreach (var v in haystack) {
				if (v == needle) {
					return true;
				}

				if (RecursiveContains (v.Subviews, needle)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Orientation of the dividing line (Horizontal or Vertical).
		/// </summary>
		public Orientation Orientation {
			get { return orientation; }
			set {
				orientation = value;
				LayoutSubviews ();
			}
		}
		/// <inheritdoc/>
		public override void LayoutSubviews ()
		{
			var contentArea = Bounds;

			if (HasBorder ()) {
				contentArea = new Rect (
					contentArea.X + 1,
					contentArea.Y + 1,
					Math.Max (0, contentArea.Width - 2),
					Math.Max (0, contentArea.Height - 2));
			}

			Setup (contentArea);
			base.LayoutSubviews ();
		}

		/// <summary>
		/// <para>Attempts to update the <see cref="splitterDistances"/> of line at <paramref name="idx"/>
		/// to the new <paramref name="value"/>. Returns false if the new position is not allowed because of
		/// <see cref="Tile.MinSize"/>, location of other splitters etc.
		/// </para>
		/// <para>Only absolute values (e.g. 10) and percent values (i.e. <see cref="Pos.Percent(float)"/>)
		/// are supported for this property.</para>
		/// </summary>
		public bool SetSplitterPos (int idx, Pos value)
		{
			if (!(value is Pos.PosAbsolute) && !(value is Pos.PosFactor)) {
				throw new ArgumentException ($"Only Percent and Absolute values are supported. Passed value was {value.GetType ().Name}");
			}

			var fullSpace = orientation == Orientation.Vertical ? Bounds.Width : Bounds.Height;

			if (fullSpace != 0 && !IsValidNewSplitterPos (idx, value, fullSpace)) {
				return false;
			}

			splitterDistances [idx] = value;
			GetRootTileView ().LayoutSubviews ();
			OnSplitterMoved (idx);
			return true;
		}

		/// <summary>
		/// The line style to use when drawing the splitter lines.
		/// </summary>
		public LineStyle LineStyle { get; set; } = LineStyle.None;

		/// <summary>
		/// Overridden so no Frames get drawn (BUGBUG: v2 fix this hack)
		/// </summary>
		/// <param name="bounds"></param>
		/// <returns></returns>
		public override bool OnDrawFrames (Rect bounds)
		{
			return false;
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			Clear ();

			base.Redraw (bounds);

			var lc = new LineCanvas ();

			var allLines = GetAllLineViewsRecursively (this);
			var allTitlesToRender = GetAllTitlesToRenderRecursively (this);

			if (IsRootTileView ()) {
				if (HasBorder ()) {

					lc.AddLine (new Point (0, 0), bounds.Width - 1, Orientation.Horizontal, LineStyle);
					lc.AddLine (new Point (0, 0), bounds.Height - 1, Orientation.Vertical, LineStyle);

					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Width + 1, Orientation.Horizontal, LineStyle);
					lc.AddLine (new Point (bounds.Width - 1, bounds.Height - 1), -bounds.Height + 1, Orientation.Vertical, LineStyle);
				}

				foreach (var line in allLines) {
					bool isRoot = splitterLines.Contains (line);

					line.ViewToScreen (0, 0, out var x1, out var y1);
					var origin = ScreenToView (x1, y1);
					var length = line.Orientation == Orientation.Horizontal ?
							line.Frame.Width - 1 :
							line.Frame.Height - 1;

					if (!isRoot) {
						if (line.Orientation == Orientation.Horizontal) {
							origin.X -= 1;
						} else {
							origin.Y -= 1;
						}
						length += 2;
					}

					lc.AddLine (origin, length, line.Orientation, LineStyle);
				}
			}

			Driver.SetAttribute (ColorScheme.Normal);
			foreach (var p in lc.GenerateImage (bounds)) {
				this.AddRune (p.Key.X, p.Key.Y, p.Value);
			}
			
			// Redraw the lines so that focus/drag symbol renders
			foreach (var line in allLines) {
				line.DrawSplitterSymbol ();
			}

			// Draw Titles over Border

			foreach (var titleToRender in allTitlesToRender) {
				var renderAt = titleToRender.GetLocalCoordinateForTitle (this);

				if (renderAt.Y < 0) {
					// If we have no border then root level tiles
					// have nowhere to render their titles.
					continue;
				}

				// TODO: Render with focus color if focused

				var title = titleToRender.GetTrimmedTitle ();

				for (int i = 0; i < title.Length; i++) {
					AddRune (renderAt.X + i, renderAt.Y, title [i]);
				}
			}
		}

		/// <summary>
		/// Converts of <see cref="Tiles"/> element <paramref name="idx"/>
		/// from a regular <see cref="View"/> to a new nested <see cref="TileView"/> 
		/// the specified <paramref name="numberOfPanels"/>.
		/// Returns false if the element already contains a nested view.
		/// </summary>
		/// <remarks>After successful splitting, the old contents will be moved to the 
		/// <paramref name="result"/> <see cref="TileView"/>'s first tile.</remarks>
		/// <param name="idx">The element of <see cref="Tiles"/> that is to be subdivided.</param>
		/// <param name="numberOfPanels">The number of panels that the <see cref="Tile"/> should be split into</param>
		/// <param name="result">The new nested <see cref="TileView"/>.</param>
		/// <returns><see langword="true"/> if a <see cref="View"/> was converted to a new nested
		/// <see cref="TileView"/>. <see langword="false"/> if it was already a nested
		/// <see cref="TileView"/></returns>
		public bool TrySplitTile (int idx, int numberOfPanels, out TileView result)
		{
			// when splitting a view into 2 sub views we will need to migrate
			// the title too
			var tile = tiles [idx];

			var title = tile.Title;
			View toMove = tile.ContentView;

			if (toMove is TileView existing) {
				result = existing;
				return false;
			}

			var newContainer = new TileView (numberOfPanels) {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				parentTileView = this,
			};

			// Take everything out of the View we are moving
			var childViews = toMove.Subviews.ToArray ();
			toMove.RemoveAll ();

			// Remove the view itself and replace it with the new TileView
			Remove (toMove);
			toMove.Dispose ();
			toMove = null;

			Add (newContainer);

			tile.ContentView = newContainer;

			var newTileView1 = newContainer.tiles [0].ContentView;
			// Add the original content into the first view of the new container
			foreach (var childView in childViews) {
				newTileView1.Add (childView);
			}

			// Move the title across too
			newContainer.tiles [0].Title = title;
			tile.Title = string.Empty;

			result = newContainer;
			return true;
		}

		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			bool focusMoved = false;

			if(keyEvent.Key == ToggleResizable) {
				foreach(var l in splitterLines) {

					var iniBefore = l.IsInitialized;
					l.IsInitialized = false;
					l.CanFocus = !l.CanFocus;
					l.IsInitialized = iniBefore;

					if (l.CanFocus && !focusMoved) {
						l.SetFocus ();
						focusMoved = true;
					}
				}
				return true;
			}

			return base.ProcessHotKey (keyEvent);
		}

		private bool IsValidNewSplitterPos (int idx, Pos value, int fullSpace)
		{
			int newSize = value.Anchor (fullSpace);
			bool isGettingBigger = newSize > splitterDistances [idx].Anchor (fullSpace);
			int lastSplitterOrBorder = HasBorder () ? 1 : 0;
			int nextSplitterOrBorder = HasBorder () ? fullSpace - 1 : fullSpace;

			// Cannot move off screen right
			if (newSize >= fullSpace - (HasBorder () ? 1 : 0)) {

				if (isGettingBigger) {
					return false;
				}
			}

			// Cannot move off screen left
			if (newSize < (HasBorder () ? 1 : 0)) {

				if (!isGettingBigger) {
					return false;
				}
			}

			// Do not allow splitter to move left of the one before
			if (idx > 0) {
				int posLeft = splitterDistances [idx - 1].Anchor (fullSpace);

				if (newSize <= posLeft) {
					return false;
				}

				lastSplitterOrBorder = posLeft;
			}

			// Do not allow splitter to move right of the one after
			if (idx + 1 < splitterDistances.Count) {
				int posRight = splitterDistances [idx + 1].Anchor (fullSpace);

				if (newSize >= posRight) {
					return false;
				}
				nextSplitterOrBorder = posRight;
			}

			if (isGettingBigger) {
				var spaceForNext = nextSplitterOrBorder - newSize;

				// space required for the last line itself
				if (idx > 0) {
					spaceForNext--;
				}

				// don't grow if it would take us below min size of right panel
				if (spaceForNext < tiles [idx + 1].MinSize) {
					return false;
				}
			} else {
				var spaceForLast = newSize - lastSplitterOrBorder;

				// space required for the line itself
				if (idx > 0) {
					spaceForLast--;
				}


				// don't shrink if it would take us below min size of left panel
				if (spaceForLast < tiles [idx].MinSize) {
					return false;
				}
			}

			return true;
		}

		private List<TileViewLineView> GetAllLineViewsRecursively (View v)
		{
			var lines = new List<TileViewLineView> ();

			foreach (var sub in v.Subviews) {
				if (sub is TileViewLineView s) {
					if (s.Visible && s.Parent.GetRootTileView () == this) {
						lines.Add (s);
					}
				} else {
					if (sub.Visible) {
						lines.AddRange (GetAllLineViewsRecursively (sub));
					}
				}
			}

			return lines;
		}

		private List<TileTitleToRender> GetAllTitlesToRenderRecursively (TileView v, int depth = 0)
		{
			var titles = new List<TileTitleToRender> ();

			foreach (var sub in v.Tiles) {

				// Don't render titles for invisible stuff!
				if (!sub.ContentView.Visible) {
					continue;
				}

				if (sub.ContentView is TileView subTileView) {
					// Panels with sub split tiles in them can never
					// have their Titles rendered. Instead we dive in
					// and pull up their children as titles
					titles.AddRange (GetAllTitlesToRenderRecursively (subTileView, depth + 1));
				} else {
					if (sub.Title.Length > 0) {
						titles.Add (new TileTitleToRender (v, sub, depth));
					}
				}
			}

			return titles;
		}

		/// <summary>
		/// <para>
		/// <see langword="true"/> if <see cref="TileView"/> is nested within a parent <see cref="TileView"/>
		/// e.g. via the <see cref="TrySplitTile"/>. <see langword="false"/> if it is a root level <see cref="TileView"/>.
		/// </para>
		/// </summary>
		/// <remarks>Note that manually adding one <see cref="TileView"/> to another will not result in a parent/child
		/// relationship and both will still be considered 'root' containers. Always use
		/// <see cref="TrySplitTile(int, int, out TileView)"/> if you want to subdivide a <see cref="TileView"/>.</remarks>
		/// <returns></returns>
		public bool IsRootTileView ()
		{
			return parentTileView == null;
		}

		/// <summary>
		/// Returns the immediate parent <see cref="TileView"/> of this. Note that in case
		/// of deep nesting this might not be the root <see cref="TileView"/>. Returns null
		/// if this instance is not a nested child (created with 
		/// <see cref="TrySplitTile(int, int, out TileView)"/>)
		/// </summary>
		/// <remarks>
		/// Use <see cref="IsRootTileView"/> to determine if the returned value is the root.
		/// </remarks>
		/// <returns></returns>
		public TileView GetParentTileView ()
		{
			return this.parentTileView;
		}
		private TileView GetRootTileView ()
		{
			TileView root = this;

			while (root.parentTileView != null) {
				root = root.parentTileView;
			}

			return root;
		}
		private void Setup (Rect contentArea)
		{
			if (contentArea.IsEmpty || contentArea.Height <= 0 || contentArea.Width <= 0) {
				return;
			}

			for (int i = 0; i < splitterLines.Count; i++) {
				var line = splitterLines [i];

				line.Orientation = Orientation;
				line.Width = orientation == Orientation.Vertical
					? 1 : Dim.Fill ();
				line.Height = orientation == Orientation.Vertical
					? Dim.Fill () : 1;
				line.LineRune = orientation == Orientation.Vertical ?
					Driver.VLine : Driver.HLine;

				if (orientation == Orientation.Vertical) {
					line.X = splitterDistances [i];
					line.Y = 0;
				} else {
					line.Y = splitterDistances [i];
					line.X = 0;
				}

			}

			HideSplittersBasedOnTileVisibility ();

			var visibleTiles = tiles.Where (t => t.ContentView.Visible).ToArray ();
			var visibleSplitterLines = splitterLines.Where (l => l.Visible).ToArray ();

			for (int i = 0; i < visibleTiles.Length; i++) {
				var tile = visibleTiles [i];

				if (Orientation == Orientation.Vertical) {
					tile.ContentView.X = i == 0 ? contentArea.X : Pos.Right (visibleSplitterLines [i - 1]);
					tile.ContentView.Y = contentArea.Y;
					tile.ContentView.Height = contentArea.Height;
					tile.ContentView.Width = GetTileWidthOrHeight (i, Bounds.Width, visibleTiles, visibleSplitterLines);
				} else {
					tile.ContentView.X = contentArea.X;
					tile.ContentView.Y = i == 0 ? contentArea.Y : Pos.Bottom (visibleSplitterLines [i - 1]);
					tile.ContentView.Width = contentArea.Width;
					tile.ContentView.Height = GetTileWidthOrHeight (i, Bounds.Height, visibleTiles, visibleSplitterLines);
				}
			}
		}

		private void HideSplittersBasedOnTileVisibility ()
		{
			if (splitterLines.Count == 0) {
				return;
			}

			foreach (var line in splitterLines) {
				line.Visible = true;
			}

			for (int i = 0; i < tiles.Count; i++) {
				if (!tiles [i].ContentView.Visible) {

					// when a tile is not visible, prefer hiding
					// the splitter on it's left
					var candidate = splitterLines [Math.Max (0, i - 1)];

					// unless that splitter is already hidden
					// e.g. when hiding panels 0 and 1 of a 3 panel 
					// container
					if (candidate.Visible) {
						candidate.Visible = false;
					} else {
						splitterLines [Math.Min (i, splitterLines.Count - 1)].Visible = false;
					}


				}
			}
		}

		private Dim GetTileWidthOrHeight (int i, int space, Tile [] visibleTiles, TileViewLineView [] visibleSplitterLines)
		{
			// last tile
			if (i + 1 >= visibleTiles.Length) {
				return Dim.Fill (HasBorder () ? 1 : 0);
			}

			var nextSplitter = visibleSplitterLines [i];
			var nextSplitterPos = Orientation == Orientation.Vertical ?
				nextSplitter.X : nextSplitter.Y;
			var nextSplitterDistance = nextSplitterPos.Anchor (space);

			var lastSplitter = i >= 1 ? visibleSplitterLines [i - 1] : null;
			var lastSplitterPos = Orientation == Orientation.Vertical ?
				lastSplitter?.X : lastSplitter?.Y;
			var lastSplitterDistance = lastSplitterPos?.Anchor (space) ?? 0;

			var distance = nextSplitterDistance - lastSplitterDistance;

			if (i > 0) {
				return distance - 1;
			}

			return distance - (HasBorder () ? 1 : 0);
		}

		private class TileTitleToRender {
			public TileView Parent { get; }
			public Tile Tile { get; }

			public int Depth { get; }

			public TileTitleToRender (TileView parent, Tile tile, int depth)
			{
				Parent = parent;
				Tile = tile;
				Depth = depth;
			}

			/// <summary>
			/// Translates the <see cref="Tile"/> title location from its local
			/// coordinate space <paramref name="intoCoordinateSpace"/>.
			/// </summary>
			public Point GetLocalCoordinateForTitle (TileView intoCoordinateSpace)
			{
				Tile.ContentView.ViewToScreen (0, 0, out var screenCol, out var screenRow);
				screenRow--;
				return intoCoordinateSpace.ScreenToView (screenCol, screenRow);
			}

			internal string GetTrimmedTitle ()
			{
				Dim spaceDim = Tile.ContentView.Width;

				var spaceAbs = spaceDim.Anchor (Parent.Bounds.Width);

				var title = $" {Tile.Title} ";

				if (title.Length > spaceAbs) {
					return title.Substring (0, spaceAbs);
				}

				return title;
			}
		}

		private class TileViewLineView : LineView {
			public TileView Parent { get; private set; }
			public int Idx { get; }

			Point? dragPosition;
			Pos dragOrignalPos;
			public Point? moveRuneRenderLocation;

			public TileViewLineView (TileView parent, int idx)
			{
				CanFocus = false;
				TabStop = true;

				this.Parent = parent;
				Idx = idx;
				base.AddCommand (Command.Right, () => {
					return MoveSplitter (1, 0);
				});

				base.AddCommand (Command.Left, () => {
					return MoveSplitter (-1, 0);
				});

				base.AddCommand (Command.LineUp, () => {
					return MoveSplitter (0, -1);
				});

				base.AddCommand (Command.LineDown, () => {
					return MoveSplitter (0, 1);
				});

				AddKeyBinding (Key.CursorRight, Command.Right);
				AddKeyBinding (Key.CursorLeft, Command.Left);
				AddKeyBinding (Key.CursorUp, Command.LineUp);
				AddKeyBinding (Key.CursorDown, Command.LineDown);
			}

			public override bool ProcessKey (KeyEvent kb)
			{
				if (!CanFocus || !HasFocus) {
					return base.ProcessKey (kb);
				}

				var result = InvokeKeybindings (kb);
				if (result != null)
					return (bool)result;

				return base.ProcessKey (kb);
			}

			public override void PositionCursor ()
			{
				base.PositionCursor ();
				var location = moveRuneRenderLocation ??
					new Point (Bounds.Width / 2, Bounds.Height / 2);
				Move (location.X, location.Y);
			}

			public override bool OnEnter (View view)
			{
				Driver.SetCursorVisibility (CursorVisibility.Default);
				PositionCursor ();

				return base.OnEnter (view);
			}

			public override void Redraw (Rect bounds)
			{
				base.Redraw (bounds);

				DrawSplitterSymbol ();
			}

			public void DrawSplitterSymbol ()
			{
				if (dragPosition != null || CanFocus) {
					var location = moveRuneRenderLocation ??
						new Point (Bounds.Width / 2, Bounds.Height / 2);

					AddRune (location.X, location.Y, Driver.Diamond);
				}
			}

			public override bool MouseEvent (MouseEvent mouseEvent)
			{
				if (!dragPosition.HasValue && (mouseEvent.Flags == MouseFlags.Button1Pressed)) {

					// Start a Drag
					SetFocus ();
					Application.EnsuresTopOnFront ();

					if (mouseEvent.Flags == MouseFlags.Button1Pressed) {
						dragPosition = new Point (mouseEvent.X, mouseEvent.Y);
						dragOrignalPos = Orientation == Orientation.Horizontal ? Y : X;
						Application.GrabMouse (this);

						if (Orientation == Orientation.Horizontal) {

						} else {
							moveRuneRenderLocation = new Point (0, Math.Max (1, Math.Min (Bounds.Height - 2, mouseEvent.Y)));
						}
					}

					return true;
				} else if (
					dragPosition.HasValue &&
					(mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {

					// Continue Drag

					// how far has user dragged from original location?						
					if (Orientation == Orientation.Horizontal) {
						int dy = mouseEvent.Y - dragPosition.Value.Y;
						Parent.SetSplitterPos (Idx, Offset (Y, dy));
						moveRuneRenderLocation = new Point (mouseEvent.X, 0);
					} else {
						int dx = mouseEvent.X - dragPosition.Value.X;
						Parent.SetSplitterPos (Idx, Offset (X, dx));
						moveRuneRenderLocation = new Point (0, Math.Max (1, Math.Min (Bounds.Height - 2, mouseEvent.Y)));
					}

					Parent.SetNeedsDisplay ();
					return true;
				}

				if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && dragPosition.HasValue) {

					// End Drag

					Application.UngrabMouse ();
					Driver.UncookMouse ();
					FinalisePosition (
						dragOrignalPos,
						Orientation == Orientation.Horizontal ? Y : X);
					dragPosition = null;
					moveRuneRenderLocation = null;
				}

				return false;
			}

			private bool MoveSplitter (int distanceX, int distanceY)
			{
				if (Orientation == Orientation.Vertical) {

					// Cannot move in this direction
					if (distanceX == 0) {
						return false;
					}

					var oldX = X;
					return FinalisePosition (oldX, Offset (X, distanceX));
				} else {

					// Cannot move in this direction
					if (distanceY == 0) {
						return false;
					}

					var oldY = Y;
					return FinalisePosition (oldY, (Pos)Offset (Y, distanceY));
				}
			}

			private Pos Offset (Pos pos, int delta)
			{
				var posAbsolute = pos.Anchor (Orientation == Orientation.Horizontal ?
					Parent.Bounds.Height : Parent.Bounds.Width);

				return posAbsolute + delta;
			}

			/// <summary>
			/// <para>
			/// Moves <see cref="Parent"/> <see cref="TileView.SplitterDistances"/> to 
			/// <see cref="Pos"/> <paramref name="newValue"/> preserving <see cref="Pos"/> format
			/// (absolute / relative) that <paramref name="oldValue"/> had.
			/// </para>
			/// <remarks>This ensures that if splitter location was e.g. 50% before and you move it
			/// to absolute 5 then you end up with 10% (assuming a parent had 50 width). </remarks>
			/// </summary>
			/// <param name="oldValue"></param>
			/// <param name="newValue"></param>
			private bool FinalisePosition (Pos oldValue, Pos newValue)
			{
				if (oldValue is Pos.PosFactor) {
					if (Orientation == Orientation.Horizontal) {
						return Parent.SetSplitterPos (Idx, ConvertToPosFactor (newValue, Parent.Bounds.Height));
					} else {
						return Parent.SetSplitterPos (Idx, ConvertToPosFactor (newValue, Parent.Bounds.Width));
					}
				} else {
					return Parent.SetSplitterPos (Idx, newValue);
				}
			}

			/// <summary>
			/// <para>
			/// Determines the absolute position of <paramref name="p"/> and
			/// returns a <see cref="Pos.PosFactor"/> that describes the percentage of that.
			/// </para>
			/// <para>Effectively turning any <see cref="Pos"/> into a <see cref="Pos.PosFactor"/>
			/// (as if created with <see cref="Pos.Percent(float)"/>)</para>
			/// </summary>
			/// <param name="p">The <see cref="Pos"/> to convert to <see cref="Pos.Percent(float)"/></param>
			/// <param name="parentLength">The Height/Width that <paramref name="p"/> lies within</param>
			/// <returns></returns>
			private Pos ConvertToPosFactor (Pos p, int parentLength)
			{
				// calculate position in the 'middle' of the cell at p distance along parentLength
				float position = p.Anchor (parentLength) + 0.5f;

				return new Pos.PosFactor (position / parentLength);
			}
		}

		private bool HasBorder ()
		{
			return LineStyle != LineStyle.None;
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			foreach (var tile in Tiles) {
				Remove (tile.ContentView);
				tile.ContentView.Dispose ();
			}
			base.Dispose (disposing);
		}

	}

	/// <summary>
	/// Represents a method that will handle splitter events.
	/// </summary>
	public delegate void SplitterEventHandler (object sender, SplitterEventArgs e);
}
