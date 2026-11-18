namespace ViewBaseTests.Layout;

public class TopologicalSortTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void TopologicalSort_Missing_Add ()
    {
        var root = new View ();
        var sub1 = new View ();
        root.Add (sub1);
        var sub2 = new View ();
        sub1.Width = Dim.Width (sub2);

        Assert.Throws<LayoutException> (() => root.LayoutSubViews ());

        sub2.Width = Dim.Width (sub1);

        Assert.Throws<LayoutException> (() => root.LayoutSubViews ());
        root.Dispose ();
        sub1.Dispose ();
        sub2.Dispose ();
    }

    [Fact]
    public void TopologicalSort_Recursive_Ref_Does_Not_Throw ()
    {
        var root = new View ();
        var sub1 = new View ();
        root.Add (sub1);
        var sub2 = new View ();
        root.Add (sub2);
        sub2.Width = Dim.Width (sub2);

        Exception? exception = Record.Exception (root.LayoutSubViews);
        Assert.Null (exception);
        root.Dispose ();
        sub1.Dispose ();
        sub2.Dispose ();
    }


    [Fact]
    public void TopologicalSort_Throws_If_SuperView_Refs_SubView ()
    {
        var top = new View ();
        var superView = new View ();
        top.Add (superView);

        var subView = new View ();
        superView.Y = Pos.Top (subView);
        superView.Add (subView);

        Assert.Throws<LayoutException> (() => top.LayoutSubViews ());
        superView.Dispose ();
    }

    [Fact]
    public void TopologicalSort_View_Not_Added_Throws ()
    {
        var top = new View { Width = 80, Height = 50 };

        var super = new View { Width = Dim.Width (top) - 2, Height = Dim.Height (top) - 2 };
        top.Add (super);

        var sub = new View ();
        super.Add (sub);

        var v1 = new View { Width = Dim.Width (super) - 2, Height = Dim.Height (super) - 2 };
        var v2 = new View { Width = Dim.Width (v1) - 2, Height = Dim.Height (v1) - 2 };
        sub.Add (v1);

        // v2 not added to sub; should cause exception on Layout since it's referenced by sub.
        sub.Width = Dim.Fill () - Dim.Width (v2);
        sub.Height = Dim.Fill () - Dim.Height (v2);

        Assert.Throws<LayoutException> (() => top.Layout ());
    }
}
