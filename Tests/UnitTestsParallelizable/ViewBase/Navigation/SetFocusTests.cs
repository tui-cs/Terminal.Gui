using UnitTests;

namespace ViewBaseTests.Navigation;

public class SetFocusTests : TestsAllViews
{
    [Fact]
    public void SetFocus_With_Null_Superview_Does_Not_Throw_Exception ()
    {
        var view = new View { Id = "view", CanFocus = true };
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        Exception? exception = Record.Exception (() => view.SetFocus ());
        Assert.Null (exception);

        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void SetFocus_SetsFocus ()
    {
        var view = new View { Id = "view", CanFocus = true };
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void SetFocus_NoSubView_Focused_Is_Null ()
    {
        var view = new View { Id = "view", CanFocus = true };
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Null (view.Focused);
    }

    [Fact]
    public void SetFocus_SubView_Focused_Is_Set ()
    {
        var view = new Window { Id = "view", CanFocus = true };

        var subview = new View { Id = "subview", CanFocus = true };
        view.Add (subview);
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (subview, view.Focused);
    }

    [Fact]
    public void SetFocus_SetsFocus_DeepestSubView ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subview = new View { Id = "subview", CanFocus = true };
        view.Add (subview);

        view.SetFocus ();
        Assert.True (subview.HasFocus);
        Assert.Equal (subview, view.Focused);
    }

    [Fact]
    public void SetFocus_SetsFocus_DeepestSubView_CompoundSubView ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subView = new View { Id = "subView", CanFocus = true };

        var subViewSubView1 = new View { Id = "subViewSubView1", CanFocus = false };

        var subViewSubView2 = new View { Id = "subViewSubView2", CanFocus = true };

        var subViewSubView3 = new View { Id = "subViewSubView3", CanFocus = false };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView2, subView.Focused);
    }

    [Fact]
    public void SetFocus_CompoundSubView_SetFocus_Sets ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subView = new View { Id = "subView", CanFocus = true };

        var subViewSubView1 = new View { Id = "subViewSubView1", CanFocus = true };

        var subViewSubView2 = new View { Id = "subViewSubView2", CanFocus = true };

        var subViewSubView3 = new View { Id = "subViewSubView3", CanFocus = true };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.True (subViewSubView1.HasFocus);
        Assert.Equal (subViewSubView1, subView.Focused);

        subViewSubView2.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.True (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);
    }

    [Fact]
    public void SetFocus_AdornmentSubView_SetFocus_Sets ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subView = new View { Id = "subView", CanFocus = true };

        view.Add (subView);

        var borderSubView = new View { Id = "borderSubView", CanFocus = true };

        var subViewSubView1 = new View { Id = "subViewSubView1", CanFocus = true };

        var subViewSubView2 = new View { Id = "subViewSubView2", CanFocus = true };

        var subViewSubView3 = new View { Id = "subViewSubView3", CanFocus = true };
        borderSubView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Border.GetOrCreateView ().Add (borderSubView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (borderSubView.HasFocus);

        view.Border.GetOrCreateView ();
        view.Border.View?.CanFocus = true;
        subViewSubView1.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.True (borderSubView.HasFocus);
        Assert.True (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.Border.View?.CanFocus = false;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (view.Border.View?.HasFocus);
        Assert.False (borderSubView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.Border.View?.CanFocus = true;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (view.Border.View?.HasFocus);
        Assert.False (borderSubView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.Border.View!.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (view.Border.View?.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.True (borderSubView.HasFocus);
        Assert.True (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.Border.View?.CanFocus = false;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (view.Border.View?.HasFocus);
        Assert.False (borderSubView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.Border.View?.CanFocus = true;
        subViewSubView1.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.True (view.Border.View?.HasFocus);
        Assert.True (borderSubView.HasFocus);
        Assert.True (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        subView.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (view.Border.View?.HasFocus);
        Assert.False (borderSubView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);
    }

    [Fact]
    public void SetFocus_Peer_LeavesOther ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subview1 = new View { Id = "subview1", CanFocus = true };

        var subview2 = new View { Id = "subview2", CanFocus = true };
        view.Add (subview1, subview2);

        view.SetFocus ();
        Assert.Equal (subview1, view.Focused);
        Assert.True (subview1.HasFocus);
        Assert.False (subview2.HasFocus);

        subview2.SetFocus ();
        Assert.Equal (subview2, view.Focused);
        Assert.True (subview2.HasFocus);
        Assert.False (subview1.HasFocus);
    }

    [Fact]
    public void SetFocus_On_Peer_Moves_Focus_To_Peer ()
    {
        var top = new View { Id = "top", CanFocus = true };

        var view1 = new View { Id = "view1", CanFocus = true };

        var subView1 = new View { Id = "subView1", CanFocus = true };

        view1.Add (subView1);

        var subView1SubView1 = new View { Id = "subView1subView1", CanFocus = true };

        subView1.Add (subView1SubView1);

        var view2 = new View { Id = "view2", CanFocus = true };

        top.Add (view1, view2);
        Assert.False (view1.HasFocus);
        Assert.False (view2.HasFocus);

        view1.SetFocus ();
        Assert.True (view1.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.True (subView1SubView1.HasFocus);
        Assert.Equal (subView1, view1.Focused);
        Assert.Equal (subView1SubView1, subView1.Focused);

        view2.SetFocus ();
        Assert.False (view1.HasFocus);
        Assert.True (view2.HasFocus);
    }
}
