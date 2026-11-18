#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Content scrolling, IScrollBars, etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    private object? _progressTimer;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ();
        win.Title = GetQuitKeyAndName ();

        var label = new Label { X = 0, Y = 0 };
        win.Add (label);

        var demoView = new AllViewsView
        {
            Id = "demoView",
            X = 2,
            Y = Pos.Bottom (label) + 1,
            Width = Dim.Fill (4),
            Height = Dim.Fill (4)
        };

        label.Text = $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";

        demoView.ViewportChanged += (_, _) =>
                                    {
                                        label.Text = $"{demoView}\nContentSize: {demoView.GetContentSize ()}\nViewport.Location: {demoView.Viewport.Location}";
                                    };

        win.Add (demoView);

        Label lblHScrollBar = new () { X = Pos.X (demoView), Y = Pos.Bottom (demoView), Text = "_Horizontal ScrollBar:" };
        win.Add (lblHScrollBar);

        OptionSelector<ScrollBarVisibilityMode> osHScrollBar = new ()
        {
            X = Pos.Right (lblHScrollBar) + 1,
            Y = Pos.Top (lblHScrollBar),
            Value = demoView.ViewportSettings.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar)
                        ? ScrollBarVisibilityMode.Auto
                        : ScrollBarVisibilityMode.None,
            Orientation = Orientation.Horizontal,
            AssignHotKeys = true
        };
        win.Add (osHScrollBar);

        osHScrollBar.ValueChanged += (_, e) =>
                                     {
                                         if (e.Value == ScrollBarVisibilityMode.Auto)
                                         {
                                             demoView.ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
                                         }
                                         else
                                         {
                                             demoView.ViewportSettings &= ~ViewportSettingsFlags.HasHorizontalScrollBar;
                                             demoView.HorizontalScrollBar.VisibilityMode = e.Value!.Value;
                                         }
                                     };

        Label lblVScrollBar = new () { X = Pos.Right (osHScrollBar) + 3, Y = Pos.Bottom (demoView), Text = "_Vertical ScrollBar:" };
        win.Add (lblVScrollBar);

        OptionSelector<ScrollBarVisibilityMode> osVScrollBar = new ()
        {
            X = Pos.Right (lblVScrollBar) + 1,
            Y = Pos.Top (lblVScrollBar),
            Value = demoView.ViewportSettings.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar)
                        ? ScrollBarVisibilityMode.Auto
                        : ScrollBarVisibilityMode.None,
            Orientation = Orientation.Horizontal,
            AssignHotKeys = true
        };
        win.Add (osVScrollBar);

        osVScrollBar.ValueChanged += (_, e) =>
                                     {
                                         if (e.Value == ScrollBarVisibilityMode.Auto)
                                         {
                                             demoView.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
                                         }
                                         else
                                         {
                                             demoView.ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
                                             demoView.VerticalScrollBar.VisibilityMode = e.Value!.Value;
                                         }
                                     };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar { X = Pos.Center (), Y = Pos.AnchorEnd (), Width = Dim.Fill () };

        win.Add (progress);

        win.Initialized += WinOnInitialized;
        win.IsRunningChanged += WinIsRunningChanged;

        app.Run (win);
        win.IsRunningChanged -= WinIsRunningChanged;

        return;

        void WinOnInitialized (object? sender, EventArgs e)
        {
            _progressTimer = app.AddTimeout (TimeSpan.FromMilliseconds (200), TimerFn);

            return;

            bool TimerFn ()
            {
                progress.Pulse ();

                return _progressTimer is { };
            }
        }

        void WinIsRunningChanged (object? sender, EventArgs<bool> args)
        {
            if (args.Value || _progressTimer is null)
            {
                return;
            }
            app.RemoveTimeout (_progressTimer);
            _progressTimer = null;
        }
    }
}
