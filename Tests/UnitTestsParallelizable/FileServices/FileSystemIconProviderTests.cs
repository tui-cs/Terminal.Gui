using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.InteropServices;
using System.Text;

namespace FileServicesTests;

public class FileSystemIconProviderTests
{
    [Fact]
    public void FlagsShouldBeMutuallyExclusive ()
    {
        var p = new FileSystemIconProvider { UseUnicodeCharacters = false, UseNerdIcons = false };

        Assert.False (p.UseUnicodeCharacters);
        Assert.False (p.UseNerdIcons);

        p.UseUnicodeCharacters = true;

        Assert.True (p.UseUnicodeCharacters);
        Assert.False (p.UseNerdIcons);

        // Cannot use both nerd and unicode so unicode should have switched off
        p.UseNerdIcons = true;

        Assert.True (p.UseNerdIcons);
        Assert.False (p.UseUnicodeCharacters);

        // Cannot use both unicode and nerd so now nerd should have switched off
        p.UseUnicodeCharacters = true;

        Assert.True (p.UseUnicodeCharacters);
        Assert.False (p.UseNerdIcons);
    }

    [Fact]
    public void TestBasicIcons ()
    {
        var p = new FileSystemIconProvider ();
        IFileSystem fs = GetMockFileSystem ();

        Assert.Equal (OperatingSystem.IsWindows () ? new Rune ('\\') : new Rune ('/'), p.GetIcon (fs.DirectoryInfo.New (@"c:\")));

        Assert.Equal (
                      new Rune (' '),
                      p.GetIcon (
                                 fs.FileInfo.New (GetFileSystemRoot () + @"myfile.txt")
                                )
                     );
    }

    private string GetFileSystemRoot () { return OperatingSystem.IsWindows () ? @"c:\" : "/"; }

    private IFileSystem GetMockFileSystem ()
    {
        string root = GetFileSystemRoot ();

        var fileSystem = new MockFileSystem (new Dictionary<string, MockFileData> (), root);

        fileSystem.AddFile (root + @"myfile.txt", new MockFileData ("Testing is meh."));
        fileSystem.AddFile (root + @"demo/jQuery.js", new MockFileData ("some js"));
        fileSystem.AddFile (root + @"demo/mybinary.exe", new MockFileData ("some js"));
        fileSystem.AddFile (root + @"demo/image.gif", new MockFileData (new byte [] { 0x12, 0x34, 0x56, 0xd2 }));

        var m = (MockDirectoryInfo)fileSystem.DirectoryInfo.New (root + @"demo/subfolder");
        m.Create ();

        return fileSystem;
    }
}
