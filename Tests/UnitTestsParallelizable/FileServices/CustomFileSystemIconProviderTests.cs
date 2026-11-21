// CoPilot - GPT-6.1
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace FileServicesTests;

public class CustomFileSystemIconProviderTests
{
    [Fact]
    public void GetIconWithOptionalSpace_UsesOverriddenRune ()
    {
        MockFileSystem fileSystem = new ();
        FileSystemIconProvider provider = new LetterIconProvider { UseNerdIcons = false };

        Assert.Equal ("D", provider.GetIconWithOptionalSpace (fileSystem.DirectoryInfo.New ("folder")));
        Assert.Equal ("F", provider.GetIconWithOptionalSpace (fileSystem.FileInfo.New ("file.txt")));
    }

    [Fact]
    public void CustomProvider_AcceptsNullThroughBaseContract ()
    {
        FileSystemIconProvider provider = new LetterIconProvider { UseNerdIcons = false };

        Assert.Equal (new Rune ('F'), provider.GetIcon (null));
        Assert.Equal ("F", provider.GetIconWithOptionalSpace (null));
    }

    [Fact]
    public void GetIconWithOptionalSpace_PreservesSupplementaryCodepoint ()
    {
        MockFileSystem fileSystem = new ();
        FileSystemIconProvider provider = new () { UseNerdIcons = true };

        Assert.Equal ("\U000F031B ", provider.GetIconWithOptionalSpace (fileSystem.FileInfo.New ("Program.cs")));
    }

    [Theory]
    [InlineData (false, 0xEA83)]
    [InlineData (true, 0xEAF7)]
    public void GetIcon_UsesIsOpenGetter (bool isOpen, int codepoint)
    {
        MockFileSystem fileSystem = new ();
        FileSystemIconProvider provider = new () { UseNerdIcons = true, IsOpenGetter = _ => isOpen };

        Assert.Equal (new Rune (codepoint), provider.GetIcon (fileSystem.DirectoryInfo.New ("folder")));
    }

    private sealed class LetterIconProvider : FileSystemIconProvider
    {
        public override Rune GetIcon (IFileSystemInfo? fileSystemInfo) => new (fileSystemInfo is IDirectoryInfo ? 'D' : 'F');
    }
}
