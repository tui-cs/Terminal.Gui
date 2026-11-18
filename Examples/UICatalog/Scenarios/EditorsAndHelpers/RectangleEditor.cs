#nullable enable
namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for editing a Rectangle value (Location and Size).
/// </summary>
public sealed class RectangleEditor : View, IValue<Rectangle?>
{
    private TwoIntEditor<Point>? _locationEditor;
    private TwoIntEditor<Size>? _sizeEditor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RectangleEditor"/> class.
    /// </summary>
    public RectangleEditor ()
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        CanFocus = true;
    }

    private Rectangle? _value;

    /// <summary>
    ///     Gets or sets the Rectangle value being edited.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ValueChanging"/> and <see cref="ValueChanged"/> events are raised when the value changes.
    ///         Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    ///     </para>
    /// </remarks>
    public Rectangle? Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<Rectangle?>.Default.Equals (_value, value))
            {
                return;
            }

            Rectangle? oldValue = _value;
            ValueChangingEventArgs<Rectangle?> changingArgs = new (oldValue, value);

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _value = changingArgs.NewValue;
            UpdateEditors ();

            ValueChangedEventArgs<Rectangle?> changedArgs = new (oldValue, _value);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
        }
    }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<Rectangle?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<Rectangle?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    private void UpdateEditors ()
    {
        if (_locationEditor is { })
        {
            _locationEditor.Value = _value?.Location;
        }

        if (_sizeEditor is { })
        {
            _sizeEditor.Value = _value?.Size;
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        _locationEditor = TwoIntEditor<Point>.ForPoint ();
        _locationEditor.Value = _value?.Location;
        _locationEditor.ValueChanged += LocationValueChanged;

        _sizeEditor = TwoIntEditor<Size>.ForSize ();
        _sizeEditor.X = Pos.Right (_locationEditor) + 1;
        _sizeEditor.Value = _value?.Size;
        _sizeEditor.ValueChanged += SizeValueChanged;

        Add (_locationEditor, _sizeEditor);
    }

    private void LocationValueChanged (object? sender, ValueChangedEventArgs<Point?> e)
    {
        if (e.NewValue is null)
        {
            return;
        }

        Size size = _value?.Size ?? Size.Empty;
        Value = new Rectangle (e.NewValue.Value, size);
    }

    private void SizeValueChanged (object? sender, ValueChangedEventArgs<Size?> e)
    {
        if (e.NewValue is null)
        {
            return;
        }

        Point location = _value?.Location ?? Point.Empty;
        Value = new Rectangle (location, e.NewValue.Value);
    }
}
