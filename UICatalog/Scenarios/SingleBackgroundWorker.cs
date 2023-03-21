using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Single BackgroundWorker", Description: "A single BackgroundWorker threading opening another Toplevel")]
	[ScenarioCategory ("Threading")]
	[ScenarioCategory ("Top Level Windows")]
	public class SingleBackgroundWorker : Scenario {
		public override void Run ()
		{
			Application.Top.Dispose ();

			Application.Run<MainApp> ();

			Application.Top.Dispose ();
		}

		public class MainApp : Toplevel {
			private BackgroundWorker worker;
			private List<string> log = new List<string> ();
			private DateTime? startStaging;
			private ListView listLog;

			public MainApp ()
			{
				var menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Options", new MenuItem [] {
						new MenuItem ("_Run Worker", "", () => RunWorker(), null, null, Key.CtrlMask | Key.R),
						null,
						new MenuItem ("_Quit", "", () => Application.RequestStop(), null, null, Key.CtrlMask | Key.Q)
					})
				});
				Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Application.RequestStop()),
					new StatusItem(Key.CtrlMask | Key.P, "~^R~ Run Worker", () => RunWorker())
				});
				Add (statusBar);

				var top = new Toplevel ();

				top.Add (new Label ("Worker Log") {
					X = Pos.Center (),
					Y = 0
				});

				listLog = new ListView (log) {
					X = 0,
					Y = 2,
					Width = Dim.Fill (),
					Height = Dim.Fill ()
				};
				top.Add (listLog);
				Add (top);
			}

			private void RunWorker ()
			{
				worker = new BackgroundWorker () { WorkerSupportsCancellation = true };

				var cancel = new Button ("Cancel Worker");
				cancel.Clicked += (s,e) => {
					if (worker == null) {
						log.Add ($"Worker is not running at {DateTime.Now}!");
						listLog.SetNeedsDisplay ();
						return;
					}

					log.Add ($"Worker {startStaging}.{startStaging:fff} is canceling at {DateTime.Now}!");
					listLog.SetNeedsDisplay ();
					worker.CancelAsync ();
				};

				startStaging = DateTime.Now;
				log.Add ($"Worker is started at {startStaging}.{startStaging:fff}");
				listLog.SetNeedsDisplay ();

				var md = new Dialog ($"Running Worker started at {startStaging}.{startStaging:fff}", cancel);
				md.Add (new Label ("Wait for worker to finish...") {
					X = Pos.Center (),
					Y = Pos.Center ()
				});

				worker.DoWork += (s, e) => {
					var stageResult = new List<string> ();
					for (int i = 0; i < 500; i++) {
						stageResult.Add ($"Worker {i} started at {DateTime.Now}");
						e.Result = stageResult;
						Thread.Sleep (1);
						if (worker.CancellationPending) {
							e.Cancel = true;
							return;
						}
					}
				};

				worker.RunWorkerCompleted += (s, e) => {
					if (md.IsCurrentTop) {
						//Close the dialog
						Application.RequestStop ();
					}

					if (e.Error != null) {
						// Failed
						log.Add ($"Exception occurred {e.Error.Message} on Worker {startStaging}.{startStaging:fff} at {DateTime.Now}");
						listLog.SetNeedsDisplay ();
					} else if (e.Cancelled) {
						// Canceled
						log.Add ($"Worker {startStaging}.{startStaging:fff} was canceled at {DateTime.Now}!");
						listLog.SetNeedsDisplay ();
					} else {
						// Passed
						log.Add ($"Worker {startStaging}.{startStaging:fff} was completed at {DateTime.Now}.");
						listLog.SetNeedsDisplay ();
						Application.Refresh ();
						var builderUI = new StagingUIController (startStaging, e.Result as List<string>);
						builderUI.Load ();
					}
					worker = null;
				};
				worker.RunWorkerAsync ();
				Application.Run (md);
			}
		}

		public class StagingUIController : Window {
			Toplevel top;

			public StagingUIController (DateTime? start, List<string> list)
			{
				top = new Toplevel (Application.Top.Frame);
				top.KeyPress += (s,e) => {
					// Prevents Ctrl+Q from closing this.
					// Only Ctrl+C is allowed.
					if (e.KeyEvent.Key == Application.QuitKey) {
						e.Handled = true;
					}
				};

				bool Close ()
				{
					var n = MessageBox.Query (50, 7, "Close Window.", "Are you sure you want to close this window?", "Yes", "No");
					return n == 0;
				}

				var menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Stage", new MenuItem [] {
						new MenuItem ("_Close", "", () => { if (Close()) { Application.RequestStop(); } }, null, null, Key.CtrlMask | Key.C)
					})
				});
				top.Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Key.CtrlMask | Key.C, "~^C~ Close", () => { if (Close()) { Application.RequestStop(); } }),
				});
				top.Add (statusBar);

				Title = $"Worker started at {start}.{start:fff}";
				ColorScheme = Colors.Base;

				Add (new ListView (list) {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill ()
				});

				top.Add (this);
			}

			public void Load ()
			{
				Application.Run (top);
			}
		}
	}
}
