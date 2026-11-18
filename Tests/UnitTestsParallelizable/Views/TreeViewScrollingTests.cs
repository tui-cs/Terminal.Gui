// Copilot

using JetBrains.Annotations;

namespace ViewsTests;

/// <summary>
///     Tests for TreeView's integration with the View content-area system (Viewport, SetContentSize, ScrollBars).
/// </summary>
[TestSubject (typeof (TreeView))]
public class TreeViewScrollingTests
{
    #region Content Size

    [Fact]
    public void ContentSize_UpdatesOnExpand ()
    {
        TreeView<string> tree = CreateTree (out string root, out string _, out string _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        // Before expand: only root visible
        Assert.Equal (1, tree.GetContentHeight ());

        tree.Expand (root);

        // After expand: root + 2 children
        Assert.Equal (3, tree.GetContentHeight ());
    }

    [Fact]
    public void ContentSize_UpdatesOnCollapse ()
    {
        TreeView<string> tree = CreateTree (out string root, out string _, out string _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        tree.Expand (root);
        Assert.Equal (3, tree.GetContentHeight ());

        tree.Collapse (root);
        Assert.Equal (1, tree.GetContentHeight ());
    }

    [Fact]
    public void ContentSize_ReflectsMaxWidth ()
    {
        TreeView<string> tree = CreateTree (out string root, out string _, out string _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        tree.Expand (root);

        // Content width should reflect the widest branch (including expand symbols and indentation)
        int width = tree.GetContentWidth (false);
        Assert.True (width > 0);
    }

    #endregion

    #region Vertical Scrolling

    [Fact]
    public void ScrollOffsetVertical_ClampedToContentBounds ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        // 3 items, viewport height 2 → max scroll is 1
        tree.ScrollOffsetVertical = 100;
        Assert.Equal (1, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void ScrollOffsetVertical_CannotBeNegative ()
    {
        TreeView<string> tree = CreateTree (out _, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        tree.ScrollOffsetVertical = -50;
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void ScrollOffsetVertical_DelegatesToViewport ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        tree.ScrollOffsetVertical = 1;
        Assert.Equal (1, tree.Viewport.Y);

        tree.ScrollOffsetVertical = 0;
        Assert.Equal (0, tree.Viewport.Y);
    }

    [Fact]
    public void ScrollDown_IncrementsViewport ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        // Start at top
        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.ScrollDown ();
        Assert.Equal (1, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void ScrollDown_StopsAtBottom ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        // 3 items, viewport 2 → max scroll is 1
        tree.ScrollDown ();
        Assert.Equal (1, tree.ScrollOffsetVertical);

        // Should not scroll further
        tree.ScrollDown ();
        Assert.Equal (1, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void ScrollUp_DecrementsViewport ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        tree.ScrollOffsetVertical = 1;
        tree.ScrollUp ();
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void ScrollUp_StopsAtTop ()
    {
        TreeView<string> tree = CreateTree (out string _, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        tree.ScrollUp ();
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    #endregion

    #region Horizontal Scrolling

    [Fact]
    public void ScrollOffsetHorizontal_DelegatesToViewport ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 5, 10);

        tree.Expand (root);

        tree.ScrollOffsetHorizontal = 2;
        Assert.Equal (2, tree.Viewport.X);
    }

    [Fact]
    public void ScrollOffsetHorizontal_CannotBeNegative ()
    {
        TreeView<string> tree = CreateTree (out _, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        tree.ScrollOffsetHorizontal = -10;
        Assert.Equal (0, tree.ScrollOffsetHorizontal);
    }

    #endregion

    #region EnsureVisible

    [Fact]
    public void EnsureVisible_ScrollsDownToShowItem ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out string child2);
        tree.Frame = new Rectangle (0, 0, 40, 1);

        tree.Expand (root);

        // child2 is at index 2, viewport height 1, so need to scroll to 2
        tree.EnsureVisible (child2);
        Assert.Equal (2, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void EnsureVisible_ScrollsUpToShowItem ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 1);

        tree.Expand (root);

        tree.ScrollOffsetVertical = 2;
        tree.EnsureVisible (root);
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void EnsureVisible_NoOpWhenAlreadyVisible ()
    {
        TreeView<string> tree = CreateTree (out string root, out string child1, out _);
        tree.Frame = new Rectangle (0, 0, 40, 5);

        tree.Expand (root);

        // All items fit in the viewport, so EnsureVisible should not scroll
        tree.EnsureVisible (child1);
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void EnsureVisible_NullModel_NoOp ()
    {
        TreeView<string> tree = CreateTree (out _, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        tree.EnsureVisible (null);
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    #endregion

    #region GoTo / GoToFirst / GoToEnd

    [Fact]
    public void GoToEnd_ScrollsToLastItem ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out string child2);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        tree.GoToEnd ();

        Assert.Equal (child2, tree.SelectedObject);

        // 3 items, viewport 2 → scroll to 1
        Assert.Equal (1, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void GoToFirst_ResetsScroll ()
    {
        TreeView<string> tree = CreateTree (out string root, out _, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        tree.ScrollOffsetVertical = 1;
        tree.GoToFirst ();

        Assert.Equal (root, tree.SelectedObject);
        Assert.Equal (0, tree.ScrollOffsetVertical);
    }

    #endregion

    #region ExpandCollapse + Scroll Interaction

    [Fact]
    public void ExpandAll_UpdatesContentSize ()
    {
        TreeView<string> tree = CreateDeepTree (out string root);
        tree.Frame = new Rectangle (0, 0, 40, 10);

        Assert.Equal (1, tree.GetContentHeight ());

        tree.ExpandAll (root);

        Assert.Equal (3, tree.GetContentHeight ());
    }

    #endregion

    #region HitTest + GetObjectOnRow

    [Fact]
    public void GetObjectOnRow_ReturnsCorrectItemAfterScroll ()
    {
        TreeView<string> tree = CreateTree (out string root, out string child1, out string child2);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        // Initially row 0 shows root, row 1 shows child1
        Assert.Equal (root, tree.GetObjectOnRow (0));
        Assert.Equal (child1, tree.GetObjectOnRow (1));

        // After scrolling down 1, row 0 shows child1, row 1 shows child2
        tree.ScrollOffsetVertical = 1;
        Assert.Equal (child1, tree.GetObjectOnRow (0));
        Assert.Equal (child2, tree.GetObjectOnRow (1));
    }

    [Fact]
    public void GetObjectRow_ReturnsCorrectRowAfterScroll ()
    {
        TreeView<string> tree = CreateTree (out string root, out string child1, out _);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        // Root is at index 0, child1 at index 1
        Assert.Equal (0, tree.GetObjectRow (root));
        Assert.Equal (1, tree.GetObjectRow (child1));

        // After scrolling down 1, root is at row -1, child1 at row 0
        tree.ScrollOffsetVertical = 1;
        Assert.Equal (-1, tree.GetObjectRow (root));
        Assert.Equal (0, tree.GetObjectRow (child1));
    }

    #endregion

    #region Navigation + Scroll

    [Fact]
    public void AdjustSelection_Down_ScrollsWhenNeeded ()
    {
        TreeView<string> tree = CreateTree (out string root, out string child1, out string child2);
        tree.Frame = new Rectangle (0, 0, 40, 2);

        tree.Expand (root);

        tree.SelectedObject = root;
        Assert.Equal (0, tree.ScrollOffsetVertical);

        // Move down twice: first to child1 (still in viewport), then to child2 (needs scroll)
        tree.AdjustSelection (1);
        Assert.Equal (child1, tree.SelectedObject);
        Assert.Equal (0, tree.ScrollOffsetVertical);

        tree.AdjustSelection (1);
        Assert.Equal (child2, tree.SelectedObject);
        Assert.Equal (1, tree.ScrollOffsetVertical);
    }

    [Fact]
    public void PageDown_ScrollsCorrectly ()
    {
        TreeView<string> tree = CreateManyItemTree (10);
        tree.Frame = new Rectangle (0, 0, 40, 3);

        tree.SelectedObject = tree.Objects?.First ();
        tree.MovePageDown ();

        // After page down, should have scrolled by viewport height
        Assert.True (tree.ScrollOffsetVertical > 0);
    }

    #endregion

    #region ViewportSettings

    [Fact]
    public void Constructor_EnablesScrollBars ()
    {
        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false));

        Assert.True (tree.ViewportSettings.FastHasFlags (ViewportSettingsFlags.HasVerticalScrollBar));
        Assert.True (tree.ViewportSettings.FastHasFlags (ViewportSettingsFlags.HasHorizontalScrollBar));
    }

    #endregion

    #region Test Setup

    private TreeView<string> CreateTree (out string root, out string child1, out string child2)
    {
        root = "Root";
        child1 = "Child1";
        child2 = "Child2";

        string capturedRoot = root;
        string capturedChild1 = child1;
        string capturedChild2 = child2;

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (s => s == capturedRoot ? [capturedChild1, capturedChild2] : [], s => s == capturedRoot));
        tree.AddObject (root);
        tree.BeginInit ();
        tree.EndInit ();

        return tree;
    }

    private TreeView<string> CreateDeepTree (out string root)
    {
        root = "Root";
        var child = "Child";
        var grandchild = "Grandchild";

        string capturedRoot = root;

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (s =>
                                                                      {
                                                                          if (s == capturedRoot)
                                                                          {
                                                                              return [child];
                                                                          }

                                                                          if (s == child)
                                                                          {
                                                                              return [grandchild];
                                                                          }

                                                                          return [];
                                                                      },
                                                                      s => s == capturedRoot || s == child));
        tree.AddObject (root);
        tree.BeginInit ();
        tree.EndInit ();

        return tree;
    }

    private TreeView<string> CreateManyItemTree (int count)
    {
        List<string> items = Enumerable.Range (0, count).Select (i => $"Item{i}").ToList ();

        TreeView<string> tree = new (new DelegateTreeBuilder<string> (_ => [], _ => false));

        foreach (string item in items)
        {
            tree.AddObject (item);
        }

        tree.BeginInit ();
        tree.EndInit ();

        return tree;
    }

    #endregion
}
