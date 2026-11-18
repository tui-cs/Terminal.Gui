#nullable disable

namespace ViewBaseTests.Layout;

public class DimFillTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    public void DimFill_Equal ()
    {
        var margin1 = 0;
        var margin2 = 0;
        Dim dim1 = Dim.Fill (margin1);
        Dim dim2 = Dim.Fill (margin2);
        Assert.Equal (dim1, dim2);
    }

    // Tests that Dim.Fill honors the margin parameter correctly
    [Theory]
    [InlineData (0, true, 25)]
    [InlineData (0, false, 25)]
    [InlineData (1, true, 24)]
    [InlineData (1, false, 24)]
    [InlineData (2, true, 23)]
    [InlineData (2, false, 23)]
    [InlineData (-2, true, 27)]
    [InlineData (-2, false, 27)]
    public void DimFill_Margin (int margin, bool width, int expected)
    {
        var super = new View { Width = 25, Height = 25 };

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = width ? Dim.Fill (margin) : 1,
            Height = width ? 1 : Dim.Fill (margin)
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

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

    // Tests that Dim.Fill fills the dimension REMAINING from the View's X position to the end of the super view's width
    [Theory]
    [InlineData (0, true, 25)]
    [InlineData (0, false, 25)]
    [InlineData (1, true, 24)]
    [InlineData (1, false, 24)]
    [InlineData (2, true, 23)]
    [InlineData (2, false, 23)]
    [InlineData (-2, true, 27)]
    [InlineData (-2, false, 27)]
    public void DimFill_Offset (int offset, bool width, int expected)
    {
        var super = new View { Width = 25, Height = 25 };

        var view = new View
        {
            X = width ? offset : 0,
            Y = width ? 0 : offset,
            Width = width ? Dim.Fill () : 1,
            Height = width ? 1 : Dim.Fill ()
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

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

    // TODO: Other Dim.Height tests (e.g. Equal?)

    [Fact]
    public void DimFill_SetsValue ()
    {
        var testMargin = 0;
        Dim dim = Dim.Fill ();
        Assert.Equal (testMargin, dim!.GetAnchor(0));

        testMargin = 0;
        dim = Dim.Fill (testMargin);
        Assert.Equal (testMargin, dim!.GetAnchor (0));

        testMargin = 5;
        dim = Dim.Fill (testMargin);
        Assert.Equal (-testMargin, dim!.GetAnchor (0));
    }

    [Fact]
    public void DimFill_Margin_Is_Dim_SetsValue ()
    {
        Dim testMargin = Dim.Func (_ => 0);
        Dim dim = Dim.Fill (testMargin);
        Assert.Equal (0, dim!.GetAnchor (0));


        testMargin = Dim.Func (_ => 5);
        dim = Dim.Fill (testMargin);
        Assert.Equal (-5, dim!.GetAnchor (0));
    }

    [Fact]
    public void DimFill_Calculate_ReturnsCorrectValue ()
    {
        var dim = Dim.Fill ();
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (100, result);
    }

    [Fact]
    public void ResizeView_With_Dim_Fill_After_IsInitialized ()
    {
        var super = new View { Frame = new (0, 0, 30, 80) };
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        super.Add (view);

        view.Text = "New text\nNew line";
        super.Layout ();
        Rectangle expectedViewBounds = new (0, 0, 30, 80);

        Assert.Equal (expectedViewBounds, view.Viewport);
        Assert.False (view.IsInitialized);

        super.BeginInit ();
        super.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.Equal (expectedViewBounds, view.Viewport);
    }

    [Fact]
    public void DimFill_SizedCorrectly ()
    {
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };
        var top = new View { Width = 80, Height = 25 };
        top.Add (view);

        top.Layout ();

        view.SetRelativeLayout (new (32, 5));
        Assert.Equal (32, view.Frame.Width);
        Assert.Equal (5, view.Frame.Height);
        top.Dispose ();
    }

    [Theory]
    [InlineData (100, 0, 40, 100)] // Fill size (100) > minimum (40) -> use fill size
    [InlineData (100, 0, 150, 150)] // Fill size (100) < minimum (150) -> use minimum
    [InlineData (100, 0, 100, 100)] // Fill size (100) == minimum (100) -> use either (same)
    [InlineData (100, 10, 40, 90)] // Fill with margin: (100-10=90) > minimum (40) -> use 90
    [InlineData (100, 10, 100, 100)] // Fill with margin: (100-10=90) < minimum (100) -> use minimum
    public void DimFill_With_MinimumContentDim_Calculate (int superviewSize, int margin, int minimum, int expected)
    {
        View view = new () { Width = Dim.Fill (margin, minimumContentDim: minimum) };
        View top = new () { Width = superviewSize };
        top.Add (view);

        int calculated = view.Width.Calculate (0, superviewSize, view, Dimension.Width);

        Assert.Equal (expected, calculated);
        top.Dispose ();
    }

    [Fact]
    public void DimFill_With_MinimumContentDim_ToString ()
    {
        Dim fill = Dim.Fill (5, minimumContentDim: 40);

        Assert.Equal ("Fill(Absolute(5),min:Absolute(40))", fill.ToString ());
    }

    [Fact]
    public void DimFill_Without_MinimumContentDim_ToString ()
    {
        Dim fill = Dim.Fill (5);

        Assert.Equal ("Fill(Absolute(5))", fill.ToString ());
    }

    // Claude - Opus 4.5
    // Tests for DimFill with 'to:' parameter
    [Fact]
    public void DimFill_To_Width_FillsToViewX ()
    {
        View super = new () { Width = 100, Height = 25 };

        View targetView = new ()
        {
            X = 80,
            Y = 0,
            Width = 10,
            Height = 1
        };

        View fillView = new ()
        {
            X = 10,
            Y = 0,
            Width = Dim.Fill (to: targetView),
            Height = 1
        };

        super.Add (targetView, fillView);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (100, super.Frame.Width);
        Assert.Equal (80, targetView.Frame.X);
        Assert.Equal (10, fillView.Frame.X);
        Assert.Equal (70, fillView.Frame.Width); // 80 - 10 = 70
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_Height_FillsToViewY ()
    {
        View super = new () { Width = 100, Height = 100 };

        View targetView = new ()
        {
            X = 0,
            Y = 80,
            Width = 10,
            Height = 10
        };

        View fillView = new ()
        {
            X = 0,
            Y = 10,
            Width = 10,
            Height = Dim.Fill (to: targetView)
        };

        super.Add (targetView, fillView);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (100, super.Frame.Height);
        Assert.Equal (80, targetView.Frame.Y);
        Assert.Equal (10, fillView.Frame.Y);
        Assert.Equal (70, fillView.Frame.Height); // 80 - 10 = 70
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_WithMargin_FillsToViewXMinusMargin ()
    {
        View super = new () { Width = 100, Height = 25 };

        View targetView = new ()
        {
            X = 80,
            Y = 0,
            Width = 10,
            Height = 1
        };

        View fillView = new ()
        {
            X = 10,
            Y = 0,
            Width = Dim.Fill (margin: 5, to: targetView),
            Height = 1
        };

        super.Add (targetView, fillView);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (80, targetView.Frame.X);
        Assert.Equal (10, fillView.Frame.X);
        Assert.Equal (65, fillView.Frame.Width); // 80 - 10 - 5 = 65
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_WithMinimumContentDim_RespectsMinimum ()
    {
        View super = new () { Width = 100, Height = 25 };

        View targetView = new ()
        {
            X = 20, // Very close to fillView
            Y = 0,
            Width = 10,
            Height = 1
        };

        View fillView = new ()
        {
            X = 10,
            Y = 0,
            Width = Dim.Fill (margin: 0, minimumContentDim: 50, to: targetView),
            Height = 1
        };

        super.Add (targetView, fillView);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (20, targetView.Frame.X);
        Assert.Equal (10, fillView.Frame.X);
        // Should be 50 (minimum), not 10 (20 - 10)
        Assert.Equal (50, fillView.Frame.Width);
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_ExampleFromIssue4656 ()
    {
        // This test validates the example code from the issue
        View appWindow = new () { Width = 100, Height = 25 };

        Label label = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Text = "Name:"
        };

        Button btn = new ()
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = 10,
            Text = "OK"
        };

        TextField textField = new ()
        {
            X = Pos.Right (label) + 1,
            Y = 0,
            Width = Dim.Fill (to: btn)
        };

        appWindow.Add (label, btn, textField);
        appWindow.BeginInit ();
        appWindow.EndInit ();
        appWindow.Layout ();

        Assert.Equal (100, appWindow.Frame.Width);
        Assert.Equal (0, label.Frame.X);
        Assert.Equal (10, label.Frame.Width);
        Assert.Equal (90, btn.Frame.X); // AnchorEnd with width 10
        Assert.Equal (11, textField.Frame.X); // Right(label) + 1 = 10 + 1
        Assert.Equal (79, textField.Frame.Width); // 90 - 11 = 79
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_ToString ()
    {
        View targetView = new ();
        Dim fill = Dim.Fill (to: targetView);

        string str = fill.ToString ();
        Assert.Contains ("Fill(Absolute(0)", str);
        Assert.Contains ($"to:{targetView}", str);
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_WithMargin_ToString ()
    {
        View targetView = new ();
        Dim fill = Dim.Fill (margin: 5, to: targetView);

        string str = fill.ToString ();
        Assert.Contains ("Fill(Absolute(5)", str);
        Assert.Contains ($"to:{targetView}", str);
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_WithMinimumContentDim_ToString ()
    {
        View targetView = new ();
        Dim fill = Dim.Fill (margin: 5, minimumContentDim: 40, to: targetView);

        string str = fill.ToString ();
        Assert.Contains ("Fill(Absolute(5)", str);
        Assert.Contains ("min:Absolute(40)", str);
        Assert.Contains ($"to:{targetView}", str);
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_ReferencesOtherViews_ReturnsTrue ()
    {
        View targetView = new ();
        Dim fill = Dim.Fill (to: targetView);

        Assert.True (fill.ReferencesOtherViews ());
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_WithoutTo_ReferencesOtherViews_ReturnsFalse ()
    {
        Dim fill = Dim.Fill ();

        Assert.False (fill.ReferencesOtherViews ());
    }

    // Claude - Opus 4.5
    [Fact]
    public void DimFill_To_NegativeResult_ReturnsZero ()
    {
        // Test that when the target view is before the fill view, it returns 0
        View super = new () { Width = 100, Height = 25 };

        View targetView = new ()
        {
            X = 5,
            Y = 0,
            Width = 10,
            Height = 1
        };

        View fillView = new ()
        {
            X = 20,
            Y = 0,
            Width = Dim.Fill (to: targetView),
            Height = 1
        };

        super.Add (targetView, fillView);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (5, targetView.Frame.X);
        Assert.Equal (20, fillView.Frame.X);
        Assert.Equal (0, fillView.Frame.Width); // Should be 0, not negative
    }
}
