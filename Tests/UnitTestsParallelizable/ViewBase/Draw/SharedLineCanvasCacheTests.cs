using UnitTests;

namespace ViewBaseTests.Draw;

// CoPilot - gpt-6.1-sol
public class SharedLineCanvasCacheTests : TestDriverBase
{
    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void ContentLines_SurviveRepeatedSubViewOnlyRedraws (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out _);
        left.DrawingContent += (_, _) => left.LineCanvas.AddLine (left.FrameToScreen ().Location with { Y = 3 },
                                                                12, Orientation.Horizontal, LineStyle.Single);
        left.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal ("├", driver.Contents! [3, 0].Grapheme);
        string expected = driver.ToString ();

        for (var i = 0; i < 3; i++)
        {
            left.SubViews.Single ().SetNeedsDraw ();
            root.SetClipToScreen ();
            root.Draw ();
            Assert.Equal (expected, driver.ToString ());
        }
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void DirectDraw_DoesNotDuplicateLinesOnNextSuperViewPass (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.SetNeedsDraw ();
        right.SetClipToScreen ();
        right.Draw ();
        int lineCount = right.LineCanvas.Lines.Count;
        var composedLineCount = 0;
        right.DrawComplete += (_, _) => composedLineCount = right.LineCanvas.Lines.Count;

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();

        Assert.Equal (lineCount, composedLineCount);
        AssertMatchesFullRedraw (root, driver);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void InheritedSchemeChange_RefreshesCleanOffViewportBorder (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        // A partial invalidation does not reach the right view. Attribute inheritance must
        // still be checked before reusing already-resolved shared cells.
        root.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Blue) });
        right.ClearNeedsDraw ();
        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();

        Assert.Equal (right.Border.View!.GetAttributeForRole (VisualRole.Normal), driver.Contents! [4, 22].Attribute);
        AssertMatchesFullRedraw (root, driver);
    }

    [Theory]
    [InlineData (false, "Enabled")]
    [InlineData (true, "Enabled")]
    [InlineData (false, "Focus")]
    [InlineData (true, "Focus")]
    [InlineData (false, "Attribute")]
    [InlineData (true, "Attribute")]
    [InlineData (false, "Thickness")]
    [InlineData (true, "Thickness")]
    [InlineData (true, "ZOrder")]
    [InlineData (false, "ReAdd")]
    [InlineData (true, "ReAdd")]
    public void VisualStateChange_MatchesFullRedraw (bool overlapped, string change)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        switch (change)
        {
            case "Enabled":
                root.Enabled = false;
                break;
            case "Focus":
                root.CanFocus = true;
                right.CanFocus = true;
                right.SetFocus ();
                Assert.True (right.HasFocus);
                break;
            case "Attribute":
                right.Border.View!.GettingAttributeForRole += (_, args) =>
                                                              {
                                                                  args.Result = new Attribute (Color.Red, Color.Blue);
                                                                  args.Handled = true;
                                                              };
                // Custom attribute resolution can change without setting any draw flag.
                break;
            case "Thickness":
                right.Border.Thickness = new Thickness (2);
                root.Layout ();
                break;
            case "ZOrder":
                root.MoveSubViewToStart (right);
                break;
            case "ReAdd":
                root.Remove (right);
                right.BorderStyle = LineStyle.Double;
                root.Add (right);
                root.Layout ();
                break;
        }

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        AssertMatchesFullRedraw (root, driver);
        Assert.False (root.NeedsDraw);
        Assert.False (root.SubViewNeedsDraw);

        if (change == "Attribute")
        {
            Assert.Equal (new Attribute (Color.Red, Color.Blue), driver.Contents! [4, 22].Attribute);
        }
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void TransparentSharedView_WithShadow_PreservesLineOutput (bool shadow)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = new () { Driver = driver, Width = 24, Height = 8 };
        View left = new () { Width = 8, Height = 6, BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true };
        left.Add (new Label { Text = "Left" });
        View right = new ()
        {
            X = 10, Y = 1, Width = 10, Height = 6, BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true, Arrangement = ViewArrangement.Overlapped,
            ViewportSettings = ViewportSettingsFlags.Transparent,
            ShadowStyle = shadow ? ShadowStyles.Transparent : ShadowStyles.None,
            Text = "Right"
        };
        root.Add (left, right);
        root.Layout ();
        View.Draw ([root], true);
        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        View.Draw ([root], false);
        string partial = driver.ToString ();
        Attribute? [] attributes = driver.Contents!.Cast<Cell> ().Select (cell => cell.Attribute).ToArray ();

        root.SetClipToScreen ();
        View.Draw ([root], true);
        Assert.Equal (partial, driver.ToString ());
        // Shadow color blending reads the previous output buffer. Compare the line
        // attributes independently of that existing margin-color behavior.
        for (var y = 1; y < 6; y++)
        {
            Assert.Equal (attributes [y * driver.Cols + 10], driver.Contents! [y, 10].Attribute);
        }

        if (!shadow)
        {
            Assert.Equal (attributes, driver.Contents!.Cast<Cell> ().Select (cell => cell.Attribute));
        }
    }

    [Fact]
    public void TiledGrid_LineJunctionsAndScrolledViewport_MatchFullRedraw ()
    {
        using IDriver driver = CreateTestDriver (46, 19);
        using View root = new () { Driver = driver, Width = 46, Height = 19 };
        Button? dirty = null;
        for (var i = 0; i < 24; i++)
        {
            FrameView frame = new ()
            {
                X = i % 6 * 7, Y = i / 6 * 4, Width = 8, Height = 5,
                BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true
            };
            Button button = new () { Text = "B" };
            frame.Add (button, new Line { Y = 2, Length = 6 });
            root.Add (frame);
            dirty ??= button;
        }

        root.SetContentSize (new Size (60, 30));
        root.Layout ();
        root.Viewport = root.Viewport with { Location = new Point (1, 1) };
        root.Draw ();
        Assert.Contains ("┼", driver.ToString ());

        for (var i = 0; i < 3; i++)
        {
            dirty!.SetNeedsDraw ();
            root.SetClipToScreen ();
            root.Draw ();
            AssertMatchesFullRedraw (root, driver);
            Assert.False (root.SubViewNeedsDraw);
        }
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void ReservedCellsOnly_CleanContributionIsReused (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.DrawingContent += (_, _) => right.LineCanvas.Reserve (new Rectangle (15, 2, 1, 1));
        right.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        var borderDraws = 0;
        right.Border.View!.DrawComplete += (_, _) => borderDraws++;
        for (var i = 0; i < 3; i++)
        {
            left.SubViews.Single ().SetNeedsDraw ();
            root.SetClipToScreen ();
            root.Draw ();
        }

        Assert.Equal (0, borderDraws);
    }

    [Fact]
    public void PopoverOverTiledViews_PartialRedrawMatchesFullRedraw ()
    {
        using IApplication app = Application.Create ().Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 15);
        using Runnable root = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        FrameView left = new () { Width = 15, Height = 8, SuperViewRendersLineCanvas = true };
        FrameView right = new () { X = 14, Width = 15, Height = 8, SuperViewRendersLineCanvas = true };
        Button button = new () { Text = "Button", ShadowStyle = ShadowStyles.None };
        left.Add (button, new Line { Y = 4, Length = 13 });
        root.Add (left, right);
        SessionToken? session = app.Begin (root);
        Assert.NotNull (session);
        using Popover<Label, string> popover = new (new Label { Text = "Popup" })
        {
            App = app, X = 5, Y = 3, Width = 12, Height = 4,
            BorderStyle = LineStyle.Single, ShadowStyle = ShadowStyles.None
        };
        app.Popovers!.Register (popover);
        app.Popovers.Show (popover);
        app.LayoutAndDraw (true);
        Assert.Contains ("Popup", app.Driver.ToString ());

        button.SetNeedsDraw ();
        app.LayoutAndDraw ();
        string partial = app.Driver.ToString ()!;
        Attribute? [] attributes = app.Driver.Contents!.Cast<Cell> ().Select (cell => cell.Attribute).ToArray ();
        app.LayoutAndDraw (true);
        Assert.Equal (partial, app.Driver.ToString ());
        Assert.Equal (attributes, app.Driver.Contents!.Cast<Cell> ().Select (cell => cell.Attribute));
        app.End (session);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void CleanSharedView_DoesNotRebuildBordersOrVisitSubViews (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        string expected = driver.ToString ();
        var borderDraws = 0;
        var controlVisits = 0;
        right.Border.View!.DrawComplete += (_, _) => borderDraws++;
        right.SubViews.Single ().DrawComplete += (_, _) => controlVisits++;

        for (var i = 0; i < 3; i++)
        {
            left.SubViews.Single ().SetNeedsDraw ();
            root.SetClipToScreen ();
            root.Draw ();
            Assert.Equal (expected, driver.ToString ());
        }

        Assert.Equal (0, borderDraws);
        Assert.Equal (0, controlVisits);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void DirectDraw_RemovingBorder_ReplacesCachedLinesWithEmptyContribution (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.BorderStyle = LineStyle.None;
        root.Layout ();
        right.SetClipToScreen ();
        right.Draw ();
        root.ClearNeedsDraw ();

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        AssertMatchesFullRedraw (root, driver);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void CompositionModeChange_RebuildsCachedContribution (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.Arrangement = overlapped ? ViewArrangement.Fixed : ViewArrangement.Overlapped;

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        AssertMatchesFullRedraw (root, driver);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void PendingCustomLines_AreIncludedAndRetainedOnCleanRedraw (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.LineCanvas.AddLine (new Point (14, 3), 4, Orientation.Horizontal, LineStyle.Double);

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal ("═", driver.Contents! [3, 14].Grapheme);
        string expected = driver.ToString ();

        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (expected, driver.ToString ());
    }

    [Fact]
    public void PendingCustomLines_PreserveCachedReservedCells ()
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, true, out View left, out View right);
        right.DrawingContent += (_, _) => right.LineCanvas.Reserve (new Rectangle (22, 3, 1, 1));
        root.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        string gap = driver.Contents! [3, 22].Grapheme;
        right.LineCanvas.AddLine (new Point (14, 3), 4, Orientation.Horizontal, LineStyle.Double);
        left.SubViews.Single ().SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();

        Assert.Equal (gap, driver.Contents! [3, 22].Grapheme);
        Assert.Equal ("═", driver.Contents! [3, 14].Grapheme);
    }

    [Theory]
    [InlineData (false)]
    [InlineData (true)]
    public void DrawCompleteLines_AreCapturedAfterDirectAndCleanDraws (bool overlapped)
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = CreateViews (driver, overlapped, out View left, out View right);
        right.SetNeedsDraw ();
        right.SetClipToScreen ();
        right.Draw ();
        right.DrawComplete += (_, _) => right.LineCanvas.AddLine (new Point (14, 3), 4, Orientation.Horizontal, LineStyle.Double);

        for (var i = 0; i < 3; i++)
        {
            left.SubViews.Single ().SetNeedsDraw ();
            root.SetClipToScreen ();
            root.Draw ();
            Assert.Equal ("═", driver.Contents! [3, 14].Grapheme);
        }

        right.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal ("═", driver.Contents! [3, 14].Grapheme);
    }

    [Fact]
    public void CulledView_RebuildsEarlierSharedContributionWhenSharingResumes ()
    {
        using IDriver driver = CreateTestDriver (24, 8);
        using View root = new () { Driver = driver, Width = 24, Height = 8 };
        View bottom = new ()
        {
            Width = Dim.Fill (), Height = Dim.Fill (), Arrangement = ViewArrangement.Overlapped,
            BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true
        };
        View top = new () { Width = Dim.Fill (), Height = Dim.Fill (), Arrangement = ViewArrangement.Overlapped, Visible = false };
        root.Add (bottom, top);
        root.Layout ();
        root.Draw ();

        var bottomDraws = 0;
        bottom.DrawComplete += (_, _) => bottomDraws++;
        bottom.SuperViewRendersLineCanvas = false;
        bottom.BorderStyle = LineStyle.Double;
        top.Visible = true;
        root.Layout ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (0, bottomDraws);
        Assert.False (bottom.NeedsDraw);

        var borderDraws = 0;
        bottom.Border.View!.DrawComplete += (_, _) => borderDraws++;
        bottom.SuperViewRendersLineCanvas = true;
        top.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();

        Assert.Equal (1, borderDraws);
        AssertMatchesFullRedraw (root, driver);
    }

    private static View CreateViews (IDriver driver, bool overlapped, out View left, out View right)
    {
        View root = new () { Driver = driver, Width = 24, Height = 8 };
        ViewArrangement arrangement = overlapped ? ViewArrangement.Overlapped : ViewArrangement.Fixed;
        left = new () { Width = 12, Height = 6, Title = "Left", BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true, Arrangement = arrangement };
        right = new () { X = 11, Width = 12, Height = 6, Title = "Right", BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true, Arrangement = arrangement };
        left.Add (new Button { Text = "Left" });
        right.Add (new Button { Text = "Right" });
        root.Add (left, right);
        root.Layout ();
        root.Draw ();

        return root;
    }

    private static void AssertMatchesFullRedraw (View root, IDriver driver)
    {
        string partial = driver.ToString ();
        Attribute? [,] attributes = new Attribute? [driver.Rows, driver.Cols];

        for (var y = 0; y < driver.Rows; y++)
        {
            for (var x = 0; x < driver.Cols; x++)
            {
                attributes [y, x] = driver.Contents! [y, x].Attribute;
            }
        }

        root.SetNeedsDraw ();
        root.SetClipToScreen ();
        root.Draw ();
        Assert.Equal (driver.ToString (), partial);

        for (var y = 0; y < driver.Rows; y++)
        {
            for (var x = 0; x < driver.Cols; x++)
            {
                Assert.Equal (attributes [y, x], driver.Contents! [y, x].Attribute);
            }
        }
    }
}
