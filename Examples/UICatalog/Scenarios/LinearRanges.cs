using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("LinearRanges", "Demonstrates the LinearSelector / LinearMultiSelector / LinearRange views.")]
[ScenarioCategory ("Controls")]
public class LinearRanges : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window mainWindow = new ();
        mainWindow.Title = GetQuitKeyAndName ();

        ObservableCollection<string> eventSource = [];

        ListView eventLog = new ()
        {
            X = Pos.Percent (50),
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent),
            Title = "Events",
            BorderStyle = LineStyle.Single,
            Source = new ListWrapper<string> (eventSource)
        };
        mainWindow.Add (eventLog);

        // ---- LinearSelector<int> ---------------------------------------------------------------
        LinearSelector<int> single = new ([10, 20, 30, 40, 50])
        {
            Title = "_Single (LinearSelector<int>)",
            X = 0,
            Y = 0,
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            AllowEmpty = false
        };
        single.Value = 30;

        single.ValueChanged += (_, args) =>
                               {
                                   eventSource.Add ($"Single ValueChanged: {args.OldValue} -> {args.NewValue}");
                                   eventLog.MoveDown ();
                               };
        mainWindow.Add (single);

        // ---- LinearMultiSelector<string> -------------------------------------------------------
        LinearMultiSelector<string> multi = new (["Red", "Green", "Blue", "Yellow"])
        {
            Title = "_Multiple (LinearMultiSelector<string>)",
            X = 0,
            Y = Pos.Bottom (single),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            AllowEmpty = true
        };
        multi.Value = ["Red", "Blue"];

        multi.ValueChanged += (_, args) =>
                              {
                                  eventSource.Add ($"Multi ValueChanged: [{string.Join (",", args.NewValue ?? [])}]");
                                  eventLog.MoveDown ();
                              };
        mainWindow.Add (multi);

        // ---- LinearRange<int> (closed) ---------------------------------------------------------
        LinearRange<int> closed = new ([1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
        {
            Title = "_Closed (LinearRange<int>)",
            X = 0,
            Y = Pos.Bottom (multi),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            AllowEmpty = true,
            RangeAllowSingle = true,
            RangeKind = LinearRangeSpanKind.Closed
        };

        closed.ValueChanged += (_, args) =>
                               {
                                   LinearRangeSpan<int> v = args.NewValue;

                                   eventSource.Add (
                                                    $"Closed ValueChanged: kind={v.Kind} start={v.Start} end={v.End} (idx {v.StartIndex}..{v.EndIndex})");
                                   eventLog.MoveDown ();
                               };
        mainWindow.Add (closed);

        // ---- LinearRange<int> (left-bounded) ---------------------------------------------------
        LinearRange<int> leftBounded = new ([1, 2, 3, 4, 5])
        {
            Title = "_LeftBounded (LinearRange<int>)",
            X = 0,
            Y = Pos.Bottom (closed),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            RangeKind = LinearRangeSpanKind.LeftBounded,
            AllowEmpty = true
        };

        leftBounded.ValueChanged += (_, args) =>
                                    {
                                        eventSource.Add ($"LeftBounded ValueChanged: end={args.NewValue.End} (idx {args.NewValue.EndIndex})");
                                        eventLog.MoveDown ();
                                    };
        mainWindow.Add (leftBounded);

        // ---- LinearRange<int> (right-bounded) --------------------------------------------------
        LinearRange<int> rightBounded = new ([1, 2, 3, 4, 5])
        {
            Title = "_RightBounded (LinearRange<int>)",
            X = 0,
            Y = Pos.Bottom (leftBounded),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            RangeKind = LinearRangeSpanKind.RightBounded,
            AllowEmpty = true
        };

        rightBounded.ValueChanged += (_, args) =>
                                     {
                                         eventSource.Add ($"RightBounded ValueChanged: start={args.NewValue.Start} (idx {args.NewValue.StartIndex})");
                                         eventLog.MoveDown ();
                                     };
        mainWindow.Add (rightBounded);

        mainWindow.FocusDeepest (NavigationDirection.Forward, null);

        app.Run (mainWindow);
    }
}
