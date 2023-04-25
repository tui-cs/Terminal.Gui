using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests {
	public class BorderTests {
		readonly ITestOutputHelper output;

		public BorderTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void View_BorderStyle_Defaults ()
		{
			var view = new View ();
			Assert.Equal (LineStyle.None, view.BorderStyle);
			Assert.Equal (Thickness.Empty, view.Border.Thickness);
		}

		[Fact]
		public void View_SetBorderStyle ()
		{
			var view = new View ();
			view.BorderStyle = LineStyle.Single;
			Assert.Equal (LineStyle.Single, view.BorderStyle);
			Assert.Equal (new Thickness(1), view.Border.Thickness);

			view.BorderStyle = LineStyle.Double;
			Assert.Equal (LineStyle.Double, view.BorderStyle);
			Assert.Equal (new Thickness (1), view.Border.Thickness);

			view.BorderStyle = LineStyle.None;
			Assert.Equal (LineStyle.None, view.BorderStyle);
			Assert.Equal (Thickness.Empty, view.Border.Thickness);
		}

		//[Fact]
		//public void View_BorderStyleChanged ()
		//{
		//}
	}
}
