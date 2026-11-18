namespace DrawingTests.RegionTests;

using Xunit;

public class SubtractRectangleTests
{
    [Fact]
    public void SubtractRectangle_NoOverlap_ReturnsOriginalRectangle ()
    {
        // Arrange: Non-overlapping rectangles
        Rectangle original = new (0, 0, 10, 10);
        Rectangle subtract = new (15, 15, 5, 5);

        // Act: Subtract non-overlapping rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: Original rectangle unchanged
        Assert.Single (result);
        Assert.Equal (new Rectangle (0, 0, 10, 10), result [0]);
    }

    [Fact]
    public void SubtractRectangle_CompleteOverlap_ReturnsEmpty ()
    {
        // Arrange: Subtract rectangle completely overlaps original
        Rectangle original = new (0, 0, 5, 5);
        Rectangle subtract = new (0, 0, 5, 5);

        // Act: Subtract overlapping rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: No rectangles returned (empty result)
        Assert.Empty (result);
    }

    [Fact]
    public void SubtractRectangle_PartialOverlap_TopAndBottom ()
    {
        // Arrange: Original rectangle with subtract overlapping top and bottom
        Rectangle original = new (0, 0, 10, 10);
        Rectangle subtract = new (0, 4, 10, 2); // Overlaps y=4 to y=5

        // Act: Subtract overlapping rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: Expect top (0,0,10,4) and bottom (0,6,10,4)
        Assert.Equal (2, result.Count);
        Assert.Contains (new Rectangle (0, 0, 10, 4), result); // Top part
        Assert.Contains (new Rectangle (0, 6, 10, 4), result); // Bottom part
    }

    [Fact]
    public void SubtractRectangle_PartialOverlap_LeftAndRight ()
    {
        // Arrange: Original rectangle with subtract overlapping left and right
        Rectangle original = new (0, 0, 10, 10);
        Rectangle subtract = new (4, 0, 2, 10); // Overlaps x=4 to x=5

        // Act: Subtract overlapping rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: Expect left (0,0,4,10) and right (6,0,4,10)
        Assert.Equal (2, result.Count);
        Assert.Contains (new Rectangle (0, 0, 4, 10), result); // Left part
        Assert.Contains (new Rectangle (6, 0, 4, 10), result); // Right part
    }


    [Fact]
    public void SubtractRectangle_EmptyOriginal_ReturnsEmpty ()
    {
        // Arrange: Empty original rectangle
        Rectangle original = Rectangle.Empty;
        Rectangle subtract = new (0, 0, 5, 5);

        // Act: Subtract from empty rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: No rectangles returned (empty result)
        Assert.Empty (result);
    }

    [Fact]
    public void SubtractRectangle_EmptySubtract_ReturnsOriginal ()
    {
        // Arrange: Empty subtract rectangle
        Rectangle original = new (0, 0, 5, 5);
        Rectangle subtract = Rectangle.Empty;

        // Act: Subtract empty rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: Original rectangle unchanged
        Assert.Single (result);
        Assert.Equal (new Rectangle (0, 0, 5, 5), result [0]);
    }


    [Fact]
    public void SubtractRectangle_ZeroWidthOrHeight_HandlesCorrectly ()
    {
        // Arrange: Rectangle with zero width or height
        Rectangle original = new (0, 0, 5, 5);
        Rectangle subtract = new (0, 0, 0, 5); // Zero width

        // Act: Subtract zero-width rectangle
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // Assert: Original rectangle unchanged
        Assert.Single (result);
        Assert.Equal (new Rectangle (0, 0, 5, 5), result [0]);
    }

    [Fact]
    public void SubtractRectangle_NegativeCoordinates_HandlesCorrectly ()
    {
        // Arrange:
        // Original rectangle: (-5,-5) with width 10 and height 10.
        // Subtract rectangle: (-3,-3) with width 2 and height 2.
        Rectangle original = new (-5, -5, 10, 10);
        Rectangle subtract = new (-3, -3, 2, 2);

        // Act:
        var results = Region.SubtractRectangle (original, subtract).ToList ();

        // Expected fragments based on our algorithm:
        // Top:    (-5,-5,10,2)
        // Bottom: (-5,-1,10,6)
        // Left:   (-5,-3,2,2)
        // Right:  (-1,-3,6,2)
        var expectedTop = new Rectangle (-5, -5, 10, 2);
        var expectedBottom = new Rectangle (-5, -1, 10, 6);
        var expectedLeft = new Rectangle (-5, -3, 2, 2);
        var expectedRight = new Rectangle (-1, -3, 6, 2);

        // Assert:
        Assert.Contains (expectedTop, results);
        Assert.Contains (expectedBottom, results);
        Assert.Contains (expectedLeft, results);
        Assert.Contains (expectedRight, results);
        Assert.Equal (4, results.Count);
    }

    [Fact]
    public void SubtractRectangle_EdgeOverlap_TopLeftCorner ()
    {
        // Arrange: Subtract overlaps only the top-left corner.
        // Original: (0,0,5,5); Subtract: (0,0,1,1)
        Rectangle original = new (0, 0, 5, 5);
        Rectangle subtract = new (0, 0, 1, 1);

        // Act:
        var result = Region.SubtractRectangle (original, subtract).ToList ();

        // The algorithm produces two fragments:
        // 1. Bottom: (0,1,5,4)
        // 2. Right:  (1,0,4,1)
        Assert.Equal (2, result.Count);
        Assert.Contains (new Rectangle (0, 1, 5, 4), result);
        Assert.Contains (new Rectangle (1, 0, 4, 1), result);
    }

    // Updated L-shaped test: The algorithm produces 6 fragments, not 5.
    [Fact]
    public void SubtractRectangle_LShapedSubtract_MultipleFragments ()
    {
        // Arrange:
        // Original: (0,0,6,6)
        // subtract1: (2,2,2,1) creates a horizontal gap.
        // subtract2: (2,3,1,1) removes an additional piece from the bottom fragment.
        Rectangle original = new (0, 0, 6, 6);
        Rectangle subtract1 = new (2, 2, 2, 1);
        Rectangle subtract2 = new (2, 3, 1, 1);

        // Act:
        var intermediateResult = Region.SubtractRectangle (original, subtract1).ToList ();
        var finalResult = intermediateResult.SelectMany (r => Region.SubtractRectangle (r, subtract2)).ToList ();

        // Explanation:
        // After subtracting subtract1, we expect these 4 fragments:
        //   - Top:    (0,0,6,2)
        //   - Bottom: (0,3,6,3)
        //   - Left:   (0,2,2,1)
        //   - Right:  (4,2,2,1)
        // Then subtracting subtract2 from the bottom fragment (0,3,6,3) produces 3 fragments:
        //   - Bottom part: (0,4,6,2)
        //   - Left part:   (0,3,2,1)
        //   - Right part:  (3,3,3,1)
        // Total fragments = 1 (top) + 3 (modified bottom) + 1 (left) + 1 (right) = 6.
        Assert.Equal (6, finalResult.Count);
        Assert.Contains (new Rectangle (0, 0, 6, 2), finalResult);  // Top fragment remains unchanged.
        Assert.Contains (new Rectangle (0, 4, 6, 2), finalResult);  // Bottom part from modified bottom.
        Assert.Contains (new Rectangle (0, 3, 2, 1), finalResult);  // Left part from modified bottom.
        Assert.Contains (new Rectangle (3, 3, 3, 1), finalResult);  // Right part from modified bottom.
        Assert.Contains (new Rectangle (0, 2, 2, 1), finalResult);  // Left fragment from first subtraction.
        Assert.Contains (new Rectangle (4, 2, 2, 1), finalResult);  // Right fragment from first subtraction.
    }

    // Additional focused tests for L-shaped subtraction scenarios:

    [Fact]
    public void SubtractRectangle_LShapedSubtract_HorizontalThenVertical ()
    {
        // Arrange:
        // First, subtract a horizontal band.
        Rectangle original = new (0, 0, 10, 10);
        Rectangle subtractHorizontal = new (0, 4, 10, 2);
        var horizontalResult = Region.SubtractRectangle (original, subtractHorizontal).ToList ();
        // Expect two fragments: top (0,0,10,4) and bottom (0,6,10,4).
        Assert.Equal (2, horizontalResult.Count);
        Assert.Contains (new Rectangle (0, 0, 10, 4), horizontalResult);
        Assert.Contains (new Rectangle (0, 6, 10, 4), horizontalResult);

        // Act:
        // Now, subtract a vertical piece from the top fragment.
        Rectangle subtractVertical = new (3, 0, 2, 4);
        var finalResult = Region.SubtractRectangle (horizontalResult [0], subtractVertical).ToList ();

        // Assert:
        // The subtraction yields two fragments:
        // Left fragment: (0,0,3,4)
        // Right fragment: (5,0,5,4)
        Assert.Equal (2, finalResult.Count);
        Assert.Contains (new Rectangle (0, 0, 3, 4), finalResult);
        Assert.Contains (new Rectangle (5, 0, 5, 4), finalResult);
    }

    [Fact]
    public void SubtractRectangle_LShapedSubtract_VerticalThenHorizontal ()
    {
        // Arrange:
        // First, subtract a vertical band.
        // Original: (0,0,10,10)
        // Vertical subtract: (4,0,2,10) produces two fragments:
        //   Left: (0,0,4,10) and Right: (6,0,4,10)
        Rectangle original = new (0, 0, 10, 10);
        Rectangle subtractVertical = new (4, 0, 2, 10);
        var verticalResult = Region.SubtractRectangle (original, subtractVertical).ToList ();
        Assert.Equal (2, verticalResult.Count);
        Assert.Contains (new Rectangle (0, 0, 4, 10), verticalResult);
        Assert.Contains (new Rectangle (6, 0, 4, 10), verticalResult);

        // Act:
        // Now, subtract a horizontal piece from the left fragment.
        // Horizontal subtract: (0,3,4,2)
        // from the left fragment (0,0,4,10)
        // Let's determine the expected fragments:
        // 1. Top band: since original.Top (0) < subtract.Top (3), we get:
        //    (0,0,4, 3)  -- height = subtract.Top - original.Top = 3.
        // 2. Bottom band: since original.Bottom (10) > subtract.Bottom (5), we get:
        //    (0,5,4, 5)  -- height = original.Bottom - subtract.Bottom = 5.
        // No left or right fragments are produced because subtractHorizontal spans the full width.
        // Total fragments: 2.
        Rectangle subtractHorizontal = new (0, 3, 4, 2);
        var finalResult = Region.SubtractRectangle (verticalResult [0], subtractHorizontal).ToList ();

        // Assert:
        // Expecting two fragments: (0,0,4,3) and (0,5,4,5).
        Assert.Equal (2, finalResult.Count);
        Assert.Contains (new Rectangle (0, 0, 4, 3), finalResult);
        Assert.Contains (new Rectangle (0, 5, 4, 5), finalResult);
    }

}