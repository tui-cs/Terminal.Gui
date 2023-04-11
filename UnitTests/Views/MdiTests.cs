using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewsTests {
	public class MdiTests {
		public MdiTests ()
		{
#if DEBUG_IDISPOSABLE
			Responder.Instances.Clear ();
			Application.RunState.Instances.Clear ();
#endif
		}


		[Fact]
		public void Dispose_Toplevel_IsMdiContainer_False_With_Begin_End ()
		{
			Application.Init (new FakeDriver ());

			var top = new Toplevel ();
			var rs = Application.Begin (top);
			Application.End (rs);

			Application.Shutdown ();

#if DEBUG_IDISPOSABLE
			Assert.Equal (8, Responder.Instances.Count);
			Assert.True (Responder.Instances [0].WasDisposed);
			Assert.True (Responder.Instances [1].WasDisposed);
#endif
		}

		[Fact]
		public void Dispose_Toplevel_IsMdiContainer_True_With_Begin ()
		{
			Application.Init (new FakeDriver ());

			var mdi = new Toplevel { IsMdiContainer = true };
			var rs = Application.Begin (mdi);
			Application.End (rs);

			Application.Shutdown ();
#if DEBUG_IDISPOSABLE
			Assert.Equal (8, Responder.Instances.Count);
			Assert.True (Responder.Instances [0].WasDisposed);
			Assert.True (Responder.Instances [1].WasDisposed);
#endif
		}

		[Fact, AutoInitShutdown]
		public void Application_RequestStop_With_Params_On_A_Not_MdiContainer_Always_Use_Application_Current ()
		{
			var top1 = new Toplevel ();
			var top2 = new Toplevel ();
			var top3 = new Window ();
			var top4 = new Window ();
			var d = new Dialog ();

			// top1, top2, top3, d1 = 4
			var iterations = 4;

			top1.Ready += (s, e) => {
				Assert.Null (Application.MdiChildren);
				Application.Run (top2);
			};
			top2.Ready += (s, e) => {
				Assert.Null (Application.MdiChildren);
				Application.Run (top3);
			};
			top3.Ready += (s, e) => {
				Assert.Null (Application.MdiChildren);
				Application.Run (top4);
			};
			top4.Ready += (s, e) => {
				Assert.Null (Application.MdiChildren);
				Application.Run (d);
			};

			d.Ready += (s, e) => {
				Assert.Null (Application.MdiChildren);
				// This will close the d because on a not MdiContainer the Application.Current it always used.
				Application.RequestStop (top1);
				Assert.True (Application.Current == d);
			};

			d.Closed += (s,e) => Application.RequestStop (top1);

			Application.Iteration += () => {
				Assert.Null (Application.MdiChildren);
				if (iterations == 4) 					Assert.True (Application.Current == d);
else if (iterations == 3) 					Assert.True (Application.Current == top4);
else if (iterations == 2) 					Assert.True (Application.Current == top3);
else if (iterations == 1) 					Assert.True (Application.Current == top2);
else 					Assert.True (Application.Current == top1);
				Application.RequestStop (top1);
				iterations--;
			};

			Application.Run (top1);

			Assert.Null (Application.MdiChildren);
		}

		class Mdi : Toplevel {
			public Mdi ()
			{
				IsMdiContainer = true;
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void MdiContainer_With_Toplevel_RequestStop_Balanced ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();
			var d = new Dialog ();

			// MdiChild = c1, c2, c3
			// d1 = 1
			var iterations = 4;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (d);
			};

			// More easy because the Mdi Container handles all at once
			d.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				// This will not close the MdiContainer because d is a modal toplevel and will be closed.
				mdi.RequestStop ();
			};

			// Now this will close the MdiContainer propagating through the MdiChildes.
			d.Closed += (s,e) => {
				mdi.RequestStop ();
			};

			Application.Iteration += () => {
				if (iterations == 4) {
					// The Dialog was not closed before and will be closed now.
					Assert.True (Application.Current == d);
					Assert.False (d.Running);
				} else {
					Assert.Equal (iterations, Application.MdiChildren.Count);
					for (int i = 0; i < iterations; i++) 						Assert.Equal ((iterations - i + 1).ToString (), Application.MdiChildren [i].Id);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void MdiContainer_With_Application_RequestStop_MdiTop_With_Params ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();
			var d = new Dialog ();

			// MdiChild = c1, c2, c3
			// d1 = 1
			var iterations = 4;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (d);
			};

			// Also easy because the Mdi Container handles all at once
			d.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				// This will not close the MdiContainer because d is a modal toplevel
				Application.RequestStop (mdi);
			};

			// Now this will close the MdiContainer propagating through the MdiChildes.
			d.Closed += (s,e) => Application.RequestStop (mdi);

			Application.Iteration += () => {
				if (iterations == 4) {
					// The Dialog was not closed before and will be closed now.
					Assert.True (Application.Current == d);
					Assert.False (d.Running);
				} else {
					Assert.Equal (iterations, Application.MdiChildren.Count);
					for (int i = 0; i < iterations; i++) 						Assert.Equal ((iterations - i + 1).ToString (), Application.MdiChildren [i].Id);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void MdiContainer_With_Application_RequestStop_MdiTop_Without_Params ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();
			var d = new Dialog ();

			// MdiChild = c1, c2, c3 = 3
			// d1 = 1
			var iterations = 4;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (d);
			};

			//More harder because it's sequential.
			d.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				// Close the Dialog
				Application.RequestStop ();
			};

			// Now this will close the MdiContainer propagating through the MdiChildes.
			d.Closed += (s, e) => Application.RequestStop (mdi);

			Application.Iteration += () => {
				if (iterations == 4) {
					// The Dialog still is the current top and we can't request stop to MdiContainer
					// because we are not using parameter calls.
					Assert.True (Application.Current == d);
					Assert.False (d.Running);
				} else {
					Assert.Equal (iterations, Application.MdiChildren.Count);
					for (int i = 0; i < iterations; i++) 						Assert.Equal ((iterations - i + 1).ToString (), Application.MdiChildren [i].Id);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void IsMdiChild_Testing ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();
			var d = new Dialog ();

			Application.Iteration += () => {
				Assert.False (mdi.IsMdiChild);
				Assert.True (c1.IsMdiChild);
				Assert.True (c2.IsMdiChild);
				Assert.True (c3.IsMdiChild);
				Assert.False (d.IsMdiChild);

				mdi.RequestStop ();
			};

			Application.Run (mdi);
		}

		[Fact]
		[AutoInitShutdown]
		public void Modal_Toplevel_Can_Open_Another_Modal_Toplevel_But_RequestStop_To_The_Caller_Also_Sets_Current_Running_To_False_Too ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();
			var d1 = new Dialog ();
			var d2 = new Dialog ();

			// MdiChild = c1, c2, c3 = 3
			// d1, d2 = 2
			var iterations = 5;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (d1);
			};
			d1.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (d2);
			};

			d2.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Assert.True (Application.Current == d2);
				Assert.True (Application.Current.Running);
				// Trying to close the Dialog1
				d1.RequestStop ();
			};

			// Now this will close the MdiContainer propagating through the MdiChildes.
			d1.Closed += (s, e) => {
				Assert.True (Application.Current == d1);
				Assert.False (Application.Current.Running);
				mdi.RequestStop ();
			};

			Application.Iteration += () => {
				if (iterations == 5) {
					// The Dialog2 still is the current top and we can't request stop to MdiContainer
					// because Dialog2 and Dialog1 must be closed first.
					// Dialog2 will be closed in this iteration.
					Assert.True (Application.Current == d2);
					Assert.False (Application.Current.Running);
					Assert.False (d1.Running);
				} else if (iterations == 4) {
					// Dialog1 will be closed in this iteration.
					Assert.True (Application.Current == d1);
					Assert.False (Application.Current.Running);
				} else {
					Assert.Equal (iterations, Application.MdiChildren.Count);
					for (int i = 0; i < iterations; i++) 						Assert.Equal ((iterations - i + 1).ToString (), Application.MdiChildren [i].Id);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void Modal_Toplevel_Can_Open_Another_Not_Modal_Toplevel_But_RequestStop_To_The_Caller_Also_Sets_Current_Running_To_False_Too ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();
			var d1 = new Dialog ();
			var c4 = new Toplevel ();

			// MdiChild = c1, c2, c3, c4 = 4
			// d1 = 1
			var iterations = 5;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (d1);
			};
			d1.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				Application.Run (c4);
			};

			c4.Ready += (s, e) => {
				Assert.Equal (4, Application.MdiChildren.Count);
				// Trying to close the Dialog1
				d1.RequestStop ();
			};

			// Now this will close the MdiContainer propagating through the MdiChildes.
			d1.Closed += (s, e) => {
				mdi.RequestStop ();
			};

			Application.Iteration += () => {
				if (iterations == 5) {
					// The Dialog2 still is the current top and we can't request stop to MdiContainer
					// because Dialog2 and Dialog1 must be closed first.
					// Using request stop here will call the Dialog again without need
					Assert.True (Application.Current == d1);
					Assert.False (Application.Current.Running);
					Assert.True (c4.Running);
				} else {
					Assert.Equal (iterations, Application.MdiChildren.Count);
					for (int i = 0; i < iterations; i++) 						Assert.Equal ((iterations - i + (iterations == 4 && i == 0 ? 2 : 1)).ToString (),
							Application.MdiChildren [i].Id);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void MoveCurrent_Returns_False_If_The_Current_And_Top_Parameter_Are_Both_With_Running_Set_To_False ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();

			// MdiChild = c1, c2, c3
			var iterations = 3;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				c3.RequestStop ();
				c1.RequestStop ();
			};
			// Now this will close the MdiContainer propagating through the MdiChildes.
			c1.Closed += (s, e) => {
				mdi.RequestStop ();
			};
			Application.Iteration += () => {
				if (iterations == 3) {
					// The Current still is c3 because Current.Running is false.
					Assert.True (Application.Current == c3);
					Assert.False (Application.Current.Running);
					// But the childes order were reorder by Running = false
					Assert.True (Application.MdiChildren [0] == c3);
					Assert.True (Application.MdiChildren [1] == c1);
					Assert.True (Application.MdiChildren [^1] == c2);
				} else if (iterations == 2) {
					// The Current is c1 and Current.Running is false.
					Assert.True (Application.Current == c1);
					Assert.False (Application.Current.Running);
					Assert.True (Application.MdiChildren [0] == c1);
					Assert.True (Application.MdiChildren [^1] == c2);
				} else if (iterations == 1) {
					// The Current is c2 and Current.Running is false.
					Assert.True (Application.Current == c2);
					Assert.False (Application.Current.Running);
					Assert.True (Application.MdiChildren [^1] == c2);
				} else {
					// The Current is mdi.
					Assert.True (Application.Current == mdi);
					Assert.Empty (Application.MdiChildren);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void MdiContainer_Throws_If_More_Than_One ()
		{
			var mdi = new Mdi ();
			var mdi2 = new Mdi ();

			mdi.Ready += (s, e) => {
				Assert.Throws<InvalidOperationException> (() => Application.Run (mdi2));
				mdi.RequestStop ();
			};

			Application.Run (mdi);
		}

		[Fact]
		[AutoInitShutdown]
		public void MdiContainer_Open_And_Close_Modal_And_Open_Not_Modal_Toplevels_Randomly ()
		{
			var mdi = new Mdi ();
			var logger = new Toplevel ();

			var iterations = 1; // The logger
			var running = true;
			var stageCompleted = true;
			var allStageClosed = false;
			var mdiRequestStop = false;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (logger);
			};

			logger.Ready += (s, e) => Assert.Single (Application.MdiChildren);

			Application.Iteration += () => {
				if (stageCompleted && running) {
					stageCompleted = false;
					var stage = new Window () { Modal = true };

					stage.Ready += (s, e) => {
						Assert.Equal (iterations, Application.MdiChildren.Count);
						stage.RequestStop ();
					};

					stage.Closed += (_, _) => {
						if (iterations == 11) 							allStageClosed = true;
						Assert.Equal (iterations, Application.MdiChildren.Count);
						if (running) {
							stageCompleted = true;

							var rpt = new Window ();

							rpt.Ready += (s, e) => {
								iterations++;
								Assert.Equal (iterations, Application.MdiChildren.Count);
							};

							Application.Run (rpt);
						}
					};

					Application.Run (stage);

				} else if (iterations == 11 && running) {
					running = false;
					Assert.Equal (iterations, Application.MdiChildren.Count);

				} else if (!mdiRequestStop && running && !allStageClosed) 					Assert.Equal (iterations, Application.MdiChildren.Count);
else if (!mdiRequestStop && !running && allStageClosed) {
					Assert.Equal (iterations, Application.MdiChildren.Count);
					mdiRequestStop = true;
					mdi.RequestStop ();
				} else 					Assert.Empty (Application.MdiChildren);
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}

		[Fact]
		[AutoInitShutdown]
		public void AllChildClosed_Event_Test ()
		{
			var mdi = new Mdi ();
			var c1 = new Toplevel ();
			var c2 = new Window ();
			var c3 = new Window ();

			// MdiChild = c1, c2, c3
			var iterations = 3;

			mdi.Ready += (s, e) => {
				Assert.Empty (Application.MdiChildren);
				Application.Run (c1);
			};
			c1.Ready += (s, e) => {
				Assert.Single (Application.MdiChildren);
				Application.Run (c2);
			};
			c2.Ready += (s, e) => {
				Assert.Equal (2, Application.MdiChildren.Count);
				Application.Run (c3);
			};
			c3.Ready += (s, e) => {
				Assert.Equal (3, Application.MdiChildren.Count);
				c3.RequestStop ();
				c2.RequestStop ();
				c1.RequestStop ();
			};
			// Now this will close the MdiContainer when all MdiChildes was closed
			mdi.AllChildClosed += (s, e) => {
				mdi.RequestStop ();
			};
			Application.Iteration += () => {
				if (iterations == 3) {
					// The Current still is c3 because Current.Running is false.
					Assert.True (Application.Current == c3);
					Assert.False (Application.Current.Running);
					// But the childes order were reorder by Running = false
					Assert.True (Application.MdiChildren [0] == c3);
					Assert.True (Application.MdiChildren [1] == c2);
					Assert.True (Application.MdiChildren [^1] == c1);
				} else if (iterations == 2) {
					// The Current is c2 and Current.Running is false.
					Assert.True (Application.Current == c2);
					Assert.False (Application.Current.Running);
					Assert.True (Application.MdiChildren [0] == c2);
					Assert.True (Application.MdiChildren [^1] == c1);
				} else if (iterations == 1) {
					// The Current is c1 and Current.Running is false.
					Assert.True (Application.Current == c1);
					Assert.False (Application.Current.Running);
					Assert.True (Application.MdiChildren [^1] == c1);
				} else {
					// The Current is mdi.
					Assert.True (Application.Current == mdi);
					Assert.False (Application.Current.Running);
					Assert.Empty (Application.MdiChildren);
				}
				iterations--;
			};

			Application.Run (mdi);

			Assert.Empty (Application.MdiChildren);
		}
	}
}
