using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a scrollable list of data where each item can be activated to perform an action,
///     with a strongly-typed <see cref="Value"/> property that returns the selected object of type
///     <typeparamref name="T"/> from the underlying <see cref="ObservableCollection&lt;T&gt;"/>.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <remarks>
///     <para>
///         <see cref="ListView&lt;T&gt;"/> extends <see cref="ListView"/> by implementing
///         <see cref="IValue&lt;T&gt;"/>. The <see cref="Value"/> property returns the currently selected
///         object of type <typeparamref name="T"/> rather than the selected index.
///     </para>
///     <para>
///         All <see cref="ListView"/> functionality (rendering, marking, keyboard navigation,
///         key and mouse bindings) is inherited unchanged. Use
///         <see cref="SetSource(ObservableCollection&lt;T&gt;?)"/> to provide the typed source collection.
///     </para>
///     <para>
///         The base <see cref="ListView.Value"/> (index-based, <see cref="IValue&lt;T&gt;"/> with
///         <c>T = int?</c>) remains accessible by casting to <see cref="ListView"/> or
///         <c>IValue&lt;int?&gt;</c>.
///     </para>
/// </remarks>
public class ListView<T> : ListView, IValue<T>
{
    private ObservableCollection<T>? _typedSource;

    /// <summary>
    ///     Initializes a new instance of <see cref="ListView&lt;T&gt;"/>.
    /// </summary>
    public ListView ()
    {
        base.ValueChanging += TranslateValueChanging;
        base.ValueChanged += TranslateValueChanged;
    }

    /// <summary>
    ///     Sets the source collection and updates the display.
    /// </summary>
    /// <param name="source">
    ///     The <see cref="ObservableCollection&lt;T&gt;"/> to display,
    ///     or <see langword="null"/> to clear the list.
    /// </param>
    public void SetSource (ObservableCollection<T>? source)
    {
        _typedSource = source;
        base.SetSource (source);
    }

    #region IValue<T> Implementation

    /// <summary>
    ///     Gets or sets the currently selected item as a <typeparamref name="T"/> object.
    /// </summary>
    /// <value>
    ///     The selected item, or <see langword="null"/> if no item is selected or the source is not set.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         The getter retrieves the object at the selected index from the typed source collection.
    ///     </para>
    ///     <para>
    ///         The setter locates the object in the collection and updates
    ///         <see cref="ListView.SelectedItem"/> to the corresponding index.
    ///     </para>
    ///     <para>
    ///         If <paramref name="value"/> is <see langword="null"/>, the selection is cleared.
    ///     </para>
    ///     <para>
    ///         If the source collection has not been set, or if <paramref name="value"/> is not found
    ///         in the collection, the setter is a no-op and the selection remains unchanged.
    ///         This differs from the base <see cref="ListView.SelectedItem"/> setter, which throws
    ///         <see cref="ArgumentException"/> for an out-of-range index. Here, a value not present in
    ///         the collection is not considered an error — the caller may hold a stale reference or the
    ///         collection may have changed since the reference was obtained.
    ///     </para>
    /// </remarks>
    public new T? Value
    {
        get => GetObjectAt (base.SelectedItem);
        set
        {
            if (value is null)
            {
                base.SelectedItem = null;

                return;
            }

            if (_typedSource is null)
            {
                return;
            }

            int index = _typedSource.IndexOf (value);

            if (index < 0)
            {
                return;
            }

            base.SelectedItem = index;
        }
    }

    /// <summary>
    ///     Gets the currently selected item as a boxed <typeparamref name="T"/> object.
    /// </summary>
    /// <returns>
    ///     The selected item of type <typeparamref name="T"/>, boxed as <see cref="object"/>,
    ///     or <see langword="null"/> if no item is selected.
    /// </returns>
    /// <remarks>
    ///     This explicit implementation overrides the base <see cref="ListView"/>'s behavior,
    ///     which returns the selected index as <see langword="int?"/>.
    ///     Here, the returned value is the selected object from the typed source collection,
    ///     consistent with <see cref="Value"/>.
    /// </remarks>
    object? IValue.GetValue () => Value;

    /// <inheritdoc/>
    /// <remarks>
    ///     Resolves the diamond between <see cref="ListView"/>'s <c>IValue&lt;int?&gt;</c> and
    ///     this view's <c>IValue&lt;T&gt;</c> by parsing into <typeparamref name="T"/>.
    /// </remarks>
    bool IValue.TrySetValueFromString (string input)
    {
        if (!IValueParser.TryParseValue (input, out T? parsed))
        {
            return false;
        }

        Value = parsed;

        return true;
    }

    /// <summary>
    ///     Gets or sets the currently selected object.
    ///     This is a convenience property that is an alias for <see cref="Value"/>.
    /// </summary>
    /// <value>
    ///     The selected object of type <typeparamref name="T"/>,
    ///     or <see langword="null"/> if no item is selected.
    /// </value>
    public new T? SelectedItem { get => Value; set => Value = value; }

    /// <summary>
    ///     Gets or sets the zero-based index of the currently selected item.
    /// </summary>
    /// <value>
    ///     The index of the selected item, or <see langword="null"/> if no item is selected.
    /// </value>
    /// <remarks>
    ///     Use this property to get or set the selection by index directly.
    ///     To get or set the selection by object, use <see cref="SelectedItem"/> or <see cref="Value"/>.
    /// </remarks>
    public int? Index { get => base.Value; set => base.Value = value; }

    /// <summary>
    ///     Called when <see cref="Value"/> is about to change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed typed values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<T?> args) => false;

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public new event EventHandler<ValueChangingEventArgs<T?>>? ValueChanging;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new typed values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<T?> args) { }

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public new event EventHandler<ValueChangedEventArgs<T?>>? ValueChanged;

    /// <inheritdoc/>
    public new event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    #endregion IValue<T> Implementation

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            base.ValueChanging -= TranslateValueChanging;
            base.ValueChanged -= TranslateValueChanged;
        }

        base.Dispose (disposing);
    }

    private T? GetObjectAt (int? index)
    {
        if (index is null || _typedSource is null || index < 0 || index >= _typedSource.Count)
        {
            return default (T?);
        }

        return _typedSource [index.Value];
    }

    private void TranslateValueChanging (object? sender, ValueChangingEventArgs<int?> intArgs)
    {
        T? oldObj = GetObjectAt (intArgs.CurrentValue);
        T? newObj = GetObjectAt (intArgs.NewValue);
        ValueChangingEventArgs<T?> tArgs = new (oldObj, newObj);

        if (OnValueChanging (tArgs) || tArgs.Handled)
        {
            intArgs.Handled = true;

            return;
        }

        ValueChanging?.Invoke (this, tArgs);

        if (tArgs.Handled)
        {
            intArgs.Handled = true;
        }
    }

    private void TranslateValueChanged (object? sender, ValueChangedEventArgs<int?> intArgs)
    {
        T? oldObj = GetObjectAt (intArgs.OldValue);
        T? newObj = GetObjectAt (intArgs.NewValue);
        ValueChangedEventArgs<T?> tArgs = new (oldObj, newObj);
        OnValueChanged (tArgs);
        ValueChanged?.Invoke (this, tArgs);
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldObj, newObj));
    }
}
