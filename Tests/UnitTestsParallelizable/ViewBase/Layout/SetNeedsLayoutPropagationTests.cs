namespace ViewBaseTests.Layout;

// Copilot

/// <summary>
///     Tests for the split between downward subtree invalidation and upward ancestor-only
///     propagation in <see cref="View.SetNeedsLayout"/>. Issue #5357.
/// </summary>
/// <remarks>
///     Before #5357, calling <see cref="View.SetNeedsLayout"/> on a deep SubView marked every
///     sibling subtree dirty because the upward recursion re-entered the downward cascade. These
///     tests pin the new contract: ancestors and the changed view's own subtree are marked, but
///     unaffected sibling subtrees stay clean.
/// </remarks>
public class SetNeedsLayoutPropagationTests
{
    private static void ClearAllNeedsLayout (View root)
    {
        root.NeedsLayout = false;

        foreach (View sv in root.SubViews)
        {
            ClearAllNeedsLayout (sv);
        }

        if (root.Margin.View is { })
        {
            ClearAllNeedsLayout (root.Margin.View);
        }

        if (root.Border.View is { })
        {
            ClearAllNeedsLayout (root.Border.View);
        }

        if (root.Padding.View is { })
        {
            ClearAllNeedsLayout (root.Padding.View);
        }
    }

    [Fact]
    public void SetNeedsLayout_OnSubView_Does_Not_Mark_Sibling_Subtree ()
    {
        View root = new () { Id = "root" };

        View sibling1 = new () { Id = "sibling1" };
        View sibling1Child = new () { Id = "sibling1Child" };
        sibling1.Add (sibling1Child);

        View sibling2 = new () { Id = "sibling2" };
        View sibling2Child = new () { Id = "sibling2Child" };
        sibling2.Add (sibling2Child);

        root.Add (sibling1, sibling2);

        ClearAllNeedsLayout (root);

        sibling1.SetNeedsLayout ();

        Assert.True (sibling1.NeedsLayout, "Changed view must be marked.");
        Assert.True (sibling1Child.NeedsLayout, "Changed view's subtree must be marked.");
        Assert.True (root.NeedsLayout, "Ancestor must be marked.");

        Assert.False (sibling2.NeedsLayout, "Sibling must not be re-marked.");
        Assert.False (sibling2Child.NeedsLayout, "Sibling's subtree must not be re-marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_OnDeepSubView_Marks_All_Ancestors ()
    {
        View root = new () { Id = "root" };
        View middle = new () { Id = "middle" };
        View deep = new () { Id = "deep" };

        middle.Add (deep);
        root.Add (middle);

        ClearAllNeedsLayout (root);

        deep.SetNeedsLayout ();

        Assert.True (deep.NeedsLayout);
        Assert.True (middle.NeedsLayout);
        Assert.True (root.NeedsLayout);

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_OnSubView_Cascades_Through_Own_Subtree ()
    {
        View root = new () { Id = "root" };
        View middle = new () { Id = "middle" };
        View deep1 = new () { Id = "deep1" };
        View deep2 = new () { Id = "deep2" };

        middle.Add (deep1, deep2);
        root.Add (middle);

        ClearAllNeedsLayout (root);

        middle.SetNeedsLayout ();

        Assert.True (middle.NeedsLayout);
        Assert.True (deep1.NeedsLayout, "Changed view's whole subtree must be marked.");
        Assert.True (deep2.NeedsLayout, "Changed view's whole subtree must be marked.");
        Assert.True (root.NeedsLayout);

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_On_Cousin_Does_Not_Mark_Other_Branch ()
    {
        View root = new () { Id = "root" };

        View branchA = new () { Id = "branchA" };
        View branchAChild = new () { Id = "branchAChild" };
        branchA.Add (branchAChild);

        View branchB = new () { Id = "branchB" };
        View branchBChild = new () { Id = "branchBChild" };
        View branchBGrand = new () { Id = "branchBGrand" };
        branchBChild.Add (branchBGrand);
        branchB.Add (branchBChild);

        root.Add (branchA, branchB);

        ClearAllNeedsLayout (root);

        branchBGrand.SetNeedsLayout ();

        Assert.True (branchBGrand.NeedsLayout);
        Assert.True (branchBChild.NeedsLayout);
        Assert.True (branchB.NeedsLayout);
        Assert.True (root.NeedsLayout);

        Assert.False (branchA.NeedsLayout, "Other branch root must not be re-marked.");
        Assert.False (branchAChild.NeedsLayout, "Other branch's subtree must not be re-marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_OnSubView_Does_Not_Mark_Ancestor_Adornment_Subtree ()
    {
        View root = new () { Id = "root" };
        View rootBorderSubView = new () { Id = "rootBorderSubView" };
        root.Border.GetOrCreateView ().Add (rootBorderSubView);

        View branch = new () { Id = "branch" };
        View branchChild = new () { Id = "branchChild" };
        branch.Add (branchChild);
        root.Add (branch);

        ClearAllNeedsLayout (root);

        branchChild.SetNeedsLayout ();

        Assert.True (branchChild.NeedsLayout);
        Assert.True (branch.NeedsLayout);
        Assert.True (root.NeedsLayout);

        Assert.False (root.Border.View!.NeedsLayout, "Uninvolved ancestor adornment view must not be re-marked.");
        Assert.False (rootBorderSubView.NeedsLayout, "Uninvolved ancestor adornment subtree must not be re-marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_Preserves_Adornment_SubView_Marking_On_Changed_View ()
    {
        View root = new () { Id = "root" };
        View view = new () { Id = "view" };
        View borderSubView = new () { Id = "borderSubView" };
        view.Border.GetOrCreateView ().Add (borderSubView);
        root.Add (view);

        ClearAllNeedsLayout (root);

        view.SetNeedsLayout ();

        Assert.True (view.NeedsLayout);
        Assert.True (view.Border.View!.NeedsLayout, "Adornment view must be marked when its parent is the changed view.");
        Assert.True (borderSubView.NeedsLayout, "Adornment subview must be marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_From_AdornmentSubView_Reaches_AdornmentParent ()
    {
        View root = new () { Id = "root" };
        View view = new () { Id = "view" };
        View borderSubView = new () { Id = "borderSubView" };
        view.Border.GetOrCreateView ().Add (borderSubView);
        root.Add (view);

        ClearAllNeedsLayout (root);

        borderSubView.SetNeedsLayout ();

        Assert.True (borderSubView.NeedsLayout);
        Assert.True (view.Border.View!.NeedsLayout, "AdornmentView must be marked as ancestor.");
        Assert.True (view.NeedsLayout, "AdornmentView.Parent must be marked.");
        Assert.True (root.NeedsLayout, "Adornment parent's SuperView must be marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_On_AdornmentView_Directly_Reaches_AdornmentParent_Without_Marking_Siblings ()
    {
        View root = new () { Id = "root" };
        View view = new () { Id = "view" };
        View sibling = new () { Id = "sibling" };
        View borderSubView = new () { Id = "borderSubView" };
        view.Border.GetOrCreateView ().Add (borderSubView);
        root.Add (view, sibling);

        ClearAllNeedsLayout (root);

        view.Border.View!.SetNeedsLayout ();

        Assert.True (view.Border.View.NeedsLayout, "AdornmentView must be marked.");
        Assert.True (borderSubView.NeedsLayout, "AdornmentView subtree must be marked.");
        Assert.True (view.NeedsLayout, "Adornment parent must be marked.");
        Assert.True (root.NeedsLayout, "Adornment parent's SuperView must be marked.");
        Assert.False (sibling.NeedsLayout, "Sibling subtree must not be marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_Tabs_Active_Scroll_Does_Not_Mark_Inactive_Pages ()
    {
        View root = new () { Id = "root", Width = 60, Height = 20 };
        Tabs tabs = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        root.Add (tabs);

        View page1 = new () { Id = "page1", Title = "Tab1", Width = Dim.Fill (), Height = Dim.Fill () };
        View page1Child = new () { Id = "page1Child" };
        page1.Add (page1Child);

        View page2 = new () { Id = "page2", Title = "Tab2", Width = Dim.Fill (), Height = Dim.Fill () };
        View page2Child = new () { Id = "page2Child" };
        page2.Add (page2Child);

        View page3 = new () { Id = "page3", Title = "Tab3", Width = Dim.Fill (), Height = Dim.Fill () };
        View page3Child = new () { Id = "page3Child" };
        page3.Add (page3Child);

        tabs.Add (page1, page2, page3);
        root.Layout ();

        ClearAllNeedsLayout (root);

        page1.SetNeedsLayout ();

        Assert.True (page1.NeedsLayout);
        Assert.True (page1Child.NeedsLayout);
        Assert.True (tabs.NeedsLayout);
        Assert.True (root.NeedsLayout);

        Assert.False (page2.NeedsLayout, "Inactive tab page must not be re-marked.");
        Assert.False (page2Child.NeedsLayout, "Inactive tab page subtree must not be re-marked.");
        Assert.False (page3.NeedsLayout, "Inactive tab page must not be re-marked.");
        Assert.False (page3Child.NeedsLayout, "Inactive tab page subtree must not be re-marked.");

        root.Dispose ();
    }

    [Fact]
    public void SetNeedsLayout_Tabs_Active_Scroll_Does_Not_Redraw_Inactive_Pages_After_Layout ()
    {
        View root = new () { Id = "root", Width = 60, Height = 20 };
        Tabs tabs = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        root.Add (tabs);

        View page1 = new () { Id = "page1", Title = "Tab1", Width = Dim.Fill (), Height = Dim.Fill () };
        View page1Child = new () { Id = "page1Child", Width = 10, Height = 1 };
        page1.Add (page1Child);

        View page2 = new () { Id = "page2", Title = "Tab2", Width = Dim.Fill (), Height = Dim.Fill () };
        View page2Child = new () { Id = "page2Child", Width = 10, Height = 1 };
        page2.Add (page2Child);

        tabs.Add (page1, page2);
        root.Layout ();
        root.ClearNeedsDraw ();
        ClearAllNeedsLayout (root);

        page1.SetNeedsLayout ();
        root.Layout ();

        Assert.False (page2.NeedsDraw, "Inactive tab page must not be redrawn.");
        Assert.False (page2Child.NeedsDraw, "Inactive tab page subtree must not be redrawn.");

        root.Dispose ();
    }
}
