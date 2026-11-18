using System.Drawing;
using Xunit;

namespace AppTestHelpers.XunitHelpers;

public static partial class XunitContextExtensions
{
    // Placeholder


    /// <summary>
    /// Asserts that the last set cursor position matches <paramref name="expected"/>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="expected"></param>
    /// <returns></returns>
    public static AppTestHelper AssertCursorPosition (this AppTestHelper context, Point expected)
    {
        try
        {
            Assert.Equal (expected, context.GetCursorPosition ());
        }
        catch (Exception)
        {
            context.HardStop ();

            throw;
        }

        return context;
    }
}
