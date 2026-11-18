#nullable enable
using System.Collections.ObjectModel;
using System.Text;
using Terminal.Gui.Editor;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("AnsiEscapeSequenceRequest", "Ansi Escape Sequence Request")]
[ScenarioCategory ("Tests")]
public sealed class AnsiEscapeSequenceRequests : Scenario
{
    private readonly List<DateTime> _sends = [];

    private readonly Lock _lockAnswers = new ();
    private readonly Dictionary<DateTime, string> _answers = new ();
    private GraphView? _graphView;

    private ScatterSeries? _sentSeries;
    private ScatterSeries? _answeredSeries;
    private Label? _lblSummary;

    private object? _updateTimeoutToken;
    private object? _sendDarTimeoutToken;
    private IApplication? _app;

    public override void Main ()
    {
        // Init
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        // Restored: Tabs with Single and Bulk tabs (#4183)
        View singleView = BuildSingleTab ();
        View bulkView = BuildBulkTab ();

        // Setup - Create a top-level application window and configure it.
        using Window appWindow = new ();
        appWindow.Title = GetQuitKeyAndName ();

        Tabs tabs = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        View singleTab = new () { Title = "_Single" };
        singleView.Width = Dim.Fill ();
        singleView.Height = Dim.Fill ();
        singleTab.Add (singleView);

        View bulkTab = new () { Title = "_Bulk" };
        bulkView.Width = Dim.Fill ();
        bulkView.Height = Dim.Fill ();
        bulkTab.Add (bulkView);

        tabs.Add (singleTab, bulkTab);
        tabs.Value = singleTab;

        appWindow.Add (tabs);

        // Run - Start the application.
        app.Run (appWindow);
        tabs.Dispose ();

        _app.RemoveTimeout (_updateTimeoutToken!);
        _app.RemoveTimeout (_sendDarTimeoutToken!);
    }

    private View BuildBulkTab ()
    {
        var w = new View { Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };

        var lbl = new Label
        {
            Text =
                "This scenario tests Ansi request/response processing. Use the Editor to ensure regular user interaction continues as normal during sends",
            Height = 2,
            Width = Dim.Fill ()
        };

        _updateTimeoutToken = _app!.AddTimeout (TimeSpan.FromMilliseconds (1000),
                                                () =>
                                                {
                                                    lock (_lockAnswers)
                                                    {
                                                        UpdateGraph ();

                                                        UpdateResponses ();
                                                    }

                                                    return true;
                                                });

        Editor editor = new () { Y = Pos.Bottom (lbl), Width = Dim.Percent (50), Height = Dim.Fill () };

        var lblDar = new Label { Y = Pos.Bottom (lbl), X = Pos.Right (editor) + 1, Text = "DAR per second" };
        var cbDar = new NumericUpDown { X = Pos.Right (lblDar), Y = Pos.Bottom (lbl), Value = 0 };

        cbDar.ValueChanging += (_, e) =>
                               {
                                   if (e.NewValue is < 0 or > 20)
                                   {
                                       e.Handled = true;
                                   }
                               };
        w.Add (cbDar);

        int lastSendTime = Environment.TickCount;
        var lockObj = new object ();

        _sendDarTimeoutToken = _app.AddTimeout (TimeSpan.FromMilliseconds (50),
                                                () =>
                                                {
                                                    lock (lockObj)
                                                    {
                                                        switch (cbDar.Value)
                                                        {
                                                            case > 0:
                                                            {
                                                                int interval = 1000 / cbDar.Value; // Calculate the desired interval in milliseconds
                                                                int currentTime = Environment.TickCount; // Current system time in milliseconds

                                                                // Check if the time elapsed since the last send is greater than the interval
                                                                if (currentTime - lastSendTime >= interval)
                                                                {
                                                                    SendDar (); // Send the request
                                                                    lastSendTime = currentTime; // Update the last send time
                                                                }

                                                                break;
                                                            }
                                                        }
                                                    }

                                                    return true;
                                                });

        _graphView = new GraphView { Y = Pos.Bottom (cbDar), X = Pos.Right (editor), Width = Dim.Fill (), Height = Dim.Fill (1) };

        _lblSummary = new Label { Y = Pos.Bottom (_graphView), X = Pos.Right (editor), Width = Dim.Fill () };

        SetupGraph ();

        w.Add (lbl);
        w.Add (lblDar);
        w.Add (cbDar);
        w.Add (editor);
        w.Add (_graphView);
        w.Add (_lblSummary);

        return w;
    }

    private View BuildSingleTab ()
    {
        var w = new View { Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };

        w.Padding.Thickness = new Thickness (1);

        List<string> scrRequests =
        [
            "CSI_SendDeviceAttributes",
            "CSI_ReportTerminalSizeInChars",
            "CSI_RequestCursorPositionReport",
            "CSI_SendDeviceAttributes2",
            "CSI_RequestWindowSizeInPixels",
            "CSI_RequestSixelResolution"
        ];

        // TODO: This UI would be cleaner/less rigid if Pos.Align were used
        DropDownList cbRequests = new ()
        {
            Title = "Ansi Escape Sequence",
            BorderStyle = LineStyle.Dotted,
            ReadOnly = true,
            Source = new ListWrapper<string> (new ObservableCollection<string> (scrRequests)),
            Text = scrRequests [0]
        };
        w.Add (cbRequests);

        var label = new Label { Y = Pos.Bottom (cbRequests) + 1, Text = "Request:" };
        var tfRequest = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 20 };
        w.Add (label, tfRequest);

        label = new Label { X = Pos.Right (tfRequest) + 1, Y = Pos.Top (tfRequest) - 1, Text = "Value:" };
        var tfValue = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6 };
        w.Add (label, tfValue);

        label = new Label { X = Pos.Right (tfValue) + 1, Y = Pos.Top (tfValue) - 1, Text = "Terminator:" };
        var tfTerminator = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4 };
        w.Add (label, tfTerminator);

        cbRequests.ValueChanged += (_, args) =>
                                   {
                                       if (string.IsNullOrEmpty (args.NewValue))
                                       {
                                           return;
                                       }

                                       string selAnsiEscapeSequenceRequestName = args.NewValue;
                                       AnsiEscapeSequence? selAnsiEscapeSequenceRequest = null;

                                       switch (selAnsiEscapeSequenceRequestName)
                                       {
                                           case "CSI_SendDeviceAttributes":
                                               selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_SendDeviceAttributes;

                                               break;

                                           case "CSI_ReportTerminalSizeInChars":
                                               selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_ReportWindowSizeInChars;

                                               break;

                                           case "CSI_RequestCursorPositionReport":
                                               selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_RequestCursorPositionReport;

                                               break;

                                           case "CSI_SendDeviceAttributes2":
                                               selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_SendDeviceAttributes2;

                                               break;

                                           case "CSI_RequestWindowSizeInPixels":
                                               selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_RequestWindowSizeInPixels;

                                               break;

                                           case "CSI_RequestSixelResolution":
                                               selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_RequestSixelResolution;

                                               break;
                                       }

                                       tfRequest.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Request : "";
                                       tfValue.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Value ?? "" : "";
                                       tfTerminator.Text = (selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Terminator : "")!;
                                   };

        label = new Label { Y = Pos.Bottom (tfRequest) + 2, Text = "Response:" };

        var tvResponse = new Code
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 40,
            Height = 4,
            SyntaxHighlighter = null
        };
        w.Add (label, tvResponse);

        label = new Label { X = Pos.Right (tvResponse) + 1, Y = Pos.Top (tvResponse) - 1, Text = "Error:" };

        var tvError = new Code
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 40,
            Height = 4,
            SyntaxHighlighter = null
        };
        w.Add (label, tvError);

        label = new Label { X = Pos.Right (tvError) + 1, Y = Pos.Top (tvError) - 1, Text = "Value:" };

        var tvValue = new Code
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 6,
            Height = 4,
            SyntaxHighlighter = null
        };
        w.Add (label, tvValue);

        label = new Label { X = Pos.Right (tvValue) + 1, Y = Pos.Top (tvValue) - 1, Text = "Terminator:" };

        var tvTerminator = new Code
        {
            X = Pos.Left (label),
            Y = Pos.Bottom (label),
            Width = 4,
            Height = 4,
            SyntaxHighlighter = null
        };
        w.Add (label, tvTerminator);

        var btnResponse = new Button { X = Pos.Center (), Y = Pos.Bottom (tvResponse) + 2, Text = "Send Request", IsDefault = true };

        var lblSuccess = new Label { X = Pos.Center (), Y = Pos.Bottom (btnResponse) + 1 };
        w.Add (lblSuccess);

        btnResponse.Accepting += (_, _) =>
                                 {
                                     var ansiEscapeSequenceRequest = new AnsiEscapeSequence
                                     {
                                         Request = tfRequest.Text,
                                         Terminator = tfTerminator.Text,
                                         Value = string.IsNullOrEmpty (tfValue.Text) ? null : tfValue.Text
                                     };

                                     _app?.Driver?.QueueAnsiRequest (new AnsiEscapeSequenceRequest
                                     {
                                         Request = ansiEscapeSequenceRequest.Request,
                                         Terminator = ansiEscapeSequenceRequest.Terminator,
                                         ResponseReceived =
                                             s => OnSuccess (s!, tvResponse, tvError, tvValue, tvTerminator, lblSuccess),
                                         Abandoned = () => OnFail (tvResponse, tvError, tvValue, tvTerminator, lblSuccess)
                                     });
                                 };

        w.Add (btnResponse);

        w.Add (new Label { Y = Pos.Bottom (lblSuccess) + 2, Text = "You can send other requests by editing the TextFields." });

        return w;
    }

    private string GetSummary ()
    {
        if (_answers.Count == 0)
        {
            return "No requests sent yet";
        }

        int totalSends = _sends.Count;

        string last = _answers.Last ().Value;

        int unique = _answers.Values.Distinct ().Count ();
        int totalAnswers = _answers.Count;

        return $"TS:{totalSends} Last:{last} U:{unique} TA:{totalAnswers}";
    }

    private void HandleResponse (string response)
    {
        lock (_lockAnswers)
        {
            _answers.Add (DateTime.Now, response);
        }
    }

    private void OnFail (Code tvResponse, Code tvError, Code tvValue, Code tvTerminator, Label lblSuccess)
    {
        tvResponse.Text = string.Empty;
        tvError.Text = "No Response";
        tvValue.Text = string.Empty;
        tvTerminator.Text = string.Empty;

        lblSuccess.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Error);
        lblSuccess.Text = "Error";
    }

    private void OnSuccess (string response, Code tvResponse, Code tvError, Code tvValue, Code tvTerminator, Label lblSuccess)
    {
        tvResponse.Text = response;
        tvError.Text = string.Empty;
        tvValue.Text = string.Empty;
        tvTerminator.Text = string.Empty;

        lblSuccess.SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
        lblSuccess.Text = "Successful";
    }

    private void SendDar ()
    {
        _app?.Driver?.QueueAnsiRequest (new AnsiEscapeSequenceRequest
        {
            Request = EscSeqUtils.CSI_SendDeviceAttributes.Request,
            Value = EscSeqUtils.CSI_SendDeviceAttributes.Value,
            Terminator = EscSeqUtils.CSI_SendDeviceAttributes.Terminator,
            ResponseReceived = HandleResponse!
        });

        _sends.Add (DateTime.Now);
    }

    private void SetupGraph ()
    {
        _graphView!.Series.Add (_sentSeries = new ScatterSeries ());
        _graphView!.Series.Add (_answeredSeries = new ScatterSeries ());

        _sentSeries.Fill = new GraphCellToRender (new Rune ('.'), new Attribute (ColorName16.BrightGreen, ColorName16.Black));
        _answeredSeries.Fill = new GraphCellToRender (new Rune ('.'), new Attribute (ColorName16.BrightRed, ColorName16.Black));

        // Todo:
        // _graphView.Annotations.Add (_sentSeries new PathAnnotation {});

        _graphView.CellSize = new PointF (1, 1);
        _graphView.MarginBottom = 2;
        _graphView.AxisX.Increment = 1;
        _graphView.AxisX.Text = "Seconds";
        _graphView.GraphColor = new Attribute (Color.Green, Color.Black);
    }

    private int ToSeconds (DateTime t) => (int)(DateTime.Now - t).TotalSeconds;

    private void UpdateGraph ()
    {
        _sentSeries!.Points = _sends.GroupBy (ToSeconds).Select (g => new PointF (g.Key, g.Count ())).ToList ();

        _answeredSeries!.Points = _answers.Keys.GroupBy (ToSeconds).Select (g => new PointF (g.Key, g.Count ())).ToList ();

        //  _graphView.ScrollOffset  = new PointF(,0);
        _graphView!.SetNeedsDraw ();
    }

    private void UpdateResponses ()
    {
        _lblSummary!.Text = GetSummary ();
        _lblSummary.SetNeedsDraw ();
    }
}
