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

		[Theory, AutoInitShutdown]
		[InlineData (true)]
		[InlineData (false)]
		public void TestSpinnerView_AutoSpin (bool callStop)
		{
			var view = GetSpinnerView ();

			Assert.Empty (Application.MainLoop.timeouts);
			view.AutoSpin = true;
			Assert.NotEmpty (Application.MainLoop.timeouts);
			Assert.True (view.AutoSpin);

			//More calls to AutoSpin do not add more timeouts
			Assert.Single (Application.MainLoop.timeouts);
			view.AutoSpin = true;
			view.AutoSpin = true;
			view.AutoSpin = true;
			Assert.True (view.AutoSpin);
			Assert.Single (Application.MainLoop.timeouts);

			if (callStop) {
				view.AutoSpin = false;
				Assert.Empty (Application.MainLoop.timeouts);
				Assert.False (view.AutoSpin);
			} else {
				Assert.NotEmpty (Application.MainLoop.timeouts);
			}

			// Dispose clears timeout
			view.Dispose ();
			Assert.Empty (Application.MainLoop.timeouts);
		}

		[Fact, AutoInitShutdown]
		public void TestSpinnerView_ThrottlesAnimation ()
		{
			var view = GetSpinnerView ();

			view.Draw ();

			var expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Draw ();

			expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Draw ();

			expected = @"\";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Task.Delay (400).Wait ();

			view.SetNeedsDisplay ();
			view.Draw ();

			expected = "|";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void TestSpinnerView_NoThrottle ()
		{
			var view = GetSpinnerView ();
			view.SpinDelay = 0;

			view.Draw ();

			var expected = "|";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			view.SetNeedsDisplay ();
			view.Draw ();

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