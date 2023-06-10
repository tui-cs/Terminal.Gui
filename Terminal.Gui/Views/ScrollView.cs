//
// ScrollView.cs: ScrollView view.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
//
// TODO:
// - focus in scrollview
// - focus handling in scrollview to auto scroll to focused view
// - Raise events
// - Perhaps allow an option to not display the scrollbar arrow indicators?

using System;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Scrollviews are views that present a window into a virtual space where subviews are added.  Similar to the iOS UIScrollView.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The subviews that are added to this <see cref="Gui.ScrollView"/> are offset by the
	///   <see cref="ContentOffset"/> property.  The view itself is a window into the 
	///   space represented by the <see cref="ContentSize"/>.
	/// </para>
	/// <para>
	///   Use the 
	/// </para>
	/// </remarks>
	public class ScrollView : View {

		// The ContentView is the view that contains the subviews  and content that are being scrolled
		// The ContentView is the size of the ContentSize and is offset by the ContentOffset
		private class ContentView : View {
			public ContentView (Rect frame) : base (frame)
			{
				Id = "ScrollView.ContentView";
				CanFocus = true;
			}
		}

		ContentView _contentView;
		ScrollBarView _vertical, _horizontal;

		/// <summary>
		///  Initializes a new instance of the <see cref="Gui.ScrollView"/> class using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="frame"></param>
		public ScrollView (Rect frame) : base (frame)
		{
			SetInitialProperties (frame);
		}

		/// <summary>
		///  Initializes a new instance of the <see cref="Gui.ScrollView"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public ScrollView () : base ()
		{
			SetInitialProperties (Rect.Empty);
		}

		void SetInitialProperties (Rect frame)
		{
			_contentView = new ContentView (frame);
			_vertical = new ScrollBarView (1, 0, isVertical: true) {
				X = Pos.AnchorEnd (1),
				Y = 0,
				Width = 1,
				Height = Dim.Fill (_showHorizontalScrollIndicator ? 1 : 0),
				Host = this
			};

			_horizontal = new ScrollBarView (1, 0, isVertical: false) {
				X = 0,
				Y = Pos.AnchorEnd (1),
				Width = Dim.Fill (_showVerticalScrollIndicator ? 1 : 0),
				Height = 1,
				Host = this
			};

			_vertical.OtherScrollBarView = _horizontal;
			_horizontal.OtherScrollBarView = _vertical;
			base.Add (_contentView);
			CanFocus = true;

			MouseEnter += View_MouseEnter;
			MouseLeave += View_MouseLeave;
			_contentView.MouseEnter += View_MouseEnter;
			_contentView.MouseLeave += View_MouseLeave;

			// Things this view knows how to do
			AddCommand (Command.ScrollUp, () => ScrollUp (1));
			AddCommand (Command.ScrollDown, () => ScrollDown (1));
			AddCommand (Command.ScrollLeft, () => ScrollLeft (1));
			AddCommand (Command.ScrollRight, () => ScrollRight (1));
			AddCommand (Command.PageUp, () => ScrollUp (Bounds.Height));
			AddCommand (Command.PageDown, () => ScrollDown (Bounds.Height));
			AddCommand (Command.PageLeft, () => ScrollLeft (Bounds.Width));
			AddCommand (Command.PageRight, () => ScrollRight (Bounds.Width));
			AddCommand (Command.TopHome, () => ScrollUp (_contentSize.Height));
			AddCommand (Command.BottomEnd, () => ScrollDown (_contentSize.Height));
			AddCommand (Command.LeftHome, () => ScrollLeft (_contentSize.Width));
			AddCommand (Command.RightEnd, () => ScrollRight (_contentSize.Width));

			// Default keybindings for this view
			AddKeyBinding (Key.CursorUp, Command.ScrollUp);
			AddKeyBinding (Key.CursorDown, Command.ScrollDown);
			AddKeyBinding (Key.CursorLeft, Command.ScrollLeft);
			AddKeyBinding (Key.CursorRight, Command.ScrollRight);

			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding ((Key)'v' | Key.AltMask, Command.PageUp);

			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.V | Key.CtrlMask, Command.PageDown);

			AddKeyBinding (Key.PageUp | Key.CtrlMask, Command.PageLeft);
			AddKeyBinding (Key.PageDown | Key.CtrlMask, Command.PageRight);
			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.Home | Key.CtrlMask, Command.LeftHome);
			AddKeyBinding (Key.End | Key.CtrlMask, Command.RightEnd);

			Initialized += (s, e) => {
				if (!_vertical.IsInitialized) {
					_vertical.BeginInit ();
					_vertical.EndInit ();
				}
				if (!_horizontal.IsInitialized) {
					_horizontal.BeginInit ();
					_horizontal.EndInit ();
				}
				SetContentOffset (_contentOffset);
				_contentView.Frame = new Rect (ContentOffset, ContentSize);
				_vertical.ChangedPosition += delegate {
					ContentOffset = new Point (ContentOffset.X, _vertical.Position);
				};
				_horizontal.ChangedPosition += delegate {
					ContentOffset = new Point (_horizontal.Position, ContentOffset.Y);
				};
			};
		}

		//public override void BeginInit ()
		//{
		//	SetContentOffset (contentOffset);
		//	base.BeginInit ();
		//}

		Size _contentSize;
		Point _contentOffset;
		bool _showHorizontalScrollIndicator;
		bool _showVerticalScrollIndicator;
		bool _keepContentAlwaysInViewport = true;
		bool _autoHideScrollBars = true;

		/// <summary>
		/// Represents the contents of the data shown inside the scrollview
		/// </summary>
		/// <value>The size of the content.</value>
		public Size ContentSize {
			get {
				return _contentSize;
			}
			set {
				if (_contentSize != value) {
					_contentSize = value;
					_contentView.Frame = new Rect (_contentOffset, value);
					_vertical.Size = _contentSize.Height;
					_horizontal.Size = _contentSize.Width;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Represents the top left corner coordinate that is displayed by the scrollview
		/// </summary>
		/// <value>The content offset.</value>
		public Point ContentOffset {
			get {
				return _contentOffset;
			}
			set {
				if (!IsInitialized) {
					// We're not initialized so we can't do anything fancy. Just cache value.
					_contentOffset = new Point (-Math.Abs (value.X), -Math.Abs (value.Y)); ;
					return;
				}

				SetContentOffset (value);
			}
		}

		private void SetContentOffset (Point offset)
		{
			var co = new Point (-Math.Abs (offset.X), -Math.Abs (offset.Y));
			_contentOffset = co;
			_contentView.Frame = new Rect (_contentOffset, _contentSize);
			var p = Math.Max (0, -_contentOffset.Y);
			if (_vertical.Position != p) {
				_vertical.Position = Math.Max (0, -_contentOffset.Y);
			}
			p = Math.Max (0, -_contentOffset.X);
			if (_horizontal.Position != p) {
				_horizontal.Position = Math.Max (0, -_contentOffset.X);
			}
			SetNeedsDisplay ();
		}

		/// <summary>
		/// If true the vertical/horizontal scroll bars won't be showed if it's not needed.
		/// </summary>
		public bool AutoHideScrollBars {
			get => _autoHideScrollBars;
			set {
				if (_autoHideScrollBars != value) {
					_autoHideScrollBars = value;
					if (Subviews.Contains (_vertical)) {
						_vertical.AutoHideScrollBars = value;
					}
					if (Subviews.Contains (_horizontal)) {
						_horizontal.AutoHideScrollBars = value;
					}
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollView"/>
		/// </summary>
		public bool KeepContentAlwaysInViewport {
			get { return _keepContentAlwaysInViewport; }
			set {
				if (_keepContentAlwaysInViewport != value) {
					_keepContentAlwaysInViewport = value;
					_vertical.OtherScrollBarView.KeepContentAlwaysInViewport = value;
					_horizontal.OtherScrollBarView.KeepContentAlwaysInViewport = value;
					Point p = default;
					if (value && -_contentOffset.X + Bounds.Width > _contentSize.Width) {
						p = new Point (_contentSize.Width - Bounds.Width + (_showVerticalScrollIndicator ? 1 : 0), -_contentOffset.Y);
					}
					if (value && -_contentOffset.Y + Bounds.Height > _contentSize.Height) {
						if (p == default) {
							p = new Point (-_contentOffset.X, _contentSize.Height - Bounds.Height + (_showHorizontalScrollIndicator ? 1 : 0));
						} else {
							p.Y = _contentSize.Height - Bounds.Height + (_showHorizontalScrollIndicator ? 1 : 0);
						}
					}
					if (p != default) {
						ContentOffset = p;
					}
				}
			}
		}

		View _contentBottomRightCorner;

		/// <summary>
		/// Adds the view to the scrollview.
		/// </summary>
		/// <param name="view">The view to add to the scrollview.</param>
		public override void Add (View view)
		{
			if (view.Id == "contentBottomRightCorner") {
				_contentBottomRightCorner = view;
				base.Add (view);
			} else {
				if (!IsOverridden (view, "MouseEvent")) {
					view.MouseEnter += View_MouseEnter;
					view.MouseLeave += View_MouseLeave;
				}
				_contentView.Add (view);
			}
			SetNeedsLayout ();
		}

		void View_MouseLeave (object sender, MouseEventEventArgs e)
		{
			if (Application.MouseGrabView != null && Application.MouseGrabView != _vertical && Application.MouseGrabView != _horizontal) {
				Application.UngrabMouse ();
			}
		}

		void View_MouseEnter (object sender, MouseEventEventArgs e)
		{
			Application.GrabMouse (this);
		}

		/// <summary>
		/// Gets or sets the visibility for the horizontal scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show horizontal scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowHorizontalScrollIndicator {
			get => _showHorizontalScrollIndicator;
			set {
				if (value != _showHorizontalScrollIndicator) {
					_showHorizontalScrollIndicator = value;
					SetNeedsLayout ();
					if (value) {
						_horizontal.OtherScrollBarView = _vertical;
						base.Add (_horizontal);
						_horizontal.ShowScrollIndicator = value;
						_horizontal.AutoHideScrollBars = _autoHideScrollBars;
						_horizontal.OtherScrollBarView.ShowScrollIndicator = value;
						_horizontal.MouseEnter += View_MouseEnter;
						_horizontal.MouseLeave += View_MouseLeave;
					} else {
						base.Remove (_horizontal);
						_horizontal.OtherScrollBarView = null;
						_horizontal.MouseEnter -= View_MouseEnter;
						_horizontal.MouseLeave -= View_MouseLeave;
					}
				}
				_vertical.Height = Dim.Fill (_showHorizontalScrollIndicator ? 1 : 0);
			}
		}

		/// <summary>
		///   Removes all widgets from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll ()
		{
			_contentView.RemoveAll ();
		}

		/// <summary>
		/// Gets or sets the visibility for the vertical scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowVerticalScrollIndicator {
			get => _showVerticalScrollIndicator;
			set {
				if (value != _showVerticalScrollIndicator) {
					_showVerticalScrollIndicator = value;
					SetNeedsLayout ();
					if (value) {
						_vertical.OtherScrollBarView = _horizontal;
						base.Add (_vertical);
						_vertical.ShowScrollIndicator = value;
						_vertical.AutoHideScrollBars = _autoHideScrollBars;
						_vertical.OtherScrollBarView.ShowScrollIndicator = value;
						_vertical.MouseEnter += View_MouseEnter;
						_vertical.MouseLeave += View_MouseLeave;
					} else {
						Remove (_vertical);
						_vertical.OtherScrollBarView = null;
						_vertical.MouseEnter -= View_MouseEnter;
						_vertical.MouseLeave -= View_MouseLeave;
					}
				}
				_horizontal.Width = Dim.Fill (_showVerticalScrollIndicator ? 1 : 0);
			}
		}

		/// <inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			SetViewsNeedsDisplay ();

			var savedClip = ClipToBounds ();
			Driver.SetAttribute (GetNormalColor ());
			Clear ();

			if (!string.IsNullOrEmpty (_contentView.Text) || _contentView.Subviews.Count > 0) {
				_contentView.Draw ();
			}

			DrawScrollBars ();

			Driver.Clip = savedClip;
		}

		private void DrawScrollBars ()
		{
			if (_autoHideScrollBars) {
				ShowHideScrollBars ();
			} else {
				if (ShowVerticalScrollIndicator) {
					_vertical.Draw ();
				}
				if (ShowHorizontalScrollIndicator) {
					_horizontal.Draw ();
				}
				if (ShowVerticalScrollIndicator && ShowHorizontalScrollIndicator) {
					SetContentBottomRightCornerVisibility ();
					_contentBottomRightCorner.Draw ();
				}
			}
		}

		private void SetContentBottomRightCornerVisibility ()
		{
			if (_showHorizontalScrollIndicator && _showVerticalScrollIndicator) {
				_contentBottomRightCorner.Visible = true;
			} else if (_horizontal.IsAdded || _vertical.IsAdded) {
				_contentBottomRightCorner.Visible = false;
			}
		}

		void ShowHideScrollBars ()
		{
			bool v = false, h = false; bool p = false;

			if (Bounds.Height == 0 || Bounds.Height > _contentSize.Height) {
				if (ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = false;
				}
				v = false;
			} else if (Bounds.Height > 0 && Bounds.Height == _contentSize.Height) {
				p = true;
			} else {
				if (!ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = true;
				}
				v = true;
			}
			if (Bounds.Width == 0 || Bounds.Width > _contentSize.Width) {
				if (ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = false;
				}
				h = false;
			} else if (Bounds.Width > 0 && Bounds.Width == _contentSize.Width && p) {
				if (ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = false;
				}
				h = false;
				if (ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = false;
				}
				v = false;
			} else {
				if (p) {
					if (!ShowVerticalScrollIndicator) {
						ShowVerticalScrollIndicator = true;
					}
					v = true;
				}
				if (!ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = true;
				}
				h = true;
			}
			var dim = Dim.Fill (h ? 1 : 0);
			if (!_vertical.Height.Equals (dim)) {
				_vertical.Height = dim;
			}
			dim = Dim.Fill (v ? 1 : 0);
			if (!_horizontal.Width.Equals (dim)) {
				_horizontal.Width = dim;
			}

			if (v) {
				_vertical.SetRelativeLayout (Bounds);
				_vertical.Draw ();
			}
			if (h) {
				_horizontal.SetRelativeLayout (Bounds);
				_horizontal.Draw ();
			}
			SetContentBottomRightCornerVisibility ();
			if (v && h) {
				_contentBottomRightCorner.SetRelativeLayout (Bounds);
				_contentBottomRightCorner.Draw ();
			}
		}

		void SetViewsNeedsDisplay ()
		{
			foreach (View view in _contentView.Subviews) {
				view.SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (InternalSubviews.Count == 0)
				Move (0, 0);
			else
				base.PositionCursor ();
		}

		/// <summary>
		/// Scrolls the view up.
		/// </summary>
		/// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="lines">Number of lines to scroll.</param>
		public bool ScrollUp (int lines)
		{
			if (_contentOffset.Y < 0) {
				ContentOffset = new Point (_contentOffset.X, Math.Min (_contentOffset.Y + lines, 0));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Scrolls the view to the left
		/// </summary>
		/// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="cols">Number of columns to scroll by.</param>
		public bool ScrollLeft (int cols)
		{
			if (_contentOffset.X < 0) {
				ContentOffset = new Point (Math.Min (_contentOffset.X + cols, 0), _contentOffset.Y);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Scrolls the view down.
		/// </summary>
		/// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="lines">Number of lines to scroll.</param>
		public bool ScrollDown (int lines)
		{
			if (_vertical.CanScroll (lines, out _, true)) {
				ContentOffset = new Point (_contentOffset.X, _contentOffset.Y - lines);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Scrolls the view to the right.
		/// </summary>
		/// <returns><c>true</c>, if right was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="cols">Number of columns to scroll by.</param>
		public bool ScrollRight (int cols)
		{
			if (_horizontal.CanScroll (cols, out _)) {
				ContentOffset = new Point (_contentOffset.X - cols, _contentOffset.Y);
				return true;
			}
			return false;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (base.ProcessKey (kb))
				return true;

			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return false;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledRight && me.Flags != MouseFlags.WheeledLeft &&
				//				me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
				!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {

				return false;
			}

			if (me.Flags == MouseFlags.WheeledDown && ShowVerticalScrollIndicator) {
				ScrollDown (1);
			} else if (me.Flags == MouseFlags.WheeledUp && ShowVerticalScrollIndicator) {
				ScrollUp (1);
			} else if (me.Flags == MouseFlags.WheeledRight && _showHorizontalScrollIndicator) {
				ScrollRight (1);
			} else if (me.Flags == MouseFlags.WheeledLeft && ShowVerticalScrollIndicator) {
				ScrollLeft (1);
			} else if (me.X == _vertical.Frame.X && ShowVerticalScrollIndicator) {
				_vertical.MouseEvent (me);
			} else if (me.Y == _horizontal.Frame.Y && ShowHorizontalScrollIndicator) {
				_horizontal.MouseEvent (me);
			} else if (IsOverridden (me.View, "MouseEvent")) {
				Application.UngrabMouse ();
			}
			return true;
		}

		///<inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			if (!_showVerticalScrollIndicator) {
				// It was not added to SuperView, so it won't get disposed automatically
				_vertical?.Dispose ();
			}
			if (!_showHorizontalScrollIndicator) {
				// It was not added to SuperView, so it won't get disposed automatically
				_horizontal?.Dispose ();
			}
			base.Dispose (disposing);
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (Subviews.Count == 0 || !Subviews.Any (subview => subview.CanFocus)) {
				Application.Driver?.SetCursorVisibility (CursorVisibility.Invisible);
			}

			return base.OnEnter (view);
		}
	}
}
