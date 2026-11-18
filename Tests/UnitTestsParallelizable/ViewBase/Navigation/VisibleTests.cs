using UnitTests;

namespace ViewBaseTests;


public class VisibleTests () : TestsAllViews
{
    [Fact]
    public void Visible_False_Leaves ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.SetFocus ();
        Assert.True (view.HasFocus);

        view.Visible = false;
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Visible_False_Leaves_SubView ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);

        view.Visible = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Visible_False_Leaves_SubView2 ()
    {
        var view = new Window
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);

        view.Visible = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Visible_False_On_SubView_Leaves_Just_SubView ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);

        subView.Visible = false;
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Visible_False_Focuses_Deepest_Focusable_SubView ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = true // This is the one that will be focused
        };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView2, subView.Focused);

        subViewSubView2.Visible = false;
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView3, subView.Focused);
        Assert.True (subViewSubView3.HasFocus);
    }

    [Fact]
    public void Visible_True_SubView_Focuses_SubView ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true,
            Visible = false
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);

        view.Visible = true;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void Visible_True_On_SubView_Focuses ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true,
            Visible = false
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);

        subView.Visible = true;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void Visible_True_Focuses_Deepest_Focusable_SubView ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true,
            Visible = false
        };

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true // This is the one that will be focused
        };

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = true
        };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.False (subView.HasFocus);
        Assert.False (subViewSubView2.HasFocus);

        subView.Visible = true;
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView2, subView.Focused);
        Assert.True (subViewSubView2.HasFocus);
    }
}
