using System;
using Terminal.Gui;
using Xunit;

namespace Terminal.Gui.TypeTests {
	public class PointTests {
		[Fact]
		public void Point_New ()
		{
			var point = new Point ();
			Assert.True (point.IsEmpty);

			point = new Point (new Size ());
			Assert.True (point.IsEmpty);

			point = new Point (1, 2);
			Assert.False (point.IsEmpty);

			point = new Point (-1, -2);
			Assert.False (point.IsEmpty);
		}

		[Fact]
		public void Point_SetsValue ()
		{
			var point = new Point () {
				X = 0,
				Y = 0
			};
			Assert.True (point.IsEmpty);

			point = new Point () {
				X = 1,
				Y = 2
			};
			Assert.False (point.IsEmpty);

			point = new Point () {
				X = -1,
				Y = -2
			};
			Assert.False (point.IsEmpty);
		}

		[Fact]
		public void Point_Equals ()
		{
			var point1 = new Point ();
			var point2 = new Point ();
			Assert.Equal (point1, point2);

			point1 = new Point (1, 2);
			point2 = new Point (1, 2);
			Assert.Equal (point1, point2);

			point1 = new Point (1, 2);
			point2 = new Point (0, 2);
			Assert.NotEqual (point1, point2);

			point1 = new Point (1, 2);
			point2 = new Point (0, 3);
			Assert.NotEqual (point1, point2);
		}

		[Fact]
		public void Point_Size ()
		{
			var point = new Point (1, 2);
			var size = (Size)point;
			Assert.False (size.IsEmpty);

			point = new Point (-1, 2);
			Action action = () => size = (Size)point;
			var ex = Assert.Throws<ArgumentException> (action);
			Assert.Equal ("Either Width and Height must be greater or equal to 0.", ex.Message);

		}
	}
}
