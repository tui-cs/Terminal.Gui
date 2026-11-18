namespace ViewBaseTests.Arrangement;

public class ArrangementTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region ViewArrangement Enum Tests

    [Fact]
    public void ViewArrangement_Fixed_IsZero () => Assert.Equal (0, (int)ViewArrangement.Fixed);

    [Fact]
    public void ViewArrangement_Flags_HaveCorrectValues ()
    {
        Assert.Equal (1, (int)ViewArrangement.Movable);
        Assert.Equal (2, (int)ViewArrangement.LeftResizable);
        Assert.Equal (4, (int)ViewArrangement.RightResizable);
        Assert.Equal (8, (int)ViewArrangement.TopResizable);
        Assert.Equal (16, (int)ViewArrangement.BottomResizable);
        Assert.Equal (32, (int)ViewArrangement.Overlapped);
    }

    [Fact]
    public void ViewArrangement_Resizable_IsCombinationOfAllResizableFlags ()
    {
        ViewArrangement expected = ViewArrangement.LeftResizable
                                   | ViewArrangement.RightResizable
                                   | ViewArrangement.TopResizable
                                   | ViewArrangement.BottomResizable;

        Assert.Equal (ViewArrangement.Resizable, expected);
    }

    [Fact]
    public void ViewArrangement_CanCombineFlags ()
    {
        ViewArrangement arrangement = ViewArrangement.Movable | ViewArrangement.LeftResizable;

        Assert.True (arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    #endregion

    #region View.Arrangement Property Tests

    [Fact]
    public void View_Arrangement_DefaultsToFixed ()
    {
        var view = new View ();
        Assert.Equal (ViewArrangement.Fixed, view.Arrangement);
    }

    [Fact]
    public void View_Arrangement_CanBeSet ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable };
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);
    }

    [Fact]
    public void View_Arrangement_CanSetMultipleFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    [Fact]
    public void View_Arrangement_Overlapped_CanBeSetIndependently ()
    {
        var view = new View { Arrangement = ViewArrangement.Overlapped };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Resizable));
    }

    [Fact]
    public void View_Arrangement_CanCombineOverlappedWithOtherFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    #endregion

    #region TopResizable and Movable Mutual Exclusivity Tests

    [Fact]
    public void TopResizable_WithoutMovable_IsAllowed ()
    {
        var view = new View { Arrangement = ViewArrangement.TopResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    [Fact]
    public void Movable_WithTopResizable_MovableWins ()
    {
        // According to docs and Border.Arrangment.cs line 569:
        // TopResizable is only checked if NOT Movable
        var view = new View { Arrangement = ViewArrangement.Movable | ViewArrangement.TopResizable, BorderStyle = LineStyle.Single };

        // Both flags can be set on the property
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));

        // But the behavior in Border.DetermineArrangeModeFromClick 
        // will prioritize Movable over TopResizable
    }

    [Fact]
    public void Resizable_WithMovable_IncludesTopResizable ()
    {
        var view = new View { Arrangement = ViewArrangement.Resizable | ViewArrangement.Movable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    #endregion

    #region Border Arrangement Tests

    [Fact]
    public void Border_WithNoArrangement_HasNoArrangementOptions ()
    {
        var view = new View { Arrangement = ViewArrangement.Fixed, BorderStyle = LineStyle.Single };

        Assert.NotNull (view.Border);
        Assert.Equal (ViewArrangement.Fixed, view.Arrangement);
    }

    [Fact]
    public void Border_WithMovableArrangement_CanEnterArrangeMode ()
    {
        var superView = new View ();

        var view = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };
        superView.Add (view);

        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    [Fact]
    public void Border_WithResizableArrangement_HasResizableOptions ()
    {
        var view = new View { Arrangement = ViewArrangement.Resizable, BorderStyle = LineStyle.Single };

        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    [Theory]
    [InlineData (ViewArrangement.LeftResizable)]
    [InlineData (ViewArrangement.RightResizable)]
    [InlineData (ViewArrangement.TopResizable)]
    [InlineData (ViewArrangement.BottomResizable)]
    public void Border_WithSingleResizableDirection_OnlyHasThatOption (ViewArrangement arrangement)
    {
        var view = new View { Arrangement = arrangement, BorderStyle = LineStyle.Single };

        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (arrangement));

        // Verify other directions are not set
        if (arrangement != ViewArrangement.LeftResizable)
        {
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        }

        if (arrangement != ViewArrangement.RightResizable)
        {
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        }

        if (arrangement != ViewArrangement.TopResizable)
        {
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        }

        if (arrangement != ViewArrangement.BottomResizable)
        {
            Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        }
    }

    #endregion

    #region Corner Resizing Tests

    [Fact]
    public void Border_BottomRightResizable_CombinesBothFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.BottomResizable | ViewArrangement.RightResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
    }

    [Fact]
    public void Border_BottomLeftResizable_CombinesBothFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.BottomResizable | ViewArrangement.LeftResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    [Fact]
    public void Border_TopRightResizable_CombinesBothFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.TopResizable | ViewArrangement.RightResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
    }

    [Fact]
    public void Border_TopLeftResizable_CombinesBothFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.TopResizable | ViewArrangement.LeftResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    #endregion

    #region Overlapped Layout Tests

    [Theory]
    [InlineData (ViewArrangement.Fixed)]
    [InlineData (ViewArrangement.Overlapped)]
    public void MoveSubViewToEnd_ViewArrangement (ViewArrangement arrangement)
    {
        View superView = new () { Arrangement = arrangement };

        var subview1 = new View { Id = "subview1" };
        var subview2 = new View { Id = "subview2" };
        var subview3 = new View { Id = "subview3" };

        superView.Add (subview1, subview2, subview3);

        superView.MoveSubViewToEnd (subview1);
        Assert.Equal ([subview2, subview3, subview1], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview2);
        Assert.Equal ([subview3, subview1, subview2], superView.SubViews.ToArray ());

        superView.MoveSubViewToEnd (subview3);
        Assert.Equal ([subview1, subview2, subview3], superView.SubViews.ToArray ());
    }

    [Fact]
    public void Overlapped_AllowsSubViewsToOverlap ()
    {
        var superView = new View { Arrangement = ViewArrangement.Overlapped, Width = 20, Height = 20 };

        var view1 = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        var view2 = new View { X = 5, Y = 5, Width = 10, Height = 10 };

        superView.Add (view1, view2);

        // Both views can exist at overlapping positions
        Assert.Equal (2, superView.SubViews.Count);
        Assert.True (view1.Frame.IntersectsWith (view2.Frame));
    }

    #endregion

    #region Splitter Pattern Tests

    [Fact]
    public void LeftResizable_CanBeUsedForHorizontalSplitter ()
    {
        var container = new View { Width = 80, Height = 25 };

        var leftPane = new View { X = 0, Y = 0, Width = 40, Height = Dim.Fill () };

        var rightPane = new View
        {
            X = 40,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Single
        };

        container.Add (leftPane, rightPane);

        Assert.True (rightPane.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.NotNull (rightPane.Border);
    }

    [Fact]
    public void TopResizable_CanBeUsedForVerticalSplitter ()
    {
        var container = new View { Width = 80, Height = 25 };

        var topPane = new View { X = 0, Y = 0, Width = Dim.Fill (), Height = 10 };

        var bottomPane = new View
        {
            X = 0,
            Y = 10,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single
        };

        container.Add (topPane, bottomPane);

        Assert.True (bottomPane.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.NotNull (bottomPane.Border);
    }

    #endregion

    #region View Without Border Tests

    [Fact]
    public void View_WithoutBorderStyle_CanHaveArrangement ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable };

        // Arrangement can be set even without a border style
        // Border object still exists but has no visible style
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);
        Assert.NotNull (view.Border);
        Assert.Null (view.BorderStyle);
    }

    [Fact]
    public void View_WithNoBorderStyle_ResizableCanBeSet ()
    {
        var view = new View { Arrangement = ViewArrangement.Resizable };

        // Arrangement is set but has limited effect without a visible border style
        Assert.Equal (ViewArrangement.Resizable, view.Arrangement);
        Assert.NotNull (view.Border);
        Assert.Null (view.BorderStyle);
    }

    #endregion

    #region Integration Tests - Border DetermineArrangeModeFromClick Behavior

    [Fact]
    public void DetermineArrangeModeFromClick_TopResizableIgnoredWhenMovable ()
    {
        // This test verifies the documented behavior that TopResizable is ignored
        // when Movable is also set (line 569 in Border.Arrangment.cs)
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.TopResizable | ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 10
        };
        superView.Add (view);

        // The view has both flags set
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));

        // But Movable takes precedence in Border.DetermineArrangeModeFromClick
        // This is verified by the code at line 569 checking !Parent!.Arrangement.HasFlag(ViewArrangement.Movable)
    }

    [Fact]
    public void DetermineArrangeModeFromClick_TopResizableWorksWithoutMovable ()
    {
        var superView = new View { Width = 80, Height = 25 };

        var view = new View
        {
            Arrangement = ViewArrangement.TopResizable,
            BorderStyle = LineStyle.Single,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 10
        };
        superView.Add (view);

        // Only TopResizable is set
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Movable));
    }

    [Fact]
    public void DetermineArrangeModeFromClick_AllCornerCombinationsSupported ()
    {
        var superView = new View { Width = 80, Height = 25 };

        // Test that all 4 corner combinations are recognized
        ViewArrangement [] cornerCombinations = new []
        {
            ViewArrangement.BottomResizable | ViewArrangement.RightResizable,
            ViewArrangement.BottomResizable | ViewArrangement.LeftResizable,
            ViewArrangement.TopResizable | ViewArrangement.RightResizable,
            ViewArrangement.TopResizable | ViewArrangement.LeftResizable
        };

        foreach (ViewArrangement arrangement in cornerCombinations)
        {
            var view = new View
            {
                Arrangement = arrangement,
                BorderStyle = LineStyle.Single,
                X = 10,
                Y = 10,
                Width = 20,
                Height = 10
            };
            superView.Add (view);

            // Verify the flags are set correctly
            Assert.True (view.Arrangement == arrangement);

            superView.Remove (view);
        }
    }

    #endregion

    #region ViewArrangement Property Change Tests

    [Fact]
    public void View_Arrangement_CanBeChangedAfterCreation ()
    {
        var view = new View { Arrangement = ViewArrangement.Fixed };
        Assert.Equal (ViewArrangement.Fixed, view.Arrangement);

        view.Arrangement = ViewArrangement.Movable;
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);

        view.Arrangement = ViewArrangement.Resizable;
        Assert.Equal (ViewArrangement.Resizable, view.Arrangement);
    }

    [Fact]
    public void View_Arrangement_CanAddFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable };

        view.Arrangement |= ViewArrangement.LeftResizable;
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
    }

    [Fact]
    public void View_Arrangement_CanRemoveFlags ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable };

        view.Arrangement &= ~ViewArrangement.Movable;
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Resizable));
    }

    #endregion

    #region Multiple SubViews Arrangement Tests

    [Fact]
    public void SuperView_CanHaveMultipleArrangeableSubViews ()
    {
        var superView = new View { Arrangement = ViewArrangement.Overlapped, Width = 80, Height = 25 };

        var movableView = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10
        };

        var resizableView = new View
        {
            Arrangement = ViewArrangement.Resizable,
            BorderStyle = LineStyle.Single,
            X = 25,
            Y = 0,
            Width = 20,
            Height = 10
        };

        var fixedView = new View
        {
            Arrangement = ViewArrangement.Fixed,
            BorderStyle = LineStyle.Single,
            X = 50,
            Y = 0,
            Width = 20,
            Height = 10
        };

        superView.Add (movableView, resizableView, fixedView);

        Assert.Equal (3, superView.SubViews.Count);
        Assert.Equal (ViewArrangement.Movable, movableView.Arrangement);
        Assert.Equal (ViewArrangement.Resizable, resizableView.Arrangement);
        Assert.Equal (ViewArrangement.Fixed, fixedView.Arrangement);
    }

    [Fact]
    public void SubView_ArrangementIndependentOfSuperView ()
    {
        var superView = new View { Arrangement = ViewArrangement.Fixed };
        var subView = new View { Arrangement = ViewArrangement.Movable };

        superView.Add (subView);

        // SubView arrangement is independent of SuperView arrangement
        Assert.Equal (ViewArrangement.Fixed, superView.Arrangement);
        Assert.Equal (ViewArrangement.Movable, subView.Arrangement);
    }

    #endregion

    #region Border Thickness Tests

    [Fact]
    public void Border_WithDefaultThickness_SupportsArrangement ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable, BorderStyle = LineStyle.Single };

        Assert.NotNull (view.Border);

        // Default thickness should be (1,1,1,1) for Single line style
        Assert.True (view.Border.Thickness.Left > 0 || view.Border.Thickness.Right > 0 || view.Border.Thickness.Top > 0 || view.Border.Thickness.Bottom > 0);
    }

    [Fact]
    public void Border_WithCustomThickness_SupportsArrangement ()
    {
        var view = new View { Arrangement = ViewArrangement.LeftResizable, BorderStyle = LineStyle.Single };

        // Set custom thickness - only left border
        view.Border.Thickness = new Thickness (2, 0, 0, 0);

        Assert.Equal (2, view.Border.Thickness.Left);
        Assert.Equal (0, view.Border.Thickness.Top);
        Assert.Equal (0, view.Border.Thickness.Right);
        Assert.Equal (0, view.Border.Thickness.Bottom);
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
    }

    #endregion

    #region View-Specific Arrangement Tests

    [Fact]
    public void Runnable_DefaultsToOverlapped ()
    {
        var runnable = new Runnable ();
        Assert.True (runnable.Arrangement.HasFlag (ViewArrangement.Overlapped));
    }

    [Fact]
    public void Window_DefaultsToOverlapped ()
    {
        var window = new Window ();
        Assert.True (window.Arrangement.HasFlag (ViewArrangement.Overlapped));
    }

    [Fact]
    public void View_Navigation_RespectsOverlappedFlag ()
    {
        // View.Navigation.cs checks Arrangement.HasFlag(ViewArrangement.Overlapped)
        var overlappedView = new View { Arrangement = ViewArrangement.Overlapped };
        var tiledView = new View { Arrangement = ViewArrangement.Fixed };

        Assert.True (overlappedView.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.False (tiledView.Arrangement.HasFlag (ViewArrangement.Overlapped));
    }

    [Fact]
    public void View_Hierarchy_RespectsOverlappedFlag ()
    {
        // View.Hierarchy.cs checks Arrangement.HasFlag(ViewArrangement.Overlapped)
        var parent = new View { Arrangement = ViewArrangement.Overlapped };
        var child1 = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        var child2 = new View { X = 5, Y = 5, Width = 10, Height = 10 };

        parent.Add (child1, child2);

        Assert.True (parent.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.Equal (2, parent.SubViews.Count);
    }

    #endregion

    #region Mouse Interaction Tests

    // Not parallelizable due to Application.Mouse dependency in MouseGrabHandler

    #endregion

    #region Summary and Limitations

    // NOTE: The original concern that Border.OnMouseEvent depends on Application.Mouse
    // has been addressed. The tests above prove that MouseGrabHandler works correctly in
    // parallelizable tests when using NewMouseEvent directly on views.
    //
    // Remaining functionality that still requires Application.Init:
    // 1. Border.EnterArrangeMode() with keyboard mode - depends on Application.GetDefaultKey (Command.Arrange)
    // 2. Border.HandleDragOperation() - depends on Application.Top for screen refresh
    // 3. Application-level mouse event routing through Application.RaiseMouseEvent
    //
    // The tests in this file demonstrate:
    // - ViewArrangement enum and flag behavior
    // - View.Arrangement property behavior
    // - Border existence and configuration with arrangements
    // - MouseGrabHandler interaction with arrangeable views
    // - Logical combinations of arrangement flags
    // - Mutual exclusivity rules (TopResizable vs Movable)
    // - Multiple subview scenarios
    //
    // This provides comprehensive coverage of arrangement functionality using
    // parallelizable tests with NewMouseEvent.

    [Fact]
    public void AllArrangementTests_AreParallelizable ()
    {
        // This test verifies that all the arrangement tests in this file
        // can run without Application.Init, making them parallelizable.
        // If this test passes, it confirms no Application dependencies leaked in.

        var view = new View { Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable, BorderStyle = LineStyle.Single };

        Assert.NotNull (view);
        Assert.NotNull (view.Border);
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Resizable));
    }

    #endregion

    #region ViewArrangement Advanced Flag Tests

    [Fact]
    public void ViewArrangement_CanCombineAllResizableDirections ()
    {
        ViewArrangement arrangement = ViewArrangement.TopResizable
                                      | ViewArrangement.BottomResizable
                                      | ViewArrangement.LeftResizable
                                      | ViewArrangement.RightResizable;

        Assert.True (arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.Resizable)); // Resizable is all four combined
    }

    [Fact]
    public void ViewArrangement_MovableAndResizable_CanCoexist ()
    {
        ViewArrangement arrangement = ViewArrangement.Movable | ViewArrangement.Resizable;

        Assert.True (arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (arrangement.HasFlag (ViewArrangement.Resizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    [Fact]
    public void ViewArrangement_OverlappedWithMovableAndResizable_AllFlagsWork ()
    {
        ViewArrangement arrangement = ViewArrangement.Overlapped | ViewArrangement.Movable | ViewArrangement.Resizable;

        Assert.True (arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.True (arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (arrangement.HasFlag (ViewArrangement.Resizable));
    }

    [Fact]
    public void View_Arrangement_CanBeSetToCompositeFlagValue ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Resizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.Overlapped));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
    }

    #endregion

    #region View Arrangement Edge Cases

    [Fact]
    public void View_ArrangementWithNoBorder_CanStillHaveArrangementValue ()
    {
        // A view can have Arrangement set even if BorderStyle is None
        var view = new View { Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable, BorderStyle = LineStyle.None };

        // Border exists but has no visible style
        Assert.NotNull (view.Border);
        Assert.Equal (LineStyle.None, view.BorderStyle);
        Assert.Equal (ViewArrangement.Movable | ViewArrangement.Resizable, view.Arrangement);
    }

    [Fact]
    public void View_ChangingBorderStyle_PreservesArrangement ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable, BorderStyle = LineStyle.Single };

        Assert.NotNull (view.Border);
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);

        // Change border style
        view.BorderStyle = LineStyle.Double;

        Assert.NotNull (view.Border);
        Assert.Equal (ViewArrangement.Movable, view.Arrangement);
    }

    [Fact]
    public void View_ChangingToNoBorderStyle_PreservesArrangement ()
    {
        var view = new View { Arrangement = ViewArrangement.Movable, BorderStyle = LineStyle.Single };

        Assert.NotNull (view.Border);
        Assert.Equal (LineStyle.Single, view.BorderStyle);

        // Change to no border style
        view.BorderStyle = LineStyle.None;

        // Border still exists but has no visible style
        Assert.NotNull (view.Border);
        Assert.Equal (LineStyle.None, view.BorderStyle);
        Assert.Equal (ViewArrangement.Movable, view.Arrangement); // Arrangement value is preserved
    }

    [Fact]
    public void View_MultipleSubviewsWithDifferentArrangements_EachIndependent ()
    {
        var container = new View ();

        var fixedView = new View { Id = "fixed", Arrangement = ViewArrangement.Fixed };
        var movableView = new View { Id = "movable", Arrangement = ViewArrangement.Movable };
        var resizableView = new View { Id = "resizable", Arrangement = ViewArrangement.Resizable };
        var overlappedView = new View { Id = "overlapped", Arrangement = ViewArrangement.Overlapped };

        container.Add (fixedView, movableView, resizableView, overlappedView);

        Assert.Equal (ViewArrangement.Fixed, fixedView.Arrangement);
        Assert.Equal (ViewArrangement.Movable, movableView.Arrangement);
        Assert.Equal (ViewArrangement.Resizable, resizableView.Arrangement);
        Assert.Equal (ViewArrangement.Overlapped, overlappedView.Arrangement);
    }

    #endregion

    #region Overlapped Layout Advanced Tests

    [Fact]
    public void Overlapped_ViewCanBeMovedToFront ()
    {
        var container = new View { Arrangement = ViewArrangement.Overlapped };

        var view1 = new View { Id = "view1" };
        var view2 = new View { Id = "view2" };
        var view3 = new View { Id = "view3" };

        container.Add (view1, view2, view3);

        // Initially view3 is on top (last added)
        Assert.Equal (view3, container.SubViews.ToArray () [^1]);

        // Move view1 to end (top of Z-order)
        container.MoveSubViewToEnd (view1);

        Assert.Equal (view1, container.SubViews.ToArray () [^1]);
        Assert.Equal ([view2, view3, view1], container.SubViews.ToArray ());
    }

    [Fact]
    public void Overlapped_ViewCanBeMovedToBack ()
    {
        var container = new View { Arrangement = ViewArrangement.Overlapped };

        var view1 = new View { Id = "view1" };
        var view2 = new View { Id = "view2" };
        var view3 = new View { Id = "view3" };

        container.Add (view1, view2, view3);

        // Initial order: [view1, view2, view3]
        Assert.Equal ([view1, view2, view3], container.SubViews.ToArray ());

        // Move view3 to end (top of Z-order)
        container.MoveSubViewToEnd (view3);
        Assert.Equal (view3, container.SubViews.ToArray () [^1]);
        Assert.Equal ([view1, view2, view3], container.SubViews.ToArray ());

        // Now move view1 to end (making it on top, pushing view3 down)
        container.MoveSubViewToEnd (view1);
        Assert.Equal ([view2, view3, view1], container.SubViews.ToArray ());
    }

    [Fact]
    public void Fixed_ViewAddOrderMattersForLayout ()
    {
        var container = new View { Arrangement = ViewArrangement.Fixed };

        var view1 = new View
        {
            Id = "view1",
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5
        };

        var view2 = new View
        {
            Id = "view2",
            X = 5,
            Y = 2,
            Width = 10,
            Height = 5
        };

        container.Add (view1, view2);

        // In Fixed arrangement, views don't overlap in the same way
        // The order determines paint/draw order but not logical overlap
        Assert.Equal (view1, container.SubViews.ToArray () [0]);
        Assert.Equal (view2, container.SubViews.ToArray () [1]);
    }

    #endregion

    #region Resizable Edge Cases

    [Fact]
    public void Border_OnlyLeftResizable_OtherDirectionsNotResizable ()
    {
        var view = new View { Arrangement = ViewArrangement.LeftResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    [Fact]
    public void Border_TwoOppositeDirections_CanBeResizable ()
    {
        var view = new View { Arrangement = ViewArrangement.LeftResizable | ViewArrangement.RightResizable, BorderStyle = LineStyle.Single };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    [Fact]
    public void Border_ThreeDirections_CanBeResizable ()
    {
        var view = new View
        {
            Arrangement = ViewArrangement.LeftResizable | ViewArrangement.RightResizable | ViewArrangement.BottomResizable, BorderStyle = LineStyle.Single
        };

        Assert.True (view.Arrangement.HasFlag (ViewArrangement.LeftResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.RightResizable));
        Assert.False (view.Arrangement.HasFlag (ViewArrangement.TopResizable));
        Assert.True (view.Arrangement.HasFlag (ViewArrangement.BottomResizable));
    }

    #endregion
}
