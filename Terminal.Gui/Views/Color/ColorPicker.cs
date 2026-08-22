using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Color Picker supporting RGB, HSL, and HSV color models. Supports choosing colors with
///     sliders and color names from the <see cref="IColorNameResolver"/>.
/// </summary>
/// <remarks>
/// <img src="../images/views/ColorPicker.gif" alt="ColorPicker demo"/>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Double-Click</term>
///             <description>Accepts the selected color (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class ColorPicker : View, IValue<Color?>, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="ColorPicker"/>. Use
    ///     <see cref="Style"/> to change color model. Use <see cref="Value"/>
    ///     to change initial <see cref="Color"/>.
    /// </summary>
    public ColorPicker ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        Height = Dim.Auto ();
        Width = Dim.Auto (minimumContentDim: 32);
        ApplyStyleChanges ();

        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.Remove (MouseFlags.LeftButtonClicked);
    }

    private readonly Dictionary<IColorBar, TextField> _textFields = new ();
    private readonly ColorModelStrategy _strategy = new ();
    private TextField? _tfHex;
    private Label? _lbHex;

    private DropDownList? _ddlName;
    private Label? _lbName;

    private Color _selectedColor = Color.Black;
    private bool _syncingSubViews;

    // TODO: Add interface
    private readonly IColorNameResolver _colorNameResolver = new StandardColorsNameResolver ();

    private List<IColorBar> _bars = [];

    /// <summary>
    ///     Rebuild the user interface to reflect the new state of <see cref="Style"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        Color oldValue = _selectedColor;
        DisposeOldViews ();

        var y = 0;
        const int TEXT_FIELD_WIDTH = 4;

        foreach (ColorBar bar in _strategy.CreateBars (Style.ColorModel))
        {
            bar.Y = y;
            bar.Width = Dim.Fill (Style.ShowTextFields ? TEXT_FIELD_WIDTH : 0);

            TextField? tfValue = null;

            if (Style.ShowTextFields)
            {
                tfValue = new TextField { X = Pos.AnchorEnd (TEXT_FIELD_WIDTH), Y = y, Width = TEXT_FIELD_WIDTH };
                tfValue.HasFocusChanged += UpdateSingleBarValueFromTextField;
                tfValue.Accepting += (s, _) => UpdateSingleBarValueFromTextField (s);
                _textFields.Add (bar, tfValue);
            }

            y++;

            bar.ValueChanged += RebuildColorFromBar;

            _bars.Add (bar);

            Add (bar);

            if (tfValue is { })
            {
                Add (tfValue);
            }
        }

        if (Style.ShowColorName)
        {
            CreateNameField ();
        }

        CreateTextField ();
        SelectedColor = oldValue;

        SetNeedsLayout ();
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);
        SetAttribute (new Attribute (SelectedColor, normal.Background, Enabled ? TextStyle.None : TextStyle.Faint));
        int y = _bars.Count + (Style.ShowColorName ? 1 : 0);
        AddRune (13, y, (Rune)'■');

        return true;
    }

    /// <summary>
    ///     The color selected in the picker. Identical to <see cref="Value"/> but non-nullable.
    /// </summary>
    public Color SelectedColor { get => _selectedColor; set => SetSelectedColor (value, true); }

    /// <summary>
    ///     Gets or sets the selected color. Implements <see cref="IValue{T}"/>.
    /// </summary>
    /// <remarks>
    ///     Setting <see langword="null"/> is equivalent to setting <see cref="Color.Black"/>.
    /// </remarks>
    public Color? Value { get => _selectedColor; set => SelectedColor = value ?? Color.Black; }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<Color?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<Color?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called before <see cref="Value"/> changes. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<Color?> args) => false;

    /// <summary>
    ///     Called after <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (ValueChangedEventArgs<Color?> args) { }

    /// <inheritdoc/>
    protected override bool OnTextChanging (string newText)
    {
        // Reject text that cannot be parsed as a valid color
        if (!_colorNameResolver.TryParseColor (newText, out _))
        {
            return true;
        }

        return base.OnTextChanging (newText);
    }

    /// <inheritdoc/>
    protected override void OnTextChanged ()
    {
        if (_colorNameResolver.TryParseColor (Text, out Color newColor))
        {
            SelectedColor = newColor;
        }

        base.OnTextChanged ();
    }

    /// <summary>
    ///     Style settings for the color picker.  After making changes ensure you call
    ///     <see cref="ApplyStyleChanges"/>.
    /// </summary>
    public ColorPickerStyle Style { get; set; } = new ();

    private void CreateNameField ()
    {
        _lbName = new Label { Text = "Name:", X = 0, Y = 3 };

        ObservableCollection<string> colorNames = new (_colorNameResolver.GetColorNames ());

        _ddlName = new DropDownList
        {
            Y = 3, X = 6, Width = 20, // width of "LightGoldenRodYellow" - the longest w3c color name
            ReadOnly = true,
            Source = new ListWrapper<string> (colorNames)
        };

        Add (_lbName);
        Add (_ddlName);

        _ddlName.ValueChanged += (_, _) => UpdateValueFromName ();
    }

    private void CreateTextField ()
    {
        int y = _bars.Count;

        if (Style.ShowColorName)
        {
            y++;
        }

        _lbHex = new Label { Text = "Hex:", X = 0, Y = y };

        _tfHex = new TextField { Y = y, X = 4, Width = 8 };

        Add (_lbHex);
        Add (_tfHex);

        _tfHex.HasFocusChanged += UpdateValueFromTextField;
        _tfHex.Accepting += (_, _) => UpdateValueFromTextField ();
    }

    private void DisposeOldViews ()
    {
        foreach (ColorBar bar in _bars.Cast<ColorBar> ())
        {
            bar.ValueChanged -= RebuildColorFromBar;

            if (_textFields.TryGetValue (bar, out TextField? tf))
            {
                Remove (tf);
                tf.Dispose ();
            }

            Remove (bar);
            bar.Dispose ();
        }

        _bars = [];
        _textFields.Clear ();

        if (_lbHex != null)
        {
            Remove (_lbHex);
            _lbHex.Dispose ();
            _lbHex = null;
        }

        if (_tfHex != null)
        {
            Remove (_tfHex);
            _tfHex.Dispose ();
            _tfHex = null;
        }

        if (_lbName != null)
        {
            Remove (_lbName);
            _lbName.Dispose ();
            _lbName = null;
        }

        if (_ddlName != null)
        {
            Remove (_ddlName);
            _ddlName.Dispose ();
            _ddlName = null;
        }
    }

    private void RebuildColorFromBar (object? sender, EventArgs<int> e) => SetSelectedColor (_strategy.GetColorFromBars (_bars, Style.ColorModel), false);

    private void SetSelectedColor (Color value, bool syncBars)
    {
        if (_selectedColor != value)
        {
            Color oldValue = _selectedColor;

            // CWP: Fire ValueChanging (allows cancellation)
            ValueChangingEventArgs<Color?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                SyncSubViewValues (syncBars);

                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                SyncSubViewValues (syncBars);

                return;
            }

            // Do the work
            _selectedColor = value;

            // Keep Text in sync with the new color
            SetTextDirect (_selectedColor.ToString ());

            // CWP: Fire ValueChanged
            ValueChangedEventArgs<Color?> changedArgs = new (oldValue, value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);

            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, value));
        }

        SyncSubViewValues (syncBars);
    }

    private void SyncSubViewValues (bool syncBars)
    {
        _syncingSubViews = true;

        try
        {
            if (syncBars)
            {
                _strategy.SetBarsToColor (_bars, _selectedColor, Style.ColorModel);
            }

            foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
            {
                kvp.Value.Text = kvp.Key.Value.ToString ();
            }

            string colorHex = _selectedColor.ToString ($"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}");

            if (_ddlName is { })
            {
                _ddlName.Text = _colorNameResolver.TryNameColor (_selectedColor, out string? name) ? name : string.Empty;
            }

            _tfHex?.Text = colorHex;

            SetNeedsLayout ();
        }
        finally
        {
            _syncingSubViews = false;
        }
    }

    private void UpdateSingleBarValueFromTextField (object? sender, HasFocusEventArgs e)
    {
        // if the new value of Focused is true then it is an enter event so ignore
        if (e.NewValue)
        {
            return;
        }

        // it is a leave event so update
        UpdateSingleBarValueFromTextField (sender);
    }

    private void UpdateSingleBarValueFromTextField (object? sender)
    {
        foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
        {
            if (kvp.Value != sender)
            {
                continue;
            }

            if (int.TryParse (kvp.Value.Text, out int v))
            {
                kvp.Key.Value = v;
            }
        }
    }

    private void UpdateValueFromName ()
    {
        if (_ddlName == null || _syncingSubViews)
        {
            return;
        }

        if (_colorNameResolver.TryParseColor (_ddlName.Text, out Color newColor))
        {
            SelectedColor = newColor;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SyncSubViewValues (false);
        }
    }

    private void UpdateValueFromTextField (object? sender, HasFocusEventArgs e)
    {
        // if the new value of Focused is true then it is an enter event so ignore
        if (e.NewValue)
        {
            return;
        }

        // it is a leave event so update
        UpdateValueFromTextField ();
    }

    private void UpdateValueFromTextField ()
    {
        if (_tfHex == null)
        {
            return;
        }

        if (Color.TryParse (_tfHex.Text, out Color? newColor))
        {
            SelectedColor = newColor.Value;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SyncSubViewValues (false);
        }
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,Tab,wait:400,CursorRight,wait:300,CursorRight,wait:300,CursorRight,wait:800";

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        SelectedColor = Color.BrightRed;

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        DisposeOldViews ();
        base.Dispose (disposing);
    }
}
