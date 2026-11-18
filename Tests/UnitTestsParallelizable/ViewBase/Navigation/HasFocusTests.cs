using UnitTests;

namespace ViewBaseTests.Navigation;

public class HasFocusTests : TestsAllViews
{
    [Fact]
    public void HasFocus_False ()
    {
        var view = new View { Id = "view", CanFocus = true };

        view.SetFocus ();
        Assert.True (view.HasFocus);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void HasFocus_False_WithSuperView_Does_Not_Change_SuperView ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subview = new View { Id = "subview", CanFocus = true };
        view.Add (subview);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subview.HasFocus);

        subview.HasFocus = false;
        Assert.False (subview.HasFocus);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HasFocus_False_WithSubView_Leaves_All ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subview = new View { Id = "subview", CanFocus = true };
        view.Add (subview);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subview.HasFocus);
        Assert.Equal (subview, view.Focused);

        view.HasFocus = false;
        Assert.Null (view.Focused);
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
    }

    [Fact]
    public void Enabled_False_Sets_HasFocus_To_False ()
    {
        var wasClicked = false;
        var view = new Button { Text = "Click Me" };
        view.Accepting += (s, e) => wasClicked = !wasClicked;

        view.NewKeyDownEvent (Key.Space);
        Assert.True (wasClicked);
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed });
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased });
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonClicked });
        Assert.False (wasClicked);
        Assert.True (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        view.Enabled = false;
        view.NewKeyDownEvent (Key.Space);
        Assert.False (wasClicked);
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonPressed });
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonReleased });
        view.NewMouseEvent (new Mouse { Flags = MouseFlags.LeftButtonClicked });
        Assert.False (wasClicked);
        Assert.False (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void HasFocus_False_CompoundSubView_Leaves_All ()
    {
        var view = new View { Id = "view", CanFocus = true };

        var subView = new View { Id = "subView", CanFocus = true };

        var subViewSubView1 = new View { Id = "subViewSubView1", CanFocus = false };

        var subViewSubView2 = new View { Id = "subViewSubView2", CanFocus = true };

        var subViewSubView3 = new View { Id = "subViewSubView3", CanFocus = false };

        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
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
    }

    [Fact]
    public void HasFocus_False_SubView_Raises_HasFocusChanged ()
    {
        var top = new View { Id = "top", CanFocus = true };

        var subView1 = new View { Id = "subView1", CanFocus = true };

        var subView2 = new View { Id = "subView2", CanFocus = true };
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

        subView1.HasFocus = false; // this should have the same resuilt as top.AdvanceFocus (NavigationDirection.Forward, null);

        Assert.False (subView1.HasFocus);
        Assert.True (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (1, subView1HasFocusChangedFalseCount);

        Assert.Equal (1, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);
    }
}
