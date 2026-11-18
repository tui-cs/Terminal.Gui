using Xunit.Sdk;

namespace DrawingTests.RegionTests;

public class DifferenceTests
{

    [Fact]
    public void Difference_Rectangle_ExcludesFromRegion ()
    {
        var region = new Region (new (10, 10, 50, 50));
        region.Combine (new Rectangle (20, 20, 20, 20), RegionOp.Difference);
        Assert.False (region.Contains (25, 25));
        Assert.True (region.Contains (15, 15));
    }

    [Fact]
    public void Difference_Region_ExcludesRegions ()
    {
        var region1 = new Region (new (10, 10, 50, 50));
        var region2 = new Region (new (20, 20, 20, 20));
        region1.Combine (region2, RegionOp.Difference);
        Assert.False (region1.Contains (25, 25));
        Assert.True (region1.Contains (15, 15));
    }

    [Fact]
    public void Difference_WithRectangle_ExcludesRectangle ()
    {
        var region = new Region (new (10, 10, 50, 50));
        var rect = new Rectangle (30, 30, 50, 50);

        region.Combine (rect, RegionOp.Difference);

        Assert.True (region.Contains (20, 20));
        Assert.False (region.Contains (35, 35));
    }

    [Fact]
    public void Difference_WithRegion_ExcludesRegion ()
    {
        var region1 = new Region (new (10, 10, 50, 50));
        var region2 = new Region (new (30, 30, 50, 50));

        region1.Combine (region2, RegionOp.Difference);

        Assert.True (region1.Contains (20, 20));
        Assert.False (region1.Contains (35, 35));
    }

    [Fact]
    public void Difference_ContainedRectangle_ExcludesRectangle ()
    {

        /*
        INPUT: (top-left origin, x→, y↓):

           x=0 1 2 3 4 5
        y=0  A A A A A A
        y=1  A A A A A A
        y=2  A A B B A A
        y=3  A A B B A A
        y=4  A A A A A A
        y=5  A A A A A A

        */


        var regionA = new Region (new (0, 0, 6, 6));
        var rectangleB = new Rectangle (2, 2, 2, 2);

        regionA.Combine (rectangleB, RegionOp.Difference);

        Assert.True (regionA.Contains (0, 0));
        Assert.True (regionA.Contains (1, 1));
        Assert.True (regionA.Contains (4, 4));
        Assert.True (regionA.Contains (5, 5));

        Assert.False (regionA.Contains (2, 2));
        Assert.False (regionA.Contains (3, 3));
    }

    [Fact]
    public void Difference_ContainedRegion_ExcludesRegion ()
    {

        /*
        INPUT: (top-left origin, x→, y↓):

           x=0 1 2 3 4 5
        y=0  A A A A A A
        y=1  A A A A A A
        y=2  A A B B A A
        y=3  A A B B A A
        y=4  A A A A A A
        y=5  A A A A A A

        */


        var regionA = new Region (new (0, 0, 6, 6));
        var regionB = new Region (new (2, 2, 2, 2));

        regionA.Combine (regionB, RegionOp.Difference);

        Assert.True (regionA.Contains (0, 0));
        Assert.True (regionA.Contains (1, 1));
        Assert.True (regionA.Contains (4, 4));
        Assert.True (regionA.Contains (5, 5));

        Assert.False (regionA.Contains (2, 2));
        Assert.False (regionA.Contains (3, 3));
    }

    [Fact]
    public void Difference_NonRectangularRegion_ExcludesRegion ()
    {

        /*
        INPUT: (top-left origin, x→, y↓):

           x=0 1 2 3 4 5
        y=0  A A A A A A
        y=1  A A A A A A
        y=2  A A B B A A
        y=3  A A B A A A
        y=4  A A A A A A
        y=5  A A A A A A

        */

        var regionA = new Region (new (0, 0, 6, 6));

        var regionB = new Region ();
        regionB.Combine (new Rectangle (2, 2, 2, 1), RegionOp.MinimalUnion);
        regionB.Combine (new Rectangle (2, 3, 1, 1), RegionOp.MinimalUnion);

        // regionB is a non-rectangular region that looks like this:
        // x=   0 1 2 3 4 5
        // y=0  . . . . . .
        // y=1  . . . . . .
        // y=2  . . B B . .
        // y=3  . . B . . .
        // y=4  . . . . . .
        // y=5  . . . . . .

        Assert.True (regionB.Contains (2, 2));
        Assert.True (regionB.Contains (3, 2));
        Assert.True (regionB.Contains (2, 3));
        Assert.False (regionB.Contains (3, 3));

        regionA.Combine (regionB, RegionOp.Difference);

        Assert.True (regionA.Contains (0, 0));
        Assert.True (regionA.Contains (1, 1));
        Assert.True (regionA.Contains (3, 3));
        Assert.True (regionA.Contains (4, 4));
        Assert.True (regionA.Contains (5, 5));

        Assert.False (regionA.Contains (2, 2));
        Assert.False (regionA.Contains (3, 2));
        Assert.False (regionA.Contains (2, 3));

    }

}