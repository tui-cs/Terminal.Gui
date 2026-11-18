// Copilot
#nullable enable

namespace FileServicesTests;

/// <summary>
///     Parallelizable tests for <see cref="AllowedType.IsAllowed"/> extension matching logic.
/// </summary>
public class AllowedTypeTests
{
    [Theory]
    [InlineData (".csv", null, false)]
    [InlineData (".csv", "", false)]
    [InlineData (".csv", "c:\\MyFile.csv", true)]
    [InlineData (".csv", "c:\\MyFile.CSV", true)]
    [InlineData (".csv", "c:\\MyFile.csv.bak", false)]
    public void IsAllowed_BasicExtension (string allowed, string? candidate, bool expected)
    {
        Assert.Equal (expected, new AllowedType ("Test", allowed).IsAllowed (candidate!));
    }

    [Theory]
    [InlineData (".Designer.cs", "c:\\MyView.Designer.cs", true)]
    [InlineData (".Designer.cs", "c:\\temp/MyView.Designer.cs", true)]
    [InlineData (".Designer.cs", "MyView.Designer.cs", true)]
    [InlineData (".Designer.cs", "c:\\MyView.DESIGNER.CS", true)]
    [InlineData (".Designer.cs", "MyView.cs", false)]
    public void IsAllowed_DoubleBarreledExtension (string allowed, string candidate, bool expected)
    {
        Assert.Equal (expected, new AllowedType ("Test", allowed).IsAllowed (candidate));
    }

    [Theory]
    [InlineData ("Dockerfile", "c:\\temp\\Dockerfile", true)]
    [InlineData ("Dockerfile", "Dockerfile", true)]
    [InlineData ("Dockerfile", "someimg.Dockerfile", true)]
    public void IsAllowed_SpecificFile (string allowed, string candidate, bool expected)
    {
        Assert.Equal (expected, new AllowedType ("Test", allowed).IsAllowed (candidate));
    }
}
