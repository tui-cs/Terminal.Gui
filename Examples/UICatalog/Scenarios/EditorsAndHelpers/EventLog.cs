#nullable enable
using System.Collections.ObjectModel;
using System.Text;

namespace UICatalog.Scenarios;

/// <summary>
///     An event log that automatically shows the Command-related events that are raised.
/// </summary>
/// <remarks>
/// </remarks>
public class EventLog : ListView
{
    public EventLog ()
    {
        Title = "Event Log";
        CanFocus = true;

        X = Pos.AnchorEnd ();
        Y = 0;

        Width = Dim.Func (_ =>
                          {
                              if (!IsInitialized)
                              {
                                  return 0;
                              }

                              return Math.Min (SuperView!.Viewport.Width / 3, MaxItemLength + GetAdornmentsThickness ().Horizontal);
                          });
        Height = Dim.Fill ();

        ExpandButton = new ExpanderButton { Orientation = Orientation.Horizontal };

        Initialized += EventLog_Initialized;

        ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        AddCommand (Command.DeleteAll,
                    () =>
                    {
                        SelectedItem = null;
                        _eventSource.Clear ();

                        return true;
                    });

        KeyBindings.Add (Key.Delete, Command.DeleteAll);
    }

    public ExpanderButton? ExpandButton { get; }

    private readonly ObservableCollection<string> _eventSource = [];

    private View? _viewToLog;

    public View? ViewToLog
    {
        get => _viewToLog;
        set
        {
            if (_viewToLog == value)
            {
                return;
            }

            UnsubscribeFromViewToLog (_viewToLog);

            _viewToLog = value;

            if (_viewToLog is { })
            {
                SetViewToLog (_viewToLog);
            }
        }
    }

    private void UnsubscribeFromViewToLog (View? view)
    {
        view?.Initialized -= OnViewOnInitialized;
        view?.HandlingHotKey -= OnViewOnHandlingHotKey;
        view?.Activating -= OnViewOnActivating;
        view?.Activated -= OnViewOnActivated;
        view?.Accepting -= OnViewOnAccepting;
        view?.Accepted -= OnViewOnAccepted;

        if (view is IValue valueView)
        {
            valueView.ValueChangedUntyped -= OnValueViewOnValueChanged;
        }
    }

    public void SetViewToLog (View? view)
    {
        view?.Initialized += OnViewOnInitialized;
        view?.HandlingHotKey += OnViewOnHandlingHotKey;
        view?.Activating += OnViewOnActivating;
        view?.Activated += OnViewOnActivated;
        view?.Accepting += OnViewOnAccepting;
        view?.Accepted += OnViewOnAccepted;

        if (view is IValue valueView)
        {
            valueView.ValueChangedUntyped += OnValueViewOnValueChanged;
        }
    }

    private void OnViewOnInitialized (object? s, EventArgs _) => Log ($"{(s as View).ToIdentifyingString ()} Initialized");

    private void OnViewOnAccepted (object? s, CommandEventArgs args) => Log ($"{(s as View).ToIdentifyingString ()} Accepted: {FormatContext (args.Context)}");

    private void OnViewOnAccepting (object? s, CommandEventArgs args) =>
        Log ($"{(s as View).ToIdentifyingString ()} Accepting: {FormatContext (args.Context)}");

    private void OnViewOnActivating (object? s, CommandEventArgs args) =>
        Log ($"{(s as View).ToIdentifyingString ()} Activating: {FormatContext (args.Context)}");

    private void OnViewOnActivated (object? sender, EventArgs<ICommandContext?> e) =>
        Log ($"{(sender as View).ToIdentifyingString ()} Activated: {FormatContext (e.Value)}");

    private void OnViewOnHandlingHotKey (object? s, CommandEventArgs args) =>
        Log ($"{(s as View).ToIdentifyingString ()} HandlingHotKey: {FormatContext (args.Context)}");

    private void OnValueViewOnValueChanged (object? s, ValueChangedEventArgs<object?> e) =>
        Log ($"{(s as View).ToIdentifyingString ()} ValueChanged: {e.OldValue} -> {e.NewValue}");

    private string FormatContext (ICommandContext? context)
    {
        if (context is null)
        {
            return "null";
        }

        StringBuilder sb = new ();
        sb.Append ($"{context.Command}");

        if (context.Binding is { } binding)
        {
            sb.Append ($", Binding={binding}");
        }

        if (context.Source is { })
        {
            sb.Append ($", Source={context.Source.ToIdentifyingString ()}");
        }

        if (context.Value is { })
        {
            sb.Append ($", Value={context.Value}");
        }

        return sb.ToString ();
    }

    public void Log (string text)
    {
        // Logging.Debug (text);
        _eventSource.Add (text);
        MoveEnd ();
        SelectedItem = null;
    }

    private void EventLog_Initialized (object? _, EventArgs e)
    {
        Border.GetOrCreateView ().Add (ExpandButton!);
        Source = new ListWrapper<string> (_eventSource);
    }
}
