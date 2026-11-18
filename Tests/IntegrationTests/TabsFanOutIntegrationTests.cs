using System.Text;
using AppTestHelpers;

namespace IntegrationTests;

// Copilot

/// <summary>
///     Integration counterpart to <c>TabsFanOutDiagnosticTests</c>. Drives the active tab via real
///     key injection through the driver's input processor → command dispatch → main-loop
///     <c>LayoutAndDraw</c> path, instead of mutating <see cref="View.Viewport"/> directly. This
///     verifies the fan-out from issue #4973 / #5357 is observable end-to-end, not just under
///     synthetic <see cref="View.Layout()"/> / <see cref="View.Draw"/> calls.
/// </summary>
/// <remarks>
///     Instrumentation-only. The per-tab counters are attached to event subscriptions on
///     <see cref="View.DrawComplete"/>, <see cref="View.SubViewsLaidOut"/>, and
///     <see cref="View.ClearedViewport"/>; no rendering or invalidation behavior is changed.
/// </remarks>
public class TabsFanOutIntegrationTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    /// <summary>
    ///     A <see cref="Code"/> that registers <see cref="Command.ScrollDown"/> /
    ///     <see cref="Command.ScrollUp"/> so <see cref="Key.PageDown"/> / <see cref="Key.PageUp"/>
    ///     drive vertical scrolling through the normal command pipeline. Used only by this test —
    ///     <see cref="Code"/> doesn't expose <c>AddCommand</c> publicly, so a subclass is the
    ///     simplest way to wire a real input → scroll path without modifying production code.
    /// </summary>
    private sealed class ScrollableCode : Code
    {
        public ScrollableCode ()
        {
            AddCommand (Command.ScrollDown, () => ScrollVertical (1));
            AddCommand (Command.ScrollUp, () => ScrollVertical (-1));

            KeyBindings.Add (Key.PageDown, Command.ScrollDown);
            KeyBindings.Add (Key.PageUp, Command.ScrollUp);
        }
    }

    private sealed class Counters
    {
        public int SubViewsLaidOut;
        public int DrawComplete;
        public int ClearedViewport;
        public int DrawingText;
    }

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

    /// <summary>
    ///     End-to-end check: a real <see cref="Key.PageDown"/> on the active tab keeps both
    ///     layout and text drawing on the active tab.
    /// </summary>
    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Integration_RealPageDown_OnActiveTab_DoesNotFanOutLayoutToInactiveTabs (string driverName)
    {
        const int TabCount = 4;

        Tabs tabs = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        ScrollableCode [] codes = new ScrollableCode [TabCount];

        for (var i = 0; i < TabCount; i++)
        {
            codes [i] = new ScrollableCode
            {
                Title = $"Tab{i + 1}",
                Text = MakeText ($"Tab{i + 1} line", 80),
                Language = null,
                SyntaxHighlighter = null,
                Width = Dim.Fill (),
                Height = Dim.Fill ()
            };

            tabs.Add (codes [i]);
        }

        Counters [] perTab = new Counters [TabCount];
        Counters tabsContainer = new ();

        for (var i = 0; i < TabCount; i++)
        {
            int captured = i;
            perTab [i] = new Counters ();
            codes [i].SubViewsLaidOut += (_, _) => perTab [captured].SubViewsLaidOut++;
            codes [i].DrawComplete += (_, _) => perTab [captured].DrawComplete++;
            codes [i].ClearedViewport += (_, _) => perTab [captured].ClearedViewport++;
            codes [i].DrawingText += (_, _) => perTab [captured].DrawingText++;
        }

        tabs.SubViewsLaidOut += (_, _) => tabsContainer.SubViewsLaidOut++;
        tabs.DrawComplete += (_, _) => tabsContainer.DrawComplete++;
        tabs.ClearedViewport += (_, _) => tabsContainer.ClearedViewport++;
        tabs.DrawingText += (_, _) => tabsContainer.DrawingText++;

        ScrollableCode active = codes [0];

        using AppTestHelper helper = With.A<Window> (60, 20, driverName, _out)
                                         .Add (tabs)
                                         .Focus (active)
                                         .Then (
                                                _ =>
                                                {
                                                    for (var i = 0; i < TabCount; i++)
                                                    {
                                                        perTab [i].SubViewsLaidOut = 0;
                                                        perTab [i].DrawComplete = 0;
                                                        perTab [i].ClearedViewport = 0;
                                                        perTab [i].DrawingText = 0;
                                                    }

                                                    tabsContainer.SubViewsLaidOut = 0;
                                                    tabsContainer.DrawComplete = 0;
                                                    tabsContainer.ClearedViewport = 0;
                                                    tabsContainer.DrawingText = 0;
                                                })
                                         .KeyDown (Key.PageDown)
                                         .KeyDown (Key.PageDown)
                                         .KeyDown (Key.PageDown);

        outputHelper.WriteLine ($"Driver: {driverName}");
        outputHelper.WriteLine ($"Active tab viewport Y after 3 PageDowns: {active.Viewport.Y}");
        outputHelper.WriteLine ("Per-tab counters (after 3 PageDowns on active tab):");
        outputHelper.WriteLine ("  tab        laidOut  drawComplete  clearedViewport  drawingText");
        outputHelper.WriteLine ($"  Tabs       {tabsContainer.SubViewsLaidOut,7}  {tabsContainer.DrawComplete,12}  {tabsContainer.ClearedViewport,15}  {tabsContainer.DrawingText,11}");

        for (var i = 0; i < TabCount; i++)
        {
            outputHelper.WriteLine ($"  Code{i + 1,-6} {perTab [i].SubViewsLaidOut,7}  {perTab [i].DrawComplete,12}  {perTab [i].ClearedViewport,15}  {perTab [i].DrawingText,11}");
        }

        Assert.True (
                     active.Viewport.Y > 0,
                     $"PageDown should have scrolled the active tab via real input → command path. Got Viewport.Y={active.Viewport.Y}.");

        Assert.True (
                     perTab [0].DrawComplete > 0,
                     $"Active tab must draw in response to real PageDown, got DrawComplete={perTab [0].DrawComplete}.");

        int inactiveTextDraws = 0;
        int inactiveLayouts = 0;

        for (var i = 1; i < TabCount; i++)
        {
            inactiveTextDraws += perTab [i].DrawingText;
            inactiveLayouts += perTab [i].SubViewsLaidOut;
        }

        outputHelper.WriteLine ($"Sum inactive DrawingText = {inactiveTextDraws}");
        outputHelper.WriteLine ($"Sum inactive SubViewsLaidOut = {inactiveLayouts}");

        Assert.Equal (0, inactiveTextDraws);

        Assert.Equal (0, inactiveLayouts);
    }
}
