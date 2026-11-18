using UnitTests;
using static Terminal.Gui.ViewBase.Pos;

namespace ViewBaseTests.Layout;

public class PosCenterTests (ITestOutputHelper output) : TestDriverBase
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void PosCenter_Constructor ()
    {
        var posCenter = new PosCenter ();
        Assert.NotNull (posCenter);
    }

    [Fact]
    public void PosCenter_ToString ()
    {
        var posCenter = new PosCenter ();
        var expectedString = "Center";

        Assert.Equal (expectedString, posCenter.ToString ());
    }

    [Fact]
    public void PosCenter_GetAnchor ()
    {
        var posCenter = new PosCenter ();
        var width = 50;
        int expectedAnchor = width / 2;

        Assert.Equal (expectedAnchor, posCenter.GetAnchor (width));
    }

    [Fact]
    public void PosCenter_CreatesCorrectInstance ()
    {
        Pos pos = Center ();
        Assert.IsType<PosCenter> (pos);
    }

    [Theory]
    [InlineData (10, 2, 4)]
    [InlineData (10, 10, 0)]
    [InlineData (10, 11, 0)]
    [InlineData (10, 12, -1)]
    [InlineData (19, 20, 0)]
    public void PosCenter_Calculate_ReturnsExpectedValue (int superviewDimension, int width, int expectedX)
    {
        var posCenter = new PosCenter ();
        int result = posCenter.Calculate (superviewDimension, new DimAbsolute (width), null!, Dimension.Width);
        Assert.Equal (expectedX, result);
    }

    [Fact]
    public void PosCenter_Bigger_Than_SuperView ()
    {
        var superView = new View { Width = 10, Height = 10 };
        var view = new View { X = Center (), Y = Center (), Width = 20, Height = 20 };
        superView.Add (view);
        superView.LayoutSubViews ();

        Assert.Equal (-5, view.Frame.Left);
        Assert.Equal (-5, view.Frame.Top);
    }

    [Theory]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void PosCenter_SubView_85_Percent_Height (int height)
    {
        IDriver driver = CreateTestDriver (20, height);
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Driver = driver;

        var subview = new Window
        {
            X = Center (), Y = Center (), Width = Dim.Percent (85), Height = Dim.Percent (85)
        };

        win.Add (subview);
        win.BeginInit ();
        win.EndInit ();
        win.SetRelativeLayout (driver.Screen.Size);
        win.LayoutSubViews ();
        win.Draw ();

        var expected = string.Empty;

        switch (height)
        {
            case 1:
                expected = @"
────────────────────";

                break;
            case 2:
                expected = @"
┌──────────────────┐
└──────────────────┘
";

                break;
            case 3:
                expected = @"
┌──────────────────┐
│                  │
└──────────────────┘
";

                break;
            case 4:
                expected = @"
┌──────────────────┐
│ ───────────────  │
│                  │
└──────────────────┘";

                break;
            case 5:
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 6:
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 7:
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 8:
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 9:
                expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 10:
                expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘"
                    ;

                break;
        }

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, _output, driver);
        win.Dispose ();
        driver.Dispose ();
    }

    [Theory]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void PosCenter_SubView_85_Percent_Width (int width)
    {
        IDriver driver = CreateTestDriver (width, 7);
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Driver = driver;

        var subview = new Window
        {
            X = Center (), Y = Center (), Width = Dim.Percent (85), Height = Dim.Percent (85)
        };

        win.Add (subview);
        win.BeginInit ();
        win.EndInit ();
        win.SetRelativeLayout (driver.Screen.Size);
        win.LayoutSubViews ();
        win.Draw ();

        var expected = string.Empty;

        switch (width)
        {
            case 1:
                Assert.Equal (new (0, 0, 0, 4), subview.Frame);

                expected = @"
│
│
│
│
│
│
│";

                break;
            case 2:
                Assert.Equal (new (0, 0, 0, 4), subview.Frame);

                expected = @"
┌┐
││
││
││
││
││
└┘";

                break;
            case 3:
                Assert.Equal (new (0, 0, 0, 4), subview.Frame);

                expected = @"
┌─┐
│ │
│ │
│ │
│ │
│ │
└─┘";

                break;
            case 4:
                Assert.Equal (new (0, 0, 1, 4), subview.Frame);

                expected = @"
┌──┐
││ │
││ │
││ │
││ │
│  │
└──┘";

                break;
            case 5:
                Assert.Equal (new (0, 0, 2, 4), subview.Frame);

                expected = @"
┌───┐
│┌┐ │
│││ │
│││ │
│└┘ │
│   │
└───┘";

                break;
            case 6:
                Assert.Equal (new (0, 0, 3, 4), subview.Frame);

                expected = @"
┌────┐
│┌─┐ │
││ │ │
││ │ │
│└─┘ │
│    │
└────┘";

                break;
            case 7:
                Assert.Equal (new (0, 0, 4, 4), subview.Frame);

                expected = @"
┌─────┐
│┌──┐ │
││  │ │
││  │ │
│└──┘ │
│     │
└─────┘";

                break;
            case 8:
                Assert.Equal (new (0, 0, 5, 4), subview.Frame);

                expected = @"
┌──────┐
│┌───┐ │
││   │ │
││   │ │
│└───┘ │
│      │
└──────┘";

                break;
            case 9:
                Assert.Equal (new (1, 0, 5, 4), subview.Frame);

                expected = @"
┌───────┐
│ ┌───┐ │
│ │   │ │
│ │   │ │
│ └───┘ │
│       │
└───────┘";

                break;
            case 10:
                Assert.Equal (new (1, 0, 6, 4), subview.Frame);

                expected = @"
┌────────┐
│ ┌────┐ │
│ │    │ │
│ │    │ │
│ └────┘ │
│        │
└────────┘"
                    ;

                break;
        }

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, _output, driver);
        win.Dispose ();
        driver.Dispose ();
    }
}
