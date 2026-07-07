// Copilot

using System.Reflection;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Moq;

namespace UnitTests.Views;

/// <summary>
///     Tests for <see cref="FileDialog"/> refactored to inherit from <see cref="Dialog{TResult}"/>
///     where TResult is <c>IReadOnlyList&lt;string&gt;?</c>.
/// </summary>
public class FileDialogResultTests
{
    [Fact]
    public void FileDialog_Result_IsNull_WhenNotAccepted ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        // Assert - Result should be null before any acceptance
        Assert.Null (fd.Result);
        Assert.True (fd.Canceled);
    }

    [Fact]
    public void FileDialog_Result_IsNull_BeforeAcceptance ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("hello"));
        using SaveDialog sd = new TestableSaveDialog (fs);
        sd.Path = "/testdir/file1.txt";

        // Assert - Result should be null before any acceptance
        Assert.True (sd.Canceled);
        Assert.Null (sd.Result);
    }

    [Fact]
    public void FileDialog_Canceled_IsTrue_WhenResultIsNull ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        // Assert
        Assert.True (fd.Canceled);
    }

    [Fact]
    public void FileDialog_Canceled_IsFalse_WhenAccepted_ThroughCommandPipeline ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("hello"));
        using SaveDialog sd = new TestableSaveDialog (fs);
        sd.Path = "/testdir/file1.txt";

        // Act
        bool? accepted = sd.InvokeCommand (Command.Accept);

        // Assert
        Assert.True (accepted is true);
        Assert.False (sd.Canceled);
        Assert.NotNull (sd.Result);
        Assert.Single (sd.Result);
        Assert.Equal ("/testdir/file1.txt", sd.Result [0]);
    }

    [Fact]
    public void FileDialog_InheritsFromDialogOfReadOnlyListString ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        // Assert - verify the new base type
        Assert.IsAssignableFrom<Dialog<IReadOnlyList<string>?>> (fd);
    }

    [Fact]
    public void OpenDialog_FilePaths_IsEmpty_WhenCanceled ()
    {
        // Arrange
        using OpenDialog od = new TestableOpenDialog ();

        // Assert - Result is null → Canceled → FilePaths empty
        Assert.True (od.Canceled);
        Assert.Empty (od.FilePaths);
    }

    [Fact]
    public void SaveDialog_FileName_IsNull_WhenCanceled ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using SaveDialog sd = new TestableSaveDialog (fs);

        // Assert
        Assert.True (sd.Canceled);
        Assert.Null (sd.FileName);
    }

    [Fact]
    public void FileDialog_Result_MultiSelection_IsPopulated ()
    {
        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("a"));
        fs.AddFile ("/testdir/file2.txt", new MockFileData ("b"));
        using FileDialog fd = new TestableFileDialog (fs);

        // Act - directly set Result as would happen after multi-select acceptance
        List<string> paths = ["/testdir/file1.txt", "/testdir/file2.txt"];
        fd.Result = paths.AsReadOnly ();

        // Assert
        Assert.False (fd.Canceled);
        Assert.Equal (2, fd.Result.Count);
        Assert.Contains ("/testdir/file1.txt", fd.Result);
        Assert.Contains ("/testdir/file2.txt", fd.Result);
    }

    [Fact]
    public void FileDialog_Cancel_ClearsResult_WhenPreviouslyAccepted ()
    {
        // Copilot
        // This test validates that canceling a dialog after a previous acceptance
        // correctly clears Result to null (so Canceled == true).
        // Without the explicit `Result = null` in the Cancel path, this test would fail
        // because Result would retain the previously accepted value.

        // Arrange
        MockFileSystem fs = new ();
        fs.AddFile ("/testdir/file1.txt", new MockFileData ("hello"));
        using SaveDialog sd = new TestableSaveDialog (fs);
        sd.Path = "/testdir/file1.txt";

        // Act - first accept the dialog to set Result
        sd.InvokeCommand (Command.Accept);
        Assert.False (sd.Canceled); // Result is set — not canceled

        // Act - now simulate pressing Cancel button
        Button cancelBtn = sd.Buttons [sd.CancelButtonIndex];
        cancelBtn.InvokeCommand (Command.Accept);

        // Assert - Result should be cleared, Canceled should be true
        Assert.Null (sd.Result);
        Assert.True (sd.Canceled);
    }

    [Fact]
    public void OpenDialog_UsesInnerTableSeparatorsWithoutOuterBorders ()
    {
        using OpenDialog od = new TestableOpenDialog ();

        FieldInfo? tableViewField = typeof (FileDialog).GetField ("_tableView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tableViewField);

        TableView tableView = Assert.IsType<TableView> (tableViewField.GetValue (od));

        Assert.True (tableView.Style.ShowVerticalCellLines);
        Assert.True (tableView.Style.ShowVerticalHeaderLines);
        Assert.False (tableView.Style.ShowVerticalCellLineForFirstColumn);
        Assert.False (tableView.Style.ShowVerticalCellLineForLastColumn);
    }

    [Fact]
    public void FileDialog_PathField_End_MovesInsertionPointToEnd ()
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        FieldInfo? tbPathField = typeof (FileDialog).GetField ("_tbPath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tbPathField);

        TextField tbPath = Assert.IsType<TextField> (tbPathField.GetValue (fd));
        tbPath.Text = "/testdir/example.txt";

        tbPath.NewKeyDownEvent (Key.Home);
        Assert.Equal (0, tbPath.InsertionPoint);

        tbPath.NewKeyDownEvent (Key.End);
        Assert.Equal (tbPath.Text.Length, tbPath.InsertionPoint);
    }

    [Theory]
    [InlineData ('"')]
    [InlineData ('<')]
    [InlineData ('>')]
    [InlineData ('|')]
    [InlineData ('*')]
    [InlineData ('?')]
    public void FileDialog_PathField_BadChars_AreSuppressed (char badChar)
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        using FileDialog fd = new TestableFileDialog (fs);

        FieldInfo? tbPathField = typeof (FileDialog).GetField ("_tbPath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tbPathField);

        TextField tbPath = Assert.IsType<TextField> (tbPathField.GetValue (fd));
        tbPath.Text = "/testdir/";
        tbPath.MoveEnd ();

        int insertionPointBefore = tbPath.InsertionPoint;
        string textBefore = tbPath.Text;

        tbPath.NewKeyDownEvent (new Key (badChar));

        Assert.Equal (textBefore, tbPath.Text);
        Assert.Equal (insertionPointBefore, tbPath.InsertionPoint);
    }

    [Fact]
    public void FileDialog_MixedMode_PathSetBeforeEndInit_RespectsAllowedTypes ()
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/testdir");
        fs.AddDirectory ("/testdir/subdir");
        fs.AddFile ("/testdir/allowed.Designer.cs", new MockFileData ("allowed"));
        fs.AddFile ("/testdir/blocked.txt", new MockFileData ("blocked"));
        using FileDialog fd = new TestableFileDialog (fs)
        {
            OpenMode = OpenMode.Mixed,
            AllowedTypes = [new AllowedType ("View", ".Designer.cs")]
        };

        fd.Path = "/testdir";
        fd.EndInit ();

        Assert.NotNull (fd.CurrentFilter);
        Assert.NotNull (fd.State);

        List<string> visibleEntries = fd.State!.Children
                                       .Where (c => !c.IsParent)
                                       .Select (c => c.Name)
                                       .OrderBy (name => name)
                                       .ToList ();

        Assert.Equal (["allowed.Designer.cs", "subdir"], visibleEntries);
    }

    [Fact]
    public void FileDialog_MixedMode_SkipsUnreadableEntry_AndKeepsReadableEntries ()
    {
        IFileSystem fileSystem = CreateFileSystemWithDirectory (out IDirectoryInfo directory);
        IFileInfo goodFile = CreateFile ("/testdir/good.txt", "good.txt");
        IDirectoryInfo goodDirectory = CreateDirectory ("/testdir/good-dir", "good-dir", directory);
        IFileInfo badFile = CreateUnreadableFile ("/testdir/bad.txt", "bad.txt");

        Mock.Get (directory)
            .Setup (d => d.GetFileSystemInfos ())
            .Returns ([goodFile, badFile, goodDirectory]);

        Mock.Get (directory)
            .Setup (d => d.EnumerateFileSystemInfos ("*", It.IsAny<EnumerationOptions> ()))
            .Returns ([goodFile, badFile, goodDirectory]);

        using FileDialog fd = new TestableFileDialog (fileSystem);
        fd.OpenMode = OpenMode.Mixed;

        fd.Path = "/testdir";

        Assert.NotNull (fd.State);
        Assert.Contains (fd.State!.Children, c => c.Name == "good.txt");
        Assert.Contains (fd.State.Children, c => c.Name == "good-dir");
        Assert.Contains (fd.State.Children, c => c.IsParent && c.Name == "..");
        Assert.DoesNotContain (fd.State.Children, c => c.Name == "bad.txt");
    }

    [Fact]
    public void FileDialog_DirectoryMode_SkipsUnreadableDirectory_AndKeepsParentNavigation ()
    {
        IFileSystem fileSystem = CreateFileSystemWithDirectory (out IDirectoryInfo directory);
        IDirectoryInfo goodDirectory = CreateDirectory ("/testdir/good-dir", "good-dir", directory);
        IDirectoryInfo badDirectory = CreateUnreadableDirectory ("/testdir/bad-dir", "bad-dir", directory);

        Mock.Get (directory)
            .Setup (d => d.GetDirectories ())
            .Returns ([goodDirectory, badDirectory]);

        Mock.Get (directory)
            .Setup (d => d.EnumerateDirectories ("*", It.IsAny<EnumerationOptions> ()))
            .Returns ([goodDirectory, badDirectory]);

        using FileDialog fd = new TestableFileDialog (fileSystem);
        fd.OpenMode = OpenMode.Directory;

        fd.Path = "/testdir";

        Assert.NotNull (fd.State);
        Assert.Contains (fd.State!.Children, c => c.Name == "good-dir");
        Assert.Contains (fd.State.Children, c => c.IsParent && c.Name == "..");
        Assert.DoesNotContain (fd.State.Children, c => c.Name == "bad-dir");
    }

    [Fact]
    public void FileDialog_MixedMode_WhenEagerListingThrows_StillKeepsReadableEntries ()
    {
        IFileSystem fileSystem = CreateFileSystemWithDirectory (out IDirectoryInfo directory);
        IFileInfo goodFile = CreateFile ("/testdir/good.txt", "good.txt");
        IDirectoryInfo goodDirectory = CreateDirectory ("/testdir/good-dir", "good-dir", directory);

        Mock.Get (directory)
            .Setup (d => d.GetFileSystemInfos ())
            .Throws (new UnauthorizedAccessException ());

        Mock.Get (directory)
            .Setup (d => d.EnumerateFileSystemInfos ())
            .Returns ([goodFile, goodDirectory]);

        Mock.Get (directory)
            .Setup (d => d.EnumerateFileSystemInfos ("*", It.IsAny<EnumerationOptions> ()))
            .Returns ([goodFile, goodDirectory]);

        using FileDialog fd = new TestableFileDialog (fileSystem);
        fd.OpenMode = OpenMode.Mixed;

        fd.Path = "/testdir";

        Assert.NotNull (fd.State);
        Assert.Contains (fd.State!.Children, c => c.Name == "good.txt");
        Assert.Contains (fd.State.Children, c => c.Name == "good-dir");
        Assert.Contains (fd.State.Children, c => c.IsParent && c.Name == "..");
    }

    [Fact]
    public void FileDialog_DirectoryMode_WhenEagerListingThrows_StillKeepsReadableDirectories ()
    {
        IFileSystem fileSystem = CreateFileSystemWithDirectory (out IDirectoryInfo directory);
        IDirectoryInfo goodDirectory = CreateDirectory ("/testdir/good-dir", "good-dir", directory);

        Mock.Get (directory)
            .Setup (d => d.GetDirectories ())
            .Throws (new UnauthorizedAccessException ());

        Mock.Get (directory)
            .Setup (d => d.EnumerateDirectories ())
            .Returns ([goodDirectory]);

        Mock.Get (directory)
            .Setup (d => d.EnumerateDirectories ("*", It.IsAny<EnumerationOptions> ()))
            .Returns ([goodDirectory]);

        using FileDialog fd = new TestableFileDialog (fileSystem);
        fd.OpenMode = OpenMode.Directory;

        fd.Path = "/testdir";

        Assert.NotNull (fd.State);
        Assert.Contains (fd.State!.Children, c => c.Name == "good-dir");
        Assert.Contains (fd.State.Children, c => c.IsParent && c.Name == "..");
    }

    [Fact]
    public void FileDialog_Accepting_Directory_From_Table_Keeps_Path_On_Opened_Directory ()
    {
        // Copilot
        MockFileSystem fs = new ();
        fs.AddDirectory ("/UI");
        fs.AddDirectory ("/UI/Window");
        fs.AddFile ("/UI/Window/file1.txt", new MockFileData ("hello"));

        using FileDialog fd = new TestableFileDialog (fs) { OpenMode = OpenMode.File };
        fd.Path = "/UI";

        FieldInfo? tableViewField = typeof (FileDialog).GetField ("_tableView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tableViewField);

        TableView tableView = Assert.IsType<TableView> (tableViewField!.GetValue (fd));

        // Find the "Window" directory row by scanning the table source rather than hard-coding an index.
        int windowRow = FindRowByName (tableView, "Window");
        Assert.True (windowRow >= 0, "Expected to find a row named 'Window' in the table.");

        tableView.SetFocus ();
        tableView.SetSelection (0, windowRow, false);

        // Path should end with the selected directory name (platform-independent check).
        Assert.EndsWith ("Window", fd.Path);

        string pathBeforeAccept = fd.Path;

        tableView.InvokeCommand (Command.Accept);

        // After accepting a directory in File mode, the path should remain unchanged
        // (navigates into the directory rather than selecting a file).
        Assert.Equal (pathBeforeAccept, fd.Path);
    }

    /// <summary>Finds a row in a <see cref="TableView"/> whose first column contains the given name.</summary>
    private static int FindRowByName (TableView tableView, string name)
    {
        ITableSource? source = tableView.Table;

        if (source is null)
        {
            return -1;
        }

        for (var row = 0; row < source.Rows; row++)
        {
            string cellText = source [row, 0]?.ToString () ?? string.Empty;

            if (cellText.Contains (name, StringComparison.Ordinal))
            {
                return row;
            }
        }

        return -1;
    }

    private static IFileSystem CreateFileSystemWithDirectory (out IDirectoryInfo directory)
    {
        Mock<IFileSystem> fileSystem = new ();
        Mock<IDirectoryInfoFactory> directoryInfoFactory = new ();
        Mock<IDirectoryInfo> parent = CreateDirectoryMock ("/", string.Empty, null);
        Mock<IDirectoryInfo> dir = CreateDirectoryMock ("/testdir", "testdir", parent.Object);

        directoryInfoFactory.Setup (f => f.New ("/testdir")).Returns (dir.Object);
        fileSystem.SetupGet (f => f.DirectoryInfo).Returns (directoryInfoFactory.Object);

        directory = dir.Object;

        return fileSystem.Object;
    }

    private static IFileInfo CreateFile (string fullName, string name)
    {
        Mock<IFileInfo> file = new ();
        file.SetupGet (f => f.Exists).Returns (true);
        file.SetupGet (f => f.FullName).Returns (fullName);
        file.SetupGet (f => f.Name).Returns (name);
        file.SetupGet (f => f.Extension).Returns (System.IO.Path.GetExtension (name));
        file.SetupGet (f => f.Length).Returns (12);
        file.SetupGet (f => f.LastWriteTime).Returns (new DateTime (2026, 1, 1));

        return file.Object;
    }

    private static IFileInfo CreateUnreadableFile (string fullName, string name)
    {
        Mock<IFileInfo> file = Mock.Get (CreateFile (fullName, name));
        file.SetupGet (f => f.LastWriteTime).Throws (new IOException ("Transport endpoint is not connected"));

        return file.Object;
    }

    private static IDirectoryInfo CreateDirectory (string fullName, string name, IDirectoryInfo? parent)
    {
        return CreateDirectoryMock (fullName, name, parent).Object;
    }

    private static IDirectoryInfo CreateUnreadableDirectory (string fullName, string name, IDirectoryInfo? parent)
    {
        Mock<IDirectoryInfo> directory = CreateDirectoryMock (fullName, name, parent);
        directory.SetupGet (d => d.LastWriteTime).Throws (new UnauthorizedAccessException ());

        return directory.Object;
    }

    private static Mock<IDirectoryInfo> CreateDirectoryMock (string fullName, string name, IDirectoryInfo? parent)
    {
        Mock<IDirectoryInfo> directory = new ();
        directory.SetupGet (d => d.Exists).Returns (true);
        directory.SetupGet (d => d.FullName).Returns (fullName);
        directory.SetupGet (d => d.Name).Returns (name);
        directory.SetupGet (d => d.Extension).Returns (string.Empty);
        directory.SetupGet (d => d.Parent).Returns (parent);
        directory.SetupGet (d => d.LastWriteTime).Returns (new DateTime (2026, 1, 1));
        directory.Setup (d => d.GetFileSystemInfos ()).Returns ([]);
        directory.Setup (d => d.GetDirectories ()).Returns ([]);
        directory.Setup (d => d.EnumerateFileSystemInfos ()).Returns ([]);
        directory.Setup (d => d.EnumerateDirectories ()).Returns ([]);
        directory.Setup (d => d.EnumerateFileSystemInfos ("*", It.IsAny<EnumerationOptions> ())).Returns ([]);
        directory.Setup (d => d.EnumerateDirectories ("*", It.IsAny<EnumerationOptions> ())).Returns ([]);

        return directory;
    }

    /// <summary>Testable subclass that exposes the internal file-system constructor.</summary>
    private sealed class TestableFileDialog : FileDialog
    {
        public TestableFileDialog (MockFileSystem fs) : base (fs) { }

        public TestableFileDialog (IFileSystem fileSystem) : base (fileSystem) { }
    }

    /// <summary>Testable subclass for OpenDialog.</summary>
    private sealed class TestableOpenDialog : OpenDialog
    {
        public TestableOpenDialog () { }
    }

    /// <summary>Testable subclass for SaveDialog.</summary>
    private sealed class TestableSaveDialog : SaveDialog
    {
        public TestableSaveDialog (MockFileSystem fs) : base (fs) { }
    }
}
