namespace ViewBaseTests.Adornments;

// Copilot
public class AdornmentViewTests
{
    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4883.
    ///     AdornmentView.Contains must not throw NullReferenceException when Adornment is null
    ///     (i.e. the view was created via its parameter-less constructor, as happens when
    ///     AllViewsTester / Themes UICatalog scenario creates it via reflection).
    /// </summary>
    [Fact]
    public void Contains_Returns_False_When_Adornment_Is_Null ()
    {
        // Arrange - parameter-less ctor leaves Adornment = null
        AdornmentView adornmentView = new ();

        // Act & Assert - must not throw NullReferenceException
        bool result = adornmentView.Contains (new Point (0, 0));

        Assert.False (result);
    }


}
