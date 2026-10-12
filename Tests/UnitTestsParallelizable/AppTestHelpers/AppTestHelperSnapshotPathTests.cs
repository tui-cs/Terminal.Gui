using AppTestHelpers;

namespace AppTestHelpersTests;

// CoPilot - GitHub Copilot
public class AppTestHelperSnapshotPathTests
{
    [Theory]
    [InlineData ("C:\\src\\Tests.cs")]
    [InlineData ("C:/src/Tests.cs")]
    [InlineData ("C:/src\\Tests.cs")]
    public void IsWindowsStylePath_Recognizes_Drive_Letter_Paths (string path)
    {
        Assert.True (AppTestHelper.IsWindowsStylePath (path));
    }

    [Theory]
    [InlineData ("/tmp/suite:case/Test.cs")]
    [InlineData ("/mnt/c/src/Tests.cs")]
    [InlineData ("Tests.cs")]
    public void IsWindowsStylePath_Rejects_Non_Drive_Letter_Paths (string path)
    {
        Assert.False (AppTestHelper.IsWindowsStylePath (path));
    }

    [Fact]
    public void ToWslPath_Maps_Windows_Path_With_Mixed_Separators ()
    {
        string translated = AppTestHelper.ToWslPath ("C:/src\\Tests.cs");

        Assert.Equal ("/mnt/c/src/Tests.cs", translated);
    }
}
