using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides time editing functionality using <see cref="TextValidateField"/> with culture-aware formatting.
/// </summary>
/// <remarks>
/// <img src="../images/views/TimeEditor.gif" alt="TimeEditor demo"/>
///     <para>
///         TimeEditor extends <see cref="TextValidateField"/> with time-specific functionality:
///         <list type="bullet">
///             <item>
///                 <description>Uses <see cref="TimeTextProvider"/> for validation and formatting</description>
///             </item>
///             <item>
///                 <description>
///                     Supports both 12-hour and 24-hour formats via <see cref="DateTimeFormatInfo"/>
///                 </description>
///             </item>
///             <item>
///                 <description>Cursor automatically skips over separator characters</description>
///             </item>
///             <item>
///                 <description>Supports AM/PM toggling for 12-hour formats</description>
///             </item>
///             <item>
///                 <description>Auto-adjusts width based on time pattern</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Usage Examples:</b>
///         <code>
///         // Use default (current culture's long time pattern)
///         TimeEditor timeEditor = new () { Value = TimeSpan.FromHours (14.5) };
///         // en-US displays: " 2:30:00 PM"
///         // en-GB displays: " 14:30:00"
/// 
///         // Use specific culture's format
///         timeEditor.Format = CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat;
///         // Displays: " 14:30:00"
/// 
///         // Want short time? Modify the LongTimePattern
///         DateTimeFormatInfo format = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
///         format.LongTimePattern = format.ShortTimePattern;
///         timeEditor.Format = format;
///         // en-US displays: " 2:30 PM"
/// 
///         // Custom pattern with milliseconds
///         DateTimeFormatInfo customFormat = (DateTimeFormatInfo)CultureInfo.CurrentCulture.DateTimeFormat.Clone ();
///         customFormat.LongTimePattern = "HH:mm:ss.fff";
///         timeEditor.Format = customFormat;
///         // Displays: " 14:30:00.000"
///         </code>
///     </para>
/// </remarks>
public class TimeEditor : TextValidateField, IValue<TimeSpan>, IDesignable
{
    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="TimeEditor"/>. All standard bindings are
    ///     inherited from <see cref="TextValidateField.DefaultKeyBindings"/> and <see cref="View.DefaultKeyBindings"/>,
    ///     so this dictionary is empty by default.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ();

    private TimeTextProvider TimeProvider => (TimeTextProvider)Provider!;

    private TimeSpan _value;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TimeEditor"/> class.
    /// </summary>
    public TimeEditor ()
    {
        Provider = new TimeTextProvider ();

        // Add one so there is always a blank cell after the last editable character for the cursor.
        Width = Dim.Auto (minimumContentDim: Provider!.DisplayText.Length + 1);
        _value = TimeProvider.TimeValue;

        // Apply TimeEditor-specific bindings (empty by default; enables user overrides via DefaultKeyBindings)
        ApplyKeyBindings (DefaultKeyBindings);
    }

    /// <summary>
    ///     Gets or sets the <see cref="DateTimeFormatInfo"/> used for time formatting.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The editor uses <see cref="DateTimeFormatInfo.LongTimePattern"/> to determine the display format.
    ///         To use a short time format, clone the DateTimeFormatInfo and set LongTimePattern to ShortTimePattern.
    ///     </para>
    ///     <para>
    ///         The width automatically adjusts when the format changes to accommodate the new pattern.
    ///     </para>
    /// </remarks>
    public DateTimeFormatInfo Format
    {
        get => TimeProvider.Format;
        set
        {
            TimeProvider.Format = value;

            // Add one so there is always a blank cell after the last editable character for the cursor.
            Width = TimeProvider.DisplayText.Length + 1;
            SetNeedsDraw ();
        }
    }

    #region IValue<TimeSpan> Implementation

    /// <summary>
    ///     Gets or sets the current time value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property follows the Cancellable Work Pattern (CWP) using
    ///         <see cref="CWPPropertyHelper.ChangeProperty{T}"/>.
    ///         The change can be prevented by handling <see cref="ValueChanging"/> and setting
    ///         <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public new TimeSpan Value
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
                                                  SuppressValueEvents = true;
                                                  TimeProvider.TimeValue = newValue;
                                                  base.Text = TimeProvider.Text;
                                                  SuppressValueEvents = false;
                                                  SetNeedsDraw ();
                                              },
                                              OnValueChanged,
                                              ValueChanged,
                                              out _);
    }

    /// <inheritdoc/>
    public new event EventHandler<ValueChangingEventArgs<TimeSpan>>? ValueChanging;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<TimeSpan>>? ValueChanged;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    object? IValue.GetValue () => Value;

    /// <inheritdoc/>
    /// <remarks>
    ///     Resolves the diamond between <see cref="TextValidateField"/>'s <c>IValue&lt;string&gt;</c>
    ///     and this view's <c>IValue&lt;TimeSpan&gt;</c> by parsing into <see cref="TimeSpan"/>.
    /// </remarks>
    bool IValue.TrySetValueFromString (string input)
    {
        if (!IValueParser.TryParseValue (input, out TimeSpan parsed))
        {
            return false;
        }

        Value = parsed;

        return true;
    }

    /// <summary>
    ///     Synchronizes the <see cref="TimeSpan"/> backing field when the base class
    ///     <see cref="View.Text"/> property changes programmatically.
    /// </summary>
    protected override void OnValueChanged (ValueChangedEventArgs<string?> args) => _value = TimeProvider.TimeValue;

    /// <summary>
    ///     Called when the <see cref="Value"/> is about to change.
    ///     Allows derived classes to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<TimeSpan> args) => false;

    /// <summary>
    ///     Called when the <see cref="Value"/> has changed.
    ///     Allows derived classes to react to value changes.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<TimeSpan> args) =>
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue));

    #endregion

    /// <summary>
    ///     Handles provider text changes for TimeEditor by raising <see cref="TimeSpan"/>-typed
    ///     value events instead of string-typed events.
    /// </summary>
    protected override void HandleProviderTextChanged (string oldText, string newText)
    {
        TimeSpan newTimeValue = TimeProvider.TimeValue;

        if (_value == newTimeValue)
        {
            return;
        }

        TimeSpan oldTimeValue = _value;
        ValueChangingEventArgs<TimeSpan> args = new (oldTimeValue, newTimeValue);

        if (OnValueChanging (args) || args.Handled)
        {
            RevertTimeValue (oldTimeValue);

            return;
        }

        ValueChanging?.Invoke (this, args);

        if (args.Handled)
        {
            RevertTimeValue (oldTimeValue);

            return;
        }

        _value = newTimeValue;
        ValueChangedEventArgs<TimeSpan> changedArgs = new (oldTimeValue, newTimeValue);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);
    }

    private void RevertTimeValue (TimeSpan oldValue)
    {
        SuppressValueEvents = true;
        TimeProvider.TimeValue = oldValue;
        base.Text = TimeProvider.Text;
        SuppressValueEvents = false;
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,CursorRight,wait:300,CursorUp,wait:600,CursorUp,wait:800";

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Value = new TimeSpan (14, 30, 0);

        return true;
    }
}
