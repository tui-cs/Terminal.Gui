using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	public partial class View  {
		static readonly IList<View> _empty = new List<View> (0).AsReadOnly ();

		View _superView = null;

		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public virtual View SuperView {
			get {
				return _superView;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		List<View> _subviews; // This is null, and allocated on demand.
		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => _subviews?.AsReadOnly () ?? _empty;

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => _subviews ?? _empty;

		/// <summary>
		/// Returns a value indicating if this View is currently on Top (Active)
		/// </summary>
		public bool IsCurrentTop => Application.Current == this;

		/// <summary>
		/// Event fired when this view is added to another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Added;

		internal bool _addingView;

		/// <summary>
		///   Adds a subview (child) to this view.
		/// </summary>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public virtual void Add (View view)
		{
			if (view == null) {
				return;
			}
			if (_subviews == null) {
				_subviews = new List<View> ();
			}
			if (_tabIndexes == null) {
				_tabIndexes = new List<View> ();
			}
			_subviews.Add (view);
			_tabIndexes.Add (view);
			view._superView = this;
			if (view.CanFocus) {
				_addingView = true;
				if (SuperView?.CanFocus == false) {
					SuperView._addingView = true;
					SuperView.CanFocus = true;
					SuperView._addingView = false;
				}
				CanFocus = true;
				view._tabIndex = _tabIndexes.IndexOf (view);
				_addingView = false;
			}
			if (view.Enabled && !Enabled) {
				view._oldEnabled = true;
				view.Enabled = false;
			}
			SetNeedsLayout ();
			SetNeedsDisplay ();

			OnAdded (new SuperViewChangedEventArgs (this, view));
			if (IsInitialized && !view.IsInitialized) {
				view.BeginInit ();
				view.EndInit ();
			}
		}

		/// <summary>
		/// Adds the specified views (children) to the view.
		/// </summary>
		/// <param name="views">Array of one or more views (can be optional parameter).</param>
		/// <remarks>
		/// The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. 
		/// See also <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/> 
		/// </remarks>
		public void Add (params View [] views)
		{
			if (views == null) {
				return;
			}
			foreach (var view in views) {
				Add (view);
			}
		}

		/// <summary>
		/// Method invoked when a subview is being added to this view.
		/// </summary>
		/// <param name="e">Event where <see cref="ViewEventArgs.View"/> is the subview being added.</param>
		public virtual void OnAdded (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = true;
			view.OnResizeNeeded ();
			view._x ??= view._frame.X;
			view._y ??= view._frame.Y;
			view._width ??= view._frame.Width;
			view._height ??= view._frame.Height;

			view.Added?.Invoke (this, e);
		}

		/// <summary>
		/// Gets information if the view was already added to the <see cref="SuperView"/>.
		/// </summary>
		public bool IsAdded { get; private set; }

		/// <summary>
		/// Event fired when this view is removed from another.
		/// </summary>
		public event EventHandler<SuperViewChangedEventArgs> Removed;

		/// <summary>
		///   Removes all subviews (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		public virtual void RemoveAll ()
		{
			if (_subviews == null) {
				return;
			}

			while (_subviews.Count > 0) {
				Remove (_subviews [0]);
			}
		}

		/// <summary>
		///   Removes a subview added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null || _subviews == null) return;

			var touched = view.Frame;
			_subviews.Remove (view);
			_tabIndexes.Remove (view);
			view._superView = null;
			view._tabIndex = -1;
			SetNeedsLayout ();
			SetNeedsDisplay ();

			foreach (var v in _subviews) {
				if (v.Frame.IntersectsWith (touched))
					view.SetNeedsDisplay ();
			}
			OnRemoved (new SuperViewChangedEventArgs (this, view));
			if (_focused == view) {
				_focused = null;
			}
		}

		/// <summary>
		/// Method invoked when a subview is being removed from this view.
		/// </summary>
		/// <param name="e">Event args describing the subview being removed.</param>
		public virtual void OnRemoved (SuperViewChangedEventArgs e)
		{
			var view = e.Child;
			view.IsAdded = false;
			view.Removed?.Invoke (this, e);
		}


		void PerformActionForSubview (View subview, Action<View> action)
		{
			if (_subviews.Contains (subview)) {
				action (subview);
			}

			SetNeedsDisplay ();
			subview.SetNeedsDisplay ();
		}

		/// <summary>
		/// Brings the specified subview to the front so it is drawn on top of any other views.
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="SendSubviewToBack"/>.
		/// </remarks>
		public void BringSubviewToFront (View subview)
		{
			PerformActionForSubview (subview, x => {
				_subviews.Remove (x);
				_subviews.Add (x);
			});
		}

		/// <summary>
		/// Sends the specified subview to the front so it is the first view drawn
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="BringSubviewToFront(View)"/>.
		/// </remarks>
		public void SendSubviewToBack (View subview)
		{
			PerformActionForSubview (subview, x => {
				_subviews.Remove (x);
				_subviews.Insert (0, subview);
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void SendSubviewBackwards (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = _subviews.IndexOf (x);
				if (idx > 0) {
					_subviews.Remove (x);
					_subviews.Insert (idx - 1, x);
				}
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void BringSubviewForward (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = _subviews.IndexOf (x);
				if (idx + 1 < _subviews.Count) {
					_subviews.Remove (x);
					_subviews.Insert (idx + 1, x);
				}
			});
		}

		/// <summary>
		/// Get the top superview of a given <see cref="View"/>.
		/// </summary>
		/// <returns>The superview view.</returns>
		public View GetTopSuperView (View view = null, View superview = null)
		{
			View top = superview ?? Application.Top;
			for (var v = view?.SuperView ?? (this?.SuperView); v != null; v = v.SuperView) {
				top = v;
				if (top == superview) {
					break;
				}
			}

			return top;
		}



		#region Focus
		View _focused = null;

		internal enum Direction {
			Forward,
			Backward
		}

		/// <summary>
		/// Event fired when the view gets focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Enter;

		/// <summary>
		/// Event fired when the view looses focus.
		/// </summary>
		public event EventHandler<FocusEventArgs> Leave;

		Direction _focusDirection;
		internal Direction FocusDirection {
			get => SuperView?.FocusDirection ?? _focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					_focusDirection = value;
			}
		}


		// BUGBUG: v2 - Seems weird that this is in View and not Responder.
		bool _hasFocus;

		/// <inheritdoc/>
		public override bool HasFocus => _hasFocus;

		void SetHasFocus (bool value, View view, bool force = false)
		{
			if (_hasFocus != value || force) {
				_hasFocus = value;
				if (value) {
					OnEnter (view);
				} else {
					OnLeave (view);
				}
				SetNeedsDisplay ();
			}

			// Remove focus down the chain of subviews if focus is removed
			if (!value && _focused != null) {
				var f = _focused;
				f.OnLeave (view);
				f.SetHasFocus (false, view);
				_focused = null;
			}
		}

		/// <summary>
		/// Event fired when the <see cref="CanFocus"/> value is being changed.
		/// </summary>
		public event EventHandler CanFocusChanged;

		/// <inheritdoc/>
		public override void OnCanFocusChanged () => CanFocusChanged?.Invoke (this, EventArgs.Empty);

		bool _oldCanFocus;
		/// <inheritdoc/>
		public override bool CanFocus {
			get => base.CanFocus;
			set {
				if (!_addingView && IsInitialized && SuperView?.CanFocus == false && value) {
					throw new InvalidOperationException ("Cannot set CanFocus to true if the SuperView CanFocus is false!");
				}
				if (base.CanFocus != value) {
					base.CanFocus = value;

					switch (value) {
					case false when _tabIndex > -1:
						TabIndex = -1;
						break;
					case true when SuperView?.CanFocus == false && _addingView:
						SuperView.CanFocus = true;
						break;
					}

					if (value && _tabIndex == -1) {
						TabIndex = SuperView != null ? SuperView._tabIndexes.IndexOf (this) : -1;
					}
					TabStop = value;

					if (!value && SuperView?.Focused == this) {
						SuperView._focused = null;
					}
					if (!value && HasFocus) {
						SetHasFocus (false, this);
						SuperView?.EnsureFocus ();
						if (SuperView != null && SuperView.Focused == null) {
							SuperView.FocusNext ();
							if (SuperView.Focused == null) {
								Application.Current.FocusNext ();
							}
							Application.BringOverlappedTopToFront ();
						}
					}
					if (_subviews != null && IsInitialized) {
						foreach (var view in _subviews) {
							if (view.CanFocus != value) {
								if (!value) {
									view._oldCanFocus = view.CanFocus;
									view._oldTabIndex = view._tabIndex;
									view.CanFocus = false;
									view._tabIndex = -1;
								} else {
									if (_addingView) {
										view._addingView = true;
									}
									view.CanFocus = view._oldCanFocus;
									view._tabIndex = view._oldTabIndex;
									view._addingView = false;
								}
							}
						}
					}
					OnCanFocusChanged ();
					SetNeedsDisplay ();
				}
			}
		}


		/// <inheritdoc/>
		public override bool OnEnter (View view)
		{
			var args = new FocusEventArgs (view);
			Enter?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (base.OnEnter (view))
				return true;

			return false;
		}

		/// <inheritdoc/>
		public override bool OnLeave (View view)
		{
			var args = new FocusEventArgs (view);
			Leave?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (base.OnLeave (view))
				return true;

			return false;
		}

		/// <summary>
		/// Returns the currently focused view inside this view, or null if nothing is focused.
		/// </summary>
		/// <value>The focused.</value>
		public View Focused => _focused;

		/// <summary>
		/// Returns the most focused view in the chain of subviews (the leaf view that has the focus).
		/// </summary>
		/// <value>The most focused View.</value>
		public View MostFocused {
			get {
				if (Focused == null)
					return null;
				var most = Focused.MostFocused;
				if (most != null)
					return most;
				return Focused;
			}
		}

		/// <summary>
		/// Causes the specified subview to have focus.
		/// </summary>
		/// <param name="view">View.</param>
		void SetFocus (View view)
		{
			if (view == null) {
				return;
			}
			//Console.WriteLine ($"Request to focus {view}");
			if (!view.CanFocus || !view.Visible || !view.Enabled) {
				return;
			}
			if (_focused?._hasFocus == true && _focused == view) {
				return;
			}
			if ((_focused?._hasFocus == true && _focused?.SuperView == view) || view == this) {

				if (!view._hasFocus) {
					view._hasFocus = true;
				}
				return;
			}
			// Make sure that this view is a subview
			View c;
			for (c = view._superView; c != null; c = c._superView)
				if (c == this)
					break;
			if (c == null)
				throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

			if (_focused != null)
				_focused.SetHasFocus (false, view);

			var f = _focused;
			_focused = view;
			_focused.SetHasFocus (true, f);
			_focused.EnsureFocus ();

			// Send focus upwards
			if (SuperView != null) {
				SuperView.SetFocus (this);
			} else {
				SetFocus (this);
			}
		}

		/// <summary>
		/// Causes the specified view and the entire parent hierarchy to have the focused order updated.
		/// </summary>
		public void SetFocus ()
		{
			if (!CanBeVisible (this) || !Enabled) {
				if (HasFocus) {
					SetHasFocus (false, this);
				}
				return;
			}

			if (SuperView != null) {
				SuperView.SetFocus (this);
			} else {
				SetFocus (this);
			}
		}

		/// <summary>
		/// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, does nothing.
		/// </summary>
		public void EnsureFocus ()
		{
			if (_focused == null && _subviews?.Count > 0) {
				if (FocusDirection == Direction.Forward) {
					FocusFirst ();
				} else {
					FocusLast ();
				}
			}
		}

		/// <summary>
		/// Focuses the first focusable subview if one exists.
		/// </summary>
		public void FocusFirst ()
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (_tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			foreach (var view in _tabIndexes) {
				if (view.CanFocus && view._tabStop && view.Visible && view.Enabled) {
					SetFocus (view);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the last focusable subview if one exists.
		/// </summary>
		public void FocusLast ()
		{
			if (!CanBeVisible (this)) {
				return;
			}

			if (_tabIndexes == null) {
				SuperView?.SetFocus (this);
				return;
			}

			for (var i = _tabIndexes.Count; i > 0;) {
				i--;

				var v = _tabIndexes [i];
				if (v.CanFocus && v._tabStop && v.Visible && v.Enabled) {
					SetFocus (v);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the previous view.
		/// </summary>
		/// <returns><see langword="true"/> if previous was focused, <see langword="false"/> otherwise.</returns>
		public bool FocusPrev ()
		{
			if (!CanBeVisible (this)) {
				return false;
			}

			FocusDirection = Direction.Backward;
			if (_tabIndexes == null || _tabIndexes.Count == 0)
				return false;

			if (_focused == null) {
				FocusLast ();
				return _focused != null;
			}

			var focusedIdx = -1;
			for (var i = _tabIndexes.Count; i > 0;) {
				i--;
				var w = _tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusPrev ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w._tabStop && w.Visible && w.Enabled) {
					_focused.SetHasFocus (false, w);

					if (w.CanFocus && w._tabStop && w.Visible && w.Enabled)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (_focused != null) {
				_focused.SetHasFocus (false, this);
				_focused = null;
			}
			return false;
		}

		/// <summary>
		/// Focuses the next view.
		/// </summary>
		/// <returns><see langword="true"/> if next was focused, <see langword="false"/> otherwise.</returns>
		public bool FocusNext ()
		{
			if (!CanBeVisible (this)) {
				return false;
			}

			FocusDirection = Direction.Forward;
			if (_tabIndexes == null || _tabIndexes.Count == 0)
				return false;

			if (_focused == null) {
				FocusFirst ();
				return _focused != null;
			}
			var focusedIdx = -1;
			for (var i = 0; i < _tabIndexes.Count; i++) {
				var w = _tabIndexes [i];

				if (w.HasFocus) {
					if (w.FocusNext ())
						return true;
					focusedIdx = i;
					continue;
				}
				if (w.CanFocus && focusedIdx != -1 && w._tabStop && w.Visible && w.Enabled) {
					_focused.SetHasFocus (false, w);

					if (w.CanFocus && w._tabStop && w.Visible && w.Enabled)
						w.FocusFirst ();

					SetFocus (w);
					return true;
				}
			}
			if (_focused != null) {
				_focused.SetHasFocus (false, this);
				_focused = null;
			}
			return false;
		}

		View GetMostFocused (View view)
		{
			if (view == null) {
				return null;
			}

			return view._focused != null ? GetMostFocused (view._focused) : view;
		}

		/// <summary>
		///   Positions the cursor in the right position based on the currently focused view in the chain.
		/// </summary>
		///    Views that are focusable should override <see cref="PositionCursor"/> to ensure
		///    the cursor is placed in a location that makes sense. Unix terminals do not have
		///    a way of hiding the cursor, so it can be distracting to have the cursor left at
		///    the last focused view. Views should make sure that they place the cursor
		///    in a visually sensible place.
		public virtual void PositionCursor ()
		{
			if (!CanBeVisible (this) || !Enabled) {
				return;
			}

			// BUGBUG: v2 - This needs to support children of Frames too

			if (_focused == null && SuperView != null) {
				SuperView.EnsureFocus ();
			} else if (_focused?.Visible == true && _focused?.Enabled == true && _focused?.Frame.Width > 0 && _focused.Frame.Height > 0) {
				_focused.PositionCursor ();
			} else if (_focused?.Visible == true && _focused?.Enabled == false) {
				_focused = null;
			} else if (CanFocus && HasFocus && Visible && Frame.Width > 0 && Frame.Height > 0) {
				Move (TextFormatter.HotKeyPos == -1 ? 0 : TextFormatter.CursorPosition, 0);
			} else {
				Move (_frame.X, _frame.Y);
			}
		}
		#endregion Focus
	}
}
