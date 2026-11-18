using System.Text;
using UnitTests;

namespace ViewsTests;

// Copilot

/// <summary>
///     Diagnostic tests for issue #5357 — tabbed redraw/layout fan-out.
/// </summary>
/// <remarks>
///     <para>
///         These tests are intentionally instrumentation-only: they observe layout and draw activity
///         on each tab via <see cref="View.SubViewsLaidOut"/>, <see cref="View.DrawComplete"/>, and
///         <see cref="View.ClearedViewport"/> events, but do not change rendering or invalidation
///         semantics. They lock in the current behavior so that future refactors of the layout/draw
///         pipeline (see issue #4973) can be measured and verified.
///     </para>
///     <para>
///         The diagnostics rely on counters/event traces — not on <c>Driver.Contents</c> alone —
///         so they remain meaningful in the presence of clipping, transparent viewports,
///         shadow margins, and adornment subviews.
///     </para>
///     <para>
///         <see cref="Code"/> is used as the scrollable tab content because the older
///         <c>TextView</c> is being deprecated. The fan-out behavior lives in the <see cref="View"/>
///         layout/draw pipeline and is independent of which content widget is used, so any view
///         that scrolls inside an Overlapped-arrangement <see cref="Tabs"/> exercises the same
///         code paths.
///     </para>
///     <para>
///         Each test reports its measured counts through <see cref="ITestOutputHelper"/> so the data
///         is visible in CI logs even when the test passes. When the fan-out problem is fixed, these
///         tests are expected to be updated to assert the new (lower) counts; the diagnostic helper
///         and the test scaffolding remain useful as a regression check.
///     </para>
/// </remarks>
public class TabsFanOutDiagnosticTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Records per-view layout and draw activity for a set of tracked views. Used as the primary
    ///     diagnostic for tab fan-out — the counts here are deterministic and survive future changes
    ///     to clipping, shadow, transparency, and adornment-subview behavior.
    /// </summary>
    private sealed class ViewActivityCounters
    {
        private readonly Dictionary<View, Counts> _counts = new ();
        private readonly List<(View View, string Label)> _order = [];

        public void Track (View view, string label)
        {
            Counts counts = new ();
            _counts [view] = counts;
            _order.Add ((view, label));

            view.SubViewsLaidOut += (_, _) => counts.SubViewsLaidOut++;
            view.DrawComplete += (_, _) => counts.DrawComplete++;
            view.ClearedViewport += (_, _) => counts.ClearedViewport++;
            view.DrawingContent += (_, _) => counts.DrawingContent++;
            view.DrawingText += (_, _) => counts.DrawingText++;
            view.FrameChanged += (_, _) => counts.FrameChanged++;
        }

        public Counts Get (View view) => _counts [view];

        public void Reset ()
        {
            foreach (Counts counts in _counts.Values)
            {
                counts.Reset ();
            }
        }

        public string Report (string title)
        {
            StringBuilder sb = new ();
            sb.AppendLine (title);
            sb.AppendLine ("  view                       laidOut  drawComplete  clearedViewport  drawingContent  drawingText  frameChanged");

            foreach ((View view, string label) in _order)
            {
                Counts c = _counts [view];

                sb.AppendLine (
                               $"  {label,-26} {c.SubViewsLaidOut,7}  {c.DrawComplete,12}  {c.ClearedViewport,15}  {c.DrawingContent,14}  {c.DrawingText,11}  {c.FrameChanged,12}");
            }

            return sb.ToString ();
        }

        public sealed class Counts
        {
            public int SubViewsLaidOut;
            public int DrawComplete;
            public int ClearedViewport;
            public int DrawingContent;
            public int DrawingText;
            public int FrameChanged;

            public void Reset ()
            {
                SubViewsLaidOut = 0;
                DrawComplete = 0;
                ClearedViewport = 0;
                DrawingContent = 0;
                DrawingText = 0;
                FrameChanged = 0;
            }
        }
    }

    private const int TabCount = 4;
    private const int DriverWidth = 60;
    private const int DriverHeight = 20;

    private static string MakeText (string prefix, int lines)
    {
        StringBuilder sb = new ();

        for (var i = 1; i <= lines; i++)
        {
            sb.Append (prefix);
            sb.Append (' ');
            sb.Append (i);

            if (i < lines)
            {
                sb.Append ('\n');
            }
        }

        return sb.ToString ();
    }

    private static Code MakeCode (string title, string text) =>
        new ()
        {
            Title = title,
            Text = text,
            Language = null,
            SyntaxHighlighter = null,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

    private static (View Root, Tabs Tabs, Code [] Codes, ViewActivityCounters Counters) BuildTabbedScenario (IDriver driver)
    {
        View root = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        root.Add (tabs);

        Code [] codes = new Code [TabCount];

        for (var i = 0; i < TabCount; i++)
        {
            codes [i] = MakeCode ($"Tab{i + 1}", MakeText ($"Tab{i + 1} line", 50));
            tabs.Add (codes [i]);
        }

        ViewActivityCounters counters = new ();
        counters.Track (tabs, "Tabs");

        for (var i = 0; i < TabCount; i++)
        {
            counters.Track (codes [i], $"Code{i + 1}");
        }

        return (root, tabs, codes, counters);
    }

    private static (View Root, Code Code, ViewActivityCounters Counters) BuildSingleScenario (IDriver driver)
    {
        View root = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Code code = MakeCode ("Only", MakeText ("Only line", 50));
        root.Add (code);

        ViewActivityCounters counters = new ();
        counters.Track (code, "Code");

        return (root, code, counters);
    }

    /// <summary>
    ///     Issue #5357 acceptance criterion: layout work stays on the active tab when the active tab scrolls.
    /// </summary>
    [Fact]
    public void Diagnostic_ActiveTabScroll_LayoutEvents_StayOnActiveTab ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, Code [] codes, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        Code active = (Code)tabs.Value!;
        Assert.Same (codes [0], active);

        counters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
        }

        output.WriteLine (counters.Report ("After 5 active-tab scrolls (layout-only):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.SubViewsLaidOut > 0, $"Active tab should receive layout activity, got {activeCounts.SubViewsLaidOut}");

        int inactiveLayouts = 0;

        for (var i = 1; i < TabCount; i++)
        {
            inactiveLayouts += counters.Get (codes [i]).SubViewsLaidOut;
        }

        output.WriteLine ($"Active SubViewsLaidOut: {activeCounts.SubViewsLaidOut}");
        output.WriteLine ($"Sum of inactive SubViewsLaidOut: {inactiveLayouts}");

        Assert.Equal (
                      0,
                      inactiveLayouts);

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Issue #5356 / #4973 acceptance criterion: diagnostics can show whether inactive tabs
    ///     received draw work when the active tab scrolls.
    /// </summary>
    [Fact]
    public void Diagnostic_ActiveTabScroll_DrawEvents_OnEachTab ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, Code [] codes, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        Code active = (Code)tabs.Value!;

        counters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
            root.Draw ();
        }

        output.WriteLine (counters.Report ("After 5 active-tab scrolls (layout + draw):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, $"Active tab must receive draw activity, got {activeCounts.DrawComplete}");

        int inactiveDraws = 0;
        int inactiveClears = 0;
        int inactiveContentDraws = 0;
        int inactiveTextDraws = 0;

        for (var i = 1; i < TabCount; i++)
        {
            ViewActivityCounters.Counts c = counters.Get (codes [i]);
            inactiveDraws += c.DrawComplete;
            inactiveClears += c.ClearedViewport;
            inactiveContentDraws += c.DrawingContent;
            inactiveTextDraws += c.DrawingText;
        }

        output.WriteLine ($"Active DrawComplete: {activeCounts.DrawComplete}");
        output.WriteLine ($"Sum of inactive DrawComplete:    {inactiveDraws}");
        output.WriteLine ($"Sum of inactive ClearedViewport: {inactiveClears}");
        output.WriteLine ($"Sum of inactive DrawingContent:  {inactiveContentDraws}");
        output.WriteLine ($"Sum of inactive DrawingText:     {inactiveTextDraws}");

        // Code owns text rendering, so the generic DrawingText event must not fire
        // for either the active or inactive tabs.
        Assert.Equal (0, activeCounts.DrawingText);
        Assert.Equal (0, inactiveTextDraws);

        // Issue #5358 fix: the Tabs container itself no longer clears its viewport
        // when only a child tab is dirty. Before the fix this was > 0; after, == 0.
        ViewActivityCounters.Counts tabsCounts = counters.Get (tabs);
        Assert.Equal (0, tabsCounts.ClearedViewport);

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Issue #5356 acceptance criterion: a comparable fan-out metric between an equivalent
    ///     single-Code scenario and a tabbed-Code scenario.
    /// </summary>
    [Fact]
    public void Diagnostic_TabbedFanOut_ComparedTo_SingleViewBaseline ()
    {
        IDriver driverSingle = CreateTestDriver (DriverWidth, DriverHeight);
        (View singleRoot, Code single, ViewActivityCounters singleCounters) = BuildSingleScenario (driverSingle);

        singleRoot.Layout ();
        singleRoot.Draw ();
        singleCounters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            single.Viewport = single.Viewport with { Y = y };
            singleRoot.Layout ();
            singleRoot.Draw ();
        }

        ViewActivityCounters.Counts singleCounts = singleCounters.Get (single);
        output.WriteLine (singleCounters.Report ("Single-Code baseline (5 scrolls):"));

        IDriver driverTabbed = CreateTestDriver (DriverWidth, DriverHeight);
        (View tabRoot, Tabs tabs, Code [] codes, ViewActivityCounters tabCounters) = BuildTabbedScenario (driverTabbed);

        tabRoot.Layout ();
        tabRoot.Draw ();

        Code active = (Code)tabs.Value!;
        tabCounters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            tabRoot.Layout ();
            tabRoot.Draw ();
        }

        ViewActivityCounters.Counts tabActiveCounts = tabCounters.Get (active);
        output.WriteLine (tabCounters.Report ("Tabbed scenario (5 scrolls of active tab):"));

        int totalTabLayouts = tabActiveCounts.SubViewsLaidOut;

        for (var i = 1; i < TabCount; i++)
        {
            totalTabLayouts += tabCounters.Get (codes [i]).SubViewsLaidOut;
        }

        double layoutFanOut = singleCounts.SubViewsLaidOut == 0 ? double.NaN : (double)totalTabLayouts / singleCounts.SubViewsLaidOut;

        output.WriteLine ($"Tabbed total layouts / single layouts       = {totalTabLayouts} / {singleCounts.SubViewsLaidOut} = {layoutFanOut:F2}");

        Assert.Equal (0, singleCounts.DrawingText);
        Assert.Equal (0, tabActiveCounts.DrawingText);

        Assert.Equal (1.0, layoutFanOut);

        singleRoot.Dispose ();
        tabRoot.Dispose ();
        driverSingle.Dispose ();
        driverTabbed.Dispose ();
    }

    /// <summary>
    ///     Edge case: transparent inactive tabs must not silently bypass the diagnostic.
    ///     <see cref="ViewportSettingsFlags.Transparent"/> changes the clear/draw sequence in the inner
    ///     view's draw path but it does not eliminate <see cref="View.DrawComplete"/> firing on the
    ///     view itself. The diagnostic must still observe activity on transparent tabs.
    /// </summary>
    [Fact]
    public void Diagnostic_TransparentInactiveTab_StillObservable ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, Code [] codes, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        codes [1].ViewportSettings |= ViewportSettingsFlags.Transparent;
        codes [2].ViewportSettings |= ViewportSettingsFlags.Transparent;

        root.Layout ();
        root.Draw ();

        Code active = (Code)tabs.Value!;
        counters.Reset ();

        for (var y = 1; y <= 3; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
            root.Draw ();
        }

        output.WriteLine (counters.Report ("Transparent inactive tabs (3 active-tab scrolls):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, "Active tab still draws when peers are transparent.");

        for (var i = 1; i < TabCount; i++)
        {
            ViewActivityCounters.Counts c = counters.Get (codes [i]);

            // Code suppresses the generic text pass regardless of transparency.
            Assert.Equal (0, c.DrawingText);
        }

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Edge case: shadow margins draw in a separate pass (<see cref="MarginView.DrawMargins"/>).
    ///     The diagnostic must observe the active tab's draw activity even when its margin has a shadow.
    /// </summary>
    [Fact]
    public void Diagnostic_ShadowMargin_DoesNotMaskActiveTabActivity ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, Code [] codes, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        codes [0].ShadowStyle = ShadowStyles.Opaque;

        root.Layout ();
        root.Draw ();

        Code active = (Code)tabs.Value!;
        Assert.Same (codes [0], active);

        counters.Reset ();

        for (var y = 1; y <= 3; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
            root.Draw ();
        }

        output.WriteLine (counters.Report ("Active tab with shadow margin (3 scrolls):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, $"Active tab with shadow must still record DrawComplete (got {activeCounts.DrawComplete}).");

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Acceptance criterion: assertions must not depend solely on <c>Driver.Contents</c>.
    ///     This test confirms the diagnostic can also reach the actual <see cref="IOutput"/> layer —
    ///     <see cref="IOutput.GetLastOutput"/> returns the ANSI bytes that would be written to the
    ///     terminal, which is the source of truth beyond the in-memory <c>Driver.Contents</c> grid.
    /// </summary>
    [Fact]
    public void Diagnostic_IOutputLayer_ObservesActiveTabContent ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, Code [] codes, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        Code active = (Code)tabs.Value!;

        IOutput output1 = driver.GetOutput ();
        IOutputBuffer buffer = driver.GetOutputBuffer ();
        output1.Write (buffer);
        string ansiAfterInitial = output1.GetLastOutput ();

        output.WriteLine ($"Initial GetLastOutput length: {ansiAfterInitial.Length}");
        Assert.False (string.IsNullOrEmpty (ansiAfterInitial), "IOutput must produce non-empty output for initial draw.");

        counters.Reset ();

        active.Viewport = active.Viewport with { Y = 10 };
        root.Layout ();
        root.Draw ();

        output1.Write (buffer);
        string ansiAfterScroll = output1.GetLastOutput ();

        output.WriteLine ($"After-scroll GetLastOutput length: {ansiAfterScroll.Length}");
        Assert.False (string.IsNullOrEmpty (ansiAfterScroll), "IOutput must produce non-empty output after a scroll.");

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, "Active tab must draw for IOutput to receive content.");

        Assert.Contains ($"Tab{1} line", ansiAfterInitial);

        output.WriteLine (counters.Report ("IOutput-level check (1 scroll):"));

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Diagnostic reports layout and draw fan-out as separate metrics, so a regression can be
    ///     localized to one pipeline (layout vs draw) without conflating the two.
    /// </summary>
    [Fact]
    public void Diagnostic_LayoutAndDraw_ReportedSeparately ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, Code [] codes, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        Code active = (Code)tabs.Value!;
        counters.Reset ();

        active.Viewport = active.Viewport with { Y = 2 };
        root.Layout ();
        root.Draw ();

        int activeLayouts = counters.Get (active).SubViewsLaidOut;
        int activeDraws = counters.Get (active).DrawComplete;

        var inactiveLayouts = 0;
        var inactiveDraws = 0;

        for (var i = 1; i < TabCount; i++)
        {
            inactiveLayouts += counters.Get (codes [i]).SubViewsLaidOut;
            inactiveDraws += counters.Get (codes [i]).DrawComplete;
        }

        output.WriteLine ($"Layout: active={activeLayouts}, inactive_total={inactiveLayouts}");
        output.WriteLine ($"Draw:   active={activeDraws}, inactive_total={inactiveDraws}");
        output.WriteLine (counters.Report ("Single-scroll layout-vs-draw breakdown:"));

        Assert.True (activeLayouts > 0, $"Active tab must register layout activity, got {activeLayouts}.");
        Assert.True (activeDraws > 0, $"Active tab must register draw activity, got {activeDraws}.");

        // The two counters can move independently — RC2 in #4973 affects layout fan-out only,
        // RC1 affects draw fan-out only. Reporting them separately lets a regression land in just
        // one pipeline without being masked by the other.
        output.WriteLine ($"layout_fanout_ratio = {(activeLayouts == 0 ? "n/a" : ((double)inactiveLayouts / activeLayouts).ToString ("F2"))}");
        output.WriteLine ($"draw_fanout_ratio   = {(activeDraws == 0 ? "n/a" : ((double)inactiveDraws / activeDraws).ToString ("F2"))}");

        root.Dispose ();
        driver.Dispose ();
    }
}
