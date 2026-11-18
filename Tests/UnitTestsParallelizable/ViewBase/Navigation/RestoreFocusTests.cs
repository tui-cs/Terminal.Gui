using UnitTests;

namespace ViewBaseTests.Navigation;


public class RestoreFocusTests () : TestsAllViews
{
    [Fact]
    public void RestoreFocus_Restores ()
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
            CanFocus = true
        };

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = true
        };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.True (subViewSubView1.HasFocus);
        Assert.Equal (subViewSubView1, subView.Focused);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.RestoreFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView1, subView.Focused);
        Assert.True (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        subViewSubView2.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.True (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.RestoreFocus ();
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.True (subViewSubView2.HasFocus);
        Assert.Equal (subViewSubView2, subView.Focused);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView3.HasFocus);
    }

    [Fact]
    public void RestoreFocus_Across_TabGroup ()
    {
        var top = new View
        {
            Id = "top",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var tabGroup1 = new View
        {
            Id = "tabGroup1",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var tabGroup1SubView1 = new View
        {
            Id = "tabGroup1SubView1",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        var tabGroup1SubView2 = new View
        {
            Id = "tabGroup1SubView2",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        tabGroup1.Add (tabGroup1SubView1, tabGroup1SubView2);

        var tabGroup2 = new View
        {
            Id = "tabGroup2",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var tabGroup2SubView1 = new View
        {
            Id = "tabGroup2SubView1",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        var tabGroup2SubView2 = new View
        {
            Id = "tabGroup2SubView2",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        tabGroup2.Add (tabGroup2SubView1, tabGroup2SubView2);

        top.Add (tabGroup1, tabGroup2);

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (tabGroup1, top.Focused);
        Assert.Equal (tabGroup1SubView1, tabGroup1.Focused);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        Assert.True (top.HasFocus);
        Assert.Equal (tabGroup2, top.Focused);
        Assert.Equal (tabGroup2SubView1, tabGroup2.Focused);

        top.HasFocus = false;
        Assert.False (top.HasFocus);

        top.RestoreFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (tabGroup2, top.Focused);
        Assert.Equal (tabGroup2SubView1, tabGroup2.Focused);

        top.HasFocus = false;
        Assert.False (top.HasFocus);

        top.RestoreFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (tabGroup2, top.Focused);
        Assert.Equal (tabGroup2SubView1, tabGroup2.Focused);


    }
}
