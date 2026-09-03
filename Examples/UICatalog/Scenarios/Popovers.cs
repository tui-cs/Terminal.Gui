#nullable enable
using System.Collections.ObjectModel;
using System.Reflection;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Popovers", "Demonstrates the Popover<TView, TResult> infrastructure.")]
[ScenarioCategory ("Popups")]
[ScenarioCategory ("Controls")]
public class Popovers : Scenario
{
    private IApplication? _app;
    private Dictionary<string, Type>? _viewClasses;
    private ListView? _viewListView;
    private FrameView? _popoverTargetFrame;
    private EventLog? _eventLog;
    private Button? _activateButton;
    private TextField? _resultTextField;
    private TextField? _initialValueTextField;

    private readonly Dictionary<int, IPopoverView> _popoverInstances = [];

    public override void Main ()
    {
        _app = Application.Create ();
        _app.Init ();

        using Window window = new ();
        window.Title = GetQuitKeyAndName ();
        window.BorderStyle = LineStyle.None;

        // Get all View classes
        _viewClasses = GetAllViewClasses ()
                       .OrderBy (t => t.Name)
                       .Select (t => new KeyValuePair<string, Type> (GetFormattedTypeName (t), t))
                       .ToDictionary (t => t.Key, t => t.Value);

        // Left: View list with marks — space toggles mark (register/deregister), enter shows marked popover
        _viewListView = new ListView
        {
            Title = "_Views (marked=registered)",
            Width = 35,
            Height = Dim.Fill (),
            ShowMarks = true,
            MarkMultiple = true,
            SelectedItem = 0,
            BorderStyle = LineStyle.Single,
            Source = new ListWrapper<string> (new ObservableCollection<string> (_viewClasses.Keys))
        };
        _viewListView.Activated += ViewListView_Activated;
        _viewListView.Accepting += ShowPopover;

        // Right: Event log
        _eventLog = new EventLog
        {
            Title = "_Event Log",
            X = Pos.AnchorEnd (),
            Width = Dim.Auto (maximumContentDim: 50),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Double,
            Arrangement = ViewArrangement.LeftResizable
        };

        _popoverTargetFrame = new FrameView
        {
            X = Pos.Right (_viewListView),
            Y = Pos.Top (_viewListView),
            Width = Dim.Fill (_eventLog!),
            Height = Dim.Height (_viewListView),
            BorderStyle = LineStyle.Single,
            CommandsToBubbleUp = [Command.Accept, Command.Activate]
        };

        _popoverTargetFrame.Activated += (sender, args) => { _resultTextField!.Text = args.Value?.Value?.ToString () ?? Glyphs.Null.ToString (); };

        _popoverTargetFrame.Accepted += (sender, args) =>
                                        {
                                            _resultTextField!.Text = args.Context?.Value?.ToString () ?? Glyphs.Null.ToString ();
                                            _app.Popovers?.Hide ();
                                        };

        _activateButton = new Button { Title = "_Make Visible (Enter)" };

        _activateButton.Accepting += ShowPopover;

        _initialValueTextField = new TextField
        {
            Y = Pos.Bottom (_activateButton),
            Width = Dim.Fill (),
            Title = "_Initial Value (TrySetValueFromString)",
            BorderStyle = LineStyle.Dotted
        };

        _resultTextField = new TextField
        {
            Y = Pos.Bottom (_initialValueTextField),
            Width = Dim.Fill (),
            ReadOnly = true,
            Title = "Result",
            BorderStyle = LineStyle.Dotted
        };
        _popoverTargetFrame.Add (_activateButton, _initialValueTextField, _resultTextField);

        _eventLog.SetViewToLog (window);
        window.Add (_viewListView, _popoverTargetFrame, _eventLog);

        _app.Run (window);

        foreach (IPopoverView popover in _popoverInstances.Values)
        {
            _app?.Popovers?.DeRegister (popover);
            (popover as IDisposable)?.Dispose ();
        }

        _app?.Dispose ();
        _app = null;
    }

    /// <summary>
    ///     Handles space key (Activated) — sync mark state to register/deregister the popover.
    /// </summary>
    private void ViewListView_Activated (object? sender, EventArgs<ICommandContext?> args) => SyncRegistrations ();

    /// <summary>
    ///     Handles enter key (Accepting) — show the popover for the selected item if it's marked/registered.
    /// </summary>
    private void ShowPopover (object? sender, CommandEventArgs args)
    {
        if (_viewListView?.SelectedItem is not { } selectedIndex || _viewClasses is null)
        {
            return;
        }

        // Sync registrations first in case marks changed
        SyncRegistrations ();

        if (!_popoverInstances.TryGetValue (selectedIndex, out IPopoverView? popover))
        {
            _eventLog?.Log ($"{_viewClasses.Keys.ElementAt (selectedIndex)} is not registered (mark it with space first)");

            return;
        }

        args.Handled = true;

        // Apply initial value via IValue.TrySetValueFromString if the TextField has content
        if (!string.IsNullOrEmpty (_initialValueTextField?.Text))
        {
            View? contentView = (popover as View)?.SubViews.FirstOrDefault ();

            if (contentView is IValue iValue)
            {
                bool success = iValue.TrySetValueFromString (_initialValueTextField.Text);
                _eventLog?.Log ($"TrySetValueFromString(\"{_initialValueTextField.Text}\") on {contentView.GetType ().Name}: {(success ? "succeeded" : "failed")}");
            }
            else
            {
                _eventLog?.Log ($"Content view does not implement IValue; cannot apply initial value.");
            }
        }

        try
        {
            Point idealPosition = _resultTextField?.FrameToScreen ().Location ?? Point.Empty;
            idealPosition.Y += _resultTextField?.Frame.Height ?? 0;
            popover.MakeVisible (idealPosition);
            _eventLog?.Log ($"Showed: {_viewClasses.Keys.ElementAt (selectedIndex)}");
        }
        catch (Exception ex)
        {
            _eventLog?.Log ($"Error showing popover: {ex.Message}");
        }
    }

    /// <summary>
    ///     Syncs mark state with popover registrations — marked items get registered, unmarked get deregistered.
    /// </summary>
    private void SyncRegistrations ()
    {
        if (_viewListView?.Source is null || _viewClasses is null)
        {
            return;
        }

        for (var i = 0; i < _viewListView.Source.Count; i++)
        {
            bool isMarked = _viewListView.Source.IsMarked (i);
            bool isRegistered = _popoverInstances.ContainsKey (i);

            if (isMarked && !isRegistered)
            {
                // Register
                Type viewType = _viewClasses.Values.ElementAt (i);

                try
                {
                    IPopoverView? popoverView = CreatePopoverViewInstance (viewType);

                    popoverView.Target = new WeakReference<View> (_popoverTargetFrame!);
                    RegisterPopover (i, popoverView);
                }
                catch (Exception ex)
                {
                    _eventLog?.Log ($"Error creating popover for {viewType.Name}: {ex.Message}");
                    _viewListView.Source.SetMark (i, false);
                }
            }
            else if (!isMarked && isRegistered)
            {
                // Deregister
                IPopoverView popover = _popoverInstances [i];
                _app?.Popovers?.DeRegister (popover);
                _popoverInstances.Remove (i);
                (popover as IDisposable)?.Dispose ();
                string key = _viewClasses.Keys.ElementAt (i);
                _eventLog?.Log ($"Deregistered: {key}");
            }
        }

        if (_viewListView.SelectedItem is not { } selectedIndex)
        {
            return;
        }

        if (!_popoverInstances.TryGetValue (selectedIndex, out IPopoverView? selectedPopoverView))
        {
            _popoverTargetFrame?.Title = "_Selected IPopoverView: `none`";

            return;
        }

        _popoverTargetFrame?.Title = $"_Selected IPopoverView: `Popover<{GetFormattedTypeName (selectedPopoverView.GetType ())}, {GetFormattedTypeName (GetIValueResultType (selectedPopoverView.GetType ()) ?? typeof (string))}>`";
    }

    private void RegisterPopover (int index, IPopoverView popoverView)
    {
        string key = _viewClasses!.Keys.ElementAt (index);

        try
        {
            View? contentView = (popoverView as View)?.SubViews.ElementAt (0);

            _app?.Popovers?.Register (popoverView);
            _popoverInstances [index] = popoverView;
            _eventLog?.SetViewToLog (contentView);
            _eventLog?.SetViewToLog (popoverView as View);
            _eventLog?.Log ($"Registered: {key}");
        }
        catch (Exception ex)
        {
            _eventLog?.Log ($"Error registering popover for {key}: {ex.Message}");
        }
    }

    private IPopoverView CreatePopoverViewInstance (Type viewType)
    {
        Type resultType = GetIValueResultType (viewType) ?? typeof (string);
        Type popoverType = typeof (Popover<,>).MakeGenericType (viewType, resultType);

        object? popoverObj = Activator.CreateInstance (popoverType,
                                                       CreateViewInstance (viewType) ?? throw new InvalidOperationException ("Failed to create view instance"));

        if (popoverObj is not IPopoverView popover)
        {
            throw new InvalidOperationException ($"Failed to create popover instance for {viewType.Name}");
        }

        return popover;
    }

    private Type? GetIValueResultType (Type viewType)
    {
        Type? iValueInterface = viewType.GetInterfaces ().FirstOrDefault (i => i.IsGenericType && i.GetGenericTypeDefinition () == typeof (IValue<>));

        return iValueInterface?.GetGenericArguments () [0];
    }

    private View? CreateViewInstance (Type viewType)
    {
        ConstructorInfo? ctor = viewType.GetConstructor (Type.EmptyTypes);

        View? view = null;

        if (ctor is { })
        {
            view = (View?)Activator.CreateInstance (viewType);

            if (view is null)
            {
                return view;
            }
        }

        if (view is null && viewType.IsGenericTypeDefinition)
        {
            Type [] typeParams = viewType.GetGenericArguments ();
            Type [] constraintTypes = new Type [typeParams.Length];

            for (var i = 0; i < typeParams.Length; i++)
            {
                Type [] constraints = typeParams [i].GetGenericParameterConstraints ();
                constraintTypes [i] = constraints.Length > 0 ? constraints [0] : typeof (object);
            }

            Type constructedType = viewType.MakeGenericType (constraintTypes);

            view = (View?)Activator.CreateInstance (constructedType);
        }

        view?.Initialized += (sender, _) =>
                             {
                                 if (sender is not View v)
                                 {
                                     return;
                                 }

                                 if (!v.Visible)
                                 {
                                     return;
                                 }

                                 ConfigurePopoverContentView (view);
                             };

        return view;
    }

    private const int MAX_VIEW_FRAME_HEIGHT = 30;

    private void ConfigurePopoverContentView (View? view)
    {
        view!.BorderStyle = LineStyle.Dotted;
        view.Arrangement = ViewArrangement.Resizable | ViewArrangement.Movable;

        if (view.Width == Dim.Absolute (0))
        {
            view.Width = Dim.Fill ();
        }

        if (view.Height == Dim.Absolute (0))
        {
            view.Height = MAX_VIEW_FRAME_HEIGHT - 2;
        }

        if (!view.Width.Has<DimAuto> (out _))
        {
            view.Width = Dim.Fill (0, 100);
        }

        if (!view.Height.Has<DimAuto> (out _))
        {
            view.Height = Dim.Auto (minimumContentDim: MAX_VIEW_FRAME_HEIGHT - 2,
                                    maximumContentDim: MAX_VIEW_FRAME_HEIGHT - view.GetAdornmentsThickness ().Vertical);
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        view.Title = $"Popover<{GetFormattedTypeName (view.GetType ())}, {GetFormattedTypeName (GetIValueResultType (view.GetType ()) ?? typeof (string))}>";
    }

    private string GetFormattedTypeName (Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        string baseName = type.Name [..type.Name.IndexOf ('`')];
        string args = string.Join (", ", type.GetGenericArguments ().Select (t => t.Name));

        return $"{baseName}<{args}>";
    }

    private IEnumerable<Type> GetAllViewClasses ()
    {
        IEnumerable<Type> types = typeof (View).Assembly.GetTypes ()
                                               .Where (t => t is { IsPublic: true, IsAbstract: false }
                                                            && t.IsSubclassOf (typeof (View))
                                                            && t != typeof (AdornmentView)
                                                            && !t.IsSubclassOf (typeof (AdornmentView))
                                                            && t != typeof (PopoverMenu)
                                                            && !t.IsGenericType);

        return types;
    }
}
