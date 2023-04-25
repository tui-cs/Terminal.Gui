using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class SpinnerViewTests {

		readonly ITestOutputHelper output;

		public SpinnerViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void TestSpinnerView_AutoSpin()
		{
			var view = GetSpinnerView ();

			Assert.Empty (Application.MainLoop.timeouts);
			view.AutoSpin ();
			Assert.NotEmpty (Application.MainLoop.timeouts);

			//More calls to AutoSpin do not add more timeouts
			Assert.Single (Application.MainLoop.timeouts);
			view.AutoSpin ();
			view.AutoSpin ();
			view.AutoSpin ();
			Assert.Single (Application.MainLoop.timeouts);

			// Dispose clears timeout
			Assert.NotEmpty (Application.MainLoop.timeouts);
			view.Dispose ();
			Assert.Empty (Application.MainLoop.timeouts);
		}

		[Fact, AutoInitShutdown]
		public void TestSpinnerView_ThrottlesAnimation ()
		{
			var view = GetSpinnerView ();

			view.Redraw (view.Bounds);

			var expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);

			expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);

			expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Task.Delay (400).Wait();

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);

			expected = "|";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void TestSpinnerView_NoThrottle ()
		{
			var view = GetSpinnerView ();
			view.SpinDelay = 0;

			view.Redraw (view.Bounds);

			var expected = "|";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Redraw (view.Bounds);

			expected = "/";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		private SpinnerView GetSpinnerView ()
		{
			var view = new SpinnerView ();

			Application.Top.Add (view);
			Application.Begin (Application.Top);

			Assert.Equal (1, view.Width);
			Assert.Equal (1, view.Height);

			return view;
		}
	}
}