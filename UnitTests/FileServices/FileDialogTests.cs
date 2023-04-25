using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.FileServicesTests {
	public class FileDialogTests {

		readonly ITestOutputHelper output;

		public FileDialogTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void OnLoad_TextBoxIsFocused ()
		{
			var dlg = GetInitializedFileDialog ();

			var tf = dlg.Subviews.FirstOrDefault (t => t.HasFocus);
			Assert.NotNull (tf);
			Assert.IsType<TextField> (tf);
		}

		[Fact, AutoInitShutdown]
		public void DirectTyping_Allowed ()
		{
			var dlg = GetInitializedFileDialog ();
			var tf = dlg.Subviews.OfType<TextField> ().First (t => t.HasFocus);
			tf.ClearAllSelection ();
			tf.CursorPosition = tf.Text.Length;
			Assert.True (tf.HasFocus);

			SendSlash ();

			Assert.Equal (
				new DirectoryInfo (Environment.CurrentDirectory + Path.DirectorySeparatorChar).FullName,
				new DirectoryInfo (dlg.Path + Path.DirectorySeparatorChar).FullName
				);

			// continue typing the rest of the path
			Send ("bob");
			Send ('.', ConsoleKey.OemPeriod, false);
			Send ("csv");

			Assert.True (dlg.Canceled);

			Send ('\n', ConsoleKey.Enter, false);
			Assert.False (dlg.Canceled);
			Assert.Equal ("bob.csv", Path.GetFileName (dlg.Path));
		}

		private void SendSlash ()
		{
			if (Path.DirectorySeparatorChar == '/') {
				Send ('/', ConsoleKey.Separator, false);
			} else {
				Send ('\\', ConsoleKey.Separator, false);
			}
		}

		[Fact, AutoInitShutdown]
		public void DirectTyping_AutoComplete ()
		{
			var dlg = GetInitializedFileDialog ();
			var openIn = Path.Combine (Environment.CurrentDirectory, "zz");

			Directory.CreateDirectory (openIn);

			var expectedDest = Path.Combine (openIn, "xx");
			Directory.CreateDirectory (expectedDest);

			dlg.Path = openIn + Path.DirectorySeparatorChar;

			Send ("x");

			// nothing selected yet
			Assert.True (dlg.Canceled);
			Assert.Equal ("x", Path.GetFileName (dlg.Path));

			// complete auto typing
			Send ('\t', ConsoleKey.Tab, false);

			// but do not close dialog
			Assert.True (dlg.Canceled);
			Assert.EndsWith ("xx" + Path.DirectorySeparatorChar, dlg.Path);

			// press enter again to confirm the dialog
			Send ('\n', ConsoleKey.Enter, false);
			Assert.False (dlg.Canceled);
			Assert.EndsWith ("xx" + Path.DirectorySeparatorChar, dlg.Path);
		}

		[Fact, AutoInitShutdown]
		public void DoNotConfirmSelectionWhenFindFocused ()
		{
			var dlg = GetInitializedFileDialog ();
			var openIn = Path.Combine (Environment.CurrentDirectory, "zz");
			Directory.CreateDirectory (openIn);
			dlg.Path = openIn + Path.DirectorySeparatorChar;

			Send ('f', ConsoleKey.F, false, false, true);

			Assert.IsType<TextField> (dlg.MostFocused);
			var tf = (TextField)dlg.MostFocused;
			Assert.Equal ("Enter Search", tf.Caption);

			// Dialog has not yet been confirmed with a choice
			Assert.True (dlg.Canceled);

			//pressing enter while search focused should not confirm path
			Send ('\n', ConsoleKey.Enter, false);

			Assert.True (dlg.Canceled);

			// tabbing out of search 
			Send ('\t', ConsoleKey.Tab, false);

			//should allow enter to confirm path
			Send ('\n', ConsoleKey.Enter, false);

			// Dialog has not yet been confirmed with a choice
			Assert.False (dlg.Canceled);
		}

		[Theory, AutoInitShutdown]
		[InlineData(true,true)]
		[InlineData(true,false)]
		[InlineData(false,true)]
		[InlineData(false,false)]
		public void PickDirectory_DirectTyping (bool openModeMixed, bool multiple)
		{
			var dlg = GetDialog();
			dlg.OpenMode = openModeMixed ? OpenMode.Mixed : OpenMode.Directory;
			dlg.AllowsMultipleSelection = multiple;

			// whe first opening the text field will have select all on
			// so to add to current path user must press End or right
			Send ('>', ConsoleKey.RightArrow, false);

			Send("subfolder");

			// Dialog has not yet been confirmed with a choice
			Assert.True(dlg.Canceled);

			// Now it has
			Send ('\n', ConsoleKey.Enter, false);
			Assert.False(dlg.Canceled);
			AssertIsTheSubfolder(dlg.Path);
		}

		[Theory, AutoInitShutdown]
		[InlineData(true,true)]
		[InlineData(true,false)]
		[InlineData(false,true)]
		[InlineData(false,false)]
		public void PickDirectory_ArrowNavigation (bool openModeMixed, bool multiple)
		{
			var dlg = GetDialog();
			dlg.OpenMode = openModeMixed ? OpenMode.Mixed : OpenMode.Directory;
			dlg.AllowsMultipleSelection = multiple;

			Assert.IsType<TextField>(dlg.MostFocused);
			Send ('v', ConsoleKey.DownArrow, false);
			Assert.IsType<TableView>(dlg.MostFocused);

			// Should be selecting ..
			Send ('v', ConsoleKey.DownArrow, false);

			// Down to the directory
			Assert.True(dlg.Canceled);
			// Alt+O to open (enter would just navigate into the child dir)
			Send ('o', ConsoleKey.O, false,true);
			Assert.False(dlg.Canceled);

			AssertIsTheSubfolder(dlg.Path);
		}

		[Theory, AutoInitShutdown]
		[InlineData(true)]
		[InlineData(false)]
		public void MultiSelectDirectory_CannotToggleDotDot (bool acceptWithEnter)
		{
			var dlg = GetDialog();
			dlg.OpenMode = OpenMode.Directory;
			dlg.AllowsMultipleSelection = true;
			IReadOnlyCollection<string> eventMultiSelected = null;
			dlg.FilesSelected += (s,e)=>
			{
				eventMultiSelected  = e.Dialog.MultiSelected;
			};

			Assert.IsType<TextField>(dlg.MostFocused);
			Send ('v', ConsoleKey.DownArrow, false);
			Assert.IsType<TableView>(dlg.MostFocused);

			// Try to toggle '..'
			Send (' ', ConsoleKey.Spacebar, false);
			Send ('v', ConsoleKey.DownArrow, false);
			// Toggle subfolder
			Send (' ', ConsoleKey.Spacebar, false);

			Assert.True(dlg.Canceled);

			if(acceptWithEnter)
			{
				Send ('\n', ConsoleKey.Enter);
			}
			else
			{
				Send ('o', ConsoleKey.O,false,true);
			}
			Assert.False(dlg.Canceled);

			Assert.Multiple(
				()=>{
					// Only the subfolder should be selected
					Assert.Equal(1,dlg.MultiSelected.Count);
					AssertIsTheSubfolder(dlg.Path);
					AssertIsTheSubfolder(dlg.MultiSelected.Single());
				},
				()=>{
					// Event should also agree with the final state
					Assert.NotNull(eventMultiSelected);
					Assert.Equal(1,eventMultiSelected.Count);
					AssertIsTheSubfolder(eventMultiSelected.Single());
				}
			);
		}
		
		[Fact, AutoInitShutdown]
		public void DotDot_MovesToRoot_ThenPressBack ()
		{
			var dlg = GetDialog();
			dlg.OpenMode = OpenMode.Directory;
			dlg.AllowsMultipleSelection = true;
			bool selected = false;
			dlg.FilesSelected += (s,e)=>
			{
				selected = true;
			};

			AssertIsTheStartingDirectory(dlg.Path);

			Assert.IsType<TextField>(dlg.MostFocused);
			Send ('v', ConsoleKey.DownArrow, false);
			Assert.IsType<TableView>(dlg.MostFocused);
			
			// ".." should be the first thing selected
			// ".." should not mess with the displayed path
			AssertIsTheStartingDirectory(dlg.Path);

			// Accept navigation up a directory
			Send ('\n', ConsoleKey.Enter);

			AssertIsTheRootDirectory(dlg.Path);
			
			Assert.True(dlg.Canceled);
			Assert.False(selected);

			// Now press the back button (in table view)
			Send ('<', ConsoleKey.Backspace);

			// Should move us back to the root
			AssertIsTheStartingDirectory(dlg.Path);

			Assert.True(dlg.Canceled);
			Assert.False(selected);
		}

		[Fact, AutoInitShutdown]
		public void MultiSelectDirectory_EnterOpensFolder ()
		{
			var dlg = GetDialog();
			dlg.OpenMode = OpenMode.Directory;
			dlg.AllowsMultipleSelection = true;
			IReadOnlyCollection<string> eventMultiSelected = null;
			dlg.FilesSelected += (s,e)=>
			{
				eventMultiSelected  = e.Dialog.MultiSelected;
			};

			Assert.IsType<TextField>(dlg.MostFocused);
			Send ('v', ConsoleKey.DownArrow, false);
			Assert.IsType<TableView>(dlg.MostFocused);
			// Move selection to subfolder
			Send ('v', ConsoleKey.DownArrow, false);

			Send ('\n', ConsoleKey.Enter);

			// Path should update to the newly opened folder
			AssertIsTheSubfolder(dlg.Path);

			// No selection will have been confirmed
			Assert.True(dlg.Canceled);
			Assert.Empty(dlg.MultiSelected);
			Assert.Null(eventMultiSelected);
		}

		[Theory, AutoInitShutdown]
		[InlineData(true)]
		[InlineData(false)]
		public void MultiSelectDirectory_CanToggleThenAccept (bool acceptWithEnter)
		{
			var dlg = GetDialog();
			dlg.OpenMode = OpenMode.Directory;
			dlg.AllowsMultipleSelection = true;
			IReadOnlyCollection<string> eventMultiSelected = null;
			dlg.FilesSelected += (s,e)=>
			{
				eventMultiSelected  = e.Dialog.MultiSelected;
			};

			Assert.IsType<TextField>(dlg.MostFocused);
			Send ('v', ConsoleKey.DownArrow, false);
			Assert.IsType<TableView>(dlg.MostFocused);
			// Move selection to subfolder
			Send ('v', ConsoleKey.DownArrow, false);
			// Toggle subfolder
			Send (' ', ConsoleKey.Spacebar, false);

			Assert.True(dlg.Canceled);

			if(acceptWithEnter)
			{
				Send ('\n', ConsoleKey.Enter);
			}
			else
			{
				Send ('o', ConsoleKey.O,false,true);
			}
			Assert.False(dlg.Canceled);

			Assert.Multiple(
				()=>{
					// Only the subfolder should be selected
					Assert.Equal(1,dlg.MultiSelected.Count);
					AssertIsTheSubfolder(dlg.Path);
					AssertIsTheSubfolder(dlg.MultiSelected.Single());
				},
				()=>{
					// Event should also agree with the final state
					Assert.NotNull(eventMultiSelected);
					Assert.Equal(1,eventMultiSelected.Count);
					AssertIsTheSubfolder(eventMultiSelected.Single());
				}
			);
		}

		private void AssertIsTheStartingDirectory (string path)
		{
			if(IsWindows())
			{
				Assert.Equal (@"c:\demo\",path);
			}
			else
			{
				Assert.Equal ("/demo/",path);
			}
		}

		private void AssertIsTheRootDirectory (string path)
		{
			if(IsWindows())
			{
				Assert.Equal (@"c:\",path);
			}
			else
			{
				Assert.Equal ("/",path);
			}
		}

		private void AssertIsTheSubfolder (string path)
		{
			if(IsWindows())
			{
				Assert.Equal (@"c:\demo\subfolder",path);
			}
			else
			{
				Assert.Equal ("/demo/subfolder",path);
			}
		}

		[Fact, AutoInitShutdown]
		public void TestDirectoryContents_Linux ()
		{
			if (IsWindows()) {
				return;
			}
			var fd = GetLinuxDialog();
			fd.Title = string.Empty;

			fd.Style.Culture = new CultureInfo("en-US");

			fd.Redraw (fd.Bounds);
			
			string expected =
			@"
 ┌──────────────────────────────────────────────────────────────────┐
 │/demo/                                                            │
 │[▲]                                                               │
 │┌────────────┬──────────┬──────────────────────────────┬─────────┐│
 ││Filename (▲)│Size      │Modified                      │Type     ││
 │├────────────┼──────────┼──────────────────────────────┼─────────┤│
 ││..          │          │                              │dir      ││
 ││\subfolder  │          │2002-01-01T22:42:10           │dir      ││
 ││image.gif   │4.00 bytes│2002-01-01T22:42:10           │.gif     ││
 ││jQuery.js   │7.00 bytes│2001-01-01T11:44:42           │.js      ││
 │                                                                  │
 │                                                                  │
 │                                                                  │
 │[ ►► ] Enter Search                            [ Cancel ] [ Ok ]  │
 └──────────────────────────────────────────────────────────────────┘
";
			TestHelpers.AssertDriverContentsAre (expected, output, true);
		}

		[Fact, AutoInitShutdown]
		public void TestDirectoryContents_Windows ()
		{
			if (!IsWindows()) {
				return;
			}

			var fd = GetWindowsDialog();
			fd.Title = string.Empty;

			fd.Style.Culture = new CultureInfo("en-US");

			fd.Redraw (fd.Bounds);


			string expected =
			@"
┌──────────────────────────────────────────────────────────────────┐
│c:\demo\                                                          │
│[▲]                                                               │
│┌────────────┬──────────┬──────────────────────────────┬─────────┐│
││Filename (▲)│Size      │Modified                      │Type     ││
│├────────────┼──────────┼──────────────────────────────┼─────────┤│
││..          │          │                              │dir      ││
││\subfolder  │          │2002-01-01T22:42:10           │dir      ││
││image.gif   │4.00 bytes│2002-01-01T22:42:10           │.gif     ││
││jQuery.js   │7.00 bytes│2001-01-01T11:44:42           │.js      ││
│                                                                  │
│                                                                  │
│                                                                  │
│[ ►► ] Enter Search                            [ Cancel ] [ Ok ]  │
└──────────────────────────────────────────────────────────────────┘
";
			TestHelpers.AssertDriverContentsAre (expected, output, true);
		}

		[Theory, AutoInitShutdown]
		[InlineData (true)]
		[InlineData (false)]
		public void CancelSelection (bool cancel)
		{
			var dlg = GetInitializedFileDialog ();
			var openIn = Path.Combine (Environment.CurrentDirectory, "zz");
			Directory.CreateDirectory (openIn);
			dlg.Path = openIn + Path.DirectorySeparatorChar;

			dlg.FilesSelected += (s, e) => e.Cancel = cancel;

			//pressing enter will complete the current selection
			// unless the event cancels the confirm
			Send ('\n', ConsoleKey.Enter, false);

			Assert.Equal (cancel, dlg.Canceled);
		}

		private void Send (char ch, ConsoleKey ck, bool shift = false, bool alt = false, bool control = false)
		{
			Application.Driver.SendKeys (ch, ck, shift, alt, control);
		}
		private void Send (string chars)
		{
			foreach (var ch in chars) {
				Application.Driver.SendKeys (ch, ConsoleKey.NoName, false, false, false);
			}
		}
		/*
				[Fact, AutoInitShutdown]
				public void Autocomplete_NoSuggestion_WhenTextMatchesExactly ()
				{
					var tb = new TextFieldWithAppendAutocomplete ();
					ForceFocus (tb);

					tb.Text = "/bob/fish";
					tb.CursorPosition = tb.Text.Length;
					tb.GenerateSuggestions (null, "fish", "fishes");

					// should not report success for autocompletion because we already have that exact
					// string
					Assert.False (tb.AcceptSelectionIfAny ());
				}

				[Fact, AutoInitShutdown]
				public void Autocomplete_AcceptSuggstion ()
				{
					var tb = new TextFieldWithAppendAutocomplete ();
					ForceFocus (tb);

					tb.Text = @"/bob/fi";
					tb.CursorPosition = tb.Text.Length;
					tb.GenerateSuggestions (null, "fish", "fishes");

					Assert.True (tb.AcceptSelectionIfAny ());
					Assert.Equal (@"/bob/fish", tb.Text);
				}*/

		private bool IsWindows()
		{
			return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform (System.Runtime.InteropServices.OSPlatform.Windows);
		}

		private FileDialog GetDialog()
		{
			return IsWindows() ? GetWindowsDialog() : GetLinuxDialog();			
		}

		private FileDialog GetWindowsDialog()
		{
			// Arrange
			var fileSystem = new MockFileSystem (new Dictionary<string, MockFileData> (), @"c:\");
			fileSystem.MockTime (() => new DateTime (2010, 01, 01, 11, 12, 43));

			fileSystem.AddFile (@"c:\myfile.txt", new MockFileData ("Testing is meh.") { LastWriteTime = new DateTime (2001, 01, 01, 11, 12, 11) });
			fileSystem.AddFile (@"c:\demo\jQuery.js", new MockFileData ("some js") { LastWriteTime = new DateTime (2001, 01, 01, 11, 44, 42) });
			fileSystem.AddFile (@"c:\demo\image.gif", new MockFileData (new byte [] { 0x12, 0x34, 0x56, 0xd2 }) { LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10) });

			var m = (MockDirectoryInfo)fileSystem.DirectoryInfo.New (@"c:\demo\subfolder");
			m.Create ();
			m.LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10);

			fileSystem.AddFile (@"c:\demo\subfolder\image2.gif", new MockFileData (new byte [] { 0x12, 0x34, 0x56, 0xd2 }) { LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10) });

			var fd = new FileDialog (fileSystem) {
				Height = 15
			};
			fd.Path = @"c:\demo\";
			Begin (fd);
			return fd;
		}

		private FileDialog GetLinuxDialog()
		{
			// Arrange
			var fileSystem = new MockFileSystem (new Dictionary<string, MockFileData> (), "/");
			fileSystem.MockTime (() => new DateTime (2010, 01, 01, 11, 12, 43));

			fileSystem.AddFile (@"/myfile.txt", new MockFileData ("Testing is meh.") { LastWriteTime = new DateTime (2001, 01, 01, 11, 12, 11) });
			fileSystem.AddFile (@"/demo/jQuery.js", new MockFileData ("some js") { LastWriteTime = new DateTime (2001, 01, 01, 11, 44, 42) });
			fileSystem.AddFile (@"/demo/image.gif", new MockFileData (new byte [] { 0x12, 0x34, 0x56, 0xd2 }) { LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10) });

			var m = (MockDirectoryInfo)fileSystem.DirectoryInfo.New (@"/demo/subfolder");
			m.Create ();
			m.LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10);

			fileSystem.AddFile (@"/demo/subfolder/image2.gif", new MockFileData (new byte [] { 0x12, 0x34, 0x56, 0xd2 }) { LastWriteTime = new DateTime (2002, 01, 01, 22, 42, 10) });

			var fd = new FileDialog (fileSystem) {
				Height = 15
			};
			fd.Path = @"/demo/";
			Begin (fd);
			return fd;
		}
		private FileDialog GetInitializedFileDialog ()
		{
			var dlg = new FileDialog ();
			Begin (dlg);

			return dlg;
		}
		private void Begin (FileDialog dlg)
		{
			dlg.BeginInit ();
			dlg.EndInit ();
			Application.Begin (dlg);
		}
	}
}