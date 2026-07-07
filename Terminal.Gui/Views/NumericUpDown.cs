using System.Globalization;
using System.Numerics;

namespace Terminal.Gui.Views;

/// <summary>
///     Enables the user to increase or decrease a value with the mouse or keyboard in type-safe way.
/// </summary>
/// <remarks>
/// <img src="../images/views/NumericUpDown.gif" alt="NumericUpDown demo"/>
///     <para>
///         Supports the following types: <see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="double"/>
///         ,
///         <see cref="decimal"/>. Attempting to use any other type will result in an
///         <see cref="InvalidOperationException"/>.
///     </para>
///     <para>
///         Default key bindings are inherited from <see cref="View.DefaultKeyBindings"/>:
///     </para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>CursorUp</term> <description>Increases the value (<see cref="Command.Up"/>).</description>
///         </item>
///         <item>
///             <term>CursorDown</term> <description>Decreases the value (<see cref="Command.Down"/>).</description>
///         </item>
///     </list>
///     <para>
///         View-specific bindings can be added via <see cref="DefaultKeyBindings"/>.
///     </para>
/// </remarks>
public class NumericUpDown<T> : View, IValue<T> where T : notnull
{
    /// <summary>
    ///     Gets or sets the view-specific default key bindings for <see cref="NumericUpDown{T}"/>. All standard navigation
    ///     bindings are inherited from <see cref="View.DefaultKeyBindings"/>, so this dictionary is empty by default.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ();

    private readonly ScrollButton _down;
    private readonly ScrollButton _up;

    private View _number;
    private TextField? _editor;
    private bool _updatingText;
    private bool _canEdit;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NumericUpDown{T}"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public NumericUpDown ()
    {
        Type type = typeof (T);

        if (!(type == typeof (object) || NumericHelper.SupportsType (type)))
        {
            throw new InvalidOperationException ("T must be a numeric type that supports addition and subtraction.");
        }

        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        _down = new ScrollButton
        {
            Orientation = Orientation.Vertical,
            Direction = NavigationDirection.Forward
        };

        _number = CreateNumberView ();

        _up = new ScrollButton
        {
            X = Pos.Right (_number),
            Y = Pos.Top (_number),
            Orientation = Orientation.Vertical,
            Direction = NavigationDirection.Backward
        };

        _down.Accepting += OnDownButtonOnAccept;
        _up.Accepting += OnUpButtonOnAccept;

        Add (_down, _number, _up);

        // `object` is supported only for AllViewsTester
        if (type != typeof (object))
        {
            if (NumericHelper.TryGetHelper (typeof (T), out INumericHelper? helper))
            {
                Increment = (T)helper!.One;
                Value = (T)helper.Zero;
            }
        }

        AddCommand (Command.Up,
                    _ =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        InvokeCommand (Command.Activate);

                        if (Value is { } v && Increment is { } i && NumericHelper.TryGetHelper (typeof (T), out INumericHelper? helper))
                        {
                            Value = (T)helper!.Add (v, i);
                        }

                        return true;
                    });

        AddCommand (Command.Down,
                    _ =>
                    {
                        if (type == typeof (object))
                        {
                            return false;
                        }

                        InvokeCommand (Command.Activate);

                        if (Value is { } v && Increment is { } i && NumericHelper.TryGetHelper (typeof (T), out INumericHelper? helper))
                        {
                            Value = (T)helper!.Subtract (v, i);
                        }

                        return true;
                    });

        // Apply layered key bindings (base View layer + NumericUpDown-specific layer)
        ApplyKeyBindings (View.DefaultKeyBindings, DefaultKeyBindings);

        SetText ();

        return;

        void OnDownButtonOnAccept (object? s, CommandEventArgs e)
        {
            InvokeCommand (Command.Down);
            e.Handled = true;
        }

        void OnUpButtonOnAccept (object? s, CommandEventArgs e)
        {
            InvokeCommand (Command.Up);
            e.Handled = true;
        }
    }

    /// <summary>
    ///     Gets or sets whether the value area uses an editable text field instead of the compact display.
    /// </summary>
    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (_canEdit == value)
            {
                return;
            }

            _canEdit = value;
            UpdateNumberView ();
        }
    }

    private T? _value;

    /// <summary>
    ///     Gets or sets the value that will be incremented or decremented.
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

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            _value = changingArgs.NewValue;
            SetText ();

            ValueChangedEventArgs<T?> changedArgs = new (oldValue, _value);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<T?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<T?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called before <see cref="Value"/> changes. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<T?> args) => false;

    /// <summary>
    ///     Called after <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (ValueChangedEventArgs<T?> args) { }

    /// <summary>
    ///     Gets or sets the format string used to display the value. The default is "{0}".
    /// </summary>
    public string Format
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            FormatChanged?.Invoke (this, new EventArgs<string> (value));
            SetText ();
        }
    } = "{0}";

    /// <summary>
    ///     Raised when <see cref="Format"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<string>>? FormatChanged;

    private View CreateNumberView ()
    {
        View view = new ()
        {
            Text = GetDisplayText (),
            X = Pos.Right (_down),
            Y = Pos.Top (_down),
            Width = Dim.Auto (minimumContentDim: Dim.Func (_ => GetNumberWidth ())),
            Height = 1,
            TextAlignment = Alignment.Center,
            CanFocus = true
        };

        return view;
    }

    private TextField CreateEditorView ()
    {
        TextField editor = new ()
        {
            Text = GetDisplayText (),
            X = Pos.Right (_down),
            Y = Pos.Top (_down),
            Width = Dim.Auto (minimumContentDim: Dim.Func (_ => GetNumberWidth ())),
            Height = 1,
            TextAlignment = Alignment.Center,
            CanFocus = true
        };

        editor.ValueChanged += EditorValueChanged;

        return editor;
    }

    private void UpdateNumberView ()
    {
        View oldNumber = _number;
        View newNumber = CanEdit ? CreateEditorView () : CreateNumberView ();

        if (oldNumber != newNumber)
        {
            Remove (oldNumber);
            oldNumber.Dispose ();
        }

        _editor = newNumber as TextField;
        _number = newNumber;
        _number.X = Pos.Right (_down);
        _number.Y = Pos.Top (_down);
        _number.Width = Dim.Auto (minimumContentDim: Dim.Func (_ => GetNumberWidth ()));
        _number.Height = 1;
        _number.TextAlignment = Alignment.Center;
        _number.CanFocus = true;

        _up.X = Pos.Right (_number);
        _up.Y = Pos.Top (_number);

        Add (_number);
        SetNeedsLayout ();
        SetText ();
    }

    private void EditorValueChanged (object? sender, ValueChangedEventArgs<string?> e)
    {
        if (_updatingText || !CanEdit || _editor is null)
        {
            return;
        }

        Text = _editor.Text;

        if (TryParse (_editor.Text, out T? parsedValue))
        {
            Value = parsedValue;
        }
    }

    private bool TryParse (string? text, out T? result)
    {
        if (string.IsNullOrWhiteSpace (text))
        {
            result = default;

            return false;
        }

        try
        {
            object? convertedValue = Convert.ChangeType (text, typeof (T), CultureInfo.CurrentCulture);
            result = (T?)convertedValue;

            return true;
        }
        catch
        {
            result = default;

            return false;
        }
    }

    private int GetNumberWidth ()
    {
        int width = GetDisplayText ().GetColumns ();

        if (CanEdit)
        {
            width++;
        }

        return Math.Max (width, 1);
    }

    private string GetDisplayText () => string.Format (Format, _value);

    private void SetText ()
    {
        string displayText = GetDisplayText ();

        if (_number is TextField textField)
        {
            _updatingText = true;

            try
            {
                textField.Text = displayText;
            }
            finally
            {
                _updatingText = false;
            }
        }
        else
        {
            _number.Text = displayText;
        }

        Text = displayText;
    }

    /// <summary>
    /// </summary>
    public T? Increment
    {
        get;
        set
        {
            if (field is { } oldVal && value is { } && oldVal.Equals (value))
            {
                return;
            }

            field = value;

            IncrementChanged?.Invoke (this, new EventArgs<T> (value!));
        }
    }

    /// <summary>
    ///     Raised when <see cref="Increment"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<T>>? IncrementChanged;

    // Prevent the drawing of Text
    /// <inheritdoc/>
    protected override bool OnDrawingText () => true;

    /// <summary>
    ///     Attempts to convert the specified <paramref name="value"/> to type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to which the value should be converted.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">
    ///     When this method returns, contains the converted value if the conversion succeeded,
    ///     or the default value of <typeparamref name="TValue"/> if the conversion failed.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the conversion was successful; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryConvert<TValue> (object value, out TValue? result)
    {
        try
        {
            result = (TValue)Convert.ChangeType (value, typeof (TValue));

            return true;
        }
        catch
        {
            result = default (TValue);

            return false;
        }
    }
}

/// <summary>
///     Enables the user to increase or decrease an <see langword="int"/> by clicking on the up or down buttons.
/// </summary>
public class NumericUpDown : NumericUpDown<int>;

internal interface INumericHelper
{
    object One { get; }
    object Zero { get; }
    object Add (object a, object b);
    object Subtract (object a, object b);
}

internal static class NumericHelper
{
    private static readonly Dictionary<Type, INumericHelper> _helpers = new ();

    static NumericHelper ()
    {
        // Register known INumber<T> types
        Register<int> ();
        Register<long> ();
        Register<float> ();
        Register<double> ();
        Register<decimal> ();

        // Add more as needed
    }

    private static void Register<T> () where T : INumber<T> => _helpers [typeof (T)] = new NumericHelperImpl<T> ();

    public static bool TryGetHelper (Type t, out INumericHelper? helper) => _helpers.TryGetValue (t, out helper);

    private class NumericHelperImpl<T> : INumericHelper where T : INumber<T>
    {
        public object One => T.One;
        public object Zero => T.Zero;
        public object Add (object a, object b) => (T)a + (T)b;
        public object Subtract (object a, object b) => (T)a - (T)b;
    }

    public static bool SupportsType (Type type) => _helpers.ContainsKey (type);
}
