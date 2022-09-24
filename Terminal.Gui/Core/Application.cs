//
// Core.cs: The core engine for gui.cs
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimizations
//   - Add rendering limitation to the exposed area
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.IO;

namespace Terminal.Gui {

	/// <summary>
	/// A static, singleton class providing the main application driver for Terminal.Gui apps. 
	/// </summary>
	/// <example>
	/// <code>
	/// // A simple Terminal.Gui app that creates a window with a frame and title with 
	/// // 5 rows/columns of padding.
	/// Application.Init();
	/// var win = new Window ("Hello World - CTRL-Q to quit") {
	///     X = 5,
	///     Y = 5,
	///     Width = Dim.Fill (5),
	///     Height = Dim.Fill (5)
	/// };
	/// Application.Top.Add(win);
	/// Application.Run();
	/// </code>
	/// </example>
	/// <remarks>
	///   <para>
	///     Creates a instance of <see cref="Terminal.Gui.MainLoop"/> to process input events, handle timers and
	///     other sources of data. It is accessible via the <see cref="MainLoop"/> property.
	///   </para>
	///   <para>
	///     You can hook up to the <see cref="Iteration"/> event to have your method
	///     invoked on each iteration of the <see cref="Terminal.Gui.MainLoop"/>.
	///   </para>
	///   <para>
	///     When invoked sets the SynchronizationContext to one that is tied
	///     to the mainloop, allowing user code to use async/await.
	///   </para>
	/// </remarks>
	public static class Application {
		static Stack<Toplevel> toplevels = new Stack<Toplevel> ();

		/// <summary>
		/// The current <see cref="ConsoleDriver"/> in use.
		/// </summary>
		public static ConsoleDriver Driver;

		/// <summary>
		/// Gets all the Mdi childes which represent all the not modal <see cref="Toplevel"/> from the <see cref="MdiTop"/>.
		/// </summary>
		public static List<Toplevel> MdiChildes {
			get {
				if (MdiTop != null) {
					List<Toplevel> mdiChildes = new List<Toplevel> ();
					foreach (var top in toplevels) {
						if (top != MdiTop && !top.Modal) {
							mdiChildes.Add (top);
						}
					}
					return mdiChildes;
				}
				return null;
			}
		}

		/// <summary>
		/// The <see cref="Toplevel"/> object used for the application on startup which <see cref="Toplevel.IsMdiContainer"/> is true.
		/// </summary>
		public static Toplevel MdiTop {
			get {
				if (Top.IsMdiContainer) {
					return Top;
				}
				return null;
			}
		}

		/// <summary>
		/// The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Application.Top"/>)
		/// </summary>
		/// <value>The top.</value>
		public static Toplevel Top { get; private set; }

		/// <summary>
		/// The current <see cref="Toplevel"/> object. This is updated when <see cref="Application.Run(Func{Exception, bool})"/> enters and leaves to point to the current <see cref="Toplevel"/> .
		/// </summary>
		/// <value>The current.</value>
		public static Toplevel Current { get; private set; }

		/// <summary>
		/// The current <see cref="View"/> object that wants continuous mouse button pressed events.
		/// </summary>
		public static View WantContinuousButtonPressedView { get; private set; }

		/// <summary>
		/// The current <see cref="ConsoleDriver.HeightAsBuffer"/> used in the terminal.
		/// </summary>
		public static bool HeightAsBuffer {
			get {
				if (Driver == null) {
					throw new ArgumentNullException ("The driver must be initialized first.");
				}
				return Driver.HeightAsBuffer;
			}
			set {
				if (Driver == null) {
					throw new ArgumentNullException ("The driver must be initialized first.");
				}
				Driver.HeightAsBuffer = value;
			}
		}

		static Key alternateForwardKey = Key.PageDown | Key.CtrlMask;

		/// <summary>
		/// Alternative key to navigate forwards through all views. Ctrl+Tab is always used.
		/// </summary>
		public static Key AlternateForwardKey {
			get => alternateForwardKey;
			set {
				if (alternateForwardKey != value) {
					var oldKey = alternateForwardKey;
					alternateForwardKey = value;
					OnAlternateForwardKeyChanged (oldKey);
				}
			}
		}

		static void OnAlternateForwardKeyChanged (Key oldKey)
		{
			foreach (var top in toplevels) {
				top.OnAlternateForwardKeyChanged (oldKey);
			}
		}

		static Key alternateBackwardKey = Key.PageUp | Key.CtrlMask;

		/// <summary>
		/// Alternative key to navigate backwards through all views. Shift+Ctrl+Tab is always used.
		/// </summary>
		public static Key AlternateBackwardKey {
			get => alternateBackwardKey;
			set {
				if (alternateBackwardKey != value) {
					var oldKey = alternateBackwardKey;
					alternateBackwardKey = value;
					OnAlternateBackwardKeyChanged (oldKey);
				}
			}
		}

		static void OnAlternateBackwardKeyChanged (Key oldKey)
		{
			foreach (var top in toplevels) {
				top.OnAlternateBackwardKeyChanged (oldKey);
			}
		}

		static Key quitKey = Key.Q | Key.CtrlMask;

		/// <summary>
		/// Gets or sets the key to quit the application.
		/// </summary>
		public static Key QuitKey {
			get => quitKey;
			set {
				if (quitKey != value) {
					var oldKey = quitKey;
					quitKey = value;
					OnQuitKeyChanged (oldKey);
				}
			}
		}

		private static List<CultureInfo> supportedCultures;

		/// <summary>
		/// Gets all supported cultures by the application without the invariant language.
		/// </summary>
		public static List<CultureInfo> SupportedCultures => supportedCultures;

		static void OnQuitKeyChanged (Key oldKey)
		{
			foreach (var top in toplevels) {
				top.OnQuitKeyChanged (oldKey);
			}
		}

		/// <summary>
		/// The <see cref="MainLoop"/>  driver for the application
		/// </summary>
		/// <value>The main loop.</value>
		public static MainLoop MainLoop { get; private set; }

		/// <summary>
		/// Disable or enable the mouse in this <see cref="Application"/>
		/// </summary>
		public static bool IsMouseDisabled { get; set; }

		/// <summary>
		/// Set to true to cause the RunLoop method to exit after the first iterations.
		/// Set to false (the default) to cause the RunLoop to continue running until Application.RequestStop() is called.
		/// </summary>
		public static bool ExitRunLoopAfterFirstIteration { get; set; } = false;

		/// <summary>
		/// Notify that a new <see cref="RunState"/> token was created,
		/// used if <see cref="ExitRunLoopAfterFirstIteration"/> is true.
		/// </summary>
		public static event Action<RunState> NotifyNewRunState;

		/// <summary>
		/// Notify that a existent <see cref="RunState"/> token is stopping,
		/// used if <see cref="ExitRunLoopAfterFirstIteration"/> is true.
		/// </summary>
		public static event Action<Toplevel> NotifyStopRunState;

		/// <summary>
		///   This event is raised on each iteration of the <see cref="MainLoop"/> 
		/// </summary>
		/// <remarks>
		///   See also <see cref="Timeout"/>
		/// </remarks>
		public static Action Iteration;

		/// <summary>
		/// Returns a rectangle that is centered in the screen for the provided size.
		/// </summary>
		/// <returns>The centered rect.</returns>
		/// <param name="size">Size for the rectangle.</param>
		public static Rect MakeCenteredRect (Size size)
		{
			return new Rect (new Point ((Driver.Cols - size.Width) / 2, (Driver.Rows - size.Height) / 2), size);
		}

		//
		// provides the sync context set while executing code in Terminal.Gui, to let
		// users use async/await on their code
		//
		class MainLoopSyncContext : SynchronizationContext {
			MainLoop mainLoop;

			public MainLoopSyncContext (MainLoop mainLoop)
			{
				this.mainLoop = mainLoop;
			}

			public override SynchronizationContext CreateCopy ()
			{
				return new MainLoopSyncContext (MainLoop);
			}

			public override void Post (SendOrPostCallback d, object state)
			{
				mainLoop.AddIdle (() => {
					d (state);
					return false;
				});
				//mainLoop.Driver.Wakeup ();
			}

			public override void Send (SendOrPostCallback d, object state)
			{
				if (Thread.CurrentThread.ManagedThreadId == _mainThreadId) {
					d (state);
				} else {
					var wasExecuted = false;
					mainLoop.Invoke (() => {
						d (state);
						wasExecuted = true;
					});
					while (!wasExecuted) {
						Thread.Sleep (15);
					}
				}
			}
		}

		/// <summary>
		/// If set, it forces the use of the System.Console-based driver.
		/// </summary>
		public static bool UseSystemConsole;

		/// <summary>
		/// Initializes a new instance of <see cref="Terminal.Gui"/> Application. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Call this method once per instance (or after <see cref="Shutdown"/> has been called).
		/// </para>
		/// <para>
		/// Loads the right <see cref="ConsoleDriver"/> for the platform.
		/// </para>
		/// <para>
		/// Creates a <see cref="Toplevel"/> and assigns it to <see cref="Top"/>
		/// </para>
		/// </remarks>
		public static void Init (ConsoleDriver driver = null, IMainLoopDriver mainLoopDriver = null) => Init (() => Toplevel.Create (), driver, mainLoopDriver);

		internal static bool _initialized = false;
		internal static int _mainThreadId = -1;

		/// <summary>
		/// Initializes the Terminal.Gui application
		/// </summary>
		static void Init (Func<Toplevel> topLevelFactory, ConsoleDriver driver = null, IMainLoopDriver mainLoopDriver = null)
		{
			if (_initialized && driver == null) return;

			if (_initialized) {
				throw new InvalidOperationException ("Init must be bracketed by Shutdown");
			}

			// Used only for start debugging on Unix.
			//#if DEBUG
			//			while (!System.Diagnostics.Debugger.IsAttached) {
			//				System.Threading.Thread.Sleep (100);
			//			}
			//			System.Diagnostics.Debugger.Break ();
			//#endif

			// Reset all class variables (Application is a singleton).
			ResetState ();

			// This supports Unit Tests and the passing of a mock driver/loopdriver
			if (driver != null) {
				if (mainLoopDriver == null) {
					throw new ArgumentNullException ("mainLoopDriver cannot be null if driver is provided.");
				}
				Driver = driver;
				Driver.Init (TerminalResized);
				MainLoop = new MainLoop (mainLoopDriver);
				SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			}

			if (Driver == null) {
				var p = Environment.OSVersion.Platform;
				if (UseSystemConsole) {
					Driver = new NetDriver ();
					mainLoopDriver = new NetMainLoop (Driver);
				} else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
					Driver = new WindowsDriver ();
					mainLoopDriver = new WindowsMainLoop (Driver);
				} else {
					mainLoopDriver = new UnixMainLoop ();
					Driver = new CursesDriver ();
				}
				Driver.Init (TerminalResized);
				MainLoop = new MainLoop (mainLoopDriver);
				SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			}
			Top = topLevelFactory ();
			Current = Top;
			supportedCultures = GetSupportedCultures ();
			_mainThreadId = Thread.CurrentThread.ManagedThreadId;
			_initialized = true;
		}

		/// <summary>
		/// Captures the execution state for the provided <see cref="Toplevel"/>  view.
		/// </summary>
		public class RunState : IDisposable {
			/// <summary>
			/// Initializes a new <see cref="RunState"/> class.
			/// </summary>
			/// <param name="view"></param>
			public RunState (Toplevel view)
			{
				Toplevel = view;
			}
			/// <summary>
			/// The <see cref="Toplevel"/> belong to this <see cref="RunState"/>.
			/// </summary>
			public Toplevel Toplevel { get; internal set; }

			/// <summary>
			/// Releases alTop = l resource used by the <see cref="Application.RunState"/> object.
			/// </summary>
			/// <remarks>Call <see cref="Dispose()"/> when you are finished using the <see cref="Application.RunState"/>. The
			/// <see cref="Dispose()"/> method leaves the <see cref="Application.RunState"/> in an unusable state. After
			/// calling <see cref="Dispose()"/>, you must release all references to the
			/// <see cref="Application.RunState"/> so the garbage collector can reclaim the memory that the
			/// <see cref="Application.RunState"/> was occupying.</remarks>
			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			/// <summary>
			/// Dispose the specified disposing.
			/// </summary>
			/// <returns>The dispose.</returns>
			/// <param name="disposing">If set to <c>true</c> disposing.</param>
			protected virtual void Dispose (bool disposing)
			{
				if (Toplevel != null && disposing) {
					End (Toplevel);
					Toplevel.Dispose ();
					Toplevel = null;
				}
			}
		}

		static void ProcessKeyEvent (KeyEvent ke)
		{
			if (RootKeyEvent?.Invoke (ke) ?? false) {
				return;
			}

			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.ProcessHotKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}

			foreach (var topLevel in chain) {
				if (topLevel.ProcessKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}

			foreach (var topLevel in chain) {
				// Process the key normally
				if (topLevel.ProcessColdKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}

		static void ProcessKeyDownEvent (KeyEvent ke)
		{
			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.OnKeyDown (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}


		static void ProcessKeyUpEvent (KeyEvent ke)
		{
			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.OnKeyUp (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}

		static View FindDeepestTop (Toplevel start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			if (toplevels != null) {
				int count = toplevels.Count;
				if (count > 0) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					foreach (var t in toplevels) {
						if (t != Current) {
							if (t != start && t.Visible && t.Frame.Contains (rx, ry)) {
								start = t;
								break;
							}
						}
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static View FindDeepestMdiView (View start, int x, int y, out int resx, out int resy)
		{
			if (start.GetType ().BaseType != typeof (Toplevel)
				&& !((Toplevel)start).IsMdiContainer) {
				resx = 0;
				resy = 0;
				return null;
			}

			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			int count = toplevels.Count;
			for (int i = count - 1; i >= 0; i--) {
				foreach (var top in toplevels) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					if (top.Visible && top.Frame.Contains (rx, ry)) {
						var deep = FindDeepestView (top, rx, ry, out resx, out resy);
						if (deep == null)
							return FindDeepestMdiView (top, rx, ry, out resx, out resy);
						if (deep != MdiTop)
							return deep;
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static View FindDeepestView (View start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			if (start.InternalSubviews != null) {
				int count = start.InternalSubviews.Count;
				if (count > 0) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					for (int i = count - 1; i >= 0; i--) {
						View v = start.InternalSubviews [i];
						if (v.Visible && v.Frame.Contains (rx, ry)) {
							var deep = FindDeepestView (v, rx, ry, out resx, out resy);
							if (deep == null)
								return v;
							return deep;
						}
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static View FindTopFromView (View view)
		{
			View top = view?.SuperView != null && view?.SuperView != Top
				? view.SuperView : view;

			while (top?.SuperView != null && top?.SuperView != Top) {
				top = top.SuperView;
			}
			return top;
		}

		static View mouseGrabView;

		/// <summary>
		/// The view that grabbed the mouse, to where will be routed all the mouse events.
		/// </summary>
		public static View MouseGrabView => mouseGrabView;

		/// <summary>
		/// Event to be invoked when a view grab the mouse.
		/// </summary>
		public static event Action<View> GrabbedMouse;

		/// <summary>
		/// Event to be invoked when a view ungrab the mouse.
		/// </summary>
		public static event Action<View> UnGrabbedMouse;

		/// <summary>
		/// Grabs the mouse, forcing all mouse events to be routed to the specified view until UngrabMouse is called.
		/// </summary>
		/// <returns>The grab.</returns>
		/// <param name="view">View that will receive all mouse events until UngrabMouse is invoked.</param>
		public static void GrabMouse (View view)
		{
			if (view == null)
				return;
			OnGrabbedMouse (view);
			mouseGrabView = view;
			Driver.UncookMouse ();
		}

		/// <summary>
		/// Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.
		/// </summary>
		public static void UngrabMouse ()
		{
			if (mouseGrabView == null)
				return;
			OnUnGrabbedMouse (mouseGrabView);
			mouseGrabView = null;
			Driver.CookMouse ();
		}

		static void OnGrabbedMouse (View view)
		{
			if (view == null)
				return;
			GrabbedMouse?.Invoke (view);
		}

		static void OnUnGrabbedMouse (View view)
		{
			if (view == null)
				return;
			UnGrabbedMouse?.Invoke (view);
		}

		/// <summary>
		/// Merely a debugging aid to see the raw mouse events
		/// </summary>
		public static Action<MouseEvent> RootMouseEvent;

		/// <summary>
		/// <para>
		/// Called for new KeyPress events before any processing is performed or
		/// views evaluate.  Use for global key handling and/or debugging.
		/// </para>
		/// <para>Return true to suppress the KeyPress event</para>
		/// </summary>
		public static Func<KeyEvent, bool> RootKeyEvent;

		static View lastMouseOwnerView;

		static void ProcessMouseEvent (MouseEvent me)
		{
			if (IsMouseDisabled) {
				return;
			}

			var view = FindDeepestView (Current, me.X, me.Y, out int rx, out int ry);

			if (view != null && view.WantContinuousButtonPressed)
				WantContinuousButtonPressedView = view;
			else
				WantContinuousButtonPressedView = null;
			if (view != null) {
				me.View = view;
			}
			RootMouseEvent?.Invoke (me);
			if (mouseGrabView != null) {
				if (view == null) {
					UngrabMouse ();
					return;
				}

				var newxy = mouseGrabView.ScreenToView (me.X, me.Y);
				var nme = new MouseEvent () {
					X = newxy.X,
					Y = newxy.Y,
					Flags = me.Flags,
					OfX = me.X - newxy.X,
					OfY = me.Y - newxy.Y,
					View = view
				};
				if (OutsideFrame (new Point (nme.X, nme.Y), mouseGrabView.Frame)) {
					lastMouseOwnerView?.OnMouseLeave (me);
				}
				// System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
				if (mouseGrabView?.OnMouseEvent (nme) == true) {
					return;
				}
			}

			if ((view == null || view == MdiTop) && !Current.Modal && MdiTop != null
				&& me.Flags != MouseFlags.ReportMousePosition && me.Flags != 0) {

				var top = FindDeepestTop (Top, me.X, me.Y, out _, out _);
				view = FindDeepestView (top, me.X, me.Y, out rx, out ry);

				if (view != null && view != MdiTop && top != Current) {
					MoveCurrent ((Toplevel)top);
				}
			}

			if (view != null) {
				var nme = new MouseEvent () {
					X = rx,
					Y = ry,
					Flags = me.Flags,
					OfX = 0,
					OfY = 0,
					View = view
				};

				if (lastMouseOwnerView == null) {
					lastMouseOwnerView = view;
					view.OnMouseEnter (nme);
				} else if (lastMouseOwnerView != view) {
					lastMouseOwnerView.OnMouseLeave (nme);
					view.OnMouseEnter (nme);
					lastMouseOwnerView = view;
				}

				if (!view.WantMousePositionReports && me.Flags == MouseFlags.ReportMousePosition)
					return;

				if (view.WantContinuousButtonPressed)
					WantContinuousButtonPressedView = view;
				else
					WantContinuousButtonPressedView = null;

				// Should we bubbled up the event, if it is not handled?
				view.OnMouseEvent (nme);

				EnsuresTopOnFront ();
			}
		}

		// Only return true if the Current has changed.
		static bool MoveCurrent (Toplevel top)
		{
			// The Current is modal and the top is not modal toplevel then
			// the Current must be moved above the first not modal toplevel.
			if (MdiTop != null && top != MdiTop && top != Current && Current?.Modal == true && !toplevels.Peek ().Modal) {
				lock (toplevels) {
					toplevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
				}
				var index = 0;
				var savedToplevels = toplevels.ToArray ();
				foreach (var t in savedToplevels) {
					if (!t.Modal && t != Current && t != top && t != savedToplevels [index]) {
						lock (toplevels) {
							toplevels.MoveTo (top, index, new ToplevelEqualityComparer ());
						}
					}
					index++;
				}
				return false;
			}
			// The Current and the top are both not running toplevel then
			// the top must be moved above the first not running toplevel.
			if (MdiTop != null && top != MdiTop && top != Current && Current?.Running == false && !top.Running) {
				lock (toplevels) {
					toplevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
				}
				var index = 0;
				foreach (var t in toplevels.ToArray ()) {
					if (!t.Running && t != Current && index > 0) {
						lock (toplevels) {
							toplevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
						}
					}
					index++;
				}
				return false;
			}
			if ((MdiTop != null && top?.Modal == true && toplevels.Peek () != top)
				|| (MdiTop != null && Current != MdiTop && Current?.Modal == false && top == MdiTop)
				|| (MdiTop != null && Current?.Modal == false && top != Current)
				|| (MdiTop != null && Current?.Modal == true && top == MdiTop)) {
				lock (toplevels) {
					toplevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
					Current = top;
				}
			}
			return true;
		}

		static bool OutsideFrame (Point p, Rect r)
		{
			return p.X < 0 || p.X > r.Width - 1 || p.Y < 0 || p.Y > r.Height - 1;
		}

		/// <summary>
		/// Building block API: Prepares the provided <see cref="Toplevel"/>  for execution.
		/// </summary>
		/// <returns>The runstate handle that needs to be passed to the <see cref="End(RunState)"/> method upon completion.</returns>
		/// <param name="toplevel">Toplevel to prepare execution for.</param>
		/// <remarks>
		///  This method prepares the provided toplevel for running with the focus,
		///  it adds this to the list of toplevels, sets up the mainloop to process the
		///  event, lays out the subviews, focuses the first element, and draws the
		///  toplevel in the screen. This is usually followed by executing
		///  the <see cref="RunLoop"/> method, and then the <see cref="End(RunState)"/> method upon termination which will
		///   undo these changes.
		/// </remarks>
		public static RunState Begin (Toplevel toplevel)
		{
			if (toplevel == null) {
				throw new ArgumentNullException (nameof (toplevel));
			} else if (toplevel.IsMdiContainer && MdiTop != null) {
				throw new InvalidOperationException ("Only one Mdi Container is allowed.");
			}

			var rs = new RunState (toplevel);

			Init ();
			if (toplevel is ISupportInitializeNotification initializableNotification &&
			    !initializableNotification.IsInitialized) {
				initializableNotification.BeginInit ();
				initializableNotification.EndInit ();
			} else if (toplevel is ISupportInitialize initializable) {
				initializable.BeginInit ();
				initializable.EndInit ();
			}

			lock (toplevels) {
				if (string.IsNullOrEmpty (toplevel.Id.ToString ())) {
					var count = 1;
					var id = (toplevels.Count + count).ToString ();
					while (toplevels.Count > 0 && toplevels.FirstOrDefault (x => x.Id.ToString () == id) != null) {
						count++;
						id = (toplevels.Count + count).ToString ();
					}
					toplevel.Id = (toplevels.Count + count).ToString ();

					toplevels.Push (toplevel);
				} else {
					var dup = toplevels.FirstOrDefault (x => x.Id.ToString () == toplevel.Id);
					if (dup == null) {
						toplevels.Push (toplevel);
					}
				}

				if (toplevels.FindDuplicates (new ToplevelEqualityComparer ()).Count > 0) {
					throw new ArgumentException ("There are duplicates toplevels Id's");
				}
			}
			if (toplevel.IsMdiContainer) {
				Top = toplevel;
			}

			var refreshDriver = true;
			if (MdiTop == null || toplevel.IsMdiContainer || (Current?.Modal == false && toplevel.Modal)
				|| (Current?.Modal == false && !toplevel.Modal) || (Current?.Modal == true && toplevel.Modal)) {

				if (toplevel.Visible) {
					Current = toplevel;
					SetCurrentAsTop ();
				} else {
					refreshDriver = false;
				}
			} else if ((MdiTop != null && toplevel != MdiTop && Current?.Modal == true && !toplevels.Peek ().Modal)
				|| (MdiTop != null && toplevel != MdiTop && Current?.Running == false)) {
				refreshDriver = false;
				MoveCurrent (toplevel);
			} else {
				refreshDriver = false;
				MoveCurrent (Current);
			}

			Driver.PrepareToRun (MainLoop, ProcessKeyEvent, ProcessKeyDownEvent, ProcessKeyUpEvent, ProcessMouseEvent);
			if (toplevel.LayoutStyle == LayoutStyle.Computed)
				toplevel.SetRelativeLayout (new Rect (0, 0, Driver.Cols, Driver.Rows));
			toplevel.LayoutSubviews ();
			toplevel.PositionToplevels ();
			toplevel.WillPresent ();
			if (refreshDriver) {
				if (MdiTop != null) {
					MdiTop.OnChildLoaded (toplevel);
				}
				toplevel.OnLoaded ();
				Redraw (toplevel);
				toplevel.PositionCursor ();
				Driver.Refresh ();
			}

			return rs;
		}

		/// <summary>
		/// Building block API: completes the execution of a <see cref="Toplevel"/>  that was started with <see cref="Begin(Toplevel)"/> .
		/// </summary>
		/// <param name="runState">The runstate returned by the <see cref="Begin(Toplevel)"/> method.</param>
		public static void End (RunState runState)
		{
			if (runState == null)
				throw new ArgumentNullException (nameof (runState));

			if (MdiTop != null) {
				MdiTop.OnChildUnloaded (runState.Toplevel);
			} else {
				runState.Toplevel.OnUnloaded ();
			}
			runState.Dispose ();
		}

		/// <summary>
		/// Shutdown an application initialized with <see cref="Init(ConsoleDriver, IMainLoopDriver)"/>
		/// </summary>
		public static void Shutdown ()
		{
			ResetState ();
		}

		// Encapsulate all setting of initial state for Application; Having
		// this in a function like this ensures we don't make mistakes in
		// guaranteeing that the state of this singleton is deterministic when Init
		// starts running and after Shutdown returns.
		static void ResetState ()
		{
			// Shutdown is the bookend for Init. As such it needs to clean up all resources
			// Init created. Apps that do any threading will need to code defensively for this.
			// e.g. see Issue #537
			// TODO: Some of this state is actually related to Begin/End (not Init/Shutdown) and should be moved to `RunState` (#520)
			foreach (var t in toplevels) {
				t.Running = false;
				t.Dispose ();
			}
			toplevels.Clear ();
			Current = null;
			Top = null;

			MainLoop = null;
			Driver?.End ();
			Driver = null;
			Iteration = null;
			RootMouseEvent = null;
			RootKeyEvent = null;
			Resized = null;
			_mainThreadId = -1;
			NotifyNewRunState = null;
			NotifyStopRunState = null;
			_initialized = false;
			mouseGrabView = null;

			// Reset synchronization context to allow the user to run async/await,
			// as the main loop has been ended, the synchronization context from 
			// gui.cs does no longer process any callbacks. See #1084 for more details:
			// (https://github.com/gui-cs/Terminal.Gui/issues/1084).
			SynchronizationContext.SetSynchronizationContext (syncContext: null);
		}


		static void Redraw (View view)
		{
			view.Redraw (view.Bounds);
			Driver.Refresh ();
		}

		static void Refresh (View view)
		{
			view.Redraw (view.Bounds);
			Driver.Refresh ();
		}

		/// <summary>
		/// Triggers a refresh of the entire display.
		/// </summary>
		public static void Refresh ()
		{
			Driver.UpdateOffScreen ();
			View last = null;
			foreach (var v in toplevels.Reverse ()) {
				if (v.Visible) {
					v.SetNeedsDisplay ();
					v.Redraw (v.Bounds);
				}
				last = v;
			}
			last?.PositionCursor ();
			Driver.Refresh ();
		}

		internal static void End (View view)
		{
			if (toplevels.Peek () != view)
				throw new ArgumentException ("The view that you end with must be balanced");
			toplevels.Pop ();

			(view as Toplevel)?.OnClosed ((Toplevel)view);

			if (MdiTop != null && !((Toplevel)view).Modal && view != MdiTop) {
				MdiTop.OnChildClosed (view as Toplevel);
			}

			if (toplevels.Count == 0) {
				Current = null;
			} else {
				Current = toplevels.Peek ();
				if (toplevels.Count == 1 && Current == MdiTop) {
					MdiTop.OnAllChildClosed ();
				} else {
					SetCurrentAsTop ();
				}
				Refresh ();
			}
		}

		/// <summary>
		///   Building block API: Runs the main loop for the created dialog
		/// </summary>
		/// <remarks>
		///   Use the wait parameter to control whether this is a
		///   blocking or non-blocking call.
		/// </remarks>
		/// <param name="state">The state returned by the Begin method.</param>
		/// <param name="wait">By default this is true which will execute the runloop waiting for events, if you pass false, you can use this method to run a single iteration of the events.</param>
		public static void RunLoop (RunState state, bool wait = true)
		{
			if (state == null)
				throw new ArgumentNullException (nameof (state));
			if (state.Toplevel == null)
				throw new ObjectDisposedException ("state");

			bool firstIteration = true;
			for (state.Toplevel.Running = true; state.Toplevel.Running;) {
				if (ExitRunLoopAfterFirstIteration && !firstIteration)
					return;
				RunMainLoopIteration (ref state, wait, ref firstIteration);
			}
		}

		/// <summary>
		/// Run one iteration of the MainLoop.
		/// </summary>
		/// <param name="state">The state returned by the Begin method.</param>
		/// <param name="wait">If will execute the runloop waiting for events.</param>
		/// <param name="firstIteration">If it's the first run loop iteration.</param>
		public static void RunMainLoopIteration (ref RunState state, bool wait, ref bool firstIteration)
		{
			if (MainLoop.EventsPending (wait)) {
				// Notify Toplevel it's ready
				if (firstIteration) {
					state.Toplevel.OnReady ();
				}

				MainLoop.MainIteration ();
				Iteration?.Invoke ();

				EnsureModalOrVisibleAlwaysOnTop (state.Toplevel);
				if ((state.Toplevel != Current && Current?.Modal == true)
					|| (state.Toplevel != Current && Current?.Modal == false)) {
					MdiTop?.OnDeactivate (state.Toplevel);
					state.Toplevel = Current;
					MdiTop?.OnActivate (state.Toplevel);
					Top.SetChildNeedsDisplay ();
					Refresh ();
				}
				if (Driver.EnsureCursorVisibility ()) {
					state.Toplevel.SetNeedsDisplay ();
				}
			} else if (!wait) {
				return;
			}
			firstIteration = false;

			if (state.Toplevel != Top
				&& (!Top.NeedDisplay.IsEmpty || Top.ChildNeedsDisplay || Top.LayoutNeeded)) {
				Top.Redraw (Top.Bounds);
				foreach (var top in toplevels.Reverse ()) {
					if (top != Top && top != state.Toplevel) {
						top.SetNeedsDisplay ();
						top.Redraw (top.Bounds);
					}
				}
				state.Toplevel.SetNeedsDisplay (state.Toplevel.Bounds);
			}
			if (!state.Toplevel.NeedDisplay.IsEmpty || state.Toplevel.ChildNeedsDisplay || state.Toplevel.LayoutNeeded
				|| MdiChildNeedsDisplay ()) {
				state.Toplevel.Redraw (state.Toplevel.Bounds);
				if (DebugDrawBounds) {
					DrawBounds (state.Toplevel);
				}
				state.Toplevel.PositionCursor ();
				Driver.Refresh ();
			} else {
				Driver.UpdateCursor ();
			}
			if (state.Toplevel != Top && !state.Toplevel.Modal
				&& (!Top.NeedDisplay.IsEmpty || Top.ChildNeedsDisplay || Top.LayoutNeeded)) {
				Top.Redraw (Top.Bounds);
			}
		}

		static void EnsureModalOrVisibleAlwaysOnTop (Toplevel toplevel)
		{
			if (!toplevel.Running || (toplevel == Current && toplevel.Visible) || MdiTop == null || toplevels.Peek ().Modal) {
				return;
			}

			foreach (var top in toplevels.Reverse ()) {
				if (top.Modal && top != Current) {
					MoveCurrent (top);
					return;
				}
			}
			if (!toplevel.Visible && toplevel == Current) {
				MoveNext ();
			}
		}

		static bool MdiChildNeedsDisplay ()
		{
			if (MdiTop == null) {
				return false;
			}

			foreach (var top in toplevels) {
				if (top != Current && top.Visible && (!top.NeedDisplay.IsEmpty || top.ChildNeedsDisplay || top.LayoutNeeded)) {
					MdiTop.SetChildNeedsDisplay ();
					return true;
				}
			}
			return false;
		}

		internal static bool DebugDrawBounds = false;

		// Need to look into why this does not work properly.
		static void DrawBounds (View v)
		{
			v.DrawFrame (v.Frame, padding: 0, fill: false);
			if (v.InternalSubviews != null && v.InternalSubviews.Count > 0)
				foreach (var sub in v.InternalSubviews)
					DrawBounds (sub);
		}

		/// <summary>
		/// Runs the application by calling <see cref="Run(Toplevel, Func{Exception, bool})"/> with the value of <see cref="Top"/>
		/// </summary>
		public static void Run (Func<Exception, bool> errorHandler = null)
		{
			Run (Top, errorHandler);
		}

		/// <summary>
		/// Runs the application by calling <see cref="Run(Toplevel, Func{Exception, bool})"/> with a new instance of the specified <see cref="Toplevel"/>-derived class
		/// </summary>
		public static void Run<T> (Func<Exception, bool> errorHandler = null) where T : Toplevel, new()
		{
			if (_initialized && Driver != null) {
				var top = new T ();
				var type = top.GetType ().BaseType;
				while (type != typeof (Toplevel) && type != typeof (object)) {
					type = type.BaseType;
				}
				if (type != typeof (Toplevel)) {
					throw new ArgumentException ($"{top.GetType ().Name} must be derived from TopLevel");
				}
				Run (top, errorHandler);
			} else {
				Init (() => new T ());
				Run (Top, errorHandler);
			}
		}

		/// <summary>
		///   Runs the main loop on the given <see cref="Toplevel"/> container.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method is used to start processing events
		///     for the main application, but it is also used to
		///     run other modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
		///   </para>
		///   <para>
		///     To make a <see cref="Run(Toplevel, Func{Exception, bool})"/> stop execution, call <see cref="Application.RequestStop"/>.
		///   </para>
		///   <para>
		///     Calling <see cref="Run(Toplevel, Func{Exception, bool})"/> is equivalent to calling <see cref="Begin(Toplevel)"/>, followed by <see cref="RunLoop(RunState, bool)"/>,
		///     and then calling <see cref="End(RunState)"/>.
		///   </para>
		///   <para>
		///     Alternatively, to have a program control the main loop and 
		///     process events manually, call <see cref="Begin(Toplevel)"/> to set things up manually and then
		///     repeatedly call <see cref="RunLoop(RunState, bool)"/> with the wait parameter set to false.   By doing this
		///     the <see cref="RunLoop(RunState, bool)"/> method will only process any pending events, timers, idle handlers and
		///     then return control immediately.
		///   </para>
		///   <para>
		///     When <paramref name="errorHandler"/> is null the exception is rethrown, when it returns true the application is resumed and when false method exits gracefully.
		///   </para>
		/// </remarks>
		/// <param name="view">The <see cref="Toplevel"/> to run modally.</param>
		/// <param name="errorHandler">Handler for any unhandled exceptions (resumes when returns true, rethrows when null).</param>
		public static void Run (Toplevel view, Func<Exception, bool> errorHandler = null)
		{
			var resume = true;
			while (resume) {
#if !DEBUG
				try {
#endif
				resume = false;
				var runToken = Begin (view);
				RunLoop (runToken);
				if (!ExitRunLoopAfterFirstIteration)
					End (runToken);
				else
					// If ExitRunLoopAfterFirstIteration is true then the user must deal his disposing when it ends
					// by using NotifyStopRunState event.
					NotifyNewRunState?.Invoke (runToken);
#if !DEBUG
				}
				catch (Exception error)
				{
					if (errorHandler == null)
					{
						throw;
					}
					resume = errorHandler(error);
				}
#endif
			}
		}

		/// <summary>
		/// Stops running the most recent <see cref="Toplevel"/> or the <paramref name="top"/> if provided.
		/// </summary>
		/// <param name="top">The toplevel to request stop.</param>
		/// <remarks>
		///   <para>
		///   This will cause <see cref="Application.Run(Func{Exception, bool})"/> to return.
		///   </para>
		///   <para>
		///     Calling <see cref="Application.RequestStop"/> is equivalent to setting the <see cref="Toplevel.Running"/> property on the currently running <see cref="Toplevel"/> to false.
		///   </para>
		/// </remarks>
		public static void RequestStop (Toplevel top = null)
		{
			if (MdiTop == null || top == null || (MdiTop == null && top != null)) {
				top = Current;
			}

			if (MdiTop != null && top.IsMdiContainer && top?.Running == true
				&& (Current?.Modal == false || (Current?.Modal == true && Current?.Running == false))) {

				MdiTop.RequestStop ();
			} else if (MdiTop != null && top != Current && Current?.Running == true && Current?.Modal == true
				&& top.Modal && top.Running) {

				var ev = new ToplevelClosingEventArgs (Current);
				Current.OnClosing (ev);
				if (ev.Cancel) {
					return;
				}
				ev = new ToplevelClosingEventArgs (top);
				top.OnClosing (ev);
				if (ev.Cancel) {
					return;
				}
				Current.Running = false;
				OnNotifyStopRunState (Current);
				top.Running = false;
				OnNotifyStopRunState (top);
			} else if ((MdiTop != null && top != MdiTop && top != Current && Current?.Modal == false
				&& Current?.Running == true && !top.Running)
				|| (MdiTop != null && top != MdiTop && top != Current && Current?.Modal == false
				&& Current?.Running == false && !top.Running && toplevels.ToArray () [1].Running)) {

				MoveCurrent (top);
			} else if (MdiTop != null && Current != top && Current?.Running == true && !top.Running
				&& Current?.Modal == true && top.Modal) {
				// The Current and the top are both modal so needed to set the Current.Running to false too.
				Current.Running = false;
				OnNotifyStopRunState (Current);
			} else if (MdiTop != null && Current == top && MdiTop?.Running == true && Current?.Running == true && top.Running
				&& Current?.Modal == true && top.Modal) {
				// The MdiTop was requested to stop inside a modal toplevel which is the Current and top,
				// both are the same, so needed to set the Current.Running to false too.
				Current.Running = false;
				OnNotifyStopRunState (Current);
			} else {
				Toplevel currentTop;
				if (top == Current || (Current?.Modal == true && !top.Modal)) {
					currentTop = Current;
				} else {
					currentTop = top;
				}
				if (!currentTop.Running) {
					return;
				}
				var ev = new ToplevelClosingEventArgs (currentTop);
				currentTop.OnClosing (ev);
				if (ev.Cancel) {
					return;
				}
				currentTop.Running = false;
				OnNotifyStopRunState (currentTop);
			}
		}

		static void OnNotifyStopRunState (Toplevel top)
		{
			if (ExitRunLoopAfterFirstIteration)
				NotifyStopRunState?.Invoke (top);
		}

		/// <summary>
		/// Event arguments for the <see cref="Application.Resized"/> event.
		/// </summary>
		public class ResizedEventArgs : EventArgs {
			/// <summary>
			/// The number of rows in the resized terminal.
			/// </summary>
			public int Rows { get; set; }
			/// <summary>
			/// The number of columns in the resized terminal.
			/// </summary>
			public int Cols { get; set; }
		}

		/// <summary>
		/// Invoked when the terminal was resized. The new size of the terminal is provided.
		/// </summary>
		public static Action<ResizedEventArgs> Resized;

		static void TerminalResized ()
		{
			var full = new Rect (0, 0, Driver.Cols, Driver.Rows);
			SetToplevelsSize (full);
			Resized?.Invoke (new ResizedEventArgs () { Cols = full.Width, Rows = full.Height });
			Driver.Clip = full;
			foreach (var t in toplevels) {
				t.SetRelativeLayout (full);
				t.LayoutSubviews ();
				t.PositionToplevels ();
				t.OnResized (full.Size);
			}
			Refresh ();
		}

		static void SetToplevelsSize (Rect full)
		{
			if (MdiTop == null) {
				foreach (var t in toplevels) {
					if (t?.SuperView == null && !t.Modal) {
						t.Frame = full;
						t.Width = full.Width;
						t.Height = full.Height;
					}
				}
			} else {
				Top.Frame = full;
				Top.Width = full.Width;
				Top.Height = full.Height;
			}
		}

		static bool SetCurrentAsTop ()
		{
			if (MdiTop == null && Current != Top && Current?.SuperView == null && Current?.Modal == false) {
				if (Current.Frame != new Rect (0, 0, Driver.Cols, Driver.Rows)) {
					Current.Frame = new Rect (0, 0, Driver.Cols, Driver.Rows);
				}
				Top = Current;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Move to the next Mdi child from the <see cref="MdiTop"/>.
		/// </summary>
		public static void MoveNext ()
		{
			if (MdiTop != null && !Current.Modal) {
				lock (toplevels) {
					toplevels.MoveNext ();
					var isMdi = false;
					while (toplevels.Peek () == MdiTop || !toplevels.Peek ().Visible) {
						if (!isMdi && toplevels.Peek () == MdiTop) {
							isMdi = true;
						} else if (isMdi && toplevels.Peek () == MdiTop) {
							MoveCurrent (Top);
							break;
						}
						toplevels.MoveNext ();
					}
					Current = toplevels.Peek ();
				}
			}
		}

		/// <summary>
		/// Move to the previous Mdi child from the <see cref="MdiTop"/>.
		/// </summary>
		public static void MovePrevious ()
		{
			if (MdiTop != null && !Current.Modal) {
				lock (toplevels) {
					toplevels.MovePrevious ();
					var isMdi = false;
					while (toplevels.Peek () == MdiTop || !toplevels.Peek ().Visible) {
						if (!isMdi && toplevels.Peek () == MdiTop) {
							isMdi = true;
						} else if (isMdi && toplevels.Peek () == MdiTop) {
							MoveCurrent (Top);
							break;
						}
						toplevels.MovePrevious ();
					}
					Current = toplevels.Peek ();
				}
			}
		}

		internal static bool ShowChild (Toplevel top)
		{
			if (top.Visible && MdiTop != null && Current?.Modal == false) {
				lock (toplevels) {
					toplevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
					Current = top;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Wakes up the mainloop that might be waiting on input, must be thread safe.
		/// </summary>
		public static void DoEvents ()
		{
			MainLoop.Driver.Wakeup ();
		}

		/// <summary>
		/// Ensures that the superview of the most focused view is on front.
		/// </summary>
		public static void EnsuresTopOnFront ()
		{
			if (MdiTop != null) {
				return;
			}
			var top = FindTopFromView (Top?.MostFocused);
			if (top != null && Top.Subviews.Count > 1 && Top.Subviews [Top.Subviews.Count - 1] != top) {
				Top.BringSubviewToFront (top);
			}
		}

		internal static List<CultureInfo> GetSupportedCultures ()
		{
			CultureInfo [] culture = CultureInfo.GetCultures (CultureTypes.AllCultures);

			// Get the assembly
			Assembly assembly = Assembly.GetExecutingAssembly ();

			//Find the location of the assembly
			string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

			// Find the resource file name of the assembly
			string resourceFilename = $"{Path.GetFileNameWithoutExtension (assembly.Location)}.resources.dll";

			// Return all culture for which satellite folder found with culture code.
			return culture.Where (cultureInfo =>
			     assemblyLocation != null &&
			     Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name)) &&
			     File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename))
			).ToList ();
		}
	}
}
