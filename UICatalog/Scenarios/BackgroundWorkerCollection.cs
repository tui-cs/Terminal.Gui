using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "BackgroundWorker Collection", Description: "A persisting multi Toplevel BackgroundWorker threading")]
	[ScenarioCategory ("Threading")]
	[ScenarioCategory ("Top Level Windows")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Controls")]
	public class BackgroundWorkerCollection : Scenario {

		public override void Run ()
		{
			Application.Run<OverlappedMain> ();
		}

		class OverlappedMain : Toplevel {
			private WorkerApp workerApp;
			private bool canOpenWorkerApp;
			MenuBar menu;

			public OverlappedMain ()
			{
				Data = "OverlappedMain";

				IsOverlappedContainer = true;

				workerApp = new WorkerApp () { Visible = false };

				menu = new MenuBar (new MenuBarItem [] {
					new MenuBarItem ("_Options", new MenuItem [] {
						new MenuItem ("_Run Worker", "", () => workerApp.RunWorker(), null, null, Key.CtrlMask | Key.R),
						new MenuItem ("_Cancel Worker", "", () => workerApp.CancelWorker(), null, null, Key.CtrlMask | Key.C),
						null,
						new MenuItem ("_Quit", "", () => Quit(), null, null, Application.QuitKey)
					}),
					new MenuBarItem ("_View", new MenuItem [] { }),
					new MenuBarItem ("_Window", new MenuItem [] { })
				}); ;
				menu.MenuOpening += Menu_MenuOpening;
				Add (menu);

				var statusBar = new StatusBar (new [] {
					new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
					new StatusItem(Key.CtrlMask | Key.R, "~^R~ Run Worker", () => workerApp.RunWorker()),
					new StatusItem(Key.CtrlMask | Key.C, "~^C~ Cancel Worker", () => workerApp.CancelWorker())
				});
				Add (statusBar);

				Activate += OverlappedMain_Activate;
				Deactivate += OverlappedMain_Deactivate;

				Closed += OverlappedMain_Closed;

				Application.Iteration += () => {
					if (canOpenWorkerApp && !workerApp.Running && Application.OverlappedTop.Running) {
						Application.Run (workerApp);
					}
				};
			}

			private void OverlappedMain_Closed (object sender, ToplevelEventArgs e)
			{
				workerApp.Dispose ();
				Dispose ();
			}

			private void Menu_MenuOpening (object sender, MenuOpeningEventArgs menu)
			{
				if (!canOpenWorkerApp) {
					canOpenWorkerApp = true;
					return;
				}
				if (menu.CurrentMenu.Title == "_Window") {
					menu.NewMenuBarItem = OpenedWindows ();
				} else if (menu.CurrentMenu.Title == "_View") {
					menu.NewMenuBarItem = View ();
				}
			}

			private void OverlappedMain_Deactivate (object sender, ToplevelEventArgs top)
			{
				workerApp.WriteLog ($"{top.Toplevel.Data} deactivate.");
			}

			private void OverlappedMain_Activate (object sender, ToplevelEventArgs top)
			{
				workerApp.WriteLog ($"{top.Toplevel.Data} activate.");
			}

			private MenuBarItem View ()
			{
				List<MenuItem> menuItems = new List<MenuItem> ();
				var item = new MenuItem () {
					Title = "WorkerApp",
					CheckType = MenuItemCheckStyle.Checked
				};
				var top = Application.OverlappedChildren?.Find ((x) => x.Data.ToString () == "WorkerApp");
				if (top != null) {
					item.Checked = top.Visible;
				}
				item.Action += () => {
					var top = Application.OverlappedChildren.Find ((x) => x.Data.ToString () == "WorkerApp");
					item.Checked = top.Visible = (bool)!item.Checked;
					if (top.Visible) {
						Application.MoveToOverlappedChild (top);
					} else {
						Application.OverlappedTop.SetNeedsDisplay ();
					}
				};
				menuItems.Add (item);
				return new MenuBarItem ("_View",
					new List<MenuItem []> () { menuItems.Count == 0 ? new MenuItem [] { } : menuItems.ToArray () });
			}

			private MenuBarItem OpenedWindows ()
			{
				var index = 1;
				List<MenuItem> menuItems = new List<MenuItem> ();
				var sortedChildren = Application.OverlappedChildren;
				sortedChildren.Sort (new ToplevelComparer ());
				foreach (var top in sortedChildren) {
					if (top.Data.ToString () == "WorkerApp" && !top.Visible) {
						continue;
					}
					var item = new MenuItem ();
					item.Title = top is Window ? $"{index} {((Window)top).Title}" : $"{index} {top.Data}";
					index++;
					item.CheckType |= MenuItemCheckStyle.Checked;
					var topTitle = top is Window ? ((Window)top).Title : top.Data.ToString ();
					var itemTitle = item.Title.Substring (index.ToString ().Length + 1);
					if (top == Application.GetTopOverlappedChild () && topTitle == itemTitle) {
						item.Checked = true;
					} else {
						item.Checked = false;
					}
					item.Action += () => {
						Application.MoveToOverlappedChild (top);
					};
					menuItems.Add (item);
				}
				if (menuItems.Count == 0) {
					return new MenuBarItem ("_Window", "", null);
				} else {
					return new MenuBarItem ("_Window", new List<MenuItem []> () { menuItems.ToArray () });
				}
			}

			private void Quit ()
			{
				RequestStop ();
			}
		}

		class WorkerApp : Toplevel {
			private List<string> log = new List<string> ();
			private ListView listLog;
			private Dictionary<Staging, BackgroundWorker> stagingWorkers;
			private List<StagingUIController> stagingsUI;

			public WorkerApp ()
			{
				Data = "WorkerApp";

				Width = Dim.Percent (80);
				Height = Dim.Percent (50);

				ColorScheme = Colors.Base;

				var label = new Label ("Worker collection Log") {
					X = Pos.Center (),
					Y = 0
				};
				Add (label);

				listLog = new ListView (log) {
					X = 0,
					Y = Pos.Bottom (label),
					Width = Dim.Fill (),
					Height = Dim.Fill ()
				};
				Add (listLog);
			}

			public void RunWorker ()
			{
				var stagingUI = new StagingUIController () { Modal = true };

				Staging staging = null;
				var worker = new BackgroundWorker () { WorkerSupportsCancellation = true };

				worker.DoWork += (s, e) => {
					var stageResult = new List<string> ();
					for (int i = 0; i < 500; i++) {
						stageResult.Add (
							$"Worker {i} started at {DateTime.Now}");
						e.Result = stageResult;
						Thread.Sleep (1);
						if (worker.CancellationPending) {
							e.Cancel = true;
							return;
						}
					}
				};

				worker.RunWorkerCompleted += (s, e) => {
					if (e.Error != null) {
						// Failed
						WriteLog ($"Exception occurred {e.Error.Message} on Worker {staging.StartStaging}.{staging.StartStaging:fff} at {DateTime.Now}");
					} else if (e.Cancelled) {
						// Canceled
						WriteLog ($"Worker {staging.StartStaging}.{staging.StartStaging:fff} was canceled at {DateTime.Now}!");
					} else {
						// Passed
						WriteLog ($"Worker {staging.StartStaging}.{staging.StartStaging:fff} was completed at {DateTime.Now}.");
						Application.Refresh ();

						var stagingUI = new StagingUIController (staging, e.Result as List<string>) {
							Modal = false,
							Title = $"Worker started at {staging.StartStaging}.{staging.StartStaging:fff}",
							Data = $"{staging.StartStaging}.{staging.StartStaging:fff}"
						};

						stagingUI.ReportClosed += StagingUI_ReportClosed;

						if (stagingsUI == null) {
							stagingsUI = new List<StagingUIController> ();
						}
						stagingsUI.Add (stagingUI);
						stagingWorkers.Remove (staging);

						stagingUI.Run ();
					}
				};

				Application.Run (stagingUI);

				if (stagingUI.Staging != null && stagingUI.Staging.StartStaging != null) {
					staging = new Staging (stagingUI.Staging.StartStaging);
					WriteLog ($"Worker is started at {staging.StartStaging}.{staging.StartStaging:fff}");
					if (stagingWorkers == null) {
						stagingWorkers = new Dictionary<Staging, BackgroundWorker> ();
					}
					stagingWorkers.Add (staging, worker);
					worker.RunWorkerAsync ();
					stagingUI.Dispose ();
				}
			}

			private void StagingUI_ReportClosed (StagingUIController obj)
			{
				WriteLog ($"Report {obj.Staging.StartStaging}.{obj.Staging.StartStaging:fff} closed.");
				stagingsUI.Remove (obj);
			}

			public void CancelWorker ()
			{
				if (stagingWorkers == null || stagingWorkers.Count == 0) {
					WriteLog ($"Worker is not running at {DateTime.Now}!");
					return;
				}

				foreach (var sw in stagingWorkers) {
					var key = sw.Key;
					var value = sw.Value;
					if (!key.Completed) {
						value.CancelAsync ();
					}
					WriteLog ($"Worker {key.StartStaging}.{key.StartStaging:fff} is canceling at {DateTime.Now}!");

					stagingWorkers.Remove (sw.Key);
				}
			}

			public void WriteLog (string msg)
			{
				log.Add (msg);
				listLog.MoveDown ();
			}
		}

		class StagingUIController : Window {
			private Label label;
			private ListView listView;
			private Button start;
			private Button close;
			public Staging Staging { get; private set; }

			public event Action<StagingUIController> ReportClosed;

			public StagingUIController (Staging staging, List<string> list) : this ()
			{
				Staging = staging;
				label.Text = "Work list:";
				listView.SetSource (list);
				start.Visible = false;
				Id = "";
			}

			public StagingUIController ()
			{
				X = Pos.Center ();
				Y = Pos.Center ();
				Width = Dim.Percent (85);
				Height = Dim.Percent (85);

				ColorScheme = Colors.Dialog;

				Title = "Run Worker";

				label = new Label ("Press start to do the work or close to quit.") {
					X = Pos.Center (),
					Y = 1,
					ColorScheme = Colors.Dialog
				};
				Add (label);

				listView = new ListView () {
					X = 0,
					Y = 2,
					Width = Dim.Fill (),
					Height = Dim.Fill (2)
				};
				Add (listView);

				start = new Button ("Start") { IsDefault = true, ClearOnVisibleFalse = false };
				start.Clicked += (s, e) => {
					Staging = new Staging (DateTime.Now);
					RequestStop ();
				};
				Add (start);

				close = new Button ("Close");
				close.Clicked += OnReportClosed;
				Add (close);

				KeyPress += (s, e) => {
					if (e.KeyEvent.Key == Key.Esc) {
						OnReportClosed (this, EventArgs.Empty);
					}
				};

				LayoutStarted += (s,e) => {
					var btnsWidth = start.Frame.Width + close.Frame.Width + 2 - 1;
					var shiftLeft = Math.Max ((Bounds.Width - btnsWidth) / 2 - 2, 0);

					shiftLeft += close.Frame.Width + 1;
					close.X = Pos.AnchorEnd (shiftLeft);
					close.Y = Pos.AnchorEnd (1);

					shiftLeft += start.Frame.Width + 1;
					start.X = Pos.AnchorEnd (shiftLeft);
					start.Y = Pos.AnchorEnd (1);
				};
			}

			private void OnReportClosed (object sender, EventArgs e)
			{
				if (Staging?.StartStaging != null) {
					ReportClosed?.Invoke (this);
				}
				RequestStop ();
			}

			public void Run ()
			{
				Application.Run (this);
			}
		}

		class Staging {
			public DateTime? StartStaging { get; private set; }
			public bool Completed { get; }

			public Staging (DateTime? startStaging, bool completed = false)
			{
				StartStaging = startStaging;
				Completed = completed;
			}
		}
	}
}
