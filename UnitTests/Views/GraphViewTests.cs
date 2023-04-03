using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Terminal.Gui.Graphs;
using Point = Terminal.Gui.Point;
using Attribute = Terminal.Gui.Attribute;
using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using Rune = System.Rune;

namespace Terminal.Gui.ViewTests {

	#region Helper Classes
	class FakeHAxis : HorizontalAxis {

		public List<Point> DrawAxisLinePoints = new List<Point> ();
		public List<int> LabelPoints = new List<int> ();

		protected override void DrawAxisLine (GraphView graph, int x, int y)
		{
			base.DrawAxisLine (graph, x, y);
			DrawAxisLinePoints.Add (new Point (x, y));
		}

		public override void DrawAxisLabel (GraphView graph, int screenPosition, string text)
		{
			base.DrawAxisLabel (graph, screenPosition, text);
			LabelPoints.Add (screenPosition);
		}
	}

	class FakeVAxis : VerticalAxis {

		public List<Point> DrawAxisLinePoints = new List<Point> ();
		public List<int> LabelPoints = new List<int> ();

		protected override void DrawAxisLine (GraphView graph, int x, int y)
		{
			base.DrawAxisLine (graph, x, y);
			DrawAxisLinePoints.Add (new Point (x, y));
		}
		public override void DrawAxisLabel (GraphView graph, int screenPosition, string text)
		{
			base.DrawAxisLabel (graph, screenPosition, text);
			LabelPoints.Add (screenPosition);
		}
	}
	#endregion

	public class GraphViewTests {

		static string LastInitFakeDriver;

		public static FakeDriver InitFakeDriver ()
		{
			var driver = new FakeDriver ();
			try {
				Application.Init (driver);
			} catch (InvalidOperationException) {

				// close it so that we don't get a thousand of these errors in a row
				Application.Shutdown ();

				// but still report a failure and name the test that didn't shut down.  Note
				// that the test that didn't shutdown won't be the one currently running it will
				// be the last one
				throw new Exception ("A test did not call shutdown correctly.  Test stack trace was:" + LastInitFakeDriver);
			}
			
			driver.Init (() => { });

			LastInitFakeDriver = Environment.StackTrace;
			return driver;
		}

		/// <summary>
		/// Returns a basic very small graph (10 x 5)
		/// </summary>
		/// <returns></returns>
		public static GraphView GetGraph ()
		{
			GraphViewTests.InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.MarginBottom = 1;
			gv.MarginLeft = 1;
			gv.Bounds = new Rect (0, 0, 10, 5);

			return gv;
		}

		#region Screen to Graph Tests

		[Fact]
		public void ScreenToGraphSpace_DefaultCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 9);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);


			// up 2 rows of the console and along 1 col
			var up2along1 = gv.ScreenToGraphSpace (1, 7);
			Assert.Equal (1, up2along1.X);
			Assert.Equal (2, up2along1.Y);
		}
		[Fact]
		public void ScreenToGraphSpace_DefaultCellSize_WithMargin ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.ScreenToGraphSpace (0, 9);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);

			gv.MarginLeft = 1;

			botLeft = gv.ScreenToGraphSpace (0, 9);
			// Origin should be at 1,9 now to leave a margin of 1
			// so screen position 0,9 would be data space -1,0
			Assert.Equal (-1, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);

			gv.MarginLeft = 1;
			gv.MarginBottom = 1;

			botLeft = gv.ScreenToGraphSpace (0, 9);
			// Origin should be at 1,0 (to leave a margin of 1 in both sides)
			// so screen position 0,9 would be data space -1,-1
			Assert.Equal (-1, botLeft.X);
			Assert.Equal (-1, botLeft.Y);
			Assert.Equal (1, botLeft.Width);
			Assert.Equal (1, botLeft.Height);
		}
		[Fact]
		public void ScreenToGraphSpace_CustomCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen measures 5 units in graph data model vertically and 1/4 horizontally
			gv.CellSize = new PointF (0.25f, 5);

			// origin should be bottom left 
			// (note that y=10 is actually overspilling the control, the last row is 9)
			var botLeft = gv.ScreenToGraphSpace (0, 9);
			Assert.Equal (0, botLeft.X);
			Assert.Equal (0, botLeft.Y);
			Assert.Equal (0.25f, botLeft.Width);
			Assert.Equal (5, botLeft.Height);

			// up 2 rows of the console and along 1 col
			var up2along1 = gv.ScreenToGraphSpace (1, 7);
			Assert.Equal (0.25f, up2along1.X);
			Assert.Equal (10, up2along1.Y);
			Assert.Equal (0.25f, botLeft.Width);
			Assert.Equal (5, botLeft.Height);
		}

		#endregion

		#region Graph to Screen Tests

		[Fact]
		public void GraphSpaceToScreen_DefaultCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (2, along2up1.X);
			Assert.Equal (8, along2up1.Y);
		}

		[Fact]
		public void GraphSpaceToScreen_DefaultCellSize_WithMargin ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (0, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left

			gv.MarginLeft = 1;

			// With a margin of 1 the origin should be at x=1 y= 9
			botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (1, botLeft.X);
			Assert.Equal (9, botLeft.Y); // row 9 of the view is the bottom left

			gv.MarginLeft = 1;
			gv.MarginBottom = 1;

			// With a margin of 1 in both directions the origin should be at x=1 y= 9
			botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (1, botLeft.X);
			Assert.Equal (8, botLeft.Y); // row 8 of the view is the bottom left up 1 cell
		}

		[Fact]
		public void GraphSpaceToScreen_ScrollOffset ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			//graph is scrolled to present chart space -5 to 5 in both axes
			gv.ScrollOffset = new PointF (-5, -5);

			// origin should be right in the middle of the control
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (5, botLeft.X);
			Assert.Equal (4, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (7, along2up1.X);
			Assert.Equal (3, along2up1.Y);
		}
		[Fact]
		public void GraphSpaceToScreen_CustomCellSize ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen is responsible for rendering 5 units in graph data model
			// vertically and 1/4 horizontally
			gv.CellSize = new PointF (0.25f, 5);

			// origin should be bottom left
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (0, botLeft.X);
			// row 9 of the view is the bottom left (height is 10 so 0,1,2,3..9)
			Assert.Equal (9, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (8, along2up1.X);
			Assert.Equal (9, along2up1.Y);

			// Y value 4 should be rendered in bottom most row
			Assert.Equal (9, gv.GraphSpaceToScreen (new PointF (2, 4)).Y);

			// Cell height is 5 so this is the first point of graph space that should
			// be rendered in the graph in next row up (row 9)
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 5)).Y);

			// More boundary testing for this cell size
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 6)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 7)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 8)).Y);
			Assert.Equal (8, gv.GraphSpaceToScreen (new PointF (2, 9)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointF (2, 10)).Y);
			Assert.Equal (7, gv.GraphSpaceToScreen (new PointF (2, 11)).Y);
		}


		[Fact]
		public void GraphSpaceToScreen_CustomCellSize_WithScrollOffset ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 20, 10);

			// Each cell of screen is responsible for rendering 5 units in graph data model
			// vertically and 1/4 horizontally
			gv.CellSize = new PointF (0.25f, 5);

			//graph is scrolled to present some negative chart (4 negative cols and 2 negative rows)
			gv.ScrollOffset = new PointF (-1, -10);

			// origin should be in the lower left (but not right at the bottom)
			var botLeft = gv.GraphSpaceToScreen (new PointF (0, 0));
			Assert.Equal (4, botLeft.X);
			Assert.Equal (7, botLeft.Y);

			// along 2 and up 1 in graph space
			var along2up1 = gv.GraphSpaceToScreen (new PointF (2, 1));
			Assert.Equal (12, along2up1.X);
			Assert.Equal (7, along2up1.Y);


			// More boundary testing for this cell size/offset
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointF (2, 6)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointF (2, 7)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointF (2, 8)).Y);
			Assert.Equal (6, gv.GraphSpaceToScreen (new PointF (2, 9)).Y);
			Assert.Equal (5, gv.GraphSpaceToScreen (new PointF (2, 10)).Y);
			Assert.Equal (5, gv.GraphSpaceToScreen (new PointF (2, 11)).Y);
		}

		#endregion


		/// <summary>
		/// A cell size of 0 would result in mapping all graph space into the
		/// same cell of the console.  Since <see cref="GraphView.CellSize"/>
		/// is mutable a sensible place to check this is in redraw.
		/// </summary>
		[Fact]
		public void CellSizeZero ()
		{
			InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);
			gv.Series.Add (new ScatterSeries () { Points = new List<PointF> { new PointF (1, 1) } });
			gv.CellSize = new PointF (0, 5);
			var ex = Assert.Throws<Exception> (() => gv.Redraw (gv.Bounds));

			Assert.Equal ("CellSize cannot be 0", ex.Message);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}



		/// <summary>
		/// Tests that each point in the screen space maps to a rectangle of
		/// (float) graph space and that each corner of that rectangle of graph
		/// space maps back to the same row/col of the graph that was fed in
		/// </summary>
		[Fact]
		public void TestReversing_ScreenToGraphSpace ()
		{
			var gv = new GraphView ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			// How much graph space each cell of the console depicts
			gv.CellSize = new PointF (0.1f, 0.25f);
			gv.AxisX.Increment = 1;
			gv.AxisX.ShowLabelsEvery = 1;

			gv.AxisY.Increment = 1;
			gv.AxisY.ShowLabelsEvery = 1;

			// Start the graph at 80
			gv.ScrollOffset = new PointF (0, 80);

			for (int x = 0; x < gv.Bounds.Width; x++) {
				for (int y = 0; y < gv.Bounds.Height; y++) {

					var graphSpace = gv.ScreenToGraphSpace (x, y);

					// See 
					// https://en.wikipedia.org/wiki/Machine_epsilon
					float epsilon = 0.0001f;

					var p = gv.GraphSpaceToScreen (new PointF (graphSpace.Left + epsilon, graphSpace.Top + epsilon));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointF (graphSpace.Right - epsilon, graphSpace.Top + epsilon));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointF (graphSpace.Left + epsilon, graphSpace.Bottom - epsilon));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

					p = gv.GraphSpaceToScreen (new PointF (graphSpace.Right - epsilon, graphSpace.Bottom - epsilon));
					Assert.Equal (x, p.X);
					Assert.Equal (y, p.Y);

				}
			}
		}
	}

	public class SeriesTests {

		[Fact]
		public void Series_GetsPassedCorrectBounds_AllAtOnce ()
		{
			GraphViewTests.InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			RectangleF fullGraphBounds = RectangleF.Empty;
			Rect graphScreenBounds = Rect.Empty;

			var series = new FakeSeries ((v, s, g) => { graphScreenBounds = s; fullGraphBounds = g; });
			gv.Series.Add (series);


			gv.Redraw (gv.Bounds);
			Assert.Equal (new RectangleF (0, 0, 50, 30), fullGraphBounds);
			Assert.Equal (new Rect (0, 0, 50, 30), graphScreenBounds);

			// Now we put a margin in
			// Graph should not spill into the margins

			gv.MarginBottom = 2;
			gv.MarginLeft = 5;

			// Even with a margin the graph should be drawn from 
			// the origin, we just get less visible width/height
			gv.Redraw (gv.Bounds);
			Assert.Equal (new RectangleF (0, 0, 45, 28), fullGraphBounds);

			// The screen space the graph will be rendered into should
			// not overspill the margins
			Assert.Equal (new Rect (5, 0, 45, 28), graphScreenBounds);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		/// <summary>
		/// Tests that the bounds passed to the ISeries for drawing into are 
		/// correct even when the <see cref="GraphView.CellSize"/> results in
		/// multiple units of graph space being condensed into each cell of
		/// console
		/// </summary>
		[Fact]
		public void Series_GetsPassedCorrectBounds_AllAtOnce_LargeCellSize ()
		{
			GraphViewTests.InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);

			// the larger the cell size the more condensed (smaller) the graph space is
			gv.CellSize = new PointF (2, 5);

			RectangleF fullGraphBounds = RectangleF.Empty;
			Rect graphScreenBounds = Rect.Empty;

			var series = new FakeSeries ((v, s, g) => { graphScreenBounds = s; fullGraphBounds = g; });

			gv.Series.Add (series);

			gv.Redraw (gv.Bounds);
			// Since each cell of the console is 2x5 of graph space the graph
			// bounds to be rendered are larger
			Assert.Equal (new RectangleF (0, 0, 100, 150), fullGraphBounds);
			Assert.Equal (new Rect (0, 0, 50, 30), graphScreenBounds);

			// Graph should not spill into the margins

			gv.MarginBottom = 2;
			gv.MarginLeft = 5;

			// Even with a margin the graph should be drawn from 
			// the origin, we just get less visible width/height
			gv.Redraw (gv.Bounds);
			Assert.Equal (new RectangleF (0, 0, 90, 140), fullGraphBounds);

			// The screen space the graph will be rendered into should
			// not overspill the margins
			Assert.Equal (new Rect (5, 0, 45, 28), graphScreenBounds);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		private class FakeSeries : ISeries {

			readonly Action<GraphView, Rect, RectangleF> drawSeries;

			public FakeSeries (
				Action<GraphView, Rect, RectangleF> drawSeries
				)
			{
				this.drawSeries = drawSeries;
			}

			public void DrawSeries (GraphView graph, Rect bounds, RectangleF graphBounds)
			{
				drawSeries (graph, bounds, graphBounds);
			}
		}
	}

	public class MultiBarSeriesTests {

		readonly ITestOutputHelper output;

		public MultiBarSeriesTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void MultiBarSeries_BarSpacing ()
		{

			// Creates clusters of 5 adjacent bars with 2 spaces between clusters
			var series = new MultiBarSeries (5, 7, 1);

			Assert.Equal (5, series.SubSeries.Count);

			Assert.Equal (0, series.SubSeries.ElementAt (0).Offset);
			Assert.Equal (1, series.SubSeries.ElementAt (1).Offset);
			Assert.Equal (2, series.SubSeries.ElementAt (2).Offset);
			Assert.Equal (3, series.SubSeries.ElementAt (3).Offset);
			Assert.Equal (4, series.SubSeries.ElementAt (4).Offset);
		}


		[Fact]
		public void MultiBarSeriesColors_WrongNumber ()
		{

			var fake = new FakeDriver ();

			var colors = new []{
				fake.MakeAttribute(Color.Green,Color.Black)
			};

			// user passes 1 color only but asks for 5 bars
			var ex = Assert.Throws<ArgumentException> (() => new MultiBarSeries (5, 7, 1, colors));
			Assert.Equal ("Number of colors must match the number of bars (Parameter 'numberOfBarsPerCategory')", ex.Message);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void MultiBarSeriesColors_RightNumber ()
		{

			var fake = new FakeDriver ();

			var colors = new []{
				fake.MakeAttribute(Color.Green,Color.Black),
				fake.MakeAttribute(Color.Green,Color.White),
				fake.MakeAttribute(Color.BrightYellow,Color.White)
			};

			// user passes 3 colors and asks for 3 bars
			var series = new MultiBarSeries (3, 7, 1, colors);

			Assert.Equal (series.SubSeries.ElementAt (0).OverrideBarColor, colors [0]);
			Assert.Equal (series.SubSeries.ElementAt (1).OverrideBarColor, colors [1]);
			Assert.Equal (series.SubSeries.ElementAt (2).OverrideBarColor, colors [2]);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void MultiBarSeriesAddValues_WrongNumber ()
		{

			// user asks for 3 bars per category
			var series = new MultiBarSeries (3, 7, 1);

			var ex = Assert.Throws<ArgumentException> (() => series.AddBars ("Cars", '#', 1));

			Assert.Equal ("Number of values must match the number of bars per category (Parameter 'values')", ex.Message);
		}



		[Fact]
		public void TestRendering_MultibarSeries ()
		{

			GraphViewTests.InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();

			// y axis goes from 0.1 to 1 across 10 console rows
			// x axis goes from 0 to 20 across 20 console columns
			gv.Bounds = new Rect (0, 0, 20, 10);
			gv.CellSize = new PointF (1f, 0.1f);
			gv.MarginBottom = 1;
			gv.MarginLeft = 1;

			var multibarSeries = new MultiBarSeries (2, 4, 1);

			//nudge them left to avoid float rounding errors at the boundaries of cells
			foreach (var sub in multibarSeries.SubSeries) {
				sub.Offset -= 0.001f;
			}

			gv.Series.Add (multibarSeries);

			FakeHAxis fakeXAxis;

			// don't show axis labels that means any labels
			// that appaer are explicitly from the bars
			gv.AxisX = fakeXAxis = new FakeHAxis () { Increment = 0 };
			gv.AxisY = new FakeVAxis () { Increment = 0 };

			gv.Redraw (gv.Bounds);

			// Since bar series has no bars yet no labels should be displayed
			Assert.Empty (fakeXAxis.LabelPoints);

			multibarSeries.AddBars ("hey", 'M', 0.5001f, 0.5001f);
			fakeXAxis.LabelPoints.Clear ();
			gv.Redraw (gv.Bounds);

			Assert.Equal (4, fakeXAxis.LabelPoints.Single ());

			multibarSeries.AddBars ("there", 'M', 0.24999f, 0.74999f);
			multibarSeries.AddBars ("bob", 'M', 1, 2);
			fakeXAxis.LabelPoints.Clear ();
			gv.Redraw (gv.Bounds);

			Assert.Equal (3, fakeXAxis.LabelPoints.Count);
			Assert.Equal (4, fakeXAxis.LabelPoints [0]);
			Assert.Equal (8, fakeXAxis.LabelPoints [1]);
			Assert.Equal (12, fakeXAxis.LabelPoints [2]);

			string looksLike =
@" 
 │          MM
 │       M  MM
 │       M  MM
 │  MM   M  MM
 │  MM   M  MM
 │  MM   M  MM
 │  MM  MM  MM
 │  MM  MM  MM
 ┼──┬M──┬M──┬M──────
   heytherebob  ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}
	}

	public class BarSeriesTests {


		private GraphView GetGraph (out FakeBarSeries series, out FakeHAxis axisX, out FakeVAxis axisY)
		{
			GraphViewTests.InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();

			// y axis goes from 0.1 to 1 across 10 console rows
			// x axis goes from 0 to 10 across 20 console columns
			gv.Bounds = new Rect (0, 0, 20, 10);
			gv.CellSize = new PointF (0.5f, 0.1f);

			gv.Series.Add (series = new FakeBarSeries ());

			// don't show axis labels that means any labels
			// that appaer are explicitly from the bars
			gv.AxisX = axisX = new FakeHAxis () { Increment = 0 };
			gv.AxisY = axisY = new FakeVAxis () { Increment = 0 };

			return gv;
		}

		[Fact]
		public void TestZeroHeightBar_WithName ()
		{

			var graph = GetGraph (out FakeBarSeries barSeries, out FakeHAxis axisX, out FakeVAxis axisY);
			graph.Redraw (graph.Bounds);

			// no bars
			Assert.Empty (barSeries.BarScreenStarts);
			Assert.Empty (axisX.LabelPoints);
			Assert.Empty (axisY.LabelPoints);

			// bar of height 0
			barSeries.Bars.Add (new BarSeries.Bar ("hi", new GraphCellToRender ('.'), 0));
			barSeries.Orientation = Orientation.Vertical;

			// redraw graph
			graph.Redraw (graph.Bounds);

			// bar should not be drawn
			Assert.Empty (barSeries.BarScreenStarts);

			Assert.NotEmpty (axisX.LabelPoints);
			Assert.Empty (axisY.LabelPoints);

			// but bar name should be
			// Screen position x=2 because bars are drawn every 1f of
			// graph space and CellSize.X is 0.5f
			Assert.Contains (2, axisX.LabelPoints);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void TestTwoTallBars_WithOffset ()
		{

			var graph = GetGraph (out FakeBarSeries barSeries, out FakeHAxis axisX, out FakeVAxis axisY);
			graph.Redraw (graph.Bounds);

			// no bars
			Assert.Empty (barSeries.BarScreenStarts);
			Assert.Empty (axisX.LabelPoints);
			Assert.Empty (axisY.LabelPoints);

			// 0.5 units of graph fit every screen cell
			// so 1 unit of graph space is 2 screen columns
			graph.CellSize = new PointF (0.5f, 0.1f);

			// Start bar 1 screen unit along
			barSeries.Offset = 0.5f;
			barSeries.BarEvery = 1f;

			barSeries.Bars.Add (
				new BarSeries.Bar ("hi1", new GraphCellToRender ('.'), 100));
			barSeries.Bars.Add (
				new BarSeries.Bar ("hi2", new GraphCellToRender ('.'), 100));

			barSeries.Orientation = Orientation.Vertical;

			// redraw graph
			graph.Redraw (graph.Bounds);

			// bar should be drawn at BarEvery 1f + offset 0.5f = 3 screen units
			Assert.Equal (3, barSeries.BarScreenStarts [0].X);
			Assert.Equal (3, barSeries.BarScreenEnds [0].X);

			// second bar should be BarEveryx2 = 2f + offset 0.5f = 5 screen units
			Assert.Equal (5, barSeries.BarScreenStarts [1].X);
			Assert.Equal (5, barSeries.BarScreenEnds [1].X);

			// both bars should have labels
			Assert.Equal (2, axisX.LabelPoints.Count);
			Assert.Contains (3, axisX.LabelPoints);
			Assert.Contains (5, axisX.LabelPoints);

			// bars are very tall but should not draw up off top of screen
			Assert.Equal (9, barSeries.BarScreenStarts [0].Y);
			Assert.Equal (0, barSeries.BarScreenEnds [0].Y);
			Assert.Equal (9, barSeries.BarScreenStarts [1].Y);
			Assert.Equal (0, barSeries.BarScreenEnds [1].Y);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TestOneLongOneShortHorizontalBars_WithOffset ()
		{

			var graph = GetGraph (out FakeBarSeries barSeries, out FakeHAxis axisX, out FakeVAxis axisY);
			graph.Redraw (graph.Bounds);

			// no bars
			Assert.Empty (barSeries.BarScreenStarts);
			Assert.Empty (axisX.LabelPoints);
			Assert.Empty (axisY.LabelPoints);

			// 0.1 units of graph y fit every screen row
			// so 1 unit of graph y space is 10 screen rows
			graph.CellSize = new PointF (0.5f, 0.1f);

			// Start bar 3 screen units up (y = height-3)
			barSeries.Offset = 0.25f;
			// 1 bar every 3 rows of screen
			barSeries.BarEvery = 0.3f;
			barSeries.Orientation = Orientation.Horizontal;

			// 1 bar that is very wide (100 graph units horizontally = screen pos 50 but bounded by screen)
			barSeries.Bars.Add (
				new BarSeries.Bar ("hi1", new GraphCellToRender ('.'), 100));

			// 1 bar that is shorter
			barSeries.Bars.Add (
				new BarSeries.Bar ("hi2", new GraphCellToRender ('.'), 5));

			// redraw graph
			graph.Redraw (graph.Bounds);

			// since bars are horizontal all have the same X start cordinates
			Assert.Equal (0, barSeries.BarScreenStarts [0].X);
			Assert.Equal (0, barSeries.BarScreenStarts [1].X);

			// bar goes all the way to the end so bumps up against right screen boundary
			// width of graph is 20
			Assert.Equal (19, barSeries.BarScreenEnds [0].X);

			// shorter bar is 5 graph units wide which is 10 screen units
			Assert.Equal (10, barSeries.BarScreenEnds [1].X);

			// first  bar should be offset 6 screen units (0.25f + 0.3f graph units)
			// since height of control is 10 then first bar should be at screen row 4 (10-6)
			Assert.Equal (4, barSeries.BarScreenStarts [0].Y);

			// second  bar should be offset 9 screen units (0.25f + 0.6f graph units)
			// since height of control is 10 then second bar should be at screen row 1 (10-9)
			Assert.Equal (1, barSeries.BarScreenStarts [1].Y);

			// both bars should have labels but on the y axis
			Assert.Equal (2, axisY.LabelPoints.Count);
			Assert.Empty (axisX.LabelPoints);

			// labels should align with the bars (same screen y axis point)
			Assert.Contains (4, axisY.LabelPoints);
			Assert.Contains (1, axisY.LabelPoints);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		private class FakeBarSeries : BarSeries {
			public GraphCellToRender FinalColor { get; private set; }

			public List<Point> BarScreenStarts { get; private set; } = new List<Point> ();
			public List<Point> BarScreenEnds { get; private set; } = new List<Point> ();

			protected override GraphCellToRender AdjustColor (GraphCellToRender graphCellToRender)
			{
				return FinalColor = base.AdjustColor (graphCellToRender);
			}

			protected override void DrawBarLine (GraphView graph, Point start, Point end, Bar beingDrawn)
			{
				base.DrawBarLine (graph, start, end, beingDrawn);

				BarScreenStarts.Add (start);
				BarScreenEnds.Add (end);
			}

		}
	}


	public class AxisTests {


		private GraphView GetGraph (out FakeHAxis axis)
		{
			return GetGraph (out axis, out _);
		}
		private GraphView GetGraph (out FakeVAxis axis)
		{
			return GetGraph (out _, out axis);
		}
		private GraphView GetGraph (out FakeHAxis axisX, out FakeVAxis axisY)
		{
			GraphViewTests.InitFakeDriver ();

			var gv = new GraphView ();
			gv.ColorScheme = new ColorScheme ();
			gv.Bounds = new Rect (0, 0, 50, 30);
			// graph can't be completely empty or it won't draw
			gv.Series.Add (new ScatterSeries ());

			axisX = new FakeHAxis ();
			axisY = new FakeVAxis ();
			gv.AxisX = axisX;
			gv.AxisY = axisY;

			return gv;
		}

		#region HorizontalAxis Tests

		/// <summary>
		/// Tests that the horizontal axis is computed correctly and does not over spill
		/// it's bounds
		/// </summary>
		[Fact]
		public void TestHAxisLocation_NoMargin ()
		{
			var gv = GetGraph (out FakeHAxis axis);

			gv.Redraw (gv.Bounds);

			Assert.DoesNotContain (new Point (-1, 29), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 29), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (1, 29), axis.DrawAxisLinePoints);

			Assert.Contains (new Point (48, 29), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (49, 29), axis.DrawAxisLinePoints);
			Assert.DoesNotContain (new Point (50, 29), axis.DrawAxisLinePoints);

			Assert.InRange (axis.LabelPoints.Max (), 0, 49);
			Assert.InRange (axis.LabelPoints.Min (), 0, 49);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TestHAxisLocation_MarginBottom ()
		{
			var gv = GetGraph (out FakeHAxis axis);

			gv.MarginBottom = 10;
			gv.Redraw (gv.Bounds);

			Assert.DoesNotContain (new Point (-1, 19), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 19), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (1, 19), axis.DrawAxisLinePoints);

			Assert.Contains (new Point (48, 19), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (49, 19), axis.DrawAxisLinePoints);
			Assert.DoesNotContain (new Point (50, 19), axis.DrawAxisLinePoints);

			Assert.InRange (axis.LabelPoints.Max (), 0, 49);
			Assert.InRange (axis.LabelPoints.Min (), 0, 49);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TestHAxisLocation_MarginLeft ()
		{
			var gv = GetGraph (out FakeHAxis axis);

			gv.MarginLeft = 5;
			gv.Redraw (gv.Bounds);

			Assert.DoesNotContain (new Point (4, 29), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (5, 29), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (6, 29), axis.DrawAxisLinePoints);

			Assert.Contains (new Point (48, 29), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (49, 29), axis.DrawAxisLinePoints);
			Assert.DoesNotContain (new Point (50, 29), axis.DrawAxisLinePoints);

			// Axis lables should not be drawn in the margin
			Assert.InRange (axis.LabelPoints.Max (), 5, 49);
			Assert.InRange (axis.LabelPoints.Min (), 5, 49);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		#endregion

		#region VerticalAxisTests


		/// <summary>
		/// Tests that the horizontal axis is computed correctly and does not over spill
		/// it's bounds
		/// </summary>
		[Fact]
		public void TestVAxisLocation_NoMargin ()
		{
			var gv = GetGraph (out FakeVAxis axis);

			gv.Redraw (gv.Bounds);

			Assert.DoesNotContain (new Point (0, -1), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 1), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 2), axis.DrawAxisLinePoints);

			Assert.Contains (new Point (0, 28), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 29), axis.DrawAxisLinePoints);
			Assert.DoesNotContain (new Point (0, 30), axis.DrawAxisLinePoints);

			Assert.InRange (axis.LabelPoints.Max (), 0, 29);
			Assert.InRange (axis.LabelPoints.Min (), 0, 29);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TestVAxisLocation_MarginBottom ()
		{
			var gv = GetGraph (out FakeVAxis axis);

			gv.MarginBottom = 10;
			gv.Redraw (gv.Bounds);

			Assert.DoesNotContain (new Point (0, -1), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 1), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 2), axis.DrawAxisLinePoints);

			Assert.Contains (new Point (0, 18), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (0, 19), axis.DrawAxisLinePoints);
			Assert.DoesNotContain (new Point (0, 20), axis.DrawAxisLinePoints);

			// Labels should not be drawn into the axis
			Assert.InRange (axis.LabelPoints.Max (), 0, 19);
			Assert.InRange (axis.LabelPoints.Min (), 0, 19);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TestVAxisLocation_MarginLeft ()
		{
			var gv = GetGraph (out FakeVAxis axis);

			gv.MarginLeft = 5;
			gv.Redraw (gv.Bounds);

			Assert.DoesNotContain (new Point (5, -1), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (5, 1), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (5, 2), axis.DrawAxisLinePoints);

			Assert.Contains (new Point (5, 28), axis.DrawAxisLinePoints);
			Assert.Contains (new Point (5, 29), axis.DrawAxisLinePoints);
			Assert.DoesNotContain (new Point (5, 30), axis.DrawAxisLinePoints);

			Assert.InRange (axis.LabelPoints.Max (), 0, 29);
			Assert.InRange (axis.LabelPoints.Min (), 0, 29);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		#endregion
	}

	public class TextAnnotationTests {
		readonly ITestOutputHelper output;

		public TextAnnotationTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void TestTextAnnotation_ScreenUnits ()
		{
			var gv = GraphViewTests.GetGraph ();

			gv.Annotations.Add (new TextAnnotation () {
				Text = "hey!",
				ScreenPosition = new Point (3, 1)
			});

			gv.Redraw (gv.Bounds);

			var expected =
@"
 │
 ┤ hey!
 ┤
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// user scrolls up one unit of graph space
			gv.ScrollOffset = new PointF (0, 1f);
			gv.Redraw (gv.Bounds);

			// we expect no change in the location of the annotation (only the axis label changes)
			// this is because screen units are constant and do not change as the viewport into
			// graph space scrolls to different areas of the graph
			expected =
@"
 │
 ┤ hey!
 ┤
1┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void TestTextAnnotation_GraphUnits ()
		{
			var gv = GraphViewTests.GetGraph ();

			gv.Annotations.Add (new TextAnnotation () {
				Text = "hey!",
				GraphPosition = new PointF (2, 2)
			});

			gv.Redraw (gv.Bounds);

			var expected =
@"
 │
 ┤ hey!
 ┤
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// user scrolls up one unit of graph space
			gv.ScrollOffset = new PointF (0, 1f);
			gv.Redraw (gv.Bounds);

			// we expect the text annotation to go down one line since
			// the scroll offset means that that point of graph space is 
			// lower down in the view.  Note the 1 on the axis too, our viewport
			// (excluding margins) now shows y of 1 to 4 (previously 0 to 5)
			expected =
@"
 │
 ┤ 
 ┤ hey!
1┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void TestTextAnnotation_LongText ()
		{
			var gv = GraphViewTests.GetGraph ();

			gv.Annotations.Add (new TextAnnotation () {
				Text = "hey there partner hows it going boy its great",
				GraphPosition = new PointF (2, 2)
			});

			gv.Redraw (gv.Bounds);

			// long text should get truncated
			// margin takes up 1 units
			// the GraphPosition of the anntation is 2
			// Leaving 7 characters of the annotation renderable (including space)
			var expected =
@"
 │
 ┤ hey the
 ┤
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void TestTextAnnotation_Offscreen ()
		{
			var gv = GraphViewTests.GetGraph ();

			gv.Annotations.Add (new TextAnnotation () {
				Text = "hey there partner hows it going boy its great",
				GraphPosition = new PointF (9, 2)
			});

			gv.Redraw (gv.Bounds);

			// Text is off the screen (graph x axis runs to 8 not 9)
			var expected =
@"
 │
 ┤
 ┤
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Theory]
		[InlineData (null)]
		[InlineData ("  ")]
		[InlineData ("\t\t")]
		public void TestTextAnnotation_EmptyText (string whitespace)
		{
			var gv = GraphViewTests.GetGraph ();

			gv.Annotations.Add (new TextAnnotation () {
				Text = whitespace,
				GraphPosition = new PointF (4, 2)
			});

			// add a point a bit further along the graph so if the whitespace were rendered
			// the test would pick it up (AssertDriverContentsAre ignores trailing whitespace on lines)
			var points = new ScatterSeries ();
			points.Points.Add (new PointF (7, 2));
			gv.Series.Add (points);

			gv.Redraw (gv.Bounds);

			var expected =
@"
 │
 ┤      x
 ┤
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}
	}

	public class LegendTests {
		readonly ITestOutputHelper output;

		public LegendTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void LegendNormalUsage_WithBorder ()
		{
			var gv = GraphViewTests.GetGraph ();
			var legend = new LegendAnnotation (new Rect (2, 0, 5, 3));
			legend.AddEntry (new GraphCellToRender ('A'), "Ant");
			legend.AddEntry (new GraphCellToRender ('B'), "Bat");

			gv.Annotations.Add (legend);
			gv.Redraw (gv.Bounds);

			var expected =
@"
 │┌───┐
 ┤│AAn│
 ┤└───┘
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void LegendNormalUsage_WithoutBorder ()
		{
			var gv = GraphViewTests.GetGraph ();
			var legend = new LegendAnnotation (new Rect (2, 0, 5, 3));
			legend.AddEntry (new GraphCellToRender ('A'), "Ant");
			legend.AddEntry (new GraphCellToRender ('B'), "?"); // this will exercise pad
			legend.AddEntry (new GraphCellToRender ('C'), "Cat");
			legend.AddEntry (new GraphCellToRender ('H'), "Hattter"); // not enough space for this oen
			legend.Border = false;

			gv.Annotations.Add (legend);
			gv.Redraw (gv.Bounds);

			var expected =
@"
 │AAnt
 ┤B?
 ┤CCat
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}
	}

	public class PathAnnotationTests {
		readonly ITestOutputHelper output;

		public PathAnnotationTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void PathAnnotation_Box ()
		{
			var gv = GraphViewTests.GetGraph ();

			var path = new PathAnnotation ();
			path.Points.Add (new PointF (1, 1));
			path.Points.Add (new PointF (1, 3));
			path.Points.Add (new PointF (6, 3));
			path.Points.Add (new PointF (6, 1));

			// list the starting point again so that it draws a complete square
			// (otherwise it will miss out the last line along the bottom)
			path.Points.Add (new PointF (1, 1));

			gv.Annotations.Add (path);
			gv.Redraw (gv.Bounds);

			var expected =
@"
 │......
 ┤.    .
 ┤......
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void YAxisLabels_With_MarginBottom ()
		{
			GraphViewTests.InitFakeDriver ();
			var gv = new GraphView {
				ColorScheme = new ColorScheme (),
				Bounds = new Rect (0, 0, 10, 7)
			};

			gv.CellSize = new PointF (1, 0.5f);
			gv.AxisY.Increment = 1;
			gv.AxisY.ShowLabelsEvery = 1;

			gv.Series.Add (new ScatterSeries {
				Points = { new PointF (1, 1), new PointF (5, 0) }
			});

			// reserve 3 cells of the console for the margin
			gv.MarginBottom = 3;
			gv.MarginLeft = 1;

			gv.Redraw (gv.Bounds);

			var expected =
@"
 │
1┤x
 │ 
0┼┬┬┬┬x┬┬┬
 0    5   
          
          ";
			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void XAxisLabels_With_MarginLeft ()
		{
			GraphViewTests.InitFakeDriver ();
			var gv = new GraphView {
				ColorScheme = new ColorScheme (),
				Bounds = new Rect (0, 0, 10, 7)
			};

			gv.CellSize = new PointF (1, 0.5f);
			gv.AxisY.Increment = 1;
			gv.AxisY.ShowLabelsEvery = 1;

			gv.Series.Add (new ScatterSeries {
				Points = { new PointF (1, 1), new PointF (5, 0) }
			});

			// reserve 3 cells of the left for the margin
			gv.MarginLeft = 3;
			gv.MarginBottom = 1;

			gv.Redraw (gv.Bounds);

			var expected =
	@"
   │
  2┤
   │
  1┤x
   │ 
  0┼┬┬┬┬x┬
   0    5
         
          ";
			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}


		[Fact]
		public void MarginBottom_BiggerThanHeight_ExpectBlankGraph ()
		{
			var gv = GraphViewTests.GetGraph ();
			gv.Height = 10;
			gv.MarginBottom = 20;

			gv.Series.Add (new ScatterSeries {
				Points = { new PointF (1, 1), new PointF (5, 0) }
			});


			gv.Redraw (gv.Bounds);

			var expected =
	@"
         
         
          ";
			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}
		[Fact]
		public void MarginLeft_BiggerThanWidth_ExpectBlankGraph ()
		{
			var gv = GraphViewTests.GetGraph ();
			gv.Width = 10;
			gv.MarginLeft = 20;

			gv.Series.Add (new ScatterSeries {
				Points = { new PointF (1, 1), new PointF (5, 0) }
			});


			gv.Redraw (gv.Bounds);

			var expected =
	@"
         
         
          ";
			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void PathAnnotation_Diamond ()
		{
			var gv = GraphViewTests.GetGraph ();

			var path = new PathAnnotation ();
			path.Points.Add (new PointF (1, 2));
			path.Points.Add (new PointF (3, 3));
			path.Points.Add (new PointF (6, 2));
			path.Points.Add (new PointF (3, 1));

			// list the starting point again to close the shape
			path.Points.Add (new PointF (1, 2));

			gv.Annotations.Add (path);
			gv.Redraw (gv.Bounds);

			var expected =
@"
 │  ..
 ┤..  ..
 ┤ ...
0┼┬┬┬┬┬┬┬┬
 0    5";

			TestHelpers.AssertDriverContentsAre (expected, output);


			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Theory]
		[InlineData (true)]
		[InlineData (false)]
		public void LabelChangeText_RendersCorrectly (bool useFill)
		{
			var driver = new FakeDriver ();
			Application.Init (driver);
			driver.Init (() => { });

			// create a wide window
			var mount = new View () {
				Width = 100,
				Height = 100
			};

			try {
				// Create a label with a short text 
				var lbl1 = new Label ("ff");

				// Specify that the label should be very wide
				if (useFill) {
					lbl1.Width = Dim.Fill ();
				} else {
					lbl1.Width = 100;
				}

				//put label into view
				mount.Add (lbl1);

				//putting mount into toplevel since changing size
				//also change AutoSize to false
				Application.Top.Add (mount);
				Application.Begin (Application.Top);

				// render view
				lbl1.ColorScheme = new ColorScheme ();
				Assert.Equal (1, lbl1.Height);
				mount.Redraw (mount.Bounds);

				// should have the initial text
				TestHelpers.AssertDriverContentsAre ("ff", null);

				// change the text and redraw
				lbl1.Text = "ff1234";
				mount.Redraw (mount.Bounds);

				// should have the new text rendered
				TestHelpers.AssertDriverContentsAre ("ff1234", null);


			} finally {
				Application.Shutdown ();
			}
		}
	}

	public class AxisIncrementToRenderTests {
		[Fact]
		public void AxisIncrementToRenderTests_Constructor ()
		{
			var render = new AxisIncrementToRender (Orientation.Horizontal, 1, 6.6f);

			Assert.Equal (Orientation.Horizontal, render.Orientation);
			Assert.Equal (1, render.ScreenLocation);
			Assert.Equal (6.6f, render.Value);
		}
	}
}
