using System;
using System.Collections.Generic;
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

		[Fact, AutoInitShutdown]
		public void TestDirectoryContents_Linux ()
		{
			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform (System.Runtime.InteropServices.OSPlatform.Windows)) {
				// Cannot run test except on linux :( 
				// See: https://github.com/TestableIO/System.IO.Abstractions/issues/800
				return;
			}

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
			fd.Title = string.Empty;

			fd.Redraw (fd.Bounds);

			fd.Style.DateFormat = "yyyy-MM-dd hh:mm:ss";

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
			if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform (System.Runtime.InteropServices.OSPlatform.Windows)) {
				// Can only run this test on windows :( 
				// See: https://github.com/TestableIO/System.IO.Abstractions/issues/800
				return;
			}
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
			fd.Title = string.Empty;

			fd.Redraw (fd.Bounds);

			fd.Style.DateFormat = "yyyy-MM-dd hh:mm:ss";

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