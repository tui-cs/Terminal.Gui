using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Toplevel views can be modally executed. They are used for both an application's main view (filling the entire screeN and
	/// for pop-up views such as <see cref="Dialog"/>, <see cref="MessageBox"/>, and <see cref="Wizard"/>.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Toplevels can be modally executing views, started by calling <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>. 
	///     They return control to the caller when <see cref="Application.RequestStop(Toplevel)"/> has 
	///     been called (which sets the <see cref="Toplevel.Running"/> property to <c>false</c>). 
	///   </para>
	///   <para>
	///     A Toplevel is created when an application initializes Terminal.Gui by calling <see cref="Application.Init(ConsoleDriver, IMainLoopDriver)"/>.
	///     The application Toplevel can be accessed via <see cref="Application.Top"/>. Additional Toplevels can be created 
	///     and run (e.g. <see cref="Dialog"/>s. To run a Toplevel, create the <see cref="Toplevel"/> and 
	///     call <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>.
	///   </para>
	/// </remarks>
	public class Toplevel : View {
		/// <summary>
		/// Gets or sets whether the <see cref="MainLoop"/> for this <see cref="Toplevel"/> is running or not. 
		/// </summary>
		/// <remarks>
		///    Setting this property directly is discouraged. Use <see cref="Application.RequestStop"/> instead. 
		/// </remarks>
		public bool Running { get; set; }

		/// <summary>
		/// Invoked when the Toplevel <see cref="Application.RunState"/> has begun to be loaded.
		/// A Loaded event handler is a good place to finalize initialization before calling 
		/// <see cref="Application.RunLoop(Application.RunState, bool)"/>.
		/// </summary>
		public event EventHandler Loaded;

		/// <summary>
		/// Invoked when the Toplevel <see cref="MainLoop"/> has started it's first iteration.
		/// Subscribe to this event to perform tasks when the <see cref="Toplevel"/> has been laid out and focus has been set.
		/// changes. 
		/// <para>A Ready event handler is a good place to finalize initialization after calling 
		/// <see cref="Application.Run(Func{Exception, bool})"/> on this Toplevel.</para>
		/// </summary>
		public event EventHandler Ready;

		/// <summary>
		/// Invoked when the Toplevel <see cref="Application.RunState"/> has been unloaded.
		/// A Unloaded event handler is a good place to dispose objects after calling <see cref="Application.End(Application.RunState)"/>.
		/// </summary>
		public event EventHandler Unloaded;

		/// <summary>
		/// Invoked when the Toplevel <see cref="Application.RunState"/> becomes the <see cref="Application.Current"/> Toplevel.
		/// </summary>
		public event EventHandler<ToplevelEventArgs> Activate;

		/// <summary>
		/// Invoked when the Toplevel<see cref="Application.RunState"/> ceases to be the <see cref="Application.Current"/> Toplevel.
		/// </summary>
		public event EventHandler<ToplevelEventArgs> Deactivate;

		/// <summary>
		/// Invoked when a child of the Toplevel <see cref="Application.RunState"/> is closed by  
		/// <see cref="Application.End(Application.RunState)"/>.
		/// </summary>
		public event EventHandler<ToplevelEventArgs> ChildClosed;

		/// <summary>
		/// Invoked when the last child of the Toplevel <see cref="Application.RunState"/> is closed from 
		/// by <see cref="Application.End(Application.RunState)"/>.
		/// </summary>
		public event EventHandler AllChildClosed;

		/// <summary>
		/// Invoked when the Toplevel's <see cref="Application.RunState"/> is being closed by  
		/// <see cref="Application.RequestStop(Toplevel)"/>.
		/// </summary>
		public event EventHandler<ToplevelClosingEventArgs> Closing;

		/// <summary>
		/// Invoked when the Toplevel's <see cref="Application.RunState"/> is closed by <see cref="Application.End(Application.RunState)"/>.
		/// </summary>
		public event EventHandler<ToplevelEventArgs> Closed;

		/// <summary>
		/// Invoked when a child Toplevel's <see cref="Application.RunState"/> has been loaded.
		/// </summary>
		public event EventHandler<ToplevelEventArgs> ChildLoaded;

		/// <summary>
		/// Invoked when a cjhild Toplevel's <see cref="Application.RunState"/> has been unloaded.
		/// </summary>
		public event EventHandler<ToplevelEventArgs> ChildUnloaded;

		/// <summary>
		/// Invoked when the terminal has been resized. The new <see cref="Size"/> of the terminal is provided.
		/// </summary>
		public event EventHandler<SizeChangedEventArgs> Resized;

		internal virtual void OnResized (SizeChangedEventArgs size)
		{
			Resized?.Invoke (this, size);
		}

		internal virtual void OnChildUnloaded (Toplevel top)
		{
			ChildUnloaded?.Invoke (this, new ToplevelEventArgs (top));
		}

		internal virtual void OnChildLoaded (Toplevel top)
		{
			ChildLoaded?.Invoke (this, new ToplevelEventArgs (top));
		}

		internal virtual void OnClosed (Toplevel top)
		{
			Closed?.Invoke (this, new ToplevelEventArgs (top));
		}

		internal virtual bool OnClosing (ToplevelClosingEventArgs ev)
		{
			Closing?.Invoke (this, ev);
			return ev.Cancel;
		}

		internal virtual void OnAllChildClosed ()
		{
			AllChildClosed?.Invoke (this, EventArgs.Empty);
		}

		internal virtual void OnChildClosed (Toplevel top)
		{
			if (IsMdiContainer) {
				SetSubViewNeedsDisplay ();
			}
			ChildClosed?.Invoke (this, new ToplevelEventArgs (top));
		}

		internal virtual void OnDeactivate (Toplevel activated)
		{
			Deactivate?.Invoke (this, new ToplevelEventArgs (activated));
		}

		internal virtual void OnActivate (Toplevel deactivated)
		{
			Activate?.Invoke (this, new ToplevelEventArgs (deactivated));
		}

		/// <summary>
		/// Called from <see cref="Application.Begin(Toplevel)"/> before the <see cref="Toplevel"/> redraws for the first time. 
		/// </summary>
		virtual public void OnLoaded ()
		{
			IsLoaded = true;
			foreach (Toplevel tl in Subviews.Where (v => v is Toplevel)) {
				tl.OnLoaded ();
			}
			Loaded?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Called from <see cref="Application.RunLoop"/> after the <see cref="Toplevel"/> has entered the 
		/// first iteration of the loop.
		/// </summary>
		internal virtual void OnReady ()
		{
			foreach (Toplevel tl in Subviews.Where (v => v is Toplevel)) {
				tl.OnReady ();
			}
			Ready?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Called from <see cref="Application.End(Application.RunState)"/> before the <see cref="Toplevel"/> is disposed.
		/// </summary>
		internal virtual void OnUnloaded ()
		{
			foreach (Toplevel tl in Subviews.Where (v => v is Toplevel)) {
				tl.OnUnloaded ();
			}
			Unloaded?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Toplevel"/> class with the specified <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="frame">A superview-relative rectangle specifying the location and size for the new Toplevel</param>
		public Toplevel (Rect frame) : base (frame)
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Toplevel"/> class with <see cref="LayoutStyle.Computed"/> layout, 
		/// defaulting to full screen.
		/// </summary>
		public Toplevel () : base ()
		{
			Initialize ();
			Width = Dim.Fill ();
			Height = Dim.Fill ();
		}

		void Initialize ()
		{
			ColorScheme = Colors.TopLevel;

			Application.GrabbingMouse += Application_GrabbingMouse;
			Application.UnGrabbingMouse += Application_UnGrabbingMouse;

			// TODO: v2 - ALL Views (Responders??!?!) should support the commands related to 
			//    - Focus
			//  Move the appropriate AddCommand calls to `Responder`

			// Things this view knows how to do
			AddCommand (Command.QuitToplevel, () => { QuitToplevel (); return true; });
			AddCommand (Command.Suspend, () => { Driver.Suspend (); ; return true; });
			AddCommand (Command.NextView, () => { MoveNextView (); return true; });
			AddCommand (Command.PreviousView, () => { MovePreviousView (); return true; });
			AddCommand (Command.NextViewOrTop, () => { MoveNextViewOrTop (); return true; });
			AddCommand (Command.PreviousViewOrTop, () => { MovePreviousViewOrTop (); return true; });
			AddCommand (Command.Refresh, () => { Application.Refresh (); return true; });

			// Default keybindings for this view
			AddKeyBinding (Application.QuitKey, Command.QuitToplevel);
			AddKeyBinding (Key.Z | Key.CtrlMask, Command.Suspend);

			AddKeyBinding (Key.Tab, Command.NextView);

			AddKeyBinding (Key.CursorRight, Command.NextView);
			AddKeyBinding (Key.F | Key.CtrlMask, Command.NextView);

			AddKeyBinding (Key.CursorDown, Command.NextView);
			AddKeyBinding (Key.I | Key.CtrlMask, Command.NextView); // Unix

			AddKeyBinding (Key.BackTab | Key.ShiftMask, Command.PreviousView);
			AddKeyBinding (Key.CursorLeft, Command.PreviousView);
			AddKeyBinding (Key.CursorUp, Command.PreviousView);
			AddKeyBinding (Key.B | Key.CtrlMask, Command.PreviousView);

			AddKeyBinding (Key.Tab | Key.CtrlMask, Command.NextViewOrTop);
			AddKeyBinding (Application.AlternateForwardKey, Command.NextViewOrTop); // Needed on Unix

			AddKeyBinding (Key.Tab | Key.ShiftMask | Key.CtrlMask, Command.PreviousViewOrTop);
			AddKeyBinding (Application.AlternateBackwardKey, Command.PreviousViewOrTop); // Needed on Unix

			AddKeyBinding (Key.L | Key.CtrlMask, Command.Refresh);
		}

		private void Application_UnGrabbingMouse (object sender, GrabMouseEventArgs e)
		{
			if (Application.MouseGrabView == this && dragPosition.HasValue) {
				e.Cancel = true;
			}
		}

		private void Application_GrabbingMouse (object sender, GrabMouseEventArgs e)
		{
			if (Application.MouseGrabView == this && dragPosition.HasValue) {
				e.Cancel = true;
			}
		}

		/// <summary>
		/// Invoked when the <see cref="Application.AlternateForwardKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> AlternateForwardKeyChanged;

		/// <summary>
		/// Virtual method to invoke the <see cref="AlternateForwardKeyChanged"/> event.
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnAlternateForwardKeyChanged (KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
			AlternateForwardKeyChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Invoked when the <see cref="Application.AlternateBackwardKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> AlternateBackwardKeyChanged;

		/// <summary>
		/// Virtual method to invoke the <see cref="AlternateBackwardKeyChanged"/> event.
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnAlternateBackwardKeyChanged (KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
			AlternateBackwardKeyChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Invoked when the <see cref="Application.QuitKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> QuitKeyChanged;

		/// <summary>
		/// Virtual method to invoke the <see cref="QuitKeyChanged"/> event.
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnQuitKeyChanged (KeyChangedEventArgs e)
		{
			ReplaceKeyBinding (e.OldKey, e.NewKey);
			QuitKeyChanged?.Invoke (this, e);
		}

		/// <summary>
		/// Convenience factory method that creates a new Toplevel with the current terminal dimensions.
		/// </summary>
		/// <returns>The created Toplevel.</returns>
		public static Toplevel Create ()
		{
			return new Toplevel (new Rect (0, 0, Driver.Cols, Driver.Rows));
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Toplevel"/> can focus.
		/// </summary>
		/// <value><c>true</c> if can focus; otherwise, <c>false</c>.</value>
		public override bool CanFocus {
			get => SuperView == null ? true : base.CanFocus;
		}

		/// <summary>
		/// Determines whether the <see cref="Toplevel"/> is modal or not. 
		/// If set to <c>false</c> (the default):
		/// 
		/// <list type="bullet">
		///   <item>
		///		<description><see cref="ProcessKey(KeyEvent)"/> events will propagate keys upwards.</description>
		///   </item>
		///   <item>
		///		<description>The Toplevel will act as an embedded view (not a modal/pop-up).</description>
		///   </item>
		/// </list>
		///
		/// If set to <c>true</c>:
		/// 
		/// <list type="bullet">
		///   <item>
		///		<description><see cref="ProcessKey(KeyEvent)"/> events will NOT propogate keys upwards.</description>
		///	  </item>
		///   <item>
		///		<description>The Toplevel will and look like a modal (pop-up) (e.g. see <see cref="Dialog"/>.</description>
		///   </item>
		/// </list>
		/// </summary>
		public bool Modal { get; set; }

		/// <summary>
		/// Gets or sets the menu for this Toplevel.
		/// </summary>
		public virtual MenuBar MenuBar { get; set; }

		/// <summary>
		/// Gets or sets the status bar for this Toplevel.
		/// </summary>
		public virtual StatusBar StatusBar { get; set; }

		/// <summary>
		/// Gets or sets if this Toplevel is a Mdi container.
		/// </summary>
		public bool IsMdiContainer { get; set; }

		/// <summary>
		/// Gets or sets if this Toplevel is a Mdi child.
		/// </summary>
		public bool IsMdiChild {
			get {
				return Application.MdiTop != null && Application.MdiTop != this && !Modal;
			}
		}

		/// <summary>
		/// <see langword="true"/> if was already loaded by the <see cref="Application.Begin(Toplevel)"/>
		/// <see langword="false"/>, otherwise.
		/// </summary>
		public bool IsLoaded { get; private set; }

		///<inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (base.OnKeyDown (keyEvent)) {
				return true;
			}

			switch (keyEvent.Key) {
			case Key.AltMask:
			case Key.AltMask | Key.Space:
			case Key.CtrlMask | Key.Space:
			case Key _ when (keyEvent.Key & Key.AltMask) == Key.AltMask:
				return MenuBar != null && MenuBar.OnKeyDown (keyEvent);
			}

			return false;
		}

		///<inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (base.OnKeyUp (keyEvent)) {
				return true;
			}

			switch (keyEvent.Key) {
			case Key.AltMask:
			case Key.AltMask | Key.Space:
			case Key.CtrlMask | Key.Space:
				if (MenuBar != null && MenuBar.OnKeyUp (keyEvent)) {
					return true;
				}
				break;
			}

			return false;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (base.ProcessKey (keyEvent))
				return true;

			var result = InvokeKeybindings (new KeyEvent (ShortcutHelper.GetModifiersKey (keyEvent),
				new KeyModifiers () { Alt = keyEvent.IsAlt, Ctrl = keyEvent.IsCtrl, Shift = keyEvent.IsShift }));
			if (result != null)
				return (bool)result;

#if false
			if (keyEvent.Key == Key.F5) {
				Application.DebugDrawBounds = !Application.DebugDrawBounds;
				SetNeedsDisplay ();
				return true;
			}
#endif
			return false;
		}

		private void MovePreviousViewOrTop ()
		{
			if (Application.MdiTop == null) {
				var top = Modal ? this : Application.Top;
				top.FocusPrev ();
				if (top.Focused == null) {
					top.FocusPrev ();
				}
				top.SetNeedsDisplay ();
				Application.EnsuresTopOnFront ();
			} else {
				MovePrevious ();
			}
		}

		private void MoveNextViewOrTop ()
		{
			if (Application.MdiTop == null) {
				var top = Modal ? this : Application.Top;
				top.FocusNext ();
				if (top.Focused == null) {
					top.FocusNext ();
				}
				top.SetNeedsDisplay ();
				Application.EnsuresTopOnFront ();
			} else {
				MoveNext ();
			}
		}

		private void MovePreviousView ()
		{
			var old = GetDeepestFocusedSubview (Focused);
			if (!FocusPrev ())
				FocusPrev ();
			if (old != Focused && old != Focused?.Focused) {
				old?.SetNeedsDisplay ();
				Focused?.SetNeedsDisplay ();
			} else {
				FocusNearestView (SuperView?.TabIndexes?.Reverse (), Direction.Backward);
			}
		}

		private void MoveNextView ()
		{
			var old = GetDeepestFocusedSubview (Focused);
			if (!FocusNext ())
				FocusNext ();
			if (old != Focused && old != Focused?.Focused) {
				old?.SetNeedsDisplay ();
				Focused?.SetNeedsDisplay ();
			} else {
				FocusNearestView (SuperView?.TabIndexes, Direction.Forward);
			}
		}

		private void QuitToplevel ()
		{
			if (Application.MdiTop != null) {
				Application.MdiTop.RequestStop ();
			} else {
				Application.RequestStop ();
			}
		}

		///<inheritdoc/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			if (base.ProcessColdKey (keyEvent)) {
				return true;
			}

			if (ShortcutHelper.FindAndOpenByShortcut (keyEvent, this)) {
				return true;
			}
			return false;
		}

		View GetDeepestFocusedSubview (View view)
		{
			if (view == null) {
				return null;
			}

			foreach (var v in view.Subviews) {
				if (v.HasFocus) {
					return GetDeepestFocusedSubview (v);
				}
			}
			return view;
		}

		void FocusNearestView (IEnumerable<View> views, Direction direction)
		{
			if (views == null) {
				return;
			}

			bool found = false;
			bool focusProcessed = false;
			int idx = 0;

			foreach (var v in views) {
				if (v == this) {
					found = true;
				}
				if (found && v != this) {
					if (direction == Direction.Forward) {
						SuperView?.FocusNext ();
					} else {
						SuperView?.FocusPrev ();
					}
					focusProcessed = true;
					if (SuperView.Focused != null && SuperView.Focused != this) {
						return;
					}
				} else if (found && !focusProcessed && idx == views.Count () - 1) {
					views.ToList () [0].SetFocus ();
				}
				idx++;
			}
		}

		///<inheritdoc/>
		public override void Add (View view)
		{
			AddMenuStatusBar (view);
			base.Add (view);
		}

		internal void AddMenuStatusBar (View view)
		{
			if (view is MenuBar) {
				MenuBar = view as MenuBar;
			}
			if (view is StatusBar) {
				StatusBar = view as StatusBar;
			}
		}

		///<inheritdoc/>
		public override void Remove (View view)
		{
			if (this is Toplevel toplevel && toplevel.MenuBar != null) {
				RemoveMenuStatusBar (view);
			}
			base.Remove (view);
		}

		///<inheritdoc/>
		public override void RemoveAll ()
		{
			if (this == Application.Top) {
				MenuBar?.Dispose ();
				MenuBar = null;
				StatusBar?.Dispose ();
				StatusBar = null;
			}
			base.RemoveAll ();
		}

		internal void RemoveMenuStatusBar (View view)
		{
			if (view is MenuBar) {
				MenuBar?.Dispose ();
				MenuBar = null;
			}
			if (view is StatusBar) {
				StatusBar?.Dispose ();
				StatusBar = null;
			}
		}

		internal View EnsureVisibleBounds (Toplevel top, int x, int y,
			out int nx, out int ny, out MenuBar mb, out StatusBar sb)
		{
			int l;
			View superView;
			if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
				l = Driver.Cols;
				superView = Application.Top;
			} else {
				l = top.SuperView.Frame.Width;
				superView = top.SuperView;
			}
			var mfLength = top.Border?.DrawMarginFrame == true ? 2 : 1;
			if (top.Frame.Width <= l) {
				nx = Math.Max (x, 0);
				nx = nx + top.Frame.Width > l ? Math.Max (l - top.Frame.Width, 0) : nx;
				if (nx + mfLength > top.Frame.X + top.Frame.Width) {
					nx = Math.Max (top.Frame.Right - mfLength, 0);
				}
			} else {
				nx = x;
			}
			//System.Diagnostics.Debug.WriteLine ($"nx:{nx}, rWidth:{rWidth}");
			bool m, s;
			if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
				m = Application.Top.MenuBar?.Visible == true;
				mb = Application.Top.MenuBar;
			} else {
				var t = top.SuperView;
				while (t is not Toplevel) {
					t = t.SuperView;
				}
				m = ((Toplevel)t).MenuBar?.Visible == true;
				mb = ((Toplevel)t).MenuBar;
			}
			if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
				l = m ? 1 : 0;
			} else {
				l = 0;
			}
			ny = Math.Max (y, l);
			if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
				s = Application.Top.StatusBar?.Visible == true;
				sb = Application.Top.StatusBar;
			} else {
				var t = top.SuperView;
				while (t is not Toplevel) {
					t = t.SuperView;
				}
				s = ((Toplevel)t).StatusBar?.Visible == true;
				sb = ((Toplevel)t).StatusBar;
			}
			if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
				l = s ? Driver.Rows - 1 : Driver.Rows;
			} else {
				l = s ? top.SuperView.Frame.Height - 1 : top.SuperView.Frame.Height;
			}
			ny = Math.Min (ny, l);
			if (top.Frame.Height <= l) {
				ny = ny + top.Frame.Height >= l ? Math.Max (l - top.Frame.Height, m ? 1 : 0) : ny;
				if (ny + mfLength > top.Frame.Y + top.Frame.Height) {
					ny = Math.Max (top.Frame.Bottom - mfLength, 0);
				}
			}
			//System.Diagnostics.Debug.WriteLine ($"ny:{ny}, rHeight:{rHeight}");

			return superView;
		}

		internal void PositionToplevels ()
		{
			PositionToplevel (this);
			foreach (var top in Subviews) {
				if (top is Toplevel) {
					PositionToplevel ((Toplevel)top);
				}
			}
		}

		/// <summary>
		/// Adjusts the location and size of <paramref name="top"/> within this Toplevel.
		/// Virtual method enabling implementation of specific positions for inherited <see cref="Toplevel"/> views.
		/// </summary>
		/// <param name="top">The Toplevel to adjust.</param>
		public virtual void PositionToplevel (Toplevel top)
		{
			var superView = EnsureVisibleBounds (top, top.Frame.X, top.Frame.Y,
				out int nx, out int ny, out _, out StatusBar sb);
			bool layoutSubviews = false;
			if ((superView != top || top?.SuperView != null || (top != Application.Top && top.Modal)
				|| (top?.SuperView == null && top.IsMdiChild))
				&& (top.Frame.X + top.Frame.Width > Driver.Cols || ny > top.Frame.Y) && top.LayoutStyle == LayoutStyle.Computed) {

				if ((top.X == null || top.X is Pos.PosAbsolute) && top.Frame.X != nx) {
					top.X = nx;
					layoutSubviews = true;
				}
				if ((top.Y == null || top.Y is Pos.PosAbsolute) && top.Frame.Y != ny) {
					top.Y = ny;
					layoutSubviews = true;
				}
			}

			// TODO: v2 - This is a hack to get the StatusBar to be positioned correctly.
			if (sb != null && ny + top.Frame.Height != superView.Frame.Height - (sb.Visible ? 1 : 0)
				&& top.Height is Dim.DimFill && -top.Height.Anchor (0) < 1) {

				top.Height = Dim.Fill (sb.Visible ? 1 : 0);
				layoutSubviews = true;
			}

			if (layoutSubviews) {
				superView.LayoutSubviews ();
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (!Visible) {
				return;
			}

			if (!NeedDisplay.IsEmpty || ChildNeedsDisplay || LayoutNeeded) {
				Driver.SetAttribute (GetNormalColor ());

				// This is the Application.Top. Clear just the region we're being asked to redraw 
				// (the bounds passed to us).
				Clear ();
				Driver.SetAttribute (Enabled ? Colors.Base.Normal : Colors.Base.Disabled);

				LayoutSubviews ();
				PositionToplevels ();

				if (this == Application.MdiTop) {
					foreach (var top in Application.MdiChildes.AsEnumerable ().Reverse ()) {
						if (top.Frame.IntersectsWith (bounds)) {
							if (top != this && !top.IsCurrentTop && !OutsideTopFrame (top) && top.Visible) {
								top.SetNeedsLayout ();
								top.SetNeedsDisplay (top.Bounds);
								top.Redraw (top.Bounds);
							}
						}
					}
				}

				foreach (var view in Subviews) {
					if (view.Frame.IntersectsWith (bounds) && !OutsideTopFrame (this)) {
						view.SetNeedsLayout ();
						view.SetNeedsDisplay (view.Bounds);
					}
				}
			}

			base.Redraw (Bounds);
		}

		bool OutsideTopFrame (Toplevel top)
		{
			if (top.Frame.X > Driver.Cols || top.Frame.Y > Driver.Rows) {
				return true;
			}
			return false;
		}

		internal static Point? dragPosition;
		Point start;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			if (!CanFocus) {
				return true;
			}

			//System.Diagnostics.Debug.WriteLine ($"dragPosition before: {dragPosition.HasValue}");

			int nx, ny;
			if (!dragPosition.HasValue && (mouseEvent.Flags == MouseFlags.Button1Pressed
				|| mouseEvent.Flags == MouseFlags.Button2Pressed
				|| mouseEvent.Flags == MouseFlags.Button3Pressed)) {

				SetFocus ();
				Application.EnsuresTopOnFront ();

				// Only start grabbing if the user clicks on the title bar.
				if (mouseEvent.Y == 0 && mouseEvent.Flags == MouseFlags.Button1Pressed) {
					start = new Point (mouseEvent.X, mouseEvent.Y);
					dragPosition = new Point ();
					nx = mouseEvent.X - mouseEvent.OfX;
					ny = mouseEvent.Y - mouseEvent.OfY;
					dragPosition = new Point (nx, ny);
					Application.GrabMouse (this);
				}

				//System.Diagnostics.Debug.WriteLine ($"Starting at {dragPosition}");
				return true;
			} else if (mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) ||
				mouseEvent.Flags == MouseFlags.Button3Pressed) {
				if (dragPosition.HasValue) {
					if (SuperView == null) {
						// Redraw the entire app window using just our Frame. Since we are 
						// Application.Top, and our Frame always == our Bounds (Location is always (0,0))
						// our Frame is actually view-relative (which is what Redraw takes).
						// We need to pass all the view bounds because since the windows was 
						// moved around, we don't know exactly what was the affected region.
						Application.Top.SetNeedsDisplay ();
					} else {
						SuperView.SetNeedsDisplay ();
					}
					EnsureVisibleBounds (this, mouseEvent.X + (SuperView == null ? mouseEvent.OfX - start.X : Frame.X - start.X),
						mouseEvent.Y + (SuperView == null ? mouseEvent.OfY - start.Y : Frame.Y - start.Y),
						out nx, out ny, out _, out _);

					dragPosition = new Point (nx, ny);
					X = nx;
					Y = ny;
					//System.Diagnostics.Debug.WriteLine ($"Drag: nx:{nx},ny:{ny}");

					SetNeedsDisplay ();
					return true;
				}
			}

			if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && dragPosition.HasValue) {
				dragPosition = null;
				Application.UngrabMouse ();
			}

			//System.Diagnostics.Debug.WriteLine ($"dragPosition after: {dragPosition.HasValue}");
			//System.Diagnostics.Debug.WriteLine ($"Toplevel: {mouseEvent}");
			return false;
		}

		/// <summary>
		/// Invoked by <see cref="Application.Begin"/> as part of  <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> 
		/// after the views have been laid out, and before the views are drawn for the first time.
		/// </summary>
		public virtual void WillPresent ()
		{
			FocusFirst ();
		}

		/// <summary>
		/// Move to the next Mdi child from the <see cref="Application.MdiTop"/>.
		/// </summary>
		public virtual void MoveNext ()
		{
			Application.MoveNext ();
		}

		/// <summary>
		/// Move to the previous Mdi child from the <see cref="Application.MdiTop"/>.
		/// </summary>
		public virtual void MovePrevious ()
		{
			Application.MovePrevious ();
		}

		/// <summary>
		/// Stops and closes this <see cref="Toplevel"/>. If this Toplevel is the top-most Toplevel, 
		/// <see cref="Application.RequestStop(Toplevel)"/> will be called, causing the application to exit.
		/// </summary>
		public virtual void RequestStop ()
		{
			if (IsMdiContainer && Running
				&& (Application.Current == this
				|| Application.Current?.Modal == false
				|| Application.Current?.Modal == true && Application.Current?.Running == false)) {

				foreach (var child in Application.MdiChildes) {
					var ev = new ToplevelClosingEventArgs (this);
					if (child.OnClosing (ev)) {
						return;
					}
					child.Running = false;
					Application.RequestStop (child);
				}
				Running = false;
				Application.RequestStop (this);
			} else if (IsMdiContainer && Running && Application.Current?.Modal == true && Application.Current?.Running == true) {
				var ev = new ToplevelClosingEventArgs (Application.Current);
				if (OnClosing (ev)) {
					return;
				}
				Application.RequestStop (Application.Current);
			} else if (!IsMdiContainer && Running && (!Modal || (Modal && Application.Current != this))) {
				var ev = new ToplevelClosingEventArgs (this);
				if (OnClosing (ev)) {
					return;
				}
				Running = false;
				Application.RequestStop (this);
			} else {
				Application.RequestStop (Application.Current);
			}
		}

		/// <summary>
		/// Stops and closes the <see cref="Toplevel"/> specified by <paramref name="top"/>. If <paramref name="top"/> is the top-most Toplevel, 
		/// <see cref="Application.RequestStop(Toplevel)"/> will be called, causing the application to exit.
		/// </summary>
		/// <param name="top">The toplevel to request stop.</param>
		public virtual void RequestStop (Toplevel top)
		{
			top.RequestStop ();
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (!IsMdiContainer) {
				base.PositionCursor ();
				if (Focused == null) {
					EnsureFocus ();
					if (Focused == null) {
						Driver.SetCursorVisibility (CursorVisibility.Invisible);
					}
				}
				return;
			}

			if (Focused == null) {
				foreach (var top in Application.MdiChildes) {
					if (top != this && top.Visible) {
						top.SetFocus ();
						return;
					}
				}
			}
			base.PositionCursor ();
			if (Focused == null) {
				Driver.SetCursorVisibility (CursorVisibility.Invisible);
			}
		}

		/// <summary>
		/// Gets the current visible Toplevel Mdi child that matches the arguments pattern.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <param name="exclude">The strings to exclude.</param>
		/// <returns>The matched view.</returns>
		public View GetTopMdiChild (Type type = null, string [] exclude = null)
		{
			if (Application.MdiTop == null) {
				return null;
			}

			foreach (var top in Application.MdiChildes) {
				if (type != null && top.GetType () == type
					&& exclude?.Contains (top.Data.ToString ()) == false) {
					return top;
				} else if ((type != null && top.GetType () != type)
					|| (exclude?.Contains (top.Data.ToString ()) == true)) {
					continue;
				}
				return top;
			}
			return null;
		}

		/// <summary>
		/// Shows the Mdi child indicated by <paramref name="top"/>, setting it as <see cref="Application.Current"/>.
		/// </summary>
		/// <param name="top">The Toplevel.</param>
		/// <returns><c>true</c> if the toplevel can be shown or <c>false</c> if not.</returns>
		public virtual bool ShowChild (Toplevel top = null)
		{
			if (Application.MdiTop != null) {
				return Application.ShowChild (top == null ? this : top);
			}
			return false;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			return MostFocused?.OnEnter (view) ?? base.OnEnter (view);
		}

		///<inheritdoc/>
		public override bool OnLeave (View view)
		{
			return MostFocused?.OnLeave (view) ?? base.OnLeave (view);
		}

		///<inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			dragPosition = null;
			base.Dispose (disposing);
		}
	}

	/// <summary>
	/// Implements the <see cref="IEqualityComparer{T}"/> for comparing two <see cref="Toplevel"/>s
	/// used by <see cref="StackExtensions"/>.
	/// </summary>
	public class ToplevelEqualityComparer : IEqualityComparer<Toplevel> {
		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first object of type <see cref="Toplevel" /> to compare.</param>
		/// <param name="y">The second object of type <see cref="Toplevel" /> to compare.</param>
		/// <returns>
		///     <see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
		public bool Equals (Toplevel x, Toplevel y)
		{
			if (y == null && x == null)
				return true;
			else if (x == null || y == null)
				return false;
			else if (x.Id == y.Id)
				return true;
			else
				return false;
		}

		/// <summary>Returns a hash code for the specified object.</summary>
		/// <param name="obj">The <see cref="Toplevel" /> for which a hash code is to be returned.</param>
		/// <returns>A hash code for the specified object.</returns>
		/// <exception cref="ArgumentNullException">The type of <paramref name="obj" /> 
		/// is a reference type and <paramref name="obj" /> is <see langword="null" />.</exception>
		public int GetHashCode (Toplevel obj)
		{
			if (obj == null)
				throw new ArgumentNullException ();

			int hCode = 0;
			if (int.TryParse (obj.Id.ToString (), out int result)) {
				hCode = result;
			}
			return hCode.GetHashCode ();
		}
	}

	/// <summary>
	/// Implements the <see cref="IComparer{T}"/> to sort the <see cref="Toplevel"/> 
	/// from the <see cref="Application.MdiChildes"/> if needed.
	/// </summary>
	public sealed class ToplevelComparer : IComparer<Toplevel> {
		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero
		///             <paramref name="x" /> is less than <paramref name="y" />.Zero
		///             <paramref name="x" /> equals <paramref name="y" />.Greater than zero
		///             <paramref name="x" /> is greater than <paramref name="y" />.</returns>
		public int Compare (Toplevel x, Toplevel y)
		{
			if (ReferenceEquals (x, y))
				return 0;
			else if (x == null)
				return -1;
			else if (y == null)
				return 1;
			else
				return string.Compare (x.Id.ToString (), y.Id.ToString ());
		}
	}
}
