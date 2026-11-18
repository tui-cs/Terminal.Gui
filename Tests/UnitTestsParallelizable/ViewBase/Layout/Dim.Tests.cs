#nullable disable

using static Terminal.Gui.ViewBase.Dim;

namespace ViewBaseTests.Layout;


public class DimTests
{
    [Fact]
    public void DimAbsolute_Calculate_ReturnsCorrectValue ()
    {
        var dim = new DimAbsolute (10);
        int result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Null ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new View
        {
            BorderStyle = LineStyle.Single,
            X = 1,
            Y = 2,
            Width = 4,
            Height = 5,
            Title = "w"
        };
        t.Add (w);
        t.LayoutSubViews ();

        Assert.Equal (3, w.Width = 3);
        Assert.Equal (4, w.Height = 4);
        t.Dispose ();
    }

    [Fact]
    public void DimHeight_Set_To_Null_Throws ()
    {
        Dim dim = Height (null);
        Assert.Throws<NullReferenceException> (() => dim.ToString ());
    }

    [Fact]
    public void DimHeight_SetsValue ()
    {
        var testVal = Rectangle.Empty;
        var testValView = new View { Frame = testVal };
        Dim dim = Height (testValView);
        Assert.Equal (0, dim.GetAnchor (0));
        testValView.Dispose ();

        testVal = new (1, 2, 3, 4);
        testValView = new () { Frame = testVal };
        dim = Height (testValView);
        Assert.Equal (4, dim.GetAnchor (0));
        testValView.Dispose ();
    }

    [Fact]
    public void Internal_Tests ()
    {
        var dimFactor = new DimPercent (10);
        Assert.Equal (10, dimFactor.GetAnchor (100));

        var dimAbsolute = new DimAbsolute (10);
        Assert.Equal (10, dimAbsolute.GetAnchor (0));

        var dimFill = new DimFill (1);
        Assert.Equal (99, dimFill.GetAnchor (100));

        var dimCombine = new DimCombine (AddOrSubtract.Add, dimFactor, dimAbsolute);
        Assert.Equal (dimCombine.Left, dimFactor);
        Assert.Equal (dimCombine.Right, dimAbsolute);
        Assert.Equal (20, dimCombine.GetAnchor (100));

        var view = new View { Frame = new (20, 10, 20, 1) };
        var dimViewHeight = new DimView (view, Dimension.Height);
        Assert.Equal (1, dimViewHeight.GetAnchor (0));
        var dimViewWidth = new DimView (view, Dimension.Width);
        Assert.Equal (20, dimViewWidth.GetAnchor (0));

        view.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Referencing_SuperView_Does_Not_Throw ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };

        var view = new View
        {
            Width = Width (super), // this is allowed
            Height = Height (super), // this is allowed
            Text = "view"
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubViews);
        Assert.Null (exception);
        super.Dispose ();
    }

    [Fact]
    public void DimSized_Equals ()
    {
        var n1 = 0;
        var n2 = 0;
        Dim dim1 = Absolute (n1);
        Dim dim2 = Absolute (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 1;
        dim1 = Absolute (n1);
        dim2 = Absolute (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = -1;
        dim1 = Absolute (n1);
        dim2 = Absolute (n2);
        Assert.Equal (dim1, dim2);

        n1 = 0;
        n2 = 1;
        dim1 = Absolute (n1);
        dim2 = Absolute (n2);
        Assert.NotEqual (dim1, dim2);
    }

    [Fact]
    public void DimSized_SetsValue ()
    {
        Dim dim = Absolute (0);
        Assert.Equal ("Absolute(0)", dim.ToString ());

        var testVal = 5;
        dim = Absolute (testVal);
        Assert.Equal ($"Absolute({testVal})", dim.ToString ());

        testVal = -1;
        dim = Absolute (testVal);
        Assert.Equal ($"Absolute({testVal})", dim.ToString ());
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Validation_Does_Not_Throw_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Null ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window
        {
            X = 1,
            Y = 2,
            Width = 4,
            Height = 5,
            Title = "w"
        };
        t.Add (w);
        t.LayoutSubViews ();

        Assert.Equal (3, w.Width = 3);
        Assert.Equal (4, w.Height = 4);
        t.Dispose ();
    }

    [Fact]
    public void DimWidth_Equals ()
    {
        var testRect1 = Rectangle.Empty;
        var view1 = new View { Frame = testRect1 };
        var testRect2 = Rectangle.Empty;
        var view2 = new View { Frame = testRect2 };

        Dim dim1 = Width (view1);
        Dim dim2 = Width (view1);

        // FIXED: Dim.Width should support Equals() and this should change to Equal.
        Assert.Equal (dim1, dim2);

        dim2 = Width (view2);
        Assert.NotEqual (dim1, dim2);

        testRect1 = new (0, 1, 2, 3);
        view1 = new () { Frame = testRect1 };
        testRect2 = new (0, 1, 2, 3);
        dim1 = Width (view1);
        dim2 = Width (view1);

        // FIXED: Dim.Width should support Equals() and this should change to Equal.
        Assert.Equal (dim1, dim2);

        testRect1 = new (0, -1, 2, 3);
        view1 = new () { Frame = testRect1 };
        testRect2 = new (0, -1, 2, 3);
        dim1 = Width (view1);
        dim2 = Width (view1);

        // FIXED: Dim.Width should support Equals() and this should change to Equal.
        Assert.Equal (dim1, dim2);

        testRect1 = new (0, -1, 2, 3);
        view1 = new () { Frame = testRect1 };
        testRect2 = Rectangle.Empty;
        view2 = new () { Frame = testRect2 };
        dim1 = Width (view1);
        dim2 = Width (view2);
        Assert.NotEqual (dim1, dim2);
    }

    [Fact]
    public void DimWidth_Set_To_Null_Throws ()
    {
        Dim dim = Width (null);
        Assert.Throws<NullReferenceException> (() => dim.ToString ());
    }

    [Fact]
    public void DimWidth_SetsValue ()
    {
        var testVal = Rectangle.Empty;
        var testValView = new View { Frame = testVal };
        Dim dim = Width (testValView);
        Assert.Equal (0, dim.GetAnchor (0));
        testValView.Dispose ();

        testVal = new (1, 2, 3, 4);
        testValView = new () { Frame = testVal };
        dim = Width (testValView);
        Assert.Equal (3, dim.GetAnchor (0));
        testValView.Dispose ();
    }
}
