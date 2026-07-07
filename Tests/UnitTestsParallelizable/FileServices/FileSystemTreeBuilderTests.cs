using System.IO.Abstractions;
using Moq;

namespace FileServicesTests;

public class FileSystemTreeBuilderTests
{
    [Fact]
    public void CanExpand_DirectoryWithUnreadableAttributes_DoesNotThrowAndReturnsFalse ()
    {
        Mock<IDirectoryInfo> directory = new ();
        directory.SetupGet (d => d.Attributes).Throws (new UnauthorizedAccessException ("Access denied"));
        directory.SetupGet (d => d.Exists).Returns (true);
        directory.Setup (d => d.GetFileSystemInfos ()).Returns ([]);

        FileSystemTreeBuilder builder = new ();

        bool canExpand = builder.CanExpand (directory.Object);

        Assert.False (canExpand);
    }

    [Fact]
    public void GetChildren_DirectoryWithUnreadableAttributes_DoesNotThrowAndReturnsEmpty ()
    {
        Mock<IDirectoryInfo> directory = new ();
        directory.SetupGet (d => d.Attributes).Throws (new UnauthorizedAccessException ("Access denied"));
        directory.SetupGet (d => d.Exists).Returns (true);
        directory.Setup (d => d.GetFileSystemInfos ()).Returns ([]);

        FileSystemTreeBuilder builder = new ();

        IEnumerable<IFileSystemInfo> children = builder.GetChildren (directory.Object);

        Assert.Empty (children);
    }

    // Claude - Opus 4.8
    [Fact]
    public void IsReparsePoint_WhenAttributesUnreadable_FailsClosed ()
    {
        Mock<IDirectoryInfo> directory = new ();
        directory.SetupGet (d => d.Attributes).Throws (new UnauthorizedAccessException ("Access denied"));

        // Fail closed: an entry whose reparse status cannot be read must be treated as a reparse
        // point so callers (CanExpand, GetChildren, FileDialog search) do not traverse it.
        Assert.True (FileSystemTreeBuilder.IsReparsePoint (directory.Object));
    }
}
