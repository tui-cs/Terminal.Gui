#nullable enable

// GitHub Copilot - Tests for View.CollectDim method

namespace ViewBaseTests.Layout;

public class CollectDimTests
{
    [Fact]
    public void CollectDim_WithDimAutoContainingDimView_DoesNotCauseAssertionFailure ()
    {
        // This test verifies that CollectDim handles DimAuto containing a DimView correctly.
        //
        // The bug: When HasInner was added to DimAuto/DimFill, Has<DimView>() would
        // find nested DimView instances. CollectDim then checks if dv.Target != this
        // and asserts `dim.ReferencesOtherViews()`. But the outer `dim` (DimAuto) doesn't
        // directly reference other views - only the nested DimView does.
        //
        // The fix: CollectDim should use direct type checking (`dim is DimView`)
        // instead of `dim.Has<DimView>()` to only process the top-level Dim type.

        View superView = new () { Width = 100, Height = 100 };
        View referenceView = new () { Width = 20, Height = 10 };
        superView.Add (referenceView);

        // Create a view with DimAuto that has a minimumContentDim referencing another view
        View childView = new ()
        {
            // DimAuto with minimumContentDim = Dim.Width(referenceView) (which is a DimView)
            Width = Dim.Auto (minimumContentDim: Dim.Width (referenceView)),
            Height = Dim.Auto (minimumContentDim: Dim.Height (referenceView))
        };

        superView.Add (childView);

        // This should not throw or cause assertion failure
        // Before the fix, the Debug.Assert(dim.ReferencesOtherViews()) would fail
        // because dim is DimAuto, not DimView, so ReferencesOtherViews() returns false
        superView.Layout ();

        // Verify layout completed successfully
        Assert.True (childView.Frame.Width >= 0);
        Assert.True (childView.Frame.Height >= 0);

        superView.Dispose ();
    }

    [Fact]
    public void CollectDim_WithDimFillContainingDimCombine_DoesNotRecurseIntoNestedCombine ()
    {
        // This test verifies that CollectDim does not incorrectly recurse into nested
        // DimCombine instances found inside DimFill's Margin property.

        View superView = new () { Width = 100, Height = 100 };

        // Create a view with DimFill that has a margin containing a DimCombine
        View childView = new ()
        {
            // DimFill with margin = Dim.Percent(10) + 5 (which is a DimCombine)
            Width = Dim.Fill (Dim.Percent (10) + 5),
            Height = Dim.Fill (Dim.Percent (10) + 5)
        };

        superView.Add (childView);

        // This should not throw or cause any issues
        superView.Layout ();

        // Verify layout completed successfully
        Assert.True (childView.Frame.Width >= 0);
        Assert.True (childView.Frame.Height >= 0);

        superView.Dispose ();
    }

    [Fact]
    public void CollectDim_WithDimAutoContainingDimCombine_DoesNotRecurseIntoNestedCombine ()
    {
        // This test verifies that CollectDim does not incorrectly recurse into nested
        // DimCombine instances found inside DimAuto's MinimumContentDim property.

        View superView = new () { Width = 100, Height = 100 };

        // Create a view with DimAuto that has a minimumContentDim containing a DimCombine
        View childView = new ()
        {
            // DimAuto with minimumContentDim = DimFill() + 5 (which is a DimCombine)
            Width = Dim.Auto (minimumContentDim: Dim.Fill () + 5),
            Height = Dim.Auto (minimumContentDim: Dim.Fill () + 5)
        };

        superView.Add (childView);

        // This should not throw or cause any issues
        superView.Layout ();

        // Verify layout completed successfully
        Assert.True (childView.Frame.Width >= 0);
        Assert.True (childView.Frame.Height >= 0);

        superView.Dispose ();
    }
}
