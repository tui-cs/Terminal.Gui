#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for editing a value with two integer components (e.g., Point or Size).
/// </summary>
/// <typeparam name="T">The type of value being edited (e.g., <see cref="Point"/> or <see cref="Size"/>).</typeparam>
public sealed class TwoIntEditor<T> : View, IValue<T?> where T : struct
{
    private Label? _firstLabel;
    private NumericUpDown<int>? _firstNumericUpDown;
    private Label? _secondLabel;
    private NumericUpDown<int>? _secondNumericUpDown;

    private readonly Func<T, int> _getFirst;
    private readonly Func<T, int> _getSecond;
    private readonly Func<int, int, T> _create;
    private readonly string _firstLabelText;
    private readonly string _secondLabelText;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TwoIntEditor{T}"/> class.
    /// </summary>
    /// <param name="getFirst">Function to get the first component from a value.</param>
    /// <param name="getSecond">Function to get the second component from a value.</param>
    /// <param name="create">Function to create a new value from two components.</param>
    /// <param name="firstLabel">Label text for the first component (e.g., "X:").</param>
    /// <param name="secondLabel">Label text for the second component (e.g., "Y:").</param>
    public TwoIntEditor (Func<T, int> getFirst, Func<T, int> getSecond, Func<int, int, T> create, string firstLabel = "", string secondLabel = "")
    {
        _getFirst = getFirst;
        _getSecond = getSecond;
        _create = create;
        _firstLabelText = firstLabel;
        _secondLabelText = secondLabel;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        CanFocus = true;
    }

    private T? _value;

    /// <summary>
    ///     Gets or sets the value being edited.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ValueChanging"/> and <see cref="ValueChanged"/> events are raised when the value changes.
    ///         Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    ///     </para>
    /// </remarks>
    public T? Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T?>.Default.Equals (_value, value))
            {
                return;
            }

            T? oldValue = _value;
            ValueChangingEventArgs<T?> changingArgs = new (oldValue, value);

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _value = changingArgs.NewValue;
            UpdateNumericUpDowns ();

            ValueChangedEventArgs<T?> changedArgs = new (oldValue, _value);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
        }
    }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<T?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<T?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    private void UpdateNumericUpDowns ()
    {
        if (_firstNumericUpDown is { })
        {
            _firstNumericUpDown.Value = _value.HasValue ? _getFirst (_value.Value) : 0;
        }

        if (_secondNumericUpDown is { })
        {
            _secondNumericUpDown.Value = _value.HasValue ? _getSecond (_value.Value) : 0;
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        _firstLabel = new Label { Title = _firstLabelText };

        _firstNumericUpDown = new NumericUpDown<int> { X = Pos.Right (_firstLabel), CanFocus = true, Value = _value.HasValue ? _getFirst (_value.Value) : 0 };
        _firstNumericUpDown.ValueChanging += FirstValueChanging;

        _secondLabel = new Label { Title = _secondLabelText, X = Pos.Right (_firstNumericUpDown) + 1 };

        _secondNumericUpDown = new NumericUpDown<int>
        {
            X = Pos.Right (_secondLabel), CanFocus = true, Value = _value.HasValue ? _getSecond (_value.Value) : 0
        };
        _secondNumericUpDown.ValueChanging += SecondValueChanging;

        Add (_firstLabel, _firstNumericUpDown, _secondLabel, _secondNumericUpDown);
    }

    private void FirstValueChanging (object? sender, ValueChangingEventArgs<int> e)
    {
        int second = _value.HasValue ? _getSecond (_value.Value) : 0;
        Value = _create (e.NewValue, second);
    }

    private void SecondValueChanging (object? sender, ValueChangingEventArgs<int> e)
    {
        int first = _value.HasValue ? _getFirst (_value.Value) : 0;
        Value = _create (first, e.NewValue);
    }

    /// <summary>
    ///     Creates a <see cref="TwoIntEditor{T}"/> for editing <see cref="Point"/> values.
    /// </summary>
    public static TwoIntEditor<Point> ForPoint () => new (p => p.X, p => p.Y, (x, y) => new Point (x, y), "X:", "Y:");

    /// <summary>
    ///     Creates a <see cref="TwoIntEditor{T}"/> for editing <see cref="Size"/> values.
    /// </summary>
    public static TwoIntEditor<Size> ForSize () => new (s => s.Width, s => s.Height, (w, h) => new Size (w, h), "W:", "H:");
}
