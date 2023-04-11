using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Text;
using Terminal.Gui;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Terminal.Gui.DialogTests {

	public class MessageBoxTests {
		readonly ITestOutputHelper output;

		public MessageBoxTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		[AutoInitShutdown]
		public void Size_Default ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (string.Empty, string.Empty, null);

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					Assert.IsType<Dialog> (Application.Current);
					// Default size is Percent(60) 
					Assert.Equal (new Size ((int)(100 * .60), 5), Application.Current.Frame.Size);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact]
		[AutoInitShutdown]
		public void Location_Default ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (string.Empty, string.Empty, null);

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					Assert.IsType<Dialog> (Application.Current);
					// Default location is centered, so
					// X = (100 / 2) - (60 / 2) = 20
					// Y = (100 / 2) - (5 / 2) = 47
					Assert.Equal (new Point (20, 47), Application.Current.Frame.Location);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Theory]
		[InlineData (0, 0)]
		[InlineData (1, 1)]
		[InlineData (7, 5)]
		[InlineData (50, 50)]
		[AutoInitShutdown]
		public void Size_Not_Default_No_Message (int height, int width)
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (height, width, string.Empty, string.Empty, null);

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					Assert.IsType<Dialog> (Application.Current);
					Assert.Equal (new Size (height, width), Application.Current.Frame.Size);

					Application.RequestStop ();
				}
			};
		}

		[Theory]
		[InlineData (0, 0, "1")]
		[InlineData (1, 1, "1")]
		[InlineData (7, 5, "1")]
		[InlineData (50, 50, "1")]
		[InlineData (0, 0, "message")]
		[InlineData (1, 1, "message")]
		[InlineData (7, 5, "message")]
		[InlineData (50, 50, "message")]
		[AutoInitShutdown]
		public void Size_Not_Default_Message (int height, int width, string message)
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (height, width, string.Empty, message, null);

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					Assert.IsType<Dialog> (Application.Current);
					Assert.Equal (new Size (height, width), Application.Current.Frame.Size);

					Application.RequestStop ();
				}
			};
		}

		[Theory]
		[InlineData (0, 0, "1")]
		[InlineData (1, 1, "1")]
		[InlineData (7, 5, "1")]
		[InlineData (50, 50, "1")]
		[InlineData (0, 0, "message")]
		[InlineData (1, 1, "message")]
		[InlineData (7, 5, "message")]
		[InlineData (50, 50, "message")]
		[AutoInitShutdown]
		public void Size_Not_Default_Message_Button (int height, int width, string message)
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (100, 100);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (height, width, string.Empty, message, "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					Assert.IsType<Dialog> (Application.Current);
					Assert.Equal (new Size (height, width), Application.Current.Frame.Size);

					Application.RequestStop ();
				}
			};
		}

		[Fact, AutoInitShutdown]
		public void Size_None_No_Buttons ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query ("Title", "Message");

					Application.RequestStop ();

				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
                ┌┤Title├───────────────────────────────────────┐
                │                   Message                    │
                │                                              │
                │                                              │
                └──────────────────────────────────────────────┘
", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Size_No_With_Button ()
		{
			Application.Top.BorderStyle = LineStyle.Double;
			var iterations = -1;
			Application.Begin (Application.Top);

			var aboutMessage = new StringBuilder ();
			aboutMessage.AppendLine (@"0123456789012345678901234567890123456789");
			aboutMessage.AppendLine (@"https://github.com/gui-cs/Terminal.Gui");
			var message = aboutMessage.ToString ();

			((FakeDriver)Application.Driver).SetBufferSize (40 + 4, 8);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {


					MessageBox.Query (string.Empty, message, "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════════════════════════════╗
║┌────────────────────────────────────────┐║
║│0123456789012345678901234567890123456789│║
║│ https://github.com/gui-cs/Terminal.Gui │║
║│                                        │║
║│                [◦ Ok ◦]                │║
║└────────────────────────────────────────┘║
╚══════════════════════════════════════════╝
", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Size_Tiny_Fixed_Size ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (7, 5, string.Empty, "Message", "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					Assert.Equal (new Size (7, 5), Application.Current.Frame.Size);

					TestHelpers.AssertDriverContentsWithFrameAre (@"
                                    ┌─────┐
                                    │Messa│
                                    │ ge  │
                                    │ Ok ◦│
                                    └─────┘
", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Size_JustBigEnough_Fixed_Size ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (11, 5, string.Empty, "Message", "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
                                  ┌─────────┐
                                  │ Message │
                                  │         │
                                  │[◦ Ok ◦] │
                                  └─────────┘
", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Message_Long_Without_Spaces_WrapMessage_True ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			Application.Top.BorderStyle = LineStyle.Double;
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					// 50 characters should make the height of the wrapped text 7
					MessageBox.Query (string.Empty, new string ('f', 50), defaultButton: 0, wrapMessage: true, "btn");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();

					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║┌────────────────┐║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│       ff       │║
║│                │║
║│    [◦ btn ◦]   │║
║└────────────────┘║
╚══════════════════╝", output);
					Assert.Equal (new Size (20 - 2, 10 - 2), Application.Current.Frame.Size);
					Application.RequestStop ();

					// Really long text
					MessageBox.Query (string.Empty, new string ('f', 500), defaultButton: 0, wrapMessage: true, "btn");
				} else if (iterations == 2) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔┌────────────────┐╗
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│    [◦ btn ◦]   │║
╚└────────────────┘╝", output);
					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Message_With_Spaces_WrapMessage_True ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			Application.Top.BorderStyle = LineStyle.Double;
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					var sb = new StringBuilder ();
					for (int i = 0; i < 17; i++)
						sb.Append ("ff ");

					MessageBox.Query (string.Empty, sb.ToString (), defaultButton: 0, wrapMessage: true, "btn");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║ ┌──────────────┐ ║
║ │ff ff ff ff ff│ ║
║ │ff ff ff ff ff│ ║
║ │ff ff ff ff ff│ ║
║ │    ff ff     │ ║
║ │              │ ║
║ │   [◦ btn ◦]  │ ║
║ └──────────────┘ ║
╚══════════════════╝", output);
					Application.RequestStop ();

					// Really long text
					MessageBox.Query (string.Empty, new string ('f', 500), defaultButton: 0, wrapMessage: true, "btn");
				} else if (iterations == 2) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔┌────────────────┐╗
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│    [◦ btn ◦]   │║
╚└────────────────┘╝", output);
					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Message_Without_Spaces_WrapMessage_False ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			Application.Top.BorderStyle = LineStyle.Double;
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (string.Empty, new string ('f', 50), defaultButton: 0, wrapMessage: true, "btn");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║┌────────────────┐║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│       ff       │║
║│                │║
║│    [◦ btn ◦]   │║
║└────────────────┘║
╚══════════════════╝", output);

					Application.RequestStop ();

					// Really long text
					MessageBox.Query (string.Empty, new string ('f', 500), defaultButton: 0, wrapMessage: true, "btn");
				} else if (iterations == 2) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔┌────────────────┐╗
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│    [◦ btn ◦]   │║
╚└────────────────┘╝", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void Message_With_Spaces_WrapMessage_False ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);
			Application.Top.BorderStyle = LineStyle.Double;
			((FakeDriver)Application.Driver).SetBufferSize (20, 10);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					var sb = new StringBuilder ();
					for (int i = 0; i < 17; i++)
						sb.Append ("ff ");

					MessageBox.Query (string.Empty, sb.ToString (), defaultButton: 0, wrapMessage: false, "btn");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║ ┌──────────────┐ ║
║ │ff ff ff ff ff│ ║
║ │ff ff ff ff ff│ ║
║ │ff ff ff ff ff│ ║
║ │    ff ff     │ ║
║ │              │ ║
║ │   [◦ btn ◦]  │ ║
║ └──────────────┘ ║
╚══════════════════╝", output);
					Application.RequestStop ();

					// Really long text
					MessageBox.Query (string.Empty, new string ('f', 500), defaultButton: 0, wrapMessage: false, "btn");
				} else if (iterations == 2) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
╔┌────────────────┐╗
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│ffffffffffffffff│║
║│    [◦ btn ◦]   │║
╚└────────────────┘╝", output);
					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Theory, AutoInitShutdown]
		[InlineData (" ", true)]
		[InlineData (" ", false)]
		[InlineData ("", true)]
		[InlineData ("", false)]
		[InlineData ("\n", true)]
		[InlineData ("\n", false)]
		[InlineData (" \n", true)]
		[InlineData (" \n", false)]
		public void Message_Empty_Or_A_NewLline_WrapMessagge_True_Or_False (string message, bool wrapMessage)
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (string.Empty, message, 0, wrapMessage, "ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
                ┌──────────────────────────────────────────────┐
                │                                              │
                │                                              │
                │                   [◦ ok ◦]                   │
                └──────────────────────────────────────────────┘", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}
	}
}