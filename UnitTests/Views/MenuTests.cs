using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;
//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests {
	public class MenuTests {
		readonly ITestOutputHelper output;

		public MenuTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constuctors_Defaults ()
		{
			var menu = new Menu (new MenuBar (), 0, 0, new MenuBarItem ());
			Assert.Equal (Colors.Menu, menu.ColorScheme);
			Assert.True (menu.CanFocus);
			Assert.False (menu.WantContinuousButtonPressed);

			var menuBar = new MenuBar ();
			Assert.Equal (0, menuBar.X);
			Assert.Equal (0, menuBar.Y);
			Assert.IsType<Dim.DimFill> (menuBar.Width);
			Assert.Equal (1, menuBar.Height);
			Assert.Empty (menuBar.Menus);
			Assert.Equal (Colors.Menu, menuBar.ColorScheme);
			Assert.True (menuBar.WantMousePositionReports);
			Assert.False (menuBar.IsMenuOpen);

			menuBar = new MenuBar (new MenuBarItem [] { });
			Assert.Equal (0, menuBar.X);
			Assert.Equal (0, menuBar.Y);
			Assert.IsType<Dim.DimFill> (menuBar.Width);
			Assert.Equal (1, menuBar.Height);
			Assert.Empty (menuBar.Menus);
			Assert.Equal (Colors.Menu, menuBar.ColorScheme);
			Assert.True (menuBar.WantMousePositionReports);
			Assert.False (menuBar.IsMenuOpen);

			var menuBarItem = new MenuBarItem ();
			Assert.Equal ("", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem (new MenuBarItem [] { });
			Assert.Equal ("", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem ("Test", new MenuBarItem [] { });
			Assert.Equal ("Test", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem ("Test", new List<MenuItem []> ());
			Assert.Equal ("Test", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem ("Test", "Help", null);
			Assert.Equal ("Test", menuBarItem.Title);
			Assert.Equal ("Help", menuBarItem.Help);
			Assert.Null (menuBarItem.Action);
			Assert.Null (menuBarItem.CanExecute);
			Assert.Null (menuBarItem.Parent);
			Assert.Equal (Key.Null, menuBarItem.Shortcut);

			var menuItem = new MenuItem ();
			Assert.Equal ("", menuItem.Title);
			Assert.Equal ("", menuItem.Help);
			Assert.Null (menuItem.Action);
			Assert.Null (menuItem.CanExecute);
			Assert.Null (menuItem.Parent);
			Assert.Equal (Key.Null, menuItem.Shortcut);

			menuItem = new MenuItem ("Test", "Help", Run, () => { return true; }, new MenuItem (), Key.F1);
			Assert.Equal ("Test", menuItem.Title);
			Assert.Equal ("Help", menuItem.Help);
			Assert.Equal (Run, menuItem.Action);
			Assert.NotNull (menuItem.CanExecute);
			Assert.NotNull (menuItem.Parent);
			Assert.Equal (Key.F1, menuItem.Shortcut);

			void Run () { }
		}

		[Fact]
		public void Exceptions ()
		{
			Assert.Throws<ArgumentNullException> (() => new MenuBarItem ("Test", (MenuItem [])null, null));
			Assert.Throws<ArgumentNullException> (() => new MenuBarItem ("Test", (List<MenuItem []>)null, null));
		}

		[Fact]
		[AutoInitShutdown]
		public void MenuOpening_MenuOpened_MenuClosing_Events ()
		{
			var miAction = "";
			var isMenuClosed = true;
			var cancelClosing = false;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "Creates new file.", New)
				})
			});
			menu.MenuOpening += (s, e) => {
				Assert.Equal ("_File", e.CurrentMenu.Title);
				Assert.Equal ("_New", e.CurrentMenu.Children [0].Title);
				Assert.Equal ("Creates new file.", e.CurrentMenu.Children [0].Help);
				Assert.Equal (New, e.CurrentMenu.Children [0].Action);
				e.CurrentMenu.Children [0].Action ();
				Assert.Equal ("New", miAction);
				e.NewMenuBarItem = new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "Copies the selection.", Copy)
				});
			};
			menu.MenuOpened += (s, e) => {
				var mi = e.MenuItem;

				Assert.Equal ("_Edit", mi.Parent.Title);
				Assert.Equal ("_Copy", mi.Title);
				Assert.Equal ("Copies the selection.", mi.Help);
				Assert.Equal (Copy, mi.Action);
				mi.Action ();
				Assert.Equal ("Copy", miAction);
			};
			menu.MenuClosing += (s, e) => {
				Assert.False (isMenuClosed);
				if (cancelClosing) {
					e.Cancel = true;
					isMenuClosed = false;
				} else 					isMenuClosed = true;
			};
			Application.Top.Add (menu);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			isMenuClosed = !menu.IsMenuOpen;
			Assert.False (isMenuClosed);
			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
Edit
┌──────────────────────────────┐
│ Copy   Copies the selection. │
└──────────────────────────────┘
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			cancelClosing = true;
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (isMenuClosed);
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
Edit
┌──────────────────────────────┐
│ Copy   Copies the selection. │
└──────────────────────────────┘
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			cancelClosing = false;
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.True (isMenuClosed);
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
Edit
";
			TestHelpers.AssertDriverContentsAre (expected, output);

			void New () => miAction = "New";
			void Copy () => miAction = "Copy";
		}

		[Fact, AutoInitShutdown]
		public void MenuOpened_On_Disabled_MenuItem ()
		{
			MenuItem miCurrent = null;
			Menu mCurrent = null;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuBarItem ("_New", new MenuItem [] {
						new MenuItem ("_New doc", "Creates new doc.", null, () => false)
					}),
					null,
					new MenuItem ("_Save", "Saves the file.", null, null)
				})
			});
			menu.MenuOpened += (s, e) => {
				miCurrent = e.MenuItem;
				mCurrent = menu.openMenu;
			};
			menu.UseKeysUpDownAsKeysLeftRight = true;
			Application.Top.Add (menu);

			// open the menu
			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", miCurrent.Parent.Title);
			Assert.Equal ("_New", miCurrent.Title);

			Assert.True (mCurrent.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 1,
				Flags = MouseFlags.ReportMousePosition,
				View = mCurrent
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", miCurrent.Parent.Title);
			Assert.Equal ("_New", miCurrent.Title);

			Assert.True (mCurrent.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 2,
				Flags = MouseFlags.ReportMousePosition,
				View = mCurrent
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", miCurrent.Parent.Title);
			Assert.Equal ("_New", miCurrent.Title);

			Assert.True (mCurrent.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 3,
				Flags = MouseFlags.ReportMousePosition,
				View = mCurrent
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", miCurrent.Parent.Title);
			Assert.Equal ("_Save", miCurrent.Title);

			// close the menu
			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Assert.False (menu.IsMenuOpen);

			// open the menu
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_New", miCurrent.Parent.Title);
			Assert.Equal ("_New doc", miCurrent.Title);

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", miCurrent.Parent.Title);
			Assert.Equal ("_Save", miCurrent.Title);

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", miCurrent.Parent.Title);
			Assert.Equal ("_New", miCurrent.Title);

			// close the menu
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
		}

		[Fact]
		[AutoInitShutdown]
		public void MouseEvent_Test ()
		{
			MenuItem miCurrent = null;
			Menu mCurrent = null;
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", null),
					new MenuItem ("_Open", "", null),
					new MenuItem ("_Save", "", null)
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});
			menu.MenuOpened += (s, e) => {
				miCurrent = e.MenuItem;
				mCurrent = menu.openCurrentMenu;
			};
			Application.Top.Add (menu);

			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 10,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", miCurrent.Parent.Title);
			Assert.Equal ("_Copy", miCurrent.Title);

			Assert.True (mCurrent.MouseEvent (new MouseEvent () {
				X = 10,
				Y = 3,
				Flags = MouseFlags.ReportMousePosition,
				View = mCurrent
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", miCurrent.Parent.Title);
			Assert.Equal ("_Paste", miCurrent.Title);

			for (int i = 2; i >= -1; i--) {
				if (i == -1) 					Assert.False (mCurrent.MouseEvent (new MouseEvent () {
						X = 10,
						Y = i,
						Flags = MouseFlags.ReportMousePosition,
						View = menu
					}));
else 					Assert.True (mCurrent.MouseEvent (new MouseEvent () {
						X = 10,
						Y = i,
						Flags = MouseFlags.ReportMousePosition,
						View = mCurrent
					}));
				Assert.True (menu.IsMenuOpen);
				if (i == 2) {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("C_ut", miCurrent.Title);
				} else if (i == 1) {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("_Copy", miCurrent.Title);
				} else if (i == 0) {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("_Copy", miCurrent.Title);
				} else {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("_Copy", miCurrent.Title);
				}
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var miAction = "";
			MenuItem mbiCurrent = null;
			MenuItem miCurrent = null;
			Menu mCurrent = null;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => miAction ="New"),
					new MenuItem ("_Open", "", () => miAction ="Open"),
					new MenuItem ("_Save", "", () => miAction ="Save"),
					null,
					new MenuItem ("_Quit", "", () => miAction ="Quit"),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => miAction ="Copy"),
					new MenuItem ("C_ut", "", () => miAction ="Cut"),
					new MenuItem ("_Paste", "", () => miAction ="Paste"),
					new MenuBarItem ("_Find and Replace", new MenuItem [] {
						new MenuItem ("F_ind", "", null),
						new MenuItem ("_Replace", "", null)
					}),
					new MenuItem ("_Select All", "", () => miAction ="Select All")
				}),
				new MenuBarItem ("_About", "Top-Level", () => miAction ="About")
			});
			menu.MenuOpening += (s, e) => mbiCurrent = e.CurrentMenu;
			menu.MenuOpened += (s, e) => {
				miCurrent = e.MenuItem;
				mCurrent = menu.openCurrentMenu;
			};
			menu.MenuClosing += (s, e) => {
				mbiCurrent = null;
				miCurrent = null;
				mCurrent = null;
			};
			menu.UseKeysUpDownAsKeysLeftRight = true;
			Application.Top.Add (menu);
			Application.Begin (Application.Top);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_About", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_About", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (menu.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (menu.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.False (mCurrent.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.True (Application.Top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Quit", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_About", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_About", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Application.MainLoop.MainIteration ();
			Assert.Equal ("New", miAction);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_About", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_About", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Application.MainLoop.MainIteration ();
			Assert.Equal ("About", miAction);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Copy", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("C_ut", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Paste", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("F_ind", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Replace", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("F_ind", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Select All", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("F_ind", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Paste", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("C_ut", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Copy", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Application.MainLoop.MainIteration ();
			Assert.Equal ("Copy", miAction);


			string GetCurrentMenuBarItemTitle ()
			{
				return mbiCurrent != null ? mbiCurrent.Title.ToString () : "Closed";
			}

			string GetCurrenParenttMenuItemTitle ()
			{
				return miCurrent?.Parent != null ? miCurrent.Parent.Title.ToString () : "None";
			}

			string GetCurrentMenuTitle ()
			{
				return miCurrent != null ? miCurrent.Title.ToString () : "None";
			}
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_With_Positive_Positions ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			});

			Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));

			menu.OpenMenu ();
			Application.Begin (Application.Top);

			var expected = @"
┌──────┐
│ One  │
│ Two  │
└──────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 1, 8, 4), pos);
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_With_Negative_Positions ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			}) {
				X = -1,
				Y = -1
			};

			Assert.Equal (new Point (-1, -1), new Point (menu.Frame.X, menu.Frame.Y));

			menu.OpenMenu ();
			Application.Begin (Application.Top);

			var expected = @"
──────┐
 One  │
 Two  │
──────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 7, 4), pos);

			menu.CloseAllMenus ();
			menu.Frame = new Rect (-1, -2, menu.Frame.Width, menu.Frame.Height);
			menu.OpenMenu ();
			Application.Refresh ();

			expected = @"
 One  │
 Two  │
──────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 7, 3), pos);

			menu.CloseAllMenus ();
			menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
			((FakeDriver)Application.Driver).SetBufferSize (7, 5);
			menu.OpenMenu ();
			Application.Refresh ();

			expected = @"
┌──────
│ One  
│ Two  
└──────
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 1, 7, 4), pos);

			menu.CloseAllMenus ();
			menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
			((FakeDriver)Application.Driver).SetBufferSize (7, 3);
			menu.OpenMenu ();
			Application.Refresh ();

			expected = @"
┌──────
│ One  
│ Two  
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 7, 3), pos);
		}

		[Fact, AutoInitShutdown]
		public void UseSubMenusSingleFrame_False_By_Keyboard ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Numbers", new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuBarItem ("Two", new MenuItem [] {
						new MenuItem ("Sub-Menu 1", "", null),
						new MenuItem ("Sub-Menu 2", "", null)
					}),
					new MenuItem ("Three", "", null),
				})
			});
			menu.UseKeysUpDownAsKeysLeftRight = true;
			Application.Top.Add (menu);

			Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
			Assert.False (menu.UseSubMenusSingleFrame);

			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
 Numbers
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (Application.Top.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers                 
┌────────┐               
│ One    │               
│ Two   ►│┌─────────────┐
│ Three  ││ Sub-Menu 1  │
└────────┘│ Sub-Menu 2  │
          └─────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (Application.Top.Subviews [2].ProcessKey (new KeyEvent (Key.CursorLeft, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (Application.Top.Subviews [1].ProcessKey (new KeyEvent (Key.Esc, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void UseSubMenusSingleFrame_False_By_Mouse ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Numbers", new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuBarItem ("Two", new MenuItem [] {
						new MenuItem ("Sub-Menu 1", "", null),
						new MenuItem ("Sub-Menu 2", "", null)
					}),
					new MenuItem ("Three", "", null),
				})
			});

			Application.Top.Add (menu);

			Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
			Assert.False (menu.UseSubMenusSingleFrame);

			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
 Numbers
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 8, 1), pos);

			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 10, 6), pos);

			Assert.False (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 3,
				Flags = MouseFlags.ReportMousePosition,
				View = Application.Top.Subviews [1]
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers                 
┌────────┐               
│ One    │               
│ Two   ►│┌─────────────┐
│ Three  ││ Sub-Menu 1  │
└────────┘│ Sub-Menu 2  │
          └─────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 25, 7), pos);

			Assert.False (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 2,
				Flags = MouseFlags.ReportMousePosition,
				View = Application.Top.Subviews [1]
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 10, 6), pos);

			Assert.False (menu.MouseEvent (new MouseEvent () {
				X = 70,
				Y = 2,
				Flags = MouseFlags.Button1Clicked,
				View = Application.Top
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 8, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void UseSubMenusSingleFrame_True_By_Keyboard ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Numbers", new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuBarItem ("Two", new MenuItem [] {
						new MenuItem ("Sub-Menu 1", "", null),
						new MenuItem ("Sub-Menu 2", "", null)
					}),
					new MenuItem ("Three", "", null),
				})
			});

			Application.Top.Add (menu);

			Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
			Assert.False (menu.UseSubMenusSingleFrame);
			menu.UseSubMenusSingleFrame = true;
			Assert.True (menu.UseSubMenusSingleFrame);

			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
 Numbers
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 8, 1), pos);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 10, 6), pos);

			Assert.True (Application.Top.Subviews [1].ProcessKey (new KeyEvent (Key.CursorDown, null)));
			Assert.True (Application.Top.Subviews [1].ProcessKey (new KeyEvent (Key.Enter, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers       
┌─────────────┐
│◄    Two     │
├─────────────┤
│ Sub-Menu 1  │
│ Sub-Menu 2  │
└─────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 15, 7), pos);

			Assert.True (Application.Top.Subviews [2].ProcessKey (new KeyEvent (Key.Enter, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 10, 6), pos);

			Assert.True (Application.Top.Subviews [1].ProcessKey (new KeyEvent (Key.Esc, null)));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 8, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void UseSubMenusSingleFrame_True_By_Mouse ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Numbers", new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuBarItem ("Two", new MenuItem [] {
						new MenuItem ("Sub-Menu 1", "", null),
						new MenuItem ("Sub-Menu 2", "", null)
					}),
					new MenuItem ("Three", "", null),
				})
			});

			Application.Top.Add (menu);

			Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
			Assert.False (menu.UseSubMenusSingleFrame);
			menu.UseSubMenusSingleFrame = true;
			Assert.True (menu.UseSubMenusSingleFrame);

			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
 Numbers
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 8, 1), pos);

			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 10, 6), pos);

			Assert.False (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 3,
				Flags = MouseFlags.Button1Clicked,
				View = Application.Top.Subviews [1]
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers       
┌─────────────┐
│◄    Two     │
├─────────────┤
│ Sub-Menu 1  │
│ Sub-Menu 2  │
└─────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 15, 7), pos);

			Assert.False (menu.MouseEvent (new MouseEvent () {
				X = 1,
				Y = 2,
				Flags = MouseFlags.Button1Clicked,
				View = Application.Top.Subviews [2]
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 10, 6), pos);

			Assert.False (menu.MouseEvent (new MouseEvent () {
				X = 70,
				Y = 2,
				Flags = MouseFlags.Button1Clicked,
				View = Application.Top
			}));
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 Numbers
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 8, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void HotKey_MenuBar_OnKeyDown_OnKeyUp_ProcessHotKey_ProcessKey ()
		{
			var newAction = false;
			var copyAction = false;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => newAction = true)
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => copyAction = true)
				})
			});

			Application.Top.Add (menu);

			Assert.False (newAction);
			Assert.False (copyAction);

			Assert.False (menu.OnKeyDown (new (Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (menu.OnKeyUp (new (Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
 File  Edit
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 11, 1), pos);

			Assert.True (menu.ProcessKey (new (Key.N, null)));
			Application.MainLoop.MainIteration ();
			Assert.True (newAction);

			Assert.True (menu.ProcessHotKey (new (Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
 File  Edit
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (1, 0, 11, 1), pos);

			Assert.True (menu.ProcessKey (new (Key.CursorRight, null)));
			Assert.True (menu.ProcessKey (new (Key.C, null)));
			Application.MainLoop.MainIteration ();
			Assert.True (copyAction);
		}

		// Defines the expected strings for a Menu. Currently supports 
		//   - MenuBar with any number of MenuItems 
		//   - Each top-level MenuItem can have a SINGLE sub-menu
		//
		// TODO: Enable multiple sub-menus
		// TODO: Enable checked sub-menus
		// TODO: Enable sub-menus with sub-menus (perhaps better to put this in a separate class with focused unit tests?)
		//
		// E.g: 
		//
		// File  Edit
		//  New    Copy
		public class ExpectedMenuBar : MenuBar {
			FakeDriver d = (FakeDriver)Application.Driver;

			// Each MenuBar title has a 1 space pad on each side
			// See `static int leftPadding` and `static int rightPadding` on line 1037 of Menu.cs
			public string MenuBarText {
				get {
					string txt = string.Empty;
					foreach (var m in Menus) 
						txt += " " + m.Title.ToString () + " ";
					return txt;
				}
			}

			// The expected strings when the menu is closed
			public string ClosedMenuText => MenuBarText + "\n";

			// Padding for the X of the sub menu Frane
			// Menu.cs - Line 1239 in `internal void OpenMenu` is where the Menu is created
			string padding (int i)
			{
				int n = 0;
				while (i > 0) {
					n += Menus [i - 1].TitleLength + 2;
					i--;
				}
				return new string (' ', n);
			}

			// Define expected menu frame
			// "┌──────┐"
			// "│ New  │"
			// "└──────┘"
			// 
			// The width of the Frame is determined in Menu.cs line 144, where `Width` is calculated
			//   1 space before the Title and 2 spaces after the Title/Check/Help
			public string expectedTopRow (int i) => $"{d.ULCorner}{new string (d.HLine.ToString () [0], Menus [i].Children [0].TitleLength + 3)}{d.URCorner}  \n";
			// The 3 spaces at end are a result of Menu.cs line 1062 where `pos` is calculated (` + spacesAfterTitle`)
			public string expectedMenuItemRow (int i) => $"{d.VLine} {Menus [i].Children [0].Title}  {d.VLine}   \n";
			public string expectedBottomRow (int i) => $"{d.LLCorner}{new string (d.HLine.ToString () [0], Menus [i].Children [0].TitleLength + 3)}{d.LRCorner}  \n";

			// The fulll expected string for an open sub menu
			public string expectedSubMenuOpen (int i) => ClosedMenuText +
				(Menus [i].Children.Length > 0 ?
					padding (i) + expectedTopRow (i) +
					padding (i) + expectedMenuItemRow (i) +
					padding (i) + expectedBottomRow (i)
				:
				"");

			public ExpectedMenuBar (MenuBarItem [] menus) : base (menus)
			{
			}
		}

		[Fact, AutoInitShutdown]
		public void MenuBar_Submenus_Alignment_Correct ()
		{
			// Define the expected menu
			var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("Really Long Sub Menu", "",  null)
				}),
				new MenuBarItem ("123", new MenuItem [] {
					new MenuItem ("Copy", "", null)
				}),
				new MenuBarItem ("Format", new MenuItem [] {
					new MenuItem ("Word Wrap", "", null)
				}),
				new MenuBarItem ("Help", new MenuItem [] {
					new MenuItem ("About", "", null)
				}),
				new MenuBarItem ("1", new MenuItem [] {
					new MenuItem ("2", "", null)
				}),
				new MenuBarItem ("3", new MenuItem [] {
					new MenuItem ("2", "", null)
				}),
				new MenuBarItem ("Last one", new MenuItem [] {
					new MenuItem ("Test", "", null)
				})
			});

			var items = new MenuBarItem [expectedMenu.Menus.Length];
			for (var i = 0; i < expectedMenu.Menus.Length; i++) 				items [i] = new MenuBarItem (expectedMenu.Menus [i].Title, new MenuItem [] {
					new MenuItem (expectedMenu.Menus [i].Children [0].Title, "", null)
				});
			var menu = new MenuBar (items);

			Application.Top.Add (menu);

			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);

			for (var i = 0; i < expectedMenu.Menus.Length; i++) {
				menu.OpenMenu (i);
				Assert.True (menu.IsMenuOpen);
				Application.Top.Redraw (Application.Top.Bounds);
				TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (i), output);
			}
		}

		[Fact, AutoInitShutdown]
		public void HotKey_MenuBar_ProcessHotKey_Menu_ProcessKey ()
		{
			var newAction = false;
			var copyAction = false;

			// Define the expected menu
			var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("New", "",  null)
				}),
				new MenuBarItem ("Edit", new MenuItem [] {
					new MenuItem ("Copy", "", null)
				})
			});

			// The real menu
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_" + expectedMenu.Menus[0].Title, new MenuItem [] {
					new MenuItem ("_" + expectedMenu.Menus[0].Children[0].Title, "",  () => newAction = true)
				}),
				new MenuBarItem ("_" + expectedMenu.Menus[1].Title, new MenuItem [] {
					new MenuItem ("_" + expectedMenu.Menus[1].Children[0].Title, "",  () => copyAction = true)
				}),
			});

			Application.Top.Add (menu);

			Assert.False (newAction);
			Assert.False (copyAction);

			Assert.True (menu.ProcessHotKey (new (Key.AltMask | Key.F, new KeyModifiers () { Alt = true })));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			Assert.True (Application.Top.Subviews [1].ProcessKey (new (Key.N, null)));
			Application.MainLoop.MainIteration ();
			Assert.True (newAction);

			Assert.True (menu.ProcessHotKey (new (Key.AltMask | Key.E, new KeyModifiers () { Alt = true })));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), output);

			Assert.True (Application.Top.Subviews [1].ProcessKey (new (Key.C, null)));
			Application.MainLoop.MainIteration ();
			Assert.True (copyAction);
		}

		[Fact, AutoInitShutdown]
		public void MenuBar_Position_And_Size_With_HotKeys_Is_The_Same_As_Without_HotKeys ()
		{
			// Define the expected menu
			var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("12", "",  null)
				}),
				new MenuBarItem ("Edit", new MenuItem [] {
					new MenuItem ("Copy", "", null)
				})
			});

			// Test without HotKeys first
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (expectedMenu.Menus[0].Title, new MenuItem [] {
					new MenuItem (expectedMenu.Menus[0].Children[0].Title, "", null)
				}),
				new MenuBarItem (expectedMenu.Menus[1].Title, new MenuItem [] {
					new MenuItem (expectedMenu.Menus[1].Children[0].Title, "", null)
				})
			});

			Application.Top.Add (menu);

			// Open first
			Assert.True (menu.ProcessHotKey (new (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			// Open second
			Assert.True (Application.Top.Subviews [1].ProcessKey (new (Key.CursorRight, null)));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), output);

			// Close menu
			Assert.True (menu.ProcessHotKey (new (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);

			Application.Top.Remove (menu);

			// Now test WITH HotKeys
			menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_" + expectedMenu.Menus[0].Title, new MenuItem [] {
					new MenuItem ("_" + expectedMenu.Menus[0].Children[0].Title, "",  null)
				}),
				new MenuBarItem ("_" + expectedMenu.Menus[1].Title, new MenuItem [] {
					new MenuItem ("_" + expectedMenu.Menus[1].Children[0].Title, "",  null)
				}),
			});

			Application.Top.Add (menu);

			// Open first
			Assert.True (menu.ProcessHotKey (new (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			// Open second
			Assert.True (Application.Top.Subviews [1].ProcessKey (new (Key.CursorRight, null)));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), output);

			// Close menu
			Assert.True (menu.ProcessHotKey (new (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
		}

		[Fact, AutoInitShutdown]
		public void MenuBar_ButtonPressed_Open_The_Menu_ButtonPressed_Again_Close_The_Menu ()
		{
			// Define the expected menu
			var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("Open", "",  null)
				}),
				new MenuBarItem ("Edit", new MenuItem [] {
					new MenuItem ("Copy", "", null)
				})
			});

			// Test without HotKeys first
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_" + expectedMenu.Menus[0].Title, new MenuItem [] {
					new MenuItem ("_" + expectedMenu.Menus[0].Children[0].Title, "",  null)
				}),
				new MenuBarItem ("_" + expectedMenu.Menus[1].Title, new MenuItem [] {
					new MenuItem ("_" + expectedMenu.Menus[1].Children[0].Title, "",  null)
				}),
			});

			Application.Top.Add (menu);

			Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
			Assert.True (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);

			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
			Assert.False (menu.IsMenuOpen);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
		}

		[Fact]
		public void UseKeysUpDownAsKeysLeftRight_And_UseSubMenusSingleFrame_Cannot_Be_Both_True ()
		{
			var menu = new MenuBar ();
			Assert.False (menu.UseKeysUpDownAsKeysLeftRight);
			Assert.False (menu.UseSubMenusSingleFrame);

			menu.UseKeysUpDownAsKeysLeftRight = true;
			Assert.True (menu.UseKeysUpDownAsKeysLeftRight);
			Assert.False (menu.UseSubMenusSingleFrame);

			menu.UseSubMenusSingleFrame = true;
			Assert.False (menu.UseKeysUpDownAsKeysLeftRight);
			Assert.True (menu.UseSubMenusSingleFrame);
		}

		[Fact, AutoInitShutdown]
		public void Parent_MenuItem_Stay_Focused_If_Child_MenuItem_Is_Empty_By_Mouse ()
		{
			// File  Edit  Format
			//┌──────┐    ┌───────┐         
			//│ New  │    │ Wrap  │         
			//└──────┘    └───────┘         

			// Define the expected menu
			var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("New", "",  null)
				}),
				new MenuBarItem ("Edit", new MenuItem [] {}),
				new MenuBarItem ("Format", new MenuItem [] {
					new MenuItem ("Wrap", "", null)
				})
			});

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (expectedMenu.Menus[0].Title, new MenuItem [] {
					new MenuItem (expectedMenu.Menus[0].Children[0].Title, "", null)
				}),
				new MenuBarItem (expectedMenu.Menus[1].Title, new MenuItem [] {}),
				new MenuBarItem (expectedMenu.Menus[2].Title, new MenuItem [] {
					new MenuItem (expectedMenu.Menus[2].Children[0].Title, "", null)
				})
			});

			var tf = new TextField () { Y = 2, Width = 10 };
			Application.Top.Add (menu, tf);
			Application.Begin (Application.Top);

			Assert.True (tf.HasFocus);
			Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			Assert.True (menu.MouseEvent (new MouseEvent () { X = 8, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), output);

			Assert.True (menu.MouseEvent (new MouseEvent () { X = 15, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (2), output);

			Assert.True (menu.MouseEvent (new MouseEvent () { X = 8, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);

			Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);


			Assert.True (menu.MouseEvent (new MouseEvent () { X = 8, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
			Assert.False (menu.IsMenuOpen);
			Assert.True (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
		}

		[Fact, AutoInitShutdown]
		public void Parent_MenuItem_Stay_Focused_If_Child_MenuItem_Is_Empty_By_Keyboard ()
		{
			var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("New", "", null)
				}),
				new MenuBarItem ("Edit", Array.Empty<MenuItem> ()),
				new MenuBarItem ("Format", new MenuItem [] {
					new MenuItem ("Wrap", "", null)
				})
			});

			var items = new MenuBarItem [expectedMenu.Menus.Length];
			for (var i = 0; i < expectedMenu.Menus.Length; i++) 				items [i] = new MenuBarItem (expectedMenu.Menus [i].Title, expectedMenu.Menus [i].Children.Length > 0
					? new MenuItem [] {
						new MenuItem (expectedMenu.Menus [i].Children [0].Title, "", null),
					}
					: Array.Empty<MenuItem> ());
			var menu = new MenuBar (items);

			var tf = new TextField () { Y = 2, Width = 10 };
			Application.Top.Add (menu, tf);

			Application.Begin (Application.Top);
			Assert.True (tf.HasFocus);
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			// Right - Edit has no sub menu; this tests that no sub menu shows
			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), output);

			// Right - Format
			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (2), output);

			// Left - Edit
			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), output);

			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), output);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.True (tf.HasFocus);
			Application.Top.Redraw (Application.Top.Bounds);
			TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
		}

		[Fact, AutoInitShutdown]
		public void Key_Open_And_Close_The_MenuBar ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("New", "", null)
				})
			});
			Application.Top.Add (menu);
			Application.Begin (Application.Top);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);

			menu.Key = Key.F10 | Key.ShiftMask;
			Assert.False (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F10 | Key.ShiftMask, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F10 | Key.ShiftMask, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
		}

		[Fact, AutoInitShutdown]
		public void Disabled_MenuItem_Is_Never_Selected ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("Menu", new MenuItem [] {
					new MenuItem ("Enabled 1", "", null),
					new MenuItem ("Disabled", "", null, () => false),
					null,
					new MenuItem ("Enabled 2", "", null)
				})
			});

			var top = Application.Top;
			top.Add (menu);
			Application.Begin (top);

			var attributes = new Attribute [] {
				// 0
				menu.ColorScheme.Normal,
				// 1
				menu.ColorScheme.Focus,
				// 2
				menu.ColorScheme.Disabled
			};

			TestHelpers.AssertDriverColorsAre (@"
00000000000000", attributes);

			Assert.True (menu.MouseEvent (new MouseEvent {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000", attributes);

			Assert.True (top.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 2,
				Flags = MouseFlags.Button1Clicked,
				View = top.Subviews [1]
			}));
			top.Subviews [1].Redraw (top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000", attributes);

			Assert.True (top.Subviews [1].MouseEvent (new MouseEvent {
				X = 0,
				Y = 2,
				Flags = MouseFlags.ReportMousePosition,
				View = top.Subviews [1]
			}));
			top.Subviews [1].Redraw (top.Bounds);
			TestHelpers.AssertDriverColorsAre (@"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000", attributes);
		}

		[Fact, AutoInitShutdown]
		public void MenuBar_With_Action_But_Without_MenuItems_Not_Throw ()
		{
			var menu = new MenuBar (
			    menus: new []
			    {
				new MenuBarItem { Title = "Test 1", Action = () => { } },
				new MenuBarItem { Title = "Test 2", Action = () => { } },
			    });

			Application.Top.Add (menu);
			Application.Begin (Application.Top);

			Assert.False (Application.Top.OnKeyDown (new KeyEvent (Key.AltMask, new KeyModifiers { Alt = true })));
			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
		}

		[Fact, AutoInitShutdown]
		public void MenuBar_In_Window_Without_Other_Views ()
		{
			var win = new Window ();
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("New", "", null)
				}),
				new MenuBarItem ("Edit", new MenuItem [] {
					new MenuBarItem ("Delete", new MenuItem [] {
						new MenuItem ("All", "", null),
						new MenuItem ("Selected", "", null)
					})
				})
			}); ;
			win.Add (menu);
			var top = Application.Top;
			top.Add (win);
			Application.Begin (top);
			((FakeDriver)Application.Driver).SetBufferSize (40, 8);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", output);

			Assert.True (win.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", output);

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘", output);

			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘", output);

			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			top.Redraw (top.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void AllowNullChecked_Get_Set ()
		{
			var mi = new MenuItem ("Check this out 你", "", null) {
				CheckType = MenuItemCheckStyle.Checked
			};
			mi.Action = mi.ToggleChecked;
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem("Nullable Checked",new MenuItem [] {
					mi
				})
			});
			new CheckBox ();
			var top = Application.Top;
			top.Add (menu);
			Application.Begin (top);

			Assert.False (mi.Checked);
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Application.MainLoop.MainIteration ();
			Assert.True (mi.Checked);
			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Assert.True (menu.openMenu.MouseEvent (new MouseEvent () {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked,
				View = menu.openMenu
			}));
			Application.MainLoop.MainIteration ();
			Assert.False (mi.Checked);

			mi.AllowNullChecked = true;
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Application.MainLoop.MainIteration ();
			Assert.Null (mi.Checked);
			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 Nullable Checked       
┌──────────────────────┐
│ ⍰ Check this out 你  │
└──────────────────────┘", output);
			Assert.True (menu.openMenu.MouseEvent (new MouseEvent () {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked,
				View = menu.openMenu
			}));
			Application.MainLoop.MainIteration ();
			Assert.True (mi.Checked);
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.openMenu.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Application.MainLoop.MainIteration ();
			Assert.False (mi.Checked);
			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 0,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Assert.True (menu.openMenu.MouseEvent (new MouseEvent () {
				X = 0,
				Y = 1,
				Flags = MouseFlags.Button1Clicked,
				View = menu.openMenu
			}));
			Application.MainLoop.MainIteration ();
			Assert.Null (mi.Checked);

			mi.AllowNullChecked = false;
			Assert.False (mi.Checked);

			mi.CheckType = MenuItemCheckStyle.NoCheck;
			Assert.Throws<InvalidOperationException> (mi.ToggleChecked);

			mi.CheckType = MenuItemCheckStyle.Radio;
			Assert.Throws<InvalidOperationException> (mi.ToggleChecked);
		}
	}
}
