
namespace ViewsTests;

/// <summary>
///     Tests for the bespoke behaviour of <see cref="ScrollButton"/> (glyph selection, orientation,
///     direction, and constructor defaults). Button base-class behaviour is covered by
///     <see cref="ButtonTests"/>.
/// </summary>
public class ScrollButtonTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        ScrollButton btn = new ();

        Assert.False (btn.CanFocus);
        Assert.True (btn.NoDecorations);
        Assert.True (btn.NoPadding);
        Assert.Null (btn.ShadowStyle);
        Assert.Equal (MouseFlags.LeftButtonReleased, btn.MouseHoldRepeat);
        Assert.Equal (Orientation.Horizontal, btn.Orientation);

        btn.Dispose ();
    }

    [Theory]
    [InlineData (Orientation.Horizontal, NavigationDirection.Backward)]
    [InlineData (Orientation.Horizontal, NavigationDirection.Forward)]
    [InlineData (Orientation.Vertical, NavigationDirection.Backward)]
    [InlineData (Orientation.Vertical, NavigationDirection.Forward)]
    public void Title_Reflects_Direction_And_Orientation (Orientation orientation, NavigationDirection direction)
    {
        string expected = (orientation, direction) switch
                          {
                              (Orientation.Horizontal, NavigationDirection.Backward) => Glyphs.LeftArrow.ToString (),
                              (Orientation.Horizontal, NavigationDirection.Forward) => Glyphs.RightArrow.ToString (),
                              (Orientation.Vertical, NavigationDirection.Backward) => Glyphs.UpArrow.ToString (),
                              (Orientation.Vertical, NavigationDirection.Forward) => Glyphs.DownArrow.ToString (),
                              _ => throw new ArgumentOutOfRangeException ()
                          };

        ScrollButton btn = new () { Orientation = orientation, Direction = direction };

        Assert.Equal (expected, btn.Title);

        btn.Dispose ();
    }

    [Fact]
    public void Setting_Direction_Updates_Glyph ()
    {
        ScrollButton btn = new () { Orientation = Orientation.Horizontal };

        btn.Direction = NavigationDirection.Backward;
        Assert.Equal (Glyphs.LeftArrow.ToString (), btn.Title);

        btn.Direction = NavigationDirection.Forward;
        Assert.Equal (Glyphs.RightArrow.ToString (), btn.Title);

        btn.Dispose ();
    }

    [Fact]
    public void Setting_Orientation_Updates_Glyph ()
    {
        ScrollButton btn = new () { Direction = NavigationDirection.Forward };

        btn.Orientation = Orientation.Horizontal;
        Assert.Equal (Glyphs.RightArrow.ToString (), btn.Title);

        btn.Orientation = Orientation.Vertical;
        Assert.Equal (Glyphs.DownArrow.ToString (), btn.Title);

        btn.Dispose ();
    }

    [Fact]
    public void Direction_SameValue_DoesNotChange_Title ()
    {
        ScrollButton btn = new () { Orientation = Orientation.Vertical, Direction = NavigationDirection.Backward };
        string titleBefore = btn.Title;

        // Assign the same value — guard clause should prevent any mutation.
        btn.Direction = NavigationDirection.Backward;

        Assert.Equal (titleBefore, btn.Title);

        btn.Dispose ();
    }
}
