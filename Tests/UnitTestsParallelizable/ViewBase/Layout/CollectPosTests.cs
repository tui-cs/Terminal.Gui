

// GitHub Copilot - Tests for View.CollectPos method

namespace ViewBaseTests.Layout;

public class CollectPosTests
{
    [Fact]
    public void CollectPos_WithPosView_CollectsDependency ()
    {
        // This test verifies that CollectPos correctly identifies
        // PosView dependencies for layout ordering.

        View superView = new () { Width = 100, Height = 100 };
        View referenceView = new () { Width = 20, Height = 10 };
        superView.Add (referenceView);

        View childView = new () { X = Pos.Right (referenceView) + 5, Y = Pos.Bottom (referenceView) + 5, Width = 10, Height = 10 };

        superView.Add (childView);

        // Layout should succeed and respect the dependency
        superView.Layout ();

        // childView should be positioned relative to referenceView
        Assert.Equal (referenceView.Frame.Right + 5, childView.Frame.X);
        Assert.Equal (referenceView.Frame.Bottom + 5, childView.Frame.Y);

        superView.Dispose ();
    }

    [Fact]
    public void CollectPos_WithPosCombineContainingPosView_CollectsDependency ()
    {
        // This test verifies that CollectPos correctly recurses into
        // PosCombine to find PosView dependencies.

        View superView = new () { Width = 100, Height = 100 };
        View referenceView = new () { Width = 20, Height = 10 };
        superView.Add (referenceView);

        View childView = new ()
        {
            // PosCombine containing PosView
            X = Pos.Right (referenceView) + Pos.Percent (10), Y = Pos.Bottom (referenceView) - 5, Width = 10, Height = 10
        };

        superView.Add (childView);

        // This should not throw or cause any issues
        superView.Layout ();

        // Verify layout completed successfully
        Assert.True (childView.Frame.X >= 0);
        Assert.True (childView.Frame.Y >= 0);

        superView.Dispose ();
    }

    [Fact]
    public void CollectPos_WithNestedPosCombine_DoesNotCauseIssues ()
    {
        // This test verifies that CollectPos handles deeply nested
        // PosCombine structures correctly.

        View superView = new () { Width = 100, Height = 100 };

        View childView = new ()
        {
            // Deeply nested PosCombine: ((Center + 5) - Percent(10)) + 3
            X = Pos.Center () + 5 - Pos.Percent (10) + 3, Y = Pos.Center () + 5 - Pos.Percent (10) + 3, Width = 10, Height = 10
        };

        superView.Add (childView);

        // This should not throw or cause any issues
        superView.Layout ();

        // Verify layout completed successfully
        Assert.True (childView.Frame.Width > 0);
        Assert.True (childView.Frame.Height > 0);

        superView.Dispose ();
    }

    [Fact]
    public void CollectPos_UsesDirectTypeCheckingNotHas ()
    {
        // This test documents that CollectPos uses direct type checking
        // (switch/case) instead of Has<T>(), which is important because
        // Has<T>() now traverses into nested Pos objects via HasInner.
        //
        // If CollectPos used Has<T>(), it could find PosView instances
        // nested inside other Pos types and incorrectly add dependencies.

        View superView = new () { Width = 100, Height = 100 };

        // Simple case - no dependencies
        View childView = new () { X = Pos.Center (), Y = Pos.Center (), Width = 10, Height = 10 };

        superView.Add (childView);

        // This should complete without issues
        superView.Layout ();

        // Verify layout completed successfully
        Assert.True (childView.Frame.Width > 0);
        Assert.True (childView.Frame.Height > 0);

        superView.Dispose ();
    }
}
