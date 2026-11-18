#nullable enable

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Single BackgroundWorker", "A single BackgroundWorker threading opening another Runnable")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Runnable")]
public class SingleBackgroundWorker : Scenario
{
    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();
        app.Run<MainApp> ();
    }

    public class MainApp : Window
    {
        private readonly ListView _listLog;
        private readonly ObservableCollection<string> _log = [];
        private DateTime? _startStaging;
        private BackgroundWorker? _worker;

        public MainApp ()
        {
            BorderStyle = LineStyle.None;
            // MenuBar
            MenuBar menu = new ();

            menu.Add (
                      new MenuBarItem (
                                       "_Options",
                                       [
                                           new MenuItem
                                           {
                                               Title = "_Run Worker",
                                               Key = Key.R.WithCtrl,
                                               Action = RunWorker
                                           },
                                           new MenuItem
                                           {
                                               Title = Strings.cmdQuit,
                                               Key = Application.GetDefaultKey (Command.Quit),
                                               Action = () => App?.RequestStop ()
                                           }
                                       ]
                                      )
                     );

            // StatusBar
            StatusBar statusBar = new (
                                       [
                                           new (Application.GetDefaultKey (Command.Quit), "Quit", () => App?.RequestStop ()),
                                           new (Key.R.WithCtrl, "Run Worker", RunWorker)
                                       ]
                                      );

            Window workerLogTop = new ()
            {
                Title = "Worker Log Top",
                Y = Pos.Bottom (menu),
                Height = Dim.Fill (1)
            };

            workerLogTop.Add (
                              new Label { X = Pos.Center (), Y = 0, Text = "Worker Log" }
                             );

            _listLog = new ()
            {
                X = 0,
                Y = 2,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Source = new ListWrapper<string> (_log)
            };
            workerLogTop.Add (_listLog);

            Add (menu, workerLogTop, statusBar);
            Title = "MainApp";
        }

        private void RunWorker ()
        {
            _worker = new () { WorkerSupportsCancellation = true };

            Button cancel = new () { Text = "Cancel Worker" };

            cancel.Accepting += (_, _) =>
                                {
                                    if (_worker is null)
                                    {
                                        _log.Add ($"Worker is not running at {DateTime.Now}!");
                                        _listLog.SetNeedsDraw ();

                                        return;
                                    }

                                    _log.Add (
                                              $"Worker {_startStaging}.{_startStaging:fff} is canceling at {DateTime.Now}!"
                                             );
                                    _listLog.SetNeedsDraw ();
                                    _worker.CancelAsync ();
                                };

            _startStaging = DateTime.Now;
            _log.Add ($"Worker is started at {_startStaging}.{_startStaging:fff}");
            _listLog.SetNeedsDraw ();

            Dialog md = new ()
            {
                Title = $"Running Worker started at {_startStaging}.{_startStaging:fff}",
                Buttons = [cancel]
            };

            md.Add (
                    new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Wait for worker to finish..." }
                   );

            _worker.DoWork += (_, e) =>
                              {
                                  List<string> stageResult = [];

                                  for (var i = 0; i < 200; i++)
                                  {
                                      stageResult.Add ($"Worker {i} started at {DateTime.Now}");
                                      e.Result = stageResult;
                                      Thread.Sleep (1);

                                      if (_worker?.CancellationPending == true)
                                      {
                                          e.Cancel = true;

                                          return;
                                      }
                                  }
                              };

            _worker.RunWorkerCompleted += (_, e) =>
                                          {
                                              if (md.IsCurrentTop)
                                              {
                                                  //Close the dialog
                                                  md.RequestStop ();
                                              }

                                              if (e.Error is not null)
                                              {
                                                  // Failed
                                                  _log.Add (
                                                            $"Exception occurred {e.Error.Message} on Worker {_startStaging}.{_startStaging:fff} at {DateTime.Now}"
                                                           );
                                                  _listLog.SetNeedsDraw ();
                                              }
                                              else if (e.Cancelled)
                                              {
                                                  // Canceled
                                                  _log.Add (
                                                            $"Worker {_startStaging}.{_startStaging:fff} was canceled at {DateTime.Now}!"
                                                           );
                                                  _listLog.SetNeedsDraw ();
                                              }
                                              else
                                              {
                                                  // Passed
                                                  _log.Add (
                                                            $"Worker {_startStaging}.{_startStaging:fff} was completed at {DateTime.Now}."
                                                           );
                                                  _listLog.SetNeedsDraw ();
                                                  App?.LayoutAndDraw ();

                                                  StagingUIController builderUI =
                                                      new (_startStaging, e.Result as ObservableCollection<string>);
                                                  View? top = App?.TopRunnableView;

                                                  if (top is not null)
                                                  {
                                                      top.Visible = false;
                                                  }

                                                  builderUI.Load ();
                                                  builderUI.Dispose ();

                                                  if (top is not null)
                                                  {
                                                      top.Visible = true;
                                                  }
                                              }

                                              _worker = null;
                                          };
            _worker.RunWorkerAsync ();
            App?.Run (md);
            md.Dispose ();
        }
    }

    public class StagingUIController : Window
    {
        private Runnable? _top;

        public StagingUIController (DateTime? start, ObservableCollection<string>? list)
        {
            _top = new ()
            {
                Title = "_top",
                Width = Dim.Fill (),
                Height = Dim.Fill (),
            };

            _top.KeyDown += (_, e) =>
                            {
                                // Prevents App.QuitKey from closing this.
                                // Only Ctrl+C is allowed.
                                if (e == Application.GetDefaultKey (Command.Quit))
                                {
                                    e.Handled = true;
                                }
                            };

            bool Close ()
            {
                int? n = MessageBox.Query (App!,
                                          50,
                                          7,
                                          "Close Window.",
                                          "Are you sure you want to close this window?",
                                          "Yes",
                                          "No"
                                         );

                return n == 0;
            }

            // MenuBar
            MenuBar menu = new ();

            menu.Add (
                      new MenuBarItem (
                                       "_Stage",
                                       [
                                           new MenuItem
                                           {
                                               Title = Strings.cmdClose,
                                               Key = Key.C.WithCtrl,
                                               Action = () =>
                                                        {
                                                            if (Close ())
                                                            {
                                                                App?.RequestStop ();
                                                            }
                                                        }
                                           }
                                       ]
                                      )
                     );
            _top.Add (menu);

            // StatusBar
            StatusBar statusBar = new (
                                       [
                                           new (
                                                Key.C.WithCtrl,
                                                "Close",
                                                () =>
                                                {
                                                    if (Close ())
                                                    {
                                                        App?.RequestStop ();
                                                    }
                                                }
                                               )
                                       ]
                                      );
            _top.Add (statusBar);

            Y = Pos.Bottom (menu);
            Height = Dim.Fill (1);
            Title = $"Worker started at {start}.{start:fff}";
            SchemeName = "Base";

            if (list is not null)
            {
                Add (
                     new ListView
                     {
                         X = 0,
                         Y = 0,
                         Width = Dim.Fill (),
                         Height = Dim.Fill (),
                         Source = new ListWrapper<string> (list)
                     }
                    );
            }

            _top.Add (this);
        }

        public void Load ()
        {
            if (_top is not null)
            {
                App?.Run (_top);
                _top.Dispose ();
                _top = null;
            }
        }
    }
}
