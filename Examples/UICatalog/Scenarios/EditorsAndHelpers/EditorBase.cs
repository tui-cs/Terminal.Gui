#nullable enable
namespace UICatalog.Scenarios;

public abstract class EditorBase : View
{
    protected EditorBase ()
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        CanFocus = true;

        ExpanderButton = new ExpanderButton { Orientation = Orientation.Vertical };

        TabStop = TabBehavior.TabStop;

        Initialized += OnInitialized;

        AddCommand (Command.Accept, () => true);

        SchemeName = "Dialog";

        return;

        void OnInitialized (object? sender, EventArgs e)
        {
            if (ExpanderButton is { })
            {
                Border.GetOrCreateView ().Add (ExpanderButton);
            }

            App!.Mouse.MouseEvent += ApplicationOnMouseEvent;
            App!.Navigation!.FocusedChanged += NavigationOnFocusedChanged;
        }
    }

    public ExpanderButton? ExpanderButton
    {
        get;
        init
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            field = value;
        }
    }

    public bool ShowViewIdentifier
    {
        get => Padding.Thickness != Thickness.Empty;
        set
        {
            Padding.Thickness = value ? new Thickness (0, 2, 0, 0) : Thickness.Empty;

            if (value)
            {
                Padding.GetOrCreateView ();
            }
        }
    }

    public bool UpdatingLayoutSettings { get; internal set; }

    private void View_LayoutComplete (object? sender, LayoutEventArgs e)
    {
        UpdatingLayoutSettings = true;

        OnUpdateLayoutSettings ();

        UpdatingLayoutSettings = false;
    }

    private View? _viewToEdit;

    public View? ViewToEdit
    {
        get => _viewToEdit;
        set
        {
            if (_viewToEdit == value)
            {
                return;
            }

            if (value is null && _viewToEdit is { })
            {
                _viewToEdit.SubViewsLaidOut -= View_LayoutComplete;
            }

            _viewToEdit = value;

            if (_viewToEdit is { })
            {
                _viewToEdit.SubViewsLaidOut += View_LayoutComplete;
            }

            OnViewToEditChanged ();
        }
    }

    protected virtual void OnViewToEditChanged ()
    {
        if (ShowViewIdentifier)
        {
            Padding.GetOrCreateView ().Text = ViewToEdit?.ToIdentifyingString () ?? "<none>";
        }
    }

    protected virtual void OnUpdateLayoutSettings () { }

    /// <summary>
    ///     Gets or sets whether the DimEditor should automatically select the View to edit
    ///     based on the values of <see cref="AutoSelectSuperView"/> and <see cref="AutoSelectAdornments"/>.
    /// </summary>
    public bool AutoSelectViewToEdit { get; set; }

    /// <summary>
    ///     Gets or sets the View that will scope the behavior of <see cref="AutoSelectViewToEdit"/>.
    /// </summary>
    public View? AutoSelectSuperView { get; set; }

    /// <summary>
    ///     Gets or sets whether auto select with the mouse will select Adornments or just Views.
    /// </summary>
    public bool AutoSelectAdornments { get; set; }

    private void NavigationOnFocusedChanged (object? sender, EventArgs e)
    {
        if (AutoSelectSuperView is null)
        {
            return;
        }

        if (ApplicationNavigation.IsInHierarchy (this, App?.Navigation?.GetFocused ()))
        {
            return;
        }

        if (!ApplicationNavigation.IsInHierarchy (AutoSelectSuperView, App?.Navigation?.GetFocused ()))
        {
            return;
        }

        if (!AutoSelectViewToEdit || !AutoSelectAdornments)
        {
            return;
        }

        ViewToEdit = App!.Navigation!.GetFocused ();
    }

    private void ApplicationOnMouseEvent (object? sender, Mouse mouse)
    {
        if (mouse.Flags != MouseFlags.LeftButtonClicked || !AutoSelectViewToEdit || !AutoSelectAdornments)
        {
            return;
        }

        if ((AutoSelectSuperView is { } && !AutoSelectSuperView.FrameToScreen ().Contains (mouse.Position!.Value))
            || FrameToScreen ().Contains (mouse.Position!.Value))
        {
            return;
        }

        View? view = mouse.View;

        if (view is null)
        {
            return;
        }

        if (AutoSelectAdornments && view is AdornmentView adornment)
        {
            ViewToEdit = AutoSelectAdornments ? adornment : adornment.Adornment?.Parent;
        }
        else
        {
            ViewToEdit = view;
        }
    }

    /// <inheritdoc/>
    protected override bool OnSuperViewChanging (ValueChangingEventArgs<View?> args)
    {
        // Clean up event handlers before SuperView is set to null
        // This ensures App is still accessible for proper cleanup
        if (App is null)
        {
            return base.OnSuperViewChanging (args);
        }
        App.Navigation!.FocusedChanged -= NavigationOnFocusedChanged;
        App.Mouse.MouseEvent -= ApplicationOnMouseEvent;

        return base.OnSuperViewChanging (args);
    }
}
