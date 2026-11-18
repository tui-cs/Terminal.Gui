namespace Terminal.Gui.App;

/// <summary>
///     Provides data for events that allow modification or cancellation of a property change in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used in pre-change events raised by <see cref="CWPPropertyHelper.ChangeProperty{T}"/> to allow handlers to
///         modify the proposed value or cancel the change, such as for <see cref="View.SchemeName"/> or
///         <see cref="OrientationHelper"/>.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the property value, which may be a nullable reference type (e.g., <see cref="string"/>?).</typeparam>
/// <seealso cref="ValueChangedEventArgs{T}"/>
/// <seealso cref="CWPPropertyHelper"/>
public class ValueChangingEventArgs<T>
{
    /// <summary>
    ///     Gets the current value before the change.
    /// </summary>
    public T CurrentValue { get; }

    /// <summary>
    ///     Gets or sets the proposed new value, which can be modified by event handlers.
    /// </summary>
    public T NewValue { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the change has been handled. If true, the change is cancelled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueChangingEventArgs{T}"/> class.
    /// </summary>
    /// <param name="currentValue">The current value before the change, which may be null for nullable types.</param>
    /// <param name="newValue">The proposed new value, which may be null for nullable types.</param>
    public ValueChangingEventArgs (T currentValue, T newValue)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }
}
