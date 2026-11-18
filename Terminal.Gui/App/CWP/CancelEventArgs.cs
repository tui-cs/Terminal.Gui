using System.ComponentModel;

namespace Terminal.Gui.App;

#pragma warning disable CS1711

/// <summary>
///      Provides data for events that can be cancelled without a changeable result in a cancellable workflow in the Cancellable Work Pattern (CWP).
/// </summary>
/// <remarks>
///     Used for workflows where a change (e.g., a simple property change) can be cancelled, but the
///     value being changed is not directly modified by the event handlers.
/// </remarks>
/// <typeparam name="T">The type of the value that is being changed.</typeparam>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ResultEventArgs{T}"/>
public class CancelEventArgs<T> : CancelEventArgs where T : notnull
{
    /// <summary>Initializes a new instance of the <see cref="CancelEventArgs{T}"/> class.</summary>
    /// <param name="currentValue">The current (old) value of the property.</param>
    /// <param name="newValue">The value the property will be set to if the event is not cancelled.</param>
    /// <param name="cancel">Whether the event should be canceled or not.</param>
    /// <typeparam name="T">The type of the value for the change being canceled.</typeparam>
    public CancelEventArgs (ref readonly T currentValue, ref T newValue, bool cancel = false) : base (cancel)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CancelEventArgs{T}"/> class.
    /// </summary>
    /// <param name="currentValue">The current (old) value of the property.</param>
    /// <param name="newValue">The value the property will be set to if the event is not cancelled.</param>
    protected CancelEventArgs (T currentValue, T newValue)
    {
        CurrentValue = currentValue;
        NewValue = newValue;
    }

    /// <summary>The current value of the property.</summary>
    public T CurrentValue { get; }

    /// <summary>The value the property will be set to if the event is not cancelled.</summary>
    public T NewValue { get; set; }
}
