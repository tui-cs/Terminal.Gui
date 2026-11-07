using UnitTests;

namespace ViewBaseTests.Draw;

// CoPilot - gpt-6-astra
public class OverlappedLineCanvasRegionTests : TestDriverBase
{
    [Fact]
    public void RenderLineCanvas_RegionContainsBaseAndVisibleCells_ButNotReservedGaps ()
    {
        using IDriver driver = CreateTestDriver (12, 6);
        using View superView = new () { Driver = driver, Width = 12, Height = 6 };
        View back = new () { Width = 12, Height = 6, Arrangement = ViewArrangement.Overlapped, SuperViewRendersLineCanvas = true };
        View front = new () { Width = 12, Height = 6, Arrangement = ViewArrangement.Overlapped, SuperViewRendersLineCanvas = true };
        superView.Add (back, front);
        superView.Layout ();
        superView.ClearNeedsDraw ();
        back.ClearNeedsDraw ();
        front.ClearNeedsDraw ();

        superView.LineCanvas.AddLine (new (1, 4), 3, Orientation.Horizontal, LineStyle.Single);
        back.LineCanvas.AddLine (new (1, 1), 8, Orientation.Horizontal, LineStyle.Single);
        front.LineCanvas.AddLine (new (1, 1), 3, Orientation.Horizontal, LineStyle.Double);
        front.LineCanvas.Reserve (new (4, 1, 2, 1));
        superView.DrawSubViews ();
        superView.SetClipToScreen ();
        DrawContext context = new ();
        superView.RenderLineCanvas (context);
        Region region = context.GetDrawnRegion ();

        for (var y = 0; y < 6; y++)
        {
            for (var x = 0; x < 12; x++)
            {
                bool expected = (y == 4 && x is >= 1 and <= 3)
                                || (y == 1 && (x is >= 1 and <= 3 or >= 6 and <= 8));
                Assert.Equal (expected, region.Contains (x, y));
            }
        }

        Assert.Equal ("═", driver.Contents! [1, 2].Grapheme);
        Assert.Equal (" ", driver.Contents [1, 4].Grapheme);
        Assert.Equal ("─", driver.Contents [1, 7].Grapheme);
    }
}
