using UnitTests;

namespace ViewBaseTests.Navigation;


public class AddRemoveNavigationTests () : TestsAllViews
{
    [Fact]
    public void Add_First_SubView_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        top.Add (subView);

        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void Add_Subsequent_SubView_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        top.Add (subView);

        Assert.True (subView.HasFocus);

        View subView2 = new View ()
        {
            Id = "subView2",
            CanFocus = true
        };

        top.Add (subView2);

        Assert.True (subView2.HasFocus);


    }

    [Fact]
    public void Add_Nested_SubViews_Deepest_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        View subSubView = new View ()
        {
            Id = "subSubView",
            CanFocus = true
        };

        subView.Add (subSubView);

        top.Add (subView);

        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);
    }


    [Fact]
    public void Remove_SubView_Raises_HasFocusChanged ()
    {
        var top = new View
        {
            Id = "top",
            CanFocus = true
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true
        };
        top.Add (subView1, subView2);

        var subView1HasFocusChangedTrueCount = 0;
        var subView1HasFocusChangedFalseCount = 0;

        subView1.HasFocusChanged += (s, e) =>
        {
            if (e.NewValue)
            {
                subView1HasFocusChangedTrueCount++;
            }
            else
            {
                subView1HasFocusChangedFalseCount++;
            }
        };

        var subView2HasFocusChangedTrueCount = 0;
        var subView2HasFocusChangedFalseCount = 0;

        subView2.HasFocusChanged += (s, e) =>
        {
            if (e.NewValue)
            {
                subView2HasFocusChangedTrueCount++;
            }
            else
            {
                subView2HasFocusChangedFalseCount++;
            }
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (0, subView1HasFocusChangedFalseCount);

        Assert.Equal (0, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);

        top.Remove (subView1); // this should have the same resuilt as top.AdvanceFocus (NavigationDirection.Forward, null);

        Assert.False (subView1.HasFocus);
        Assert.True (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (1, subView1HasFocusChangedFalseCount);

        Assert.Equal (1, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);
    }


    [Fact]
    public void Remove_Focused_SubView_Keeps_Focus_And_SubView_Looses_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        top.Add (subView);

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);

        top.Remove (subView);
        Assert.True (top.HasFocus);
        Assert.Null (top.Focused);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Remove_Focused_SubView_Keeps_Focus_And_SubView_Looses_Focus_And_Next_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        View subView1 = new View ()
        {
            Id = "subView1",
            CanFocus = true
        };

        View subView2 = new View ()
        {
            Id = "subView2",
            CanFocus = true
        };
        top.Add (subView1, subView2);

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (subView1, top.Focused);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        top.Remove (subView1);

        Assert.True (top.HasFocus);
        Assert.True (subView2.HasFocus);
        Assert.Equal (subView2, top.Focused);
        Assert.False (subView1.HasFocus);
    }
}
