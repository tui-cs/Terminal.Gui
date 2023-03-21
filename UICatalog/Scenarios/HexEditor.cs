using System;
using System.IO;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "HexEditor", Description: "A binary (hex) editor using the HexView control.")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Top Level Windows")]
	[ScenarioCategory ("Files and IO")]
	public class HexEditor : Scenario {
		private string _fileName = "demo.bin";
		private HexView _hexView;
		private bool _saved = true;
		private MenuItem miAllowEdits;
		private StatusItem siPositionChanged;
		private StatusBar statusBar;

		public override void Setup ()
		{
			Win.Title = this.GetName () + "-" + _fileName ?? "Untitled";

			CreateDemoFile (_fileName);
			//CreateUnicodeDemoFile (_fileName);

			_hexView = new HexView (LoadFile ()) {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			_hexView.Edited += _hexView_Edited;
			_hexView.PositionChanged += _hexView_PositionChanged;
			Win.Add (_hexView);

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					null,
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => Copy()),
					new MenuItem ("C_ut", "", () => Cut()),
					new MenuItem ("_Paste", "", () => Paste())
				}),
				new MenuBarItem ("_Options", new MenuItem [] {
					miAllowEdits = new MenuItem ("_AllowEdits", "", () => ToggleAllowEdits ()){Checked = _hexView.AllowEdits, CheckType = MenuItemCheckStyle.Checked}
				})
			});
			Application.Top.Add (menu);

			statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ Open", () => Open()),
				new StatusItem(Key.F3, "~F3~ Save", () => Save()),
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
				siPositionChanged = new StatusItem(Key.Null,
					$"Position: {_hexView.Position} Line: {_hexView.CursorPosition.Y} Col: {_hexView.CursorPosition.X} Line length: {_hexView.BytesPerLine}", () => {})
			});
			Application.Top.Add (statusBar);
		}

		private void _hexView_PositionChanged (object sender, HexViewEventArgs obj)
		{
			siPositionChanged.Title = $"Position: {obj.Position} Line: {obj.CursorPosition.Y} Col: {obj.CursorPosition.X} Line length: {obj.BytesPerLine}";
			statusBar.SetNeedsDisplay ();
		}

		private void ToggleAllowEdits ()
		{
			_hexView.AllowEdits = (bool)(miAllowEdits.Checked = !miAllowEdits.Checked);
		}

		private void _hexView_Edited (object sender, HexViewEditEventArgs e)
		{
			_saved = false;
		}

		private void New ()
		{
			_fileName = null;
			_hexView.Source = LoadFile ();
		}

		private Stream LoadFile ()
		{
			MemoryStream stream = new MemoryStream ();
			if (!_saved && _hexView != null && _hexView.Edits.Count > 0) {
				if (MessageBox.ErrorQuery ("Save", "The changes were not saved. Want to open without saving?", "Yes", "No") == 1)
					return _hexView.Source;
				_hexView.DiscardEdits ();
				_saved = true;
			}

			if (_fileName != null) {
				var bin = System.IO.File.ReadAllBytes (_fileName);
				stream.Write (bin);
				Win.Title = this.GetName () + "-" + _fileName;
				_saved = true;
			} else {
				Win.Title = this.GetName () + "-" + (_fileName ?? "Untitled");
			}
			return stream;
		}

		private void Paste ()
		{
			MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Cut ()
		{
			MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Copy ()
		{
			MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Open ()
		{
			var d = new OpenDialog ("Open", "Open a file") { AllowsMultipleSelection = false };
			Application.Run (d);

			if (!d.Canceled) {
				_fileName = d.FilePaths [0];
				_hexView.Source = LoadFile ();
				_hexView.DisplayStart = 0;
			}
		}

		private void Save ()
		{
			if (_fileName != null) {
				using (FileStream fs = new FileStream (_fileName, FileMode.OpenOrCreate)) {
					_hexView.ApplyEdits (fs);
					//_hexView.Source.Position = 0;
					//_hexView.Source.CopyTo (fs);
					//fs.Flush ();
				}
				_saved = true;
			} else {
				_hexView.ApplyEdits ();
			}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void CreateDemoFile (string fileName)
		{
			var sb = new StringBuilder ();
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			var sw = System.IO.File.CreateText (fileName);
			sw.Write (sb.ToString ());
			sw.Close ();
		}

		private void CreateUnicodeDemoFile (string fileName)
		{
			var sb = new StringBuilder ();
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			byte [] buffer = Encoding.Unicode.GetBytes (sb.ToString ());
			MemoryStream ms = new MemoryStream (buffer);
			FileStream file = new FileStream (fileName, FileMode.Create, FileAccess.Write);
			ms.WriteTo (file);
			file.Close ();
			ms.Close ();
		}
	}
}
