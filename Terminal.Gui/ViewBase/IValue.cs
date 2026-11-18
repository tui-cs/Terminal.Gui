namespace Terminal.Gui.ViewBase;

/// <summary>
///     Non-generic interface for accessing a View's value as a boxed object.
/// </summary>
/// <remarks>
///     <para>
///         This interface enables command propagation to carry values without knowing the generic type.
///         When a command is invoked, the source View's value can be captured via <see cref="GetValue"/>
///         and stored in <c>CommandContext.Value</c> for handlers up the hierarchy.
///     </para>
///     <para>
///         Views should implement <see cref="IValue{TValue}"/> rather than this interface directly.
///         The generic interface provides a default implementation of <see cref="GetValue"/>.
///     </para>
/// </remarks>
/// <seealso cref="IValue{TValue}"/>
public interface IValue
{
    /// <summary>
    ///     Gets the value as a boxed object.
    /// </summary>
    /// <returns>The current value, or <see langword="null"/> if no value is set.</returns>
    object? GetValue ();

    /// <summary>
    ///     Raised when <see cref="IValue{TValue}.Value"/> has changed, providing the value as an un-typed object.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Attempts to set <see cref="IValue{TValue}.Value"/> by parsing the supplied string.
    /// </summary>
    /// <param name="input">The string representation of the value to set.</param>
    /// <returns>
    ///     <see langword="true"/> if <paramref name="input"/> was successfully parsed and assigned;
    ///     <see langword="false"/> if the value type cannot be parsed from a string or parsing failed.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The default implementation supports:
    ///     </para>
    ///     <list type="bullet">
    ///         <item><description><see cref="string"/> values (assigned directly).</description></item>
    ///         <item><description>Any type implementing <see cref="IParsable{TSelf}"/> (e.g. <see cref="int"/>, <see cref="double"/>, <see cref="System.DateTime"/>, <see cref="System.DateOnly"/>, <see cref="System.TimeOnly"/>, <see cref="System.TimeSpan"/>, <see cref="Terminal.Gui.Drawing.Color"/>).</description></item>
    ///         <item><description><see cref="System.Nullable{T}"/> wrappers around any of the above.</description></item>
    ///         <item><description><see cref="System.Enum"/> types (case-insensitive).</description></item>
    ///     </list>
    ///     <para>
    ///         Views may override this method to provide custom parsing logic.
    ///     </para>
    /// </remarks>
    bool TrySetValueFromString (string input);
}

/// <summary>
///     Interface for views that provide a strongly-typed value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <remarks>
///     <para>
///         Views implementing this interface can be used with <c>Prompt&lt;TView, TResult&gt;</c>
///         for automatic result extraction without requiring an explicit <c>resultExtractor</c>.
///     </para>
///     <para>
///         Implementers should use <see cref="CWPPropertyHelper.ChangeProperty{T}"/> to implement
///         the <see cref="Value"/> property setter, which follows the Cancellable Work Pattern (CWP).
///     </para>
///     <para>
///         This interface inherits from <see cref="IValue"/> and provides a default implementation
///         of <see cref="IValue.GetValue"/> that returns the boxed <see cref="Value"/>.
///     </para>
/// </remarks>
/// <seealso cref="IValue"/>
/// <seealso cref="CWPPropertyHelper"/>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ValueChangedEventArgs{T}"/>
public interface IValue<TValue> : IValue
{
    /// <summary>
    ///     Gets or sets the value.
    /// </summary>
    TValue? Value { get; set; }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    event EventHandler<ValueChangingEventArgs<TValue?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<TValue?>>? ValueChanged;

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;

    /// <inheritdoc/>
    /// <remarks>
    ///     The default implementation handles <see cref="string"/>, types implementing
    ///     <see cref="IParsable{TSelf}"/>, <see cref="System.Enum"/> types, and <see cref="System.Nullable{T}"/>
    ///     wrappers around any of those. Views with bespoke parsing should override this method.
    /// </remarks>
    bool IValue.TrySetValueFromString (string input)
    {
        if (!IValueParser.TryParseValue (input, out TValue? parsed))
        {
            return false;
        }

        Value = parsed;

        return true;
    }
}
