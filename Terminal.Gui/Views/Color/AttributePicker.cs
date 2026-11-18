namespace Terminal.Gui.Views;

/// <summary>
///     Allows the user to pick an <see cref="Attribute"/> by selecting foreground and background colors,
///     and text styles.
/// </summary>
/// <remarks>
/// <img src="../images/views/AttributePicker.gif" alt="AttributePicker demo"/>
/// </remarks>
public class AttributePicker : View, IValue<Attribute?>, IDesignable
{
    private Attribute? _value;

    /// <summary>
    ///     Gets or sets the selected <see cref="Attribute"/>.
    /// </summary>
    public Attribute? Value
    {
        get => _value;
        set =>
            CWPPropertyHelper.ChangeProperty (this,
                                              ref _value,
                                              value,
                                              OnValueChanging,
                                              ValueChanging,
                                              newValue =>
                                              {
                                                  _value = newValue;
                                                  SyncSubViewsToValue ();
                                                  UpdateSampleLabel ();
                                              },
                                              OnValueChanged,
                                              ValueChanged,
                                              out _);
    }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<Attribute?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<Attribute?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    private ColorPicker? _foregroundPicker;
    private ColorPicker? _backgroundPicker;
    private FlagSelector<TextStyle>? _styleSelector;
    private View? _sampleLabel;

    private string _sampleText = "Sample Text";

    /// <summary>
    ///     Gets or sets the sample text displayed to preview the attribute.
    /// </summary>
    public string SampleText
    {
        get => _sampleText;
        set
        {
            if (_sampleText == value)
            {
                return;
            }
            _sampleText = value;
            UpdateSampleLabel ();
        }
    }

    /// <summary>
    ///     Creates a new instance of <see cref="AttributePicker"/>.
    /// </summary>
    public AttributePicker ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        Height = Dim.Auto ();
        Width = Dim.Fill (0, 64);
        CommandsToBubbleUp = [Command.Accept];
        SetupSubViews ();
    }

    private void SetupSubViews ()
    {
        ColorPickerStyle colorPickerStyle = new () { ShowTextFields = true, ShowColorName = true };

        // Create foreground picker - offset X = -1 for border auto-joining with parent
        _foregroundPicker = new ColorPicker
        {
            Title = "Foreground", BorderStyle = LineStyle.Single, SuperViewRendersLineCanvas = true, Style = colorPickerStyle
        };
        _foregroundPicker.ValueChanged += OnForegroundColorChanged;
        _foregroundPicker.ApplyStyleChanges ();

        // Create background picker - positioned below foreground
        _backgroundPicker = new ColorPicker
        {
            Title = "Background",
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            X = Pos.Left (_foregroundPicker),
            Y = Pos.Bottom (_foregroundPicker) - 1,
            Width = Dim.Width (_foregroundPicker),
            Style = colorPickerStyle
        };
        _backgroundPicker.ValueChanged += OnBackgroundColorChanged;
        _backgroundPicker.ApplyStyleChanges ();

        // Create style selector - on the right side
        _styleSelector = new FlagSelector<TextStyle>
        {
            Title = "Style",
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            X = Pos.Right (_foregroundPicker) - 1,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Height (_foregroundPicker) + Dim.Height (_backgroundPicker) - 1
        };
        _styleSelector.Width = _styleSelector.GetAutoWidth ();
        _styleSelector.ValueChanged += OnStyleChanged;

        // Set color picker widths relative to style selector
        _foregroundPicker.Width = Dim.Func (_ => IsInitialized ? Viewport.Width - _styleSelector!.Frame.Width : 0);

        // Create sample label - below the pickers
        _sampleLabel = new View
        {
            Text = _sampleText,
            Y = Pos.Bottom (_backgroundPicker),
            X = Pos.Left (_foregroundPicker),
            Width = Dim.Fill (),
            Height = Dim.Auto (DimAutoStyle.Text),
            TextFormatter = new TextFormatter { Alignment = Alignment.Center }
        };

        Add (_foregroundPicker, _backgroundPicker, _styleSelector, _sampleLabel);

        // Set initial value
        _value = Attribute.Default;
        SyncSubViewsToValue ();
        UpdateSampleLabel ();
    }

    /// <summary>
    ///     Called before <see cref="Value"/> changes. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<Attribute?> args) => false;

    /// <summary>
    ///     Called after <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (ValueChangedEventArgs<Attribute?> args) =>
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue));

    private void OnForegroundColorChanged (object? sender, ValueChangedEventArgs<Color?> e) => UpdateValueFromSubViews ();

    private void OnBackgroundColorChanged (object? sender, ValueChangedEventArgs<Color?> e) => UpdateValueFromSubViews ();

    private void OnStyleChanged (object? sender, EventArgs<TextStyle?> e) => UpdateValueFromSubViews ();

    private void UpdateValueFromSubViews ()
    {
        if (_foregroundPicker is null || _backgroundPicker is null || _styleSelector is null)
        {
            return;
        }

        Attribute newValue = new (_foregroundPicker.Value!.Value, _backgroundPicker.Value!.Value, _styleSelector.Value ?? TextStyle.None);
        Value = newValue;
    }

    private void SyncSubViewsToValue ()
    {
        if (!_value.HasValue)
        {
            return;
        }

        // Temporarily unhook events to prevent recursion
        if (_foregroundPicker is { })
        {
            _foregroundPicker.ValueChanged -= OnForegroundColorChanged;
            _foregroundPicker.Value = _value.Value.Foreground;
            _foregroundPicker.ValueChanged += OnForegroundColorChanged;
        }

        if (_backgroundPicker is { })
        {
            _backgroundPicker.ValueChanged -= OnBackgroundColorChanged;
            _backgroundPicker.Value = _value.Value.Background;
            _backgroundPicker.ValueChanged += OnBackgroundColorChanged;
        }

        if (_styleSelector is { })
        {
            _styleSelector.ValueChanged -= OnStyleChanged;
            _styleSelector.Value = _value.Value.Style;
            _styleSelector.ValueChanged += OnStyleChanged;
        }
    }

    private void UpdateSampleLabel ()
    {
        if (_sampleLabel is null || !_value.HasValue)
        {
            return;
        }

        _sampleLabel.Text = _sampleText;

        // Create Scheme with Value for Normal role
        Scheme sampleScheme = new (_value.Value);
        _sampleLabel.SetScheme (sampleScheme);

        SetNeedsLayout ();
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        SampleText = "Multi-line Sample Text.\nThis is the second line.";
        Value = new Attribute (Color.BrightRed, Color.Blue, TextStyle.Bold | TextStyle.Italic);

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (_foregroundPicker is { })
            {
                _foregroundPicker.ValueChanged -= OnForegroundColorChanged;
            }

            if (_backgroundPicker is { })
            {
                _backgroundPicker.ValueChanged -= OnBackgroundColorChanged;
            }

            if (_styleSelector is { })
            {
                _styleSelector.ValueChanged -= OnStyleChanged;
            }
        }

        base.Dispose (disposing);
    }
}
