// Copilot
#nullable enable
using System.Text;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="GraphView"/>.
/// </summary>
public class GraphViewTests : TestDriverBase
{
    // ─── Coordinate conversion ──────────────────────────────────────────────────

    [Fact]
    public void ViewportToGraphSpace_DefaultCellSize_OriginIsBottomLeft ()
    {
        GraphView gv = new ();
        gv.BeginInit ();
        gv.EndInit ();
        gv.Viewport = new Rectangle (0, 0, 20, 10);

        // Bottom-left cell (col=0, row=9) should map to graph origin (0,0).
        RectangleF botLeft = gv.ViewportToGraphSpace (0, 9);
        Assert.Equal (0, botLeft.X);
        Assert.Equal (0, botLeft.Y);
        Assert.Equal (1, botLeft.Width);
        Assert.Equal (1, botLeft.Height);

        // One column right and two rows up → graph (1, 2).
        RectangleF up2along1 = gv.ViewportToGraphSpace (1, 7);
        Assert.Equal (1, up2along1.X);
        Assert.Equal (2, up2along1.Y);
    }

    [Fact]
    public void GraphSpaceToViewport_ScrollOffset_ShiftsCoordinate ()
    {
        GraphView gv = new ();
        gv.BeginInit ();
        gv.EndInit ();
        gv.Viewport = new Rectangle (0, 0, 20, 10);

        // Without scroll: graph (0,0) → screen row 9 (bottom).
        Point origin = gv.GraphSpaceToViewport (new PointF (0, 0));
        Assert.Equal (0, origin.X);
        Assert.Equal (9, origin.Y);

        // Scroll the view up by 5 units of graph-Y.
        // Graph point (0, 5) should now appear at the bottom of the screen.
        gv.ScrollOffset = new PointF (0, 5);

        Point shifted = gv.GraphSpaceToViewport (new PointF (0, 5));
        Assert.Equal (0, shifted.X);
        Assert.Equal (9, shifted.Y);
    }

    // ─── CellSize validation ────────────────────────────────────────────────────

    [Fact]
    public void CellSizeZero_Draw_ThrowsException ()
    {
        IDriver driver = CreateTestDriver (20, 10);
        driver.Clip = new Region (driver.Screen);

        GraphView gv = new ()
        {
            Driver = driver,
            Width = 20,
            Height = 10
        };
        gv.BeginInit ();
        gv.EndInit ();
        gv.LayoutSubViews ();

        gv.Series.Add (new ScatterSeries { Points = [new PointF (1, 1)] });
        gv.CellSize = new PointF (0, 5);

        Exception ex = Assert.Throws<Exception> (() => gv.Draw ());
        Assert.Equal ("CellSize cannot be 0", ex.Message);
    }

    // ─── MultiBarSeries ─────────────────────────────────────────────────────────

    [Fact]
    public void MultiBarSeries_BarSpacing_SetsSubSeriesOffsets ()
    {
        // 5 bars per cluster, cluster width of 7, starting at graph offset 1.
        MultiBarSeries series = new (5, 7, 1);

        Assert.Equal (5, series.SubSeries.Count);
        Assert.Equal (0, series.SubSeries.ElementAt (0).Offset);
        Assert.Equal (1, series.SubSeries.ElementAt (1).Offset);
        Assert.Equal (2, series.SubSeries.ElementAt (2).Offset);
        Assert.Equal (3, series.SubSeries.ElementAt (3).Offset);
        Assert.Equal (4, series.SubSeries.ElementAt (4).Offset);
    }

    [Fact]
    public void MultiBarSeries_AddBars_WrongValueCount_ThrowsArgumentException ()
    {
        // Three bars per category.
        MultiBarSeries series = new (3, 7, 1);

        ArgumentException ex = Assert.Throws<ArgumentException> (
            () => series.AddBars ("Cars", (Rune)'#', 1));

        Assert.Equal (
            "Number of values must match the number of bars per category (Parameter 'values')",
            ex.Message);
    }

    [Fact]
    public void MultiBarSeries_WrongColorCount_ThrowsArgumentException ()
    {
        // Supply only 1 color but request 5 bars.
        Attribute [] colors = [new (Color.Green, Color.Black)];

        ArgumentException ex = Assert.Throws<ArgumentException> (
            () => new MultiBarSeries (5, 7, 1, colors));

        Assert.Equal (
            "Number of colors must match the number of bars (Parameter 'numberOfBarsPerCategory')",
            ex.Message);
    }

    // ─── AxisIncrementToRender ──────────────────────────────────────────────────

    [Fact]
    public void AxisIncrementToRender_Constructor_SetsProperties ()
    {
        AxisIncrementToRender render = new (Orientation.Horizontal, 1, 6.6f);

        Assert.Equal (Orientation.Horizontal, render.Orientation);
        Assert.Equal (1, render.ScreenLocation);
        Assert.Equal (6.6f, render.Value);
    }

    // ─── LegendAnnotation ───────────────────────────────────────────────────────

    [Fact]
    public void LegendAnnotation_Constructor_SetsViewportAndBorderDefaults ()
    {
        LegendAnnotation legend = new ();
        Assert.Equal (Rectangle.Empty, legend.Viewport);
        Assert.Equal (Rectangle.Empty, legend.Frame);
        Assert.Equal (LineStyle.Single, legend.BorderStyle);
        Assert.False (legend.BeforeSeries);

        Rectangle bounds = new (1, 2, 10, 3);
        legend = new LegendAnnotation (bounds);

        // With a Single border the interior viewport shrinks by 1 on each side.
        Assert.Equal (new Rectangle (0, 0, 8, 1), legend.Viewport);
        Assert.Equal (bounds, legend.Frame);
        Assert.Equal (LineStyle.Single, legend.BorderStyle);

        legend.BorderStyle = LineStyle.None;
        Assert.Equal (new Rectangle (0, 0, 10, 3), legend.Viewport);
    }
}
