#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("OSC 9;4 Progress Indicator", "Demonstrates standalone terminal progress using OSC 9;4.")]
[ScenarioCategory ("Progress")]
public sealed class Osc94ProgressIndicator : Scenario
{
    private IApplication? _app;
    private Timer? _progressTimer;
    private Label? _availabilityLabel;
    private Label? _stateLabel;
    private Label? _valueLabel;
    private int _progressValue;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();
        appWindow.AssignHotKeys = true;

        appWindow.IsRunningChanged += Win_IsRunningChanged;

        Label summaryLabel = new () { X = 1, Y = 1, Text = "Standalone terminal progress demo. No ProgressBar view here." };
        appWindow.Add (summaryLabel);

        Label terminalLabel = new ()
        {
            X = 1, Y = Pos.Bottom (summaryLabel), Text = "Use buttons below and watch terminal tab/taskbar progress if host supports OSC 9;4."
        };
        appWindow.Add (terminalLabel);

        _availabilityLabel = new Label { X = 1, Y = Pos.Bottom (terminalLabel) + 1, Text = BuildAvailabilityText () };
        appWindow.Add (_availabilityLabel);

        _stateLabel = new Label { X = 1, Y = Pos.Bottom (_availabilityLabel) + 1, Text = "State: Cleared" };
        appWindow.Add (_stateLabel);

        _valueLabel = new Label { X = 1, Y = Pos.Bottom (_stateLabel), Text = "Value: 0%" };
        appWindow.Add (_valueLabel);

        Button startDeterminateButton = new () { X = 1, Y = Pos.Bottom (_valueLabel) + 2, Text = "Start _Determinate Timer" };
        startDeterminateButton.Accepting += (_, _) => StartDeterminateTimer ();
        appWindow.Add (startDeterminateButton);

        Button set25Button = new () { X = Pos.Right (startDeterminateButton) + 2, Y = Pos.Top (startDeterminateButton), Text = "Set _25%" };
        set25Button.Accepting += (_, _) => SetDeterminateValue (25);
        appWindow.Add (set25Button);

        Button set50Button = new () { X = Pos.Right (set25Button) + 2, Y = Pos.Top (set25Button), Text = "Set _50%" };
        set50Button.Accepting += (_, _) => SetDeterminateValue (50);
        appWindow.Add (set50Button);

        Button set75Button = new () { X = Pos.Right (set50Button) + 2, Y = Pos.Top (set50Button), Text = "Set _75%" };
        set75Button.Accepting += (_, _) => SetDeterminateValue (75);
        appWindow.Add (set75Button);

        Button indeterminateButton = new () { X = 1, Y = Pos.Bottom (startDeterminateButton) + 1, Text = "Set _Indeterminate" };
        indeterminateButton.Accepting += (_, _) => SetIndeterminate ();
        appWindow.Add (indeterminateButton);

        Button pausedButton = new () { X = Pos.Right (indeterminateButton) + 2, Y = Pos.Top (indeterminateButton), Text = "Set _Paused" };
        pausedButton.Accepting += (_, _) => SetPaused ();
        appWindow.Add (pausedButton);

        Button errorButton = new () { X = Pos.Right (pausedButton) + 2, Y = Pos.Top (indeterminateButton), Text = "Set _Error" };
        errorButton.Accepting += (_, _) => SetError ();
        appWindow.Add (errorButton);

        Button clearButton = new () { X = Pos.Right (errorButton) + 2, Y = Pos.Top (indeterminateButton), Text = "_Clear" };
        clearButton.Accepting += (_, _) => ClearIndicator ();
        appWindow.Add (clearButton);

        UpdateStatus ("Cleared");

        app.Run (appWindow);
    }

    private void StartDeterminateTimer ()
    {
        StopProgressTimer ();
        _progressValue = 0;
        ApplyDeterminateValue ("Determinate timer running");

        _progressTimer = new Timer (_ => _app?.Invoke (_ => AdvanceDeterminate ()), null, 0, 150);
    }

    private void AdvanceDeterminate ()
    {
        _progressValue = Math.Min (_progressValue + 5, 100);
        ApplyDeterminateValue ("Determinate timer running");

        if (_progressValue < 100)
        {
            return;
        }
        StopProgressTimer ();
        UpdateStatus ("Determinate complete");
    }

    private void SetDeterminateValue (int value)
    {
        StopProgressTimer ();
        _progressValue = value;
        ApplyDeterminateValue ($"Determinate {value}%");
    }

    private void ApplyDeterminateValue (string state)
    {
        _app?.Driver?.ProgressIndicator?.SetValue (_progressValue);
        UpdateStatus (state);
    }

    private void SetIndeterminate ()
    {
        StopProgressTimer ();
        _app?.Driver?.ProgressIndicator?.SetIndeterminate ();
        UpdateStatus ("Indeterminate");
    }

    private void SetPaused ()
    {
        StopProgressTimer ();
        _app?.Driver?.ProgressIndicator?.SetPaused (_progressValue);
        UpdateStatus ("Paused");
    }

    private void SetError ()
    {
        StopProgressTimer ();
        _app?.Driver?.ProgressIndicator?.SetError (_progressValue);
        UpdateStatus ("Error");
    }

    private void ClearIndicator ()
    {
        StopProgressTimer ();
        _app?.Driver?.ProgressIndicator?.Clear ();
        UpdateStatus ("Cleared");
    }

    private void StopProgressTimer ()
    {
        _progressTimer?.Dispose ();
        _progressTimer = null;
    }

    private string BuildAvailabilityText ()
    {
        string availability = _app?.Driver?.ProgressIndicator is null ? "Unavailable" : "Available";
        string driverName = _app?.Driver?.GetName () ?? "Unknown";

        return $"Driver: {driverName}. Terminal progress: {availability}.";
    }

    private void UpdateStatus (string state)
    {
        _availabilityLabel?.Text = BuildAvailabilityText ();

        _stateLabel?.Text = $"State: {state}";

        _valueLabel?.Text = $"Value: {_progressValue}%";
    }

    private void Win_IsRunningChanged (object? sender, EventArgs<bool> args)
    {
        if (args.Value)
        {
            return;
        }

        StopProgressTimer ();
        _app?.Driver?.ProgressIndicator?.Clear ();

        if (sender is Window appWindow)
        {
            appWindow.IsRunningChanged -= Win_IsRunningChanged;
        }
    }
}
