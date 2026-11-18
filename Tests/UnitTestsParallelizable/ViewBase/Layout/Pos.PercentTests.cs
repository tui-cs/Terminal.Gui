using static Terminal.Gui.ViewBase.Pos;

namespace ViewBaseTests.Layout;

public class PosPercentTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData (50, 10, 2, 5)]
    [InlineData (50, 10, 10, 5)]
    [InlineData (50, 10, 11, 5)]
    [InlineData (50, 10, 12, 5)]
    [InlineData (50, 19, 20, 9)]
    public void PosPercent_Calculate_ReturnsExpectedValue (int percent, int superviewDimension, int width, int expectedX)
    {
        var posPercent = new PosPercent (percent);
        int result = posPercent.Calculate (superviewDimension, new DimAbsolute (width), null!, Dimension.Width);
        Assert.Equal (expectedX, result);
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void PosPercent_PlusOne (bool testHorizontal)
    {
        var container = new View { Width = 100, Height = 100 };

        var view = new View
        {
            X = testHorizontal ? Percent (50) + Percent (10) + 1 : 1,
            Y = testHorizontal ? 1 : Percent (50) + Percent (10) + 1,
            Width = 10,
            Height = 10
        };

        container.Add (view);
        var top = new Runnable ();
        top.Add (container);
        top.LayoutSubViews ();

        Assert.Equal (100, container.Frame.Width);
        Assert.Equal (100, container.Frame.Height);

        if (testHorizontal)
        {
            Assert.Equal (61, view.Frame.X);
            Assert.Equal (1, view.Frame.Y);
        }
        else
        {
            Assert.Equal (1, view.Frame.X);
            Assert.Equal (61, view.Frame.Y);
        }

        top.Dispose ();
    }

    [Fact]
    public void PosPercent_SetsValue ()
    {
        var f = 0;
        Pos pos = Percent (f);
        Assert.Equal ($"Percent({f})", pos.ToString ());
        f = 50;
        pos = Percent (f);
        Assert.Equal ($"Percent({f})", pos.ToString ());
        f = 100;
        pos = Percent (f);
        Assert.Equal ($"Percent({f})", pos.ToString ());
    }

    [Fact]
    public void PosPercent_ThrowsOnInvalid ()
    {
        Pos pos = Percent (0);
        Assert.Throws<ArgumentOutOfRangeException> (() => pos = Percent (-1));

        //Assert.Throws<ArgumentException> (() => pos = Pos.Percent (101));
        //Assert.Throws<ArgumentException> (() => pos = Pos.Percent (1000001));
    }
}
