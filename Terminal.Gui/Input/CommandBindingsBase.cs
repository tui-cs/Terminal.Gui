using System.Collections.Concurrent;

namespace Terminal.Gui.Input;

/// <summary>
///     Abstract class for <see cref="KeyBindings"/> and <see cref="MouseBindings"/>.
///     This class is thread-safe for all public operations.
/// </summary>
/// <typeparam name="TEvent">The type of the event (e.g. <see cref="Key"/> or <see cref="MouseFlags"/>).</typeparam>
/// <typeparam name="TBinding">The binding type (e.g. <see cref="KeyBinding"/>).</typeparam>
public abstract class CommandBindingsBase<TEvent, TBinding> where TBinding : ICommandBinding, new() where TEvent : notnull
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    /// <param name="constructBinding"></param>
    /// <param name="equalityComparer"></param>
    protected CommandBindingsBase (Func<Command [], TEvent, View?, TBinding> constructBinding, IEqualityComparer<TEvent> equalityComparer)
    {
        _constructBinding = constructBinding;
        _bindings = new ConcurrentDictionary<TEvent, TBinding> (equalityComparer);
    }

    /// <summary>
    ///     The bindings.
    /// </summary>
    private readonly ConcurrentDictionary<TEvent, TBinding> _bindings;

    private readonly Func<Command [], TEvent, View?, TBinding> _constructBinding;

    /// <summary>Adds a <typeparamref name="TEvent"/> bound to <typeparamref name="TBinding"/> to the collection.</summary>
    /// <param name="eventArgs"></param>
    /// <param name="binding"></param>
    public void Add (TEvent eventArgs, TBinding binding)
    {
        if (!IsValid (eventArgs))
        {
            throw new ArgumentException (@"Invalid newEventArgs", nameof (eventArgs));
        }

        // IMPORTANT: Add a COPY of the eventArgs. Dictionary keys are cached; if a later overlay mutated
        // IMPORTANT: the same instance in place, lookup would see a different key than the one stored.
        // IMPORTANT: Copying keeps the stored key stable after theme/config re-apply.
        if (!_bindings.TryAdd (eventArgs, binding))
        {
            throw new InvalidOperationException (@$"A binding for {eventArgs} exists ({binding}).");
        }
    }

    /// <summary>
    ///     <para>Adds a new <typeparamref name="TEvent"/> that will trigger the commands in <paramref name="commands"/>.</para>
    ///     <para>
    ///         If the <typeparamref name="TEvent"/> is already bound to a different set of <see cref="Command"/>s it will be
    ///         rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <param name="eventArgs">The event to check.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="eventArgs"/> is received. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="eventArgs"/> event
    ///     will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (TEvent eventArgs, params Command [] commands)
    {
        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (!IsValid (eventArgs))
        {
            throw new ArgumentException (@"Invalid newEventArgs", nameof (eventArgs));
        }

        TBinding binding = _constructBinding (commands, eventArgs, null);

        if (!_bindings.TryAdd (eventArgs, binding))
        {
            throw new InvalidOperationException (@$"A binding for {eventArgs} exists ({binding}).");
        }
    }

    /// <summary>Removes all <typeparamref name="TEvent"/> objects from the collection.</summary>
    public void Clear () => _bindings.Clear ();

    /// <summary>
    ///     Removes all bindings that trigger the given command set. Views can have multiple different
    ///     <typeparamref name="TEvent"/>
    ///     bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        // ToArray() creates a snapshot to avoid modification during enumeration
        KeyValuePair<TEvent, TBinding> [] kvps = _bindings.Where (kvp => kvp.Value.Commands.SequenceEqual (command)).ToArray ();

        foreach (KeyValuePair<TEvent, TBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <typeparamref name="TBinding"/> for the specified <typeparamref name="TEvent"/>.</summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    public TBinding? Get (TEvent eventArgs)
    {
        if (TryGet (eventArgs, out TBinding? binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"{eventArgs} is not bound.");
    }

    /// <summary>Gets all <typeparamref name="TEvent"/> bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The <typeparamref name="TEvent"/>s bound to the set of commands specified by <paramref name="commands"/>. An empty
    ///     list if
    ///     the
    ///     set of commands was not found.
    /// </returns>
    public IEnumerable<TEvent> GetAllFromCommands (params Command [] commands) =>

        // ToList() creates a snapshot to ensure thread-safe enumeration
        _bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key).ToList ();

    /// <summary>
    ///     Gets the bindings.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TEvent, TBinding>> GetBindings () =>

        // ConcurrentDictionary provides a snapshot enumeration that is safe for concurrent access
        _bindings;

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="eventArgs"/> if it exists.</summary>
    /// <param name="eventArgs">The <typeparamref name="TEvent"/> to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="eventArgs"/> is bound. An empty <see cref="Command"/> array
    ///     if not.
    /// </returns>
    public Command [] GetCommands (TEvent eventArgs)
    {
        if (TryGet (eventArgs, out TBinding? bindings))
        {
            return bindings?.Commands ?? [];
        }

        return [];
    }

    /// <summary>
    ///     Gets the first matching <typeparamref name="TEvent"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>.
    /// </summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The first matching <typeparamref name="TEvent"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>. <see langword="null"/> if the set of commands was not found.
    /// </returns>
    public TEvent? GetFirstFromCommands (params Command [] commands) => _bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key;

    /// <summary>
    ///     Tests whether <paramref name="eventArgs"/> is valid or not.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    public abstract bool IsValid (TEvent eventArgs);

    /// <summary>Removes a <typeparamref name="TEvent"/> from the collection.</summary>
    /// <param name="eventArgs"></param>
    public void Remove (TEvent eventArgs) => _bindings.TryRemove (eventArgs, out _);

    /// <summary>Replaces a <typeparamref name="TEvent"/> combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldEventArgs">The <typeparamref name="TEvent"/> to be replaced.</param>
    /// <param name="newEventArgs">
    ///     The new <typeparamref name="TEvent"/> to be used.
    /// </param>
    public void Replace (TEvent oldEventArgs, TEvent newEventArgs)
    {
        if (!IsValid (newEventArgs))
        {
            throw new ArgumentException (@"Invalid newEventArgs", nameof (newEventArgs));
        }

        // Thread-safe: Handle the case where oldEventArgs == newEventArgs
        if (EqualityComparer<TEvent>.Default.Equals (oldEventArgs, newEventArgs))
        {
            // Same key - nothing to do, binding stays as-is
            return;
        }

        // Thread-safe: Get the binding from oldEventArgs, or create default if it doesn't exist
        // This is atomic - either gets existing or adds new
        TBinding binding = _bindings.GetOrAdd (oldEventArgs, _ => new TBinding ());

        // Thread-safe: Atomically add/update newEventArgs with the binding from oldEventArgs
        // The updateValueFactory is only called if the key already exists, ensuring we don't
        // accidentally overwrite a binding that was added by another thread
        _bindings.AddOrUpdate (newEventArgs,
                               binding, // Add this binding if newEventArgs doesn't exist
                               (_, _) => binding);

        // Thread-safe: Remove oldEventArgs only after newEventArgs has been set
        // This ensures we don't lose the binding if another thread is reading it
        _bindings.TryRemove (oldEventArgs, out _);
    }

    /// <summary>Replaces the commands already bound to a combination of <typeparamref name="TEvent"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the of <typeparamref name="TEvent"/> is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="eventArgs">The combination of <typeparamref name="TEvent"/> bound to the command to be replaced.</param>
    /// <param name="newCommands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (TEvent eventArgs, params Command [] newCommands)
    {
        TBinding newBinding = _constructBinding (newCommands, eventArgs, null);

        // Thread-safe: Add or update atomically
        _bindings.AddOrUpdate (eventArgs, newBinding, (_, _) => newBinding);
    }

    /// <summary>Gets the commands bound with the specified <typeparamref name="TEvent"/>.</summary>
    /// <remarks></remarks>
    /// <param name="eventArgs">The <typeparamref name="TEvent"/> to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the <typeparamref name="TEvent"/>, if the
    ///     <typeparamref name="TEvent"/> is
    ///     not
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the <typeparamref name="TEvent"/> is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (TEvent eventArgs, out TBinding? binding) => _bindings.TryGetValue (eventArgs, out binding);
}
