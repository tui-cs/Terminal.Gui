namespace Terminal.Gui.App;

/// <summary>
///     Provides data for events that notify of a completed property change in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used in post-change events raised by <see cref="CWPPropertyHelper.ChangeProperty{T}"/> to notify
///         subscribers of a property change, such as in <see cref="OrientationHelper"/> when the
///         <see cref="Orientation"/> property is updated or <see cref="View.SchemeName"/> when the scheme name changes.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the property value, which may be a nullable reference type (e.g., <see cref="string"/>?).</typeparam>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="CWPPropertyHelper"/>
public class ValueChangedEventArgs<T>
{
    /// <summary>
    ///     Gets the value before the change, which may be null for nullable types.
    /// </summary>
    public T OldValue { get; }

    /// <summary>
    ///     Gets the value after the change, which may be null for nullable types.
    /// </summary>
    public T NewValue { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueChangedEventArgs{T}"/> class.
    /// </summary>
    /// <param name="oldValue">The value before the change, which may be null for nullable types.</param>
    /// <param name="newValue">The value after the change, which may be null for nullable types.</param>
    public ValueChangedEventArgs (T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
