using System;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.TypeTests {
	public class RectTests {
		[Fact]
		public void Rect_New ()
		{
			var rect = new Rect ();
			Assert.True (rect.IsEmpty);

			rect = new Rect (new Point (), new Size ());
			Assert.True (rect.IsEmpty);

			rect = new Rect (1, 2, 3, 4);
			Assert.False (rect.IsEmpty);

			rect = new Rect (-1, -2, 3, 4);
			Assert.False (rect.IsEmpty);

			Action action = () => new Rect (1, 2, -3, 4);
			var ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Width must be greater or equal to 0.", ex.Message);

			action = () => new Rect (1, 2, 3, -4);
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Height must be greater or equal to 0.", ex.Message);

			action = () => new Rect (1, 2, -3, -4);
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Width must be greater or equal to 0.", ex.Message);
		}

		[Fact]
		public void Rect_SetsValue ()
		{
			var rect = new Rect () {
				X = 0,
				Y = 0
			};
			Assert.True (rect.IsEmpty);

			rect = new Rect () {
				X = -1,
				Y = -2
			};
			Assert.False (rect.IsEmpty);

			rect = new Rect () {
				Width = 3,
				Height = 4
			};
			Assert.False (rect.IsEmpty);

			rect = new Rect () {
				X = -1,
				Y = -2,
				Width = 3,
				Height = 4
			};
			Assert.False (rect.IsEmpty);

			Action action = () => {
				rect = new Rect () {
					X = -1,
					Y = -2,
					Width = -3,
					Height = 4
				};
			};
			var ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Width must be greater or equal to 0.", ex.Message);

			action = () => {
				rect = new Rect () {
					X = -1,
					Y = -2,
					Width = 3,
					Height = -4
				};
			};
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Height must be greater or equal to 0.", ex.Message);

			action = () => {
				rect = new Rect () {
					X = -1,
					Y = -2,
					Width = -3,
					Height = -4
				};
			};
			ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Width must be greater or equal to 0.", ex.Message);
		}

		[Fact]
		public void Rect_Equals ()
		{
			var rect1 = new Rect ();
			var rect2 = new Rect ();
			Assert.Equal (rect1, rect2);

			rect1 = new Rect (1, 2, 3, 4);
			rect2 = new Rect (1, 2, 3, 4);
			Assert.Equal (rect1, rect2);

			rect1 = new Rect (1, 2, 3, 4);
			rect2 = new Rect (-1, 2, 3, 4);
			Assert.NotEqual (rect1, rect2);
		}

		[Fact]
		public void Positive_X_Y_Positions ()
		{
			var rect = new Rect (10, 5, 100, 50);
			int yCount = 0, xCount = 0, yxCount = 0;

			for (int line = rect.Y; line < rect.Y + rect.Height; line++) {
				yCount++;
				xCount = 0;
				for (int col = rect.X; col < rect.X + rect.Width; col++) {
					xCount++;
					yxCount++;
				}
			}
			Assert.Equal (yCount, rect.Height);
			Assert.Equal (xCount, rect.Width);
			Assert.Equal (yxCount, rect.Height * rect.Width);
		}

		[Fact]
		public void Negative_X_Y_Positions ()
		{
			var rect = new Rect (-10, -5, 100, 50);
			int yCount = 0, xCount = 0, yxCount = 0;

			for (int line = rect.Y; line < rect.Y + rect.Height; line++) {
				yCount++;
				xCount = 0;
				for (int col = rect.X; col < rect.X + rect.Width; col++) {
					xCount++;
					yxCount++;
				}
			}
			Assert.Equal (yCount, rect.Height);
			Assert.Equal (xCount, rect.Width);
			Assert.Equal (yxCount, rect.Height * rect.Width);
		}

		[Theory]
		// Empty
		[InlineData (
			0, 0, 0, 0,
			0, 0,
			0, 0, 0, 0)]
		[InlineData (
			0, 0, 0, 0,
			1, 0,
			-1, 0, 2, 0)]
		[InlineData (
			0, 0, 0, 0,
			0, 1,
			0, -1, 0, 2)]
		[InlineData (
			0, 0, 0, 0,
			1, 1,
			-1, -1, 2, 2)]
		[InlineData (
			0, 0, 0, 0,
			-1, -1,        // Throws
			0, 0, 0, 0)]
		// Zero location, Size of 1
		[InlineData (
			0, 0, 1, 1,
			0, 0,
			0, 0, 1, 1)]
		[InlineData (
			0, 0, 1, 1,
			1, 0,
			-1, 0, 3, 1)]
		[InlineData (
			0, 0, 1, 1,
			0, 1,
			0, -1, 1, 3)]
		[InlineData (
			0, 0, 1, 1,
			1, 1,
			-1, -1, 3, 3)]
		// Positive location, Size of 1
		[InlineData (
			1, 1, 1, 1,
			0, 0,
			1, 1, 1, 1)]
		[InlineData (
			1, 1, 1, 1,
			1, 0,
			0, 1, 3, 1)]
		[InlineData (
			1, 1, 1, 1,
			0, 1,
			1, 0, 1, 3)]
		[InlineData (
			1, 1, 1, 1,
			1, 1,
			0, 0, 3, 3)]
		public void Inflate (int x, int y, int width, int height, int inflateWidth, int inflateHeight, int expectedX, int exptectedY, int expectedWidth, int expectedHeight)
		{
			var rect = new Rect (x, y, width, height);

			if (rect.Width + inflateWidth < 0 || rect.Height + inflateHeight < 0) {
				Assert.Throws<ArgumentException> (() => rect.Inflate (inflateWidth, inflateHeight));
			} else {
				rect.Inflate (inflateWidth, inflateHeight);
			}
			Assert.Equal (expectedWidth, rect.Width);
			Assert.Equal (expectedHeight, rect.Height);
			Assert.Equal (expectedX, rect.X);
			Assert.Equal (exptectedY, rect.Y);

			// Use the other overload (Size)
			rect = new Rect (x, y, width, height);
			if (rect.Width + inflateWidth < 0 || rect.Height + inflateHeight < 0) {
				Assert.Throws<ArgumentException> (() => rect.Inflate (new Size (inflateWidth, inflateHeight)));
			} else {
				rect.Inflate (new Size (inflateWidth, inflateHeight));
			}
			Assert.Equal (expectedWidth, rect.Width);
			Assert.Equal (expectedHeight, rect.Height);
			Assert.Equal (expectedX, rect.X);
			Assert.Equal (exptectedY, rect.Y);
		}

		[Fact]
		public void Union_PositiveCoords ()
		{
			var r1 = new Rect (0, 0, 2, 2);
			var r2 = new Rect (1, 1, 2, 2);
			var result = Rect.Union (r1, r2);
			Assert.Equal (new Rect (0, 0, 3, 3), result);
		}

		[Fact]
		public void Union_NegativeCoords ()
		{
			// arrange
			Rect rect1 = new Rect (-2, -2, 4, 4);
			Rect rect2 = new Rect (-1, -1, 5, 5);

			// act
			Rect result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (-2, -2, 6, 6), result);
		}

		[Fact]
		public void Union_EmptyRectangles ()
		{
			var r1 = new Rect (0, 0, 0, 0);
			var r2 = new Rect (1, 1, 0, 0);
			var result = Rect.Union (r1, r2);
			Assert.Equal (new Rect (0, 0, 1, 1), result);
		}

		[Fact]
		public void Union_SameRectangle ()
		{
			var r1 = new Rect (0, 0, 2, 2);
			var r2 = new Rect (0, 0, 2, 2);
			var result = Rect.Union (r1, r2);
			Assert.Equal (new Rect (0, 0, 2, 2), result);
		}

		[Fact]
		public void Union_RectanglesOverlap_ReturnsCombinedRectangle ()
		{
			// arrange
			var rect1 = new Rect (1, 1, 3, 3);
			var rect2 = new Rect (2, 2, 3, 3);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (1, 1, 4, 4), result);
		}

		[Fact]
		public void Union_RectanglesTouchHorizontally_ReturnsCombinedRectangle ()
		{
			// arrange
			var rect1 = new Rect (1, 1, 3, 3);
			var rect2 = new Rect (4, 2, 3, 3);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (1, 1, 6, 4), result);
		}

		[Fact]
		public void Union_RectanglesTouchVertically_ReturnsCombinedRectangle ()
		{
			// arrange
			var rect1 = new Rect (1, 1, 3, 3);
			var rect2 = new Rect (2, 4, 3, 3);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (1, 1, 4, 6), result);
		}

		[Fact]
		public void Union_RectanglesDoNotOverlap_ReturnsCombinedRectangle ()
		{
			// arrange
			var rect1 = new Rect (1, 1, 3, 3);
			var rect2 = new Rect (5, 5, 3, 3);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (1, 1, 7, 7), result);
		}

		[Fact]
		public void Union_RectangleBIsLarger_ReturnsB ()
		{
			// arrange
			var rect1 = new Rect (1, 1, 3, 3);
			var rect2 = new Rect (2, 2, 6, 6);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (1, 1, 7, 7), result);
		}

		[Fact]
		public void Union_RectangleAIsLarger_ReturnsA ()
		{
			// arrange
			var rect1 = new Rect (1, 1, 6, 6);
			var rect2 = new Rect (2, 2, 3, 3);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (1, 1, 6, 6), result);
		}

		[Fact]
		public void Union_RectangleAHasNegativeCoordinates_ReturnsCombinedRectangle ()
		{
			// arrange
			var rect1 = new Rect (-2, -2, 5, 5);
			var rect2 = new Rect (3, 3, 4, 4);

			// act
			var result = Rect.Union (rect1, rect2);

			// assert
			Assert.Equal (new Rect (-2, -2, 9, 9), result);
		}

	}
}
