using System.Collections;
using System.Drawing;
using System.Globalization;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

public class FileDialogTests : TestsAllDrivers
{
    private readonly TextWriter _out;

    public FileDialogTests (ITestOutputHelper outputHelper)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _out = new TestOutputWriter (outputHelper);
    }

    private MockFileSystem CreateExampleFileSystem ()
    {
        // Optional: use Ordinal to simulate Linux-style case sensitivity
        var mockFileSystem = new MockFileSystem (new Dictionary<string, MockFileData> ());

        string testDir = mockFileSystem.Path.Combine ("test-dir");
        string subDir = mockFileSystem.Path.Combine (testDir, "sub-dir");
        var logsDir = "logs";
        var emptyDir = "empty-dir";

        // Add files
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (testDir, "file1.txt"), new MockFileData ("Hello, this is file 1."));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (testDir, "file2.txt"), new MockFileData ("Hello, this is file 2."));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (subDir, "nested-file.txt"), new MockFileData ("This is a nested file."));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (logsDir, "log1.log"), new MockFileData ("Log entry 1"));
        mockFileSystem.AddFile (mockFileSystem.Path.Combine (logsDir, "log2.log"), new MockFileData ("Log entry 2"));

        // Create an empty directory
        mockFileSystem.AddDirectory (emptyDir);

        return mockFileSystem;
    }

    private IRunnable NewSaveDialog (out SaveDialog sd) => NewSaveDialog (out sd, out _);

    private IRunnable NewSaveDialog (out SaveDialog sd, out MockFileSystem fs)
    {
        fs = CreateExampleFileSystem ();
        sd = new SaveDialog (fs);

        return sd;
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void CancelFileDialog_QuitKey_Quits (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd), 100, 20, d, _out)
                                    .ScreenShot ("Save dialog", _out)
                                    .KeyDown (Application.GetDefaultKey (Command.Quit))
                                    .AssertTrue (sd!.Canceled);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void CancelFileDialog_UsingCancelButton_TabThenEnter (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .Focus<Button> (b => b.Text == Strings.btnCancel)
                                    .AssertTrue (sd!.Canceled)
                                    .KeyDown (Key.Enter);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void CancelFileDialog_UsingCancelButton_LeftClickButton (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .LeftClick<Button> (b => b.Text == Strings.btnCancel)
                                    .AssertTrue (sd!.Canceled);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void CancelFileDialog_UsingCancelButton_AltC (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd), 100, 20, d, _out)
                                    .ScreenShot ("Save dialog", _out)
                                    .KeyDown (Key.C.WithAlt)
                                    .AssertTrue (sd!.Canceled);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_UsingOkButton_Enter (string d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out fs), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .LeftClick<Button> (b => b.Text == Strings.cmdSave)
                                    .AssertFalse (sd!.Canceled)
                                    .AssertEqual (GetFileSystemRoot (fs!), sd!.FileName);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_UsingOkButton_AltS (string d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out fs), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .KeyDown (Key.S.WithAlt)
                                    .AssertFalse (sd!.Canceled)
                                    .AssertEqual (GetFileSystemRoot (fs!), sd!.FileName);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_UsingOkButton_TabEnter (string d)
    {
        SaveDialog? sd = null;
        MockFileSystem? fs = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out fs), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .Focus<Button> (b => b.Text == Strings.cmdSave)
                                    .KeyDown (Key.Enter)
                                    .AssertFalse (sd!.Canceled)
                                    .AssertEqual (GetFileSystemRoot (fs!), sd!.FileName);
    }

    private string GetFileSystemRoot (IFileSystem fs) => OperatingSystem.IsWindows () ? $@"C:{fs.Path.DirectorySeparatorChar}" : "/";

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_PressingPopTree_ShouldNotChangeCancel (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out MockFileSystem _), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .AssertTrue (sd!.Canceled)
                                    .Focus<Button> (b => b.Text == "►_Tree")
                                    .KeyDown (Key.Enter)
                                    .ScreenShot ("After pop tree", _out)
                                    .AssertTrue (sd!.Canceled);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_PopTree_AndNavigate (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out MockFileSystem _), 100, 20, d)
                                    .ScreenShot ("Save dialog", _out)
                                    .AssertTrue (sd!.Canceled)
                                    .LeftClick<Button> (b => b.Text == "►_Tree")
                                    .ScreenShot ("After pop tree", _out)
                                    .Focus<TreeView<IFileSystemInfo>> (_ => true)
                                    .KeyDown (Key.CursorRight)
                                    .ScreenShot ("After expand tree", _out)
                                    .KeyDown (Key.CursorDown)
                                    .ScreenShot ("After navigate down in tree", _out)
                                    .KeyDown (Key.Enter)
                                    .AssertFalse (sd!.Canceled)
                                    .AssertContains ("empty-dir", sd!.FileName);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_PopTree_AndNavigate_PreserveFilenameOnDirectoryChanges_True (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out MockFileSystem _), 100, 20, d)
                                    .Then (_ => sd!.Style.PreserveFilenameOnDirectoryChanges = true)
                                    .ScreenShot ("Save dialog", _out)
                                    .AssertTrue (sd!.Canceled)
                                    .Focus<TextField> (_ => true)

                                    // Clear selection by pressing right in 'file path' text box
                                    .KeyDown (Key.CursorRight)
                                    .AssertIsType<TextField> (sd!.Focused)

                                    // Type a filename into the dialog
                                    .KeyDown (Key.H)
                                    .KeyDown (Key.E)
                                    .KeyDown (Key.L)
                                    .KeyDown (Key.L)
                                    .KeyDown (Key.O)
                                    .ScreenShot ("After typing filename 'hello'", _out)
                                    .AssertEndsWith ("hello", sd!.Path)
                                    .LeftClick<Button> (b => b.Text == "►_Tree")
                                    .ScreenShot ("After pop tree", _out)
                                    .Focus<TreeView<IFileSystemInfo>> (_ => true)
                                    .KeyDown (Key.CursorRight)
                                    .ScreenShot ("After expand tree", _out)

                                    // Because of PreserveFilenameOnDirectoryChanges we should select the new dir but keep the filename
                                    .AssertEndsWith ("hello", sd!.Path)
                                    .KeyDown (Key.CursorDown)
                                    .ScreenShot ("After navigate down in tree", _out)

                                    // Because of PreserveFilenameOnDirectoryChanges we should select the new dir but keep the filename
                                    .AssertContains ("empty-dir", sd!.Path)
                                    .AssertEndsWith ("hello", sd!.Path)
                                    .KeyDown (Key.Enter)
                                    .AssertFalse (sd!.Canceled)
                                    .AssertContains ("empty-dir", sd!.FileName);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_PopTree_AndNavigate_PreserveFilenameOnDirectoryChanges_False (string d)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out MockFileSystem _), 100, 20, d)
                                    .Then (_ => sd!.Style.PreserveFilenameOnDirectoryChanges = false)
                                    .ScreenShot ("Save dialog", _out)
                                    .AssertTrue (sd!.Canceled)
                                    .Focus<TextField> (_ => true)

                                    // Clear selection by pressing right in 'file path' text box
                                    .KeyDown (Key.CursorRight)
                                    .AssertIsType<TextField> (sd!.Focused)

                                    // Type a filename into the dialog
                                    .KeyDown (Key.H)
                                    .KeyDown (Key.E)
                                    .KeyDown (Key.L)
                                    .KeyDown (Key.L)
                                    .KeyDown (Key.O)
                                    .ScreenShot ("After typing filename 'hello'", _out)
                                    .AssertEndsWith ("hello", sd!.Path)
                                    .LeftClick<Button> (b => b.Text == "►_Tree")
                                    .ScreenShot ("After pop tree", _out)
                                    .Focus<TreeView<IFileSystemInfo>> (_ => true)
                                    .KeyDown (Key.CursorRight)
                                    .ScreenShot ("After expand tree", _out)
                                    .KeyDown (Key.CursorDown)
                                    .ScreenShot ("After navigate down in tree", _out)

                                    // PreserveFilenameOnDirectoryChanges is false so just select new path
                                    .AssertEndsWith ("empty-dir", sd!.Path)
                                    .AssertDoesNotContain ("hello", sd!.Path)
                                    .KeyDown (Key.Enter)
                                    .AssertFalse (sd!.Canceled)
                                    .AssertContains ("empty-dir", sd!.FileName);
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4950
    ///     OpenFileDialog only closes after clicking Cancel or OK three times.
    ///     The first mouse-press triggers a layout pass that repositions the button,
    ///     so the subsequent mouse-release misses it.
    /// </summary>
    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void FileDialog_CancelButton_ClickDoesNotMoveButton (string d)
    {
        // Copilot
        OpenDialog? od = null;
        Button? cancelBtn = null;

        using AppTestHelper c = With.A (() =>
                                        {
                                            od = new OpenDialog ();

                                            return od;
                                        },
                                        100,
                                        50,
                                        d,
                                        _out)
                                    .ScreenShot ("Open dialog initial", _out);

        // Focus the Cancel button and grab a reference via MostFocused
        c.Focus<Button> (b => b.Text == Strings.btnCancel)
         .Then (_ =>
                {
                    cancelBtn = od!.MostFocused as Button;
                })
         .ScreenShot ("Cancel button focused", _out);

        Assert.True (cancelBtn?.HasFocus);
        Point posBefore = cancelBtn!.ViewportToScreen ().Location;

        // Click at the Cancel button position (Press + Release).
        // The bug: press triggers a layout pass that moves the button before release.
        c.LeftClick (posBefore.X + 1, posBefore.Y);

        // If the fix works, the click lands on Cancel and closes the dialog.
        // If the bug is present, the button moves and the click misses,
        // so the dialog stays open and IsRunning remains true.
        c.Then (_ =>
                {
                    // Verify the dialog was dismissed by the single click
                    Assert.False (od!.IsRunning, "Dialog should have closed on the first Cancel click");
                })
         .ScreenShot ("After click on Cancel button", _out);

        c.Stop ();
    }

    /// <summary>
    ///     Test cases for functions with signature <code>TestDriver d, bool someFlag</code>
    ///     that enumerates all variations
    /// </summary>
    private class TestDrivers_WithTrueFalseParameter : IEnumerable<object []>
    {
        public IEnumerator<object []> GetEnumerator ()
        {
            yield return [DriverRegistry.Names.WINDOWS, false];
            yield return [DriverRegistry.Names.DOTNET, false];
            yield return [DriverRegistry.Names.ANSI, true];
        }

        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers_WithTrueFalseParameter))]
    public void SaveFileDialog_TableView_UpDown_PreserveFilenameOnDirectoryChanges_True (string d, bool preserve)
    {
        SaveDialog? sd = null;

        using AppTestHelper c = With.A (() => NewSaveDialog (out sd, out MockFileSystem _), 100, 20, d)
                                    .Then (_ => sd!.Style.PreserveFilenameOnDirectoryChanges = preserve)
                                    .ScreenShot ("Save dialog", _out)
                                    .AssertTrue (sd!.Canceled)
                                    .Focus<TextField> (_ => true)

                                    // Clear selection by pressing right in 'file path' text box
                                    .KeyDown (Key.CursorRight)
                                    .AssertIsType<TextField> (sd!.Focused)

                                    // Type a filename into the dialog
                                    .KeyDown (Key.H)
                                    .KeyDown (Key.E)
                                    .KeyDown (Key.L)
                                    .KeyDown (Key.L)
                                    .KeyDown (Key.O)
                                    .ScreenShot ("After typing filename 'hello'", _out)
                                    .AssertEndsWith ("hello", sd!.Path)
                                    .Focus<TableView> (_ => true)
                                    .ScreenShot ("After focus table", _out)
                                    .KeyDown (Key.CursorDown)
                                    .ScreenShot ("After down in table", _out);

        if (preserve)
        {
            c.AssertContains ("logs", sd!.Path).AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("logs", sd!.Path).AssertDoesNotContain ("hello", sd!.Path);
        }

        c.KeyDown (Key.CursorUp).ScreenShot ("After up in table", _out);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd!.Path).AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd!.Path).AssertDoesNotContain ("hello", sd!.Path);
        }

        c.KeyDown (Key.Enter).ScreenShot ("After enter in table", _out);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd!.Path).AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd!.Path).AssertDoesNotContain ("hello", sd!.Path);
        }

        c.LeftClick<Button> (b => b.Text == Strings.cmdSave);
        c.AssertFalse (sd!.Canceled);

        if (preserve)
        {
            c.AssertContains ("empty-dir", sd!.Path).AssertEndsWith ("hello", sd!.Path);
        }
        else
        {
            c.AssertContains ("empty-dir", sd!.Path).AssertDoesNotContain ("hello", sd!.Path);
        }

        c.Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void SaveFileDialog_TableView_EnterOnDirectory_PreserveFilenameOnDirectoryChanges_True (string d)
    {
        // Copilot
        SaveDialog? sd = null;
        MockFileSystem? fs = null;

        using AppTestHelper c = With.A (() =>
                                        {
                                            IRunnable r = NewSaveDialog (out sd, out fs);
                                            sd.Style.PreserveFilenameOnDirectoryChanges = true;
                                            sd.AllowedTypes = [new AllowedType ("C# File", ".cs")];

                                            return r;
                                        }, 100, 20, d)
                                    .Then (_ =>
                                          {
                                              sd!.Path = fs!.Path.Combine (GetFileSystemRoot (fs), "hello.cs");
                                          })
                                    .ScreenShot ("Save dialog with filename", _out)
                                    .Focus<TableView> (_ => true)
                                    .KeyDown (Key.CursorDown)
                                    .ScreenShot ("After selecting logs directory", _out)
                                    .KeyDown (Key.Enter)
                                    .ScreenShot ("After entering logs directory", _out)
                                    .Then (_ =>
                                          {
                                              Assert.True (sd!.IsRunning);
                                              Assert.True (sd.Canceled);
                                              Assert.Contains ("logs", sd.Path);
                                              Assert.EndsWith ("hello.cs", sd.Path);
                                          })
                                    .KeyDown (Key.Enter)
                                    .ScreenShot ("After entering parent directory row", _out)
                                    .Then (_ =>
                                          {
                                              Assert.True (sd!.IsRunning);
                                              Assert.True (sd.Canceled);
                                              Assert.DoesNotContain ("logs", sd.Path);
                                              Assert.EndsWith ("hello.cs", sd.Path);
                                          });

        c.Stop ();
    }
}
