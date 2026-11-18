using UnitTests;
namespace DrawingTests.Lines;

public class StraightLineExtensionsTests (ITestOutputHelper output)
{
    [Fact]
    public void LineCanvasIntegrationTest ()
    {
        var lc = new LineCanvas ();
        lc.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Single);
        lc.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        lc.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Single);
        lc.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌────────┐
│        │
│        │
│        │
└────────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );
        IReadOnlyCollection<StraightLine> origLines = lc.Lines;

        lc = new (origLines.Exclude (Point.Empty, 10, Orientation.Horizontal));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
│        │
│        │
│        │
└────────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (0, 1), 10, Orientation.Horizontal));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌────────┐
          
│        │
│        │
└────────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (0, 2), 10, Orientation.Horizontal));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌────────┐
│        │
          
│        │
└────────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (0, 3), 10, Orientation.Horizontal));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌────────┐
│        │
│        │
          
└────────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (0, 4), 10, Orientation.Horizontal));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌────────┐
│        │
│        │
│        │",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (Point.Empty, 10, Orientation.Vertical));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
────────┐
        │
        │
        │
────────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (1, 0), 10, Orientation.Vertical));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌ ───────┐
│        │
│        │
│        │
└ ───────┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (8, 0), 10, Orientation.Vertical));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌─────── ┐
│        │
│        │
│        │
└─────── ┘",
                                  $"{Environment.NewLine}{lc}"
                                 );

        lc = new (origLines.Exclude (new (9, 0), 10, Orientation.Vertical));

        OutputAssert.AssertEqual (
                                  output,
                                  @"
┌────────
│        
│        
│        
└────────",
                                  $"{Environment.NewLine}{lc}"
                                 );
    }

    #region Parallel Tests

    [Fact]
    public void TestExcludeParallel_HorizontalLines_LeftOnly ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=3 to x=103
                                .Exclude (new (3, 2), 100, Orientation.Horizontal)
                                .ToArray ();

        // x=1 to x=2
        StraightLine afterLine = Assert.Single (after);
        Assert.Equal (l1.Start, afterLine.Start);
        Assert.Equal (2, afterLine.Length);
    }

    [Fact]
    public void TestExcludeParallel_HorizontalLines_RightOnly ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=0 to x=2
                                .Exclude (new (0, 2), 3, Orientation.Horizontal)
                                .ToArray ();

        // x=3 to x=10
        StraightLine afterLine = Assert.Single (after);
        Assert.Equal (3, afterLine.Start.X);
        Assert.Equal (2, afterLine.Start.Y);
        Assert.Equal (8, afterLine.Length);
    }

    [Fact]
    public void TestExcludeParallel_HorizontalLines_HorizontalSplit ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=4 to x=5
                                .Exclude (new (4, 2), 2, Orientation.Horizontal)
                                .ToArray ();

        // x=1 to x=3,
        // x=6 to x=10
        Assert.Equal (2, after.Length);
        StraightLine afterLeft = after [0];
        StraightLine afterRight = after [1];

        Assert.Equal (1, afterLeft.Start.X);
        Assert.Equal (2, afterLeft.Start.Y);
        Assert.Equal (3, afterLeft.Length);

        Assert.Equal (6, afterRight.Start.X);
        Assert.Equal (2, afterRight.Start.Y);
        Assert.Equal (5, afterRight.Length);
    }

    [Fact]
    public void TestExcludeParallel_HorizontalLines_CoverCompletely ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=4 to x=5
                                .Exclude (new (1, 2), 10, Orientation.Horizontal)
                                .ToArray ();
        Assert.Empty (after);
    }

    [Fact]
    public void TestExcludeParallel_VerticalLines_TopOnly ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=3 to y=103
                                .Exclude (new (2, 3), 100, Orientation.Vertical)
                                .ToArray ();

        // y=1 to y=2
        StraightLine afterLine = Assert.Single (after);
        Assert.Equal (l1.Start, afterLine.Start);
        Assert.Equal (2, afterLine.Length);
    }

    [Fact]
    public void TestExcludeParallel_HorizontalLines_BottomOnly ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=0 to y=2
                                .Exclude (new (2, 0), 3, Orientation.Vertical)
                                .ToArray ();

        // y=3 to y=10
        StraightLine afterLine = Assert.Single (after);
        Assert.Equal (3, afterLine.Start.Y);
        Assert.Equal (2, afterLine.Start.X);
        Assert.Equal (8, afterLine.Length);
    }

    [Fact]
    public void TestExcludeParallel_VerticalLines_VerticalSplit ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=4 to y=5
                                .Exclude (new (2, 4), 2, Orientation.Vertical)
                                .ToArray ();

        // y=1 to y=3,
        // y=6 to y=10
        Assert.Equal (2, after.Length);
        StraightLine afterLeft = after [0];
        StraightLine afterRight = after [1];

        Assert.Equal (1, afterLeft.Start.Y);
        Assert.Equal (2, afterLeft.Start.X);
        Assert.Equal (3, afterLeft.Length);

        Assert.Equal (6, afterRight.Start.Y);
        Assert.Equal (2, afterRight.Start.X);
        Assert.Equal (5, afterRight.Length);
    }

    [Fact]
    public void TestExcludeParallel_VerticalLines_CoverCompletely ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=4 to y=5
                                .Exclude (new (2, 1), 10, Orientation.Vertical)
                                .ToArray ();
        Assert.Empty (after);
    }

    #endregion

    #region Perpendicular Intersection Tests

    [Fact]
    public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_Splits ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=3 y=0-10
                                .Exclude (new (3, 0), 10, Orientation.Vertical)
                                .ToArray ();

        // x=1 to x=2,
        // x=4 to x=10
        Assert.Equal (2, after.Length);
        StraightLine afterLeft = after [0];
        StraightLine afterRight = after [1];

        Assert.Equal (1, afterLeft.Start.X);
        Assert.Equal (2, afterLeft.Start.Y);
        Assert.Equal (2, afterLeft.Length);

        Assert.Equal (4, afterRight.Start.X);
        Assert.Equal (2, afterRight.Start.Y);
        Assert.Equal (7, afterRight.Length);
    }

    [Fact]
    public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_ClipLeft ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=1 y=0-10
                                .Exclude (new (1, 0), 10, Orientation.Vertical)
                                .ToArray ();

        // x=2 to x=10,
        StraightLine lineAfter = Assert.Single (after);

        Assert.Equal (2, lineAfter.Start.X);
        Assert.Equal (2, lineAfter.Start.Y);
        Assert.Equal (9, lineAfter.Length);
    }

    [Fact]
    public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_ClipRight ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=10 y=0-10
                                .Exclude (new (10, 0), 10, Orientation.Vertical)
                                .ToArray ();

        // x=1 to x=9,
        StraightLine lineAfter = Assert.Single (after);

        Assert.Equal (1, lineAfter.Start.X);
        Assert.Equal (2, lineAfter.Start.Y);
        Assert.Equal (9, lineAfter.Length);
    }

    [Fact]
    public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_MissLeft ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=0 y=0-10
                                .Exclude (Point.Empty, 10, Orientation.Vertical)
                                .ToArray ();

        // Exclusion line is too far to the left so hits nothing
        Assert.Same (Assert.Single (after), l1);
    }

    [Fact]
    public void TestExcludePerpendicular_HorizontalLine_VerticalExclusion_MissRight ()
    {
        // x=1 to x=10
        var l1 = new StraightLine (new (1, 2), 10, Orientation.Horizontal, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude x=11 y=0-10
                                .Exclude (new (11, 0), 10, Orientation.Vertical)
                                .ToArray ();

        // Exclusion line is too far to the right so hits nothing
        Assert.Same (Assert.Single (after), l1);
    }

    [Fact]
    public void TestExcludePerpendicular_VerticalLine_HorizontalExclusion_ClipTop ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=1 x=0-10
                                .Exclude (new (0, 1), 10, Orientation.Horizontal)
                                .ToArray ();

        // y=2 to y=10,
        StraightLine lineAfter = Assert.Single (after);

        Assert.Equal (2, lineAfter.Start.Y);
        Assert.Equal (2, lineAfter.Start.X);
        Assert.Equal (9, lineAfter.Length);
    }

    [Fact]
    public void TestExcludePerpendicular_VerticalLine_HorizontalExclusion_ClipBottom ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=10 x=0-10
                                .Exclude (new (0, 10), 10, Orientation.Horizontal)
                                .ToArray ();

        // y=1 to y=9,
        StraightLine lineAfter = Assert.Single (after);

        Assert.Equal (1, lineAfter.Start.Y);
        Assert.Equal (2, lineAfter.Start.X);
        Assert.Equal (9, lineAfter.Length);
    }

    [Fact]
    public void TestExcludePerpendicular_VerticalLine_HorizontalExclusion_MissTop ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=0 x=0-10
                                .Exclude (Point.Empty, 10, Orientation.Horizontal)
                                .ToArray ();

        // Exclusion line is too far above so hits nothing
        Assert.Same (Assert.Single (after), l1);
    }

    [Fact]
    public void TestExcludePerpendicular_VerticalLine_HorizontalExclusion_MissBottom ()
    {
        // y=1 to y=10
        var l1 = new StraightLine (new (2, 1), 10, Orientation.Vertical, LineStyle.Single);

        StraightLine [] after = new [] { l1 }

                                // exclude y=11 x=0-10
                                .Exclude (new (0, 11), 10, Orientation.Horizontal)
                                .ToArray ();

        // Exclusion line is too far to the right so hits nothing
        Assert.Same (Assert.Single (after), l1);
    }

    #endregion Perpendicular Intersection Tests
}
