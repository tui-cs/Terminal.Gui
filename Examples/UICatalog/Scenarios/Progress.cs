using System.Diagnostics;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Progress", "Shows off ProgressBar and Threading.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Threading")]
[ScenarioCategory ("Progress")]
public class Progress : Scenario
{
    private Window _win;
    private uint _mainLoopTimeoutTick = 100; // ms
    private object _mainLoopTimeout;
    private Timer _systemTimer;
    private uint _systemTimerTick = 100; // ms
    private IApplication _app;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window win = new ();
        win.Title = GetQuitKeyAndName ();
        _win = win;

        // Demo #1 - Use System.Timer (and threading)
        var systemTimerDemo = new ProgressDemo { X = 0, Y = 0, Width = Dim.Percent (100), Title = "System.Timer (threads)" };

        systemTimerDemo._startBtnClick = () =>
                                        {
                                            _systemTimer?.Dispose ();
                                            _systemTimer = null;

                                            systemTimerDemo.ActivityProgressBar.Fraction = 0F;
                                            systemTimerDemo.PulseProgressBar.Fraction = 0F;

                                            _systemTimer = new Timer (_ =>
                                                                      {
                                                                          // System.Threading.Timer callbacks run on thread-pool threads and can race
                                                                          // with shutdown. Invoke throws NotInitializedException once the
                                                                          // application is no longer initialized, and that exception would be
                                                                          // unhandled here and crash the process.
                                                                          if (_app is not { Initialized: true })
                                                                          {
                                                                              return;
                                                                          }

                                                                          try
                                                                          {
                                                                              _app.Invoke (_ => systemTimerDemo.Pulse ());
                                                                          }
                                                                          catch (NotInitializedException)
                                                                          {
                                                                              // A callback can pass the Initialized check before shutdown completes.
                                                                          }
                                                                      },
                                                                      null,
                                                                      0,
                                                                      _systemTimerTick);
                                        };

        systemTimerDemo._stopBtnClick = () =>
                                       {
                                           _systemTimer?.Dispose ();
                                           _systemTimer = null;

                                           systemTimerDemo.ActivityProgressBar.Fraction = 1F;
                                           systemTimerDemo.PulseProgressBar.Fraction = 1F;
                                       };
        systemTimerDemo.Speed.Text = $"{_systemTimerTick}";

        systemTimerDemo.Speed.TextChanged += (_, _) =>
                                             {
                                                 if (uint.TryParse (systemTimerDemo.Speed.Text, out uint result))
                                                 {
                                                     _systemTimerTick = result;
                                                     Debug.WriteLine ($"{_systemTimerTick}");

                                                     if (systemTimerDemo.Started)
                                                     {
                                                         systemTimerDemo.Start ();
                                                     }
                                                 }
                                                 else
                                                 {
                                                     Debug.WriteLine ("bad entry");
                                                 }
                                             };
        _win.Add (systemTimerDemo);

        // Demo #2 - Use Application.AddTimeout (no threads)
        var mainLoopTimeoutDemo = new ProgressDemo
        {
            X = 0, Y = Pos.Bottom (systemTimerDemo), Width = Dim.Percent (100), Title = "Application.AddTimer (no threads)"
        };

        mainLoopTimeoutDemo._startBtnClick = () =>
                                            {
                                                mainLoopTimeoutDemo._stopBtnClick ();

                                                mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 0F;
                                                mainLoopTimeoutDemo.PulseProgressBar.Fraction = 0F;

                                                _mainLoopTimeout = _app?.AddTimeout (TimeSpan.FromMilliseconds (_mainLoopTimeoutTick),
                                                                                     () =>
                                                                                     {
                                                                                         mainLoopTimeoutDemo.Pulse ();

                                                                                         return true;
                                                                                     });
                                            };

        mainLoopTimeoutDemo._stopBtnClick = () =>
                                           {
                                               if (_mainLoopTimeout != null)
                                               {
                                                   _app?.RemoveTimeout (_mainLoopTimeout);
                                                   _mainLoopTimeout = null;
                                               }

                                               mainLoopTimeoutDemo.ActivityProgressBar.Fraction = 1F;
                                               mainLoopTimeoutDemo.PulseProgressBar.Fraction = 1F;
                                           };

        mainLoopTimeoutDemo.Speed.Text = $"{_mainLoopTimeoutTick}";

        mainLoopTimeoutDemo.Speed.TextChanged += (_, _) =>
                                                 {
                                                     if (!uint.TryParse (mainLoopTimeoutDemo.Speed.Text, out uint result))
                                                     {
                                                         return;
                                                     }
                                                     _mainLoopTimeoutTick = result;

                                                     if (mainLoopTimeoutDemo.Started)
                                                     {
                                                         mainLoopTimeoutDemo.Start ();
                                                     }
                                                 };
        _win.Add (mainLoopTimeoutDemo);

        var startBoth = new Button { X = Pos.Center (), Y = Pos.Bottom (mainLoopTimeoutDemo) + 1, Text = "Start Both" };

        startBoth.Accepting += (_, _) =>
                               {
                                   systemTimerDemo.Start ();
                                   mainLoopTimeoutDemo.Start ();
                               };
        _win.Add (startBoth);

        // Stop the background timers as soon as the window stops running (e.g. on Quit), while the
        // application is still initialized. Waiting for Dispose is too late: by then the application
        // has been torn down (so Invoke throws) and this window's SubViews have been disposed (so the
        // Dispose-time cleanup below can no longer reach the demos). See issue #5474.
        win.IsRunningChanged += (_, e) =>
                                {
                                    if (e.Value)
                                    {
                                        return;
                                    }

                                    _systemTimer?.Dispose ();
                                    _systemTimer = null;

                                    if (_mainLoopTimeout is { })
                                    {
                                        _app?.RemoveTimeout (_mainLoopTimeout);
                                        _mainLoopTimeout = null;
                                    }
                                };

        app.Run (_win);
    }

    protected override void Dispose (bool disposing)
    {
        // Backstop: the System.Timer is normally stopped in win.IsRunningChanged (see Main). This
        // also covers the case where the scenario is disposed without ever having run.
        if (disposing)
        {
            _systemTimer?.Dispose ();
            _systemTimer = null;
        }

        base.Dispose (disposing);
    }

    private class ProgressDemo : FrameView
    {
        private const int VERTICAL_SPACE = 1;
        private readonly Action _pulseBtnClick = null;
        internal Action _startBtnClick;
        internal Action _stopBtnClick;

        private readonly Label _startedLabel;

        internal ProgressDemo ()
        {
            SchemeName = "Dialog";

            LeftFrame = new FrameView
            {
                X = 0,
                Y = 0,
                Height = Dim.Percent (100),
                Width = Dim.Percent (25),
                Title = "Settings"
            };
            Label lbl = new () { X = 1, Y = 1, Text = "Tick every (ms):" };
            LeftFrame.Add (lbl);
            Speed = new TextField { X = Pos.X (lbl), Y = Pos.Bottom (lbl), Width = 7 };
            LeftFrame.Add (Speed);

            Add (LeftFrame);

            Button startButton = new () { X = Pos.Right (LeftFrame) + 1, Y = 0, Text = "Start Timer" };
            startButton.Accepting += (_, _) => Start ();
            Button pulseButton = new () { X = Pos.Right (startButton) + 2, Y = Pos.Y (startButton), Text = "Pulse" };
            pulseButton.Accepting += (_, _) => Pulse ();

            Button stopButton = new () { X = Pos.Right (pulseButton) + 2, Y = Pos.Top (pulseButton), Text = "Stop Timer" };
            stopButton.Accepting += (_, _) => Stop ();

            Add (startButton);
            Add (pulseButton);
            Add (stopButton);

            ActivityProgressBar = new ProgressBar
            {
                X = Pos.Right (LeftFrame) + 1,
                Y = Pos.Bottom (startButton) + 1,
                Width = Dim.Fill () - 1,
                Height = 1,
                Fraction = 0.25F,
                SchemeName = "Error"
            };
            Add (ActivityProgressBar);

            Spinner = new SpinnerView { Style = new SpinnerStyle.Dots2 (), SpinReverse = true, Y = ActivityProgressBar.Y, Visible = false };
            ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
            Spinner.X = Pos.Right (ActivityProgressBar);

            Add (Spinner);

            PulseProgressBar = new ProgressBar
            {
                X = Pos.Right (LeftFrame) + 1,
                Y = Pos.Bottom (ActivityProgressBar) + 1,
                Width = Dim.Fill () - Spinner.Width,
                Height = 1,
                SchemeName = "Error"
            };
            Add (PulseProgressBar);

            _startedLabel = new Label { X = Pos.Right (LeftFrame) + 1, Y = Pos.Bottom (PulseProgressBar), Text = "Stopped" };
            Add (_startedLabel);

            // TODO: Great use of Dim.Auto
            Initialized += (_, _) =>
                           {
                               // Set height to height of controls + spacing + frame
                               Height = 2
                                        + VERTICAL_SPACE
                                        + startButton.Frame.Height
                                        + VERTICAL_SPACE
                                        + ActivityProgressBar.Frame.Height
                                        + VERTICAL_SPACE
                                        + PulseProgressBar.Frame.Height
                                        + VERTICAL_SPACE;
                           };
        }

        internal ProgressBar ActivityProgressBar { get; }
        private FrameView LeftFrame { get; }
        internal ProgressBar PulseProgressBar { get; }
        internal TextField Speed { get; }
        private SpinnerView Spinner { get; }

        internal bool Started { get => _startedLabel.Text == "Started"; private set => _startedLabel.Text = value ? "Started" : "Stopped"; }

        internal void Pulse ()
        {
            Spinner.Visible = true;

            if (_pulseBtnClick != null)
            {
                _pulseBtnClick?.Invoke ();
            }
            else
            {
                if (ActivityProgressBar.Fraction + 0.01F >= 1)
                {
                    ActivityProgressBar.Fraction = 0F;
                }
                else
                {
                    ActivityProgressBar.Fraction += 0.01F;
                }

                PulseProgressBar.Pulse ();
                Spinner.AdvanceAnimation ();
            }
        }

        internal void Start ()
        {
            Started = true;
            _startBtnClick?.Invoke ();

            App?.Invoke (() =>
                         {
                             Spinner.Visible = true;
                             ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
                         });
        }

        private void Stop ()
        {
            Started = false;
            _stopBtnClick?.Invoke ();

            App?.Invoke (() =>
                         {
                             Spinner.Visible = false;
                             ActivityProgressBar.Width = Dim.Fill () - Spinner.Width;
                         });
        }
    }
}
