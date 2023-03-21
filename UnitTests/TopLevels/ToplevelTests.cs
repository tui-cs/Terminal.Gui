using System;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.TopLevelTests {
	public class ToplevelTests {
		readonly ITestOutputHelper output;

		public ToplevelTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		[AutoInitShutdown]
		public void Constructor_Default ()
		{
			var top = new Toplevel ();

			Assert.Equal (Colors.TopLevel, top.ColorScheme);
			Assert.Equal ("Fill(0)", top.Width.ToString ());
			Assert.Equal ("Fill(0)", top.Height.ToString ());
			Assert.False (top.Running);
			Assert.False (top.Modal);
			Assert.Null (top.MenuBar);
			Assert.Null (top.StatusBar);
			Assert.False (top.IsMdiContainer);
			Assert.False (top.IsMdiChild);
		}

		[Fact]
		[AutoInitShutdown]
		public void Create_Toplevel ()
		{
			var top = Toplevel.Create ();
			Assert.Equal (new Rect (0, 0, Application.Driver.Cols, Application.Driver.Rows), top.Bounds);
		}


		[Fact]
		[AutoInitShutdown]
		public void Application_Top_EnsureVisibleBounds_To_Driver_Rows_And_Cols ()
		{
			var iterations = 0;

			Application.Iteration += () => {
				if (iterations == 0) {
					Assert.False (Application.Top.AutoSize);
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.R, new KeyModifiers ()));
				} else if (iterations == 1) {
					Assert.Equal ("Top2", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.C, new KeyModifiers ()));
				} else if (iterations == 3) {
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.R, new KeyModifiers ()));
				} else if (iterations == 4) {
					Assert.Equal ("Top2", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.C, new KeyModifiers ()));
				} else if (iterations == 6) {
					Assert.Equal ("Top1", Application.Top.Text);
					Assert.Equal (0, Application.Top.Frame.X);
					Assert.Equal (0, Application.Top.Frame.Y);
					Assert.Equal (Application.Driver.Cols, Application.Top.Frame.Width);
					Assert.Equal (Application.Driver.Rows, Application.Top.Frame.Height);

					Application.Top.ProcessHotKey (new KeyEvent (Key.CtrlMask | Key.Q, new KeyModifiers ()));
				}
				iterations++;
			};

			Application.Run (Top1 ());

			Toplevel Top1 ()
			{
				var top = Application.Top;
				top.Text = "Top1";
				var menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Options", new MenuItem [] {
						new MenuItem ("_Run Top2", "", () => Application.Run (Top2 ()), null, null, Key.CtrlMask | Key.R),
						new MenuItem ("_Quit", "", () => Application.RequestStop(), null, null, Key.CtrlMask | Key.Q)
					})
				});
				top.Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Key.CtrlMask | Key.R, "~^R~ Run Top2", () => Application.Run (Top2 ())),
					new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Application.RequestStop())
				});
				top.Add (statusBar);

				var t1 = new Toplevel ();
				top.Add (t1);

				return top;
			}

			Toplevel Top2 ()
			{
				var top = new Toplevel (Application.Top.Frame);
				top.Text = "Top2";
				var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
				var menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Stage", new MenuItem [] {
						new MenuItem ("_Close", "", () => Application.RequestStop(), null, null, Key.CtrlMask | Key.C)
					})
				});
				top.Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Key.CtrlMask | Key.C, "~^C~ Close", () => Application.RequestStop()),
				});
				top.Add (statusBar);

				win.Add (new ListView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill ()
				});
				top.Add (win);

				return top;
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void Internal_Tests ()
		{
			var top = new Toplevel ();

			var eventInvoked = "";

			top.ChildUnloaded += (s, e) => eventInvoked = "ChildUnloaded";
			top.OnChildUnloaded (top);
			Assert.Equal ("ChildUnloaded", eventInvoked);
			top.ChildLoaded += (s, e) => eventInvoked = "ChildLoaded";
			top.OnChildLoaded (top);
			Assert.Equal ("ChildLoaded", eventInvoked);
			top.Closed += (s, e) => eventInvoked = "Closed";
			top.OnClosed (top);
			Assert.Equal ("Closed", eventInvoked);
			top.Closing += (s, e) => eventInvoked = "Closing";
			top.OnClosing (new ToplevelClosingEventArgs (top));
			Assert.Equal ("Closing", eventInvoked);
			top.AllChildClosed += (s, e) => eventInvoked = "AllChildClosed";
			top.OnAllChildClosed ();
			Assert.Equal ("AllChildClosed", eventInvoked);
			top.ChildClosed += (s, e) => eventInvoked = "ChildClosed";
			top.OnChildClosed (top);
			Assert.Equal ("ChildClosed", eventInvoked);
			top.Deactivate += (s, e) => eventInvoked = "Deactivate";
			top.OnDeactivate (top);
			Assert.Equal ("Deactivate", eventInvoked);
			top.Activate += (s, e) => eventInvoked = "Activate";
			top.OnActivate (top);
			Assert.Equal ("Activate", eventInvoked);
			top.Loaded += (s, e) => eventInvoked = "Loaded";
			top.OnLoaded ();
			Assert.Equal ("Loaded", eventInvoked);
			top.Ready += (s, e) => eventInvoked = "Ready";
			top.OnReady ();
			Assert.Equal ("Ready", eventInvoked);
			top.Unloaded += (s, e) => eventInvoked = "Unloaded";
			top.OnUnloaded ();
			Assert.Equal ("Unloaded", eventInvoked);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);
			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);
			top.RemoveMenuStatusBar (top.MenuBar);
			Assert.Null (top.MenuBar);
			top.RemoveMenuStatusBar (top.StatusBar);
			Assert.Null (top.StatusBar);

			Application.Begin (top);
			Assert.Equal (top, Application.Top);

			// Application.Top without menu and status bar.
			var supView = top.EnsureVisibleBounds (top, 2, 2, out int nx, out int ny, out MenuBar mb, out StatusBar sb);
			Assert.Equal (Application.Top, supView);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);

			// Application.Top with a menu and without status bar.
			top.EnsureVisibleBounds (top, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (1, ny);
			Assert.NotNull (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);

			// Application.Top with a menu and status bar.
			top.EnsureVisibleBounds (top, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			// The available height is lower than the Application.Top height minus
			// the menu bar and status bar, then the top can go beyond the bottom
			Assert.Equal (2, ny);
			Assert.NotNull (mb);
			Assert.NotNull (sb);

			top.RemoveMenuStatusBar (top.MenuBar);
			Assert.Null (top.MenuBar);

			// Application.Top without a menu and with a status bar.
			top.EnsureVisibleBounds (top, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			// The available height is lower than the Application.Top height minus
			// the status bar, then the top can go beyond the bottom
			Assert.Equal (2, ny);
			Assert.Null (mb);
			Assert.NotNull (sb);

			top.RemoveMenuStatusBar (top.StatusBar);
			Assert.Null (top.StatusBar);
			Assert.Null (top.MenuBar);

			var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
			top.Add (win);
			top.LayoutSubviews ();

			// The SuperView is always the same regardless of the caller.
			supView = top.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (Application.Top, supView);
			supView = win.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (Application.Top, supView);

			// Application.Top without menu and status bar.
			top.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);

			// Application.Top with a menu and without status bar.
			top.EnsureVisibleBounds (win, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (1, ny);
			Assert.NotNull (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);

			// Application.Top with a menu and status bar.
			top.EnsureVisibleBounds (win, 30, 20, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			// The available height is lower than the Application.Top height minus
			// the menu bar and status bar, then the top can go beyond the bottom
			Assert.Equal (20, ny);
			Assert.NotNull (mb);
			Assert.NotNull (sb);

			top.RemoveMenuStatusBar (top.MenuBar);
			top.RemoveMenuStatusBar (top.StatusBar);
			Assert.Null (top.StatusBar);
			Assert.Null (top.MenuBar);

			top.Remove (win);

			win = new Window () { Width = 60, Height = 15 };
			top.Add (win);

			// Application.Top without menu and status bar.
			top.EnsureVisibleBounds (win, 0, 0, out nx, out ny, out mb, out sb);
			Assert.Equal (0, nx);
			Assert.Equal (0, ny);
			Assert.Null (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new MenuBar ());
			Assert.NotNull (top.MenuBar);

			// Application.Top with a menu and without status bar.
			top.EnsureVisibleBounds (win, 2, 2, out nx, out ny, out mb, out sb);
			Assert.Equal (2, nx);
			Assert.Equal (2, ny);
			Assert.NotNull (mb);
			Assert.Null (sb);

			top.AddMenuStatusBar (new StatusBar ());
			Assert.NotNull (top.StatusBar);

			// Application.Top with a menu and status bar.
			top.EnsureVisibleBounds (win, 30, 20, out nx, out ny, out mb, out sb);
			Assert.Equal (20, nx); // 20+60=80
			Assert.Equal (9, ny); // 9+15+1(mb)=25
			Assert.NotNull (mb);
			Assert.NotNull (sb);

			top.PositionToplevels ();
			Assert.Equal (new Rect (0, 1, 60, 15), win.Frame);

			Assert.Null (Toplevel.dragPosition);
			win.MouseEvent (new MouseEvent () { X = 6, Y = 0, Flags = MouseFlags.Button1Pressed });
			Assert.Equal (new Point (6, 0), Toplevel.dragPosition);
			win.MouseEvent (new MouseEvent () { X = 6, Y = 0, Flags = MouseFlags.Button1Released });
			Assert.Null (Toplevel.dragPosition);
			win.CanFocus = false;
			win.MouseEvent (new MouseEvent () { X = 6, Y = 0, Flags = MouseFlags.Button1Pressed });
			Assert.Null (Toplevel.dragPosition);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var isRunning = false;

			var win1 = new Window ("Win1") { Width = Dim.Percent (50f), Height = Dim.Fill () };
			var lblTf1W1 = new Label ("Enter text in TextField on Win1:");
			var tf1W1 = new TextField ("Text1 on Win1") { X = Pos.Right (lblTf1W1) + 1, Width = Dim.Fill () };
			var lblTvW1 = new Label ("Enter text in TextView on Win1:") { Y = Pos.Bottom (lblTf1W1) + 1 };
			var tvW1 = new TextView () { X = Pos.Left (tf1W1), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win1" };
			var lblTf2W1 = new Label ("Enter text in TextField on Win1:") { Y = Pos.Bottom (lblTvW1) + 1 };
			var tf2W1 = new TextField ("Text2 on Win1") { X = Pos.Left (tf1W1), Width = Dim.Fill () };
			win1.Add (lblTf1W1, tf1W1, lblTvW1, tvW1, lblTf2W1, tf2W1);

			var win2 = new Window ("Win2") { X = Pos.Right (win1) + 1, Width = Dim.Percent (50f), Height = Dim.Fill () };
			var lblTf1W2 = new Label ("Enter text in TextField on Win2:");
			var tf1W2 = new TextField ("Text1 on Win2") { X = Pos.Right (lblTf1W2) + 1, Width = Dim.Fill () };
			var lblTvW2 = new Label ("Enter text in TextView on Win2:") { Y = Pos.Bottom (lblTf1W2) + 1 };
			var tvW2 = new TextView () { X = Pos.Left (tf1W2), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win2" };
			var lblTf2W2 = new Label ("Enter text in TextField on Win2:") { Y = Pos.Bottom (lblTvW2) + 1 };
			var tf2W2 = new TextField ("Text2 on Win2") { X = Pos.Left (tf1W2), Width = Dim.Fill () };
			win2.Add (lblTf1W2, tf1W2, lblTvW2, tvW2, lblTf2W2, tf2W2);

			var top = Application.Top;
			top.Add (win1, win2);
			top.Loaded += (s, e) => isRunning = true;
			top.Closing += (s, e) => isRunning = false;
			Application.Begin (top);
			top.Running = true;

			Assert.Equal (new Rect (0, 0, 40, 25), win1.Frame);
			Assert.Equal (new Rect (41, 0, 40, 25), win2.Frame);
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf1W1, top.MostFocused);

			Assert.True (isRunning);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Application.QuitKey, new KeyModifiers ())));
			Assert.False (isRunning);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.False (top.Focused.ProcessKey (new KeyEvent (Key.F5, new KeyModifiers ())));

			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tvW1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ($"\tFirst line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"First line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf1W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf1W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tvW1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.I | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tvW1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf1W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win2, top.Focused);
			Assert.Equal (tf1W2, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Application.AlternateForwardKey, new KeyModifiers ())));
			Assert.Equal (win2, top.Focused);
			Assert.Equal (tf1W2, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Application.AlternateBackwardKey, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tvW1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.B | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf1W1, top.MostFocused);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tvW1, top.MostFocused);
			Assert.Equal (new Point (0, 0), tvW1.CursorPosition);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tvW1, top.MostFocused);
			Assert.Equal (new Point (16, 1), tvW1.CursorPosition);
			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.F | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, top.Focused);
			Assert.Equal (tf2W1, top.MostFocused);

			Assert.True (top.Focused.ProcessKey (new KeyEvent (Key.L | Key.CtrlMask, new KeyModifiers ())));
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command_With_MdiTop ()
		{
			var top = Application.Top;
			Assert.Null (Application.MdiTop);
			top.IsMdiContainer = true;
			Application.Begin (top);
			Assert.Equal (Application.Top, Application.MdiTop);

			var isRunning = true;

			var win1 = new Window ("Win1") { Width = Dim.Percent (50f), Height = Dim.Fill () };
			var lblTf1W1 = new Label ("Enter text in TextField on Win1:");
			var tf1W1 = new TextField ("Text1 on Win1") { X = Pos.Right (lblTf1W1) + 1, Width = Dim.Fill () };
			var lblTvW1 = new Label ("Enter text in TextView on Win1:") { Y = Pos.Bottom (lblTf1W1) + 1 };
			var tvW1 = new TextView () { X = Pos.Left (tf1W1), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win1" };
			var lblTf2W1 = new Label ("Enter text in TextField on Win1:") { Y = Pos.Bottom (lblTvW1) + 1 };
			var tf2W1 = new TextField ("Text2 on Win1") { X = Pos.Left (tf1W1), Width = Dim.Fill () };
			win1.Add (lblTf1W1, tf1W1, lblTvW1, tvW1, lblTf2W1, tf2W1);

			var win2 = new Window ("Win2") { Width = Dim.Percent (50f), Height = Dim.Fill () };
			var lblTf1W2 = new Label ("Enter text in TextField on Win2:");
			var tf1W2 = new TextField ("Text1 on Win2") { X = Pos.Right (lblTf1W2) + 1, Width = Dim.Fill () };
			var lblTvW2 = new Label ("Enter text in TextView on Win2:") { Y = Pos.Bottom (lblTf1W2) + 1 };
			var tvW2 = new TextView () { X = Pos.Left (tf1W2), Width = Dim.Fill (), Height = 2, Text = "First line Win1\nSecond line Win2" };
			var lblTf2W2 = new Label ("Enter text in TextField on Win2:") { Y = Pos.Bottom (lblTvW2) + 1 };
			var tf2W2 = new TextField ("Text2 on Win2") { X = Pos.Left (tf1W2), Width = Dim.Fill () };
			win2.Add (lblTf1W2, tf1W2, lblTvW2, tvW2, lblTf2W2, tf2W2);

			win1.Closing += (s, e) => isRunning = false;
			Assert.Null (top.Focused);
			Assert.Equal (top, Application.Current);
			Assert.True (top.IsCurrentTop);
			Assert.Equal (top, Application.MdiTop);
			Application.Begin (win1);
			Assert.Equal (new Rect (0, 0, 40, 25), win1.Frame);
			Assert.NotEqual (top, Application.Current);
			Assert.False (top.IsCurrentTop);
			Assert.Equal (win1, Application.Current);
			Assert.True (win1.IsCurrentTop);
			Assert.True (win1.IsMdiChild);
			Assert.Null (top.Focused);
			Assert.Null (top.MostFocused);
			Assert.Equal (win1.Subviews [0], win1.Focused);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (win1.IsMdiChild);
			Assert.Single (Application.MdiChildes);
			Application.Begin (win2);
			Assert.Equal (new Rect (0, 0, 40, 25), win2.Frame);
			Assert.NotEqual (top, Application.Current);
			Assert.False (top.IsCurrentTop);
			Assert.Equal (win2, Application.Current);
			Assert.True (win2.IsCurrentTop);
			Assert.True (win2.IsMdiChild);
			Assert.Null (top.Focused);
			Assert.Null (top.MostFocused);
			Assert.Equal (win2.Subviews [0], win2.Focused);
			Assert.Equal (tf1W2, win2.MostFocused);
			Assert.Equal (2, Application.MdiChildes.Count);

			Application.ShowChild (win1);
			Assert.Equal (win1, Application.Current);
			Assert.Equal (win1, Application.MdiChildes [0]);
			win1.Running = true;
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Application.QuitKey, new KeyModifiers ())));
			Assert.False (isRunning);
			Assert.False (win1.Running);
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Z | Key.CtrlMask, new KeyModifiers ())));
			Assert.False (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.F5, new KeyModifiers ())));

			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.True (win1.IsCurrentTop);
			Assert.Equal (tvW1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal ($"\tFirst line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal ($"First line Win1{Environment.NewLine}Second line Win1", tvW1.Text);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf2W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tvW1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.I | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf2W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.BackTab | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tvW1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf2W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win2, Application.MdiChildes [0]);
			Assert.Equal (tf1W2, win2.MostFocused);
			tf2W2.SetFocus ();
			Assert.True (tf2W2.HasFocus);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.Tab | Key.CtrlMask | Key.ShiftMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Application.AlternateForwardKey, new KeyModifiers ())));
			Assert.Equal (win2, Application.MdiChildes [0]);
			Assert.Equal (tf2W2, win2.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Application.AlternateBackwardKey, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tvW1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.B | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf1W1, win1.MostFocused);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tvW1, win1.MostFocused);
			Assert.Equal (new Point (0, 0), tvW1.CursorPosition);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tvW1, win1.MostFocused);
			Assert.Equal (new Point (16, 1), tvW1.CursorPosition);
			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.F | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (win1, Application.MdiChildes [0]);
			Assert.Equal (tf2W1, win1.MostFocused);

			Assert.True (Application.MdiChildes [0].ProcessKey (new KeyEvent (Key.L | Key.CtrlMask, new KeyModifiers ())));
		}

		[Fact]
		public void Added_Event_Should_Not_Be_Used_To_Initialize_Toplevel_Events ()
		{
			Key alternateForwardKey = default;
			Key alternateBackwardKey = default;
			Key quitKey = default;
			var wasAdded = false;

			var view = new View ();
			view.Added += View_Added;

			void View_Added (object sender, SuperViewChangedEventArgs e)
			{
				Assert.Throws<NullReferenceException> (() => Application.Top.AlternateForwardKeyChanged += (s, e) => alternateForwardKey = e.OldKey);
				Assert.Throws<NullReferenceException> (() => Application.Top.AlternateBackwardKeyChanged += (s, e) => alternateBackwardKey = e.OldKey);
				Assert.Throws<NullReferenceException> (() => Application.Top.QuitKeyChanged += (s, e) => quitKey = e.OldKey);
				Assert.False (wasAdded);
				wasAdded = true;
				view.Added -= View_Added;
			}

			var win = new Window ();
			win.Add (view);
			Application.Init (new FakeDriver ());
			var top = Application.Top;
			top.Add (win);

			Assert.True (wasAdded);

			Application.Shutdown ();
		}

		[Fact]
		[AutoInitShutdown]
		public void AlternateForwardKeyChanged_AlternateBackwardKeyChanged_QuitKeyChanged_Events ()
		{
			Key alternateForwardKey = default;
			Key alternateBackwardKey = default;
			Key quitKey = default;

			var view = new View ();
			view.Initialized += View_Initialized;

			void View_Initialized (object sender, EventArgs e)
			{
				Application.Top.AlternateForwardKeyChanged += (s, e) => alternateForwardKey = e.OldKey;
				Application.Top.AlternateBackwardKeyChanged += (s, e) => alternateBackwardKey = e.OldKey;
				Application.Top.QuitKeyChanged += (s, e) => quitKey = e.OldKey;
			}

			var win = new Window ();
			win.Add (view);
			var top = Application.Top;
			top.Add (win);
			Application.Begin (top);

			Assert.Equal (Key.Null, alternateForwardKey);
			Assert.Equal (Key.Null, alternateBackwardKey);
			Assert.Equal (Key.Null, quitKey);

			Assert.Equal (Key.PageDown | Key.CtrlMask, Application.AlternateForwardKey);
			Assert.Equal (Key.PageUp | Key.CtrlMask, Application.AlternateBackwardKey);
			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);

			Application.AlternateForwardKey = Key.A;
			Application.AlternateBackwardKey = Key.B;
			Application.QuitKey = Key.C;

			Assert.Equal (Key.PageDown | Key.CtrlMask, alternateForwardKey);
			Assert.Equal (Key.PageUp | Key.CtrlMask, alternateBackwardKey);
			Assert.Equal (Key.Q | Key.CtrlMask, quitKey);

			Assert.Equal (Key.A, Application.AlternateForwardKey);
			Assert.Equal (Key.B, Application.AlternateBackwardKey);
			Assert.Equal (Key.C, Application.QuitKey);

			// Replacing the defaults keys to avoid errors on others unit tests that are using it.
			Application.AlternateForwardKey = Key.PageDown | Key.CtrlMask;
			Application.AlternateBackwardKey = Key.PageUp | Key.CtrlMask;
			Application.QuitKey = Key.Q | Key.CtrlMask;

			Assert.Equal (Key.PageDown | Key.CtrlMask, Application.AlternateForwardKey);
			Assert.Equal (Key.PageUp | Key.CtrlMask, Application.AlternateBackwardKey);
			Assert.Equal (Key.Q | Key.CtrlMask, Application.QuitKey);
		}

		[Fact]
		[AutoInitShutdown]
		public void FileDialog_FileSystemWatcher ()
		{
			for (int i = 0; i < 8; i++) {
				var fd = new FileDialog ();
				fd.Ready += (s, e) => Application.RequestStop ();
				Application.Run (fd);
			}
		}

		[Fact, AutoInitShutdown]
		public void Mouse_Drag_On_Top_With_Superview_Null ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem("File", new MenuItem [] {
					new MenuItem("New", "", null)
				})
			});

			var sbar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.N, "~CTRL-N~ New", null)
			});

			var win = new Window ("Window");
			var top = Application.Top;
			top.Add (menu, sbar, win);

			var iterations = -1;

			Application.Iteration = () => {
				iterations++;
				if (iterations == 0) {
					((FakeDriver)Application.Driver).SetBufferSize (40, 15);
					MessageBox.Query ("About", "Hello Word", "Ok");

				} else if (iterations == 1) TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                                   
┌ Window ──────────────────────────────┐
│                                      │
│                                      │
│                                      │
│       ┌ About ───────────────┐       │
│       │      Hello Word      │       │
│       │                      │       │
│       │       [◦ Ok ◦]       │       │
│       └──────────────────────┘       │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘
 CTRL-N New                             ", output);
				else if (iterations == 2) {
					Assert.Null (Application.MouseGrabView);
					// Grab the mouse
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 8,
							Y = 5,
							Flags = MouseFlags.Button1Pressed
						});

					Assert.Equal (Application.Current, Application.MouseGrabView);
					Assert.Equal (new Rect (8, 5, 24, 5), Application.MouseGrabView.Frame);

				} else if (iterations == 3) {
					Assert.Equal (Application.Current, Application.MouseGrabView);
					// Grab to left
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 7,
							Y = 5,
							Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
						});

					Assert.Equal (Application.Current, Application.MouseGrabView);
					Assert.Equal (new Rect (7, 5, 24, 5), Application.MouseGrabView.Frame);

				} else if (iterations == 4) {
					Assert.Equal (Application.Current, Application.MouseGrabView);

					TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                                   
┌ Window ──────────────────────────────┐
│                                      │
│                                      │
│                                      │
│      ┌ About ───────────────┐        │
│      │      Hello Word      │        │
│      │                      │        │
│      │       [◦ Ok ◦]       │        │
│      └──────────────────────┘        │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘
 CTRL-N New                             ", output);

					Assert.Equal (Application.Current, Application.MouseGrabView);
				} else if (iterations == 5) {
					Assert.Equal (Application.Current, Application.MouseGrabView);
					// Grab to top
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 7,
							Y = 4,
							Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
						});

					Assert.Equal (Application.Current, Application.MouseGrabView);
					Assert.Equal (new Rect (7, 4, 24, 5), Application.MouseGrabView.Frame);

				} else if (iterations == 6) {
					Assert.Equal (Application.Current, Application.MouseGrabView);

					TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                                   
┌ Window ──────────────────────────────┐
│                                      │
│                                      │
│      ┌ About ───────────────┐        │
│      │      Hello Word      │        │
│      │                      │        │
│      │       [◦ Ok ◦]       │        │
│      └──────────────────────┘        │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘
 CTRL-N New                             ", output);

					Assert.Equal (Application.Current, Application.MouseGrabView);
					Assert.Equal (new Rect (7, 4, 24, 5), Application.MouseGrabView.Frame);

				} else if (iterations == 7) {
					Assert.Equal (Application.Current, Application.MouseGrabView);
					// Ungrab the mouse
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 7,
							Y = 4,
							Flags = MouseFlags.Button1Released
						});

					Assert.Null (Application.MouseGrabView);

				} else if (iterations == 8) Application.RequestStop ();
				else if (iterations == 9) Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Mouse_Drag_On_Top_With_Superview_Not_Null ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem("File", new MenuItem [] {
					new MenuItem("New", "", null)
				})
			});

			var sbar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.N, "~CTRL-N~ New", null)
			});

			var win = new Window ("Window") {
				X = 3,
				Y = 2,
				Width = Dim.Fill (10),
				Height = Dim.Fill (5)
			};
			var top = Application.Top;
			top.Add (menu, sbar, win);

			var iterations = -1;

			Application.Iteration = () => {
				iterations++;
				if (iterations == 0) {
					((FakeDriver)Application.Driver).SetBufferSize (20, 10);

					Assert.Null (Application.MouseGrabView);
					// Grab the mouse
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 4,
							Y = 2,
							Flags = MouseFlags.Button1Pressed
						});

					Assert.Equal (win, Application.MouseGrabView);
					Assert.Equal (new Rect (3, 2, 7, 3), Application.MouseGrabView.Frame);

					TestHelpers.AssertDriverContentsWithFrameAre (@"
 File      
           
   ┌─────┐ 
   │     │ 
   └─────┘ 
           
           
           
           
 CTRL-N New", output);


				} else if (iterations == 1) {
					Assert.Equal (win, Application.MouseGrabView);
					// Grab to left
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 5,
							Y = 2,
							Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
						});

					Assert.Equal (win, Application.MouseGrabView);

				} else if (iterations == 2) {
					Assert.Equal (win, Application.MouseGrabView);

					TestHelpers.AssertDriverContentsWithFrameAre (@"
 File      
           
    ┌────┐ 
    │    │ 
    └────┘ 
           
           
           
           
 CTRL-N New", output);

					Assert.Equal (win, Application.MouseGrabView);
					Assert.Equal (new Rect (4, 2, 6, 3), Application.MouseGrabView.Frame);

				} else if (iterations == 3) {
					Assert.Equal (win, Application.MouseGrabView);
					// Grab to top
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 5,
							Y = 1,
							Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
						});

					Assert.Equal (win, Application.MouseGrabView);

				} else if (iterations == 4) {
					Assert.Equal (win, Application.MouseGrabView);

					TestHelpers.AssertDriverContentsWithFrameAre (@"
 File      
    ┌────┐ 
    │    │ 
    │    │ 
    └────┘ 
           
           
           
           
 CTRL-N New", output);

					Assert.Equal (win, Application.MouseGrabView);
					Assert.Equal (new Rect (4, 1, 6, 4), Application.MouseGrabView.Frame);

				} else if (iterations == 5) {
					Assert.Equal (win, Application.MouseGrabView);
					// Ungrab the mouse
					ReflectionTools.InvokePrivate (
						typeof (Application),
						"ProcessMouseEvent",
						new MouseEvent () {
							X = 7,
							Y = 4,
							Flags = MouseFlags.Button1Released
						});

					Assert.Null (Application.MouseGrabView);

				} else if (iterations == 8) Application.RequestStop ();
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void EnsureVisibleBounds_With_Border_Null_Not_Throws ()
		{
			var top = new Toplevel ();
			Application.Begin (top);

			var exception = Record.Exception (() => ((FakeDriver)Application.Driver).SetBufferSize (0, 10));
			Assert.Null (exception);

			exception = Record.Exception (() => ((FakeDriver)Application.Driver).SetBufferSize (10, 0));
			Assert.Null (exception);
		}

		[Fact, AutoInitShutdown]
		public void OnEnter_OnLeave_Triggered_On_Application_Begin_End ()
		{
			var isEnter = false;
			var isLeave = false;
			var v = new View ();
			v.Enter += (s, _) => isEnter = true;
			v.Leave += (s, _) => isLeave = true;
			var top = Application.Top;
			top.Add (v);

			Assert.False (v.CanFocus);
			var exception = Record.Exception (() => top.OnEnter (top));
			Assert.Null (exception);
			exception = Record.Exception (() => top.OnLeave (top));
			Assert.Null (exception);

			v.CanFocus = true;
			Application.Begin (top);

			Assert.True (isEnter);
			Assert.False (isLeave);

			isEnter = false;
			var d = new Dialog ();
			var rs = Application.Begin (d);

			Assert.False (isEnter);
			Assert.True (isLeave);

			isLeave = false;
			Application.End (rs);

			Assert.True (isEnter);
			Assert.False (isLeave);
		}

		[Fact, AutoInitShutdown]
		public void PositionCursor_SetCursorVisibility_To_Invisible_If_Focused_Is_Null ()
		{
			var tf = new TextField ("test") { Width = 5 };
			var view = new View () { Width = 10, Height = 10 };
			view.Add (tf);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			Assert.True (tf.HasFocus);
			Application.Driver.GetCursorVisibility (out CursorVisibility cursor);
			Assert.Equal (CursorVisibility.Default, cursor);

			view.Enabled = false;
			Assert.False (tf.HasFocus);
			Application.Refresh ();
			Application.Driver.GetCursorVisibility (out cursor);
			Assert.Equal (CursorVisibility.Invisible, cursor);
		}

		[Fact, AutoInitShutdown]
		public void IsLoaded_Application_Begin ()
		{
			var top = Application.Top;
			Assert.False (top.IsLoaded);

			Application.Begin (top);
			Assert.True (top.IsLoaded);
		}

		[Fact, AutoInitShutdown]
		public void IsLoaded_With_Sub_Toplevel_Application_Begin_NeedDisplay ()
		{
			var top = Application.Top;
			var subTop = new Toplevel ();
			var view = new View (new Rect (0, 0, 20, 10));
			subTop.Add (view);
			top.Add (subTop);

			Assert.False (top.IsLoaded);
			Assert.False (subTop.IsLoaded);
			Assert.Equal (new Rect (0, 0, 20, 10), view.Frame);
			Assert.Equal (new Rect (0, 0, 20, 10), view.NeedDisplay);

			view.LayoutStarted += view_LayoutStarted;

			void view_LayoutStarted (object sender, LayoutEventArgs e)
			{
				Assert.Equal (new Rect (0, 0, 20, 10), view.NeedDisplay);
				view.LayoutStarted -= view_LayoutStarted;
			}

			Application.Begin (top);

			Assert.True (top.IsLoaded);
			Assert.True (subTop.IsLoaded);
			Assert.Equal (new Rect (0, 0, 20, 10), view.Frame);

			view.Frame = new Rect (1, 3, 10, 5);
			Assert.Equal (new Rect (1, 3, 10, 5), view.Frame);
			Assert.Equal (new Rect (0, 0, 10, 5), view.NeedDisplay);

			view.Redraw (view.Bounds);
			view.Frame = new Rect (1, 3, 10, 5);
			Assert.Equal (new Rect (1, 3, 10, 5), view.Frame);
			Assert.Equal (new Rect (0, 0, 10, 5), view.NeedDisplay);
		}

		[Fact, AutoInitShutdown]
		public void Toplevel_Inside_ScrollView_MouseGrabView ()
		{
			var scrollView = new ScrollView () {
				X = 3,
				Y = 3,
				Width = 40,
				Height = 16,
				ContentSize = new Size (200, 100)
			};
			var win = new Window ("Window") { X = 3, Y = 3, Width = Dim.Fill (3), Height = Dim.Fill (3) };
			scrollView.Add (win);
			var top = Application.Top;
			top.Add (scrollView);
			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
			Assert.Equal (new Rect (3, 3, 40, 16), scrollView.Frame);
			Assert.Equal (new Rect (0, 0, 200, 100), scrollView.Subviews [0].Frame);
			Assert.Equal (new Rect (3, 3, 194, 94), win.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                                          ▲
                                          ┬
                                          │
      ┌ Window ───────────────────────────┴
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ░
      │                                   ▼
   ◄├──────┤░░░░░░░░░░░░░░░░░░░░░░░░░░░░░► ", output);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 6,
					Y = 6,
					Flags = MouseFlags.Button1Pressed
				});
			Assert.Equal (win, Application.MouseGrabView);
			Assert.Equal (new Rect (3, 3, 194, 94), win.Frame);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 9,
					Y = 9,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});
			Assert.Equal (win, Application.MouseGrabView);
			top.SetNeedsLayout ();
			top.LayoutSubviews ();
			Assert.Equal (new Rect (6, 6, 191, 91), win.Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                                          ▲
                                          ┬
                                          │
                                          ┴
                                          ░
                                          ░
         ┌ Window ────────────────────────░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ░
         │                                ▼
   ◄├──────┤░░░░░░░░░░░░░░░░░░░░░░░░░░░░░► ", output);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 5,
					Y = 5,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});
			Assert.Equal (win, Application.MouseGrabView);
			top.SetNeedsLayout ();
			top.LayoutSubviews ();
			Assert.Equal (new Rect (2, 2, 195, 95), win.Frame);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
                                          ▲
                                          ┬
     ┌ Window ────────────────────────────│
     │                                    ┴
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ░
     │                                    ▼
   ◄├──────┤░░░░░░░░░░░░░░░░░░░░░░░░░░░░░► ", output);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 5,
					Y = 5,
					Flags = MouseFlags.Button1Released
				});
			Assert.Null (Application.MouseGrabView);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 4,
					Y = 4,
					Flags = MouseFlags.ReportMousePosition
				});
			Assert.Equal (scrollView, Application.MouseGrabView);
		}

		[Fact, AutoInitShutdown]
		public void Dialog_Bounds_Bigger_Than_Driver_Cols_And_Rows_Allow_Drag_Beyond_Left_Right_And_Bottom ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem("File", new MenuItem [] {
					new MenuItem("New", "", null)
				})
			});

			var sb = new StatusBar (new StatusItem [] {
				new StatusItem(Key.N, "~CTRL-N~ New", null)
			});
			var top = Application.Top;
			top.Add (menu, sb);
			var dialog = new Dialog ("Dialog", 20, 3, new Button ("Ok"));
			Application.Begin (top);
			((FakeDriver)Application.Driver).SetBufferSize (40, 10);
			Application.Begin (dialog);
			Application.Refresh ();
			Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
			Assert.Equal (new Rect (10, 3, 20, 3), dialog.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                         
                              
                              
          ┌ Dialog ──────────┐
          │      [ Ok ]      │
          └──────────────────┘
                              
                              
                              
 CTRL-N New                   ", output);

			Assert.Null (Application.MouseGrabView);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 10,
					Y = 3,
					Flags = MouseFlags.Button1Pressed
				});

			Assert.Equal (dialog, Application.MouseGrabView);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = -11,
					Y = -4,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});

			Application.Refresh ();
			Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
			Assert.Equal (new Rect (0, 1, 20, 3), dialog.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 File               
┌ Dialog ──────────┐
│      [ Ok ]      │
└──────────────────┘
                    
                    
                    
                    
                    
 CTRL-N New         ", output);

			// Changes Top size to same size as Dialog more menu and scroll bar
			((FakeDriver)Application.Driver).SetBufferSize (20, 5);
			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = -1,
					Y = -1,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});

			Application.Refresh ();
			Assert.Equal (new Rect (0, 0, 20, 5), top.Frame);
			Assert.Equal (new Rect (0, 1, 20, 3), dialog.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 File               
┌ Dialog ──────────┐
│      [ Ok ]      │
└──────────────────┘
 CTRL-N New         ", output);

			// Changes Top size smaller than Dialog size
			((FakeDriver)Application.Driver).SetBufferSize (19, 3);
			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = -1,
					Y = -1,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});

			Application.Refresh ();
			Assert.Equal (new Rect (0, 0, 19, 3), top.Frame);
			Assert.Equal (new Rect (-1, 1, 20, 3), dialog.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 File              
 Dialog ──────────┐
      [ Ok ]      │", output);

			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 18,
					Y = 3,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});

			Application.Refresh ();
			Assert.Equal (new Rect (0, 0, 19, 3), top.Frame);
			Assert.Equal (new Rect (18, 2, 20, 3), dialog.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 File              
                   
 CTRL-N New       ┌", output);

			// On a real app we can't go beyond the SuperView bounds
			ReflectionTools.InvokePrivate (
				typeof (Application),
				"ProcessMouseEvent",
				new MouseEvent () {
					X = 19,
					Y = 4,
					Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
				});

			Application.Refresh ();
			Assert.Equal (new Rect (0, 0, 19, 3), top.Frame);
			Assert.Equal (new Rect (19, 2, 20, 3), dialog.Frame);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 File      
           
 CTRL-N New", output);
		}
	}
}