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
	}
}
