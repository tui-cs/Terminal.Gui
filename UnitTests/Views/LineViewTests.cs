using Terminal.Gui.Graphs;
using Xunit;

namespace Terminal.Gui.ViewTests {
	public class LineViewTests {

		[Fact]
		[AutoInitShutdown]
		public void LineView_DefaultConstructor()
		{
			var horizontal = new LineView();

			Assert.Equal (Orientation.Horizontal, horizontal.Orientation);
			Assert.Equal (Dim.Fill (), horizontal.Width);
			Assert.Equal (1, horizontal.Height);
		}
		[Fact]
		[AutoInitShutdown]
		public void LineView_Horizontal ()
		{
			var horizontal = new LineView (Orientation.Horizontal);

			Assert.Equal (Orientation.Horizontal, horizontal.Orientation);
			Assert.Equal (Dim.Fill (), horizontal.Width);
			Assert.Equal (1, horizontal.Height);
		}
		[Fact]
		[AutoInitShutdown]
		public void LineView_Vertical ()
		{
			var vert = new LineView (Orientation.Vertical);

			Assert.Equal (Orientation.Vertical, vert.Orientation);
			Assert.Equal (Dim.Fill(), vert.Height);
			Assert.Equal (1, vert.Width);
		}
	}
}
