// Claude - Opus 4.8

namespace ViewBaseTests.Layout;

/// <summary>
///     Regression guards for the deterministic-layout properties relevant to issue #4522
///     (<c>NeedsLayout</c> is buggy). These pin the behavior established by #5357 (split upward /
///     downward <see cref="View.SetNeedsLayout"/> propagation), #5358 and #5359 (region-aware,
///     non-escalating draw): a single property change must converge in one layout pass, must not
///     change frames it didn't need to, and must not fan out layout work across sibling subtrees.
/// </summary>
/// <remarks>
///     Before those fixes, changing one view re-laid-out and redrew large portions of the tree
///     (see the analysis in #4522 and the measured fan-out in #4973). These tests fail loudly if
///     that regression returns.
/// </remarks>
public class LayoutConvergenceTests (ITestOutputHelper output)
{
    private sealed class Counters
    {
        public int LaidOut;
        public int FrameChanged;

        public void Attach (View v)
        {
            v.SubViewsLaidOut += (_, _) => LaidOut++;
            v.FrameChanged += (_, _) => FrameChanged++;
        }
    }

    private static void ClearTree (View v)
    {
        v.NeedsLayout = false;

        foreach (View sv in v.SubViews)
        {
            ClearTree (sv);
        }
    }

    // Builds root -> chain of `depth` levels, with `breadth` ABSOLUTE-sized siblings at each level.
    // The chain follows the first sibling at each level. Returns the deepest leaf to mutate.
    private static (View root, View leaf, List<View> all) BuildAbsoluteTree (int depth, int breadth)
    {
        View root = new () { Id = "root", Width = 200, Height = 200 };
        List<View> all = [root];
        View current = root;
        View leaf = root;

        for (var d = 0; d < depth; d++)
        {
            View next = current;

            for (var b = 0; b < breadth; b++)
            {
                View subView = new () { Id = $"d{d}b{b}", X = b * 10, Y = d, Width = 8, Height = 1 };
                current.Add (subView);
                all.Add (subView);

                if (b == 0)
                {
                    next = subView;
                }
            }

            current = next;
            leaf = current;
        }

        return (root, leaf, all);
    }

    [Theory]
    [InlineData (3, 3)]
    [InlineData (6, 3)]
    [InlineData (8, 4)]
    public void Absolute_Child_Change_Converges_Single_Pass_Without_FanOut (int depth, int breadth)
    {
        (View root, View leaf, List<View> all) = BuildAbsoluteTree (depth, breadth);
        root.BeginInit ();
        root.EndInit ();
        root.Layout ();

        Counters c = new ();

        foreach (View v in all)
        {
            c.Attach (v);
        }

        ClearTree (root);

        // A single absolute-size change on the deepest leaf.
        leaf.Width = leaf.Frame.Width + 1;

        // Drive application-style layout passes; a healthy engine converges immediately.
        var passes = 0;

        while (root.NeedsLayout && passes < 20)
        {
            root.Layout ();
            passes++;
        }

        output.WriteLine ($"depth={depth} breadth={breadth} totalViews={all.Count} passes={passes} laidOut={c.LaidOut} frameChanged={c.FrameChanged}");

        // Converges in a single application layout pass — no multi-iteration thrashing (Bug #6).
        Assert.Equal (1, passes);

        // Only the leaf's frame actually changed — no spurious frame churn up/across the tree (Bug #1).
        Assert.Equal (1, c.FrameChanged);

        // Layout work stays on the affected ancestor chain (root..leaf); siblings/cousins do not
        // re-run LayoutSubViews. The chain is depth+1 views; allow one extra for the root pass.
        Assert.True (
                     c.LaidOut <= depth + 2,
                     $"Layout fan-out {c.LaidOut} exceeded the ancestor chain bound {depth + 2}.");

        // And with wide breadth the fan-out is far below the total view count (no subtree fan-out, #5357).
        Assert.True (
                     c.LaidOut < all.Count,
                     $"Layout fan-out {c.LaidOut} touched the whole tree of {all.Count} views.");

        root.Dispose ();
    }

    [Fact]
    public void DimAuto_Parent_Converges_Single_Pass_And_Tracks_Child_Growth ()
    {
        View root = new () { Id = "root", Width = 200, Height = 200 };
        View autoParent = new () { Id = "autoParent", Width = Dim.Auto (), Height = Dim.Auto () };
        View subView = new () { Id = "subView", Width = 10, Height = 3 };
        autoParent.Add (subView);
        root.Add (autoParent);

        root.BeginInit ();
        root.EndInit ();
        root.Layout ();

        subView.Width = 40;
        var passes = 0;

        while (root.NeedsLayout && passes < 20)
        {
            root.Layout ();
            passes++;
        }

        output.WriteLine ($"Dim.Auto parent grew to {autoParent.Frame.Size} in {passes} pass(es)");

        // The parent must track the child's new width and converge in a single pass.
        // (In this test the upward mark from child.Width = 40 is sufficient; the SetFrame
        // upward mark is not additionally exercised by this synthetic case.)
        Assert.Equal (40, autoParent.Frame.Width);
        Assert.Equal (1, passes);

        root.Dispose ();
    }

    [Fact]
    public void Sibling_Reference_ReLayout_Stays_Single_Pass ()
    {
        // A sibling that references the changed view (Pos.Right) must be repositioned — this is the
        // case where the ancestor re-layout is necessary, not wasteful. It must still be single-pass.
        View root = new () { Id = "root", Width = 80, Height = 25 };
        View anchor = new () { Id = "anchor", X = 0, Y = 0, Width = 10, Height = 1 };
        View follower = new () { Id = "follower", X = Pos.Right (anchor), Y = 0, Width = 5, Height = 1 };
        root.Add (anchor, follower);

        root.BeginInit ();
        root.EndInit ();
        root.Layout ();

        Assert.Equal (10, follower.Frame.X);

        anchor.Width = 20;

        var passes = 0;

        while (root.NeedsLayout && passes < 20)
        {
            root.Layout ();
            passes++;
        }

        output.WriteLine ($"follower repositioned to X={follower.Frame.X} in {passes} pass(es)");

        // The follower must track the anchor's new right edge, and converge in one pass.
        Assert.Equal (20, follower.Frame.X);
        Assert.Equal (1, passes);

        root.Dispose ();
    }
}
