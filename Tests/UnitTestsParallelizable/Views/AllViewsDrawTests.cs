#nullable enable
using UnitTests;

namespace ViewsTests;

public class AllViewsDrawTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Draw_Does_Not_Layout (Type viewType)
    {
        IDriver driver = CreateTestDriver ();

        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view is null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        view.Driver = driver;
        output.WriteLine ($"Testing {viewType}");

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var drawCompleteCount = 0;
        view.DrawComplete += (s, e) => drawCompleteCount++;

        var layoutStartedCount = 0;
        view.SubViewLayout += (s, e) => layoutStartedCount++;

        var layoutCompleteCount = 0;
        view.SubViewsLaidOut += (s, e) => layoutCompleteCount++;

        view.SetNeedsLayout ();
        view.Layout ();

        Assert.Equal (0, drawCompleteCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);

        if (view.Visible)
        {
            view.SetNeedsDraw ();
            view.Draw ();

            Assert.Equal (1, drawCompleteCount);
            Assert.Equal (1, layoutStartedCount);
            Assert.Equal (1, layoutCompleteCount);
        }
    }
}
