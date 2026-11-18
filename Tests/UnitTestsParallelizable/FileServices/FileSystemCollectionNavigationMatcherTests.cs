// Copilot
#nullable enable
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace FileServicesTests;

/// <summary>
///     Tests for <see cref="FileSystemCollectionNavigationMatcher"/> keystroke navigation behavior.
/// </summary>
public class FileSystemCollectionNavigationMatcherTests
{
    [Fact]
    public void IsMatch_IFileSystemInfo_MatchesOnName_NotAspectGetter ()
    {
        // Arrange: AspectGetter returns a prefixed string, but matcher should use Name only.
        var mockFs = new MockFileSystem ();
        mockFs.AddFile ("banana.csv", new MockFileData (""));
        IFileInfo banana = mockFs.FileInfo.New ("banana.csv");

        var matcher = new FileSystemCollectionNavigationMatcher ();

        // "b" matches because banana.csv.Name starts with 'b'
        Assert.True (matcher.IsMatch ("b", banana));

        // "[I" does NOT match even though AspectGetter might produce "[ICON] banana.csv"
        Assert.False (matcher.IsMatch ("[I", banana));
    }

    [Fact]
    public void IsMatch_NonFileSystemInfo_FallsBackToToString ()
    {
        var matcher = new FileSystemCollectionNavigationMatcher ();

        // A plain string — falls back to base ToString() matching.
        Assert.True (matcher.IsMatch ("b", "banana"));
        Assert.False (matcher.IsMatch ("x", "banana"));
    }

    [Fact]
    public void TreeView_WithMatcher_NavigatesOnName_IgnoresAspectGetterPrefix ()
    {
        // Arrange: AspectGetter adds a "[ICON] " prefix; navigation must still work on Name.
        var mockFs = new MockFileSystem ();
        mockFs.AddFile ("apple.csv", new MockFileData (""));
        mockFs.AddFile ("banana.csv", new MockFileData (""));
        mockFs.AddFile ("cherry.csv", new MockFileData (""));

        IFileInfo apple = mockFs.FileInfo.New ("apple.csv");
        IFileInfo banana = mockFs.FileInfo.New ("banana.csv");
        IFileInfo cherry = mockFs.FileInfo.New ("cherry.csv");

        TreeView<IFileSystemInfo> tv = new ()
        {
            Width = 20,
            Height = 10
        };
        tv.TreeBuilder = new DelegateTreeBuilder<IFileSystemInfo> (_ => null!, o => false);
        tv.AspectGetter = fsi => $"[ICON] {fsi.Name}";
        tv.KeystrokeNavigator.Matcher = new FileSystemCollectionNavigationMatcher ();

        tv.AddObject (apple);
        tv.AddObject (banana);
        tv.AddObject (cherry);

        tv.SelectedObject = apple;

        // Press 'b' → should jump to banana even though AspectGetter starts with "[ICON]"
        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (banana, tv.SelectedObject);

        // Press 'c' → cherry
        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (cherry, tv.SelectedObject);

        tv.Dispose ();
    }
}
