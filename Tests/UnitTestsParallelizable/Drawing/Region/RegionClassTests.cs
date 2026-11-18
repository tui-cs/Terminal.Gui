namespace DrawingTests.RegionTests;

public class RegionClassTests
{
    [Fact]
    public void Clone_CreatesExactCopy ()
    {
        var region = new Region (new (10, 10, 50, 50));
        Region clone = region.Clone ();
        Assert.True (clone.Contains (20, 20));
        Assert.Equal (region.GetRectangles (), clone.GetRectangles ());
    }

    [Fact]
    public void Combine_EmptyRectangles_ProducesEmptyRegion ()
    {
        // Arrange: Create region and combine with empty rectangles
        var region = new Region ();
        region.Combine (new Rectangle (0, 0, 0, 0), RegionOp.Union); // Empty rectangle
        region.Combine (new Region (), RegionOp.Union); // Empty region

        // Assert: Region is empty
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void Complement_Rectangle_ComplementsRegion ()
    {
        var region = new Region (new (10, 10, 50, 50));
        region.Complement (new (0, 0, 100, 100));
        Assert.True (region.Contains (5, 5));
        Assert.False (region.Contains (20, 20));
    }

    [Theory]
    [MemberData (nameof (Complement_TestData))]
    public void Complement_Region_Success (Region region, Rectangle [] rectangles, Rectangle [] expectedScans)
    {
        foreach (Rectangle rect in rectangles)
        {
            region.Complement (rect);
        }

        Rectangle [] actualScans = region.GetRectangles ();
        Assert.Equal (expectedScans, actualScans);
    }


    public static IEnumerable<object []> Complement_TestData ()
    {
        yield return new object []
        {
            new Region (new (10, 10, 100, 100)),
            new Rectangle [] { new (40, 60, 100, 20) },
            new Rectangle [] { new (110, 60, 30, 20) }
        };

        yield return new object []
        {
            new Region (new (70, 10, 100, 100)),
            new Rectangle [] { new (40, 60, 100, 20) },
            new Rectangle [] { new (40, 60, 30, 20) }
        };

        yield return new object []
        {
            new Region (new (40, 100, 100, 100)),
            new Rectangle [] { new (70, 80, 50, 40) },
            new Rectangle [] { new (70, 80, 50, 20) }
        };

        yield return new object []
        {
            new Region (new (40, 10, 100, 100)),
            new Rectangle [] { new (70, 80, 50, 40) },
            new Rectangle [] { new (70, 110, 50, 10) }
        };

        yield return new object []
        {
            new Region (new (30, 30, 80, 80)),
            new Rectangle []
            {
                new (45, 45, 200, 200),
                new (160, 260, 10, 10),
                new (170, 260, 10, 10)
            },
            new Rectangle [] { new (170, 260, 10, 10) }
        };

        yield return new object []
        {
            new Region (),
            new [] { Rectangle.Empty },
            new Rectangle[0]
        };

        yield return new object []
        {
            new Region (),
            new Rectangle [] { new (1, 2, 3, 4) },
            new Rectangle[0]
        };
    }

    [Fact]
    public void Complement_WithRectangle_ComplementsRegion ()
    {
        var region = new Region (new (10, 10, 50, 50));
        var rect = new Rectangle (0, 0, 100, 100);

        region.Complement (rect);

        // Points that were inside the original region should now be outside
        Assert.False (region.Contains (35, 35));

        // Points that were outside the original region but inside bounds should now be inside
        Assert.True (region.Contains (5, 5));
        Assert.True (region.Contains (95, 95));
    }

    [Fact]
    public void Complement_WithRegion_ComplementsRegion ()
    {
        var region = new Region (new (10, 10, 50, 50));
        var bounds = new Rectangle (0, 0, 100, 100);

        region.Complement (bounds);

        // Points that were inside the original region should now be outside
        Assert.False (region.Contains (35, 35));

        // Points that were outside the original region but inside bounds should now be inside
        Assert.True (region.Contains (5, 5));
        Assert.True (region.Contains (95, 95));
    }

    [Fact]
    public void Constructor_EmptyRegion_IsEmpty ()
    {
        var region = new Region ();
        Assert.True (region.IsEmpty ());
    }

    [Fact]
    public void Constructor_WithRectangle_IsNotEmpty ()
    {
        var region = new Region (new (10, 10, 50, 50));
        Assert.False (region.IsEmpty ());
    }

    [Fact]
    public void Contains_Point_ReturnsCorrectResult ()
    {
        var region = new Region (new (10, 10, 50, 50));

        Assert.True (region.Contains (20, 20));
        Assert.False (region.Contains (100, 100));
    }

    [Fact]
    public void Contains_PointInsideRegion_ReturnsTrue ()
    {
        var region = new Region (new (10, 10, 50, 50));
        Assert.True (region.Contains (20, 20));
    }

    [Fact]
    public void Contains_RectangleInsideRegion_ReturnsTrue ()
    {
        var region = new Region (new (10, 10, 50, 50));
        Assert.True (region.Contains (new (20, 20, 10, 10)));
    }

    [Fact]
    public void Equals_NullRegion_ReturnsFalse ()
    {
        var region = new Region ();
        Assert.False (region.Equals (null));
    }

    [Fact]
    public void Equals_SameRegion_ReturnsTrue ()
    {
        var region = new Region (new (1, 2, 3, 4));
        Assert.True (region.Equals (region));
    }

    public static IEnumerable<object []> Equals_TestData ()
    {
        static Region Empty ()
        {
            Region emptyRegion = new ();
            emptyRegion.Intersect (Rectangle.Empty);

            return emptyRegion;
        }

        yield return new object [] { new Region (), new Region (), true };
        yield return new object [] { new Region (), Empty (), true };
        yield return new object [] { new Region (), new Region (new (1, 2, 3, 4)), false };

        yield return new object [] { Empty (), Empty (), true };
        yield return new object [] { Empty (), new Region (new (0, 0, 0, 0)), true };
        yield return new object [] { Empty (), new Region (new (1, 2, 3, 3)), false };

        yield return new object [] { new Region (new (1, 2, 3, 4)), new Region (new (1, 2, 3, 4)), true };
        yield return new object [] { new Region (new (1, 2, 3, 4)), new Region (new (2, 2, 3, 4)), false };
        yield return new object [] { new Region (new (1, 2, 3, 4)), new Region (new (1, 3, 3, 4)), false };
        yield return new object [] { new Region (new (1, 2, 3, 4)), new Region (new (1, 2, 4, 4)), false };
        yield return new object [] { new Region (new (1, 2, 3, 4)), new Region (new (1, 2, 3, 5)), false };
    }

    [Theory]
    [MemberData (nameof (Equals_TestData))]
    public void Equals_Valid_ReturnsExpected (Region region1, Region region2, bool expected) { Assert.Equal (expected, region1.Equals (region2)); }

    [Fact]
    public void GetBounds_ReturnsBoundingRectangle ()
    {
        var region = new Region (new (10, 10, 50, 50));
        region.Union (new Rectangle (100, 100, 20, 20));
        Rectangle bounds = region.GetBounds ();
        Assert.Equal (new (10, 10, 110, 110), bounds);
    }

    [Fact]
    public void GetBounds_ReturnsCorrectBounds ()
    {
        var region = new Region ();
        region.Union (new Rectangle (10, 10, 50, 50));
        region.Union (new Rectangle (30, 30, 50, 50));

        Rectangle bounds = region.GetBounds ();

        Assert.Equal (new (10, 10, 70, 70), bounds);
    }

    [Fact]
    public void GetRegionScans_ReturnsAllRectangles ()
    {
        var region = new Region (new (10, 10, 50, 50));
        region.Union (new Rectangle (100, 100, 20, 20));
        Rectangle [] scans = region.GetRectangles ();
        Assert.Equal (2, scans.Length);
        Assert.Contains (new Rectangle (10, 10, 50, 50), scans);
        Assert.Contains (new Rectangle (100, 100, 20, 20), scans);
    }

    [Fact]
    public void Intersect_Rectangle_IntersectsRegion ()
    {
        var region = new Region (new (10, 10, 50, 50));
        region.Intersect (new Rectangle (30, 30, 50, 50));
        Assert.False (region.Contains (20, 20));
        Assert.True (region.Contains (40, 40));
    }

    [Fact]
    public void Intersect_Region_IntersectsRegions ()
    {
        var region1 = new Region (new (10, 10, 50, 50));
        var region2 = new Region (new (30, 30, 50, 50));
        region1.Intersect (region2);
        Assert.False (region1.Contains (20, 20));
        Assert.True (region1.Contains (40, 40));
    }

    [Fact]
    public void Intersect_WithEmptyRectangle_ResultsInEmptyRegion ()
    {
        // Arrange
        var region = new Region (new (0, 0, 10, 10));
        var rectangle = Rectangle.Empty; // Use Empty instead of 0-size

        // Act
        region.Intersect (rectangle);

        // Assert
        Assert.True (region.IsEmpty ());
    }

    [Theory]
    [InlineData (0, 0, 0, 0)] // Empty by zero size
    [InlineData (0, 0, 0, 10)] // Empty by zero width
    [InlineData (0, 0, 10, 0)] // Empty by zero height
    [InlineData (-5, -5, 0, 0)] // Empty by zero size at negative coords
    [InlineData (10, 10, -5, -5)] // Empty by negative size
    public void Intersect_WithEmptyRegion_ResultsInEmptyRegion (int x, int y, int width, int height)
    {
        // Arrange
        var region = new Region ();
        region.Union (new Rectangle (0, 0, 10, 10));
        region.Union (new Rectangle (20, 0, 10, 10));

        // Create a region that should be considered empty
        var emptyRegion = new Region ();

        if (width <= 0 || height <= 0)
        {
            // For negative or zero dimensions, use an empty region
            emptyRegion = new ();
        }
        else
        {
            emptyRegion = new (new (x, y, width, height));
        }

        // Verify initial states
        Assert.Equal (2, region.GetRectangles ().Length);
        Assert.True (emptyRegion.IsEmpty ());

        // Act
        region.Intersect (emptyRegion);

        // Assert
        Assert.True (region.IsEmpty ());
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void Intersect_WithFullyContainedRectangle_ResultsInSmallerRegion ()
    {
        // Arrange
        var region = new Region (new (0, 0, 10, 10));
        var rectangle = new Rectangle (2, 2, 4, 4);

        // Act
        region.Intersect (rectangle);

        // Assert
        Assert.Single (region.GetRectangles ());
        Assert.Equal (new (2, 2, 4, 4), region.GetRectangles () [0]);
    }

    [Fact]
    public void Intersect_WithMultipleRectanglesInRegion_IntersectsAll ()
    {
        // Arrange
        var region = new Region ();
        region.Union (new Rectangle (0, 0, 5, 5));
        region.Union (new Rectangle (10, 0, 5, 5));
        var rectangle = new Rectangle (2, 2, 10, 2);

        // Act
        region.Intersect (rectangle);

        // Assert
        Assert.Equal (2, region.GetRectangles ().Length);
        Assert.Contains (new Rectangle (2, 2, 3, 2), region.GetRectangles ());
        Assert.Contains (new Rectangle (10, 2, 2, 2), region.GetRectangles ());
    }

    //[Fact]
    //public void Intersect_WithEmptyRegion_ResultsInEmptyRegion ()
    //{
    //    // Arrange
    //    var region = new Region ();
    //    var rectangle = new Rectangle (0, 0, 10, 10);

    //    // Act
    //    region.Intersect (rectangle);

    //    // Assert
    //    Assert.True (region.IsEmpty ());
    //}

    [Fact]
    public void Intersect_WithNonOverlappingRectangle_ResultsInEmptyRegion ()
    {
        // Arrange
        var region = new Region (new (0, 0, 5, 5));
        var rectangle = new Rectangle (10, 10, 5, 5);

        // Act
        region.Intersect (rectangle);

        // Assert
        Assert.True (region.IsEmpty ());
    }

    [Fact]
    public void Intersect_WithNullRegion_ResultsInEmptyRegion ()
    {
        // Arrange
        var region = new Region ();
        region.Union (new Rectangle (0, 0, 10, 10));
        region.Union (new Rectangle (20, 0, 10, 10));

        // Verify initial state
        Assert.Equal (2, region.GetRectangles ().Length);

        // Act
        region.Intersect (null);

        // Assert
        Assert.True (region.IsEmpty ());
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void Intersect_WithPartiallyOverlappingRectangle_ResultsInIntersectedRegion ()
    {
        // Arrange
        var region = new Region (new (0, 0, 5, 5));
        var rectangle = new Rectangle (2, 2, 5, 5);

        // Act
        region.Intersect (rectangle);

        // Assert
        Assert.Single (region.GetRectangles ());
        Assert.Equal (new (2, 2, 3, 3), region.GetRectangles () [0]);
    }

    [Fact]
    public void Intersect_WithRectangle_IntersectsRectangles ()
    {
        var region = new Region (new (10, 10, 50, 50));
        var rect = new Rectangle (30, 30, 50, 50);

        region.Intersect (rect);

        Assert.True (region.Contains (35, 35));
        Assert.False (region.Contains (20, 20));
    }

    [Fact]
    public void Intersect_WithRegion_IntersectsRegions ()
    {
        var region1 = new Region (new (10, 10, 50, 50));
        var region2 = new Region (new (30, 30, 50, 50));

        region1.Intersect (region2.GetBounds ());

        Assert.True (region1.Contains (35, 35));
        Assert.False (region1.Contains (20, 20));
    }

    [Fact]
    public void Intersect_ImmediateNormalization_AffectsRectangleOrder ()
    {
        // Create a region with two overlapping rectangles
        var region1 = new Region (new (0, 0, 4, 4)); // 0,0 to 4,4

        // Intersect with a region that partially overlaps
        var region2 = new Region (new (2, 2, 4, 4)); // 2,2 to 6,6
        region1.Intersect (region2);

        // Get the resulting rectangles
        Rectangle [] result = region1.GetRectangles ();

        // Expected behavior from original Region: 
        // Intersect immediately produces a single rectangle (2,2,2,2)
        Assert.Single (result); // Original has 1 rectangle due to immediate processing
        Assert.Equal (new (2, 2, 2, 2), result [0]);

        // My updated Region defers normalization after Intersect, 
        // so GetRectangles() might merge differently or preserve order differently,
        // potentially failing the exact match or count due to _isDirty
    }

    [Fact]
    public void IsEmpty_AfterClear_ReturnsTrue ()
    {
        // Arrange
        var region = new Region (new (0, 0, 10, 10));

        // Act
        region.Intersect (Rectangle.Empty);

        // Assert
        Assert.True (region.IsEmpty ());
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void IsEmpty_AfterComplement_ReturnsCorrectState ()
    {
        // Test 1: Complement a region with bounds that fully contain it
        var region = new Region (new (2, 2, 5, 5)); // Small inner rectangle
        region.Complement (new (0, 0, 10, 10)); // Larger outer bounds
        Assert.False (region.IsEmpty ()); // Should have area around the original rectangle

        // Test 2: Complement with bounds equal to the region
        region = new (new (0, 0, 10, 10));
        region.Complement (new (0, 0, 10, 10));
        Assert.True (region.IsEmpty ()); // Should be empty as there's no area left

        // Test 3: Complement with empty bounds
        region = new (new (0, 0, 10, 10));
        region.Complement (Rectangle.Empty);
        Assert.True (region.IsEmpty ()); // Should be empty as there's no bounds
    }

    [Fact]
    public void IsEmpty_AfterExclude_ReturnsTrue ()
    {
        // Arrange
        var region = new Region (new (0, 0, 10, 10));

        // Act
        region.Exclude (new Rectangle (0, 0, 10, 10));

        // Assert
        Assert.True (region.IsEmpty ());
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void IsEmpty_AfterUnion_ReturnsFalse ()
    {
        // Arrange
        var region = new Region ();
        region.Union (new Rectangle (0, 0, 10, 10));

        // Assert
        Assert.False (region.IsEmpty ());
        Assert.Single (region.GetRectangles ());
    }

    [Fact]
    public void IsEmpty_EmptyRegion_ReturnsTrue ()
    {
        var region = new Region ();
        Assert.True (region.IsEmpty ());
    }

    [Fact]
    public void IsEmpty_MultipleOperations_ReturnsExpectedResult ()
    {
        // Arrange
        var region = new Region ();

        // Act & Assert - Should be empty initially
        Assert.True (region.IsEmpty ());

        // Add a rectangle - Should not be empty
        region.Union (new Rectangle (0, 0, 10, 10));
        Assert.False (region.IsEmpty ());

        // Exclude the same rectangle - Should be empty again
        region.Exclude (new Rectangle (0, 0, 10, 10));
        Assert.True (region.IsEmpty ());

        // Add two rectangles - Should not be empty
        region.Union (new Rectangle (0, 0, 5, 5));
        region.Union (new Rectangle (10, 10, 5, 5));
        Assert.False (region.IsEmpty ());
    }

    [Fact]
    public void IsEmpty_NewRegion_ReturnsTrue ()
    {
        // Arrange
        var region = new Region ();

        // Act & Assert
        Assert.True (region.IsEmpty ());
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void IsEmpty_ReturnsCorrectResult ()
    {
        var region = new Region ();

        Assert.True (region.IsEmpty ());

        region.Union (new Rectangle (10, 10, 50, 50));

        Assert.False (region.IsEmpty ());
    }

    [Theory]
    [InlineData (0, 0, 1, 1)] // 1x1 at origin
    [InlineData (10, 10, 5, 5)] // 5x5 at (10,10)
    [InlineData (-5, -5, 10, 10)] // Negative coordinates
    public void IsEmpty_ValidRectangle_ReturnsFalse (int x, int y, int width, int height)
    {
        // Arrange
        var region = new Region (new (x, y, width, height));

        // Assert
        Assert.False (region.IsEmpty ());
        Assert.Single (region.GetRectangles ());
    }

    [Theory]
    [InlineData (0, 0, 0, 0)] // Zero size
    [InlineData (0, 0, 0, 10)] // Zero width
    [InlineData (0, 0, 10, 0)] // Zero height
    [InlineData (-5, -5, 0, 0)] // Zero size at negative coords
    public void IsEmpty_ZeroSizeRectangle_ReturnsCorrectState (int x, int y, int width, int height)
    {
        var region = new Region (new (x, y, width, height));

        // Only check IsEmpty() since Rectangle(0,0,0,0) is still stored
        Assert.True (region.IsEmpty ());
    }

    //[Fact]
    //public void MinimalUnion_SingleRectangle_DoesNotChange ()
    //{
    //    var region = new Region (new Rectangle (0, 0, 10, 10));
    //    region.MinimalUnion (new Rectangle (0, 0, 10, 10));

    //    Assert.Single (region.GetRectangles ());
    //    Assert.Equal (new Rectangle (0, 0, 10, 10), region.GetRectangles ().First ());
    //}

    //[Fact]
    //public void MinimalUnion_OverlappingRectangles_MergesIntoOne ()
    //{
    //    var region = new Region (new Rectangle (0, 0, 10, 10));
    //    region.MinimalUnion (new Rectangle (5, 0, 10, 10));

    //    Assert.Single (region.GetRectangles ());
    //    Assert.Equal (new Rectangle (0, 0, 15, 10), region.GetRectangles ().First ());
    //}

    //[Fact]
    //public void MinimalUnion_AdjacentRectangles_MergesIntoOne ()
    //{
    //    var region = new Region (new Rectangle (0, 0, 10, 10));
    //    region.MinimalUnion (new Rectangle (10, 0, 10, 10));

    //    Assert.Single (region.GetRectangles ());
    //    Assert.Equal (new Rectangle (0, 0, 20, 10), region.GetRectangles ().First ());
    //}

    //[Fact]
    //public void MinimalUnion_SeparateRectangles_KeepsBoth ()
    //{
    //    var region = new Region (new Rectangle (0, 0, 10, 10));
    //    region.MinimalUnion (new Rectangle (20, 20, 10, 10));

    //    Assert.Equal (2, region.GetRectangles ().Length);
    //    Assert.Contains (new Rectangle (0, 0, 10, 10), region.GetRectangles ());
    //    Assert.Contains (new Rectangle (20, 20, 10, 10), region.GetRectangles ());
    //}

    //[Fact]
    //public void MinimalUnion_ComplexMerging_ProducesMinimalSet ()
    //{
    //    var region = new Region (new Rectangle (0, 0, 10, 10));
    //    region.MinimalUnion (new Rectangle (10, 0, 10, 10));
    //    region.MinimalUnion (new Rectangle (0, 10, 10, 10));
    //    region.MinimalUnion (new Rectangle (10, 10, 10, 10));

    //    Assert.Single (region.GetRectangles ());
    //    Assert.Equal (new Rectangle (0, 0, 20, 20), region.GetRectangles ().First ());
    //}


    [Fact]
    public void Intersect_ViewportLimitsDrawnRegion ()
    {
        // Arrange: Create regions for viewport and drawn content
        var viewport = new Region (new Rectangle (0, 0, 100, 100));        // Viewport
        var drawnRegion = new Region (new Rectangle (50, 50, 200, 200));    // Larger drawn content

        // Act: Intersect drawn region with viewport
        drawnRegion.Intersect (viewport);

        // Assert: Drawn region should be limited to viewport
        var rectangles = drawnRegion.GetRectangles ();
        Assert.Single (rectangles);
        Assert.Equal (new Rectangle (50, 50, 50, 50), rectangles [0]);      // Limited to viewport bounds
    }

    //[Fact]


    //public void MinimalUnion_HorizontalMerge_MergesToSingleRectangle ()
    //{
    //    // Arrange: Create a region with a rectangle at (0,0,5,5)
    //    var region = new Region (new Rectangle (0, 0, 5, 5));

    //    // Act: Merge an adjacent rectangle on the right using MinimalUnion
    //    region.MinimalUnion (new Rectangle (5, 0, 5, 5));
    //    var result = region.GetRectangles ();

    //    // Assert: Expect a single merged rectangle covering (0,0,10,5)
    //    Assert.Single (result);
    //    Assert.Equal (new Rectangle (0, 0, 10, 5), result [0]);
    //}

    //[Fact]
    //public void MinimalUnion_VerticalMerge_MergesToSingleRectangle ()
    //{
    //    // Arrange: Create a region with a rectangle at (0,0,5,5)
    //    var region = new Region (new Rectangle (0, 0, 5, 5));

    //    // Act: Merge an adjacent rectangle below using MinimalUnion
    //    region.MinimalUnion (new Rectangle (0, 5, 5, 5));
    //    var result = region.GetRectangles ();

    //    // Assert: Expect a single merged rectangle covering (0,0,5,10)
    //    Assert.Single (result);
    //    Assert.Equal (new Rectangle (0, 0, 5, 10), result [0]);
    //}

    //[Fact]
    //public void MinimalUnion_OverlappingMerge_MergesToSingleRectangle ()
    //{
    //    // Arrange: Create a region with a rectangle that overlaps with the next one horizontally
    //    var region = new Region (new Rectangle (0, 0, 6, 5));

    //    // Act: Merge an overlapping rectangle using MinimalUnion
    //    region.MinimalUnion (new Rectangle (4, 0, 6, 5));
    //    var result = region.GetRectangles ();

    //    // Assert: Expect a single merged rectangle covering (0,0,10,5)
    //    Assert.Single (result);
    //    Assert.Equal (new Rectangle (0, 0, 10, 5), result [0]);
    //}

    //[Fact]
    //public void MinimalUnion_NonAdjacentRectangles_NoMergeOccurs ()
    //{
    //    // Arrange: Create a region with one rectangle
    //    var region = new Region (new Rectangle (0, 0, 5, 5));

    //    // Act: Merge with a rectangle that does not touch the first
    //    region.MinimalUnion (new Rectangle (6, 0, 5, 5));
    //    var result = region.GetRectangles ();

    //    // Assert: Expect two separate rectangles since they are not adjacent
    //    Assert.Equal (2, result.Length);
    //    Assert.Contains (new Rectangle (0, 0, 5, 5), result);
    //    Assert.Contains (new Rectangle (6, 0, 5, 5), result);
    //}

    //[Fact]
    //public void MinimalUnion_MultipleMerge_FormsSingleContiguousRectangle ()
    //{
    //    // Arrange: Four small rectangles that form a contiguous 6x6 block
    //    var region = new Region (new Rectangle (0, 0, 3, 3));

    //    // Act: Merge adjacent rectangles one by one using MinimalUnion
    //    region.MinimalUnion (new Rectangle (3, 0, 3, 3)); // Now covers (0,0,6,3)
    //    region.MinimalUnion (new Rectangle (0, 3, 3, 3)); // Add bottom-left
    //    region.MinimalUnion (new Rectangle (3, 3, 3, 3)); // Add bottom-right to complete block
    //    var result = region.GetRectangles ();

    //    // Assert: Expect a single merged rectangle covering (0,0,6,6)
    //    Assert.Single (result);
    //    Assert.Equal (new Rectangle (0, 0, 6, 6), result [0]);
    //}

    [Fact]
    public void Translate_EmptyRegionAfterEmptyCombine_NoEffect ()
    {
        // Arrange: Create region and combine with empty rectangles
        var region = new Region ();
        region.Combine (new Rectangle (0, 0, 0, 0), RegionOp.Union); // Empty rectangle
        region.Combine (new Region (), RegionOp.Union); // Empty region

        // Act: Translate by (10, 20)
        region.Translate (10, 20);

        // Assert: Still empty
        Assert.Empty (region.GetRectangles ());
    }

    [Fact]
    public void Union_Rectangle_AddsToRegion ()
    {
        var region = new Region ();
        region.Union (new Rectangle (10, 10, 50, 50));
        Assert.False (region.IsEmpty ());
        Assert.True (region.Contains (20, 20));
    }

    [Fact]
    public void Union_Region_MergesRegions ()
    {
        var region1 = new Region (new (10, 10, 50, 50));
        var region2 = new Region (new (30, 30, 50, 50));
        region1.Union (region2);
        Assert.True (region1.Contains (20, 20));
        Assert.True (region1.Contains (40, 40));
    }

    [Fact]
    public void Union_Third_Rect_Covering_Two_Disjoint_Merges ()
    {
        var origRegion = new Region ();

        var region1 = new Region (new (0, 0, 1, 1));
        var region2 = new Region (new (1, 0, 1, 1));

        origRegion.Union (region1);
        origRegion.Union (region2);

        Assert.Equal (new Rectangle (0, 0, 2, 1), origRegion.GetBounds ());
        Assert.Equal (2, origRegion.GetRectangles ().Length);

        origRegion.Union (new Region (new (0, 0, 4, 1)));

        Assert.Equal (new Rectangle (0, 0, 4, 1), origRegion.GetBounds ());
        Assert.Equal (3, origRegion.GetRectangles ().Length);
    }

    [Fact]
    public void MinimalUnion_Third_Rect_Covering_Two_Disjoint_Merges ()
    {
        var origRegion = new Region ();

        var region1 = new Region (new (0, 0, 1, 1));
        var region2 = new Region (new (1, 0, 1, 1));

        origRegion.Union (region1);
        origRegion.Union (region2);

        Assert.Equal (new Rectangle (0, 0, 2, 1), origRegion.GetBounds ());
        Assert.Equal (2, origRegion.GetRectangles ().Length);

        origRegion.MinimalUnion (new Region (new (0, 0, 4, 1)));

        Assert.Equal (new Rectangle (0, 0, 4, 1), origRegion.GetBounds ());
        Assert.Single (origRegion.GetRectangles ());
    }

    /// <summary>
    ///     Proves MergeRegion does not overly combine regions.
    /// </summary>
    [Fact]
    public void Union_Region_MergesRegions_NonOverlapping ()
    {
        //  012345
        // 0+++
        // 1+ + 
        // 2+++
        // 3   ***
        // 4   * *
        // 5   ***

        var region1 = new Region (new (0, 0, 3, 3));
        var region2 = new Region (new (3, 3, 3, 3));
        region1.Union (region2);

        // Positive
        Assert.True (region1.Contains (0, 0));
        Assert.True (region1.Contains (1, 1));
        Assert.True (region1.Contains (2, 2));
        Assert.True (region1.Contains (4, 4));
        Assert.True (region1.Contains (5, 5));

        // Negative
        Assert.False (region1.Contains (0, 3));
        Assert.False (region1.Contains (3, 0));
        Assert.False (region1.Contains (6, 6));
    }

    /// <summary>
    ///     Proves MergeRegion does not overly combine regions.
    /// </summary>
    [Fact]
    public void Union_Region_MergesRegions_Overlapping ()
    {
        //  01234567
        // 0+++++
        // 1+   +
        // 2+   +
        // 3+  *****
        // 4+++*   *
        // 5   *   *
        // 6   *   *
        // 7   *****

        var region1 = new Region (new (0, 0, 5, 5));
        var region2 = new Region (new (3, 3, 5, 5));
        region1.Union (region2);

        // Positive
        Assert.True (region1.Contains (0, 0));
        Assert.True (region1.Contains (1, 1));
        Assert.True (region1.Contains (4, 4));
        Assert.True (region1.Contains (7, 7));

        // Negative
        Assert.False (region1.Contains (0, 5));
        Assert.False (region1.Contains (5, 0));
        Assert.False (region1.Contains (8, 8));
        Assert.False (region1.Contains (8, 8));
    }

    [Fact]
    public void Union_WithRectangle_AddsRectangle ()
    {
        var region = new Region ();
        var rect = new Rectangle (10, 10, 50, 50);

        region.Union (rect);

        Assert.True (region.Contains (20, 20));
        Assert.False (region.Contains (100, 100));
    }

    [Fact]
    public void Union_WithRegion_AddsRegion ()
    {
        var region1 = new Region (new (10, 10, 50, 50));
        var region2 = new Region (new (30, 30, 50, 50));

        region1.Union (region2.GetBounds ());

        Assert.True (region1.Contains (20, 20));
        Assert.True (region1.Contains (40, 40));
    }

    [Fact]
    public void Intersect_DeferredNormalization_PreservesSegments ()
    {
        var region = new Region (new (0, 0, 3, 1)); // Horizontal
        region.Union (new Rectangle (1, 0, 1, 2)); // Vertical
        region.Intersect (new Rectangle (0, 0, 2, 2)); // Clip

        Rectangle [] result = region.GetRectangles ();

        // The clip produces (0,0,1,1) and (1,0,1,2). The third rect from the Union,
        // (2,0,1,1), clips to (2,0,0,1) — zero width — which is correctly excluded.
        Assert.Equal (2, result.Length);
        Assert.Contains (new Rectangle (0, 0, 1, 1), result);
        Assert.Contains (new Rectangle (1, 0, 1, 2), result);
    }

    [Fact]
    public void MergeRectangles_Sort_Handles_Coincident_Events_Without_Crashing ()
    {
        // Arrange: Create rectangles designed to produce coincident start/end events
        // Rect1 ends at x=10. Rect2 and Rect3 start at x=10.
        // Rect4 ends at x=15. Rect5 starts at x=15.
        var rect1 = new Rectangle (0, 0, 10, 10); // Ends at x=10
        var rect2 = new Rectangle (10, 0, 10, 5); // Starts at x=10
        var rect3 = new Rectangle (10, 5, 10, 5); // Starts at x=10, adjacent to rect2 vertically
        var rect4 = new Rectangle (5, 10, 10, 5); // Ends at x=15
        var rect5 = new Rectangle (15, 10, 5, 5); // Starts at x=15

        var combinedList = new List<Rectangle> { rect1, rect2, rect3, rect4, rect5 };

        // Act & Assert:
        // The core assertion is that calling MergeRectangles with this list
        // does *not* throw the ArgumentException related to sorting.
        var exception = Record.Exception (() => Region.MergeRectangles (combinedList, false));

        // Assert
        Assert.Null (exception);

        // Optional secondary assertion: Check if the merge produced a reasonable number of rectangles
        // This isn't strictly necessary for proving the sort fix, but can be useful.
        // var merged = Region.MergeRectangles(combinedList, false);
        // Assert.True(merged.Count > 0 && merged.Count <= combinedList.Count);
    }

    [Fact]
    public void MergeRectangles_Sort_Handles_Multiple_Coincident_Starts ()
    {
        // Arrange: Multiple rectangles starting at the same X
        var rect1 = new Rectangle (5, 0, 10, 5);
        var rect2 = new Rectangle (5, 5, 10, 5);
        var rect3 = new Rectangle (5, 10, 10, 5);
        var combinedList = new List<Rectangle> { rect1, rect2, rect3 };

        // Act & Assert: Ensure no sorting exception
        var exception = Record.Exception (() => Region.MergeRectangles (combinedList, false));
        Assert.Null (exception);
    }

    [Fact]
    public void MergeRectangles_Sort_Handles_Multiple_Coincident_Ends ()
    {
        // Arrange: Multiple rectangles ending at the same X
        var rect1 = new Rectangle (0, 0, 10, 5);
        var rect2 = new Rectangle (0, 5, 10, 5);
        var rect3 = new Rectangle (0, 10, 10, 5);
        var combinedList = new List<Rectangle> { rect1, rect2, rect3 };

        // Act & Assert: Ensure no sorting exception
        var exception = Record.Exception (() => Region.MergeRectangles (combinedList, false));
        Assert.Null (exception);
    }

    [Fact]
    public void MergeRectangles_Sort_Handles_Coincident_Mixed_Events_Without_Crashing ()
    {
        // Arrange: Create rectangles specifically designed to produce multiple
        // Start AND End events at the same x-coordinate (e.g., x=10),
        // mimicking the pattern observed in the crash log.
        var rectA = new Rectangle (0, 0, 10, 5);  // Ends at x=10, y=[0, 5)
        var rectB = new Rectangle (0, 10, 10, 5); // Ends at x=10, y=[10, 15)
        var rectC = new Rectangle (10, 0, 10, 5); // Starts at x=10, y=[0, 5)
        var rectD = new Rectangle (10, 10, 10, 5); // Starts at x=10, y=[10, 15)

        // Add another set at a different X to increase complexity
        var rectE = new Rectangle (5, 20, 10, 5); // Ends at x=15, y=[20, 25)
        var rectF = new Rectangle (5, 30, 10, 5); // Ends at x=15, y=[30, 35)
        var rectG = new Rectangle (15, 20, 10, 5); // Starts at x=15, y=[20, 25)
        var rectH = new Rectangle (15, 30, 10, 5); // Starts at x=15, y=[30, 35)

        // Add some unrelated rectangles
        var rectI = new Rectangle (0, 40, 5, 5);
        var rectJ = new Rectangle (100, 100, 5, 5);


        var combinedList = new List<Rectangle> {
            rectA, rectB, rectC, rectD,
            rectE, rectF, rectG, rectH,
            rectI, rectJ
        };

        // Act & Assert:
        // Call MergeRectangles with the current code.
        // This test *should* fail by throwing ArgumentException due to unstable sort.
        var exception = Record.Exception (() => Region.MergeRectangles (combinedList, false));

        // Assert that no exception was thrown (this assertion will fail with the current code)
        Assert.Null (exception);
    }

    [Fact]
    public void MergeRectangles_Sort_Reproduces_UICatalog_Crash_Pattern_Directly ()
    {
        // Arrange: Rectangles derived *directly* from the events list that caused the crash at x=67
        // This aims to replicate the exact problematic pattern.
        var rect_End_67_7_30 = new Rectangle (60, 7, 7, 23);  // Ends at x=67, y=[7, 30) -> Event [33]
        var rect_Start_67_2_30 = new Rectangle (67, 2, 10, 28); // Starts at x=67, y=[2, 30) -> Event [34]
        var rect_Start_67_1_1 = new Rectangle (67, 1, 10, 0);  // Starts at x=67, y=[1, 1) -> Event [49] (Height 0)
        var rect_End_67_1_1 = new Rectangle (60, 1, 7, 0);   // Ends at x=67, y=[1, 1) -> Event [64] (Height 0)

        // Add rectangles for x=94/95 pattern
        var rect_End_94_1_30 = new Rectangle (90, 1, 4, 29); // Ends at x=94, y=[1, 30) -> Event [55]
        var rect_Start_94_1_1 = new Rectangle (94, 1, 10, 0); // Starts at x=94, y=[1, 1) -> Event [56]
        var rect_Start_94_7_30 = new Rectangle (94, 7, 10, 23); // Starts at x=94, y=[7, 30) -> Event [58]

        var rect_End_95_1_1 = new Rectangle (90, 1, 5, 0); // Ends at x=95, y=[1, 1) -> Event [57]
        var rect_End_95_7_30 = new Rectangle (90, 7, 5, 23); // Ends at x=95, y=[7, 30) -> Event [59]
        var rect_Start_95_0_30 = new Rectangle (95, 0, 10, 30); // Starts at x=95, y=[0, 30) -> Event [60]


        var combinedList = new List<Rectangle> {
            rect_End_67_7_30, rect_Start_67_2_30, rect_Start_67_1_1, rect_End_67_1_1,
            rect_End_94_1_30, rect_Start_94_1_1, rect_Start_94_7_30,
            rect_End_95_1_1, rect_End_95_7_30, rect_Start_95_0_30
        };

        // Act & Assert:
        // Call MergeRectangles. This test is specifically designed to fail with the current code.
        var exception = Record.Exception (() => Region.MergeRectangles (combinedList, false));

        // Assert that no exception was thrown (this assertion *should* fail with the current code)
        Assert.Null (exception);
    }

    [Fact]
    public void MergeRectangles_Sort_Reproduces_UICatalog_Crash_From_Captured_Data ()
    {
        // Arrange: The exact list of rectangles captured during the UICatalog crash
        var rectanglesFromCrash = new List<Rectangle> {
            new Rectangle(38, 7, 1, 11),
            new Rectangle(39, 7, 5, 23),
            new Rectangle(44, 7, 1, 23),
            new Rectangle(45, 7, 6, 23),
            new Rectangle(51, 7, 1, 23),
            new Rectangle(52, 7, 1, 23),
            new Rectangle(53, 7, 1, 23),
            new Rectangle(54, 7, 1, 23),
            new Rectangle(55, 7, 1, 23),
            new Rectangle(56, 7, 1, 23),
            new Rectangle(57, 7, 1, 23),
            new Rectangle(58, 7, 1, 23),
            new Rectangle(59, 7, 1, 23),
            new Rectangle(60, 7, 1, 23),
            new Rectangle(61, 7, 3, 23),
            new Rectangle(64, 7, 1, 23),
            new Rectangle(65, 7, 2, 23),
            new Rectangle(67, 2, 2, 28),
            new Rectangle(69, 2, 3, 28),
            new Rectangle(72, 2, 3, 28),
            new Rectangle(75, 2, 1, 28),
            new Rectangle(76, 2, 2, 28),
            new Rectangle(78, 2, 2, 28),
            new Rectangle(80, 7, 1, 23),
            new Rectangle(81, 1, 7, 29),
            new Rectangle(88, 1, 1, 29),
            new Rectangle(89, 1, 2, 29),
            new Rectangle(91, 1, 3, 29),
            new Rectangle(94, 1, 1, 0),   // Note: Zero height
            new Rectangle(94, 7, 1, 23),
            new Rectangle(95, 0, 1, 30),
            new Rectangle(96, 0, 23, 30),
            new Rectangle(67, 1, 0, 0)    // Note: Zero width and height
        };

        // Act & Assert:
        // Call MergeRectangles with the current code.
        // This test *should* fail by throwing ArgumentException due to unstable sort.
        var exception = Record.Exception (() => Region.MergeRectangles (rectanglesFromCrash, false));

        // Assert that no exception was thrown (this assertion will fail with the current code)
        Assert.Null (exception);
    }

    // Claude - Opus 4.7
    [Fact]
    public void Combine_XOR_ProducesSymmetricDifference ()
    {
        // Issue #5167: XOR mutated `this` before computing `region - this`,
        // producing a non-symmetric result. With A=(0,0,2,2) and B=(1,1,2,2)
        // the only intersecting cell is (1,1); XOR(A,B) must contain every
        // other cell from A and B but NOT (1,1).
        Region a = new (new (0, 0, 2, 2));
        Region b = new (new (1, 1, 2, 2));

        a.Combine (b, RegionOp.XOR);

        // Cells exclusive to A
        Assert.True (a.Contains (0, 0));
        Assert.True (a.Contains (1, 0));
        Assert.True (a.Contains (0, 1));

        // Cells exclusive to B
        Assert.True (a.Contains (2, 1));
        Assert.True (a.Contains (1, 2));
        Assert.True (a.Contains (2, 2));

        // The intersecting cell must be excluded from a symmetric difference.
        Assert.False (a.Contains (1, 1));
    }
}