#nullable disable

namespace ViewBaseTests.Layout;

public class DimPercentTests
{
    [Fact]
    public void DimFactor_Calculate_ReturnsCorrectValue ()
    {
        var dim = new DimPercent (50);
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (50, result);
    }

    [Fact]
    public void DimPercent_Equals ()
    {
        int n1 = 0;
        int n2 = 0;
        Dim dim1 = Dim.Percent (n1);
        Dim dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 1;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 50;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 100;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 30;
        dim1 = Dim.Percent (n1, DimPercentMode.Position);
        dim2 = Dim.Percent (n2, DimPercentMode.Position);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 30;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2, DimPercentMode.Position);
        Assert.NotEqual (dim1, dim2);

        n1 = 0;
        n2 = 1;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.NotEqual (dim1, dim2);

        n1 = 50;
        n2 = 150;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.NotEqual (dim1, dim2);
    }

    [Fact]
    public void TestEquality ()
    {
        var a = Dim.Percent (32);
        var b = Dim.Percent (32);
        Assert.True (a.Equals (b));
        Assert.True (a.GetHashCode () == b.GetHashCode ());
    }

    [Fact]
    public void DimPercent_Invalid_Throws ()
    {
        Dim dim = Dim.Percent (0);
        Assert.Throws<ArgumentOutOfRangeException> (() => dim = Dim.Percent (-1));
        //Assert.Throws<ArgumentException> (() => dim = Dim.Percent (101));
        Assert.Throws<ArgumentOutOfRangeException> (() => dim = Dim.Percent (-1000001));
        //Assert.Throws<ArgumentException> (() => dim = Dim.Percent (1000001));
    }

    [Theory]
    [InlineData (0, DimPercentMode.ContentSize, true, 12)]
    [InlineData (0, DimPercentMode.ContentSize, false, 12)]
    [InlineData (1, DimPercentMode.ContentSize, true, 12)]
    [InlineData (1, DimPercentMode.ContentSize, false, 12)]
    [InlineData (2, DimPercentMode.ContentSize, true, 12)]
    [InlineData (2, DimPercentMode.ContentSize, false, 12)]

    [InlineData (0, DimPercentMode.Position, true, 12)]
    [InlineData (0, DimPercentMode.Position, false, 12)]
    [InlineData (1, DimPercentMode.Position, true, 12)]
    [InlineData (1, DimPercentMode.Position, false, 12)]
    [InlineData (2, DimPercentMode.Position, true, 11)]
    [InlineData (2, DimPercentMode.Position, false, 11)]
    public void DimPercent_Position (int position, DimPercentMode mode, bool width, int expected)
    {
        var super = new View { Width = 25, Height = 25 };

        var view = new View
        {
            X = width ? position : 0,
            Y = width ? 0 : position,
            Width = width ? Dim.Percent (50, mode) : 1,
            Height = width ? 1 : Dim.Percent (50, mode)
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubViews ();

        Assert.Equal (25, super.Frame.Width);
        Assert.Equal (25, super.Frame.Height);

        if (width)
        {
            Assert.Equal (expected, view.Frame.Width);
            Assert.Equal (1, view.Frame.Height);
        }
        else
        {
            Assert.Equal (1, view.Frame.Width);
            Assert.Equal (expected, view.Frame.Height);
        }
    }

    [Theory]
    [InlineData (0, true)]
    [InlineData (0, false)]
    [InlineData (50, true)]
    [InlineData (50, false)]
    public void DimPercent_PlusOne (int startingDistance, bool testHorizontal)
    {
        var super = new View { Width = 100, Height = 100 };

        var view = new View
        {
            X = testHorizontal ? startingDistance : 0,
            Y = testHorizontal ? 0 : startingDistance,
            Width = testHorizontal ? Dim.Percent (50) + 1 : 1,
            Height = testHorizontal ? 1 : Dim.Percent (50) + 1
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubViews ();

        Assert.Equal (100, super.Frame.Width);
        Assert.Equal (100, super.Frame.Height);

        if (testHorizontal)
        {
            Assert.Equal (51, view.Frame.Width);
            Assert.Equal (1, view.Frame.Height);
        }
        else
        {
            Assert.Equal (1, view.Frame.Width);
            Assert.Equal (51, view.Frame.Height);
        }
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (50)]
    [InlineData (100)]
    [InlineData (101)]

    public void DimPercent_SetsValue (int percent)
    {
        Dim dim = Dim.Percent (percent);
        Assert.Equal ($"Percent({percent},ContentSize)", dim.ToString ());
    }

}
