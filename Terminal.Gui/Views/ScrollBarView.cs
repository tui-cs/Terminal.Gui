//
// ScrollBarView.cs: ScrollBarView view.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace Terminal.Gui {
	/// <summary>
	/// ScrollBarViews are views that display a 1-character scrollbar, either horizontal or vertical
	/// </summary>
	/// <remarks>
	/// <para>
	///   The scrollbar is drawn to be a representation of the Size, assuming that the 
	///   scroll position is set at Position.
	/// </para>
	/// <para>
	///   If the region to display the scrollbar is larger than three characters, 
	///   arrow indicators are drawn.
	/// </para>
	/// </remarks>
	public class ScrollBarView : View {
		/// <summary>
		/// The scroll bar types used by this <see cref="ScrollBarView"/>.
		/// </summary>
		public enum ScrollBarType {
			/// <summary>
			/// None scroll bar will be used and to avoid throwing an exception never use it in a constructor.
			/// </summary>
			None,
			/// <summary>
			/// Only the vertical scroll bar will be shown.
			/// </summary>
			Vertical,
			/// <summary>
			/// Only the horizontal scroll bar will be shown.
			/// </summary>
			Horizontal,
			/// <summary>
			/// Both vertical and horizontal scroll bars will be shown.
			/// </summary>
			Both
		}

		bool vertical;
		int size, position, selected;
		bool showScrollIndicator;
		bool keepContentAlwaysInViewport = true;
		bool autoHideScrollBars = true;
		bool hosted;
		ScrollBarView otherScrollBarView;
		View contentBottomRightCorner;

		bool _showBothScrollIndicator => OtherScrollBarView?._showScrollIndicator == true && _showScrollIndicator;

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="rect">Frame for the scrollbar.</param>
		public ScrollBarView (Rect rect) : this (rect, 0, 0, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="rect">Frame for the scrollbar.</param>
		/// <param name="size">The size that this scrollbar represents. Sets the <see cref="Size"/> property.</param>
		/// <param name="position">The position within this scrollbar. Sets the <see cref="Position"/> property.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal. Sets the <see cref="IsVertical"/> property.</param>
		public ScrollBarView (Rect rect, int size, int position, bool isVertical) : base (rect)
		{
			SetInitialProperties (size, position, isVertical);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public ScrollBarView () : this (0, 0, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="size">The size that this scrollbar represents.</param>
		/// <param name="position">The position within this scrollbar.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.</param>
		public ScrollBarView (int size, int position, bool isVertical) : base ()
		{
			SetInitialProperties (size, position, isVertical);
		}

		static (bool IsVertical, bool ShowBothScrollIndicator) GetHostScrollBar (ScrollBarType scrollBarType)
		{
			switch (scrollBarType) {
			case ScrollBarType.Vertical:
				return (true, false);
			case ScrollBarType.Horizontal:
				return (false, false);
			case ScrollBarType.Both:
				return (true, true);
			}
			throw new ArgumentException ("Cannot create an instance of ScrollBarType.None!", nameof (scrollBarType));
		}

		string lines, cols, line, col, top, left;// bottom, right;
							 //int savedBottom, savedRight;

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class based on the <see cref="ScrollBarType"/>.
		/// </summary>
		/// <param name="host">The view that will host this scrollbar.</param>
		/// <param name="scrollBarType">The <see cref="ScrollBarType"/> except the <see cref="ScrollBarType.None"/></param>
		public ScrollBarView (View host, ScrollBarType scrollBarType, bool isBuiltIn,
			[CallerMemberName] string lines = null, [CallerMemberName] string cols = null,
			//[CallerMemberName] string line = null, [CallerMemberName] string col = null,
			[CallerMemberName] string top = null, [CallerMemberName] string left = null)
			//[CallerMemberName] string bottom = null, [CallerMemberName] string right = null)
			: this (host, GetHostScrollBar (scrollBarType).IsVertical, GetHostScrollBar (scrollBarType).ShowBothScrollIndicator)
		{
			if (!isBuiltIn) {
				return;
			}

			ChangedPosition += ScrollBarView_ChangedPosition;
			//VisibleChanged += ScrollBarView_VisibleChanged;
			if (OtherScrollBarView != null) {
				OtherScrollBarView.ChangedPosition += OtherScrollBarView_ChangedPosition;
				//OtherScrollBarView.VisibleChanged += OtherScrollBarView_VisibleChanged;
			}
			host.DrawContent += Host_DrawContent;
			host.LayoutComplete += Host_LayoutComplete;
			this.lines = lines;
			this.cols = cols;
			this.top = top;
			this.left = left;
			//this.bottom = bottom;
			//this.right = right;
			//savedBottom = GetPropertyValue (bottom);
			//savedRight = GetPropertyValue (right);
		}

		private void Host_LayoutComplete (object sender, LayoutEventArgs e)
		{
			Refresh ();
		}

		private int GetPropertyValue (string propertyName) =>
			(int)Host.GetType ().GetProperty (propertyName).GetValue (Host, null);

		private void SetPropertyValue (string propertyName, int value) =>
			Host.GetType ().GetProperty (propertyName).SetValue (Host, value);

		private void Host_DrawContent (object sender, DrawEventArgs e)
		{
			Size = GetPropertyValue (lines);
			Position = GetPropertyValue (top);
			if (OtherScrollBarView != null) {
				OtherScrollBarView.Size = GetPropertyValue (cols);
				OtherScrollBarView.Position = GetPropertyValue (left);
			}
			LayoutSubviews ();
			Refresh ();
		}

		private void ScrollBarView_ChangedPosition (object sender, EventArgs e)
		{
			SetPropertyValue (top, position);
			if (GetPropertyValue (top) != Position) {
				Position = GetPropertyValue (top);
			}
			SetNeedsDisplay ();
		}

		//private void ScrollBarView_VisibleChanged ()
		//{
		//	if (Visible && GetPropertyValue (right) == 0) {
		//		SetPropertyValue (right, savedRight);
		//	} else if (!Visible && GetPropertyValue (right) == savedRight) {
		//		SetPropertyValue (right, 0);
		//	}
		//}

		private void OtherScrollBarView_ChangedPosition (object sender, EventArgs e)
		{
			SetPropertyValue (left, OtherScrollBarView.Position);
			if (GetPropertyValue (left) != OtherScrollBarView.Position) {
				OtherScrollBarView.Position = GetPropertyValue (left);
			}
			SetNeedsDisplay ();
		}

		//private void OtherScrollBarView_VisibleChanged ()
		//{
		//	if (OtherScrollBarView.Visible && GetPropertyValue (bottom) == 0) {
		//		SetPropertyValue (bottom, savedBottom);
		//	} else if (!OtherScrollBarView.Visible && GetPropertyValue (bottom) == savedBottom) {
		//		SetPropertyValue (bottom, 0);
		//	}
		//}

		//private void OtherScrollBarView_VisibleChanged ()
		//{
		//	if (OtherScrollBarView.Visible && GetPropertyValue (bottom) == 0) {
		//		SetPropertyValue (bottom, savedBottom);
		//	} else if (!OtherScrollBarView.Visible && GetPropertyValue (bottom) == savedBottom) {
		//		SetPropertyValue (bottom, 0);
		//	}
		//}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="host">The view that will host this scrollbar.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.</param>
		/// <param name="showBothScrollIndicator">If set to <c>true (default)</c> will have the other scrollbar, otherwise will have only one.</param>
		public ScrollBarView (View host, bool isVertical, bool showBothScrollIndicator = true) : this (0, 0, isVertical)
		{
			if (host == null) {
				throw new ArgumentNullException ("The host parameter can't be null.");
			} else if (host.SuperView == null) {
				throw new ArgumentNullException ("The host SuperView parameter can't be null.");
			}
			_hosted = true;
			ColorScheme = host.ColorScheme;
			X = isVertical ? Pos.Right (host) - 1 : Pos.Left (host);
			Y = isVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
			Host = host;
			CanFocus = false;
			Enabled = host.Enabled;
			Visible = host.Visible;
			//Host.CanFocusChanged += Host_CanFocusChanged;
			Host.EnabledChanged += Host_EnabledChanged;
			Host.VisibleChanged += Host_VisibleChanged;
			Host.SuperView.Add (this);
			AutoHideScrollBars = true;
			if (showBothScrollIndicator) {
				OtherScrollBarView = new ScrollBarView (0, 0, !isVertical) {
					Id = "OtherScrollBarView",
					ColorScheme = host.ColorScheme,
					Host = host,
					CanFocus = false,
					Enabled = host.Enabled,
					Visible = host.Visible,
					OtherScrollBarView = this
				};
				OtherScrollBarView._hosted = true;
				OtherScrollBarView.X = OtherScrollBarView.IsVertical ? Pos.Right (host) - 1 : Pos.Left (host);
				OtherScrollBarView.Y = OtherScrollBarView.IsVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
				OtherScrollBarView.Host.SuperView.Add (OtherScrollBarView);
				OtherScrollBarView.ShowScrollIndicator = true;
			}
			ShowScrollIndicator = true;
			CreateBottomRightCorner ();
			ClearOnVisibleFalse = false;
		}

        private void CreateBottomRightCorner()
        {
            if (Host != null && (_contentBottomRightCorner == null && OtherScrollBarView == null
                || (_contentBottomRightCorner == null && OtherScrollBarView != null && OtherScrollBarView._contentBottomRightCorner == null)))
            {

                _contentBottomRightCorner = new View()
                {
                    Id = "contentBottomRightCorner",
                    Visible = Host.Visible,
                    ClearOnVisibleFalse = false,
                    ColorScheme = ColorScheme
                };
                if (_hosted)
                {
                    Host.SuperView.Add(_contentBottomRightCorner);
                }
                else
                {
                    Host.Add(_contentBottomRightCorner);
                }
                _contentBottomRightCorner.X = Pos.Right(Host) - 1;
                _contentBottomRightCorner.Y = Pos.Bottom(Host) - 1;
                _contentBottomRightCorner.Width = 1;
                _contentBottomRightCorner.Height = 1;
                _contentBottomRightCorner.MouseClick += ContentBottomRightCorner_MouseClick;
                _contentBottomRightCorner.DrawContent += _contentBottomRightCorner_DrawContent;
            }
        }

        private void _contentBottomRightCorner_DrawContent(object sender, DrawEventArgs e)
        {
            Driver.SetAttribute(Host.HasFocus ? ColorScheme.Focus : GetNormalColor());
        }

        public override void RemoveAll ()
		{
			if (Host != null) {
				Host.SuperView.Remove (this);
				if (OtherScrollBarView != null) {
					OtherScrollBarView.Host.SuperView.Remove (OtherScrollBarView);
				}
				Host.SuperView.Remove (contentBottomRightCorner);
			}

			base.RemoveAll ();
		}

		private void Host_VisibleChanged (object sender, EventArgs e)
		{
			if (!Host.Visible) {
				Visible = Host.Visible;
				if (_otherScrollBarView != null) {
					_otherScrollBarView.Visible = Visible;
				}
				_contentBottomRightCorner.Visible = Visible;
			} else {
				ShowHideScrollBars ();
			}
		}

		private void Host_EnabledChanged (object sender, EventArgs e)
		{
			Enabled = Host.Enabled;
			if (_otherScrollBarView != null) {
				_otherScrollBarView.Enabled = Enabled;
			}
			_contentBottomRightCorner.Enabled = Enabled;
		}

		//private void Host_CanFocusChanged ()
		//{
		//	CanFocus = Host.CanFocus;
		//	if (otherScrollBarView != null) {
		//		otherScrollBarView.CanFocus = CanFocus;
		//	}
		//}

		void ContentBottomRightCorner_MouseClick (object sender, MouseEventEventArgs me)
		{
			if (me.MouseEvent.Flags == MouseFlags.WheeledDown || me.MouseEvent.Flags == MouseFlags.WheeledUp
			    || me.MouseEvent.Flags == MouseFlags.WheeledRight || me.MouseEvent.Flags == MouseFlags.WheeledLeft) {

				MouseEvent (me.MouseEvent);
			} else if (me.MouseEvent.Flags == MouseFlags.Button1Clicked) {
				Host.SetFocus ();
			}

			me.Handled = true;
		}

		void SetInitialProperties (int size, int position, bool isVertical)
		{
			Id = "ScrollBarView";
			_vertical = isVertical;
			this._position = position;
			this._size = size;
			WantContinuousButtonPressed = true;
			ClearOnVisibleFalse = false;

			Added += (s, e) => CreateBottomRightCorner ();

			Initialized += (s, e) => {
				SetWidthHeight ();
				SetRelativeLayout (SuperView?.Frame ?? Host?.Frame ?? Frame);
				if (Id == "OtherScrollBarView" || OtherScrollBarView == null) {
					// Only do this once if both scrollbars are enabled
					ShowHideScrollBars ();
				}
				SetPosition (position);
			};
		}

		/// <summary>
		/// If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.
		/// </summary>
		public bool IsVertical {
			get => _vertical;
			set {
				_vertical = value;
				if (IsInitialized) {
					SetWidthHeight ();
				}
			}
		}

		/// <summary>
		/// The size of content the scrollbar represents.
		/// </summary>
		/// <value>The size.</value>
		/// <remarks>The <see cref="Size"/> is typically the size of the virtual content. E.g. when a Scrollbar is
		/// part of a <see cref="View"/> the Size is set to the appropriate dimension of <see cref="Host"/>.</remarks>
		public int Size {
			get => _size;
			set {
				_size = value;
				if (IsInitialized) {
					SetRelativeLayout (SuperView?.Frame ?? Host.Frame);
					ShowHideScrollBars (false);
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// This event is raised when the position on the scrollbar has changed.
		/// </summary>
		public event EventHandler ChangedPosition;

		/// <summary>
		/// The position, relative to <see cref="Size"/>, to set the scrollbar at.
		/// </summary>
		/// <value>The position.</value>
		public int Position {
			get => _position;
			set {
				_position = value;
				if (IsInitialized) {
					// We're not initialized so we can't do anything fancy. Just cache value.
					SetPosition (value);
				}
			}
		}

		// Helper to assist Initialized event handler
		private void SetPosition (int newPosition)
		{
			if (CanScroll (newPosition - _position, out int max, _vertical)) {
				if (max == newPosition - _position) {
					_position = newPosition;
				} else {
					_position = Math.Max (_position + max, 0);
				}
			} else if (max < 0) {
				_position = Math.Max (_position + max, 0);
			} else {
				_position = Math.Max (newPosition, 0);
			}
			OnChangedPosition ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// The current selected object from the <see cref="Host"/>.
		/// </summary>
		public int Selected {
			get => selected;
			set {
				if (selected != value) {
					selected = value;
					var s = (vertical ? Host.Bounds.Height : Host.Bounds.Width)
						- (otherScrollBarView != null && otherScrollBarView.showBothScrollIndicator ? 1 : 0);
					//if (selected == size - 1 && position + s < size) {
					if (selected - position == s) {
						Position++;
					}
				}
			}
		}

		//bool MustScroll (ref int value, bool isVertical = false)
		//{
		//	if (position != value) {
		//		return false;
		//	}
		//	var s = isVertical ? Host.Bounds.Height + (otherScrollBarView != null && otherScrollBarView.showBothScrollIndicator ? 1 : 0) :
		//		Host.Bounds.Width + (otherScrollBarView != null && otherScrollBarView.showBothScrollIndicator ? 1 : 0);
		//	if (position + s > size) {
		//		value++;
		//		return true;
		//	}
		//	return false;
		//}

		/// <summary>
		/// Get or sets the view that host this <see cref="ScrollBarView"/>
		/// </summary>
		public View Host { get; internal set; }

		/// <summary>
		/// Represent a vertical or horizontal ScrollBarView other than this.
		/// </summary>
		public ScrollBarView OtherScrollBarView {
			get => _otherScrollBarView;
			set {
				if (value != null && (value.IsVertical && _vertical || !value.IsVertical && !_vertical)) {
					throw new ArgumentException ($"There is already a {(_vertical ? "vertical" : "horizontal")} ScrollBarView.");
				}
				_otherScrollBarView = value;
			}
		}

		// BUGBUG: v2 - Why can't we get rid of this and just use Visible?
		/// <summary>
		/// Gets or sets the visibility for the vertical or horizontal scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowScrollIndicator {
			get => _showScrollIndicator;
			set {
				//if (value == showScrollIndicator) {
				//	return;
				//}

				_showScrollIndicator = value;
				if (IsInitialized) {
					SetNeedsLayout ();
					if (value) {
						Visible = true;
					} else {
						Visible = false;
						Position = 0;
					}
					SetWidthHeight ();
				}
			}
		}

		/// <summary>
		/// Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/>
		/// </summary>
		public bool KeepContentAlwaysInViewport {
			get { return _keepContentAlwaysInViewport; }
			set {
				if (_keepContentAlwaysInViewport != value) {
					_keepContentAlwaysInViewport = value;
					int pos = 0;
					if (value && !_vertical && _position + Host.Bounds.Width > _size) {
						pos = _size - Host.Bounds.Width + (_showBothScrollIndicator ? 1 : 0);
					}
					if (value && _vertical && _position + Host.Bounds.Height > _size) {
						pos = _size - Host.Bounds.Height + (_showBothScrollIndicator ? 1 : 0);
					}
					if (pos != 0) {
						Position = pos;
					}
					if (OtherScrollBarView != null && OtherScrollBarView._keepContentAlwaysInViewport != value) {
						OtherScrollBarView.KeepContentAlwaysInViewport = value;
					}
					if (pos == 0) {
						Refresh ();
					}
				}
			}
		}

		/// <summary>
		/// If true the vertical/horizontal scroll bars won't be showed if it's not needed.
		/// </summary>
		public bool AutoHideScrollBars {
			get => _autoHideScrollBars;
			set {
				if (_autoHideScrollBars != value) {
					_autoHideScrollBars = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Virtual method to invoke the <see cref="ChangedPosition"/> action event.
		/// </summary>
		public virtual void OnChangedPosition ()
		{
			ChangedPosition?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Only used for a hosted view that will update and redraw the scrollbars.
		/// </summary>
		public virtual void Refresh ()
		{
			ShowHideScrollBars ();
		}

		void ShowHideScrollBars (bool redraw = true)
		{
			if (!_hosted || (_hosted && !_autoHideScrollBars)) {
				if (_contentBottomRightCorner != null && _contentBottomRightCorner.Visible) {
					_contentBottomRightCorner.Visible = false;
				} else if (_otherScrollBarView != null && _otherScrollBarView._contentBottomRightCorner != null && _otherScrollBarView._contentBottomRightCorner.Visible) {
					_otherScrollBarView._contentBottomRightCorner.Visible = false;
				}
				return;
			}

			var pending = CheckBothScrollBars (this);
			if (_otherScrollBarView != null) {
				CheckBothScrollBars (_otherScrollBarView, pending);
			}

			SetWidthHeight ();
			SetRelativeLayout (SuperView?.Frame ?? Host.Frame);
			if (_otherScrollBarView != null) {
				OtherScrollBarView.SetRelativeLayout (SuperView?.Frame ?? Host.Frame);
			}

			if (_showBothScrollIndicator) {
				if (_contentBottomRightCorner != null) {
					_contentBottomRightCorner.Visible = true;
				} else if (_otherScrollBarView != null && _otherScrollBarView._contentBottomRightCorner != null) {
					_otherScrollBarView._contentBottomRightCorner.Visible = true;
				}
			} else if (!_showScrollIndicator) {
				if (_contentBottomRightCorner != null) {
					_contentBottomRightCorner.Visible = false;
				} else if (_otherScrollBarView != null && _otherScrollBarView._contentBottomRightCorner != null) {
					_otherScrollBarView._contentBottomRightCorner.Visible = false;
				}
				if (Application.MouseGrabView != null && Application.MouseGrabView == this) {
					Application.UngrabMouse ();
				}
			} else if (_contentBottomRightCorner != null) {
				_contentBottomRightCorner.Visible = false;
			} else if (_otherScrollBarView != null && _otherScrollBarView._contentBottomRightCorner != null) {
				_otherScrollBarView._contentBottomRightCorner.Visible = false;
			}
			if (Host?.Visible == true && _showScrollIndicator && !Visible) {
				Visible = true;
			}
			if (Host?.Visible == true && _otherScrollBarView?._showScrollIndicator == true && !_otherScrollBarView.Visible) {
				_otherScrollBarView.Visible = true;
			}

			if (!redraw) {
				return;
			}

			if (_showScrollIndicator) {
				Draw ();
			}
			if (_otherScrollBarView != null && _otherScrollBarView._showScrollIndicator) {
				_otherScrollBarView.Draw ();
			}
			if (_contentBottomRightCorner != null && _contentBottomRightCorner.Visible) {
				_contentBottomRightCorner.Draw ();
			} else if (_otherScrollBarView != null && _otherScrollBarView._contentBottomRightCorner != null && _otherScrollBarView._contentBottomRightCorner.Visible) {
				_otherScrollBarView._contentBottomRightCorner.Draw ();
			}
		}

		bool CheckBothScrollBars (ScrollBarView scrollBarView, bool pending = false)
		{
			int barsize = scrollBarView._vertical ? scrollBarView.Bounds.Height : scrollBarView.Bounds.Width;
			var isHostWidthOrHeightZero = (bool)(Host?.Bounds.Width == 0 || Host?.Bounds.Height == 0);

			if (barsize == 0 || barsize >= scrollBarView._size || isHostWidthOrHeightZero) {
				if (scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = false;
				}
				if (isHostWidthOrHeightZero && scrollBarView.OtherScrollBarView.showScrollIndicator) {
					scrollBarView.OtherScrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.Visible) {
					scrollBarView.Visible = false;
				}
			} else if (barsize > 0 && barsize == scrollBarView._size && scrollBarView.OtherScrollBarView != null && pending) {
				if (scrollBarView._showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.Visible) {
					scrollBarView.Visible = false;
				}
				if (scrollBarView.OtherScrollBarView != null && scrollBarView._showBothScrollIndicator) {
					scrollBarView.OtherScrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.OtherScrollBarView.Visible) {
					scrollBarView.OtherScrollBarView.Visible = false;
				}
			} else if (barsize > 0 && barsize == _size && scrollBarView.OtherScrollBarView != null && !pending) {
				pending = true;
			} else {
				if (scrollBarView.OtherScrollBarView != null && pending) {
					if (!scrollBarView._showBothScrollIndicator) {
						scrollBarView.OtherScrollBarView.ShowScrollIndicator = true;
					}
					if (!scrollBarView.OtherScrollBarView.Visible) {
						scrollBarView.OtherScrollBarView.Visible = true;
					}
				}
				if (!scrollBarView._showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = true;
				}
				if (!scrollBarView.Visible) {
					scrollBarView.Visible = true;
				}
			}

			return pending;
		}

		// BUGBUG: v2 - rationalize this with View.SetMinWidthHeight
		void SetWidthHeight ()
		{
			// BUGBUG: v2 - If Host is also the ScrollBarView's superview, this is all bogus because it's not
			// supported that a view can reference it's superview's Dims. This code also assumes the host does 
			//  not have a margin/borderframe/padding.
			if (!IsInitialized) {
				return;
			}

			if (_showBothScrollIndicator) {
				Width = _vertical ? 1 : Host != SuperView ? Dim.Width (Host) - 1 : Dim.Fill () - 1;
				Height = _vertical ? Host != SuperView ? Dim.Height (Host) - 1 : Dim.Fill () - 1 : 1;

				_otherScrollBarView.Width = _otherScrollBarView._vertical ? 1 : Host != SuperView ? Dim.Width (Host) - 1 : Dim.Fill () - 1;
				_otherScrollBarView.Height = _otherScrollBarView._vertical ? Host != SuperView ? Dim.Height (Host) - 1 : Dim.Fill () - 1 : 1;
			} else if (_showScrollIndicator) {
				Width = _vertical ? 1 : Host != SuperView ? Dim.Width (Host) : Dim.Fill ();
				Height = _vertical ? Host != SuperView ? Dim.Height (Host) : Dim.Fill () : 1;
			} else if (_otherScrollBarView?._showScrollIndicator == true) {
				_otherScrollBarView.Width = _otherScrollBarView._vertical ? 1 : Host != SuperView ? Dim.Width (Host) : Dim.Fill () - 0;
				_otherScrollBarView.Height = _otherScrollBarView._vertical ? Host != SuperView ? Dim.Height (Host) : Dim.Fill () - 0 : 1;
			}
		}

		int _posTopTee;
		int _posLeftTee;
		int _posBottomTee;
		int _posRightTee;

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			if (ColorScheme == null || ((!_showScrollIndicator || Size == 0) && AutoHideScrollBars && Visible)) {
				if ((!_showScrollIndicator || Size == 0) && AutoHideScrollBars && Visible) {
					ShowHideScrollBars (false);
				}
				return;
			}

			if (Size == 0 || (_vertical && Bounds.Height == 0) || (!_vertical && Bounds.Width == 0)) {
				return;
			}

			Driver.SetAttribute (Host.HasFocus ? ColorScheme.Focus : GetNormalColor ());

			if (_vertical) {
				if (Bounds.Right < Bounds.Width - 1) {
					return;
				}

				var col = Bounds.Width - 1;
				var bh = Bounds.Height;
				Rune special;

				if (bh < 4) {
					var by1 = _position * bh / Size;
					var by2 = (_position + bh) * bh / Size;

					Move (col, 0);
					if (Bounds.Height == 1) {
						Driver.AddRune (CM.Glyphs.Diamond);
					} else {
						Driver.AddRune (CM.Glyphs.UpArrow);
					}
					if (Bounds.Height == 3) {
						Move (col, 1);
						Driver.AddRune (CM.Glyphs.Diamond);
					}
					if (Bounds.Height > 1) {
						Move (col, Bounds.Height - 1);
						Driver.AddRune (CM.Glyphs.DownArrow);
					}
				} else {
					bh -= 2;
					var by1 = KeepContentAlwaysInViewport ? _position * bh / Size : _position * bh / (Size + bh);
					var by2 = KeepContentAlwaysInViewport ? Math.Min (((_position + bh) * bh / Size) + 1, bh - 1) : (_position + bh) * bh / (Size + bh);
					if (KeepContentAlwaysInViewport && by1 == by2) {
						by1 = Math.Max (by1 - 1, 0);
					}

					Move (col, 0);
					Driver.AddRune (CM.Glyphs.UpArrow);

					bool hasTopTee = false;
					bool hasDiamond = false;
					bool hasBottomTee = false;
					for (int y = 0; y < bh; y++) {
						Move (col, y + 1);
						if ((y < by1 || y > by2) && ((_position > 0 && !hasTopTee) || (hasTopTee && hasBottomTee))) {
							special = CM.Glyphs.Stipple;
						} else {
							if (y != by2 && y > 1 && by2 - by1 == 0 && by1 < bh - 1 && hasTopTee && !hasDiamond) {
								hasDiamond = true;
								special = CM.Glyphs.Diamond;
							} else {
								if (y == by1 && !hasTopTee) {
									hasTopTee = true;
									_posTopTee = y;
									special = CM.Glyphs.TopTee;
								} else if ((_position == 0 && y == bh - 1 || y >= by2 || by2 == 0) && !hasBottomTee) {
									hasBottomTee = true;
									_posBottomTee = y;
									special = CM.Glyphs.BottomTee;
								} else {
									special = CM.Glyphs.VLine;
								}
							}
						}
						Driver.AddRune (special);
					}
					if (!hasTopTee) {
						Move (col, Bounds.Height - 2);
						Driver.AddRune (CM.Glyphs.TopTee);
					}
					Move (col, Bounds.Height - 1);
					Driver.AddRune (CM.Glyphs.DownArrow);
				}
			} else {
				if (Bounds.Bottom < Bounds.Height - 1) {
					return;
				}

				var row = Bounds.Height - 1;
				var bw = Bounds.Width;
				Rune special;

				if (bw < 4) {
					var bx1 = _position * bw / Size;
					var bx2 = (_position + bw) * bw / Size;

					Move (0, row);
					Driver.AddRune (CM.Glyphs.LeftArrow);
					Driver.AddRune (CM.Glyphs.RightArrow);
				} else {
					bw -= 2;
					var bx1 = KeepContentAlwaysInViewport ? _position * bw / Size : _position * bw / (Size + bw);
					var bx2 = KeepContentAlwaysInViewport ? Math.Min (((_position + bw) * bw / Size) + 1, bw - 1) : (_position + bw) * bw / (Size + bw);
					if (KeepContentAlwaysInViewport && bx1 == bx2) {
						bx1 = Math.Max (bx1 - 1, 0);
					}

					Move (0, row);
					Driver.AddRune (CM.Glyphs.LeftArrow);

					bool hasLeftTee = false;
					bool hasDiamond = false;
					bool hasRightTee = false;
					for (int x = 0; x < bw; x++) {
						if ((x < bx1 || x >= bx2 + 1) && ((_position > 0 && !hasLeftTee) || (hasLeftTee && hasRightTee))) {
							special = CM.Glyphs.Stipple;
						} else {
							if (x != bx2 && x > 1 && bx2 - bx1 == 0 && bx1 < bw - 1 && hasLeftTee && !hasDiamond) {
								hasDiamond = true;
								special = CM.Glyphs.Diamond;
							} else {
								if (x == bx1 && !hasLeftTee) {
									hasLeftTee = true;
									_posLeftTee = x;
									special = CM.Glyphs.LeftTee;
								} else if ((_position == 0 && x == bw - 1 || x >= bx2 || bx2 == 0) && !hasRightTee) {
									hasRightTee = true;
									_posRightTee = x;
									special = CM.Glyphs.RightTee;
								} else {
									special = CM.Glyphs.HLine;
								}
							}
						}
						Driver.AddRune (special);
					}
					if (!hasLeftTee) {
						Move (Bounds.Width - 2, row);
						Driver.AddRune (CM.Glyphs.LeftTee);
					}

					Driver.AddRune (CM.Glyphs.RightArrow);
				}
			}
		}

		int _lastLocation = -1;
		int _posBarOffset;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			if (mouseEvent.Flags != MouseFlags.Button1Pressed && mouseEvent.Flags != MouseFlags.Button1DoubleClicked &&
				!mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) &&
				mouseEvent.Flags != MouseFlags.Button1Released && mouseEvent.Flags != MouseFlags.WheeledDown &&
				mouseEvent.Flags != MouseFlags.WheeledUp && mouseEvent.Flags != MouseFlags.WheeledRight &&
				mouseEvent.Flags != MouseFlags.WheeledLeft && mouseEvent.Flags != MouseFlags.Button1TripleClicked) {

				return false;
			}

			if (!Host.CanFocus) {
				return true;
			}
			if (Host?.HasFocus == false) {
				Host.SetFocus ();
			}

			int location = _vertical ? mouseEvent.Y : mouseEvent.X;
			int barsize = _vertical ? Bounds.Height : Bounds.Width;
			int posTopLeftTee = _vertical ? _posTopTee + 1 : _posLeftTee + 1;
			int posBottomRightTee = _vertical ? _posBottomTee + 1 : _posRightTee + 1;
			barsize -= 2;
			var pos = Position;

			if (mouseEvent.Flags != MouseFlags.Button1Released
				&& (Application.MouseGrabView == null || Application.MouseGrabView != this)) {
				Application.GrabMouse (this);
			} else if (mouseEvent.Flags == MouseFlags.Button1Released && Application.MouseGrabView != null && Application.MouseGrabView == this) {
				_lastLocation = -1;
				Application.UngrabMouse ();
				return true;
			}
			if (_showScrollIndicator && (mouseEvent.Flags == MouseFlags.WheeledDown || mouseEvent.Flags == MouseFlags.WheeledUp ||
				mouseEvent.Flags == MouseFlags.WheeledRight || mouseEvent.Flags == MouseFlags.WheeledLeft)) {

				return Host.MouseEvent (mouseEvent);
			}

			if (mouseEvent.Flags == MouseFlags.Button1Pressed && location == 0) {
				if (pos > 0) {
					Position = pos - 1;
				}
			} else if (mouseEvent.Flags == MouseFlags.Button1Pressed && location == barsize + 1) {
				if (CanScroll (1, out _, _vertical)) {
					Position = pos + 1;
				}
			} else if (location > 0 && location < barsize + 1) {
				//var b1 = pos * (Size > 0 ? barsize / Size : 0);
				//var b2 = Size > 0
				//	? (KeepContentAlwaysInViewport ? Math.Min (((pos + barsize) * barsize / Size) + 1, barsize - 1) : (pos + barsize) * barsize / Size)
				//	: 0;
				//if (KeepContentAlwaysInViewport && b1 == b2) {
				//	b1 = Math.Max (b1 - 1, 0);
				//}

				if (_lastLocation > -1 || (location >= posTopLeftTee && location <= posBottomRightTee
				&& mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {
					if (_lastLocation == -1) {
						_lastLocation = location;
						_posBarOffset = _keepContentAlwaysInViewport ? Math.Max (location - posTopLeftTee, 1) : 0;
						return true;
					}

					if (location > _lastLocation) {
						if (location - _posBarOffset < barsize) {
							var np = ((location - _posBarOffset) * Size / barsize) + (Size / barsize);
							if (CanScroll (np - pos, out int nv, _vertical)) {
								Position = pos + nv;
							}
						} else if (CanScroll (Size - pos, out int nv, _vertical)) {
							Position = Math.Min (pos + nv, Size);
						}
					} else if (location < _lastLocation) {
						if (location - _posBarOffset > 0) {
							var np = ((location - _posBarOffset) * Size / barsize) - (Size / barsize);
							if (CanScroll (np - pos, out int nv, _vertical)) {
								Position = pos + nv;
							}
						} else {
							Position = 0;
						}
					} else if (location - _posBarOffset >= barsize && posBottomRightTee - posTopLeftTee >= 3 && CanScroll (Size - pos, out int nv, _vertical)) {
						Position = Math.Min (pos + nv, Size);
					} else if (location - _posBarOffset >= barsize - 1 && posBottomRightTee - posTopLeftTee <= 3 && CanScroll (Size - pos, out nv, _vertical)) {
						Position = Math.Min (pos + nv, Size);
					} else if (location - _posBarOffset <= 0 && posBottomRightTee - posTopLeftTee <= 3) {
						Position = 0;
					}
				} else if (location > posBottomRightTee) {
					if (CanScroll (barsize, out int nv, _vertical)) {
						Position = pos + nv;
					}
				} else if (location < posTopLeftTee) {
					if (CanScroll (-barsize, out int nv, _vertical)) {
						Position = pos + nv;
					}
				} else if (location == 1 && posTopLeftTee <= 3) {
					Position = 0;
				} else if (location == barsize) {
					if (CanScroll (Size - pos, out int nv, _vertical)) {
						Position = Math.Min (pos + nv, Size);
					}
				}
			}

			return true;
		}

		internal bool CanScroll (int n, out int max, bool isVertical = false)
		{
			if (Host?.Bounds.IsEmpty != false) {
				max = 0;
				return false;
			}
			int s = GetBarsize (isVertical);
			var newSize = Math.Max (Math.Min (_size - s, _position + n), 0);
			max = _size > s + newSize ? (newSize == 0 ? -_position : n) : _size - (s + _position) - 1;
			if (_size >= s + newSize && max != 0) {
				return true;
			}
			return false;
		}

		int GetBarsize (bool isVertical)
		{
			if (Host?.Bounds.IsEmpty != false) {
				return 0;
			}
			return isVertical ?
				(KeepContentAlwaysInViewport ? Host.Bounds.Height + (_showBothScrollIndicator ? -2 : -1) : 0) :
				(KeepContentAlwaysInViewport ? Host.Bounds.Width + (_showBothScrollIndicator ? -2 : -1) : 0);
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}
