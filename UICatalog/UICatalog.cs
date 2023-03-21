using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Terminal.Gui;
using System.IO;
using System.Reflection;
using System.Threading;
using Terminal.Gui.Configuration;
using static Terminal.Gui.Configuration.ConfigurationManager;
using System.Text.Json.Serialization;

#nullable enable

/// <summary>
/// UI Catalog is a comprehensive sample library for Terminal.Gui. It provides a simple UI for adding to the catalog of scenarios.
/// </summary>
/// <remarks>
/// <para>
///	UI Catalog attempts to satisfy the following goals:
/// </para>
/// <para>
/// <list type="number">
///	<item>
///		<description>
///		Be an easy to use showcase for Terminal.Gui concepts and features.
///		</description>
///	</item>
///	<item>
///		<description>
///		Provide sample code that illustrates how to properly implement said concepts & features.
///		</description>
///	</item>
///	<item>
///		<description>
///		Make it easy for contributors to add additional samples in a structured way.
///		</description>
///	</item>
/// </list>
/// </para>	
/// <para>
///	See the project README for more details (https://github.com/gui-cs/Terminal.Gui/tree/master/UICatalog/README.md).
/// </para>	
/// </remarks>
namespace UICatalog {
	/// <summary>
	/// UI Catalog is a comprehensive sample app and scenario library for <see cref="Terminal.Gui"/>
	/// </summary>
	class UICatalogApp {
		//[SerializableConfigurationProperty (Scope = typeof (AppScope), OmitClassName = true), JsonPropertyName ("UICatalog.StatusBar")]
		//public static bool ShowStatusBar { get; set; } = true;

		[SerializableConfigurationProperty (Scope = typeof (AppScope), OmitClassName = true), JsonPropertyName ("UICatalog.StatusBar")]
		public static bool ShowStatusBar { get; set; } = true;

		static readonly FileSystemWatcher _currentDirWatcher = new FileSystemWatcher ();
		static readonly FileSystemWatcher _homeDirWatcher = new FileSystemWatcher ();

		static void Main (string [] args)
		{
			Console.OutputEncoding = Encoding.Default;

			if (Debugger.IsAttached) {
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
			}

			_scenarios = Scenario.GetScenarios ();
			_categories = Scenario.GetAllCategories ();
			_nameColumnWidth = _scenarios.OrderByDescending (s => s.GetName ().Length).FirstOrDefault ().GetName ().Length;

			if (args.Length > 0 && args.Contains ("-usc")) {
				_useSystemConsole = true;
				args = args.Where (val => val != "-usc").ToArray ();
			}

			StartConfigFileWatcher ();

			// If a Scenario name has been provided on the commandline
			// run it and exit when done.
			if (args.Length > 0) {
				var item = _scenarios.FindIndex (s => s.GetName ().Equals (args [0], StringComparison.OrdinalIgnoreCase));
				_selectedScenario = (Scenario)Activator.CreateInstance (_scenarios [item].GetType ());
				Application.UseSystemConsole = _useSystemConsole;
				Application.Init ();
				_selectedScenario.Theme = _cachedTheme;
				_selectedScenario.TopLevelColorScheme = _topLevelColorScheme;
				_selectedScenario.Init ();
				_selectedScenario.Setup ();
				_selectedScenario.Run ();
				_selectedScenario.Dispose ();
				_selectedScenario = null;
				Application.Shutdown ();
				VerifyObjectsWereDisposed ();
				return;
			}

			_aboutMessage = new StringBuilder ();
			_aboutMessage.AppendLine (@"A comprehensive sample library for");
			_aboutMessage.AppendLine (@"");
			_aboutMessage.AppendLine (@"  _______                  _             _   _____       _  ");
			_aboutMessage.AppendLine (@" |__   __|                (_)           | | / ____|     (_) ");
			_aboutMessage.AppendLine (@"    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _  ");
			_aboutMessage.AppendLine (@"    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | | ");
			_aboutMessage.AppendLine (@"    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | | ");
			_aboutMessage.AppendLine (@"    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_| ");
			_aboutMessage.AppendLine (@"");
			_aboutMessage.AppendLine (@"v2 - Work in Progress");
			_aboutMessage.AppendLine (@"");
			_aboutMessage.AppendLine (@"https://github.com/gui-cs/Terminal.Gui");

			Scenario scenario;
			while ((scenario = RunUICatalogTopLevel ()) != null) {
				VerifyObjectsWereDisposed ();
				ConfigurationManager.Themes.Theme = _cachedTheme;
				ConfigurationManager.Apply ();
				scenario.Theme = _cachedTheme;
				scenario.TopLevelColorScheme = _topLevelColorScheme;
				scenario.Init ();
				scenario.Setup ();
				scenario.Run ();
				scenario.Dispose ();

				// This call to Application.Shutdown brackets the Application.Init call
				// made by Scenario.Init() above
				Application.Shutdown ();

				VerifyObjectsWereDisposed ();
			}

			StopConfigFileWatcher ();
			VerifyObjectsWereDisposed ();
		}

		private static void StopConfigFileWatcher ()
		{
			_currentDirWatcher.EnableRaisingEvents = false;
			_currentDirWatcher.Changed -= ConfigFileChanged;
			_currentDirWatcher.Created -= ConfigFileChanged;

			_homeDirWatcher.EnableRaisingEvents = false;
			_homeDirWatcher.Changed -= ConfigFileChanged;
			_homeDirWatcher.Created -= ConfigFileChanged;
		}

		private static void StartConfigFileWatcher ()
		{
			// Setup a file system watcher for `./.tui/`
			_currentDirWatcher.NotifyFilter = NotifyFilters.LastWrite;
			var f = new FileInfo (Assembly.GetExecutingAssembly ().Location);
			var tuiDir = Path.Combine (f.Directory.FullName, ".tui");

			if (!Directory.Exists (tuiDir)) {
				Directory.CreateDirectory (tuiDir);
			}
			_currentDirWatcher.Path = tuiDir;
			_currentDirWatcher.Filter = "*config.json";

			// Setup a file system watcher for `~/.tui/`
			_homeDirWatcher.NotifyFilter = NotifyFilters.LastWrite;
			f = new FileInfo (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));
			tuiDir = Path.Combine (f.FullName, ".tui");

			if (!Directory.Exists (tuiDir)) {
				Directory.CreateDirectory (tuiDir);
			}
			_homeDirWatcher.Path = tuiDir;
			_homeDirWatcher.Filter = "*config.json";

			_currentDirWatcher.Changed += ConfigFileChanged;
			//_currentDirWatcher.Created += ConfigFileChanged;
			_currentDirWatcher.EnableRaisingEvents = true;

			_homeDirWatcher.Changed += ConfigFileChanged;
			//_homeDirWatcher.Created += ConfigFileChanged;
			_homeDirWatcher.EnableRaisingEvents = true;
		}

		private static void ConfigFileChanged (object sender, FileSystemEventArgs e)
		{
			if (Application.Top == null) {
				return;
			}

			// TOOD: THis is a hack. Figure out how to ensure that the file is fully written before reading it.
			Thread.Sleep (500);
			ConfigurationManager.Load ();
			ConfigurationManager.Apply ();
		}

		/// <summary>
		/// Shows the UI Catalog selection UI. When the user selects a Scenario to run, the
		/// UI Catalog main app UI is killed and the Scenario is run as though it were Application.Top. 
		/// When the Scenario exits, this function exits.
		/// </summary>
		/// <returns></returns>
		static Scenario RunUICatalogTopLevel ()
		{
			Application.UseSystemConsole = _useSystemConsole;

			// Run UI Catalog UI. When it exits, if _selectedScenario is != null then
			// a Scenario was selected. Otherwise, the user wants to quit UI Catalog.
			Application.Init ();

			if (_cachedTheme is null) {
				_cachedTheme = ConfigurationManager.Themes.Theme;
			} else {
				ConfigurationManager.Themes.Theme = _cachedTheme;
				ConfigurationManager.Apply ();
			}

			//Application.EnableConsoleScrolling = _enableConsoleScrolling;

			Application.Run<UICatalogTopLevel> ();
			Application.Shutdown ();

			return _selectedScenario;
		}

		static List<Scenario> _scenarios;
		static List<string> _categories;
		static int _nameColumnWidth;
		// When a scenario is run, the main app is killed. These items
		// are therefore cached so that when the scenario exits the
		// main app UI can be restored to previous state
		static int _cachedScenarioIndex = 0;
		static int _cachedCategoryIndex = 0;
		static string? _cachedTheme;
		
		static StringBuilder _aboutMessage;

		// If set, holds the scenario the user selected
		static Scenario _selectedScenario = null;

		static bool _useSystemConsole = false;
		static ConsoleDriver.DiagnosticFlags _diagnosticFlags;
		static bool _enableConsoleScrolling = false;
		static bool _isFirstRunning = true;
		static string _topLevelColorScheme;

		static MenuItem [] _themeMenuItems;
		static MenuBarItem _themeMenuBarItem;

		/// <summary>
		/// This is the main UI Catalog app view. It is run fresh when the app loads (if a Scenario has not been passed on 
		/// the command line) and each time a Scenario ends.
		/// </summary>
		public class UICatalogTopLevel : Toplevel {
			public MenuItem miIsMouseDisabled;
			public MenuItem miEnableConsoleScrolling;

			public TileView ContentPane;
			public ListView CategoryListView;
			public ListView ScenarioListView;

			public StatusItem Capslock;
			public StatusItem Numlock;
			public StatusItem Scrolllock;
			public StatusItem DriverName;
			public StatusItem OS;

			public UICatalogTopLevel ()
			{
				_themeMenuItems = CreateThemeMenuItems ();
				_themeMenuBarItem = new MenuBarItem ("_Themes", _themeMenuItems);
				MenuBar = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_File", new MenuItem [] {
						new MenuItem ("_Quit", "Quit UI Catalog", () => RequestStop(), null, null)
					}),
					_themeMenuBarItem,
					new MenuBarItem ("Diag_nostics", CreateDiagnosticMenuItems()),
					new MenuBarItem ("_Help", new MenuItem [] {
						new MenuItem ("_gui.cs API Overview", "", () => OpenUrl ("https://gui-cs.github.io/Terminal.Gui/articles/overview.html"), null, null, Key.F1),
						new MenuItem ("gui.cs _README", "", () => OpenUrl ("https://github.com/gui-cs/Terminal.Gui"), null, null, Key.F2),
						new MenuItem ("_About...",
							"About UI Catalog", () =>  MessageBox.Query ("About UI Catalog", _aboutMessage.ToString(), 0, null, false, "_Ok"), null, null, Key.CtrlMask | Key.A),
					}),
				});

				Capslock = new StatusItem (Key.CharMask, "Caps", null);
				Numlock = new StatusItem (Key.CharMask, "Num", null);
				Scrolllock = new StatusItem (Key.CharMask, "Scroll", null);
				DriverName = new StatusItem (Key.CharMask, "Driver:", null);
				OS = new StatusItem (Key.CharMask, "OS:", null);

				StatusBar = new StatusBar () {
					Visible = UICatalogApp.ShowStatusBar
				};

				StatusBar.Items = new StatusItem [] {
					new StatusItem(Application.QuitKey, $"~{Application.QuitKey} to quit", () => {
						if (_selectedScenario is null){
							// This causes GetScenarioToRun to return null
							_selectedScenario = null;
							RequestStop();
						} else {
							_selectedScenario.RequestStop();
						}
					}),
					new StatusItem(Key.F10, "~F10~ Status Bar", () => {
						StatusBar.Visible = !StatusBar.Visible;
						ContentPane.Height = Dim.Fill(StatusBar.Visible ? 1 : 0);
						LayoutSubviews();
						SetSubViewNeedsDisplay();
					}),
					DriverName,
					OS
				};

				ContentPane = new TileView () {
					X = 0,
					Y = 1, // for menu
					Width = Dim.Fill (),
					Height = Dim.Fill (1),
					CanFocus = true,
					Shortcut = Key.CtrlMask | Key.C,
				};
				ContentPane.Border.BorderStyle = BorderStyle.Single;
				ContentPane.SetSplitterPos (0, 25);
				ContentPane.ShortcutAction = () => ContentPane.SetFocus ();

				CategoryListView = new ListView (_categories) {
					X = 0,
					Y = 0,
					Width = Dim.Fill (0),
					Height = Dim.Fill (0),
					AllowsMarking = false,
					CanFocus = true,
				};
				CategoryListView.OpenSelectedItem += (s,a) => {
					ScenarioListView.SetFocus ();
				};
				CategoryListView.SelectedItemChanged += CategoryListView_SelectedChanged;

				ContentPane.Tiles.ElementAt (0).Title = "Categories";
				ContentPane.Tiles.ElementAt (0).MinSize = 2;
				ContentPane.Tiles.ElementAt (0).ContentView.Add (CategoryListView);

				ScenarioListView = new ListView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (0),
					Height = Dim.Fill (0),
					AllowsMarking = false,
					CanFocus = true,
				};

				ScenarioListView.OpenSelectedItem += ScenarioListView_OpenSelectedItem;

				ContentPane.Tiles.ElementAt (1).Title = "Scenarios";
				ContentPane.Tiles.ElementAt (1).ContentView.Add (ScenarioListView);
				ContentPane.Tiles.ElementAt (1).MinSize = 2;

				KeyDown += KeyDownHandler;
				Add (MenuBar);
				Add (ContentPane);

				Add (StatusBar);

				Loaded += LoadedHandler;
				Unloaded += UnloadedHandler;

				// Restore previous selections
				CategoryListView.SelectedItem = _cachedCategoryIndex;
				ScenarioListView.SelectedItem = _cachedScenarioIndex;

				ConfigurationManager.Applied += ConfigAppliedHandler;
			}
      
			void LoadedHandler (object sender, EventArgs args)
			{
				ConfigChanged ();

				miIsMouseDisabled.Checked = Application.IsMouseDisabled;
				miEnableConsoleScrolling.Checked = Application.EnableConsoleScrolling;
				DriverName.Title = $"Driver: {Driver.GetType ().Name}";
				OS.Title = $"OS: {Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystem} {Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.OperatingSystemVersion}";

				if (_selectedScenario != null) {
					_selectedScenario = null;
					_isFirstRunning = false;
				}
				if (!_isFirstRunning) {
					ScenarioListView.SetFocus ();
				}

				StatusBar.VisibleChanged += (s, e) => {
					UICatalogApp.ShowStatusBar = StatusBar.Visible;

					var height = (StatusBar.Visible ? 1 : 0);// + (MenuBar.Visible ? 1 : 0);
					ContentPane.Height = Dim.Fill (height);
					LayoutSubviews ();
					SetSubViewNeedsDisplay ();
				};

				Loaded -= LoadedHandler;
			}

			private void UnloadedHandler (object sender, EventArgs args)
			{
				ConfigurationManager.Applied -= ConfigAppliedHandler;
				Unloaded -= UnloadedHandler;
			}
      
			void ConfigAppliedHandler (object sender, ConfigurationManagerEventArgs a)
			{
				ConfigChanged ();
			}

			/// <summary>
			/// Launches the selected scenario, setting the global _selectedScenario
			/// </summary>
			/// <param name="e"></param>
			void ScenarioListView_OpenSelectedItem (object sender, EventArgs e)
			{
				if (_selectedScenario is null) {
					// Save selected item state
					_cachedCategoryIndex = CategoryListView.SelectedItem;
					_cachedScenarioIndex = ScenarioListView.SelectedItem;
					// Create new instance of scenario (even though Scenarios contains instances)
					_selectedScenario = (Scenario)Activator.CreateInstance (ScenarioListView.Source.ToList () [ScenarioListView.SelectedItem].GetType ());

					// Tell the main app to stop
					Application.RequestStop ();
				}
			}

			List<MenuItem []> CreateDiagnosticMenuItems ()
			{
				List<MenuItem []> menuItems = new List<MenuItem []> ();
				menuItems.Add (CreateDiagnosticFlagsMenuItems ());
				menuItems.Add (new MenuItem [] { null });
				menuItems.Add (CreateEnableConsoleScrollingMenuItems ());
				menuItems.Add (CreateDisabledEnabledMouseItems ());
				menuItems.Add (CreateKeybindingsMenuItems ());
				return menuItems;
			}

			MenuItem [] CreateDisabledEnabledMouseItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				miIsMouseDisabled = new MenuItem {
					Title = "_Disable Mouse"
				};
				miIsMouseDisabled.Shortcut = Key.CtrlMask | Key.AltMask | (Key)miIsMouseDisabled.Title.ToString ().Substring (1, 1) [0];
				miIsMouseDisabled.CheckType |= MenuItemCheckStyle.Checked;
				miIsMouseDisabled.Action += () => {
					miIsMouseDisabled.Checked = Application.IsMouseDisabled = (bool)!miIsMouseDisabled.Checked;
				};
				menuItems.Add (miIsMouseDisabled);

				return menuItems.ToArray ();
			}

			MenuItem [] CreateKeybindingsMenuItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				var item = new MenuItem {
					Title = "_Key Bindings",
					Help = "Change which keys do what"
				};
				item.Action += () => {
					var dlg = new KeyBindingsDialog ();
					Application.Run (dlg);
				};

				menuItems.Add (null);
				menuItems.Add (item);

				return menuItems.ToArray ();
			}

			MenuItem [] CreateEnableConsoleScrollingMenuItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				miEnableConsoleScrolling = new MenuItem ();
				miEnableConsoleScrolling.Title = "_Enable Console Scrolling";
				miEnableConsoleScrolling.Shortcut = Key.CtrlMask | Key.AltMask | (Key)miEnableConsoleScrolling.Title.ToString ().Substring (1, 1) [0];
				miEnableConsoleScrolling.CheckType |= MenuItemCheckStyle.Checked;
				miEnableConsoleScrolling.Action += () => {
					miEnableConsoleScrolling.Checked = !miEnableConsoleScrolling.Checked;
					Application.EnableConsoleScrolling = (bool)miEnableConsoleScrolling.Checked;
				};
				menuItems.Add (miEnableConsoleScrolling);

				return menuItems.ToArray ();
			}

			MenuItem [] CreateDiagnosticFlagsMenuItems ()
			{
				const string OFF = "Diagnostics: _Off";
				const string FRAME_RULER = "Diagnostics: Frame _Ruler";
				const string FRAME_PADDING = "Diagnostics: _Frame Padding";
				var index = 0;

				List<MenuItem> menuItems = new List<MenuItem> ();
				foreach (Enum diag in Enum.GetValues (_diagnosticFlags.GetType ())) {
					var item = new MenuItem {
						Title = GetDiagnosticsTitle (diag),
						Shortcut = Key.AltMask + index.ToString () [0]
					};
					index++;
					item.CheckType |= MenuItemCheckStyle.Checked;
					if (GetDiagnosticsTitle (ConsoleDriver.DiagnosticFlags.Off) == item.Title) {
						item.Checked = (_diagnosticFlags & (ConsoleDriver.DiagnosticFlags.FramePadding
						| ConsoleDriver.DiagnosticFlags.FrameRuler)) == 0;
					} else {
						item.Checked = _diagnosticFlags.HasFlag (diag);
					}
					item.Action += () => {
						var t = GetDiagnosticsTitle (ConsoleDriver.DiagnosticFlags.Off);
						if (item.Title == t && item.Checked == false) {
							_diagnosticFlags &= ~(ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler);
							item.Checked = true;
						} else if (item.Title == t && item.Checked == true) {
							_diagnosticFlags |= (ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler);
							item.Checked = false;
						} else {
							var f = GetDiagnosticsEnumValue (item.Title);
							if (_diagnosticFlags.HasFlag (f)) {
								SetDiagnosticsFlag (f, false);
							} else {
								SetDiagnosticsFlag (f, true);
							}
						}
						foreach (var menuItem in menuItems) {
							if (menuItem.Title == t) {
								menuItem.Checked = !_diagnosticFlags.HasFlag (ConsoleDriver.DiagnosticFlags.FrameRuler)
									&& !_diagnosticFlags.HasFlag (ConsoleDriver.DiagnosticFlags.FramePadding);
							} else if (menuItem.Title != t) {
								menuItem.Checked = _diagnosticFlags.HasFlag (GetDiagnosticsEnumValue (menuItem.Title));
							}
						}
						ConsoleDriver.Diagnostics = _diagnosticFlags;
						Application.Top.SetNeedsDisplay ();
					};
					menuItems.Add (item);
				}
				return menuItems.ToArray ();

				string GetDiagnosticsTitle (Enum diag)
				{
					return Enum.GetName (_diagnosticFlags.GetType (), diag) switch {
						"Off" => OFF,
						"FrameRuler" => FRAME_RULER,
						"FramePadding" => FRAME_PADDING,
						_ => "",
					};
				}

				Enum GetDiagnosticsEnumValue (ustring title)
				{
					return title.ToString () switch {
						FRAME_RULER => ConsoleDriver.DiagnosticFlags.FrameRuler,
						FRAME_PADDING => ConsoleDriver.DiagnosticFlags.FramePadding,
						_ => null,
					};
				}

				void SetDiagnosticsFlag (Enum diag, bool add)
				{
					switch (diag) {
					case ConsoleDriver.DiagnosticFlags.FrameRuler:
						if (add) {
							_diagnosticFlags |= ConsoleDriver.DiagnosticFlags.FrameRuler;
						} else {
							_diagnosticFlags &= ~ConsoleDriver.DiagnosticFlags.FrameRuler;
						}
						break;
					case ConsoleDriver.DiagnosticFlags.FramePadding:
						if (add) {
							_diagnosticFlags |= ConsoleDriver.DiagnosticFlags.FramePadding;
						} else {
							_diagnosticFlags &= ~ConsoleDriver.DiagnosticFlags.FramePadding;
						}
						break;
					default:
						_diagnosticFlags = default;
						break;
					}
				}
			}

			public MenuItem [] CreateThemeMenuItems ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				foreach (var theme in ConfigurationManager.Themes) {
					var item = new MenuItem {
						Title = theme.Key,
						Shortcut = Key.AltMask + theme.Key [0]
					};
					item.CheckType |= MenuItemCheckStyle.Checked;
					item.Checked = theme.Key == _cachedTheme; // ConfigurationManager.Themes.Theme;
					item.Action += () => {
						ConfigurationManager.Themes.Theme = _cachedTheme = theme.Key;
						ConfigurationManager.Apply ();
					};
					menuItems.Add (item);
				}

				var schemeMenuItems = new List<MenuItem> ();
				foreach (var sc in Colors.ColorSchemes) {
					var item = new MenuItem {
						Title = $"_{sc.Key}",
						Data = sc.Key,
						Shortcut = Key.AltMask | (Key)sc.Key [..1] [0]
					};
					item.CheckType |= MenuItemCheckStyle.Radio;
					item.Checked = sc.Key == _topLevelColorScheme;
					item.Action += () => {
						_topLevelColorScheme = (string)item.Data;
						foreach (var schemeMenuItem in schemeMenuItems) {
							schemeMenuItem.Checked = (string)schemeMenuItem.Data == _topLevelColorScheme;
						}
						ColorScheme = Colors.ColorSchemes [_topLevelColorScheme];
						Application.Top.SetNeedsDisplay ();
					};
					schemeMenuItems.Add (item);
				}
				menuItems.Add (null);
				var mbi = new MenuBarItem ("_Color Scheme for Application.Top", schemeMenuItems.ToArray ());
				menuItems.Add (mbi);

				return menuItems.ToArray ();
			}

			public void ConfigChanged ()
			{
				if (_topLevelColorScheme == null || !Colors.ColorSchemes.ContainsKey (_topLevelColorScheme)) {
					_topLevelColorScheme = "Base";
				}

				_themeMenuItems = ((UICatalogTopLevel)Application.Top).CreateThemeMenuItems ();
				_themeMenuBarItem.Children = _themeMenuItems;

				var checkedThemeMenu = _themeMenuItems.Where (m => (bool)m.Checked).FirstOrDefault ();
				if (checkedThemeMenu != null) {
					checkedThemeMenu.Checked = false;
				}
				checkedThemeMenu = _themeMenuItems.Where (m => m != null && m.Title == ConfigurationManager.Themes.Theme).FirstOrDefault ();
				if (checkedThemeMenu != null) {
					ConfigurationManager.Themes.Theme = checkedThemeMenu.Title.ToString ();
					checkedThemeMenu.Checked = true;
				}
				var schemeMenuItems = ((MenuBarItem)_themeMenuItems.Where (i => i is MenuBarItem).FirstOrDefault ()).Children;
				foreach (var schemeMenuItem in schemeMenuItems) {
					schemeMenuItem.Checked = (string)schemeMenuItem.Data == _topLevelColorScheme;
				}

				ColorScheme = Colors.ColorSchemes [_topLevelColorScheme];

				ContentPane.Border.BorderStyle = FrameView.DefaultBorderStyle;

				MenuBar.Menus [0].Children [0].Shortcut = Application.QuitKey;
				StatusBar.Items [0].Shortcut = Application.QuitKey;
				StatusBar.Items [0].Title = $"~{Application.QuitKey} to quit";

				miIsMouseDisabled.Checked = Application.IsMouseDisabled;
				miEnableConsoleScrolling.Checked = Application.EnableConsoleScrolling;

				var height = (UICatalogApp.ShowStatusBar ? 1 : 0);// + (MenuBar.Visible ? 1 : 0);
				ContentPane.Height = Dim.Fill (height);

				StatusBar.Visible = UICatalogApp.ShowStatusBar;

				Application.Top.SetNeedsDisplay ();
			}

			void KeyDownHandler (object sender, KeyEventEventArgs a)
			{
				if (a.KeyEvent.IsCapslock) {
					Capslock.Title = "Caps: On";
					StatusBar.SetNeedsDisplay ();
				} else {
					Capslock.Title = "Caps: Off";
					StatusBar.SetNeedsDisplay ();
				}

				if (a.KeyEvent.IsNumlock) {
					Numlock.Title = "Num: On";
					StatusBar.SetNeedsDisplay ();
				} else {
					Numlock.Title = "Num: Off";
					StatusBar.SetNeedsDisplay ();
				}

				if (a.KeyEvent.IsScrolllock) {
					Scrolllock.Title = "Scroll: On";
					StatusBar.SetNeedsDisplay ();
				} else {
					Scrolllock.Title = "Scroll: Off";
					StatusBar.SetNeedsDisplay ();
				}
			}

			void CategoryListView_SelectedChanged (object sender, ListViewItemEventArgs e)
			{
				var item = _categories [e.Item];
				List<Scenario> newlist;
				if (e.Item == 0) {
					// First category is "All"
					newlist = _scenarios;

				} else {
					newlist = _scenarios.Where (s => s.GetCategories ().Contains (item)).ToList ();
				}
				ScenarioListView.SetSource (newlist.ToList ());
			}
		}

		static void VerifyObjectsWereDisposed ()
		{
#if DEBUG_IDISPOSABLE
			// Validate there are no outstanding Responder-based instances 
			// after a scenario was selected to run. This proves the main UI Catalog
			// 'app' closed cleanly.
			foreach (var inst in Responder.Instances) {

				Debug.Assert (inst.WasDisposed);
			}
			Responder.Instances.Clear ();

			// Validate there are no outstanding Application.RunState-based instances 
			// after a scenario was selected to run. This proves the main UI Catalog
			// 'app' closed cleanly.
			foreach (var inst in Application.RunState.Instances) {
				Debug.Assert (inst.WasDisposed);
			}
			Application.RunState.Instances.Clear ();
#endif
		}

		static void OpenUrl (string url)
		{
			try {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					url = url.Replace ("&", "^&");
					Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					using var process = new Process {
						StartInfo = new ProcessStartInfo {
							FileName = "xdg-open",
							Arguments = url,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
							UseShellExecute = false
						}
					};
					process.Start ();
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					Process.Start ("open", url);
				}
			} catch {
				throw;
			}
		}
	}
}
