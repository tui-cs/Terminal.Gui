using System.Text;
using UnitTests;
namespace DrawingTests.Lines;

/// <summary>
///     Pure unit tests for <see cref="LineCanvas"/> that don't require Application.Driver or View context.
///     These tests focus on properties and behavior that don't depend on glyph rendering.
///     Note: Tests that verify rendered output (ToString()) cannot be parallelized because LineCanvas
///     depends on Application.Driver for glyph resolution and configuration. Those tests remain in UnitTests.
/// </summary>
public class LineCanvasTests (ITestOutputHelper output) : TestDriverBase
{
    #region Basic API Tests

    [Fact]
    public void Empty_Canvas_ToString_Returns_EmptyString ()
    {
        LineCanvas canvas = new ();
        Assert.Equal (string.Empty, canvas.ToString ());
    }

    [Fact]
    public void Clear_Removes_All_Lines ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single);

        canvas.Clear ();

        Assert.Empty (canvas.Lines);
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
        Assert.Equal (string.Empty, canvas.ToString ());
    }

    [Fact]
    public void Lines_Property_Returns_ReadOnly_Collection ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        Assert.Single (canvas.Lines);
        Assert.IsAssignableFrom<IReadOnlyCollection<StraightLine>> (canvas.Lines);
    }

    [Fact]
    public void AddLine_Adds_Line_To_Collection ()
    {
        LineCanvas canvas = new ();
        Assert.Empty (canvas.Lines);

        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        Assert.Single (canvas.Lines);

        canvas.AddLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (2, canvas.Lines.Count);
    }

    [Fact]
    public void Constructor_With_Lines_Creates_Canvas_With_Lines ()
    {
        StraightLine [] lines = new []
        {
            new StraightLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single),
            new StraightLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single)
        };

        var canvas = new LineCanvas (lines);

        Assert.Equal (2, canvas.Lines.Count);
    }

    #endregion

    #region Bounds Tests - Tests for Bounds property

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 1, 1)]
    [InlineData (0, 0, 1, 0, 0, 1, 1)]
    [InlineData (0, 0, 2, 0, 0, 2, 2)]
    [InlineData (0, 0, 3, 0, 0, 3, 3)]
    [InlineData (0, 0, -1, 0, 0, 1, 1)]
    [InlineData (0, 0, -2, -1, -1, 2, 2)]
    [InlineData (0, 0, -3, -2, -2, 3, 3)]
    public void Viewport_H_And_V_Lines_Both_Positive (
        int x,
        int y,
        int length,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight
    )
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (x, y), length, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (x, y), length, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (expectedX, expectedY, expectedWidth, expectedHeight), canvas.Bounds);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 1, 1)]
    [InlineData (0, 0, 1, 0, 0, 1, 1)]
    [InlineData (0, 0, 2, 0, 0, 2, 1)]
    [InlineData (0, 0, 3, 0, 0, 3, 1)]
    [InlineData (0, 0, -1, 0, 0, 1, 1)]
    [InlineData (0, 0, -2, -1, 0, 2, 1)]
    [InlineData (0, 0, -3, -2, 0, 3, 1)]
    public void Viewport_H_Line (
        int x,
        int y,
        int length,
        int expectedX,
        int expectedY,
        int expectedWidth,
        int expectedHeight
    )
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (x, y), length, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (expectedX, expectedY, expectedWidth, expectedHeight), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Specific_Coordinates ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (5, 5), 3, Orientation.Horizontal, LineStyle.Single);
        Assert.Equal (new (5, 5, 3, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Empty_Canvas_Returns_Empty_Rectangle ()
    {
        LineCanvas canvas = new ();
        Assert.Equal (Rectangle.Empty, canvas.Bounds);
    }

    [Fact]
    public void Bounds_Single_Point_Zero_Length ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (5, 5), 0, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (5, 5, 1, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Horizontal_Line ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (2, 3), 5, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (2, 3, 5, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Vertical_Line ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (2, 3), 5, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (2, 3, 1, 5), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Multiple_Lines_Returns_Union ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 0), 3, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (0, 0, 5, 3), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Negative_Length_Line ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (5, 5), -3, Orientation.Horizontal, LineStyle.Single);

        // Line from (5,5) going left 3 positions: includes points 3, 4, 5 (width 3, X starts at 3)
        Assert.Equal (new (3, 5, 3, 1), canvas.Bounds);
    }

    [Fact]
    public void Bounds_Complex_Box ()
    {
        LineCanvas canvas = new ();

        // top
        canvas.AddLine (new (0, 0), 3, Orientation.Horizontal, LineStyle.Single);

        // left
        canvas.AddLine (new (0, 0), 2, Orientation.Vertical, LineStyle.Single);

        // right
        canvas.AddLine (new (2, 0), 2, Orientation.Vertical, LineStyle.Single);

        // bottom
        canvas.AddLine (new (0, 2), 3, Orientation.Horizontal, LineStyle.Single);

        Assert.Equal (new (0, 0, 3, 3), canvas.Bounds);
    }

    #endregion

    #region Exclusion Tests

    [Fact]
    public void ClearExclusions_Clears_Exclusion_Region ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        var region = new Region (new (0, 0, 2, 1));
        canvas.Exclude (region);
        canvas.ClearExclusions ();

        // After clearing exclusions, GetMap should return all points
        Dictionary<Point, Rune> map = canvas.GetMap ();
        Assert.Equal (5, map.Count);
    }

    [Fact]
    public void Exclude_Removes_Points_From_Map ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);

        var region = new Region (new (0, 0, 2, 1));
        canvas.Exclude (region);

        Dictionary<Point, Rune> map = canvas.GetMap ();

        // Should have 5 - 2 = 3 points (excluding the first 2)
        Assert.Equal (3, map.Count);
    }

    #endregion

    #region Fill Property Tests

    [Fact]
    public void Fill_Property_Can_Be_Set ()
    {
        var foregroundFill = new SolidFill (new (255, 0));
        var backgroundFill = new SolidFill (new (0, 0));
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var canvas = new LineCanvas { Fill = fillPair };

        Assert.Equal (fillPair, canvas.Fill);
    }

    [Fact]
    public void Fill_Property_Defaults_To_Null ()
    {
        LineCanvas canvas = new ();
        Assert.Null (canvas.Fill);
    }

    #endregion

    [Theory]

    // Horizontal lines with a vertical zero-length
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "╞"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "╡"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╟"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╢"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "├"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    "┤"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╠"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    "╣"
                )]

    // Vertical lines with a horizontal zero-length
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "╥"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "╨"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╤"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╧"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "┬"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "┴"
                )]
    [InlineData (
                    0,
                    0,
                    1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╦"
                )]
    [InlineData (
                    0,
                    0,
                    -1,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╩"
                )]

    // Crosses (two zero-length)
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "╫"
                )]
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╪"
                )]
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Single,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Single,
                    "┼"
                )]
    [InlineData (
                    0,
                    0,
                    0,
                    Orientation.Vertical,
                    LineStyle.Double,
                    0,
                    0,
                    0,
                    Orientation.Horizontal,
                    LineStyle.Double,
                    "╬"
                )]
    public void Add_2_Lines (
        int x1,
        int y1,
        int len1,
        Orientation o1,
        LineStyle s1,
        int x2,
        int y2,
        int len2,
        Orientation o2,
        LineStyle s2,
        string expected
    )
    {
        IDriver driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas lc);
        v.Width = 10;
        v.Height = 10;
        v.Viewport = new (0, 0, 10, 10);

        lc.AddLine (new (x1, y1), len1, o1, s1);
        lc.AddLine (new (x2, y2), len2, o2, s2);

        OutputAssert.AssertEqual (output, expected, lc.ToString ());
        v.Dispose ();
    }


    [Fact]
    public void Viewport_Specific ()
    {
        // Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
        // This proves we aren't drawing excess above
        var x = 1;
        var y = 2;
        var width = 3;
        var height = 2;

        var lc = new LineCanvas ();

        // 01230
        // ╔╡╞╗1
        // ║  ║2

        // Add a short horiz line for ╔╡
        lc.AddLine (new (x, y), 2, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 1), lc.Bounds);

        //LHS line down
        lc.AddLine (new (x, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 3, 2), lc.Bounds);

        // remainder of top line
        lc.AddLine (new (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        //RHS line down
        lc.AddLine (new (x + width, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
╔╡╞╗
║  ║",
                                  $"{Environment.NewLine}{lc}"
                                 );
    }

    [Fact]
    public void Viewport_Specific_With_Ustring ()
    {
        // Draw at 1,1 within client area of View (i.e. leave a top and left margin of 1)
        // This proves we aren't drawing excess above
        var x = 1;
        var y = 2;
        var width = 3;
        var height = 2;

        var lc = new LineCanvas ();

        // 01230
        // ╔╡╞╗1
        // ║  ║2

        // Add a short horiz line for ╔╡
        lc.AddLine (new (x, y), 2, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 1), lc.Bounds);

        //LHS line down
        lc.AddLine (new (x, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 2, 2), lc.Bounds);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);
        Assert.Equal (new (x, y, 3, 2), lc.Bounds);

        // remainder of top line
        lc.AddLine (new (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        //RHS line down
        lc.AddLine (new (x + width, y), height, Orientation.Vertical, LineStyle.Double);
        Assert.Equal (new (x, y, 4, 2), lc.Bounds);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
╔╡╞╗
║  ║",
                                  $"{Environment.NewLine}{lc}"
                                 );
    }

    [Fact]
    public void Canvas_Updates_On_Changes ()
    {
        var lc = new LineCanvas ();

        Assert.Equal (Rectangle.Empty, lc.Bounds);

        lc.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Double);
        Assert.NotEqual (Rectangle.Empty, lc.Bounds);

        lc.Clear ();
        Assert.Equal (Rectangle.Empty, lc.Bounds);
    }

    [InlineData (0, 0, Orientation.Horizontal, "─")]
    [InlineData (1, 0, Orientation.Horizontal, "─")]
    [InlineData (0, 1, Orientation.Horizontal, "─")]
    [InlineData (-1, 0, Orientation.Horizontal, "─")]
    [InlineData (0, -1, Orientation.Horizontal, "─")]
    [InlineData (-1, -1, Orientation.Horizontal, "─")]
    [InlineData (0, 0, Orientation.Vertical, "│")]
    [InlineData (1, 0, Orientation.Vertical, "│")]
    [InlineData (0, 1, Orientation.Vertical, "│")]
    [InlineData (0, -1, Orientation.Vertical, "│")]
    [InlineData (-1, 0, Orientation.Vertical, "│")]
    [InlineData (-1, -1, Orientation.Vertical, "│")]
    [Theory]
    public void Length_0_Is_1_Long (int x, int y, Orientation orientation, string expected)
    {
        LineCanvas canvas = new ();

        // Add a line at 5, 5 that's has length of 1
        canvas.AddLine (new (x, y), 1, orientation, LineStyle.Single);
        OutputAssert.AssertEqual (output, $"{expected}", $"{canvas}");
    }

    // X is offset by 2
    [InlineData (0, 0, 1, Orientation.Horizontal, "─")]
    [InlineData (1, 0, 1, Orientation.Horizontal, "─")]
    [InlineData (0, 1, 1, Orientation.Horizontal, "─")]
    [InlineData (0, 0, 1, Orientation.Vertical, "│")]
    [InlineData (1, 0, 1, Orientation.Vertical, "│")]
    [InlineData (0, 1, 1, Orientation.Vertical, "│")]
    [InlineData (-1, 0, 1, Orientation.Horizontal, "─")]
    [InlineData (0, -1, 1, Orientation.Horizontal, "─")]
    [InlineData (-1, 0, 1, Orientation.Vertical, "│")]
    [InlineData (0, -1, 1, Orientation.Vertical, "│")]
    [InlineData (0, 0, -1, Orientation.Horizontal, "─")]
    [InlineData (1, 0, -1, Orientation.Horizontal, "─")]
    [InlineData (0, 1, -1, Orientation.Horizontal, "─")]
    [InlineData (0, 0, -1, Orientation.Vertical, "│")]
    [InlineData (1, 0, -1, Orientation.Vertical, "│")]
    [InlineData (0, 1, -1, Orientation.Vertical, "│")]
    [InlineData (-1, 0, -1, Orientation.Horizontal, "─")]
    [InlineData (0, -1, -1, Orientation.Horizontal, "─")]
    [InlineData (-1, 0, -1, Orientation.Vertical, "│")]
    [InlineData (0, -1, -1, Orientation.Vertical, "│")]
    [InlineData (0, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (1, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (0, 1, 2, Orientation.Horizontal, "──")]
    [InlineData (1, 1, 2, Orientation.Horizontal, "──")]
    [InlineData (0, 0, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 0, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, 1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, 0, 2, Orientation.Horizontal, "──")]
    [InlineData (0, -1, 2, Orientation.Horizontal, "──")]
    [InlineData (-1, 0, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, -1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, -1, 2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, 0, -2, Orientation.Horizontal, "──")]
    [InlineData (1, 0, -2, Orientation.Horizontal, "──")]
    [InlineData (0, 1, -2, Orientation.Horizontal, "──")]
    [InlineData (0, 0, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 0, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, 1, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (1, 1, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, 0, -2, Orientation.Horizontal, "──")]
    [InlineData (0, -1, -2, Orientation.Horizontal, "──")]
    [InlineData (-1, 0, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (0, -1, -2, Orientation.Vertical, "│\r\n│")]
    [InlineData (-1, -1, -2, Orientation.Vertical, "│\r\n│")]
    [Theory]
    public void Length_n_Is_n_Long (int x, int y, int length, Orientation orientation, string expected)
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new (x, y), length, orientation, LineStyle.Single);

        var result = canvas.ToString ();
        OutputAssert.AssertEqual (output, expected, result);
    }

    [Fact]
    public void Length_Negative ()
    {
        var offset = new Point (5, 5);

        LineCanvas canvas = new ();
        canvas.AddLine (offset, -3, Orientation.Horizontal, LineStyle.Single);

        var looksLike = "───";

        Assert.Equal (looksLike, $"{canvas}");
    }

    [InlineData (Orientation.Horizontal, "─")]
    [InlineData (Orientation.Vertical, "│")]
    [Theory]
    public void Length_Zero_Alone_Is_Line (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (Orientation.Horizontal, "┼")]
    [InlineData (Orientation.Vertical, "┼")]
    [Theory]
    public void Length_Zero_Cross_Is_Cross (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add point at opposite orientation
        lc.AddLine (
                    Point.Empty,
                    0,
                    orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal,
                    LineStyle.Single
                   );

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (Orientation.Horizontal, "╥")]
    [InlineData (Orientation.Vertical, "╞")]
    [Theory]
    public void Length_Zero_NextTo_Opposite_Is_T (Orientation orientation, string expected)
    {
        var lc = new LineCanvas ();

        // Add line with length of 1 in opposite orientation starting at same location
        if (orientation == Orientation.Horizontal)
        {
            lc.AddLine (Point.Empty, 1, Orientation.Vertical, LineStyle.Double);
        }
        else
        {
            lc.AddLine (Point.Empty, 1, Orientation.Horizontal, LineStyle.Double);
        }

        // Add a line at 0, 0 that's has length of 0
        lc.AddLine (Point.Empty, 0, orientation, LineStyle.Single);
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [Fact]
    public void TestLineCanvas_LeaveMargin_Top1_Left1 ()
    {
        LineCanvas canvas = new ();

        // Upper box
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Single);

        var looksLike =
            @"
┌─
│ ";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    public void TestLineCanvas_Window_Heavy ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Heavy);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Heavy);

        v.Draw ();

        var looksLike =
            @"    
┏━━━━┳━━━┓
┃    ┃   ┃
┣━━━━╋━━━┫
┃    ┃   ┃
┗━━━━┻━━━┛";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void TestLineCanvas_Window_HeavyTop_ThinSides (LineStyle thinStyle)
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Heavy);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, thinStyle);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Heavy);

        v.Draw ();

        var looksLike =
            @"    
┍━━━━┯━━━┑
│    │   │
┝━━━━┿━━━┥
│    │   │
┕━━━━┷━━━┙
";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void TestLineCanvas_Window_ThinTop_HeavySides (LineStyle thinStyle)
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Heavy);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Heavy);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, thinStyle);

        v.Draw ();

        var looksLike =
            @"    
┎────┰───┒
┃    ┃   ┃
┠────╂───┨
┃    ┃   ┃
┖────┸───┚

";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void Top_Left_From_TopRight_LeftUp ()
    {
        LineCanvas canvas = new ();

        // Upper box
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 1), -2, Orientation.Vertical, LineStyle.Single);

        var looksLike =
            @"
┌─
│ ";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    public void Top_With_1Down ()
    {
        LineCanvas canvas = new ();

        // Top      ─  
        canvas.AddLine (Point.Empty, 1, Orientation.Horizontal, LineStyle.Single);

        // Bottom   ─
        canvas.AddLine (new (1, 1), -1, Orientation.Horizontal, LineStyle.Single);

        //// Right down
        //canvas.AddLine (new Point (9, 0), 3, Orientation.Vertical, LineStyle.Single);

        //// Bottom
        //canvas.AddLine (new Point (9, 3), -10, Orientation.Horizontal, LineStyle.Single);

        //// Left Up
        //canvas.AddLine (new Point (0, 3), -3, Orientation.Vertical, LineStyle.Single);

        Assert.Equal (new (0, 0, 2, 2), canvas.Bounds);

        Dictionary<Point, Rune> map = canvas.GetMap ();
        Assert.Equal (2, map.Count);

        OutputAssert.AssertEqual (
                                  output,
                                  @"
─ 
 ─",
                                  $"{Environment.NewLine}{canvas}"
                                 );
    }

    [Fact]
    public void ToString_Empty ()
    {
        var lc = new LineCanvas ();
        OutputAssert.AssertEqual (output, string.Empty, lc.ToString ());
    }

    //                  012
    [InlineData (0, 0, "═══")]
    [InlineData (1, 0, "═══")]
    [InlineData (0, 1, "═══")]
    [InlineData (1, 1, "═══")]
    [InlineData (2, 2, "═══")]
    [InlineData (-1, 0, "═══")]
    [InlineData (0, -1, "═══")]
    [InlineData (-1, -1, "═══")]
    [InlineData (-2, -2, "═══")]
    [Theory]
    public void ToString_Positive_Horizontal_1Line_Offset (int x, int y, string expected)
    {
        var lc = new LineCanvas ();
        lc.AddLine (new (x, y), 3, Orientation.Horizontal, LineStyle.Double);
        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (0, 0, 0, 0, "═══")]
    [InlineData (1, 0, 1, 0, "═══")]
    [InlineData (-1, 0, -1, 0, "═══")]
    [InlineData (0, 0, 1, 0, "════")]
    [InlineData (1, 0, 3, 0, "═════")]
    [InlineData (1, 0, 4, 0, "══════")]
    [InlineData (1, 0, 5, 0, "═══ ═══")]
    [InlineData (0, 0, 0, 1, "\u2550\u2550\u2550\r\n\u2550\u2550\u2550")]
    [InlineData (0, 0, 1, 1, "═══ \r\n ═══")]
    [InlineData (0, 0, 2, 1, "═══  \r\n  ═══")]
    [InlineData (1, 0, 0, 1, " ═══\r\n═══ ")]
    [InlineData (0, 1, 0, 1, "═══")]
    [InlineData (1, 1, 0, 1, "════")]
    [InlineData (2, 2, 0, 1, "═══  \r\n  ═══")]
    [Theory]
    public void ToString_Positive_Horizontal_2Line_Offset (int x1, int y1, int x2, int y2, string expected)
    {
        var lc = new LineCanvas ();
        lc.AddLine (new (x1, y1), 3, Orientation.Horizontal, LineStyle.Double);
        lc.AddLine (new (x2, y2), 3, Orientation.Horizontal, LineStyle.Double);

        OutputAssert.AssertEqual (output, expected, $"{lc}");
    }

    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Double, "═")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Double, "║")]
    [InlineData (0, 0, 0, Orientation.Horizontal, LineStyle.Single, "─")]
    [InlineData (0, 0, 0, Orientation.Vertical, LineStyle.Single, "│")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Double, "═")]
    [InlineData (0, 0, 1, Orientation.Vertical, LineStyle.Double, "║")]
    [InlineData (0, 0, 1, Orientation.Horizontal, LineStyle.Single, "─")]
    [InlineData (0, 0, 1, Orientation.Vertical, LineStyle.Single, "│")]
    [InlineData (0, 0, 2, Orientation.Horizontal, LineStyle.Double, "══")]
    [InlineData (0, 0, 2, Orientation.Vertical, LineStyle.Double, "║\n║")]
    [InlineData (0, 0, 2, Orientation.Horizontal, LineStyle.Single, "──")]
    [InlineData (0, 0, 2, Orientation.Vertical, LineStyle.Single, "│\n│")]
    [Theory]
    public void View_Draws_1LineTests (
        int x1,
        int y1,
        int length,
        Orientation o1,
        LineStyle s1,
        string expected
    )
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas lc);
        v.Width = 10;
        v.Height = 10;
        v.Viewport = new (0, 0, 10, 10);

        lc.AddLine (new (x1, y1), length, o1, s1);

        v.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, driver);
        v.Dispose ();
    }

    /// <summary>This test demonstrates how to correctly trigger a corner.  By overlapping the lines in the same cell</summary>
    [Fact]
    public void View_Draws_Corner_Correct ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Single);

        v.Draw ();

        var looksLike =
            @"    
┌─
│";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    /// <summary>
    ///     This test demonstrates that corners are only drawn when lines overlap. Not when they terminate adjacent to one
    ///     another.
    /// </summary>
    [Fact]
    public void View_Draws_Corner_NoOverlap ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 1), 2, Orientation.Vertical, LineStyle.Single);

        v.Draw ();

        var looksLike =
            @"    
──
│
│";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    [Theory]
    public void View_Draws_Horizontal (LineStyle style)
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, style);

        v.Draw ();

        var looksLike =
            @"    
──";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void View_Draws_Horizontal_Double ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @" 
══";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    [Theory]
    public void View_Draws_Vertical (LineStyle style)
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, style);

        v.Draw ();

        var looksLike =
            @"    
│
│";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]

    public void View_Draws_Vertical_Double ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);
        canvas.AddLine (Point.Empty, 2, Orientation.Vertical, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
║
║";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void View_Draws_Window_Double ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Double);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
╔════╦═══╗
║    ║   ║
╠════╬═══╣
║    ║   ║
╚════╩═══╝";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void View_Draws_Window_DoubleTop_SingleSides (LineStyle thinStyle)
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Double);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, thinStyle);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, thinStyle);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Double);

        v.Draw ();

        var looksLike =
            @"    
╒════╤═══╕
│    │   │
╞════╪═══╡
│    │   │
╘════╧═══╛
";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    /// <summary>
    ///     Demonstrates when <see cref="LineStyle.Rounded"/> corners are used. Notice how not all lines declare rounded.
    ///     If there are 1+ lines intersecting and a corner is to be used then if any of them are rounded a rounded corner is
    ///     used.
    /// </summary>
    [Fact]
    public void View_Draws_Window_Rounded ()
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Rounded);

        // LineStyle.Single is ignored because corner overlaps with the above line which is Rounded
        // this results in a rounded corner being used.
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Rounded);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        // These lines say rounded but they will result in the T sections which are never rounded.
        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Rounded);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Rounded);

        v.Draw ();

        var looksLike =
            @"    
╭────┬───╮
│    │   │
├────┼───┤
│    │   │
╰────┴───╯";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Rounded)]
    public void View_Draws_Window_SingleTop_DoubleSides (LineStyle thinStyle)
    {
        var driver = CreateTestDriver ();
        View v = GetCanvas (driver, out LineCanvas canvas);

        // outer box
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, thinStyle);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Double);

        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Double);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, thinStyle);

        v.Draw ();

        var looksLike =
            @"    
╓────╥───╖
║    ║   ║
╟────╫───╢
║    ║   ║
╙────╨───╜

";
        DriverAssert.AssertDriverContentsAre (looksLike, output, driver);
        v.Dispose ();
    }

    [Fact]
    public void Window ()
    {
        LineCanvas canvas = new ();

        // Frame
        canvas.AddLine (Point.Empty, 10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (9, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new (9, 4), -10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new (0, 4), -5, Orientation.Vertical, LineStyle.Single);

        // Cross
        canvas.AddLine (new (5, 0), 5, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new (0, 2), 10, Orientation.Horizontal, LineStyle.Single);

        var looksLike =
            @"
┌────┬───┐
│    │   │
├────┼───┤
│    │   │
└────┴───┘";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{canvas}");
    }

    [Fact]
    public void Zero_Length_Intersections ()
    {
        // Draw at 1,2 within client area of View (i.e. leave a top and left margin of 1)
        // This proves we aren't drawing excess above
        var x = 1;
        var y = 2;
        var width = 5;
        var height = 2;

        var lc = new LineCanvas ();

        // ╔╡╞═════╗
        // Add a short horiz line for ╔╡
        lc.AddLine (new (x, y), 2, Orientation.Horizontal, LineStyle.Double);

        //LHS line down
        lc.AddLine (new (x, y), height, Orientation.Vertical, LineStyle.Double);

        //Vertical line before Title, results in a ╡
        lc.AddLine (new (x + 1, y), 0, Orientation.Vertical, LineStyle.Single);

        //Vertical line after Title, results in a ╞
        lc.AddLine (new (x + 2, y), 0, Orientation.Vertical, LineStyle.Single);

        // remainder of top line
        lc.AddLine (new (x + 2, y), width - 1, Orientation.Horizontal, LineStyle.Double);

        //RHS line down
        lc.AddLine (new (x + width, y), height, Orientation.Vertical, LineStyle.Double);

        var looksLike = @"
╔╡╞══╗
║    ║";
        OutputAssert.AssertEqual (output, looksLike, $"{Environment.NewLine}{lc}");
    }

    [Fact]
    public void LineCanvas_UsesFillCorrectly ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0); // Red
        var backgroundColor = new Color (0, 0); // Black
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var lineCanvas = new LineCanvas
        {
            Fill = fillPair
        };

        // Act
        lineCanvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        // Assert
        foreach (Cell? cell in cellMap.Values)
        {
            Assert.NotNull (cell);
            Assert.Equal (foregroundColor, cell.Value.Attribute!.Value.Foreground);
            Assert.Equal (backgroundColor, cell.Value.Attribute.Value.Background);
        }
    }

    [Fact]
    public void LineCanvas_LineColorIgnoredBecauseOfFill ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0); // Red
        var backgroundColor = new Color (0, 0); // Black
        var lineColor = new Attribute (new Color (0, 255), new Color (255, 255, 255)); // Green on White
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var lineCanvas = new LineCanvas
        {
            Fill = fillPair
        };

        // Act
        lineCanvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single, lineColor);
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        // Assert
        foreach (Cell? cell in cellMap.Values)
        {
            Assert.NotNull (cell);
            Assert.Equal (foregroundColor, cell.Value.Attribute!.Value.Foreground);
            Assert.Equal (backgroundColor, cell.Value.Attribute.Value.Background);
        }
    }

    [Fact]
    public void LineCanvas_IntersectingLinesUseFillCorrectly ()
    {
        // Arrange
        var foregroundColor = new Color (255, 0); // Red
        var backgroundColor = new Color (0, 0); // Black
        var foregroundFill = new SolidFill (foregroundColor);
        var backgroundFill = new SolidFill (backgroundColor);
        var fillPair = new FillPair (foregroundFill, backgroundFill);

        var lineCanvas = new LineCanvas
        {
            Fill = fillPair
        };

        // Act
        lineCanvas.AddLine (new (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        lineCanvas.AddLine (new (2, -2), 5, Orientation.Vertical, LineStyle.Single);
        Dictionary<Point, Cell?> cellMap = lineCanvas.GetCellMap ();

        // Assert
        foreach (Cell? cell in cellMap.Values)
        {
            Assert.NotNull (cell);
            Assert.Equal (foregroundColor, cell.Value.Attribute!.Value.Foreground);
            Assert.Equal (backgroundColor, cell.Value.Attribute.Value.Background);
        }
    }

    // TODO: Remove this and make all LineCanvas tests independent of View
    /// <summary>
    ///     Creates a new <see cref="View"/> into which a <see cref="LineCanvas"/> is rendered at
    ///     <see cref="View.DrawComplete"/> time.
    /// </summary>
    /// <param name="canvas">The <see cref="LineCanvas"/> you can draw into.</param>
    /// <param name="offsetX">How far to offset drawing in X</param>
    /// <param name="offsetY">How far to offset drawing in Y</param>
    /// <returns></returns>
    private View GetCanvas (IDriver driver, out LineCanvas canvas, int offsetX = 0, int offsetY = 0)
    {
        var v = new View { Width = 10, Height = 5, Viewport = new (0, 0, 10, 5) };
        v.Driver = driver;

        LineCanvas canvasCopy = canvas = new ();

        v.DrawComplete += (s, e) =>
                          {
                              v.FillRect (v.Viewport);

                              foreach (KeyValuePair<Point, Rune> p in canvasCopy.GetMap ())
                              {
                                  v.AddRune (
                                             offsetX + p.Key.X,
                                             offsetY + p.Key.Y,
                                             p.Value
                                            );
                              }

                              canvasCopy.Clear ();
                          };

        return v;
    }

    #region GetRegion Tests

    [Fact]
    public void GetRegion_EmptyCellMap_ReturnsEmptyRegion ()
    {
        Dictionary<Point, Cell?> cellMap = new ();
        Region region = LineCanvas.GetRegion (cellMap);

        Assert.NotNull (region);
        Assert.True (region.IsEmpty ());
    }

    [Fact]
    public void GetRegion_SingleCell_ReturnsSingleRectangle ()
    {
        Dictionary<Point, Cell?> cellMap = new ()
        {
            { new Point (5, 10), new Cell { Grapheme = "X" } }
        };

        Region region = LineCanvas.GetRegion (cellMap);

        Assert.NotNull (region);
        Assert.False (region.IsEmpty ());
        Assert.True (region.Contains (5, 10));
    }

    [Fact]
    public void GetRegion_HorizontalLine_CreatesHorizontalSpan ()
    {
        Dictionary<Point, Cell?> cellMap = new ();
        // Horizontal line from (5, 10) to (9, 10)
        for (int x = 5; x <= 9; x++)
        {
            cellMap.Add (new Point (x, 10), new Cell { Grapheme = "─" });
        }

        Region region = LineCanvas.GetRegion (cellMap);

        Assert.NotNull (region);
        // All cells in the horizontal span should be in the region
        for (int x = 5; x <= 9; x++)
        {
            Assert.True (region.Contains (x, 10), $"Expected ({x}, 10) to be in region");
        }
        // Cells outside the span should not be in the region
        Assert.False (region.Contains (4, 10));
        Assert.False (region.Contains (10, 10));
        Assert.False (region.Contains (7, 9));
        Assert.False (region.Contains (7, 11));
    }

    [Fact]
    public void GetRegion_VerticalLine_CreatesMultipleHorizontalSpans ()
    {
        Dictionary<Point, Cell?> cellMap = new ();
        // Vertical line from (5, 10) to (5, 14)
        for (int y = 10; y <= 14; y++)
        {
            cellMap.Add (new Point (5, y), new Cell { Grapheme = "│" });
        }

        Region region = LineCanvas.GetRegion (cellMap);

        Assert.NotNull (region);
        // All cells in the vertical line should be in the region
        for (int y = 10; y <= 14; y++)
        {
            Assert.True (region.Contains (5, y), $"Expected (5, {y}) to be in region");
        }
        // Cells outside should not be in the region
        Assert.False (region.Contains (4, 12));
        Assert.False (region.Contains (6, 12));
    }

    [Fact]
    public void GetRegion_LShape_CreatesCorrectSpans ()
    {
        Dictionary<Point, Cell?> cellMap = new ();
        // L-shape: horizontal line from (0, 0) to (5, 0), then vertical to (5, 3)
        for (int x = 0; x <= 5; x++)
        {
            cellMap.Add (new Point (x, 0), new Cell { Grapheme = "─" });
        }
        for (int y = 1; y <= 3; y++)
        {
            cellMap.Add (new Point (5, y), new Cell { Grapheme = "│" });
        }

        Region region = LineCanvas.GetRegion (cellMap);

        // Horizontal part
        for (int x = 0; x <= 5; x++)
        {
            Assert.True (region.Contains (x, 0), $"Expected ({x}, 0) to be in region");
        }
        // Vertical part
        for (int y = 1; y <= 3; y++)
        {
            Assert.True (region.Contains (5, y), $"Expected (5, {y}) to be in region");
        }
        // Empty cells should not be in region
        Assert.False (region.Contains (1, 1));
        Assert.False (region.Contains (4, 2));
    }

    [Fact]
    public void GetRegion_DiscontiguousHorizontalCells_CreatesSeparateSpans ()
    {
        Dictionary<Point, Cell?> cellMap = new ()
        {
            { new Point (0, 5), new Cell { Grapheme = "X" } },
            { new Point (1, 5), new Cell { Grapheme = "X" } },
            // Gap at (2, 5)
            { new Point (3, 5), new Cell { Grapheme = "X" } },
            { new Point (4, 5), new Cell { Grapheme = "X" } }
        };

        Region region = LineCanvas.GetRegion (cellMap);

        Assert.True (region.Contains (0, 5));
        Assert.True (region.Contains (1, 5));
        Assert.False (region.Contains (2, 5)); // Gap
        Assert.True (region.Contains (3, 5));
        Assert.True (region.Contains (4, 5));
    }

    [Fact]
    public void GetRegion_IntersectingLines_CreatesCorrectRegion ()
    {
        Dictionary<Point, Cell?> cellMap = new ();
        // Horizontal line
        for (int x = 0; x <= 4; x++)
        {
            cellMap.Add (new Point (x, 2), new Cell { Grapheme = "─" });
        }
        // Vertical line intersecting at (2, 2)
        for (int y = 0; y <= 4; y++)
        {
            cellMap [new Point (2, y)] = new Cell { Grapheme = "┼" };
        }

        Region region = LineCanvas.GetRegion (cellMap);

        // Horizontal line
        for (int x = 0; x <= 4; x++)
        {
            Assert.True (region.Contains (x, 2), $"Expected ({x}, 2) to be in region");
        }
        // Vertical line
        for (int y = 0; y <= 4; y++)
        {
            Assert.True (region.Contains (2, y), $"Expected (2, {y}) to be in region");
        }
    }

    #endregion

    #region GetCellMapWithRegion Tests

    [Fact]
    public void GetCellMapWithRegion_EmptyCanvas_ReturnsEmptyMapAndRegion ()
    {
        LineCanvas canvas = new ();

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.Empty (cellMap);
        Assert.NotNull (region);
        Assert.True (region.IsEmpty ());
    }

    [Fact]
    public void GetCellMapWithRegion_SingleHorizontalLine_ReturnsCellMapAndRegion ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new Point (5, 10), 5, Orientation.Horizontal, LineStyle.Single);

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.NotEmpty (cellMap);
        Assert.NotNull (region);
        Assert.False (region.IsEmpty ());

        // Both cellMap and region should contain the same cells
        foreach (Point p in cellMap.Keys)
        {
            Assert.True (region.Contains (p.X, p.Y), $"Expected ({p.X}, {p.Y}) to be in region");
        }
    }

    [Fact]
    public void GetCellMapWithRegion_SingleVerticalLine_ReturnsCellMapAndRegion ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new Point (5, 10), 5, Orientation.Vertical, LineStyle.Single);

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.NotEmpty (cellMap);
        Assert.NotNull (region);
        Assert.False (region.IsEmpty ());

        // Both cellMap and region should contain the same cells
        foreach (Point p in cellMap.Keys)
        {
            Assert.True (region.Contains (p.X, p.Y), $"Expected ({p.X}, {p.Y}) to be in region");
        }
    }

    [Fact]
    public void GetCellMapWithRegion_IntersectingLines_CorrectlyHandlesIntersection ()
    {
        LineCanvas canvas = new ();
        // Create a cross pattern
        canvas.AddLine (new Point (0, 2), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (2, 0), 5, Orientation.Vertical, LineStyle.Single);

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.NotEmpty (cellMap);
        Assert.NotNull (region);

        // Verify intersection point is in both
        Assert.True (cellMap.ContainsKey (new Point (2, 2)), "Intersection should be in cellMap");
        Assert.True (region.Contains (2, 2), "Intersection should be in region");

        // All cells should be in both structures
        foreach (Point p in cellMap.Keys)
        {
            Assert.True (region.Contains (p.X, p.Y), $"Expected ({p.X}, {p.Y}) to be in region");
        }
    }

    [Fact]
    public void GetCellMapWithRegion_ComplexShape_RegionMatchesCellMap ()
    {
        LineCanvas canvas = new ();
        // Create a box
        canvas.AddLine (new Point (0, 0), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 3), 5, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, 0), 4, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new Point (4, 0), 4, Orientation.Vertical, LineStyle.Single);

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.NotEmpty (cellMap);
        Assert.NotNull (region);

        // Every cell in the map should be in the region
        foreach (Point p in cellMap.Keys)
        {
            Assert.True (region.Contains (p.X, p.Y), $"Expected ({p.X}, {p.Y}) to be in region");
        }

        // Cells not in the map should not be in the region (interior of box)
        Assert.False (cellMap.ContainsKey (new Point (2, 1)));
        // Note: Region might contain interior if it's filled, so we just verify consistency
    }

    [Fact]
    public void GetCellMapWithRegion_ResultsMatchSeparateCalls ()
    {
        LineCanvas canvas = new ();
        // Create a complex pattern
        canvas.AddLine (new Point (0, 0), 10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (5, 0), 10, Orientation.Vertical, LineStyle.Single);
        canvas.AddLine (new Point (0, 5), 10, Orientation.Horizontal, LineStyle.Double);

        // Get results from combined method
        (Dictionary<Point, Cell?> combinedCellMap, Region combinedRegion) = canvas.GetCellMapWithRegion ();

        // Get results from separate calls
        Dictionary<Point, Cell?> separateCellMap = canvas.GetCellMap ();
        Region separateRegion = LineCanvas.GetRegion (separateCellMap);

        // Cell maps should be identical
        Assert.Equal (separateCellMap.Count, combinedCellMap.Count);
        foreach (KeyValuePair<Point, Cell?> kvp in separateCellMap)
        {
            Assert.True (combinedCellMap.ContainsKey (kvp.Key), $"Combined map missing key {kvp.Key}");
        }

        // Regions should contain the same points
        foreach (Point p in combinedCellMap.Keys)
        {
            Assert.True (combinedRegion.Contains (p.X, p.Y), $"Combined region missing ({p.X}, {p.Y})");
            Assert.True (separateRegion.Contains (p.X, p.Y), $"Separate region missing ({p.X}, {p.Y})");
        }
    }

    [Fact]
    public void GetCellMapWithRegion_NegativeCoordinates_HandlesCorrectly ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new Point (-5, -5), 10, Orientation.Horizontal, LineStyle.Single);
        canvas.AddLine (new Point (0, -5), 10, Orientation.Vertical, LineStyle.Single);

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.NotEmpty (cellMap);
        Assert.NotNull (region);

        // Verify negative coordinates are handled
        Assert.True (cellMap.Keys.Any (p => p.X < 0 || p.Y < 0), "Should have negative coordinates");

        // All cells should be in region
        foreach (Point p in cellMap.Keys)
        {
            Assert.True (region.Contains (p.X, p.Y), $"Expected ({p.X}, {p.Y}) to be in region");
        }
    }

    [Fact]
    public void GetCellMapWithRegion_WithExclusion_RegionExcludesExcludedCells ()
    {
        LineCanvas canvas = new ();
        canvas.AddLine (new Point (0, 0), 10, Orientation.Horizontal, LineStyle.Single);

        // Exclude middle section
        Region exclusionRegion = new ();
        exclusionRegion.Combine (new Rectangle (3, 0, 4, 1), RegionOp.Union);
        canvas.Exclude (exclusionRegion);

        (Dictionary<Point, Cell?> cellMap, Region region) = canvas.GetCellMapWithRegion ();

        Assert.NotNull (cellMap);
        Assert.NotEmpty (cellMap);

        // Excluded cells should not be in cellMap
        for (int x = 3; x < 7; x++)
        {
            Assert.False (cellMap.ContainsKey (new Point (x, 0)), $"({x}, 0) should be excluded from cellMap");
        }

        // Region should match cellMap
        foreach (Point p in cellMap.Keys)
        {
            Assert.True (region.Contains (p.X, p.Y), $"Expected ({p.X}, {p.Y}) to be in region");
        }

        // Excluded points should not be in region
        for (int x = 3; x < 7; x++)
        {
            Assert.False (region.Contains (x, 0), $"({x}, 0) should be excluded from region");
        }
    }

    #endregion
}
