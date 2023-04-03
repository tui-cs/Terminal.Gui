using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.ViewTests {
	public class FrameViewTests {
		[Fact]
		public void Constuctors_Defaults ()
		{
			var fv = new FrameView ();
			Assert.Equal (string.Empty, fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			Assert.NotNull (fv.Border);
			Assert.Single (fv.InternalSubviews);
			Assert.Single (fv.Subviews);

			fv = new FrameView ("Test");
			Assert.Equal ("Test", fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			Assert.NotNull (fv.Border);
			Assert.Single (fv.InternalSubviews);
			Assert.Single (fv.Subviews);

			fv = new FrameView (new Rect (1, 2, 10, 20), "Test");
			Assert.Equal ("Test", fv.Title);
			Assert.Equal (string.Empty, fv.Text);
			Assert.NotNull (fv.Border);
			Assert.Single (fv.InternalSubviews);
			Assert.Single (fv.Subviews);
			Assert.Equal (new Rect (1, 2, 10, 20), fv.Frame);
		}
	}
}
