using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests {
	public class StraightLineTests {

		readonly ITestOutputHelper output;

		public StraightLineTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[InlineData (Orientation.Horizontal, 0, 0, 0,
								0, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 0, 0, 1,
								0, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 0, 0, 2,
								0, 0, 2, 1)]
		[InlineData (Orientation.Horizontal, 0, 0, 3,
								0, 0, 3, 1)]
		[InlineData (Orientation.Horizontal, 0, 0, -1,
								0, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 0, 0, -2,
								-1, 0, 2, 1)]
		[InlineData (Orientation.Horizontal, 0, 0, -3,
								-2, 0, 3, 1)]

		[InlineData (Orientation.Horizontal, 1, 0, 0,
								1, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 1, 0, 1,
								1, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 1, 0, 2,
								1, 0, 2, 1)]
		[InlineData (Orientation.Horizontal, 1, 0, 3,
								1, 0, 3, 1)]
		[InlineData (Orientation.Horizontal, 1, 0, -1,
								1, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 1, 0, -2,
								0, 0, 2, 1)]
		[InlineData (Orientation.Horizontal, 1, 0, -3,
								-1, 0, 3, 1)]

		[InlineData (Orientation.Horizontal, -1, 0, 0,
								-1, 0, 1, 1)]
		[InlineData (Orientation.Horizontal, 0, -1, 1,
								0, -1, 1, 1)]
		[InlineData (Orientation.Horizontal, -1, -1, 1,
								-1, -1, 1, 1)]
		[InlineData (Orientation.Horizontal, -1, -1, 2,
								-1, -1, 2, 1)]
		[InlineData (Orientation.Horizontal, -10, -10, 10,
								-10, -10, 10, 1)]

		[InlineData (Orientation.Horizontal, 0, -1, -1,
								0, -1, 1, 1)]
		[InlineData (Orientation.Horizontal, -1, -1, -1,
								-1, -1, 1, 1)]
		[InlineData (Orientation.Horizontal, -1, -1, -2,
								-2, -1, 2, 1)]
		[InlineData (Orientation.Horizontal, -10, -10, -10,
								-19, -10, 10, 1)]

		[InlineData (Orientation.Vertical, 0, 0, 0,
								0, 0, 1, 1)]
		[InlineData (Orientation.Vertical, 0, 0, 1,
								0, 0, 1, 1)]
		[InlineData (Orientation.Vertical, 0, 0, 2,
								0, 0, 1, 2)]
		[InlineData (Orientation.Vertical, 0, 0, 3,
								0, 0, 1, 3)]
		[InlineData (Orientation.Vertical, 0, 0, -1,
								0, 0, 1, 1)]
		[InlineData (Orientation.Vertical, 0, 0, -2,
								0, -1, 1, 2)]
		[InlineData (Orientation.Vertical, 0, 0, -3,
								0, -2, 1, 3)]
		[Theory, SetupFakeDriver]
		public void Bounds (Orientation orientation, int x, int y, int length, int expectedX, int expectedY, int expectedWidth, int expectedHeight)
		{
			var sl = new LineCanvas.StraightLine (new Point (x, y), length, orientation, LineStyle.Single);

			Assert.Equal (new Rect (expectedX, expectedY, expectedWidth, expectedHeight), sl.Bounds);
		}

	}
}
