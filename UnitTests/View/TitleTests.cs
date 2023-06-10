using System.Text;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests {
	public class TitleTests {
		readonly ITestOutputHelper output;

		public TitleTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Set_Title_Fires_TitleChanging ()
		{
			var r = new View ();
			Assert.Equal (string.Empty, r.Title);

			string expectedOld = null;
			string expectedDuring = null;
			string expectedAfter = null;
			bool cancel = false;
			r.TitleChanging += (s, args) => {
				Assert.Equal (expectedOld, args.OldTitle);
				Assert.Equal (expectedDuring, args.NewTitle);
				args.Cancel = cancel;
			};

			expectedOld = string.Empty;
			r.Title = expectedDuring = expectedAfter = "title";
			Assert.Equal (expectedAfter, r.Title);

			expectedOld = r.Title;
			r.Title = expectedDuring = expectedAfter = "a different title";
			Assert.Equal (expectedAfter, r.Title);

			// Now setup cancelling the change and change it back to "title"
			cancel = true;
			expectedOld = r.Title;
			r.Title = expectedDuring = "title";
			Assert.Equal (expectedAfter, r.Title);
			r.Dispose ();

		}

		[Fact]
		public void Set_Title_Fires_TitleChanged ()
		{
			var r = new View ();
			Assert.Equal (string.Empty, r.Title);

			string expectedOld = null;
			string expected = null;
			r.TitleChanged += (s, args) => {
				Assert.Equal (expectedOld, args.OldTitle);
				Assert.Equal (r.Title, args.NewTitle);
			};

			expected = "title";
			expectedOld = r.Title;
			r.Title = expected;
			Assert.Equal (expected, r.Title);
		}

	}
}
