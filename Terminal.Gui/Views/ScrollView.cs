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

		ContentView contentView;
		ScrollBarView vertical, horizontal;

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
			contentView = new ContentView (frame);
			vertical = new ScrollBarView (1, 0, isVertical: true) {
				X = Pos.AnchorEnd (1),
				Y = 0,
				Width = 1,
				Height = Dim.Fill (showHorizontalScrollIndicator ? 1 : 0)
			};

			vertical.Host = this;
			horizontal = new ScrollBarView (1, 0, isVertical: false) {
				X = 0,
				Y = Pos.AnchorEnd (1),
				Width = Dim.Fill (showVerticalScrollIndicator ? 1 : 0),
				Height = 1
			};

			horizontal.Host = this;
			vertical.OtherScrollBarView = horizontal;
			horizontal.OtherScrollBarView = vertical;
			base.Add (contentView);
			CanFocus = true;

			MouseEnter += View_MouseEnter;
			MouseLeave += View_MouseLeave;
			contentView.MouseEnter += View_MouseEnter;
			contentView.MouseLeave += View_MouseLeave;

			// Things this view knows how to do
			AddCommand (Command.ScrollUp, () => ScrollUp (1));
			AddCommand (Command.ScrollDown, () => ScrollDown (1));
			AddCommand (Command.ScrollLeft, () => ScrollLeft (1));
			AddCommand (Command.ScrollRight, () => ScrollRight (1));
			AddCommand (Command.PageUp, () => ScrollUp (Bounds.Height));
			AddCommand (Command.PageDown, () => ScrollDown (Bounds.Height));
			AddCommand (Command.PageLeft, () => ScrollLeft (Bounds.Width));
			AddCommand (Command.PageRight, () => ScrollRight (Bounds.Width));
			AddCommand (Command.TopHome, () => ScrollUp (contentSize.Height));
			AddCommand (Command.BottomEnd, () => ScrollDown (contentSize.Height));
			AddCommand (Command.LeftHome, () => ScrollLeft (contentSize.Width));
			AddCommand (Command.RightEnd, () => ScrollRight (contentSize.Width));

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
				if (!vertical.IsInitialized) {
					vertical.BeginInit ();
					vertical.EndInit ();
				}
				if (!horizontal.IsInitialized) {
					horizontal.BeginInit ();
					horizontal.EndInit ();
				}
				SetContentOffset (contentOffset);
				contentView.Frame = new Rect (ContentOffset, ContentSize);
				vertical.ChangedPosition += delegate {
					ContentOffset = new Point (ContentOffset.X, vertical.Position);
				};
				horizontal.ChangedPosition += delegate {
					ContentOffset = new Point (horizontal.Position, ContentOffset.Y);
				};
			};
		}

		//public override void BeginInit ()
		//{
		//	SetContentOffset (contentOffset);
		//	base.BeginInit ();
		//}

		Size contentSize;
		Point contentOffset;
		bool showHorizontalScrollIndicator;
		bool showVerticalScrollIndicator;
		bool keepContentAlwaysInViewport = true;
		bool autoHideScrollBars = true;

		/// <summary>
		/// Represents the contents of the data shown inside the scrollview
		/// </summary>
		/// <value>The size of the content.</value>
		public Size ContentSize {
			get {
				return contentSize;
			}
			set {
				if (contentSize != value) {
					contentSize = value;
					contentView.Frame = new Rect (contentOffset, value);
					vertical.Size = contentSize.Height;
					horizontal.Size = contentSize.Width;
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
				return contentOffset;
			}
			set {
				if (!IsInitialized) {
					// We're not initialized so we can't do anything fancy. Just cache value.
					contentOffset = new Point (-Math.Abs (value.X), -Math.Abs (value.Y)); ;
					return;
				}

				SetContentOffset (value);
			}
		}

		private void SetContentOffset (Point offset)
		{
			var co = new Point (-Math.Abs (offset.X), -Math.Abs (offset.Y));
			contentOffset = co;
			contentView.Frame = new Rect (contentOffset, contentSize);
			var p = Math.Max (0, -contentOffset.Y);
			if (vertical.Position != p) {
				vertical.Position = Math.Max (0, -contentOffset.Y);
			}
			p = Math.Max (0, -contentOffset.X);
			if (horizontal.Position != p) {
				horizontal.Position = Math.Max (0, -contentOffset.X);
			}
			SetNeedsDisplay ();
		}

		/// <summary>
		/// If true the vertical/horizontal scroll bars won't be showed if it's not needed.
		/// </summary>
		public bool AutoHideScrollBars {
			get => autoHideScrollBars;
			set {
				if (autoHideScrollBars != value) {
					autoHideScrollBars = value;
					if (Subviews.Contains (vertical)) {
						vertical.AutoHideScrollBars = value;
					}
					if (Subviews.Contains (horizontal)) {
						horizontal.AutoHideScrollBars = value;
					}
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollView"/>
		/// </summary>
		public bool KeepContentAlwaysInViewport {
			get { return keepContentAlwaysInViewport; }
			set {
				if (keepContentAlwaysInViewport != value) {
					keepContentAlwaysInViewport = value;
					vertical.OtherScrollBarView.KeepContentAlwaysInViewport = value;
					horizontal.OtherScrollBarView.KeepContentAlwaysInViewport = value;
					Point p = default;
					if (value && -contentOffset.X + Bounds.Width > contentSize.Width) {
						p = new Point (contentSize.Width - Bounds.Width + (showVerticalScrollIndicator ? 1 : 0), -contentOffset.Y);
					}
					if (value && -contentOffset.Y + Bounds.Height > contentSize.Height) {
						if (p == default) {
							p = new Point (-contentOffset.X, contentSize.Height - Bounds.Height + (showHorizontalScrollIndicator ? 1 : 0));
						} else {
							p.Y = contentSize.Height - Bounds.Height + (showHorizontalScrollIndicator ? 1 : 0);
						}
					}
					if (p != default) {
						ContentOffset = p;
					}
				}
			}
		}

		/// <summary>
		/// Adds the view to the scrollview.
		/// </summary>
		/// <param name="view">The view to add to the scrollview.</param>
		public override void Add (View view)
		{
			if (!IsOverridden (view, "MouseEvent")) {
				view.MouseEnter += View_MouseEnter;
				view.MouseLeave += View_MouseLeave;
			}
			contentView.Add (view);
			SetNeedsLayout ();
		}

		void View_MouseLeave (object sender, MouseEventEventArgs e)
		{
			if (Application.MouseGrabView != null && Application.MouseGrabView != vertical && Application.MouseGrabView != horizontal) {
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
			get => showHorizontalScrollIndicator;
			set {
				if (value == showHorizontalScrollIndicator) {
					return;
				}

				showHorizontalScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					base.Add (horizontal);
					horizontal.ShowScrollIndicator = value;
					horizontal.AutoHideScrollBars = autoHideScrollBars;
					horizontal.OtherScrollBarView = vertical;
					horizontal.OtherScrollBarView.ShowScrollIndicator = value;
					horizontal.MouseEnter += View_MouseEnter;
					horizontal.MouseLeave += View_MouseLeave;
				} else {
					base.Remove (horizontal);
					horizontal.OtherScrollBarView = null;
					horizontal.MouseEnter -= View_MouseEnter;
					horizontal.MouseLeave -= View_MouseLeave;
				}
				vertical.Height = Dim.Fill (showHorizontalScrollIndicator ? 1 : 0);
			}
		}

		/// <summary>
		///   Removes all widgets from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		/// <summary>
		/// Gets or sets the visibility for the vertical scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowVerticalScrollIndicator {
			get => showVerticalScrollIndicator;
			set {
				if (value == showVerticalScrollIndicator) {
					return;
				}

				showVerticalScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					base.Add (vertical);
					vertical.ShowScrollIndicator = value;
					vertical.AutoHideScrollBars = autoHideScrollBars;
					vertical.OtherScrollBarView = horizontal;
					vertical.OtherScrollBarView.ShowScrollIndicator = value;
					vertical.MouseEnter += View_MouseEnter;
					vertical.MouseLeave += View_MouseLeave;
				} else {
					Remove (vertical);
					vertical.OtherScrollBarView = null;
					vertical.MouseEnter -= View_MouseEnter;
					vertical.MouseLeave -= View_MouseLeave;
				}
				horizontal.Width = Dim.Fill (showVerticalScrollIndicator ? 1 : 0);
			}
		}

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			SetViewsNeedsDisplay ();

			var savedClip = ClipToBounds ();
			Driver.SetAttribute (GetNormalColor ());
			Clear ();

			contentView.Redraw (contentView.Bounds);
			OnDrawContent (new Rect (ContentOffset,
				new Size (Math.Max (Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0), 0),
					Math.Max (Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0), 0))));

			if (autoHideScrollBars) {
				ShowHideScrollBars ();
			} else {
				if (ShowVerticalScrollIndicator) {
					//vertical.SetRelativeLayout (Bounds);
					vertical.Redraw (vertical.Bounds);
				}

				if (ShowHorizontalScrollIndicator) {
					//horizontal.SetRelativeLayout (Bounds);
					horizontal.Redraw (horizontal.Bounds);
				}
			}

			// Fill in the bottom left corner. Note we don't rely on ScrollBarView.contentBottomRightCorner here
			// because that only applies when ScrollBarView is hosted.
			if (ShowVerticalScrollIndicator && ShowHorizontalScrollIndicator) {
				AddRune (Bounds.Width - 1, Bounds.Height - 1, ' ');
			}
			Driver.SetAttribute (GetNormalColor ());
			Driver.Clip = savedClip;
		}

		void ShowHideScrollBars ()
		{
			bool v = false, h = false; bool p = false;

			if (Bounds.Height == 0 || Bounds.Height > contentSize.Height) {
				if (ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = false;
				}
				v = false;
			} else if (Bounds.Height > 0 && Bounds.Height == contentSize.Height) {
				p = true;
			} else {
				if (!ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = true;
				}
				v = true;
			}
			if (Bounds.Width == 0 || Bounds.Width > contentSize.Width) {
				if (ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = false;
				}
				h = false;
			} else if (Bounds.Width > 0 && Bounds.Width == contentSize.Width && p) {
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
			if (!vertical.Height.Equals (dim)) {
				vertical.Height = dim;
			}
			dim = Dim.Fill (v ? 1 : 0);
			if (!horizontal.Width.Equals (dim)) {
				horizontal.Width = dim;
			}

			if (v) {
				vertical.SetRelativeLayout (Bounds);
				vertical.Redraw (vertical.Bounds);
			}
			if (h) {
				horizontal.SetRelativeLayout (Bounds);
				horizontal.Redraw (horizontal.Bounds);
			}
		}

		void SetViewsNeedsDisplay ()
		{
			foreach (View view in contentView.Subviews) {
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
			if (contentOffset.Y < 0) {
				ContentOffset = new Point (contentOffset.X, Math.Min (contentOffset.Y + lines, 0));
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
			if (contentOffset.X < 0) {
				ContentOffset = new Point (Math.Min (contentOffset.X + cols, 0), contentOffset.Y);
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
			if (vertical.CanScroll (lines, out _, true)) {
				ContentOffset = new Point (contentOffset.X, contentOffset.Y - lines);
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
			if (horizontal.CanScroll (cols, out _)) {
				ContentOffset = new Point (contentOffset.X - cols, contentOffset.Y);
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
			} else if (me.Flags == MouseFlags.WheeledRight && showHorizontalScrollIndicator) {
				ScrollRight (1);
			} else if (me.Flags == MouseFlags.WheeledLeft && ShowVerticalScrollIndicator) {
				ScrollLeft (1);
			} else if (me.X == vertical.Frame.X && ShowVerticalScrollIndicator) {
				vertical.MouseEvent (me);
			} else if (me.Y == horizontal.Frame.Y && ShowHorizontalScrollIndicator) {
				horizontal.MouseEvent (me);
			} else if (IsOverridden (me.View, "MouseEvent")) {
				Application.UngrabMouse ();
			}
			return true;
		}

		///<inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			if (!showVerticalScrollIndicator) {
				// It was not added to SuperView, so it won't get disposed automatically
				vertical?.Dispose ();
			}
			if (!showHorizontalScrollIndicator) {
				// It was not added to SuperView, so it won't get disposed automatically
				horizontal?.Dispose ();
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
