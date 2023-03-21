using NStack;
using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Terminal.Gui;
using Terminal.Gui.Configuration;
using static Terminal.Gui.Configuration.ConfigurationManager;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Configuration Editor", Description: "Edits Terminal.Gui Config Files.")]
	[ScenarioCategory ("TabView"), ScenarioCategory ("Colors"), ScenarioCategory ("Files and IO"), ScenarioCategory ("TextView")]
	public class ConfigurationEditor : Scenario {
		TileView _tileView;
		StatusItem _lenStatusItem;

		private static ColorScheme _editorColorScheme = new ColorScheme () {
			Normal = new Attribute (Color.Red, Color.White),
			Focus = new Attribute (Color.Red, Color.Black),
			HotFocus = new Attribute (Color.BrightRed, Color.Black),
			HotNormal = new Attribute (Color.Magenta, Color.White)
		};

		[SerializableConfigurationProperty (Scope = typeof (AppScope))]
		public static ColorScheme EditorColorScheme {
			get => _editorColorScheme;
			set {
				_editorColorScheme = value;
				_editorColorSchemeChanged?.Invoke ();
			}
		}

		private static Action _editorColorSchemeChanged;

		// Don't create a Window, just return the top-level view
		public override void Init ()
		{
			Application.Init ();
			ConfigurationManager.Themes.Theme = Theme;
			ConfigurationManager.Apply ();
			Application.Top.ColorScheme = Colors.ColorSchemes [TopLevelColorScheme];
		}

		public override void Setup ()
		{
			_tileView = new TileView (0) {
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
				Orientation = Terminal.Gui.Graphs.Orientation.Vertical,
				Border = new Border () { BorderStyle = BorderStyle.Single }
			};

			Application.Top.Add (_tileView);

			_lenStatusItem = new StatusItem (Key.CharMask, "Len: ", null);
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} Quit", () => Quit()),
				new StatusItem(Key.F5, "~F5~ Reload", () => Reload()),
				new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => Save()),
				_lenStatusItem,
			});

			Application.Top.Add (statusBar);

			Open ();

			ConfigurationEditor._editorColorSchemeChanged += () => {
				foreach (var t in _tileView.Tiles) {
					t.ContentView.ColorScheme = ConfigurationEditor.EditorColorScheme;
					t.ContentView.SetNeedsDisplay ();
				};
			};

			ConfigurationEditor._editorColorSchemeChanged.Invoke ();

		}

		private class ConfigTextView : TextView {
			internal TileView.Tile Tile { get; set; }
			internal FileInfo FileInfo { get; set; }

			internal ConfigTextView ()
			{
				ContentsChanged += (s,obj) => {
					if (IsDirty) {
						if (!Tile.Title.EndsWith ('*')) {
							Tile.Title += '*';
						} else {
							Tile.Title = Tile.Title.TrimEnd ('*');
						}
					}

				};
			}

			internal void Read ()
			{
				Assembly assembly = null;
				if (FileInfo.FullName.Contains ("[Terminal.Gui]")) {
					// Library resources
					assembly = typeof (ConfigurationManager).Assembly;
				} else if (FileInfo.FullName.Contains ("[UICatalog]")) {
					assembly = Assembly.GetEntryAssembly ();
				}
				if (assembly != null) {
					string name = assembly
						.GetManifestResourceNames ()
						.FirstOrDefault (x => x.EndsWith ("config.json"));
					using Stream stream = assembly.GetManifestResourceStream (name);
					using StreamReader reader = new StreamReader (stream);
					Text = reader.ReadToEnd ();
					ReadOnly = true;
					Enabled = true;
					return;
				}

				if (!FileInfo.Exists) {
					// Create empty config file
					Text = ConfigurationManager.GetEmptyJson ();
				} else {
					Text = File.ReadAllText (FileInfo.FullName);
				}
				Tile.Title = Tile.Title.TrimEnd ('*');
			}

			internal void Save ()
			{
				if (!Directory.Exists (FileInfo.DirectoryName)) {
					// Create dir
					Directory.CreateDirectory (FileInfo.DirectoryName!);
				}
				using var writer = File.CreateText (FileInfo.FullName);
				writer.Write (Text.ToString ());
				writer.Close ();
				Tile.Title = Tile.Title.TrimEnd ('*');
				//IsDirty = false;
			}
		}

		private void Open ()
		{
			var subMenu = new MenuBarItem () {
				Title = "_View",
			};

			foreach (var configFile in ConfigurationManager.Settings.Sources) {

				var homeDir = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}";
				FileInfo fileInfo = new FileInfo (configFile.Replace ("~", homeDir));

				var tile = _tileView.InsertTile (_tileView.Tiles.Count);
				tile.Title = configFile.StartsWith ("resource://") ? fileInfo.Name : configFile;

				var textView = new ConfigTextView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill (),
					FileInfo = fileInfo,
					Tile = tile
				};

				tile.ContentView.Add (textView);

				textView.Read ();

				textView.Enter += (s,e) => {
					_lenStatusItem.Title = $"Len:{textView.Text.Length}";
				};

				//var mi = new MenuItem () {
				//	Title = tile.Title,
				//	CheckType = MenuItemCheckStyle.Checked,
				//	Checked = true,
				//};
				//mi.Action += () => {
				//	mi.Checked =! mi.Checked;
				//	_tileView.SetNeedsDisplay ();
				//};

				//subMenu.Children = subMenu.Children.Append (mi).ToArray ();
			}

			//var menu = new MenuBar (new MenuBarItem [] { subMenu });
			//Application.Top.Add (menu);

		}

		private void Reload ()
		{
			if (_tileView.MostFocused is ConfigTextView editor) {
				editor.Read ();
			}
		}

		public void Save ()
		{
			if (_tileView.MostFocused is ConfigTextView editor) {
				editor.Save ();
			}
		}

		private void Quit ()
		{
			foreach (var tile in _tileView.Tiles) {
				ConfigTextView editor = tile.ContentView.Subviews [0] as ConfigTextView;
				if (editor.IsDirty) {
					int result = MessageBox.Query ("Save Changes", $"Save changes to {editor.FileInfo.FullName}", "Yes", "No", "Cancel");
					if (result == -1 || result == 2) {
						// user cancelled
					}
					if (result == 0) {
						editor.Save ();
					}
				}

			}

			Application.RequestStop ();
		}
	}
}
