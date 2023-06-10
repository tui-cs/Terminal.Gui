using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

using Color = Terminal.Gui.Color;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Graph View", Description: "Demos the GraphView control.")]
	[ScenarioCategory ("Controls")]
	public class GraphViewExample : Scenario {

		GraphView graphView;
		private TextView about;

		int currentGraph = 0;
		Action [] graphs;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			graphs = new Action [] {
				 ()=>SetupPeriodicTableScatterPlot(),    //0
				 ()=>SetupLifeExpectancyBarGraph(true),  //1
				 ()=>SetupLifeExpectancyBarGraph(false), //2
				 ()=>SetupPopulationPyramid(),           //3
				 ()=>SetupLineGraph(),                   //4
				 ()=>SetupSineWave(),                    //5
				 ()=>SetupDisco(),                       //6
				 ()=>MultiBarGraph()                     //7
			};

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("Scatter _Plot", "",()=>graphs[currentGraph = 0]()),
					new MenuItem ("_V Bar Graph", "", ()=>graphs[currentGraph = 1]()),
					new MenuItem ("_H Bar Graph", "", ()=>graphs[currentGraph = 2]()) ,
					new MenuItem ("P_opulation Pyramid","",()=>graphs[currentGraph = 3]()),
					new MenuItem ("_Line Graph","",()=>graphs[currentGraph = 4]()),
					new MenuItem ("Sine _Wave","",()=>graphs[currentGraph = 5]()),
					new MenuItem ("Silent _Disco","",()=>graphs[currentGraph = 6]()),
					new MenuItem ("_Multi Bar Graph","",()=>graphs[currentGraph = 7]()),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					new MenuItem ("Zoom _In", "", () => Zoom(0.5f)),
					 new MenuItem ("Zoom _Out", "", () =>  Zoom(2f)),
					new MenuItem ("MarginLeft++", "", () => Margin(true,true)),
					new MenuItem ("MarginLeft--", "", () => Margin(true,false)),
					new MenuItem ("MarginBottom++", "", () => Margin(false,true)),
					new MenuItem ("MarginBottom--", "", () => Margin(false,false)),
				}),

				});
			Application.Top.Add (menu);

			graphView = new GraphView () {
				X = 1,
				Y = 1,
				Width = 60,
				Height = 20,
			};

			Win.Add (graphView);

			var frameRight = new FrameView ("About") {
				X = Pos.Right (graphView) + 1,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			frameRight.Add (about = new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			});

			Win.Add (frameRight);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
				new StatusItem(Key.CtrlMask | Key.G, "~^G~ Next", ()=>graphs[currentGraph++%graphs.Length]()),
			});
			Application.Top.Add (statusBar);
		}

		private void MultiBarGraph ()
		{
			graphView.Reset ();

			about.Text = "Housing Expenditures by income thirds 1996-2003";

			var fore = graphView.ColorScheme.Normal.Foreground == Color.Black ? Color.White : graphView.ColorScheme.Normal.Foreground;
			var black = Application.Driver.MakeAttribute (fore, Color.Black);
			var cyan = Application.Driver.MakeAttribute (Color.BrightCyan, Color.Black);
			var magenta = Application.Driver.MakeAttribute (Color.BrightMagenta, Color.Black);
			var red = Application.Driver.MakeAttribute (Color.BrightRed, Color.Black);

			graphView.GraphColor = black;

			var series = new MultiBarSeries (3, 1, 0.25f, new [] { magenta, cyan, red });

			var stiple = CM.Glyphs.Stipple;

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
			graphView.SetNeedsDisplay ();

			graphView.MarginLeft = 3;
			graphView.MarginBottom = 1;

			graphView.AxisY.LabelGetter = (v) => '$' + (v.Value / 1000f).ToString ("N0") + 'k';

			// Do not show x axis labels (bars draw their own labels)
			graphView.AxisX.Increment = 0;
			graphView.AxisX.ShowLabelsEvery = 0;
			graphView.AxisX.Minimum = 0;

			graphView.AxisY.Minimum = 0;

			var legend = new LegendAnnotation (new Rect (graphView.Bounds.Width - 20, 0, 20, 5));
			legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (0).OverrideBarColor), "Lower Third");
			legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (1).OverrideBarColor), "Middle Third");
			legend.AddEntry (new GraphCellToRender (stiple, series.SubSeries.ElementAt (2).OverrideBarColor), "Upper Third");
			graphView.Annotations.Add (legend);
		}

		private void SetupLineGraph ()
		{
			graphView.Reset ();

			about.Text = "This graph shows random points";

			var black = Application.Driver.MakeAttribute (graphView.ColorScheme.Normal.Foreground, Color.Black);
			var cyan = Application.Driver.MakeAttribute (Color.BrightCyan, Color.Black);
			var magenta = Application.Driver.MakeAttribute (Color.BrightMagenta, Color.Black);
			var red = Application.Driver.MakeAttribute (Color.BrightRed, Color.Black);

			graphView.GraphColor = black;

			List<PointF> randomPoints = new List<PointF> ();

			Random r = new Random ();

			for (int i = 0; i < 10; i++) {
				randomPoints.Add (new PointF (r.Next (100), r.Next (100)));
			}

			var points = new ScatterSeries () {
				Points = randomPoints
			};

			var line = new PathAnnotation () {
				LineColor = cyan,
				Points = randomPoints.OrderBy (p => p.X).ToList (),
				BeforeSeries = true,
			};

			graphView.Series.Add (points);
			graphView.Annotations.Add (line);

			randomPoints = new List<PointF> ();

			for (int i = 0; i < 10; i++) {
				randomPoints.Add (new PointF (r.Next (100), r.Next (100)));
			}

			var points2 = new ScatterSeries () {
				Points = randomPoints,
				Fill = new GraphCellToRender ((Rune)'x', red)
			};

			var line2 = new PathAnnotation () {
				LineColor = magenta,
				Points = randomPoints.OrderBy (p => p.X).ToList (),
				BeforeSeries = true,
			};

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

			var max = line.Points.Union (line2.Points).OrderByDescending (p => p.Y).First ();
			graphView.Annotations.Add (new TextAnnotation () { Text = "(Max)", GraphPosition = new PointF (max.X + (2 * graphView.CellSize.X), max.Y) });

			graphView.SetNeedsDisplay ();
		}

		private void SetupSineWave ()
		{
			graphView.Reset ();

			about.Text = "This graph shows a sine wave";

			var points = new ScatterSeries ();
			var line = new PathAnnotation ();

			// Draw line first so it does not draw over top of points or axis labels
			line.BeforeSeries = true;

			// Generate line graph with 2,000 points
			for (float x = -500; x < 500; x += 0.5f) {
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
			graphView.AxisX.LabelGetter = (v) => v.Value.ToString ("N2");

			graphView.AxisY.Increment = 0.2f;
			graphView.AxisY.ShowLabelsEvery = 2;
			graphView.AxisY.Text = "↑Y";
			graphView.AxisY.LabelGetter = (v) => v.Value.ToString ("N2");

			graphView.ScrollOffset = new PointF (-2.5f, -1);

			graphView.SetNeedsDisplay ();
		}
		/*
		Country,Both,Male,Female

"Switzerland",83.4,81.8,85.1
"South Korea",83.3,80.3,86.1
"Singapore",83.2,81,85.5
"Spain",83.2,80.7,85.7
"Cyprus",83.1,81.1,85.1
"Australia",83,81.3,84.8
"Italy",83,80.9,84.9
"Norway",83,81.2,84.7
"Israel",82.6,80.8,84.4
"France",82.5,79.8,85.1
"Luxembourg",82.4,80.6,84.2
"Sweden",82.4,80.8,84
"Iceland",82.3,80.8,83.9
"Canada",82.2,80.4,84.1
"New Zealand",82,80.4,83.5
"Malta,81.9",79.9,83.8
"Ireland",81.8,80.2,83.5
"Netherlands",81.8,80.4,83.1
"Germany",81.7,78.7,84.8
"Austria",81.6,79.4,83.8
"Finland",81.6,79.2,84
"Portugal",81.6,78.6,84.4
"Belgium",81.4,79.3,83.5
"United Kingdom",81.4,79.8,83
"Denmark",81.3,79.6,83
"Slovenia",81.3,78.6,84.1
"Greece",81.1,78.6,83.6
"Kuwait",81,79.3,83.9
"Costa Rica",80.8,78.3,83.4*/

		private void SetupLifeExpectancyBarGraph (bool verticalBars)
		{
			graphView.Reset ();

			about.Text = "This graph shows the life expectancy at birth of a range of countries";

			var softStiple = new GraphCellToRender ((Rune)'\u2591');
			var mediumStiple = new GraphCellToRender ((Rune)'\u2592');

			var barSeries = new BarSeries () {
				Bars = new List<BarSeriesBar> () {
					new BarSeriesBar ("Switzerland", softStiple, 83.4f),
					new BarSeriesBar ("South Korea", !verticalBars?mediumStiple:softStiple, 83.3f),
					new BarSeriesBar ("Singapore", softStiple, 83.2f),
					new BarSeriesBar ("Spain", !verticalBars?mediumStiple:softStiple, 83.2f),
					new BarSeriesBar ("Cyprus", softStiple, 83.1f),
					new BarSeriesBar ("Australia", !verticalBars?mediumStiple:softStiple, 83),
					new BarSeriesBar ("Italy", softStiple, 83),
					new BarSeriesBar ("Norway", !verticalBars?mediumStiple:softStiple, 83),
					new BarSeriesBar ("Israel", softStiple, 82.6f),
					new BarSeriesBar ("France", !verticalBars?mediumStiple:softStiple, 82.5f),
					new BarSeriesBar ("Luxembourg", softStiple, 82.4f),
					new BarSeriesBar ("Sweden", !verticalBars?mediumStiple:softStiple, 82.4f),
					new BarSeriesBar ("Iceland", softStiple, 82.3f),
					new BarSeriesBar ("Canada", !verticalBars?mediumStiple:softStiple, 82.2f),
					new BarSeriesBar ("New Zealand", softStiple, 82),
					new BarSeriesBar ("Malta", !verticalBars?mediumStiple:softStiple, 81.9f),
					new BarSeriesBar ("Ireland", softStiple, 81.8f)
				}
			};

			graphView.Series.Add (barSeries);

			if (verticalBars) {

				barSeries.Orientation = Orientation.Vertical;

				// How much graph space each cell of the console depicts
				graphView.CellSize = new PointF (0.1f, 0.25f);
				// No axis marks since Bar will add it's own categorical marks
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

			} else {
				barSeries.Orientation = Orientation.Horizontal;

				// How much graph space each cell of the console depicts
				graphView.CellSize = new PointF (0.1f, 1f);
				// No axis marks since Bar will add it's own categorical marks
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

			graphView.SetNeedsDisplay ();
		}

		private void SetupPopulationPyramid ()
		{
			/*
			Age,M,F
0-4,2009363,1915127
5-9,2108550,2011016
10-14,2022370,1933970
15-19,1880611,1805522
20-24,2072674,2001966
25-29,2275138,2208929
30-34,2361054,2345774
35-39,2279836,2308360
40-44,2148253,2159877
45-49,2128343,2167778
50-54,2281421,2353119
55-59,2232388,2306537
60-64,1919839,1985177
65-69,1647391,1734370
70-74,1624635,1763853
75-79,1137438,1304709
80-84,766956,969611
85-89,438663,638892
90-94,169952,320625
95-99,34524,95559
100+,3016,12818*/

			about.Text = "This graph shows population of each age divided by gender";

			graphView.Reset ();

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointF (100_000, 1);

			//center the x axis in middle of screen to show both sides
			graphView.ScrollOffset = new PointF (-3_000_000, 0);

			graphView.AxisX.Text = "Number Of People";
			graphView.AxisX.Increment = 500_000;
			graphView.AxisX.ShowLabelsEvery = 2;

			// use Abs to make negative axis labels positive
			graphView.AxisX.LabelGetter = (v) => Math.Abs (v.Value / 1_000_000).ToString ("N2") + "M";

			// leave space for axis labels
			graphView.MarginBottom = 2;
			graphView.MarginLeft = 1;

			// do not show axis titles (bars have their own categories)
			graphView.AxisY.Increment = 0;
			graphView.AxisY.ShowLabelsEvery = 0;
			graphView.AxisY.Minimum = 0;

			var stiple = new GraphCellToRender (CM.Glyphs.Stipple);

			// Bars in 2 directions

			// Males (negative to make the bars go left)
			var malesSeries = new BarSeries () {
				Orientation = Orientation.Horizontal,
				Bars = new List<BarSeriesBar> ()
				{
					new BarSeriesBar("0-4",stiple,-2009363),
					new BarSeriesBar("5-9",stiple,-2108550),
					new BarSeriesBar("10-14",stiple,-2022370),
					new BarSeriesBar("15-19",stiple,-1880611),
					new BarSeriesBar("20-24",stiple,-2072674),
					new BarSeriesBar("25-29",stiple,-2275138),
					new BarSeriesBar("30-34",stiple,-2361054),
					new BarSeriesBar("35-39",stiple,-2279836),
					new BarSeriesBar("40-44",stiple,-2148253),
					new BarSeriesBar("45-49",stiple,-2128343),
					new BarSeriesBar("50-54",stiple,-2281421),
					new BarSeriesBar("55-59",stiple,-2232388),
					new BarSeriesBar("60-64",stiple,-1919839),
					new BarSeriesBar("65-69",stiple,-1647391),
					new BarSeriesBar("70-74",stiple,-1624635),
					new BarSeriesBar("75-79",stiple,-1137438),
					new BarSeriesBar("80-84",stiple,-766956),
					new BarSeriesBar("85-89",stiple,-438663),
					new BarSeriesBar("90-94",stiple,-169952),
					new BarSeriesBar("95-99",stiple,-34524),
					new BarSeriesBar("100+",stiple,-3016)

				}
			};
			graphView.Series.Add (malesSeries);

			// Females
			var femalesSeries = new BarSeries () {
				Orientation = Orientation.Horizontal,
				Bars = new List<BarSeriesBar> ()
				{
					new BarSeriesBar("0-4",stiple,1915127),
					new BarSeriesBar("5-9",stiple,2011016),
					new BarSeriesBar("10-14",stiple,1933970),
					new BarSeriesBar("15-19",stiple,1805522),
					new BarSeriesBar("20-24",stiple,2001966),
					new BarSeriesBar("25-29",stiple,2208929),
					new BarSeriesBar("30-34",stiple,2345774),
					new BarSeriesBar("35-39",stiple,2308360),
					new BarSeriesBar("40-44",stiple,2159877),
					new BarSeriesBar("45-49",stiple,2167778),
					new BarSeriesBar("50-54",stiple,2353119),
					new BarSeriesBar("55-59",stiple,2306537),
					new BarSeriesBar("60-64",stiple,1985177),
					new BarSeriesBar("65-69",stiple,1734370),
					new BarSeriesBar("70-74",stiple,1763853),
					new BarSeriesBar("75-79",stiple,1304709),
					new BarSeriesBar("80-84",stiple,969611),
					new BarSeriesBar("85-89",stiple,638892),
					new BarSeriesBar("90-94",stiple,320625),
					new BarSeriesBar("95-99",stiple,95559),
					new BarSeriesBar("100+",stiple,12818)
				}
			};

			var softStiple = new GraphCellToRender ((Rune)'\u2591');
			var mediumStiple = new GraphCellToRender ((Rune)'\u2592');

			for (int i = 0; i < malesSeries.Bars.Count; i++) {
				malesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
				femalesSeries.Bars [i].Fill = i % 2 == 0 ? softStiple : mediumStiple;
			}

			graphView.Series.Add (femalesSeries);

			graphView.Annotations.Add (new TextAnnotation () { Text = "M", ScreenPosition = new Terminal.Gui.Point (0, 10) });
			graphView.Annotations.Add (new TextAnnotation () { Text = "F", ScreenPosition = new Terminal.Gui.Point (graphView.Bounds.Width - 1, 10) });

			graphView.SetNeedsDisplay ();

		}

		class DiscoBarSeries : BarSeries {
			private Terminal.Gui.Attribute green;
			private Terminal.Gui.Attribute brightgreen;
			private Terminal.Gui.Attribute brightyellow;
			private Terminal.Gui.Attribute red;
			private Terminal.Gui.Attribute brightred;

			public DiscoBarSeries ()
			{

				green = Application.Driver.MakeAttribute (Color.BrightGreen, Color.Black);
				brightgreen = Application.Driver.MakeAttribute (Color.Green, Color.Black);
				brightyellow = Application.Driver.MakeAttribute (Color.BrightYellow, Color.Black);
				red = Application.Driver.MakeAttribute (Color.Red, Color.Black);
				brightred = Application.Driver.MakeAttribute (Color.BrightRed, Color.Black);
			}
			protected override void DrawBarLine (GraphView graph, Terminal.Gui.Point start, Terminal.Gui.Point end, BarSeriesBar beingDrawn)
			{
				var driver = Application.Driver;

				int x = start.X;
				for (int y = end.Y; y <= start.Y; y++) {

					var height = graph.ScreenToGraphSpace (x, y).Y;

					if (height >= 85) {
						driver.SetAttribute (red);
					} else if (height >= 66) {
						driver.SetAttribute (brightred);
					} else if (height >= 45) {
						driver.SetAttribute (brightyellow);
					} else if (height >= 25) {
						driver.SetAttribute (brightgreen);
					} else {
						driver.SetAttribute (green);
					}

					graph.AddRune (x, y, beingDrawn.Fill.Rune);
				}
			}
		}

		private void SetupDisco ()
		{
			graphView.Reset ();

			about.Text = "This graph shows a graphic equaliser for an imaginary song";

			graphView.GraphColor = Application.Driver.MakeAttribute (Color.White, Color.Black);

			var stiple = new GraphCellToRender ((Rune)'\u2593');

			Random r = new Random ();
			var series = new DiscoBarSeries ();
			var bars = new List<BarSeriesBar> ();

			Func<MainLoop, bool> genSample = (l) => {

				bars.Clear ();
				// generate an imaginary sample
				for (int i = 0; i < 31; i++) {
					bars.Add (
						new BarSeriesBar (null, stiple, r.Next (0, 100)) {
							//ColorGetter = colorDelegate
						});
				}
				graphView.SetNeedsDisplay ();

				// while the equaliser is showing
				return graphView.Series.Contains (series);
			};

			Application.MainLoop.AddTimeout (TimeSpan.FromMilliseconds (250), genSample);

			series.Bars = bars;

			graphView.Series.Add (series);

			// How much graph space each cell of the console depicts
			graphView.CellSize = new PointF (1, 10);
			graphView.AxisX.Increment = 0; // No graph ticks
			graphView.AxisX.ShowLabelsEvery = 0; // no labels

			graphView.AxisX.Visible = false;
			graphView.AxisY.Visible = false;

			graphView.SetNeedsDisplay ();
		}
		private void SetupPeriodicTableScatterPlot ()
		{
			graphView.Reset ();

			about.Text = "This graph shows the atomic weight of each element in the periodic table.\nStarting with Hydrogen (atomic Number 1 with a weight of 1.007)";

			//AtomicNumber and AtomicMass of all elements in the periodic table
			graphView.Series.Add (
				new ScatterSeries () {
					Points = new List<PointF>{
						new PointF(1,1.007f),new PointF(2,4.002f),new PointF(3,6.941f),new PointF(4,9.012f),new PointF(5,10.811f),new PointF(6,12.011f),
						new PointF(7,14.007f),new PointF(8,15.999f),new PointF(9,18.998f),new PointF(10,20.18f),new PointF(11,22.99f),new PointF(12,24.305f),
						new PointF(13,26.982f),new PointF(14,28.086f),new PointF(15,30.974f),new PointF(16,32.065f),new PointF(17,35.453f),new PointF(18,39.948f),
						new PointF(19,39.098f),new PointF(20,40.078f),new PointF(21,44.956f),new PointF(22,47.867f),new PointF(23,50.942f),new PointF(24,51.996f),
						new PointF(25,54.938f),new PointF(26,55.845f),new PointF(27,58.933f),new PointF(28,58.693f),new PointF(29,63.546f),new PointF(30,65.38f),
						new PointF(31,69.723f),new PointF(32,72.64f),new PointF(33,74.922f),new PointF(34,78.96f),new PointF(35,79.904f),new PointF(36,83.798f),
						new PointF(37,85.468f),new PointF(38,87.62f),new PointF(39,88.906f),new PointF(40,91.224f),new PointF(41,92.906f),new PointF(42,95.96f),
						new PointF(43,98f),new PointF(44,101.07f),new PointF(45,102.906f),new PointF(46,106.42f),new PointF(47,107.868f),new PointF(48,112.411f),
						new PointF(49,114.818f),new PointF(50,118.71f),new PointF(51,121.76f),new PointF(52,127.6f),new PointF(53,126.904f),new PointF(54,131.293f),
						new PointF(55,132.905f),new PointF(56,137.327f),new PointF(57,138.905f),new PointF(58,140.116f),new PointF(59,140.908f),new PointF(60,144.242f),
						new PointF(61,145),new PointF(62,150.36f),new PointF(63,151.964f),new PointF(64,157.25f),new PointF(65,158.925f),new PointF(66,162.5f),
						new PointF(67,164.93f),new PointF(68,167.259f),new PointF(69,168.934f),new PointF(70,173.054f),new PointF(71,174.967f),new PointF(72,178.49f),
						new PointF(73,180.948f),new PointF(74,183.84f),new PointF(75,186.207f),new PointF(76,190.23f),new PointF(77,192.217f),new PointF(78,195.084f),
						new PointF(79,196.967f),new PointF(80,200.59f),new PointF(81,204.383f),new PointF(82,207.2f),new PointF(83,208.98f),new PointF(84,210),
						new PointF(85,210),new PointF(86,222),new PointF(87,223),new PointF(88,226),new PointF(89,227),new PointF(90,232.038f),new PointF(91,231.036f),
						new PointF(92,238.029f),new PointF(93,237),new PointF(94,244),new PointF(95,243),new PointF(96,247),new PointF(97,247),new PointF(98,251),
						new PointF(99,252),new PointF(100,257),new PointF(101,258),new PointF(102,259),new PointF(103,262),new PointF(104,261),new PointF(105,262),
						new PointF(106,266),new PointF(107,264),new PointF(108,267),new PointF(109,268),new PointF(113,284),new PointF(114,289),new PointF(115,288),
						new PointF(116,292),new PointF(117,295),new PointF(118,294)
			}
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

			graphView.SetNeedsDisplay ();
		}

		private void Zoom (float factor)
		{
			graphView.CellSize = new PointF (
				graphView.CellSize.X * factor,
				graphView.CellSize.Y * factor
			);

			graphView.AxisX.Increment *= factor;
			graphView.AxisY.Increment *= factor;

			graphView.SetNeedsDisplay ();
		}
		private void Margin (bool left, bool increase)
		{
			if (left) {
				graphView.MarginLeft = (uint)Math.Max (0, graphView.MarginLeft + (increase ? 1 : -1));
			} else {
				graphView.MarginBottom = (uint)Math.Max (0, graphView.MarginBottom + (increase ? 1 : -1));
			}

			graphView.SetNeedsDisplay ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
