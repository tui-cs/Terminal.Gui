using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Terminal.Gui;
using Rune = System.Rune;

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
	public class UICatalogApp {
		private static Toplevel _top;
		private static MenuBar _menu;
		private static int _nameColumnWidth;
		private static FrameView _leftPane;
		private static List<string> _categories;
		private static ListView _categoryListView;
		private static FrameView _rightPane;
		private static List<Type> _scenarios;
		private static ListView _scenarioListView;
		private static StatusBar _statusBar;
		private static StatusItem _capslock;
		private static StatusItem _numlock;
		private static StatusItem _scrolllock;
		private static int _categoryListViewItem;
		private static int _scenarioListViewItem;

		private static Scenario _runningScenario = null;
		private static bool _useSystemConsole = false;
		private static ConsoleDriver.DiagnosticFlags _diagnosticFlags;
		private static bool _heightAsBuffer = false;
		private static bool _isFirstRunning = true;

		static void Main (string [] args)
		{
			Console.OutputEncoding = Encoding.Default;

			if (Debugger.IsAttached)
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");

			_scenarios = Scenario.GetDerivedClasses<Scenario> ().OrderBy (t => Scenario.ScenarioMetadata.GetName (t)).ToList ();

			if (args.Length > 0 && args.Contains ("-usc")) {
				_useSystemConsole = true;
				args = args.Where (val => val != "-usc").ToArray ();
			}
			if (args.Length > 0) {
				var item = _scenarios.FindIndex (t => Scenario.ScenarioMetadata.GetName (t).Equals (args [0], StringComparison.OrdinalIgnoreCase));
				_runningScenario = (Scenario)Activator.CreateInstance (_scenarios [item]);
				Application.UseSystemConsole = _useSystemConsole;
				Application.Init ();
				_runningScenario.Init (Application.Top, _baseColorScheme);
				_runningScenario.Setup ();
				_runningScenario.Run ();
				_runningScenario = null;
				Application.Shutdown ();
				return;
			}

			Scenario scenario;
			while ((scenario = GetScenarioToRun ()) != null) {
#if DEBUG_IDISPOSABLE
				// Validate there are no outstanding Responder-based instances 
				// after a scenario was selected to run. This proves the main UI Catalog
				// 'app' closed cleanly.
				foreach (var inst in Responder.Instances) {
					Debug.Assert (inst.WasDisposed);
				}
				Responder.Instances.Clear ();
#endif

				scenario.Init (Application.Top, _baseColorScheme);
				scenario.Setup ();
				scenario.Run ();

				//static void LoadedHandler ()
				//{
				//	_rightPane.SetFocus ();
				//	_top.Loaded -= LoadedHandler;
				//}

				//_top.Loaded += LoadedHandler;

				Application.Shutdown ();

#if DEBUG_IDISPOSABLE
				// After the scenario runs, validate all Responder-based instances
				// were disposed. This proves the scenario 'app' closed cleanly.
				foreach (var inst in Responder.Instances) {
					Debug.Assert (inst.WasDisposed);
				}
				Responder.Instances.Clear ();
#endif
			}

			Application.Shutdown ();

#if DEBUG_IDISPOSABLE
			// This proves that when the user exited the UI Catalog app
			// it cleaned up properly.
			foreach (var inst in Responder.Instances) {
				Debug.Assert (inst.WasDisposed);
			}
			Responder.Instances.Clear ();
#endif
		}

		/// <summary>
		/// This shows the selection UI. Each time it is run, it calls Application.Init to reset everything.
		/// </summary>
		/// <returns></returns>
		private static Scenario GetScenarioToRun ()
		{
			Application.UseSystemConsole = _useSystemConsole;
			Application.Init ();
			Application.HeightAsBuffer = _heightAsBuffer;

			// Set this here because not initialized until driver is loaded
			_baseColorScheme = Colors.Base;

			StringBuilder aboutMessage = new StringBuilder ();
			aboutMessage.AppendLine (@"A comprehensive sample library for");
			aboutMessage.AppendLine (@"");
			aboutMessage.AppendLine (@"  _______                  _             _   _____       _  ");
			aboutMessage.AppendLine (@" |__   __|                (_)           | | / ____|     (_) ");
			aboutMessage.AppendLine (@"    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _  ");
			aboutMessage.AppendLine (@"    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | | ");
			aboutMessage.AppendLine (@"    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | | ");
			aboutMessage.AppendLine (@"    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_| ");
			aboutMessage.AppendLine (@"");
			aboutMessage.AppendLine (@"https://github.com/gui-cs/Terminal.Gui");

			_menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Application.RequestStop(), null, null, Key.Q | Key.CtrlMask)
				}),
				new MenuBarItem ("_Color Scheme", CreateColorSchemeMenuItems()),
				new MenuBarItem ("Diag_nostics", CreateDiagnosticMenuItems()),
				new MenuBarItem ("_Help", new MenuItem [] {
					new MenuItem ("_gui.cs API Overview", "", () => OpenUrl ("https://gui-cs.github.io/Terminal.Gui/articles/overview.html"), null, null, Key.F1),
					new MenuItem ("gui.cs _README", "", () => OpenUrl ("https://github.com/gui-cs/Terminal.Gui"), null, null, Key.F2),
					new MenuItem ("_About...",
						"About UI Catalog", () =>  MessageBox.Query ("About UI Catalog", aboutMessage.ToString(), "_Ok"), null, null, Key.CtrlMask | Key.A),
				})
			});

			_leftPane = new FrameView ("Categories") {
				X = 0,
				Y = 1, // for menu
				Width = 25,
				Height = Dim.Fill (1),
				CanFocus = false,
				Shortcut = Key.CtrlMask | Key.C
			};
			_leftPane.Title = $"{_leftPane.Title} ({_leftPane.ShortcutTag})";
			_leftPane.ShortcutAction = () => _leftPane.SetFocus ();

			_categories = Scenario.GetAllCategories ();
			_categoryListView = new ListView (_categories) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				CanFocus = true,
			};
			_categoryListView.OpenSelectedItem += (a) => {
				_rightPane.SetFocus ();
			};
			_categoryListView.SelectedItemChanged += CategoryListView_SelectedChanged;
			_leftPane.Add (_categoryListView);

			_rightPane = new FrameView ("Scenarios") {
				X = 25,
				Y = 1, // for menu
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
				CanFocus = true,
				Shortcut = Key.CtrlMask | Key.S
			};
			_rightPane.Title = $"{_rightPane.Title} ({_rightPane.ShortcutTag})";
			_rightPane.ShortcutAction = () => _rightPane.SetFocus ();

			_nameColumnWidth = Scenario.ScenarioMetadata.GetName (_scenarios.OrderByDescending (t => Scenario.ScenarioMetadata.GetName (t).Length).FirstOrDefault ()).Length;

			_scenarioListView = new ListView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (0),
				Height = Dim.Fill (0),
				AllowsMarking = false,
				CanFocus = true,
			};

			_scenarioListView.OpenSelectedItem += _scenarioListView_OpenSelectedItem;
			_rightPane.Add (_scenarioListView);

			_categoryListView.SelectedItem = _categoryListViewItem;
			_categoryListView.OnSelectedChanged ();

			_capslock = new StatusItem (Key.CharMask, "Caps", null);
			_numlock = new StatusItem (Key.CharMask, "Num", null);
			_scrolllock = new StatusItem (Key.CharMask, "Scroll", null);

			_statusBar = new StatusBar () {
				Visible = true,
			};
			_statusBar.Items = new StatusItem [] {
				_capslock,
				_numlock,
				_scrolllock,
				new StatusItem(Key.Q | Key.CtrlMask, "~CTRL-Q~ Quit", () => {
					if (_runningScenario is null){
						// This causes GetScenarioToRun to return null
						_runningScenario = null;
						Application.RequestStop();
					} else {
						_runningScenario.RequestStop();
					}
				}),
				new StatusItem(Key.F10, "~F10~ Hide/Show Status Bar", () => {
					_statusBar.Visible = !_statusBar.Visible;
					_leftPane.Height = Dim.Fill(_statusBar.Visible ? 1 : 0);
					_rightPane.Height = Dim.Fill(_statusBar.Visible ? 1 : 0);
					_top.LayoutSubviews();
					_top.SetChildNeedsDisplay();
				}),
				new StatusItem (Key.CharMask, Application.Driver.GetType ().Name, null),
			};

			SetColorScheme ();
			_top = Application.Top;
			_top.KeyDown += KeyDownHandler;
			_top.Add (_menu);
			_top.Add (_leftPane);
			_top.Add (_rightPane);
			_top.Add (_statusBar);

			void TopHandler () {
				if (_runningScenario != null) {
					_runningScenario = null;
					_isFirstRunning = false;
				}
				if (!_isFirstRunning) {
					_rightPane.SetFocus ();
				}
				_top.Loaded -= TopHandler;
			}
			_top.Loaded += TopHandler;
			// The following code was moved to the TopHandler event
			//  because in the MainLoop.EventsPending (wait)
			//  from the Application.RunLoop with the WindowsDriver
			//  the OnReady event is triggered due the Focus event.
			//  On CursesDriver and NetDriver the focus event won't be triggered
			//  and if it's possible I don't know how to do it.
			//void ReadyHandler ()
			//{
			//	if (!_isFirstRunning) {
			//		_rightPane.SetFocus ();
			//	}
			//	_top.Ready -= ReadyHandler;
			//}
			//_top.Ready += ReadyHandler;

			Application.Run (_top);
			return _runningScenario;
		}

		static List<MenuItem []> CreateDiagnosticMenuItems ()
		{
			List<MenuItem []> menuItems = new List<MenuItem []> ();
			menuItems.Add (CreateDiagnosticFlagsMenuItems ());
			menuItems.Add (new MenuItem [] { null });
			menuItems.Add (CreateSizeStyle ());
			menuItems.Add (CreateDisabledEnabledMouse ());
			menuItems.Add (CreateKeybindings ());
			return menuItems;
		}

		private static MenuItem [] CreateDisabledEnabledMouse ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			var item = new MenuItem ();
			item.Title = "_Disable/Enable Mouse";
			item.Shortcut = Key.CtrlMask | Key.AltMask | (Key)item.Title.ToString ().Substring (1, 1) [0];
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = Application.IsMouseDisabled;
			item.Action += () => {
				item.Checked = Application.IsMouseDisabled = !item.Checked;
			};
			menuItems.Add (item);

			return menuItems.ToArray ();
		}
		private static MenuItem[] CreateKeybindings()
		{

			List<MenuItem> menuItems = new List<MenuItem> ();
			var item = new MenuItem ();
			item.Title = "Keybindings";
			item.Action += () => {
				var dlg = new KeyBindingsDialog ();
				Application.Run (dlg);
			};

			menuItems.Add (null);
			menuItems.Add (item);

			return menuItems.ToArray ();
		}

		static MenuItem [] CreateSizeStyle ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			var item = new MenuItem ();
			item.Title = "_Height As Buffer";
			item.Shortcut = Key.CtrlMask | Key.AltMask | (Key)item.Title.ToString ().Substring (1, 1) [0];
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = Application.HeightAsBuffer;
			item.Action += () => {
				item.Checked = !item.Checked;
				_heightAsBuffer = item.Checked;
				Application.HeightAsBuffer = _heightAsBuffer;
			};
			menuItems.Add (item);

			return menuItems.ToArray ();
		}

		static MenuItem [] CreateDiagnosticFlagsMenuItems ()
		{
			const string OFF = "Diagnostics: _Off";
			const string FRAME_RULER = "Diagnostics: Frame _Ruler";
			const string FRAME_PADDING = "Diagnostics: _Frame Padding";
			var index = 0;

			List<MenuItem> menuItems = new List<MenuItem> ();
			foreach (Enum diag in Enum.GetValues (_diagnosticFlags.GetType ())) {
				var item = new MenuItem ();
				item.Title = GetDiagnosticsTitle (diag);
				item.Shortcut = Key.AltMask + index.ToString () [0];
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
					if (item.Title == t && !item.Checked) {
						_diagnosticFlags &= ~(ConsoleDriver.DiagnosticFlags.FramePadding | ConsoleDriver.DiagnosticFlags.FrameRuler);
						item.Checked = true;
					} else if (item.Title == t && item.Checked) {
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
					_top.SetNeedsDisplay ();
				};
				menuItems.Add (item);
			}
			return menuItems.ToArray ();

			string GetDiagnosticsTitle (Enum diag)
			{
				switch (Enum.GetName (_diagnosticFlags.GetType (), diag)) {
				case "Off":
					return OFF;
				case "FrameRuler":
					return FRAME_RULER;
				case "FramePadding":
					return FRAME_PADDING;
				}
				return "";
			}

			Enum GetDiagnosticsEnumValue (ustring title)
			{
				switch (title.ToString ()) {
				case FRAME_RULER:
					return ConsoleDriver.DiagnosticFlags.FrameRuler;
				case FRAME_PADDING:
					return ConsoleDriver.DiagnosticFlags.FramePadding;
				}
				return null;
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

			//MenuItem CheckedMenuMenuItem (ustring menuItem, Action action, Func<bool> checkFunction)
			//{
			//	var mi = new MenuItem ();
			//	mi.Title = menuItem;
			//	mi.Shortcut = Key.AltMask + index.ToString () [0];
			//	index++;
			//	mi.CheckType |= MenuItemCheckStyle.Checked;
			//	mi.Checked = checkFunction ();
			//	mi.Action = () => {
			//		action?.Invoke ();
			//		mi.Title = menuItem;
			//		mi.Checked = checkFunction ();
			//	};
			//	return mi;
			//}

			//return new MenuItem [] {
			//	CheckedMenuMenuItem ("Use _System Console",
			//		() => {
			//			_useSystemConsole = !_useSystemConsole;
			//		},
			//		() => _useSystemConsole),
			//	CheckedMenuMenuItem ("Diagnostics: _Frame Padding",
			//		() => {
			//			ConsoleDriver.Diagnostics ^= ConsoleDriver.DiagnosticFlags.FramePadding;
			//			_top.SetNeedsDisplay ();
			//		},
			//		() => (ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FramePadding) == ConsoleDriver.DiagnosticFlags.FramePadding),
			//	CheckedMenuMenuItem ("Diagnostics: Frame _Ruler",
			//		() => {
			//			ConsoleDriver.Diagnostics ^= ConsoleDriver.DiagnosticFlags.FrameRuler;
			//			_top.SetNeedsDisplay ();
			//		},
			//		() => (ConsoleDriver.Diagnostics & ConsoleDriver.DiagnosticFlags.FrameRuler) == ConsoleDriver.DiagnosticFlags.FrameRuler),
			//};
		}

		static void SetColorScheme ()
		{
			_leftPane.ColorScheme = _baseColorScheme;
			_rightPane.ColorScheme = _baseColorScheme;
			_top?.SetNeedsDisplay ();
		}

		static ColorScheme _baseColorScheme;
		static MenuItem [] CreateColorSchemeMenuItems ()
		{
			List<MenuItem> menuItems = new List<MenuItem> ();
			foreach (var sc in Colors.ColorSchemes) {
				var item = new MenuItem ();
				item.Title = $"_{sc.Key}";
				item.Shortcut = Key.AltMask | (Key)sc.Key.Substring (0, 1) [0];
				item.CheckType |= MenuItemCheckStyle.Radio;
				item.Checked = sc.Value == _baseColorScheme;
				item.Action += () => {
					_baseColorScheme = sc.Value;
					SetColorScheme ();
					foreach (var menuItem in menuItems) {
						menuItem.Checked = menuItem.Title.Equals ($"_{sc.Key}") && sc.Value == _baseColorScheme;
					}
				};
				menuItems.Add (item);
			}
			return menuItems.ToArray ();
		}

		private static void _scenarioListView_OpenSelectedItem (EventArgs e)
		{
			if (_runningScenario is null) {
				_scenarioListViewItem = _scenarioListView.SelectedItem;
				var source = _scenarioListView.Source as ScenarioListDataSource;
				_runningScenario = (Scenario)Activator.CreateInstance (source.Scenarios [_scenarioListView.SelectedItem]);
				Application.RequestStop ();
			}
		}

		internal class ScenarioListDataSource : IListDataSource {
			private readonly int len;

			public List<Type> Scenarios { get; set; }

			public bool IsMarked (int item) => false;

			public int Count => Scenarios.Count;

			public int Length => len;

			public ScenarioListDataSource (List<Type> itemList)
			{
				Scenarios = itemList;
				len = GetMaxLengthItem ();
			}

			public void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
			{
				container.Move (col, line);
				// Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
				var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenario.ScenarioMetadata.GetName (Scenarios [item]));
				RenderUstr (driver, $"{s}  {Scenario.ScenarioMetadata.GetDescription (Scenarios [item])}", col, line, width, start);
			}

			public void SetMark (int item, bool value)
			{
			}

			int GetMaxLengthItem ()
			{
				if (Scenarios?.Count == 0) {
					return 0;
				}

				int maxLength = 0;
				for (int i = 0; i < Scenarios.Count; i++) {
					var s = String.Format (String.Format ("{{0,{0}}}", -_nameColumnWidth), Scenario.ScenarioMetadata.GetName (Scenarios [i]));
					var sc = $"{s}  {Scenario.ScenarioMetadata.GetDescription (Scenarios [i])}";
					var l = sc.Length;
					if (l > maxLength) {
						maxLength = l;
					}
				}

				return maxLength;
			}

			// A slightly adapted method from: https://github.com/gui-cs/Terminal.Gui/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
			private void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
			{
				int used = 0;
				int index = start;
				while (index < ustr.Length) {
					(var rune, var size) = Utf8.DecodeRune (ustr, index, index - ustr.Length);
					var count = Rune.ColumnWidth (rune);
					if (used + count >= width) break;
					driver.AddRune (rune);
					used += count;
					index += size;
				}

				while (used < width) {
					driver.AddRune (' ');
					used++;
				}
			}

			public IList ToList ()
			{
				return Scenarios;
			}

			public void PreRender (ListView container, int width)
			{
			}
		}

		/// <summary>
		/// When Scenarios are running we need to override the behavior of the Menu 
		/// and Statusbar to enable Scenarios that use those (or related key input)
		/// to not be impacted. Same as for tabs.
		/// </summary>
		/// <param name="ke"></param>
		private static void KeyDownHandler (View.KeyEventEventArgs a)
		{
			//if (a.KeyEvent.Key == Key.Tab || a.KeyEvent.Key == Key.BackTab) {
			//	// BUGBUG: Work around Issue #434 by implementing our own TAB navigation
			//	if (_top.MostFocused == _categoryListView)
			//		_top.SetFocus (_rightPane);
			//	else
			//		_top.SetFocus (_leftPane);
			//}

			if (a.KeyEvent.IsCapslock) {
				_capslock.Title = "Caps: On";
				_statusBar.SetNeedsDisplay ();
			} else {
				_capslock.Title = "Caps: Off";
				_statusBar.SetNeedsDisplay ();
			}

			if (a.KeyEvent.IsNumlock) {
				_numlock.Title = "Num: On";
				_statusBar.SetNeedsDisplay ();
			} else {
				_numlock.Title = "Num: Off";
				_statusBar.SetNeedsDisplay ();
			}

			if (a.KeyEvent.IsScrolllock) {
				_scrolllock.Title = "Scroll: On";
				_statusBar.SetNeedsDisplay ();
			} else {
				_scrolllock.Title = "Scroll: Off";
				_statusBar.SetNeedsDisplay ();
			}
		}

		private static void CategoryListView_SelectedChanged (ListViewItemEventArgs e)
		{
			if (_categoryListViewItem != _categoryListView.SelectedItem) {
				_scenarioListViewItem = 0;
			}
			_categoryListViewItem = _categoryListView.SelectedItem;
			var item = _categories [_categoryListViewItem];
			List<Type> newlist;
			if (_categoryListViewItem == 0) {
				// First category is "All"
				newlist = _scenarios;

			} else {
				newlist = _scenarios.Where (t => Scenario.ScenarioCategory.GetCategories (t).Contains (item)).ToList ();
			}
			_scenarioListView.Source = new ScenarioListDataSource (newlist);
			_scenarioListView.SelectedItem = _scenarioListViewItem;

		}

		private static void OpenUrl (string url)
		{
			try {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					url = url.Replace ("&", "^&");
					Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
					using (var process = new Process {
						StartInfo = new ProcessStartInfo {
							FileName = "xdg-open",
							Arguments = url,
							RedirectStandardError = true,
							RedirectStandardOutput = true,
							CreateNoWindow = true,
							UseShellExecute = false
						}
					}) {
						process.Start ();
					}
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					Process.Start ("open", url);
				}
			} catch {
				throw;
			}
		}
	}
}
