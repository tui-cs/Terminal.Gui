using UnitTests;
namespace ViewBaseTests.Navigation;



public class CanFocusTests () : TestsAllViews
{
    [Fact]
    public void CanFocus_False_Prevents_SubSubView_HasFocus ()
    {
        var view = new View { };
        var subView = new View { };
        var subSubView = new View { CanFocus = true };

        subView.Add (subSubView);
        view.Add (subView);

        Assert.False (view.CanFocus);
        Assert.False (subView.CanFocus);
        Assert.True (subSubView.CanFocus);

        view.SetFocus ();
        Assert.False (view.HasFocus);

        subView.SetFocus ();
        Assert.False (subView.HasFocus);

        subSubView.SetFocus ();
        Assert.False (subSubView.HasFocus);
    }

    [Fact]
    public void CanFocus_False_Prevents_SubView_HasFocus ()
    {
        var view = new View { };
        var subView = new View { CanFocus = true };
        var subSubView = new View { };

        subView.Add (subSubView);
        view.Add (subView);

        Assert.False (view.CanFocus);
        Assert.True (subView.CanFocus);
        Assert.False (subSubView.CanFocus);

        view.SetFocus ();
        Assert.False (view.HasFocus);

        subView.SetFocus ();
        Assert.False (subView.HasFocus);

        subSubView.SetFocus ();
        Assert.False (subSubView.HasFocus);
    }

    [Fact]
    public void CanFocus_Set_True_No_SuperView_Doesnt_Set_HasFocus ()
    {
        var view = new View { };

        // Act
        view.CanFocus = true;
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void CanFocus_Set_True_Sets_HasFocus_To_True ()
    {
        var view = new View { };
        var subView = new View { };
        view.Add (subView);

        Assert.False (view.CanFocus);
        Assert.False (subView.CanFocus);

        view.SetFocus ();
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);

        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);

        // Act
        subView.CanFocus = true;
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void CanFocus_Set_SubView_True_Sets_HasFocus_To_True ()
    {
        var view = new View
        {
            CanFocus = true
        };
        var subView = new View
        {
            CanFocus = false
        };
        var subSubView = new View
        {
            CanFocus = true
        };

        subView.Add (subSubView);
        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.False (subSubView.HasFocus);

        // Act
        subView.CanFocus = true;
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);
    }


    [Fact]
    public void CanFocus_Set_SubView_True_Does_Not_Change_Focus_If_SuperView_Focused_Is_True ()
    {
        var top = new View
        {
            Id = "top",
            CanFocus = true
        };
        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };
        var subSubView = new View
        {
            Id = "subSubView",
            CanFocus = true
        };

        subView.Add (subSubView);

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = false
        };

        top.Add (subView, subView2);

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);

        // Act
        subView2.CanFocus = true;
        Assert.False (subView2.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);
    }

    [Fact]
    public void CanFocus_Set_False_Sets_HasFocus_To_False ()
    {
        var view = new View { CanFocus = true };
        var view2 = new View { CanFocus = true };
        view2.Add (view);

        Assert.True (view.CanFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);

        view.CanFocus = false;
        Assert.False (view.CanFocus);
        Assert.False (view.HasFocus);
    }

    // TODO: Figure out what this test is supposed to be testing
    [Fact]
    public void CanFocus_Faced_With_Container ()
    {
        var t = new Runnable ();
        var w = new Window ();
        var f = new FrameView ();
        var v = new View { CanFocus = true };
        f.Add (v);
        w.Add (f);
        t.Add (w);

        Assert.True (t.CanFocus);
        Assert.True (w.CanFocus);
        Assert.True (f.CanFocus);
        Assert.True (v.CanFocus);

        f.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);

        v.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.False (v.CanFocus);

        v.CanFocus = true;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);
    }
    
    [Fact]
    public void CanFocus_True_Focuses ()
    {
        View view = new ()
        {
            Id = "view"
        };

        View superView = new ()
        {
            Id = "superView",
            CanFocus = true
        };

        superView.Add (view);

        superView.SetFocus ();
        Assert.True (superView.HasFocus);
        Assert.NotEqual (view, superView.Focused);

        view.CanFocus = true;
        Assert.True (superView.HasFocus);
        Assert.Equal (view, superView.Focused);
        Assert.True (view.HasFocus);

        view.CanFocus = false;
        Assert.True (superView.HasFocus);
        Assert.NotEqual (view, superView.Focused);
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void CanFocus_Set_True_Get_AdvanceFocus_Works ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> () { CanFocus = true });

        Label label = new () { Text = "label" };
        View view = new () { Text = "view", CanFocus = true };
        app.TopRunnableView!.Add (label, view);

        app.TopRunnableView.SetFocus ();
        Assert.Equal (view, app.Navigation!.GetFocused ());
        Assert.False (label.CanFocus);
        Assert.False (label.HasFocus);
        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        Assert.False (app.Navigation.AdvanceFocus (NavigationDirection.Forward, null));
        Assert.False (label.HasFocus);
        Assert.True (view.HasFocus);

        // Set label CanFocus to true
        label.CanFocus = true;
        Assert.False (label.HasFocus);
        Assert.True (view.HasFocus);

        // label can now be focused, so AdvanceFocus should move to it.
        Assert.True (app.Navigation.AdvanceFocus (NavigationDirection.Forward, null));
        Assert.True (label.HasFocus);
        Assert.False (view.HasFocus);

        // Move back to view
        view.SetFocus ();
        Assert.False (label.HasFocus);
        Assert.True (view.HasFocus);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.True (label.HasFocus);
        Assert.False (view.HasFocus);
    }
}
