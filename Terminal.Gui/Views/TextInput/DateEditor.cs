using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides date editing functionality using <see cref="TextValidateField"/> with culture-aware formatting.
/// </summary>
/// <remarks>
/// <img src="../images/views/DateEditor.gif" alt="DateEditor demo"/>
///     <para>
///         DateEditor extends <see cref="TextValidateField"/> with date-specific functionality:
///         <list type="bullet">
///             <item>
///                 <description>Uses <see cref="DateTextProvider"/> for validation and formatting</description>
///             </item>
///             <item>
///                 <description>
///                     Supports culture-specific date formats via <see cref="DateTimeFormatInfo"/>
///                 </description>
///             </item>
///             <item>
///                 <description>Cursor automatically skips over separator characters</description>
///             </item>
///             <item>
///                 <description>Auto-adjusts width based on date pattern</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Usage Examples:</b>
///         <code>
///         // Use default (current culture's short date pattern)
///         DateEditor dateEditor = new () { Value = new DateTime (2024, 3, 15) };
///         // en-US displays: "03/15/2024"
///         // en-GB displays: "15/03/2024"
///
///         // Use specific culture's format
///         dateEditor.Format = CultureInfo.GetCultureInfo ("de-DE").DateTimeFormat;
///         // Displays: "15.03.2024"
///         </code>
///     </para>
/// </remarks>
public class DateEditor : TextValidateField, IValue<DateTime>, IDesignable
{
    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="DateEditor"/>. All standard bindings are
    ///     inherited from <see cref="TextValidateField.DefaultKeyBindings"/> and <see cref="View.DefaultKeyBindings"/>,
    ///     so this dictionary is empty by default.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ();

    private DateTextProvider DateProvider => (DateTextProvider)Provider!;

    private DateTime _value;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DateEditor"/> class.
    /// </summary>
    public DateEditor ()
    {
        Provider = new DateTextProvider ();

        // Add one so there is always a blank cell after the last editable character for the cursor.
        Width = Dim.Auto (minimumContentDim: Provider!.DisplayText.Length + 1);
        _value = DateProvider.DateValue;

        // Apply DateEditor-specific bindings (empty by default; enables user overrides via DefaultKeyBindings)
        ApplyKeyBindings (DefaultKeyBindings);
    }

    /// <summary>
    ///     Gets or sets the <see cref="DateTimeFormatInfo"/> used for date formatting.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The editor uses <see cref="DateTimeFormatInfo.ShortDatePattern"/> to determine the display format.
    ///     </para>
    ///     <para>
    ///         The width automatically adjusts when the format changes to accommodate the new pattern.
    ///     </para>
    /// </remarks>
    public DateTimeFormatInfo Format
    {
        get => DateProvider.Format;
        set
        {
            DateProvider.Format = value;

            // Add one so there is always a blank cell after the last editable character for the cursor.
            Width = DateProvider.DisplayText.Length + 1;
            SetNeedsDraw ();
        }
    }

    #region IValue<DateTime?> Implementation

    /// <summary>
    ///     Gets or sets the current date value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property follows the Cancellable Work Pattern (CWP) using
    ///         <see cref="CWPPropertyHelper.ChangeProperty{T}"/>.
    ///         The change can be prevented by handling <see cref="ValueChanging"/> and setting
    ///         <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public new DateTime Value
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
                                                  DateProvider.DateValue = newValue;
                                                  base.Text = DateProvider.Text;
                                                  SuppressValueEvents = false;
                                                  SetNeedsDraw ();
                                              },
                                              OnValueChanged,
                                              ValueChanged,
                                              out _);
    }

    /// <inheritdoc/>
    public new event EventHandler<ValueChangingEventArgs<DateTime>>? ValueChanging;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<DateTime>>? ValueChanged;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    object? IValue.GetValue () => Value;

    /// <inheritdoc/>
    /// <remarks>
    ///     Resolves the diamond between <see cref="TextValidateField"/>'s <c>IValue&lt;string&gt;</c>
    ///     and this view's <c>IValue&lt;DateTime&gt;</c> by parsing into <see cref="DateTime"/>.
    /// </remarks>
    bool IValue.TrySetValueFromString (string input)
    {
        if (!IValueParser.TryParseValue (input, out DateTime parsed))
        {
            return false;
        }

        Value = parsed;

        return true;
    }

    /// <summary>
    ///     Synchronizes the <see cref="DateTime"/> backing field when the base class
    ///     <see cref="View.Text"/> property changes programmatically.
    /// </summary>
    protected override void OnValueChanged (ValueChangedEventArgs<string?> args) => _value = DateProvider.DateValue;

    /// <summary>
    ///     Called when the <see cref="Value"/> is about to change.
    ///     Allows derived classes to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<DateTime> args) => false;

    /// <summary>
    ///     Called when the <see cref="Value"/> has changed.
    ///     Allows derived classes to react to value changes.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<DateTime> args) =>
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (args.OldValue, args.NewValue));

    #endregion

    /// <summary>
    ///     Handles provider text changes for DateEditor by raising <see cref="DateTime"/>-typed
    ///     value events instead of string-typed events.
    /// </summary>
    protected override void HandleProviderTextChanged (string oldText, string newText)
    {
        DateTime newDateValue = DateProvider.DateValue;

        if (_value == newDateValue)
        {
            return;
        }

        DateTime oldDateValue = _value;
        ValueChangingEventArgs<DateTime> args = new (oldDateValue, newDateValue);

        if (OnValueChanging (args) || args.Handled)
        {
            RevertDateValue (oldDateValue);

            return;
        }

        ValueChanging?.Invoke (this, args);

        if (args.Handled)
        {
            RevertDateValue (oldDateValue);

            return;
        }

        _value = newDateValue;
        ValueChangedEventArgs<DateTime> changedArgs = new (oldDateValue, newDateValue);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);
    }

    private void RevertDateValue (DateTime oldValue)
    {
        SuppressValueEvents = true;
        DateProvider.DateValue = oldValue;

        base.Text = DateProvider.Text;
        SuppressValueEvents = false;
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,CursorRight,wait:300,CursorRight,wait:300,CursorUp,wait:600,CursorUp,wait:800";

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Value = new DateTime (2024, 1, 15);

        return true;
    }
}
