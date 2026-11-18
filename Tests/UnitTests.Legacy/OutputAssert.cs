namespace UnitTests;

/// <summary>
///     Provides xunit-style assertions for <see cref="ITestOutputHelper"/>.
/// </summary>
internal class OutputAssert
{
#pragma warning disable xUnit1013 // Public method should be marked as test
    /// <summary>
    ///     Verifies two strings are equivalent. If the assert fails, output will be generated to standard output showing
    ///     the expected and actual look.
    /// </summary>
    /// <param name="output"></param>
    /// <param name="expectedLook">
    ///     A string containing the expected look. Newlines should be specified as "\r\n" as they will
    ///     be converted to <see cref="Environment.NewLine"/> to make tests platform independent.
    /// </param>
    /// <param name="actualLook"></param>
    public static void AssertEqual (ITestOutputHelper output, string expectedLook, string actualLook)
    {
        // Convert newlines to platform-specific newlines
        expectedLook = ReplaceNewLinesToPlatformSpecific (expectedLook);

        // If test is about to fail show user what things looked like
        if (!string.Equals (expectedLook, actualLook))
        {
            output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
            output?.WriteLine (" But Was:" + Environment.NewLine + actualLook);
        }

        Assert.Equal (expectedLook, actualLook);
    }
#pragma warning restore xUnit1013 // Public method should be marked as test

    private static string ReplaceNewLinesToPlatformSpecific (string toReplace)
    {
        string replaced = toReplace;

        replaced = Environment.NewLine.Length switch
                   {
                       2 when !replaced.Contains ("\r\n") => replaced.Replace ("\n", Environment.NewLine),
                       1 => replaced.Replace ("\r\n", Environment.NewLine),
                       _ => replaced
                   };

        return replaced;
    }
}
