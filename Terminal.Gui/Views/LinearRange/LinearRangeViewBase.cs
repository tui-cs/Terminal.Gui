namespace Terminal.Gui.Views;

/// <summary>
///     Abstract base for linear range views (<see cref="LinearSelector{T}"/>,
///     <see cref="LinearMultiSelector{T}"/>, <see cref="LinearRange{T}"/>) that present a list of typed options
///     navigable by keyboard or mouse, and expose the current selection as a strongly-typed value via
///     <see cref="IValue{TValue}"/>.
/// </summary>
/// <typeparam name="TOption">The data type carried by each <see cref="LinearRangeOption{T}"/>.</typeparam>
/// <typeparam name="TValue">The shape of <see cref="Value"/>; defined by the concrete subclass.</typeparam>
/// <remarks>
///     <para>Default key bindings (when <see cref="Orientation"/> is <see cref="Orientation.Horizontal"/>):</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Left / Right</term> <description>Moves to the previous or next option.</description>
///         </item>
///         <item>
///             <term>Ctrl+Left / Ctrl+Right</term> <description>Moves by a larger step.</description>
///         </item>
///     </list>
///     <para>Default key bindings (when <see cref="Orientation"/> is <see cref="Orientation.Vertical"/>):</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Up / Down</term> <description>Moves to the previous or next option.</description>
///         </item>
///         <item>
///             <term>Ctrl+Up / Ctrl+Down</term> <description>Moves by a larger step.</description>
///         </item>
///     </list>
///     <para>Common key bindings (both orientations):</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last option.</description>
///         </item>
///         <item>
///             <term>Enter</term> <description>Accepts the current selection (<see cref="Command.Accept"/>).</description>
///         </item>
///         <item>
///             <term>Space</term>
///             <description>Activates the current selection (<see cref="Command.Activate"/>).</description>
///         </item>
///     </list>
///     <para>
///         Common bindings (Home, End, Enter, Space) are configurable via <see cref="View.DefaultKeyBindings"/> and
///         <see cref="LinearRangeViewBase{TOption,TValue}.DefaultKeyBindings"/>. Orientation-dependent cursor bindings are set dynamically
///         and cannot be reconfigured.
///     </para>
/// </remarks>
public abstract class LinearRangeViewBase<TOption, TValue> : View, IOrientation, IValue<TValue>
{
    /// <summary>
    ///     Gets or sets the view-specific default key bindings shared by all linear range views.
    ///     Contains only bindings unique to this family; shared bindings come from <see cref="View.DefaultKeyBindings"/>.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         No <see cref="ConfigurationPropertyAttribute"/> is applied because this is a generic
    ///         type. Use <see cref="View.ViewKeyBindings"/> with key <c>"LinearRange"</c> to override bindings via
    ///         configuration.
    ///     </para>
    /// </remarks>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
    {
        [Command.Accept] = Bind.All (Key.Enter),
        [Command.Activate] = Bind.All (Key.Space),
    };

    private readonly LinearRangeConfiguration _config = new ();

    // List of the current set options.
    private readonly List<int> _setOptions = [];

    // Options
    private List<LinearRangeOption<TOption>>? _options;

    private OrientationHelper? _orientationHelper;

    #region Initialize

    private void SetInitialProperties (List<LinearRangeOption<TOption>> options, Orientation orientation = Orientation.Horizontal)
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        CanFocus = true;

        _options = options;

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this); // Do not use object initializer!
        _orientationHelper.Orientation = _config._linearRangeOrientation = orientation;
        _orientationHelper.OrientationChanging += (_, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (_, e) => OrientationChanged?.Invoke (this, e);

        SetDefaultStyle ();
        SetCommands ();
        SetContentSize ();

        SubViewLayout += (_, _) => { SetContentSize (); };
    }

    // TODO: Make configurable via ConfigurationManager
    private void SetDefaultStyle ()
    {
        _config._showLegends = true;

        switch (_config._linearRangeOrientation)
        {
            case Orientation.Horizontal:
                Style.SpaceChar = new Cell { Grapheme = Glyphs.HLine.ToString () }; // '─'
                Style.OptionChar = new Cell { Grapheme = Glyphs.BlackCircle.ToString () }; // '┼●🗹□⏹'

                break;

            case Orientation.Vertical:
                Style.SpaceChar = new Cell { Grapheme = Glyphs.VLine.ToString () };
                Style.OptionChar = new Cell { Grapheme = Glyphs.BlackCircle.ToString () };

                break;
        }

        _config._legendsOrientation = _config._linearRangeOrientation;
        Style.EmptyChar = new Cell { Grapheme = " " };
        Style.SetChar = new Cell { Grapheme = Glyphs.ContinuousMeterSegment.ToString () }; // ■
        Style.RangeChar = new Cell { Grapheme = Glyphs.Stipple.ToString () }; // ░ ▒ ▓   // Medium shade not blinking on curses.
        Style.StartRangeChar = new Cell { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };
        Style.EndRangeChar = new Cell { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };
        Style.DragChar = new Cell { Grapheme = Glyphs.ContinuousMeterSegment.ToString () };
    }

    #endregion

    #region Constructors

    /// <summary>Initializes a new instance of the <see cref="LinearRangeViewBase{TOption,TValue}"/> class.</summary>
    /// <param name="renderMode">The selection rendering mode used by this concrete subclass.</param>
    protected LinearRangeViewBase (LinearRangeRenderMode renderMode) : this (new List<TOption> (), Orientation.Horizontal, renderMode) { }

    /// <summary>Initializes a new instance of the <see cref="LinearRangeViewBase{TOption,TValue}"/> class.</summary>
    /// <param name="options">Initial options.</param>
    /// <param name="orientation">Initial orientation.</param>
    /// <param name="renderMode">The selection rendering mode used by this concrete subclass.</param>
    protected LinearRangeViewBase (List<TOption>? options, Orientation orientation, LinearRangeRenderMode renderMode)
    {
        _config._renderMode = renderMode;
        Cursor = new Cursor { Style = LinearRangeDefaults.DefaultCursorStyle };

        if (options is null)
        {
            return;
        }

        if (options is { Count: 0 })
        {
            SetInitialProperties ([], orientation);
        }
        else
        {
            SetInitialProperties (options.Select (e =>
                                                  {
                                                      var legend = e?.ToString ();

                                                      return new LinearRangeOption<TOption>
                                                      {
                                                          Data = e, Legend = legend, LegendAbbr = (Rune)(legend?.Length > 0 ? legend [0] : ' ')
                                                      };
                                                  })
                                         .ToList (),
                                  orientation);
        }
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Setting the Text of a linear range is a shortcut to setting options. The text is a CSV string of the options.
    /// </summary>
    protected override void OnTextChanged ()
    {
        string value = Text;

        if (string.IsNullOrEmpty (value))
        {
            Options = [];
        }
        else
        {
            IEnumerable<string> list = value.Split (',').Select (x => x.Trim ());
            Options = list.Select (x => new LinearRangeOption<TOption> { Legend = x }).ToList ();
        }

        base.OnTextChanged ();
    }

    /// <summary>Allow no selection.</summary>
    public bool AllowEmpty
    {
        get => _config._allowEmpty;
        set
        {
            _config._allowEmpty = value;

            if (!value && _options!.Count > 0 && _setOptions.Count == 0)
            {
                FocusedOption = 0;
                SetFocusedOption ();
            }
        }
    }

    /// <summary>Gets or sets the minimum number of rows/columns between <see cref="Options"/>. The default is 1.</summary>
    public int MinimumInnerSpacing
    {
        get => _config._minInnerSpacing;
        set
        {
            int current = _config._minInnerSpacing;

            CWPPropertyHelper.ChangeProperty (this,
                                              ref current,
                                              value,
                                              OnMinimumInnerSpacingChanging,
                                              MinimumInnerSpacingChanging,
                                              newValue =>
                                              {
                                                  _config._minInnerSpacing = newValue;
                                                  SetContentSize ();
                                              },
                                              OnMinimumInnerSpacingChanged,
                                              MinimumInnerSpacingChanged,
                                              out int _);
        }
    }

    /// <summary>Event raised before the <see cref="MinimumInnerSpacing"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<int>>? MinimumInnerSpacingChanging;

    /// <summary>Event raised after the <see cref="MinimumInnerSpacing"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<int>>? MinimumInnerSpacingChanged;

    /// <summary>Called before <see cref="MinimumInnerSpacing"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnMinimumInnerSpacingChanging (ValueChangingEventArgs<int> args) => false;

    /// <summary>Called after <see cref="MinimumInnerSpacing"/> has changed.</summary>
    protected virtual void OnMinimumInnerSpacingChanged (ValueChangedEventArgs<int> args) { }

    /// <summary>
    ///     Gets the internal selection rendering mode set by the concrete subclass. Drives drawing,
    ///     hit-testing, and the <see cref="SetFocusedOption"/> behaviour.
    /// </summary>
    internal LinearRangeRenderMode RenderMode
    {
        get => _config._renderMode;
        set
        {
            if (_config._renderMode == value)
            {
                return;
            }

            ApplySelectedIndices ([]);
            _config._renderMode = value;
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/>. The default is <see cref="Orientation.Horizontal"/>.
    /// </summary>
    public Orientation Orientation { get => _orientationHelper!.Orientation; set => _orientationHelper!.Orientation = value; }

    #region IOrientation members

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        _config._linearRangeOrientation = newOrientation;

        switch (_config._linearRangeOrientation)
        {
            case Orientation.Horizontal:
                Style.SpaceChar = new Cell { Grapheme = Glyphs.HLine.ToString () }; // '─'

                break;

            case Orientation.Vertical:
                Style.SpaceChar = new Cell { Grapheme = Glyphs.VLine.ToString () };

                break;
        }

        SetKeyBindings ();
        SetContentSize ();
    }

    #endregion

    /// <summary>Legends Orientation. <see cref="ViewBase.Orientation"></see></summary>
    public Orientation LegendsOrientation
    {
        get => _config._legendsOrientation;
        set
        {
            Orientation current = _config._legendsOrientation;

            CWPPropertyHelper.ChangeProperty (this,
                                              ref current,
                                              value,
                                              OnLegendsOrientationChanging,
                                              LegendsOrientationChanging,
                                              newValue =>
                                              {
                                                  _config._legendsOrientation = newValue;
                                                  SetContentSize ();
                                              },
                                              OnLegendsOrientationChanged,
                                              LegendsOrientationChanged,
                                              out Orientation _);
        }
    }

    /// <summary>Event raised before the <see cref="LegendsOrientation"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<Orientation>>? LegendsOrientationChanging;

    /// <summary>Event raised after the <see cref="LegendsOrientation"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<Orientation>>? LegendsOrientationChanged;

    /// <summary>Called before <see cref="LegendsOrientation"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnLegendsOrientationChanging (ValueChangingEventArgs<Orientation> args) => false;

    /// <summary>Called after <see cref="LegendsOrientation"/> has changed.</summary>
    protected virtual void OnLegendsOrientationChanged (ValueChangedEventArgs<Orientation> args) { }

    /// <summary>LinearRange styles. <see cref="LinearRangeStyle"></see></summary>
    public LinearRangeStyle Style { get; set; } = new ();

    /// <summary>
    ///     Set the linear range options. When the new options no longer contain the previously selected
    ///     value(s), the selection is dropped (event semantics depend on the concrete subclass).
    /// </summary>
    public List<LinearRangeOption<TOption>> Options
    {
        get => _options ?? [];
        set
        {
            // _options should never be null
            _options = value ?? throw new ArgumentNullException (nameof (value));

            // Drop any selected indices that are no longer valid
            _setOptions.RemoveAll (i => i < 0 || i >= _options.Count);

            // Keep Text in sync with Options
            string csv = _options.Count == 0 ? string.Empty : string.Join (",", _options);
            SetTextDirect (csv);

            if (_options.Count == 0)
            {
                return;
            }

            SetContentSize ();
        }
    }

    /// <summary>
    ///     Internal accessor for whether a range is allowed to collapse to a single option (only meaningful
    ///     when <see cref="RenderMode"/> is <see cref="LinearRangeRenderMode.Span"/>). Exposed publicly only
    ///     by <see cref="LinearRange{T}"/>.
    /// </summary>
    internal bool RangeAllowSingleInternal { get => _config._rangeAllowSingle; set => _config._rangeAllowSingle = value; }

    /// <summary>Show/Hide spacing before and after the first and last option.</summary>
    public bool ShowEndSpacing
    {
        get => _config._showEndSpacing;
        set
        {
            bool current = _config._showEndSpacing;

            CWPPropertyHelper.ChangeProperty (this,
                                              ref current,
                                              value,
                                              OnShowEndSpacingChanging,
                                              ShowEndSpacingChanging,
                                              newValue =>
                                              {
                                                  _config._showEndSpacing = newValue;
                                                  SetContentSize ();
                                              },
                                              OnShowEndSpacingChanged,
                                              ShowEndSpacingChanged,
                                              out bool _);
        }
    }

    /// <summary>Event raised before the <see cref="ShowEndSpacing"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? ShowEndSpacingChanging;

    /// <summary>Event raised after the <see cref="ShowEndSpacing"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? ShowEndSpacingChanged;

    /// <summary>Called before <see cref="ShowEndSpacing"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnShowEndSpacingChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>Called after <see cref="ShowEndSpacing"/> has changed.</summary>
    protected virtual void OnShowEndSpacingChanged (ValueChangedEventArgs<bool> args) { }

    /// <summary>Show/Hide the options legends.</summary>
    public bool ShowLegends
    {
        get => _config._showLegends;
        set
        {
            bool current = _config._showLegends;

            CWPPropertyHelper.ChangeProperty (this,
                                              ref current,
                                              value,
                                              OnShowLegendsChanging,
                                              ShowLegendsChanging,
                                              newValue =>
                                              {
                                                  _config._showLegends = newValue;
                                                  SetContentSize ();
                                              },
                                              OnShowLegendsChanged,
                                              ShowLegendsChanged,
                                              out bool _);
        }
    }

    /// <summary>Event raised before the <see cref="ShowLegends"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? ShowLegendsChanging;

    /// <summary>Event raised after the <see cref="ShowLegends"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? ShowLegendsChanged;

    /// <summary>Called before <see cref="ShowLegends"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnShowLegendsChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>Called after <see cref="ShowLegends"/> has changed.</summary>
    protected virtual void OnShowLegendsChanged (ValueChangedEventArgs<bool> args) { }

    /// <summary>
    ///     Gets or sets whether the minimum or ideal size will be used when calculating the size of the linear range.
    /// </summary>
    public bool UseMinimumSize
    {
        get => _config._useMinimumSize;
        set
        {
            bool current = _config._useMinimumSize;

            CWPPropertyHelper.ChangeProperty (this,
                                              ref current,
                                              value,
                                              OnUseMinimumSizeChanging,
                                              UseMinimumSizeChanging,
                                              newValue =>
                                              {
                                                  _config._useMinimumSize = newValue;
                                                  SetContentSize ();
                                              },
                                              OnUseMinimumSizeChanged,
                                              UseMinimumSizeChanged,
                                              out bool _);
        }
    }

    /// <summary>Event raised before the <see cref="UseMinimumSize"/> property changes. Can be cancelled.</summary>
    public event EventHandler<ValueChangingEventArgs<bool>>? UseMinimumSizeChanging;

    /// <summary>Event raised after the <see cref="UseMinimumSize"/> property has changed.</summary>
    public event EventHandler<ValueChangedEventArgs<bool>>? UseMinimumSizeChanged;

    /// <summary>Called before <see cref="UseMinimumSize"/> changes. Return true to cancel the change.</summary>
    protected virtual bool OnUseMinimumSizeChanging (ValueChangingEventArgs<bool> args) => false;

    /// <summary>Called after <see cref="UseMinimumSize"/> has changed.</summary>
    protected virtual void OnUseMinimumSizeChanged (ValueChangedEventArgs<bool> args) { }

    #endregion

    #region Events

    /// <summary>
    ///     Internal hook fired whenever <see cref="_setOptions"/> changes due to user input
    ///     (keyboard, mouse, command). Concrete subclasses override <see cref="OnSelectionChanged"/>
    ///     to compute and publish their <see cref="Value"/>.
    /// </summary>
    internal void RaiseSelectionChanged ()
    {
        OnSelectionChanged ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Called by the base when the selected indices have changed due to user input.
    ///     Concrete subclasses must compute their <see cref="Value"/> from the current
    ///     selection and raise <see cref="ValueChanged"/> / <see cref="ValueChangedUntyped"/>.
    /// </summary>
    /// <remarks>
    ///     Subclasses should not raise <see cref="ValueChanging"/> from this hook;
    ///     <see cref="ValueChanging"/> is reserved for direct writes to <see cref="Value"/>.
    /// </remarks>
    protected abstract void OnSelectionChanged ();

    /// <summary>Event raised When the option is hovered with the keys or the mouse.</summary>
    public event EventHandler<LinearRangeEventArgs<TOption>>? OptionFocused;

    private int _lastFocusedOption; // for Range type; the most recently focused option. Used to determine shrink direction

    /// <summary>Overridable function that fires the <see cref="OptionFocused"/> event.</summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> if the focus change was cancelled.</returns>
    /// <param name="newFocusedOption"></param>
    public virtual bool OnOptionFocused (int newFocusedOption, LinearRangeEventArgs<TOption> args)
    {
        if (newFocusedOption > _options!.Count - 1 || newFocusedOption < 0)
        {
            return true;
        }

        OptionFocused?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.Cancel;
        }
        _lastFocusedOption = FocusedOption;
        FocusedOption = newFocusedOption;

        return args.Cancel;
    }

    #endregion Events

    #region IValue<TValue> Implementation

    /// <inheritdoc/>
    public abstract TValue? Value { get; set; }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<TValue?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<TValue?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>Raises <see cref="ValueChanging"/>. Returns <see langword="true"/> if cancelled.</summary>
    protected bool RaiseValueChanging (TValue? currentValue, TValue? newValue)
    {
        ValueChangingEventArgs<TValue?> args = new (currentValue, newValue);
        ValueChanging?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Raises <see cref="ValueChanged"/> and <see cref="ValueChangedUntyped"/>.</summary>
    protected void RaiseValueChanged (TValue? previousValue, TValue? newValue)
    {
        ValueChanged?.Invoke (this, new ValueChangedEventArgs<TValue?> (previousValue, newValue));
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (previousValue, newValue));
    }

    #endregion

    #region Selection Helpers (subclass support)

    /// <summary>Gets the currently selected option indices, in selection order, as a read-only snapshot.</summary>
    /// <remarks>To enumerate the selected option data values, project this list against <see cref="Options"/>.</remarks>
    protected internal IReadOnlyList<int> SelectedIndices => _setOptions.AsReadOnly ();

    /// <summary>
    ///     Replaces the selected indices without raising <see cref="ValueChanged"/>.
    ///     Used by concrete subclass <see cref="Value"/> setters to apply their value back to the index model.
    /// </summary>
    /// <param name="indices">The new selected indices. Out-of-range entries are ignored.</param>
    protected internal void ApplySelectedIndices (IReadOnlyList<int> indices)
    {
        if (_options is null)
        {
            return;
        }

        // Unset previous
        foreach (int i in _setOptions)
        {
            if (i >= 0 && i < _options.Count)
            {
                _options [i].OnUnSet ();
            }
        }

        _setOptions.Clear ();

        // Set new
        foreach (int i in indices)
        {
            if (i < 0 || i >= _options.Count || _setOptions.Contains (i))
            {
                continue;
            }

            _setOptions.Add (i);
            _options [i].OnSet ();
        }

        SetNeedsDraw ();
    }

    /// <summary>
    ///     Finds the first option whose <see cref="LinearRangeOption{T}.Data"/> equals <paramref name="data"/>
    ///     using the default equality comparer for <typeparamref name="TOption"/>.
    /// </summary>
    /// <returns>The option index, or <c>-1</c> if no match is found.</returns>
    protected internal int IndexOfData (TOption? data)
    {
        if (_options is null)
        {
            return -1;
        }

        EqualityComparer<TOption?> cmp = EqualityComparer<TOption?>.Default;

        for (var i = 0; i < _options.Count; i++)
        {
            if (cmp.Equals (_options [i].Data, data))
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region Public Methods

    /// <summary>The focused option (has the cursor).</summary>
    public int FocusedOption
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            UpdateCursor ();
        }
    }

    #endregion Public Methods

    #region Helpers

    private void MoveAndAdd (int x, int y, Rune rune)
    {
        Move (x, y);
        AddRune (rune);
    }

    private void MoveAndAdd (int x, int y, string str)
    {
        Move (x, y);
        AddStr (str);
    }

    /// <summary>Sets the dimensions of the LinearRange to the ideal values.</summary>
    private void SetContentSize ()
    {
        if (_options is { Count: 0 })
        {
            return;
        }

        bool horizontal = _config._linearRangeOrientation == Orientation.Horizontal;

        if (UseMinimumSize)
        {
            CalcSpacingConfig (CalcMinLength ());
        }
        else
        {
            CalcSpacingConfig (horizontal ? Viewport.Width : Viewport.Height);
        }

        SetContentSize (new Size (GetIdealWidth (), GetIdealHeight ()));

        return;

        void CalcSpacingConfig (int size)
        {
            _config._cachedInnerSpacing = 0;
            _config._startSpacing = 0;
            _config._endSpacing = 0;

            int maxLegend; // Because the legends are centered, the longest one determines inner spacing

            if (_config._linearRangeOrientation == _config._legendsOrientation)
            {
                maxLegend = int.Max (_options!.Max (s => s.Legend?.GetColumns () ?? 1), 1);
            }
            else
            {
                maxLegend = 1;
            }

            int minSizeThatFitsLegends = _options!.Count == 1 ? maxLegend : _options.Sum (o => o.Legend!.GetColumns ());

            string? first;
            string? last;

            _config._showLegendsAbbr = false;

            if (minSizeThatFitsLegends > size)
            {
                if (_config._linearRangeOrientation == _config._legendsOrientation)
                {
                    _config._showLegendsAbbr = true;

                    foreach (LinearRangeOption<TOption> o in _options.Where (op => op.LegendAbbr == default (Rune)))
                    {
                        o.LegendAbbr = (Rune)(o.Legend?.GetColumns () > 0 ? o.Legend [0] : ' ');
                    }
                }

                first = "x";
                last = "x";
            }
            else
            {
                first = _options.First ().Legend;
                last = _options.Last ().Legend;
            }

            // --o--
            // Hello
            // Left = He
            // Right = lo
            int firstLeft = (first!.Length - 1) / 2; // Chars count of the first option to the left.
            int lastRight = last!.Length / 2; // Chars count of the last option to the right.

            if (_config._linearRangeOrientation != _config._legendsOrientation)
            {
                firstLeft = 0;
                lastRight = 0;
            }

            // -1 because it's better to have an extra space at right than to clip
            int width = size - firstLeft - lastRight - 1;

            _config._startSpacing = firstLeft;

            if (_options.Count == 1)
            {
                _config._cachedInnerSpacing = maxLegend;
            }
            else
            {
                _config._cachedInnerSpacing = Math.Max (0, (int)Math.Floor ((double)width / (_options.Count - 1)) - 1);
            }

            _config._cachedInnerSpacing = Math.Max (_config._minInnerSpacing, _config._cachedInnerSpacing);

            _config._endSpacing = lastRight;
        }
    }

    /// <summary>Calculates the min dimension required for all options and inner spacing with abbreviated legends</summary>
    /// <returns></returns>
    private int CalcMinLength ()
    {
        if (_options is { Count: 0 })
        {
            return 0;
        }

        var length = 0;
        length += _config._startSpacing + _config._endSpacing;
        length += _options!.Count;
        length += (_options.Count - 1) * _config._minInnerSpacing;

        return length;
    }

    /// <summary>
    ///     Gets the ideal width of the linear range. The ideal width is the minimum width required to display all options and
    ///     inner
    ///     spacing.
    /// </summary>
    /// <returns></returns>
    public int GetIdealWidth ()
    {
        if (UseMinimumSize)
        {
            return Orientation == Orientation.Horizontal ? CalcMinLength () : CalcIdealThickness ();
        }

        return Orientation == Orientation.Horizontal ? CalcIdealLength () : CalcIdealThickness ();
    }

    /// <summary>
    ///     Gets the ideal height of the linear range. The ideal height is the minimum height required to display all options
    ///     and
    ///     inner spacing.
    /// </summary>
    /// <returns></returns>
    public int GetIdealHeight ()
    {
        if (UseMinimumSize)
        {
            return Orientation == Orientation.Horizontal ? CalcIdealThickness () : CalcMinLength ();
        }

        return Orientation == Orientation.Horizontal ? CalcIdealThickness () : CalcIdealLength ();
    }

    /// <summary>
    ///     Calculates the ideal dimension required for all options, inner spacing, and legends (non-abbreviated, with one
    ///     space between).
    /// </summary>
    /// <returns></returns>
    private int CalcIdealLength ()
    {
        if (_options is { Count: 0 })
        {
            return 0;
        }

        var length = 0;

        if (!_config._showLegends)
        {
            return Math.Max (length, CalcMinLength ());
        }

        if (_config._legendsOrientation == _config._linearRangeOrientation && _options!.Count > 0)
        {
            // Each legend should be centered in a space the width of the longest legend, with one space between.
            // Calculate the total length required for all legends.
            int maxLegend = int.Max (_options.Max (s => s.Legend?.GetColumns () ?? 1), 1);
            length = maxLegend * _options.Count + (_options.Count - 1);
        }
        else
        {
            length = CalcMinLength ();
        }

        return Math.Max (length, CalcMinLength ());
    }

    /// <summary>
    ///     Calculates the minimum dimension required for the linear range and legends.
    /// </summary>
    /// <returns></returns>
    private int CalcIdealThickness ()
    {
        var thickness = 1; // Always show the linear range.

        if (!_config._showLegends)
        {
            return thickness;
        }

        if (_config._legendsOrientation != _config._linearRangeOrientation && _options!.Count > 0)
        {
            thickness += _options.Max (s => s.Legend?.GetColumns () ?? 0);
        }
        else
        {
            thickness += 1;
        }

        return thickness;
    }

    #endregion Helpers

    #region Cursor and Position

    internal bool TryGetPositionByOption (int option, out (int x, int y) position)
    {
        position = (-1, -1);

        if (option < 0 || option >= _options!.Count)
        {
            return false;
        }

        var offset = 0;
        offset += _config._startSpacing;
        offset += option * (_config._cachedInnerSpacing + 1);

        position = _config._linearRangeOrientation == Orientation.Vertical ? (0, offset) : (offset, 0);

        return true;
    }

    /// <summary>Tries to get the option index by the position.</summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="threshold"></param>
    /// <param name="optionIdx"></param>
    /// <returns></returns>
    internal bool TryGetOptionByPosition (int x, int y, int threshold, out int optionIdx)
    {
        optionIdx = -1;

        if (Orientation == Orientation.Horizontal)
        {
            if (y != 0)
            {
                return false;
            }

            for (int xx = x - threshold; xx < x + threshold + 1; xx++)
            {
                int cx = xx;
                cx -= _config._startSpacing;

                int option = cx / (_config._cachedInnerSpacing + 1);
                bool valid = cx % (_config._cachedInnerSpacing + 1) == 0;

                if (!valid || option < 0 || option > _options!.Count - 1)
                {
                    continue;
                }

                optionIdx = option;

                return true;
            }
        }
        else
        {
            if (x != 0)
            {
                return false;
            }

            for (int yy = y - threshold; yy < y + threshold + 1; yy++)
            {
                int cy = yy;
                cy -= _config._startSpacing;

                int option = cy / (_config._cachedInnerSpacing + 1);
                bool valid = cy % (_config._cachedInnerSpacing + 1) == 0;

                if (!valid || option < 0 || option > _options!.Count - 1)
                {
                    continue;
                }

                optionIdx = option;

                return true;
            }
        }

        return false;
    }

    /// <summary>Updates the cursor position based on the focused option.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    ///     The framework automatically handles hiding the cursor when the view loses focus.
    /// </remarks>
    private void UpdateCursor ()
    {
        if (!TryGetPositionByOption (FocusedOption, out (int x, int y) position) || !IsInitialized || !Viewport.Contains (position.x, position.y))
        {
            Cursor = Cursor with { Position = null }; // Hide cursor

            return;
        }

        Cursor = Cursor with { Position = ViewportToScreen (new Point (position.x, position.y)) };
    }

    #endregion Cursor and Position

    #region Drawing

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // TODO: make this more surgical to reduce repaint

        if (_options is null || _options.Count == 0)
        {
            return true;
        }

        // Draw LinearRange
        DrawLinearRange ();

        // Draw Legends.
        if (_config._showLegends)
        {
            DrawLegends ();
        }

        if (_dragPosition.HasValue && _moveRenderPosition.HasValue)
        {
            AddStr (_moveRenderPosition.Value.X, _moveRenderPosition.Value.Y, Style.DragChar.Grapheme);
        }

        return true;
    }

    private static string AlignText (string? text, int width, Alignment alignment)
    {
        if (string.IsNullOrEmpty (text))
        {
            return "";
        }

        if (text.Length > width)
        {
            text = text [..width];
        }

        int w = width - text.Length;
        string s1 = new (' ', w / 2);
        string s2 = new (' ', w % 2);

        // Note: The formatter doesn't handle all of this ???
        switch (alignment)
        {
            case Alignment.Fill:
                return TextFormatter.Justify (text, width);

            case Alignment.Start:
                return text + s1 + s1 + s2;

            case Alignment.Center:
                if (text.Length % 2 != 0)
                {
                    return s1 + text + s1 + s2;
                }

                return s1 + s2 + text + s1;

            case Alignment.End:
                return s1 + s1 + s2 + text;

            default:
                return text;
        }
    }

    private void DrawLinearRange ()
    {
        // The base View pipeline already calls ClearViewport before OnDrawingContent
        // (see View.DoClearViewport). DrawLinearRange + DrawLegends together repaint every cell
        // in the Viewport, so a second ClearViewport here is redundant work that — because
        // ClearViewport calls SetNeedsDraw — also triggered another draw cycle, causing visible
        // flicker during mouse drag on the LinearRange family.

        // Attributes
        var normalAttr = new Attribute (Color.White, Color.Black);
        var setAttr = new Attribute (Color.Black, Color.White);

        if (IsInitialized)
        {
            normalAttr = GetAttributeForRole (VisualRole.Normal);
            setAttr = Style.SetChar.Attribute ?? GetAttributeForRole (VisualRole.HotNormal);
        }

        bool isVertical = _config._linearRangeOrientation == Orientation.Vertical;

        var x = 0;
        var y = 0;

        bool isSet = _setOptions.Count > 0;

        // Left Spacing
        if (_config is { _showEndSpacing: true, _startSpacing: > 0 })
        {
            SetAttribute (isSet && _config._renderMode == LinearRangeRenderMode.LeftSpan
                              ? Style.RangeChar.Attribute ?? normalAttr
                              : Style.SpaceChar.Attribute ?? normalAttr);
            string text = isSet && _config._renderMode == LinearRangeRenderMode.LeftSpan ? Style.RangeChar.Grapheme : Style.SpaceChar.Grapheme;

            for (var i = 0; i < _config._startSpacing; i++)
            {
                MoveAndAdd (x, y, text);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }
        else
        {
            SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);

            for (var i = 0; i < _config._startSpacing; i++)
            {
                MoveAndAdd (x, y, Style.EmptyChar.Grapheme);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }

        // LinearRange
        if (_options!.Count > 0)
        {
            for (var i = 0; i < _options.Count; i++)
            {
                var drawRange = false;

                if (isSet)
                {
                    switch (_config._renderMode)
                    {
                        case LinearRangeRenderMode.LeftSpan when i <= _setOptions [0]:
                            drawRange = i < _setOptions [0];

                            break;

                        case LinearRangeRenderMode.RightSpan when i >= _setOptions [0]:
                            drawRange = i >= _setOptions [0];

                            break;

                        case LinearRangeRenderMode.Span when _setOptions.Count == 1:
                            drawRange = false;

                            break;

                        case LinearRangeRenderMode.Span when _setOptions.Count == 2:
                            if ((i >= _setOptions [0] && i <= _setOptions [1]) || (i >= _setOptions [1] && i <= _setOptions [0]))
                            {
                                drawRange = (i >= _setOptions [0] && i < _setOptions [1]) || (i >= _setOptions [1] && i < _setOptions [0]);
                            }

                            break;
                    }
                }

                // Draw Option
                SetAttribute (isSet && _setOptions.Contains (i) ? Style.SetChar.Attribute ?? setAttr :
                              drawRange ? Style.RangeChar.Attribute ?? setAttr : Style.OptionChar.Attribute ?? normalAttr);

                string text = drawRange ? Style.RangeChar.Grapheme : Style.OptionChar.Grapheme;

                if (isSet)
                {
                    if (_setOptions [0] == i)
                    {
                        text = Style.StartRangeChar.Grapheme;
                    }
                    else if (_setOptions.Count > 1 && _setOptions [1] == i)
                    {
                        text = Style.EndRangeChar.Grapheme;
                    }
                    else if (_setOptions.Contains (i))
                    {
                        text = Style.SetChar.Grapheme;
                    }
                }

                MoveAndAdd (x, y, text);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }

                // Draw Spacing
                if (!_config._showEndSpacing && i >= _options.Count - 1)
                {
                    continue;
                }

                // Skip if is the Last Spacing.
                SetAttribute (drawRange && isSet ? Style.RangeChar.Attribute ?? setAttr : Style.SpaceChar.Attribute ?? normalAttr);

                for (var s = 0; s < _config._cachedInnerSpacing; s++)
                {
                    MoveAndAdd (x, y, drawRange && isSet ? Style.RangeChar.Grapheme : Style.SpaceChar.Grapheme);

                    if (isVertical)
                    {
                        y++;
                    }
                    else
                    {
                        x++;
                    }
                }
            }
        }

        int remaining = isVertical ? Viewport.Height - y : Viewport.Width - x;

        // Right Spacing
        if (_config._showEndSpacing)
        {
            SetAttribute (isSet && _config._renderMode == LinearRangeRenderMode.RightSpan
                              ? Style.RangeChar.Attribute ?? normalAttr
                              : Style.SpaceChar.Attribute ?? normalAttr);
            string text = isSet && _config._renderMode == LinearRangeRenderMode.RightSpan ? Style.RangeChar.Grapheme : Style.SpaceChar.Grapheme;

            for (var i = 0; i < remaining; i++)
            {
                MoveAndAdd (x, y, text);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }
        else
        {
            SetAttribute (Style.EmptyChar.Attribute ?? normalAttr);

            for (var i = 0; i < remaining; i++)
            {
                MoveAndAdd (x, y, Style.EmptyChar.Grapheme);

                if (isVertical)
                {
                    y++;
                }
                else
                {
                    x++;
                }
            }
        }
    }

    private void DrawLegends ()
    {
        // Attributes
        var normalAttr = new Attribute (Color.White, Color.Black);
        Attribute spaceAttr = normalAttr;

        if (IsInitialized)
        {
            normalAttr = Style.LegendAttributes.NormalAttribute ?? GetAttributeForRole (VisualRole.Normal);
            spaceAttr = Style.LegendAttributes.EmptyAttribute ?? normalAttr;
        }

        bool isTextVertical = _config._legendsOrientation == Orientation.Vertical;

        var x = 0;
        var y = 0;

        Move (x, y);

        switch (_config._linearRangeOrientation)
        {
            case Orientation.Horizontal when _config._legendsOrientation == Orientation.Vertical:
                x += _config._startSpacing;

                break;

            case Orientation.Vertical when _config._legendsOrientation == Orientation.Horizontal:
                y += _config._startSpacing;

                break;
        }

        if (_config._linearRangeOrientation == Orientation.Horizontal)
        {
            y += 1;
        }
        else
        {
            // Vertical
            x += 1;
        }

        for (var i = 0; i < _options!.Count; i++)
        {
            // Text || Abbreviation

            string text = (_config._showLegendsAbbr ? _options [i].LegendAbbr.ToString () : _options [i].Legend)!;

            switch (_config._linearRangeOrientation)
            {
                case Orientation.Horizontal:
                    switch (_config._legendsOrientation)
                    {
                        case Orientation.Horizontal:
                            text = AlignText (text, _config._cachedInnerSpacing + 1, Alignment.Center);

                            break;

                        case Orientation.Vertical:
                            y = 1;

                            break;
                    }

                    break;

                case Orientation.Vertical:
                    switch (_config._legendsOrientation)
                    {
                        case Orientation.Horizontal:
                            x = 1;

                            break;

                        case Orientation.Vertical:
                            text = AlignText (text, _config._cachedInnerSpacing + 1, Alignment.Center);

                            break;
                    }

                    break;
            }

            // Text
            int legendLeftSpacesCount = text.TakeWhile (e => e == ' ').Count ();
            int legendRightSpacesCount = text.Reverse ().TakeWhile (e => e == ' ').Count ();
            text = text.Trim ();

            // Calculate Start Spacing
            if (_config._linearRangeOrientation == _config._legendsOrientation)
            {
                if (i == 0)
                {
                    // The spacing for the linear range use the StartSpacing but...
                    // The spacing for the legends is the StartSpacing MINUS the total chars to the left of the first options.
                    //    ●────●────●
                    //  Hello Bye World
                    //
                    // chars_left is 2 for Hello => (5 - 1) / 2
                    //
                    // then the spacing is 2 for the linear range but 0 for the legends.

                    int charsLeft = (text.Length - 1) / 2;
                    legendLeftSpacesCount = _config._startSpacing - charsLeft;
                }

                // Option Left Spacing
                if (isTextVertical)
                {
                    y += legendLeftSpacesCount;
                }
                else
                {
                    x += legendLeftSpacesCount;
                }
            }

            // Legend - no special styling for set/focused options; the range bar itself indicates selection.
            SetAttribute (normalAttr);

            foreach (Rune c in text.EnumerateRunes ())
            {
                MoveAndAdd (x, y, c);

                if (isTextVertical)
                {
                    y += 1;
                }
                else
                {
                    x += 1;
                }
            }

            // Calculate End Spacing
            if (i == _options.Count - 1)
            {
                // See Start Spacing explanation.
                int charsRight = text.Length / 2;
                legendRightSpacesCount = _config._endSpacing - charsRight;
            }

            // Option Right Spacing of Option
            SetAttribute (spaceAttr);

            if (isTextVertical)
            {
                y += legendRightSpacesCount;
            }
            else
            {
                x += legendRightSpacesCount;
            }

            switch (_config._linearRangeOrientation)
            {
                case Orientation.Horizontal when _config._legendsOrientation == Orientation.Vertical:
                    x += _config._cachedInnerSpacing + 1;

                    break;

                case Orientation.Vertical when _config._legendsOrientation == Orientation.Horizontal:
                    y += _config._cachedInnerSpacing + 1;

                    break;
            }
        }
    }

    #endregion Drawing

    #region Keys and Mouse

    // Mouse coordinates of current drag
    private Point? _dragPosition;

    // Coordinates of where the "move cursor" is drawn (in OnDrawContent)
    private Point? _moveRenderPosition;

    // For Span (Closed) drag: the option that stays fixed while dragging the active end.
    // -1 means no drag in progress.
    private int _dragAnchorOption = -1;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (!(mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked)
              || mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed)
              || mouse.Flags.FastHasFlags (MouseFlags.PositionReport)
              || mouse.Flags.FastHasFlags (MouseFlags.LeftButtonReleased)))
        {
            return false;
        }

        SetFocus ();

        if (!_dragPosition.HasValue && mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
        {
            if (mouse.Flags.FastHasFlags (MouseFlags.PositionReport))
            {
                _dragPosition = mouse.Position;
                _moveRenderPosition = ClampMovePosition ((Point)_dragPosition!);
                App?.Mouse.GrabMouse (this);

                // Anchor the selection at the press position so a subsequent drag can extend a range.
                // Resolve the option under the press position and set it as the focused option.
                bool pressSuccess;
                int pressOption;

                if (Orientation == Orientation.Horizontal)
                {
                    pressSuccess = TryGetOptionByPosition (mouse.Position!.Value.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out pressOption);
                }
                else
                {
                    pressSuccess = TryGetOptionByPosition (0, mouse.Position!.Value.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out pressOption);
                }

                if (pressSuccess)
                {
                    if (!OnOptionFocused (pressOption, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
                    {
                        ApplyMouseSelection (pressOption, dragStart: true);
                    }
                }

                // Mark handled so the View pipeline does not later invoke Command.Activate
                // on the synthesized Released/Clicked events (which would re-toggle the selection).
                mouse.Handled = true;
            }

            SetNeedsDraw ();

            return true;
        }

        bool success;
        int option;

        if (_dragPosition.HasValue && mouse.Flags.FastHasFlags (MouseFlags.PositionReport) && mouse.Flags.FastHasFlags (MouseFlags.LeftButtonPressed))
        {
            // Continue Drag
            _dragPosition = mouse.Position;
            _moveRenderPosition = ClampMovePosition ((Point)_dragPosition!);

            // how far has user dragged from original location?
            if (Orientation == Orientation.Horizontal)
            {
                success = TryGetOptionByPosition (mouse.Position!.Value.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }
            else
            {
                success = TryGetOptionByPosition (0, mouse.Position!.Value.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out option);
            }

            // Update the selection on drag regardless of AllowEmpty so the user can drag the
            // end of a range (or move a single selection) continuously.
            if (success)
            {
                if (!OnOptionFocused (option, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
                {
                    ApplyMouseSelection (option, dragStart: false);
                }
            }

            SetNeedsDraw ();

            return true;
        }

        if (_dragPosition.HasValue && mouse.Flags.FastHasFlags (MouseFlags.LeftButtonReleased))
        {
            // End of a drag we initiated. Selection was already updated during drag continues;
            // just release the grab. Mark the event handled so the View pipeline does not
            // re-invoke Command.Activate (which would toggle the selection back off).
            App?.Mouse.UngrabMouse ();
            _dragPosition = null;
            _moveRenderPosition = null;
            _dragAnchorOption = -1;
            mouse.Handled = true;
            SetNeedsDraw ();

            return true;
        }

        if (mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            // Click events from the synthesizer that follow a drag are redundant — the drag
            // already updated selection. Otherwise (a "real" click without prior press),
            // let the View pipeline raise Command.Activate via the default mouse bindings.
            return mouse.Handled;
        }

        // End Drag
        App?.Mouse.UngrabMouse ();
        _dragPosition = null;
        _moveRenderPosition = null;
        _dragAnchorOption = -1;

        switch (Orientation)
        {
            case Orientation.Horizontal:
                success = TryGetOptionByPosition (mouse.Position!.Value.X, 0, Math.Max (0, _config._cachedInnerSpacing / 2), out option);

                break;

            default:
                success = TryGetOptionByPosition (0, mouse.Position!.Value.Y, Math.Max (0, _config._cachedInnerSpacing / 2), out option);

                break;
        }

        if (success)
        {
            if (!OnOptionFocused (option, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
            {
                SetFocusedOption ();
            }
        }

        SetNeedsDraw ();

        mouse.Handled = true;

        return mouse.Handled;

        Point ClampMovePosition (Point position)
        {
            if (Orientation == Orientation.Horizontal)
            {
                int left = _config._startSpacing;
                int width = _options!.Count + (_options.Count - 1) * _config._cachedInnerSpacing;
                int right = left + width - 1;
                int clampedX = Clamp (position.X, left, right);
                position = new Point (clampedX, 0);
            }
            else
            {
                int top = _config._startSpacing;
                int height = _options!.Count + (_options.Count - 1) * _config._cachedInnerSpacing;
                int bottom = top + height - 1;
                int clampedY = Clamp (position.Y, top, bottom);
                position = new Point (0, clampedY);
            }

            return position;

            static int Clamp (int value, int min, int max) => Math.Max (min, Math.Min (max, value));
        }
    }

    private void SetCommands ()
    {
        AddCommand (Command.Right, () => MovePlus ());
        AddCommand (Command.Down, () => MovePlus ());
        AddCommand (Command.Left, () => MoveMinus ());
        AddCommand (Command.Up, () => MoveMinus ());
        AddCommand (Command.LeftStart, () => MoveStart ());
        AddCommand (Command.RightEnd, () => MoveEnd ());
        AddCommand (Command.RightExtend, () => ExtendPlus ());
        AddCommand (Command.LeftExtend, () => ExtendMinus ());

        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        SetKeyBindings ();
    }

    // This is called during initialization and anytime orientation changes.
    // Orientation-dependent bindings cannot be in DefaultKeyBindings because they vary per instance.
    private void SetKeyBindings ()
    {
        // Remove Shift+Cursor extend bindings inherited from View.DefaultKeyBindings;
        // LinearRange uses Ctrl+Cursor for extend operations instead.
        KeyBindings.Remove (Key.CursorLeft.WithShift);
        KeyBindings.Remove (Key.CursorRight.WithShift);
        KeyBindings.Remove (Key.CursorUp.WithShift);
        KeyBindings.Remove (Key.CursorDown.WithShift);

        if (_config._linearRangeOrientation == Orientation.Horizontal)
        {
            // Remove before Add: ApplyKeyBindings already bound CursorRight/CursorLeft from View.DefaultKeyBindings
            KeyBindings.Remove (Key.CursorRight);
            KeyBindings.Add (Key.CursorRight, Command.Right);
            KeyBindings.Remove (Key.CursorDown);
            KeyBindings.Remove (Key.CursorLeft);
            KeyBindings.Add (Key.CursorLeft, Command.Left);
            KeyBindings.Remove (Key.CursorUp);

            KeyBindings.Add (Key.CursorRight.WithCtrl, Command.RightExtend);
            KeyBindings.Remove (Key.CursorDown.WithCtrl);
            KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.LeftExtend);
            KeyBindings.Remove (Key.CursorUp.WithCtrl);
        }
        else
        {
            KeyBindings.Remove (Key.CursorRight);
            // Remove before Add: ApplyKeyBindings already bound CursorDown/CursorUp from View.DefaultKeyBindings
            KeyBindings.Remove (Key.CursorDown);
            KeyBindings.Add (Key.CursorDown, Command.Down);
            KeyBindings.Remove (Key.CursorLeft);
            KeyBindings.Remove (Key.CursorUp);
            KeyBindings.Add (Key.CursorUp, Command.Up);

            KeyBindings.Remove (Key.CursorRight.WithCtrl);
            KeyBindings.Add (Key.CursorDown.WithCtrl, Command.RightExtend);
            KeyBindings.Remove (Key.CursorLeft.WithCtrl);
            KeyBindings.Add (Key.CursorUp.WithCtrl, Command.LeftExtend);
        }
    }

    private Dictionary<int, LinearRangeOption<TOption>> GetSetOptionDictionary () => _setOptions.ToDictionary (e => e, e => _options! [e]);

    /// <summary>
    ///     Applies a mouse-press or mouse-drag selection at <paramref name="option"/>. Unlike
    ///     <see cref="SetFocusedOption"/> (which has toggle/extend/shrink semantics designed
    ///     for keyboard activation), this performs <em>set</em> semantics suitable for a continuous
    ///     mouse drag: a single-bounded view's endpoint follows the cursor without toggling off
    ///     when the cursor returns to the existing value, and a Closed range tracks an anchor on
    ///     the opposite end so the range never collapses unexpectedly while dragging.
    /// </summary>
    /// <param name="option">The option index under the mouse.</param>
    /// <param name="dragStart">
    ///     <see langword="true"/> for the initial press of a drag;
    ///     <see langword="false"/> for subsequent drag-continue events.
    /// </param>
    private void ApplyMouseSelection (int option, bool dragStart)
    {
        if (_options is null or { Count: 0 } || option < 0 || option >= _options.Count)
        {
            return;
        }

        var changed = false;

        switch (_config._renderMode)
        {
            case LinearRangeRenderMode.Single:
            case LinearRangeRenderMode.LeftSpan:
            case LinearRangeRenderMode.RightSpan:
                changed = ApplyMouseSelectionSingle (option);

                break;

            case LinearRangeRenderMode.Multiple:
                if (dragStart)
                {
                    // Toggle on the press option.
                    changed = ToggleSetOption (option);
                }
                else
                {
                    // During drag, only ensure the option becomes set (don't toggle it off
                    // each time the cursor revisits a previously-set option).
                    if (!_setOptions.Contains (option))
                    {
                        _setOptions.Add (option);
                        _options [option].OnSet ();
                        changed = true;
                    }
                }

                break;

            case LinearRangeRenderMode.Span:
                changed = ApplyMouseSelectionSpan (option, dragStart);

                break;

            default:
                throw new ArgumentOutOfRangeException (_config._renderMode.ToString ());
        }

        if (changed)
        {
            RaiseSelectionChanged ();
        }
    }

    private bool ApplyMouseSelectionSingle (int option)
    {
        if (_setOptions.Count == 1 && _setOptions [0] == option)
        {
            // Already the single set option; no change. Critically, do NOT toggle off here:
            // a drag through the same option must not clear the selection.
            return false;
        }

        foreach (int existing in _setOptions)
        {
            _options! [existing].OnUnSet ();
        }

        _setOptions.Clear ();
        _setOptions.Add (option);
        _options! [option].OnSet ();

        return true;
    }

    private bool ToggleSetOption (int option)
    {
        if (_setOptions.Contains (option))
        {
            if (!_config._allowEmpty && _setOptions.Count == 1)
            {
                return false;
            }

            _setOptions.Remove (option);
            _options! [option].OnUnSet ();
        }
        else
        {
            _setOptions.Add (option);
            _options! [option].OnSet ();
        }

        return true;
    }

    private bool ApplyMouseSelectionSpan (int option, bool dragStart)
    {
        // Empty range: just place a single point at option.
        if (_setOptions.Count == 0)
        {
            if (_config._rangeAllowSingle)
            {
                _setOptions.Add (option);
                _options! [option].OnSet ();
                _dragAnchorOption = option;

                return true;
            }

            // Closed range with rangeAllowSingle = false: span [option, option+1] (or option-1).
            int next = option < _options!.Count - 1 ? option + 1 : option - 1;
            int lo = Math.Min (option, next);
            int hi = Math.Max (option, next);
            _setOptions.Add (lo);
            _setOptions.Add (hi);
            _options! [lo].OnSet ();
            _options! [hi].OnSet ();

            // Anchor at the press location so a subsequent drag moves the OTHER end.
            _dragAnchorOption = option;

            return true;
        }

        if (dragStart)
        {
            // Choose anchor for the upcoming drag.
            if (_setOptions.Count == 1)
            {
                int existing = _setOptions [0];

                if (option == existing)
                {
                    // Press on the single existing point: anchor stays here; no change yet.
                    _dragAnchorOption = existing;

                    return false;
                }

                int lo = Math.Min (existing, option);
                int hi = Math.Max (existing, option);
                _setOptions.Clear ();
                _setOptions.Add (lo);
                _setOptions.Add (hi);
                _options! [option].OnSet ();
                _dragAnchorOption = existing;

                return true;
            }

            // Count == 2: pick anchor as the endpoint farther from the press; the closer
            // endpoint becomes the active end and is moved to the press position.
            int lowEnd = _setOptions [0];
            int highEnd = _setOptions [1];

            if (option <= lowEnd)
            {
                // Press at or to the left of the range: anchor=high, active=low.
                _dragAnchorOption = highEnd;

                if (option == lowEnd)
                {
                    return false;
                }

                _options! [lowEnd].OnUnSet ();
                _setOptions [0] = option;
                _options! [option].OnSet ();

                return true;
            }

            if (option >= highEnd)
            {
                _dragAnchorOption = lowEnd;

                if (option == highEnd)
                {
                    return false;
                }

                _options! [highEnd].OnUnSet ();
                _setOptions [1] = option;
                _options! [option].OnSet ();

                return true;
            }

            // Inside the range: pick the closer endpoint as active; the other is the anchor.
            int distLow = option - lowEnd;
            int distHigh = highEnd - option;

            if (distLow <= distHigh)
            {
                _options! [lowEnd].OnUnSet ();
                _setOptions [0] = option;
                _options! [option].OnSet ();
                _dragAnchorOption = highEnd;
            }
            else
            {
                _options! [highEnd].OnUnSet ();
                _setOptions [1] = option;
                _options! [option].OnSet ();
                _dragAnchorOption = lowEnd;
            }

            return true;
        }

        // Drag continue: keep _dragAnchorOption fixed, move the active end to option.
        if (_dragAnchorOption < 0)
        {
            // Anchor was never set (e.g. drag without prior press in our pipeline). Treat as start.
            return ApplyMouseSelectionSpan (option, dragStart: true);
        }

        int anchor = _dragAnchorOption;
        int newLo = Math.Min (anchor, option);
        int newHi = Math.Max (anchor, option);

        if (newLo == newHi)
        {
            // Collapsed to a single point.
            if (!_config._rangeAllowSingle)
            {
                // Closed range with rangeAllowSingle=false cannot collapse; keep current state.
                return false;
            }

            if (_setOptions.Count == 1 && _setOptions [0] == newLo)
            {
                return false;
            }

            foreach (int s in _setOptions)
            {
                _options! [s].OnUnSet ();
            }

            _setOptions.Clear ();
            _setOptions.Add (newLo);
            _options! [newLo].OnSet ();

            return true;
        }

        if (_setOptions.Count == 2 && _setOptions [0] == newLo && _setOptions [1] == newHi)
        {
            return false;
        }

        foreach (int s in _setOptions)
        {
            _options! [s].OnUnSet ();
        }

        _setOptions.Clear ();
        _setOptions.Add (newLo);
        _setOptions.Add (newHi);
        _options! [newLo].OnSet ();
        _options! [newHi].OnSet ();

        return true;
    }

    private bool SetFocusedOption ()
    {
        if (_options is null or { Count: 0 })
        {
            return false;
        }

        var changed = false;

        switch (_config._renderMode)
        {
            case LinearRangeRenderMode.Single:
            case LinearRangeRenderMode.LeftSpan:
            case LinearRangeRenderMode.RightSpan:

                if (_setOptions.Count == 1)
                {
                    int prev = _setOptions [0];

                    if (!_config._allowEmpty && prev == FocusedOption)
                    {
                        break;
                    }

                    _setOptions.Clear ();
                    _options [FocusedOption].OnUnSet ();

                    if (FocusedOption != prev)
                    {
                        _setOptions.Add (FocusedOption);
                        _options [FocusedOption].OnSet ();
                    }
                }
                else
                {
                    _setOptions.Add (FocusedOption);
                    _options [FocusedOption].OnSet ();
                }

                // Raise slider changed event.
                RaiseSelectionChanged ();
                changed = true;

                break;

            case LinearRangeRenderMode.Multiple:
                if (_setOptions.Contains (FocusedOption))
                {
                    if (!_config._allowEmpty && _setOptions.Count == 1)
                    {
                        break;
                    }

                    _setOptions.Remove (FocusedOption);
                    _options [FocusedOption].OnUnSet ();
                }
                else
                {
                    _setOptions.Add (FocusedOption);
                    _options [FocusedOption].OnSet ();
                }

                RaiseSelectionChanged ();
                changed = true;

                break;

            case LinearRangeRenderMode.Span:
                if (_config._rangeAllowSingle)
                {
                    if (_setOptions.Count == 1)
                    {
                        int prev = _setOptions [0];

                        if (!_config._allowEmpty && prev == FocusedOption)
                        {
                            break;
                        }

                        if (FocusedOption == prev)
                        {
                            // un-set
                            _setOptions.Clear ();
                            _options [FocusedOption].OnUnSet ();
                        }
                        else
                        {
                            _setOptions [0] = FocusedOption;
                            _setOptions.Add (prev);
                            _setOptions.Sort ();
                            _options [FocusedOption].OnSet ();
                        }
                    }
                    else if (_setOptions.Count == 0)
                    {
                        _setOptions.Add (FocusedOption);
                        _options [FocusedOption].OnSet ();
                    }
                    else
                    {
                        // Extend/Shrink
                        if (FocusedOption < _setOptions [0])
                        {
                            // extend left
                            _options [_setOptions [0]].OnUnSet ();
                            _setOptions [0] = FocusedOption;
                        }
                        else if (FocusedOption > _setOptions [1])
                        {
                            // extend right
                            _options [_setOptions [1]].OnUnSet ();
                            _setOptions [1] = FocusedOption;
                        }
                        else if (FocusedOption >= _setOptions [0] && FocusedOption <= _setOptions [1])
                        {
                            if (FocusedOption < _lastFocusedOption)
                            {
                                // shrink to the left
                                _options [_setOptions [1]].OnUnSet ();
                                _setOptions [1] = FocusedOption;
                            }
                            else if (FocusedOption > _lastFocusedOption)
                            {
                                // shrink to the right
                                _options [_setOptions [0]].OnUnSet ();
                                _setOptions [0] = FocusedOption;
                            }

                            if (_setOptions.Count > 1 && _setOptions [0] == _setOptions [1])
                            {
                                _setOptions.Clear ();
                                _setOptions.Add (FocusedOption);
                            }
                        }
                    }
                }
                else
                {
                    if (_setOptions.Count == 1)
                    {
                        int prev = _setOptions [0];

                        if (!_config._allowEmpty && prev == FocusedOption)
                        {
                            break;
                        }

                        _setOptions [0] = FocusedOption;
                        _setOptions.Add (prev);
                        _setOptions.Sort ();
                        _options [FocusedOption].OnSet ();
                    }
                    else if (_setOptions.Count == 0)
                    {
                        _setOptions.Add (FocusedOption);
                        _options [FocusedOption].OnSet ();
                        int next = FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption - 1;
                        _setOptions.Add (next);
                        _options [next].OnSet ();
                    }
                    else
                    {
                        // Extend/Shrink
                        if (FocusedOption < _setOptions [0])
                        {
                            // extend left
                            _options [_setOptions [0]].OnUnSet ();
                            _setOptions [0] = FocusedOption;
                        }
                        else if (FocusedOption > _setOptions [1])
                        {
                            // extend right
                            _options [_setOptions [1]].OnUnSet ();
                            _setOptions [1] = FocusedOption;
                        }
                        else if (FocusedOption >= _setOptions [0] && FocusedOption <= _setOptions [1] && _setOptions [1] - _setOptions [0] > 1)
                        {
                            if (FocusedOption < _lastFocusedOption)
                            {
                                // shrink to the left
                                _options [_setOptions [1]].OnUnSet ();
                                _setOptions [1] = FocusedOption;
                            }
                            else if (FocusedOption > _lastFocusedOption)
                            {
                                // shrink to the right
                                _options [_setOptions [0]].OnUnSet ();
                                _setOptions [0] = FocusedOption;
                            }
                        }
                    }
                }

                // Raise LinearRange Option Changed Event.
                RaiseSelectionChanged ();
                changed = true;

                break;

            default:
                throw new ArgumentOutOfRangeException (_config._renderMode.ToString ());
        }

        return changed;
    }

    internal bool ExtendPlus ()
    {
        int next = _options is { } && FocusedOption < _options.Count - 1 ? FocusedOption + 1 : FocusedOption;

        if (next != FocusedOption && !OnOptionFocused (next, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool ExtendMinus ()
    {
        int prev = FocusedOption > 0 ? FocusedOption - 1 : FocusedOption;

        if (prev != FocusedOption && !OnOptionFocused (prev, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
        {
            SetFocusedOption ();
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        SetFocusedOption ();
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        SetFocusedOption ();

        return false;
    }

    internal bool Select () => SetFocusedOption ();

    internal bool Accept (ICommandContext? commandContext)
    {
        SetFocusedOption ();

        return RaiseAccepting (commandContext) == true;
    }

    internal bool MovePlus ()
    {
        bool cancelled = OnOptionFocused (FocusedOption + 1, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption));

        if (cancelled)
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool MoveMinus ()
    {
        bool cancelled = OnOptionFocused (FocusedOption - 1, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption));

        if (cancelled)
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool MoveStart ()
    {
        if (OnOptionFocused (0, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    internal bool MoveEnd ()
    {
        if (OnOptionFocused (_options!.Count - 1, new LinearRangeEventArgs<TOption> (GetSetOptionDictionary (), FocusedOption)))
        {
            return false;
        }

        if (!AllowEmpty)
        {
            SetFocusedOption ();
        }

        return true;
    }

    #endregion
}
