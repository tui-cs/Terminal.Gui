using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Threading", "Demonstration of how to use threading in different ways")]
[ScenarioCategory ("Threading")]
public class Threading : Scenario
{
    private IApplication _app;
    private readonly ObservableCollection<string> _log = [];
    private Action _action;
    private Button _btnActionCancel;
    private CancellationTokenSource _cancellationTokenSource;
    private EventHandler _handler;
    private ListView _itemsList;
    private Action _lambda;
    private ListView _logJob;
    private Action _sync;

    private LogarithmicTimeout _logarithmicTimeout;
    private NumericUpDown _numberLog;
    private Button _btnLogarithmic;
    private object _timeoutObj;

    private SmoothAcceleratingTimeout _smoothTimeout;
    private NumericUpDown _numberSmooth;
    private Button _btnSmooth;
    private object _timeoutObjSmooth;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window win = new () { Title = GetQuitKeyAndName () };
        _action = LoadData;

        _lambda = async () =>
                  {
                      _itemsList.Source = null;
                      LogJob ("Loading task lambda");
                      ObservableCollection<string> items = await LoadDataAsync ();
                      LogJob ("Returning from task lambda");
                      await _itemsList.SetSourceAsync (items);
                  };

        _handler = async (_, _) =>
                   {
                       _itemsList.Source = null;
                       LogJob ("Loading task handler");
                       ObservableCollection<string> items = await LoadDataAsync ();
                       LogJob ("Returning from task handler");
                       await _itemsList.SetSourceAsync (items);
                   };

        _sync = () =>
                {
                    _itemsList.Source = null;
                    LogJob ("Loading task synchronous");

                    ObservableCollection<string> items =
                        ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
                    LogJob ("Returning from task synchronous");
                    _itemsList.SetSource (items);
                };

        _btnActionCancel = new Button { X = 1, Y = 1, Text = "Cancelable Load Items" };
        _btnActionCancel.Accepting += (_, _) => _app?.Invoke (CallLoadItemsAsync);

        win.Add (new Label { X = Pos.X (_btnActionCancel), Y = Pos.Y (_btnActionCancel) + 4, Text = "Data Items:" });

        _itemsList = new ListView
        {
            X = Pos.X (_btnActionCancel),
            Y = Pos.Y (_btnActionCancel) + 6,
            Width = 10,
            Height = 10,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent)
        };

        win.Add (new Label { X = Pos.Right (_itemsList) + 10, Y = Pos.Y (_btnActionCancel) + 4, Text = "Task Logs:" });

        _logJob = new ListView
        {
            X = Pos.Right (_itemsList) + 10,
            Y = Pos.Y (_itemsList),
            Width = 50,
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Source = new ListWrapper<string> (_log)
        };

        TextField text = new () { X = 1, Y = 3, Width = 100, Text = "Type anything after press the button" };

        _btnLogarithmic = new Button ()
        {
            X = 50,
            Y = 4,
            Text = "Start Log Counter"
        };
        _btnLogarithmic.Accepting += StartStopLogTimeout;

        _numberLog = new NumericUpDown ()
        {
            X = Pos.Right (_btnLogarithmic),
            Y = 4,
        };

        _btnSmooth = new Button ()
        {
            X = Pos.Right (_numberLog),
            Y = 4,
            Text = "Start Smooth Counter"
        };
        _btnSmooth.Accepting += StartStopSmoothTimeout;

        _numberSmooth = new NumericUpDown ()
        {
            X = Pos.Right (_btnSmooth),
            Y = 4,
        };


        Button btnAction = new () { X = 80, Y = 10, Text = "Load Data Action" };
        btnAction.Accepted += (_, _) => _action.Invoke ();
        Button btnLambda = new () { X = 80, Y = 12, Text = "Load Data Lambda" };
        btnLambda.Accepted += (_, _) => _lambda.Invoke ();
        Button btnHandler = new () { X = 80, Y = 14, Text = "Load Data Handler" };
        btnHandler.Accepted += (_, _) => _handler.Invoke (null, EventArgs.Empty);
        Button btnSync = new () { X = 80, Y = 16, Text = "Load Data Synchronous" };
        btnSync.Accepted += (_, _) => _sync.Invoke ();
        Button btnMethod = new () { X = 80, Y = 18, Text = "Load Data Method" };
        btnMethod.Accepted += async (_, _) => await MethodAsync ();
        Button btnClearData = new () { X = 80, Y = 20, Text = "Clear Data" };

        btnClearData.Accepted += (_, _) =>
                                {
                                    _itemsList.Source = null;
                                    LogJob ("Cleaning Data");
                                };
        Button btnQuit = new () { X = 80, Y = 22, Text = "Quit" };
        btnQuit.Accepted += (_, _) => win.RequestStop ();

        win.Add (
                 _itemsList,
                 _btnActionCancel,
                 _logJob,
                 text,
                 _btnLogarithmic,
                 _numberLog,
                 _btnSmooth,
                 _numberSmooth,
                 btnAction,
                 btnLambda,
                 btnHandler,
                 btnSync,
                 btnMethod,
                 btnClearData,
                 btnQuit
                );

        void WinLoaded (object sender, EventArgs args)
        {
            _btnActionCancel.SetFocus ();
            win.IsModalChanged -= WinLoaded;
        }

        win.IsModalChanged += WinLoaded;

        app.Run (win);
    }

    private bool LogTimeout ()
    {
        _numberLog.Value++;
        _logarithmicTimeout.AdvanceStage ();
        return true;
    }
    private bool SmoothTimeout ()
    {
        _numberSmooth.Value++;
        _smoothTimeout.AdvanceStage ();
        return true;
    }

    private void StartStopLogTimeout (object sender, CommandEventArgs e)
    {
        if (_timeoutObj != null)
        {
            _btnLogarithmic.Text = "Start Log Counter";
            _app?.TimedEvents?.Remove (_timeoutObj);
            _timeoutObj = null;
        }
        else
        {
            _btnLogarithmic.Text = "Stop Log Counter";
            _logarithmicTimeout = new LogarithmicTimeout (TimeSpan.FromMilliseconds (500), LogTimeout);
            _timeoutObj = (_app?.TimedEvents!).Add (_logarithmicTimeout);
        }
    }

    private void StartStopSmoothTimeout (object sender, CommandEventArgs e)
    {
        if (_timeoutObjSmooth != null)
        {
            _btnSmooth.Text = "Start Smooth Counter";
            _app?.TimedEvents?.Remove (_timeoutObjSmooth);
            _timeoutObjSmooth = null;
        }
        else
        {
            _btnSmooth.Text = "Stop Smooth Counter";
            _smoothTimeout = new SmoothAcceleratingTimeout (TimeSpan.FromMilliseconds (500), TimeSpan.FromMilliseconds (50), 0.5, SmoothTimeout);
            _timeoutObjSmooth = _app?.TimedEvents?.Add (_smoothTimeout);
        }
    }

    private async void CallLoadItemsAsync ()
    {
        _cancellationTokenSource = new CancellationTokenSource ();
        _itemsList.Source = null;
        LogJob ("Clicked the button");

        if (_btnActionCancel.Text != "Cancel")
        {
            _btnActionCancel.Text = "Cancel";
        }
        else
        {
            _btnActionCancel.Text = "Cancelable Load Items";
            await _cancellationTokenSource.CancelAsync ();
        }

        try
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested ();
            }

            LogJob ($"Calling task Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
            ObservableCollection<string> items = await Task.Run (LoadItemsAsync, _cancellationTokenSource.Token);

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                LogJob (
                        $"Returned from task Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}"
                       );
                await _itemsList.SetSourceAsync (items);

                LogJob (
                        $"Finished populate list view Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}"
                       );
                _btnActionCancel.Text = "Cancelable Load Items";
            }
            else
            {
                LogJob ("Task was canceled!");
            }
        }
        catch (OperationCanceledException ex)
        {
            LogJob (ex.Message);
        }
    }

    private async void LoadData ()
    {
        _itemsList.Source = null;
        LogJob ("Loading task");
        ObservableCollection<string> items = await LoadDataAsync ();
        LogJob ("Returning from task");
        await _itemsList.SetSourceAsync (items);
    }

    private async Task<ObservableCollection<string>> LoadDataAsync ()
    {
        _itemsList.Source = null;
        LogJob ("Starting delay");
        await Task.Delay (3000);
        LogJob ("Finished delay");

        return
        [
            "One",
            "Two",
            "Three",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "Ten",
            "Four",
            "Five",
            "Six",
            "Seven",
            "Eight",
            "Nine",
            "Ten"
        ];
    }

    private async Task<ObservableCollection<string>> LoadItemsAsync ()
    {
        // Do something that takes lot of times.
        LogJob ($"Starting delay Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");
        await Task.Delay (5000);
        LogJob ($"Finished delay Thread:{Thread.CurrentThread.ManagedThreadId} {DateTime.Now}");

        return ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
    }

    private void LogJob (string job)
    {
        _log.Add (job);
        _logJob.MoveDown ();
    }

    private async Task MethodAsync ()
    {
        _itemsList.Source = null;
        LogJob ("Loading task method");
        ObservableCollection<string> items = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten"];
        await Task.Delay (3000);
        LogJob ("Returning from task method");
        await _itemsList.SetSourceAsync (items);
        _itemsList.SetNeedsDraw ();
    }
}
