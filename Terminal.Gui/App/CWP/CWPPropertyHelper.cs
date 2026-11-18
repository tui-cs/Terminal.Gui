namespace Terminal.Gui.App;

/// <summary>
///     Provides helper methods for executing property change workflows in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     <para>
///         Used for workflows where a property value is modified, such as in <see cref="OrientationHelper"/> or
///         <see cref="View.SchemeName"/>, allowing pre- and post-change events to customize or cancel the change.
///     </para>
/// </remarks>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ValueChangedEventArgs{T}"/>
public static class CWPPropertyHelper
{
    /// <summary>
    ///     Executes a CWP workflow for a property change, with pre- and post-change events.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the property value, which may be a nullable reference type (e.g., <see cref="string"/>
    ///     ?).
    /// </typeparam>
    /// <param name="sender">The sender of the event. Will be provided as the sender in <paramref name="changingEvent"/>
    /// and <paramref name="changedEvent"/>.</param>
    /// <param name="currentValue">
    ///     Reference to the current property value, which may be null for nullable types. If the change is not cancelled, this
    ///     will be set to <paramref name="finalValue"/>.
    /// </param>
    /// <param name="newValue">The proposed new property value, which may be null for nullable types.</param>
    /// <param name="onChanging">The virtual method invoked before the change, returning true to cancel.</param>
    /// <param name="changingEvent">The pre-change event raised to allow modification or cancellation.</param>
    /// <param name="doWork">The action that performs the actual work of setting the property (e.g., updating backing field, calling related methods).</param>
    /// <param name="onChanged">The virtual method invoked after the change.</param>
    /// <param name="changedEvent">The post-change event raised to notify of the completed change.</param>
    /// <param name="finalValue">
    ///     The final value after the workflow, reflecting any modifications, which may be null for
    ///     nullable types.
    /// </param>
    /// <returns>True if the property was changed, false if cancelled.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <see cref="ValueChangingEventArgs{T}.NewValue"/> is null for non-nullable reference types after the
    ///     workflow.
    /// </exception>
    /// <example>
    ///     <code>
    ///         string? current = _schemeName;
    ///         string? proposed = "Base";
    ///         Func&lt;ValueChangingEventArgs&lt;string?&gt;, bool&gt; onChanging = OnSchemeNameChanging;
    ///         EventHandler&lt;ValueChangingEventArgs&lt;string?&gt;&gt;? changingEvent = SchemeNameChanging;
    ///         Action&lt;string?&gt; doWork = value => _schemeName = value;
    ///         Action&lt;ValueChangedEventArgs&lt;string?&gt;&gt;? onChanged = OnSchemeNameChanged;
    ///         EventHandler&lt;ValueChangedEventArgs&lt;string?&gt;&gt;? changedEvent = SchemeNameChanged;
    ///         bool changed = CWPPropertyHelper.ChangeProperty(
    ///             current, proposed, onChanging, changingEvent, doWork, onChanged, changedEvent, out string? final);
    ///     </code>
    /// </example>
    public static bool ChangeProperty<T> (
        object? sender,
        ref T currentValue,
        T newValue,
        Func<ValueChangingEventArgs<T>, bool>? onChanging,
        EventHandler<ValueChangingEventArgs<T>>? changingEvent,
        Action<T> doWork,
        Action<ValueChangedEventArgs<T>>? onChanged,
        EventHandler<ValueChangedEventArgs<T>>? changedEvent,
        out T finalValue
    )
    {
        if (EqualityComparer<T>.Default.Equals (currentValue, newValue))
        {
            finalValue = currentValue;

            return false;
        }

        ValueChangingEventArgs<T> args = new (currentValue, newValue);

        if (onChanging is { })
        {
            bool cancelled = onChanging (args) || args.Handled;

            if (cancelled)
            {
                finalValue = currentValue;

                return false;
            }
        }

        changingEvent?.Invoke (sender, args);

        if (args.Handled)
        {
            finalValue = currentValue;

            return false;
        }

        // Validate NewValue for non-nullable reference types
        if (args.NewValue is null && !typeof (T).IsValueType && !Nullable.GetUnderlyingType (typeof (T))?.IsValueType == true)
        {
            throw new InvalidOperationException ("NewValue cannot be null for non-nullable reference types.");
        }

        finalValue = args.NewValue;

        // Do the work (set backing field, update related properties, etc.) BEFORE raising Changed events
        doWork (finalValue);

        ValueChangedEventArgs<T> changedArgs = new (currentValue, finalValue);
        currentValue = finalValue;
        onChanged?.Invoke (changedArgs);
        changedEvent?.Invoke (sender, changedArgs);

        return true;
    }
}
