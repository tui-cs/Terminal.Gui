// Copilot

namespace UnitTests.Views;

/// <summary>
///     Tests for path-traversal and invalid-name validation in <see cref="DefaultFileOperations"/>.
/// </summary>
public class DefaultFileOperationsSecurityTests
{
    [Theory]
    [InlineData ("/home/user/docs", "/home/user/docs/file.txt", true)]
    [InlineData ("/home/user/docs", "/home/user/docs/sub/file.txt", true)]
    [InlineData ("/home/user/docs", "/home/user/file.txt", false)]
    [InlineData ("/home/user/docs", "/home/user/docs/../file.txt", false)]
    [InlineData ("/home/user/docs", "/home/file.txt", false)]
    public void IsContainedIn_DetectsPathTraversal_Unix (string root, string candidate, bool expected)
    {
        if (!OperatingSystem.IsLinux () && !OperatingSystem.IsMacOS ())
        {
            return;
        }

        bool result = DefaultFileOperations.IsContainedIn (root, candidate);
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData ("C:\\Users\\docs", "C:\\Users\\docs\\file.txt", true)]
    [InlineData ("C:\\Users\\docs", "C:\\Users\\docs\\sub\\file.txt", true)]
    [InlineData ("C:\\Users\\docs", "C:\\Users\\file.txt", false)]
    [InlineData ("C:\\Users\\docs", "C:\\Users\\docs\\..\\file.txt", false)]
    public void IsContainedIn_DetectsPathTraversal_Windows (string root, string candidate, bool expected)
    {
        if (!OperatingSystem.IsWindows ())
        {
            return;
        }

        bool result = DefaultFileOperations.IsContainedIn (root, candidate);
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData ("validname", false)]
    [InlineData ("my-file.txt", false)]
    [InlineData ("../escape", true)]
    [InlineData ("sub/dir", true)]
    [InlineData ("", true)]
    [InlineData ("   ", true)]
    [InlineData ("file\0name", true)]
    public void ContainsInvalidNameCharacters_DetectsInvalidNames (string name, bool expected)
    {
        bool result = DefaultFileOperations.ContainsInvalidNameCharacters (name);
        Assert.Equal (expected, result);
    }

    [Fact]
    public void IsContainedIn_RootIsContainedInItself_WhenSubPath ()
    {
        // A path that is exactly the root + separator + name should be contained
        if (OperatingSystem.IsWindows ())
        {
            Assert.True (DefaultFileOperations.IsContainedIn ("C:\\root", "C:\\root\\child"));
        }
        else
        {
            Assert.True (DefaultFileOperations.IsContainedIn ("/root", "/root/child"));
        }
    }

    [Fact]
    public void IsContainedIn_RootItself_IsNotContained ()
    {
        // The root directory path itself (without trailing separator) is NOT considered "contained"
        // because it's not a child path
        if (OperatingSystem.IsWindows ())
        {
            Assert.False (DefaultFileOperations.IsContainedIn ("C:\\root", "C:\\root"));
        }
        else
        {
            Assert.False (DefaultFileOperations.IsContainedIn ("/root", "/root"));
        }
    }
}
