namespace InputTests.MouseTests;

public class MouseEventArgsTests
{
    [Fact]
    public void Constructor_Default_ShouldSetFlagsToNone ()
    {
        var eventArgs = new Mouse ();
        Assert.Equal (MouseFlags.None, eventArgs.Flags);
    }

    [Fact]
    public void HandledProperty_ShouldBeFalseByDefault ()
    {
        var eventArgs = new Mouse ();
        Assert.False (eventArgs.Handled);
    }
}
