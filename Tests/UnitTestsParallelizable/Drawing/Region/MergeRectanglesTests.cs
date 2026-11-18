namespace DrawingTests.RegionTests;


public class MergeRectanglesTests
{

    [Fact]
    public void MergeRectangles_ComplexAdjacentRectangles_NoOverlap ()
    {
        /*
            INPUT: Complex arrangement of four adjacent rectangles forming a hollow square ring.
            Top-left origin (0,0), x→, y↓:

              x=0 1 2 3 4
            y=0   A A A
            y=1 B       C
            y=2 B       C
            y=3 B       C
            y=4   D D D

            Rectangles (width × height):
              A: (1,0,3,1)  // top edge
              B: (0,1,1,3)  // left edge
              C: (4,1,1,3)  // right edge
              D: (1,4,3,1)  // bottom edge

            They only touch corners or edges, with no overlapping areas.
            The expected result is exactly these four rectangles, unmerged.
        */

        List<Rectangle> rectangles = new ()
        {
            new (1, 0, 3, 1), // A
            new (0, 1, 1, 3), // B
            new (4, 1, 1, 3), // C
            new (1, 4, 3, 1) // D
        };

        List<Rectangle> merged = Region.MergeRectangles (rectangles, false);

        // Because there's no overlapping area, the method shouldn't merge any of them.
        Assert.Equal (4, merged.Count);
        Assert.Contains (new (1, 0, 3, 1), merged);
        Assert.Contains (new (0, 1, 1, 3), merged);
        Assert.Contains (new (4, 1, 1, 3), merged);
        Assert.Contains (new (1, 4, 3, 1), merged);
    }

    [Fact]
    public void MergeRectangles_ComplexContainedRectangles_AllMergeIntoBoundingRect ()
    {
        /*
        INPUT: (top-left origin, x→, y↓):

           x=0 1 2 3 4 5
        y=0  A A A A A A
        y=1  A . . . . A
        y=2  A . B B . A
        y=3  A . B B . A
        y=4  A . . . C C
        y=5  A A A A C C

        Where:
          A = (0,0,6,6)  // Large bounding rectangle
          B = (2,2,2,2)  // Fully contained inside A
          C = (4,4,2,2)  // Also fully contained inside A
     */

        List<Rectangle> rectangles = new ()
        {
            new (0, 0, 6, 6), // A
            new (2, 2, 2, 2), // B inside A
            new (4, 4, 2, 2) // C inside A
        };

        List<Rectangle> merged = Region.MergeRectangles (rectangles, minimize: false);

        /*
           OUTPUT: The expected result should be a minimal set of non-overlapping rectangles
           that cover the same area as the input rectangles.

            x=0 1 2 3 4 5
         y=0  a a b b c c
         y=1  a a b b c c
         y=2  a a b b c c
         y=3  a a b b c c
         y=4  a a b b c c
         y=5  a a b b c c

       */

        Assert.Equal (3, merged.Count);
        Assert.Contains (new (0, 0, 2, 6), merged); // a
        Assert.Contains (new (2, 0, 2, 6), merged); // b
        Assert.Contains (new (4, 0, 2, 6), merged); // c
    }

    [Fact]
    public void MergeRectangles_ComplexOverlap_ReturnsMergedRectangles ()
    {
        /*
            INPUT: Visual diagram treating (0,0) as top-left, x increasing to the right, y increasing downward:

                  x=0 1 2 3 4 5 6 ...
              y=0   A A
              y=1   A B B
              y=2     B B
              y=3         C C
              y=4         C D D
              y=5           D D

            A overlaps B slightly; C overlaps D slightly. The union of A & B forms one rectangle,
            and the union of C & D forms another.
        */

        List<Rectangle> rectangles = new ()
        {
            // A
            new (0, 0, 2, 2),

            // B
            new (1, 1, 2, 2),

            // C
            new (3, 3, 2, 2),

            // D
            new (4, 4, 2, 2)
        };

        List<Rectangle> merged = Region.MergeRectangles (rectangles, false);

        /*
           OUTPUT:  Merged fragments (top-left origin, x→, y↓).
           Lowercase letters a..f show the six sub-rectangles:

              x=0 1 2 3 4 5
           y=0  a b
           y=1  a b c
           y=2    b c
           y=3        d e
           y=4        d e f
           y=5          e f
        */

        Assert.Equal (6, merged.Count);

        Assert.Contains (new (0, 0, 1, 2), merged); // a
        Assert.Contains (new (1, 0, 1, 3), merged); // b
        Assert.Contains (new (2, 1, 1, 2), merged); // c
        Assert.Contains (new (3, 3, 1, 2), merged); // d
        Assert.Contains (new (4, 3, 1, 3), merged); // e
        Assert.Contains (new (5, 4, 1, 2), merged); // f
    }

    [Fact]
    public void MergeRectangles_NoOverlap_ReturnsSameRectangles ()
    {
        List<Rectangle> rectangles = new ()
        {
            new (0, 0, 10, 10),
            new (20, 20, 10, 10),
            new (40, 40, 10, 10)
        };

        List<Rectangle> result = Region.MergeRectangles (rectangles, false);

        Assert.Equal (3, result.Count);
        Assert.Contains (new (0, 0, 10, 10), result);
        Assert.Contains (new (20, 20, 10, 10), result);
        Assert.Contains (new (40, 40, 10, 10), result);
    }


    [Fact]
    public void MergeRectangles_EmptyRectangles_ReturnsEmptyList ()
    {
        // Arrange: Create list of empty rectangles
        var emptyRectangles = new List<Rectangle> { new (0, 0, 0, 0), new (0, 0, 0, 0) };

        // Act: Call MergeRectangles with granular output
        var result = Region.MergeRectangles (emptyRectangles, minimize: false);

        // Assert: Result is empty
        Assert.Empty (result);
    }

}
