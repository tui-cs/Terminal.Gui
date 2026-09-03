#nullable enable

using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Graph View", "Demos the GraphView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Tabs")]
public class GraphViewExample : Scenario
{
    private readonly Thickness _thickness = new (1, 1, 1, 1);
    private CheckBox? _diagCheckBox;
    private CheckBox? _showBorderCheckBox;
    private ViewDiagnosticFlags _viewDiagnostics;
    private IApplication? _app;
    private Tabs? _tabs;
    private FrameView? _aboutFrame;

    /// <summary>
    ///     Gets the <see cref="GraphView"/> from the currently selected tab.
    /// </summary>
    private GraphView? CurrentGraphView => _tabs?.Value?.SubViews.OfType<GraphView> ().FirstOrDefault ();

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window window = new ();
        window.BorderStyle = LineStyle.None;

        // MenuBar
        MenuBar menu = new ();

        // Tabs
        _tabs = new Tabs { X = 0, Y = Pos.Bottom (menu), Width = Dim.Fill () };

        // Create tabs for each graph type
        CreateGraphTab ("Scatter _Plot", "Scatter Plot", SetupPeriodicTableScatterPlot);
        CreateGraphTab ("_Vertical Bar", "Vertical Bar Graph", gv => SetupLifeExpectancyBarGraph (gv, true));
        CreateGraphTab ("_Horizontal Bar", "Horizontal Bar Graph", gv => SetupLifeExpectancyBarGraph (gv, false));
        CreateGraphTab ("P_yramid", "Population Pyramid", SetupPopulationPyramid);
        CreateGraphTab ("_Line", "Line Graph", SetupLineGraph);
        CreateGraphTab ("Sine _Wave", "Sine Wave", SetupSineWave);
        CreateGraphTab ("_Disco", "Graphic Equalizer", SetupDisco);
        CreateGraphTab ("_Multi Bar", "Multi Bar", MultiBarGraph);

        _tabs.Value = _tabs.SubViews.ElementAt (0);

        // About
        _aboutFrame = new FrameView
        {
            Title = "About",
            X = 0,
            Y = Pos.AnchorEnd () - 1,
            Height = 5,
            Width = Dim.Fill (),
            BorderStyle = LineStyle.Dotted
        };

        _tabs.Height = Dim.Fill (_aboutFrame);

        _tabs.ValueChanged += (_, args) => { _aboutFrame.Text = args.NewValue?.SubViews.OfType<GraphView> ().FirstOrDefault ()?.Text ?? string.Empty; };

        // StatusBar
        StatusBar statusBar = new ([new Shortcut (Key.PageUp, "Zoom In", () => Zoom (0.5f)), new Shortcut (Key.PageDown, "Zoom Out", () => Zoom (2f))]);

        Shortcut diagShortcut = new () { Key = Key.F7, CommandView = new CheckBox { Title = "Diagnostics", CanFocus = false } };

        statusBar.Add (diagShortcut);
        diagShortcut.Accepting += DiagShortcut_Accept;

        // Menu setup
        _showBorderCheckBox = new CheckBox { Title = "_Enable Margin, Border, and Padding", Value = CheckState.Checked };
        _showBorderCheckBox.ValueChanged += (_, _) => ShowBorder ();

        _diagCheckBox = new CheckBox
        {
            Title = "_Diagnostics",
            Value = View.Diagnostics == (ViewDiagnosticFlags.Thickness | ViewDiagnosticFlags.Ruler) ? CheckState.Checked : CheckState.UnChecked
        };
        _diagCheckBox.ValueChanged += (_, _) => ToggleDiagnostics ();

        menu.Add (new MenuBarItem ("_View",
                                   [
                                       new MenuItem { Title = "Zoom _In", Action = () => Zoom (0.5f) },
                                       new MenuItem { Title = "Zoom _Out", Action = () => Zoom (2f) },
                                       new MenuItem { Title = "MarginLeft++", Action = () => Margin (true, true) },
                                       new MenuItem { Title = "MarginLeft--", Action = () => Margin (true, false) },
                                       new MenuItem { Title = "MarginBottom++", Action = () => Margin (false, true) },
                                       new MenuItem { Title = "MarginBottom--", Action = () => Margin (false, false) },
                                       new MenuItem { CommandView = _showBorderCheckBox },
                                       new MenuItem { CommandView = _diagCheckBox }
                                   ]));

        // Add views in order of visual appearance
        window.Add (menu, _tabs, _aboutFrame, statusBar);

        _viewDiagnostics = View.Diagnostics;
        app.Run (window);
        View.Diagnostics = _viewDiagnostics;
    }

    /// <summary>
    ///     Creates a tab containing a <see cref="GraphView"/> and an about <see cref="FrameView"/>,
    ///     then invokes the setup action to configure the graph.
    /// </summary>
    private void CreateGraphTab (string tabTitle, string graphTitle, Action<GraphView> setupAction)
    {
        View tab = new () { Title = tabTitle };

        GraphView graphView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.HeavyDotted,
            Title = graphTitle,
            Arrangement = ViewArrangement.Resizable | ViewArrangement.Overlapped
        };
        graphView.Border.Thickness = _thickness;
        graphView.Margin.Thickness = _thickness;
        graphView.Padding.Thickness = _thickness;

        tab.Add (graphView);
        _tabs?.Add (tab);

        // Defer setup to when the tab is first laid out so Viewport is valid
        graphView.Initialized += GraphViewOnInitialized;

        return;

        void GraphViewOnInitialized (object? sender, EventArgs e)
        {
            graphView.Initialized -= GraphViewOnInitialized;
            setupAction (graphView);
        }
    }

    private void DiagShortcut_Accept (object? sender, CommandEventArgs e)
    {
        ToggleDiagnostics ();

        if (sender is Shortcut { CommandView: CheckBox checkBox })
        {
            checkBox.Value = _diagCheckBox?.Value ?? CheckState.UnChecked;
        }
    }

    private void ToggleDiagnostics ()
    {
        View.Diagnostics = _diagCheckBox?.Value == CheckState.Checked ? ViewDiagnosticFlags.Thickness | ViewDiagnosticFlags.Ruler : ViewDiagnosticFlags.Off;
        _app?.LayoutAndDraw ();
    }

    private void Margin (bool left, bool increase)
    {
        GraphView? graphView = CurrentGraphView;

        if (graphView is null)
        {
            return;
        }

        if (left)
        {
            graphView.MarginLeft = (uint)Math.Max (0, (int)graphView.MarginLeft + (increase ? 1 : -1));
        }
        else
        {
            graphView.MarginBottom = (uint)Math.Max (0, (int)graphView.MarginBottom + (increase ? 1 : -1));
        }

        graphView.SetNeedsDraw ();
    }

    private void MultiBarGraph (GraphView graphView)
    {
        graphView.Reset ();

        graphView.Text = "Housing Expenditures by income thirds 1996-2003";

        Color fore = graphView.GetAttributeForRole (VisualRole.Normal).Foreground == Color.Black
                         ? Color.White
                         : graphView.GetAttributeForRole (VisualRole.Normal).Foreground;
        Attribute black = new (fore, Color.Black);
        Attribute cyan = new (Color.BrightCyan, Color.Black);
        Attribute magenta = new (Color.BrightMagenta, Color.Black);
        Attribute red = new (Color.BrightRed, Color.Black);

        graphView.GraphColor = black;

        MultiBarSeries series = new (3, 1, 0.25f, [magenta, cyan, red]);

        Rune stiple = Glyphs.Stipple;

        series.AddBars ("'96", stiple, 5900, 9000, 14000);
        series.AddBars ("'97", stiple, 6100, 9200, 14800);
        series.AddBars ("'98", stiple, 6000, 9300, 14600);
        series.AddBars ("'99", stiple, 6100, 9400, 14950);
        series.AddBars ("'00", stiple, 6200, 9500, 15200);
        series.AddBars ("'01", stiple, 6250, 9900, 16000);
        series.AddBars ("'02", stiple, 6600, 11000, 16700);
        series.AddBars ("'03", stiple, 7000, 12000, 17000);

        graphView.CellSize = new PointF (0.25f, 1000);
        graphView.Series.Add (series);
        graphView.SetNeedsDraw ();

        graphView.MarginLeft = 3;
        graphView.MarginBottom = 1;

        graphView.AxisY.LabelGetter = v => '$' + (v.Value / 1000f).ToString ("N0") + 'k';

        // Do not show x-axis labels (bars draw their own labels)
        graphView.AxisX.Increment = 0;
        graphView.AxisX.ShowLabelsEvery = 0;
        graphView.AxisX.Minimum = 0;

        graphView.AxisY.Minimum = 0;

        LegendAnnotation legend = new (new Rectangle (graphView.Viewport.Width - 20, 0, 20, 5));

        legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (0).OverrideBarColor ?? black), "Lower Third");

        legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (1).OverrideBarColor ?? cyan), "Middle Third");

        legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (2).OverrideBarColor ?? red), "Upper Third");
        graphView.Annotations.Add (legend);
    }

    private void SetupDisco (GraphView graphView)
    {
        graphView.Reset ();

        graphView.Text = "This graph shows a graphic equalizer for an imaginary song";

        graphView.GraphColor = new Attribute (Color.White, Color.Black);

        GraphCellToRender stiple = new ((Rune)'\u2593');

        Random r = new ();
        DiscoBarSeries series = new ();
        List<BarSeriesBar> bars = [];

        _app?.AddTimeout (TimeSpan.FromMilliseconds (250), GenSample);

        series.Bars = bars;

        graphView.Series.Add (series);

        // How much graph space each cell of the console depicts
        graphView.CellSize = new PointF (1, 10);
        graphView.AxisX.Increment = 0; // No graph ticks
        graphView.AxisX.ShowLabelsEvery = 0; // no labels

        graphView.AxisX.Visible = false;
        graphView.AxisY.Visible = false;

        graphView.SetNeedsDraw ();

        return;

        bool GenSample ()
        {
            bars.Clear ();

            // generate an imaginary sample
            for (var i = 0; i < 31; i++)
            {
                bars.Add (new BarSeriesBar (string.Empty, stiple, r.Next (0, 100)));
            }

            graphView.SetNeedsDraw ();

            // while the equaliser is showing
            return graphView.Series.Contains (series);
        }
    }

    private void SetupLifeExpectancyBarGraph (GraphView graphView, bool verticalBars)
    {
        graphView.Reset ();

        graphView.Text = "This graph shows the life expectancy at birth of a range of countries";

        GraphCellToRender softStiple = new ((Rune)'\u2591');
        GraphCellToRender mediumStiple = new ((Rune)'\u2592');

        BarSeries barSeries = new ()
        {
            Bars =
            [
                new BarSeriesBar ("Switzerland", softStiple, 83.4f),
                new BarSeriesBar ("South Korea", !verticalBars ? mediumStiple : softStiple, 83.3f),
                new BarSeriesBar ("Singapore", softStiple, 83.2f),
                new BarSeriesBar ("Spain", !verticalBars ? mediumStiple : softStiple, 83.2f),
                new BarSeriesBar ("Cyprus", softStiple, 83.1f),
                new BarSeriesBar ("Australia", !verticalBars ? mediumStiple : softStiple, 83),
                new BarSeriesBar ("Italy", softStiple, 83),
                new BarSeriesBar ("Norway", !verticalBars ? mediumStiple : softStiple, 83),
                new BarSeriesBar ("Israel", softStiple, 82.6f),
                new BarSeriesBar ("France", !verticalBars ? mediumStiple : softStiple, 82.5f),
                new BarSeriesBar ("Luxembourg", softStiple, 82.4f),
                new BarSeriesBar ("Sweden", !verticalBars ? mediumStiple : softStiple, 82.4f),
                new BarSeriesBar ("Iceland", softStiple, 82.3f),
                new BarSeriesBar ("Canada", !verticalBars ? mediumStiple : softStiple, 82.2f),
                new BarSeriesBar ("New Zealand", softStiple, 82),
                new BarSeriesBar ("Malta", !verticalBars ? mediumStiple : softStiple, 81.9f),
                new BarSeriesBar ("Ireland", softStiple, 81.8f)
            ]
        };

        graphView.Series.Add (barSeries);

        if (verticalBars)
        {
            barSeries.Orientation = Orientation.Vertical;

            // How much graph space each cell of the console depicts
            graphView.CellSize = new PointF (0.1f, 0.25f);

            // No axis marks since Bar will add its own categorical marks
            graphView.AxisX.Increment = 0f;
            graphView.AxisX.Text = "Country";
            graphView.AxisX.Minimum = 0;

            graphView.AxisY.Increment = 1f;
            graphView.AxisY.ShowLabelsEvery = 1;
            graphView.AxisY.LabelGetter = v => v.Value.ToString ("N2");
            graphView.AxisY.Minimum = 0;
            graphView.AxisY.Text = "Age";

            // leave space for axis labels and title
            graphView.MarginBottom = 2;
            graphView.MarginLeft = 6;

            // Start the graph at 80 years because that is where most of our data is
            graphView.ScrollOffset = new PointF (0, 80);
        }
        else
        {
            barSeries.Orientation = Orientation.Horizontal;

            // How much graph space each cell of the console depicts
            graphView.CellSize = new PointF (0.1f, 1f);

            // No axis marks since Bar will add its own categorical marks
            graphView.AxisY.Increment = 0f;
            graphView.AxisY.ShowLabelsEvery = 1;
            graphView.AxisY.Text = "Country";
            graphView.AxisY.Minimum = 0;

            graphView.AxisX.Increment = 1f;
            graphView.AxisX.ShowLabelsEvery = 1;
            graphView.AxisX.LabelGetter = v => v.Value.ToString ("N2");
            graphView.AxisX.Text = "Age";
            graphView.AxisX.Minimum = 0;

            // leave space for axis labels and title
            graphView.MarginBottom = 2;
            graphView.MarginLeft = (uint)barSeries.Bars.Max (b => b.Text.Length) + 2;

            // Start the graph at 80 years because that is where most of our data is
            graphView.ScrollOffset = new PointF (80, 0);
        }

        graphView.SetNeedsDraw ();
    }

    private void SetupLineGraph (GraphView graphView)
    {
        graphView.Reset ();

        graphView.Text = "This graph shows random points";

        Attribute black = new (graphView.GetAttributeForRole (VisualRole.Normal).Foreground,
                               Color.Black,
                               graphView.GetAttributeForRole (VisualRole.Normal).Style);
        Attribute cyan = new (Color.BrightCyan, Color.Black);
        Attribute magenta = new (Color.BrightMagenta, Color.Black);
        Attribute red = new (Color.BrightRed, Color.Black);

        graphView.GraphColor = black;

        List<PointF> randomPoints = [];

        Random r = new ();

        for (var i = 0; i < 10; i++)
        {
            randomPoints.Add (new PointF (r.Next (100), r.Next (100)));
        }

        ScatterSeries points = new () { Points = randomPoints };

        PathAnnotation line = new () { LineColor = cyan, Points = randomPoints.OrderBy (p => p.X).ToList (), BeforeSeries = true };

        graphView.Series.Add (points);
        graphView.Annotations.Add (line);

        randomPoints = [];

        for (var i = 0; i < 10; i++)
        {
            randomPoints.Add (new PointF (r.Next (100), r.Next (100)));
        }

        ScatterSeries points2 = new () { Points = randomPoints, Fill = new GraphCellToRender ((Rune)'x', red) };

        PathAnnotation line2 = new () { LineColor = magenta, Points = randomPoints.OrderBy (p => p.X).ToList (), BeforeSeries = true };

        graphView.Series.Add (points2);
        graphView.Annotations.Add (line2);

        // How much graph space each cell of the console depicts
        graphView.CellSize = new PointF (2, 5);

        // leave space for axis labels
        graphView.MarginBottom = 2;
        graphView.MarginLeft = 3;

        // One axis tick/label per
        graphView.AxisX.Increment = 20;
        graphView.AxisX.ShowLabelsEvery = 1;
        graphView.AxisX.Text = "X →";

        graphView.AxisY.Increment = 20;
        graphView.AxisY.ShowLabelsEvery = 1;
        graphView.AxisY.Text = "↑Y";

        PointF max = line.Points.Union (line2.Points).OrderByDescending (p => p.Y).First ();

        graphView.Annotations.Add (new TextAnnotation { Text = "(Max)", GraphPosition = max with { X = max.X + 2 * graphView.CellSize.X } });

        graphView.SetNeedsDraw ();
    }

    private void SetupPeriodicTableScatterPlot (GraphView graphView)
    {
        graphView.Reset ();

        graphView.Text =
            "This graph shows the atomic weight of each element in the periodic table.\nStarting with Hydrogen (atomic Number 1 with a weight of 1.007)";

        //AtomicNumber and AtomicMass of all elements in the periodic table
        graphView.Series.Add (new ScatterSeries
        {
            Points =
            [
                new PointF (1, 1.007f),
                new PointF (2, 4.002f),
                new PointF (3, 6.941f),
                new PointF (4, 9.012f),
                new PointF (5, 10.811f),
                new PointF (6, 12.011f),
                new PointF (7, 14.007f),
                new PointF (8, 15.999f),
                new PointF (9, 18.998f),
                new PointF (10, 20.18f),
                new PointF (11, 22.99f),
                new PointF (12, 24.305f),
                new PointF (13, 26.982f),
                new PointF (14, 28.086f),
                new PointF (15, 30.974f),
                new PointF (16, 32.065f),
                new PointF (17, 35.453f),
                new PointF (18, 39.948f),
                new PointF (19, 39.098f),
                new PointF (20, 40.078f),
                new PointF (21, 44.956f),
                new PointF (22, 47.867f),
                new PointF (23, 50.942f),
                new PointF (24, 51.996f),
                new PointF (25, 54.938f),
                new PointF (26, 55.845f),
                new PointF (27, 58.933f),
                new PointF (28, 58.693f),
                new PointF (29, 63.546f),
                new PointF (30, 65.38f),
                new PointF (31, 69.723f),
                new PointF (32, 72.64f),
                new PointF (33, 74.922f),
                new PointF (34, 78.96f),
                new PointF (35, 79.904f),
                new PointF (36, 83.798f),
                new PointF (37, 85.468f),
                new PointF (38, 87.62f),
                new PointF (39, 88.906f),
                new PointF (40, 91.224f),
                new PointF (41, 92.906f),
                new PointF (42, 95.96f),
                new PointF (43, 98f),
                new PointF (44, 101.07f),
                new PointF (45, 102.906f),
                new PointF (46, 106.42f),
                new PointF (47, 107.868f),
                new PointF (48, 112.411f),
                new PointF (49, 114.818f),
                new PointF (50, 118.71f),
                new PointF (51, 121.76f),
                new PointF (52, 127.6f),
                new PointF (53, 126.904f),
                new PointF (54, 131.293f),
                new PointF (55, 132.905f),
                new PointF (56, 137.327f),
                new PointF (57, 138.905f),
                new PointF (58, 140.116f),
                new PointF (59, 140.908f),
                new PointF (60, 144.242f),
                new PointF (61, 145),
                new PointF (62, 150.36f),
                new PointF (63, 151.964f),
                new PointF (64, 157.25f),
                new PointF (65, 158.925f),
                new PointF (66, 162.5f),
                new PointF (67, 164.93f),
                new PointF (68, 167.259f),
                new PointF (69, 168.934f),
                new PointF (70, 173.054f),
                new PointF (71, 174.967f),
                new PointF (72, 178.49f),
                new PointF (73, 180.948f),
                new PointF (74, 183.84f),
                new PointF (75, 186.207f),
                new PointF (76, 190.23f),
                new PointF (77, 192.217f),
                new PointF (78, 195.084f),
                new PointF (79, 196.967f),
                new PointF (80, 200.59f),
                new PointF (81, 204.383f),
                new PointF (82, 207.2f),
                new PointF (83, 208.98f),
                new PointF (84, 210),
                new PointF (85, 210),
                new PointF (86, 222),
                new PointF (87, 223),
                new PointF (88, 226),
                new PointF (89, 227),
                new PointF (90, 232.038f),
                new PointF (91, 231.036f),
                new PointF (92, 238.029f),
                new PointF (93, 237),
                new PointF (94, 244),
                new PointF (95, 243),
                new PointF (96, 247),
                new PointF (97, 247),
                new PointF (98, 251),
                new PointF (99, 252),
                new PointF (100, 257),
                new PointF (101, 258),
                new PointF (102, 259),
                new PointF (103, 262),
                new PointF (104, 261),
                new PointF (105, 262),
                new PointF (106, 266),
                new PointF (107, 264),
                new PointF (108, 267),
                new PointF (109, 268),
                new PointF (113, 284),
                new PointF (114, 289),
                new PointF (115, 288),
                new PointF (116, 292),
                new PointF (117, 295),
                new PointF (118, 294)
            ]
        });

        // How much graph space each cell of the console depicts
        graphView.CellSize = new PointF (1, 5);

        // leave space for axis labels
        graphView.MarginBottom = 2;
        graphView.MarginLeft = 3;

        // One axis tick/label per 5 atomic numbers
        graphView.AxisX.Increment = 5;
        graphView.AxisX.ShowLabelsEvery = 1;
        graphView.AxisX.Text = "Atomic Number";
        graphView.AxisX.Minimum = 0;

        // One label every 5 atomic weight
        graphView.AxisY.Increment = 5;
        graphView.AxisY.ShowLabelsEvery = 1;
        graphView.AxisY.Minimum = 0;

        graphView.SetNeedsDraw ();
    }

    private void SetupPopulationPyramid (GraphView graphView)
    {
        graphView.Reset ();

        graphView.Text = "This graph shows population of each age divided by gender";

        // How much graph space each cell of the console depicts
        graphView.CellSize = new PointF (100_000, 1);

        //center the x-axis in middle of screen to show both sides
        graphView.ScrollOffset = new PointF (-3_000_000, 0);

        graphView.AxisX.Text = "Number Of People";
        graphView.AxisX.Increment = 500_000;
        graphView.AxisX.ShowLabelsEvery = 2;

        // use Abs to make negative axis labels positive
        graphView.AxisX.LabelGetter = v => Math.Abs (v.Value / 1_000_000).ToString ("N2") + "M";

        // leave space for axis labels
        graphView.MarginBottom = 2;
        graphView.MarginLeft = 1;

        // do not show axis titles (bars have their own categories)
        graphView.AxisY.Increment = 0;
        graphView.AxisY.ShowLabelsEvery = 0;
        graphView.AxisY.Minimum = 0;

        GraphCellToRender stiple = new (Glyphs.Stipple);

        // Bars in 2 directions

        // Males (negative to make the bars go left)
        BarSeries malesSeries = new ()
        {
            Orientation = Orientation.Horizontal,
            Bars =
            [
                new BarSeriesBar ("0-4", stiple, -2009363),
                new BarSeriesBar ("5-9", stiple, -2108550),
                new BarSeriesBar ("10-14", stiple, -2022370),
                new BarSeriesBar ("15-19", stiple, -1880611),
                new BarSeriesBar ("20-24", stiple, -2072674),
                new BarSeriesBar ("25-29", stiple, -2275138),
                new BarSeriesBar ("30-34", stiple, -2361054),
                new BarSeriesBar ("35-39", stiple, -2279836),
                new BarSeriesBar ("40-44", stiple, -2148253),
                new BarSeriesBar ("45-49", stiple, -2128343),
                new BarSeriesBar ("50-54", stiple, -2281421),
                new BarSeriesBar ("55-59", stiple, -2232388),
                new BarSeriesBar ("60-64", stiple, -1919839),
                new BarSeriesBar ("65-69", stiple, -1647391),
                new BarSeriesBar ("70-74", stiple, -1624635),
                new BarSeriesBar ("75-79", stiple, -1137438),
                new BarSeriesBar ("80-84", stiple, -766956),
                new BarSeriesBar ("85-89", stiple, -438663),
                new BarSeriesBar ("90-94", stiple, -169952),
                new BarSeriesBar ("95-99", stiple, -34524),
                new BarSeriesBar ("100+", stiple, -3016)
            ]
        };
        graphView.Series.Add (malesSeries);

        // Females
        BarSeries femalesSeries = new ()
        {
            Orientation = Orientation.Horizontal,
            Bars =
            [
                new BarSeriesBar ("0-4", stiple, 1915127),
                new BarSeriesBar ("5-9", stiple, 2011016),
                new BarSeriesBar ("10-14", stiple, 1933970),
                new BarSeriesBar ("15-19", stiple, 1805522),
                new BarSeriesBar ("20-24", stiple, 2001966),
                new BarSeriesBar ("25-29", stiple, 2208929),
                new BarSeriesBar ("30-34", stiple, 2345774),
                new BarSeriesBar ("35-39", stiple, 2308360),
                new BarSeriesBar ("40-44", stiple, 2159877),
                new BarSeriesBar ("45-49", stiple, 2167778),
                new BarSeriesBar ("50-54", stiple, 2353119),
                new BarSeriesBar ("55-59", stiple, 2306537),
                new BarSeriesBar ("60-64", stiple, 1985177),
                new BarSeriesBar ("65-69", stiple, 1734370),
                new BarSeriesBar ("70-74", stiple, 1763853),
                new BarSeriesBar ("75-79", stiple, 1304709),
                new BarSeriesBar ("80-84", stiple, 969611),
                new BarSeriesBar ("85-89", stiple, 638892),
                new BarSeriesBar ("90-94", stiple, 320625),
                new BarSeriesBar ("95-99", stiple, 95559),
                new BarSeriesBar ("100+", stiple, 12818)
            ]
        };

        GraphCellToRender softStiple = new ((Rune)'\u2591');
        GraphCellToRender mediumStiple = new ((Rune)'\u2592');

        for (var i = 0; i < malesSeries.Bars.Count; i++)
        {
            malesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
            femalesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
        }

        graphView.Series.Add (femalesSeries);

        graphView.Annotations.Add (new TextAnnotation { Text = "M", ScreenPosition = new Point (0, 10) });

        graphView.Annotations.Add (new TextAnnotation { Text = "F", ScreenPosition = new Point (graphView.Viewport.Width - 1, 10) });

        graphView.SetNeedsDraw ();
    }

    private void SetupSineWave (GraphView graphView)
    {
        graphView.Reset ();

        graphView.Text = "This graph shows a sine wave";

        ScatterSeries points = new ();

        PathAnnotation line = new ()
        {
            // Draw line first so it does not draw over top of points or axis labels
            BeforeSeries = true
        };

        // Generate line graph with 2,000 points
        for (float x = -500; x < 500; x += 0.5f)
        {
            points.Points.Add (new PointF (x, (float)Math.Sin (x)));
            line.Points.Add (new PointF (x, (float)Math.Sin (x)));
        }

        graphView.Series.Add (points);
        graphView.Annotations.Add (line);

        // How much graph space each cell of the console depicts
        graphView.CellSize = new PointF (0.1f, 0.1f);

        // leave space for axis labels
        graphView.MarginBottom = 2;
        graphView.MarginLeft = 3;

        // One axis tick/label per
        graphView.AxisX.Increment = 0.5f;
        graphView.AxisX.ShowLabelsEvery = 2;
        graphView.AxisX.Text = "X →";
        graphView.AxisX.LabelGetter = v => v.Value.ToString ("N2");

        graphView.AxisY.Increment = 0.2f;
        graphView.AxisY.ShowLabelsEvery = 2;
        graphView.AxisY.Text = "↑Y";
        graphView.AxisY.LabelGetter = v => v.Value.ToString ("N2");

        graphView.ScrollOffset = new PointF (-2.5f, -1);

        graphView.SetNeedsDraw ();
    }

    private void ShowBorder ()
    {
        GraphView? graphView = CurrentGraphView;

        if (graphView is null)
        {
            return;
        }

        if (_showBorderCheckBox?.Value == CheckState.Checked)
        {
            graphView.BorderStyle = LineStyle.Single;
            graphView.Border.Thickness = _thickness;
            graphView.Margin.Thickness = _thickness;
            graphView.Padding.Thickness = _thickness;
        }
        else
        {
            graphView.BorderStyle = LineStyle.None;
            graphView.Margin.Thickness = Thickness.Empty;
            graphView.Padding.Thickness = Thickness.Empty;
        }
    }

    private void Zoom (float factor)
    {
        GraphView? graphView = CurrentGraphView;

        if (graphView is null)
        {
            return;
        }

        graphView.CellSize = new PointF (graphView.CellSize.X * factor, graphView.CellSize.Y * factor);

        graphView.AxisX.Increment *= factor;
        graphView.AxisY.Increment *= factor;

        graphView.SetNeedsDraw ();
    }

    private sealed class DiscoBarSeries : BarSeries
    {
        private readonly Attribute _brightGreen = new (Color.Green, Color.Black);
        private readonly Attribute _brightRed = new (Color.BrightRed, Color.Black);
        private readonly Attribute _brightYellow = new (Color.BrightYellow, Color.Black);
        private readonly Attribute _green = new (Color.BrightGreen, Color.Black);
        private readonly Attribute _red = new (Color.Red, Color.Black);

        protected override void DrawBarLine (GraphView graph, Point start, Point end, BarSeriesBar beingDrawn)
        {
            int x = start.X;

            for (int y = end.Y; y <= start.Y; y++)
            {
                float height = graph.ViewportToGraphSpace (x, y).Y;

                Attribute attr = height switch
                {
                    >= 85 => _red,
                    >= 66 => _brightRed,
                    >= 45 => _brightYellow,
                    >= 25 => _brightGreen,
                    _ => _green
                };

                graph.SetAttribute (attr);
                graph.AddRune (x, y, beingDrawn.Fill.Rune);
            }
        }
    }
}
