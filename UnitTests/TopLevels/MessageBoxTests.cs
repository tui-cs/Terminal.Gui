using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Text;
using Terminal.Gui;

namespace Terminal.Gui.TopLevelTests {

	public class MessageBoxTests {
		readonly ITestOutputHelper output;

		public MessageBoxTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void MessageBox_With_Empty_Size_Without_Buttons ()
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
                ┌ Title ───────────────────────────────────────┐
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
		public void MessageBox_With_Empty_Size_With_Button ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					var aboutMessage = new StringBuilder ();
					aboutMessage.AppendLine (@"A comprehensive sample library for");
					aboutMessage.AppendLine (@"");
					aboutMessage.AppendLine (@"  _______                  _             _   _____       _  ");
					aboutMessage.AppendLine (@" |__   __|                (_)           | | / ____|     (_) ");
					aboutMessage.AppendLine (@"    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _  ");
					aboutMessage.AppendLine (@"    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | | ");
					aboutMessage.AppendLine (@"    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | | ");
					aboutMessage.AppendLine (@"    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_| ");
					aboutMessage.AppendLine (@"");
					aboutMessage.AppendLine (@"https://github.com/gui-cs/Terminal.Gui");

					MessageBox.Query ("About UI Catalog", aboutMessage.ToString (), "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
         ┌ About UI Catalog ──────────────────────────────────────────┐
         │             A comprehensive sample library for             │
         │                                                            │
         │  _______                  _             _   _____       _  │
         │ |__   __|                (_)           | | / ____|     (_) │
         │    | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _  │
         │    | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | | │
         │    | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | | │
         │    |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_| │
         │                                                            │
         │           https://github.com/gui-cs/Terminal.Gui           │
         │                                                            │
         │                          [◦ Ok ◦]                          │
         └────────────────────────────────────────────────────────────┘
", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void MessageBox_With_A_Lower_Fixed_Size ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (7, 5, "Title", "Message", "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
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
		public void MessageBox_With_A_Enough_Fixed_Size ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query (11, 5, "Title", "Message", "_Ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
                                  ┌ Title ──┐
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
		public void MessageBox_With_A_Label_Without_Spaces_WrapMessagge_True ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query ("mywindow", new string ('f', 2000), "ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
┌ mywindow ────────────────────────────────────────────────────────────────────┐
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff│
│                                   [◦ ok ◦]                                   │
└──────────────────────────────────────────────────────────────────────────────┘", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void MessageBox_With_A_Label_With_Spaces_WrapMessagge_True ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					var sb = new StringBuilder ();
					for (int i = 0; i < 1000; i++)
						sb.Append ("ff ");

					MessageBox.Query ("mywindow", sb.ToString (), "ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
┌ mywindow ────────────────────────────────────────────────────────────────────┐
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff │
│                                   [◦ ok ◦]                                   │
└──────────────────────────────────────────────────────────────────────────────┘", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void MessageBox_With_A_Label_Without_Spaces_WrapMessagge_False ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query ("mywindow", new string ('f', 2000), 0, null, false, "ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
────────────────────────────────────────────────────────────────────────────────
ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff
                                                                                
                                    [◦ ok ◦]                                    
────────────────────────────────────────────────────────────────────────────────", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Fact, AutoInitShutdown]
		public void MessageBox_With_A_Label_With_Spaces_WrapMessagge_False ()
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					var sb = new StringBuilder ();
					for (int i = 0; i < 1000; i++)
						sb.Append ("ff ");

					MessageBox.Query ("mywindow", sb.ToString (), 0, null, false, "ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
────────────────────────────────────────────────────────────────────────────────
 ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff ff f
                                                                                
                                    [◦ ok ◦]                                    
────────────────────────────────────────────────────────────────────────────────", output);

					Application.RequestStop ();
				}
			};

			Application.Run ();
		}

		[Theory, AutoInitShutdown]
		[InlineData ("", true)]
		[InlineData ("", false)]
		[InlineData ("\n", true)]
		[InlineData ("\n", false)]
		public void MessageBox_With_A_Empty_Message_Or_A_NewLline_WrapMessagge_True_Or_False (string message, bool wrapMessage)
		{
			var iterations = -1;
			Application.Begin (Application.Top);

			Application.Iteration += () => {
				iterations++;

				if (iterations == 0) {
					MessageBox.Query ("mywindow", message, 0, null, wrapMessage, "ok");

					Application.RequestStop ();
				} else if (iterations == 1) {
					Application.Refresh ();
					TestHelpers.AssertDriverContentsWithFrameAre (@"
                ┌ mywindow ────────────────────────────────────┐
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