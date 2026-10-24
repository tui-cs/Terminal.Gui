using UnitTests;

namespace ViewsTests;

// CoPilot - gpt-6.1-sol
public class TabsDrawingTests : TestDriverBase
{
    [Theory]
    [InlineData (Side.Top, 0)]
    [InlineData (Side.Top, 1)]
    [InlineData (Side.Top, 2)]
    [InlineData (Side.Bottom, 0)]
    [InlineData (Side.Bottom, 1)]
    [InlineData (Side.Bottom, 2)]
    [InlineData (Side.Left, 0)]
    [InlineData (Side.Left, 1)]
    [InlineData (Side.Left, 2)]
    [InlineData (Side.Right, 0)]
    [InlineData (Side.Right, 1)]
    [InlineData (Side.Right, 2)]
    public void PartialRedraw_PreservesTabBorders (Side side, int selectedIndex)
    {
        using IDriver driver = CreateTestDriver (40, 20);
        using View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        Tabs tabs = new () { TabSide = side };
        superView.Add (tabs);

        View [] pages = [new () { Title = "One" }, new () { Title = "Two" }, new () { Title = "Three" }];

        foreach (View page in pages)
        {
            page.Add (new Button { Text = "Button" });
            tabs.Add (page);
        }

        tabs.Value = pages [selectedIndex];
        superView.Layout ();
        superView.Draw ();
        string expected = driver.ToString ();
        var contentDraws = 0;
        var viewportClears = 0;
        var controlVisits = 0;

        foreach (View page in pages)
        {
            page.DrawingContent += (_, _) => contentDraws++;
            page.ClearedViewport += (_, _) => viewportClears++;
            page.SubViews.Single ().DrawComplete += (_, _) => controlVisits++;
        }

        foreach (View page in pages)
        {
            page.SetNeedsDraw ();
            superView.SetClipToScreen ();
            superView.Draw ();
            Assert.Equal (expected, driver.ToString ());

            contentDraws = 0;
            viewportClears = 0;
            controlVisits = 0;
            page.SubViews.Single ().SetNeedsDraw ();
            superView.SetClipToScreen ();
            superView.Draw ();
            Assert.Equal (expected, driver.ToString ());
            Assert.Equal (0, contentDraws);
            Assert.Equal (0, viewportClears);
            Assert.Equal (1, controlVisits);

            ((BorderView)page.Border.View!).TitleView!.SetNeedsDraw ();
            superView.SetClipToScreen ();
            superView.Draw ();
            Assert.Equal (expected, driver.ToString ());
            Assert.Equal (0, contentDraws);
            Assert.Equal (0, viewportClears);
            Assert.Equal (2, controlVisits);
        }
    }

    [Theory]
    [InlineData ("Focus")]
    [InlineData ("Title")]
    [InlineData ("Side")]
    [InlineData ("Depth")]
    [InlineData ("Spacing")]
    [InlineData ("Scroll")]
    [InlineData ("Style")]
    [InlineData ("Scheme")]
    [InlineData ("Hide")]
    [InlineData ("Show")]
    [InlineData ("RemoveBorder")]
    [InlineData ("Move")]
    [InlineData ("Resize")]
    [InlineData ("RemoveTab")]
    [InlineData ("AddTab")]
    public void PartialRedraw_AfterBorderChange_MatchesFullRedraw (string change)
    {
        using IDriver driver = CreateTestDriver (50, 20);
        using View superView = new () { Driver = driver, CanFocus = true, Width = Dim.Fill (), Height = Dim.Fill () };
        Tabs tabs = new () { Width = Dim.Fill (2), Height = Dim.Fill (2) };
        View first = new () { Title = "One" };
        View second = new () { Title = "Two" };
        View third = new () { Title = "Three", Visible = change != "Show" };
        first.Add (new Button { Text = "Button" });
        tabs.Add (first, second, third);
        superView.Add (tabs);
        superView.Layout ();
        superView.Draw ();

        switch (change)
        {
            case "Focus":
                tabs.Value = second;
                break;
            case "Title":
                second.Title = "Longer title";
                break;
            case "Side":
                tabs.TabSide = Side.Left;
                break;
            case "Depth":
                tabs.TabDepth = 4;
                break;
            case "Spacing":
                tabs.TabSpacing = 1;
                break;
            case "Scroll":
                tabs.ScrollOffset = 2;
                break;
            case "Style":
                tabs.TabLineStyle = LineStyle.Double;
                break;
            case "Scheme":
                second.SetScheme (second.GetScheme () with { Normal = new (Color.Red, Color.Black) });
                break;
            case "Hide":
                third.Visible = false;
                break;
            case "Show":
                third.Visible = true;
                break;
            case "RemoveBorder":
                second.BorderStyle = LineStyle.None;
                break;
            case "Move":
                tabs.X = 1;
                tabs.Y = 1;
                break;
            case "Resize":
                tabs.Width = Dim.Fill (5);
                break;
            case "RemoveTab":
                tabs.Remove (second);
                second.Dispose ();
                break;
            case "AddTab":
                tabs.Add (new View { Title = "Four" });
                break;
        }

        superView.Layout ();
        superView.SetClipToScreen ();
        superView.Draw ();
        string partial = driver.ToString ();
        Terminal.Gui.Drawing.Attribute? [] partialAttributes = driver.Contents!.Cast<Cell> ().Select (cell => cell.Attribute).ToArray ();

        superView.SetNeedsDraw ();
        superView.SetClipToScreen ();
        superView.Draw ();
        Assert.Equal (driver.ToString (), partial);
        Assert.Equal (driver.Contents!.Cast<Cell> ().Select (cell => cell.Attribute), partialAttributes);
    }
}
