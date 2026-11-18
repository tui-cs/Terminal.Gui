namespace ViewBaseTests.Layout;

/// <summary>
///     Comprehensive tests for ViewManipulator class.
///     Tests both mouse-based (absolute positioning) and keyboard-based (delta/increment) manipulation.
/// </summary>
[Trait ("Category", "Adornment")]
[Trait ("Category", "ViewManipulator")]
public class ViewManipulatorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithGrabPoint_InitializesCorrectly ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        Point grabPoint = new (5, 5);
        const int MIN_WIDTH = 20;
        const int MIN_HEIGHT = 10;

        // Act
        ViewManipulator manipulator = new (view, grabPoint, MIN_WIDTH, MIN_HEIGHT);

        // Assert
        Assert.NotNull (manipulator);
    }

    [Fact]
    public void Constructor_KeyboardMode_InitializesWithoutGrabPoint ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        const int MIN_WIDTH = 20;
        const int MIN_HEIGHT = 10;

        // Act
        ViewManipulator manipulator = new (view, MIN_WIDTH, MIN_HEIGHT);

        // Assert
        Assert.NotNull (manipulator);
    }

    #endregion

    #region Mouse-Based Manipulation Tests

    [Fact]
    public void Move_UpdatesViewPosition ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        Point grabPoint = new (5, 5);
        ViewManipulator manipulator = new (view, grabPoint, 20, 10);

        // Act
        manipulator.Move (new Point (25, 20));

        // Assert - Position should be location - grabPoint
        Assert.Equal (20, view.Frame.X); // 25 - 5
        Assert.Equal (15, view.Frame.Y); // 20 - 5
    }

    [Fact]
    public void ResizeTop_AdjustsYPositionAndHeight ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        Point grabPoint = new (0, 0);
        ViewManipulator manipulator = new (view, grabPoint, 20, 10);

        // Act - Move top edge up by 5 pixels (Y=5)
        manipulator.ResizeTop (new Point (10, 5));

        // Assert
        Assert.Equal (5, view.Frame.Y);     // Y moved to 5
        Assert.Equal (35, view.Frame.Height); // Height increased by 5
    }

    [Fact]
    public void ResizeTop_RespectsMinimumHeight ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        Point grabPoint = new (0, 0);
        const int MIN_HEIGHT = 25;
        ViewManipulator manipulator = new (view, grabPoint, 20, MIN_HEIGHT);

        // Act - Try to resize to height smaller than minimum
        manipulator.ResizeTop (new Point (10, 20)); // Would make height = 20 (< MIN_HEIGHT)

        // Assert - Height should be constrained to minimum
        Assert.Equal (MIN_HEIGHT, view.Frame.Height);
    }

    [Fact]
    public void ResizeBottom_AdjustsHeightOnly ()
    {
        // Arrange
        View view = new ()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 30
        };
        view.Margin.Thickness = new (1);
        ViewManipulator manipulator = new (view, Point.Empty, 20, 10);
        var originalY = view.Frame.Y;

        // Act - Drag bottom edge down to Y=50
        manipulator.ResizeBottom (new Point (10, 50));

        // Assert
        Assert.Equal (originalY, view.Frame.Y);    // Y position unchanged
        Assert.Equal (42, view.Frame.Height);      // Height = (50 - 10) + margin.bottom(1) + 1 = 42
    }

    [Fact]
    public void ResizeBottom_RespectsMinimumHeight ()
    {
        // Arrange
        View view = new ()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 30
        };
        view.Margin.Thickness = new (1);
        const int MIN_HEIGHT = 35;
        ViewManipulator manipulator = new (view, Point.Empty, 20, MIN_HEIGHT);

        // Act - Try to resize to smaller than minimum
        manipulator.ResizeBottom (new Point (10, 15)); // Would make height = 7

        // Assert
        Assert.Equal (MIN_HEIGHT, view.Frame.Height);
    }

    [Fact]
    public void ResizeLeft_AdjustsXPositionAndWidth ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        Point grabPoint = new (0, 0);
        ViewManipulator manipulator = new (view, grabPoint, 20, 10);

        // Act - Move left edge left by 5 pixels (X=5)
        manipulator.ResizeLeft (new Point (5, 10));

        // Assert
        Assert.Equal (5, view.Frame.X);    // X moved to 5
        Assert.Equal (55, view.Frame.Width); // Width increased by 5
    }

    [Fact]
    public void ResizeLeft_RespectsMinimumWidth ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        Point grabPoint = new (0, 0);
        const int MIN_WIDTH = 45;
        ViewManipulator manipulator = new (view, grabPoint, MIN_WIDTH, 10);

        // Act - Try to resize to width smaller than minimum
        manipulator.ResizeLeft (new Point (40, 10)); // Would make width = 20 (< MIN_WIDTH)

        // Assert - Width should be constrained to minimum
        Assert.Equal (MIN_WIDTH, view.Frame.Width);
    }

    [Fact]
    public void ResizeRight_AdjustsWidthOnly ()
    {
        // Arrange
        View view = new ()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 30
        };
        view.Margin.Thickness = new (1);
        ViewManipulator manipulator = new (view, Point.Empty, 20, 10);
        var originalX = view.Frame.X;

        // Act - Drag right edge to X=70
        manipulator.ResizeRight (new Point (70, 10));

        // Assert
        Assert.Equal (originalX, view.Frame.X);    // X position unchanged
        Assert.Equal (62, view.Frame.Width);       // Width = (70 - 10) + margin.right(1) + 1 = 62
    }

    [Fact]
    public void ResizeRight_RespectsMinimumWidth ()
    {
        // Arrange
        View view = new ()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 30
        };
        view.Margin.Thickness = new (1);
        const int MIN_WIDTH = 55;
        ViewManipulator manipulator = new (view, Point.Empty, MIN_WIDTH, 10);

        // Act - Try to resize to smaller than minimum
        manipulator.ResizeRight (new Point (15, 10)); // Would make width = 7

        // Assert
        Assert.Equal (MIN_WIDTH, view.Frame.Width);
    }

    #endregion

    #region Keyboard-Based Manipulation Tests

    [Fact]
    public void AdjustX_MovesViewHorizontally ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        ViewManipulator manipulator = new (view, 20, 10);
        var originalX = view.Frame.X;

        // Act
        manipulator.AdjustX (5);

        // Assert
        Assert.Equal (originalX + 5, view.Frame.X);
        Assert.Equal (10, view.Frame.Y); // Y unchanged
    }

    [Fact]
    public void AdjustX_Negative_MovesLeft ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        ViewManipulator manipulator = new (view, 20, 10);

        // Act
        manipulator.AdjustX (-3);

        // Assert
        Assert.Equal (7, view.Frame.X);
    }

    [Fact]
    public void AdjustY_MovesViewVertically ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        ViewManipulator manipulator = new (view, 20, 10);
        int originalY = view.Frame.Y;

        // Act
        manipulator.AdjustY (5);

        // Assert
        Assert.Equal (10, view.Frame.X); // X unchanged
        Assert.Equal (originalY + 5, view.Frame.Y);
    }

    [Fact]
    public void AdjustY_Negative_MovesUp ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        ViewManipulator manipulator = new (view, 20, 10);

        // Act
        manipulator.AdjustY (-3);

        // Assert
        Assert.Equal (7, view.Frame.Y);
    }

    [Fact]
    public void AdjustWidth_IncreasesWidth ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        ViewManipulator manipulator = new (view, 20, 10);

        // Act
        var result = manipulator.AdjustWidth (10);

        // Assert
        Assert.True (result);
        Assert.Equal (60, view.Frame.Width);
    }

    [Fact]
    public void AdjustWidth_DecreasesWidth ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act
        var result = manipulator.AdjustWidth (-10);

        // Assert
        Assert.True (result);
        Assert.Equal (40, view.Frame.Width);
    }

    [Fact]
    public void AdjustWidth_RespectsMinimumWidth ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        const int MIN_WIDTH = 45;
        ViewManipulator manipulator = new (view, MIN_WIDTH, 10);

        // Act - Try to shrink below minimum
        var result = manipulator.AdjustWidth (-10); // Would be 40, but min is 45

        // Assert
        Assert.True (result); // Returns true because it adjusted to minimum
        Assert.Equal (MIN_WIDTH, view.Frame.Width);
    }

    [Fact]
    public void AdjustHeight_IncreasesHeight ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        ViewManipulator manipulator = new (view, 20, 10);

        // Act
        var result = manipulator.AdjustHeight (10);

        // Assert
        Assert.True (result);
        Assert.Equal (40, view.Frame.Height);
    }

    [Fact]
    public void AdjustHeight_DecreasesHeight ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act
        var result = manipulator.AdjustHeight (-10);

        // Assert
        Assert.True (result);
        Assert.Equal (20, view.Frame.Height);
    }

    [Fact]
    public void AdjustHeight_RespectsMinimumHeight ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        const int MIN_HEIGHT = 25;
        ViewManipulator manipulator = new (view, 20, MIN_HEIGHT);

        // Act - Try to shrink below minimum
        var result = manipulator.AdjustHeight (-10); // Would be 20, but min is 25

        // Assert
        Assert.True (result); // Returns true because it adjusted to minimum
        Assert.Equal (MIN_HEIGHT, view.Frame.Height);
    }

    [Fact]
    public void ResizeFromTop_ExpandsUpward ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act - Negative delta expands upward
        var result = manipulator.ResizeFromTop (-5);

        // Assert
        Assert.True (result);
        Assert.Equal (5, view.Frame.Y);     // Y moves up
        Assert.Equal (35, view.Frame.Height); // Height increases
    }

    [Fact]
    public void ResizeFromTop_ContractsDownward ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act - Positive delta contracts downward
        var result = manipulator.ResizeFromTop (5);

        // Assert
        Assert.True (result);
        Assert.Equal (15, view.Frame.Y);    // Y moves down
        Assert.Equal (25, view.Frame.Height); // Height decreases
    }

    [Fact]
    public void ResizeFromLeft_ExpandsLeftward ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act - Negative delta expands leftward
        var result = manipulator.ResizeFromLeft (-5);

        // Assert
        Assert.True (result);
        Assert.Equal (5, view.Frame.X);    // X moves left
        Assert.Equal (55, view.Frame.Width); // Width increases
    }

    [Fact]
    public void ResizeFromLeft_ContractsRightward ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act - Positive delta contracts rightward
        var result = manipulator.ResizeFromLeft (5);

        // Assert
        Assert.True (result);
        Assert.Equal (15, view.Frame.X);   // X moves right
        Assert.Equal (45, view.Frame.Width); // Width decreases
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void MouseAndKeyboard_ProduceSameResults_ForMove ()
    {
        // Arrange
        View view1 = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        View view2 = new () { X = 10, Y = 10, Width = 50, Height = 30 };

        ViewManipulator mouseManipulator = new (view1, Point.Empty, 20, 10);
        ViewManipulator keyboardManipulator = new (view2, 20, 10);

        // Act - Move both views by same amount
        mouseManipulator.Move (new Point (25, 20)); // Absolute position
        keyboardManipulator.AdjustX (15);           // Delta: 10 + 15 = 25
        keyboardManipulator.AdjustY (10);           // Delta: 10 + 10 = 20

        // Assert - Both should end up at same position
        Assert.Equal (view1.Frame.X, view2.Frame.X);
        Assert.Equal (view1.Frame.Y, view2.Frame.Y);
    }

    [Fact]
    public void ComplexResize_BottomRightCorner ()
    {
        // Arrange
        View view = new ()
        {
            X = 10,
            Y = 10,
            Width = 50,
            Height = 30
        };
        view.Margin.Thickness = new (1);
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act - Simulate dragging bottom-right corner
        manipulator.AdjustWidth (10);
        manipulator.AdjustHeight (5);

        // Assert
        Assert.Equal (10, view.Frame.X);   // Position unchanged
        Assert.Equal (10, view.Frame.Y);
        Assert.Equal (60, view.Frame.Width);  // Both dimensions increased
        Assert.Equal (35, view.Frame.Height);
    }

    [Fact]
    public void ComplexResize_TopLeftCorner ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 50, Height = 30 };
        view.BeginInit ();
        view.EndInit ();
        ViewManipulator manipulator = new (view, 20, 10);

        // Act - Simulate dragging top-left corner outward
        manipulator.ResizeFromLeft (-5);  // Expand left
        manipulator.ResizeFromTop (-5);   // Expand top

        // Assert
        Assert.Equal (5, view.Frame.X);     // Position moved
        Assert.Equal (5, view.Frame.Y);
        Assert.Equal (55, view.Frame.Width);  // Both dimensions increased
        Assert.Equal (35, view.Frame.Height);
    }

    [Fact]
    public void MinimumSizeEnforcement_PreventsShrinkingBelowMinimum ()
    {
        // Arrange
        View view = new () { X = 10, Y = 10, Width = 30, Height = 20 };
        const int MIN_WIDTH = 30;
        const int MIN_HEIGHT = 20;
        ViewManipulator manipulator = new (view, MIN_WIDTH, MIN_HEIGHT);

        // Act - Try to shrink below minimum in all directions
        var widthResult = manipulator.AdjustWidth (-10);
        var heightResult = manipulator.AdjustHeight (-10);

        // Assert - Should stay at minimum
        Assert.False (widthResult);  // No change because already at minimum
        Assert.False (heightResult);
        Assert.Equal (MIN_WIDTH, view.Frame.Width);
        Assert.Equal (MIN_HEIGHT, view.Frame.Height);
    }

    #endregion
}
