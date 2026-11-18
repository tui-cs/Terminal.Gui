using System.Collections.Concurrent;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IKeyboard"/> to manage keyboard input and key bindings at the Application level.
///     This implementation is thread-safe for all public operations.
///     <para>
///         This implementation decouples keyboard handling state from the static <see cref="App"/> class,
///         enabling parallelizable unit tests and better testability.
///     </para>
///     <para>
///         See <see cref="IKeyboard"/> for usage details.
///     </para>
/// </summary>
internal class ApplicationKeyboard : IKeyboard, IDisposable
{
    /// <summary>
    ///     Initializes keyboard bindings and subscribes to Application configuration property events.
    /// </summary>
    public ApplicationKeyboard ()
    {
        Application.DefaultKeyBindingsChanged += OnDefaultKeyBindingsChanged;

        AddKeyBindings ();
    }

    /// <summary>
    ///     Commands for Application. Thread-safe for concurrent access.
    /// </summary>
    private readonly ConcurrentDictionary<Command, View.CommandImplementation> _commandImplementations = new ();

    /// <inheritdoc/>
    public void Dispose ()
    {
        Application.DefaultKeyBindingsChanged -= OnDefaultKeyBindingsChanged;
    }

    /// <inheritdoc/>
    public IApplication? App { get; set; }

    /// <inheritdoc/>
    public KeyBindings KeyBindings { get; internal set; } = new ();

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc/>
    public bool RaiseKeyUpEvent (Key key)
    {
        KeyUp?.Invoke (this, key);

        if (key.Handled)
        {
            return true;
        }

        if (App?.TopRunnableView is null)
        {
            if (App?.SessionStack is null)
            {
                return false;
            }

            foreach (IRunnable? runnable in App.SessionStack.Select (r => r.Runnable))
            {
                if (runnable is View view && view.NewKeyUpEvent (key))
                {
                    return true;
                }

                if (runnable is { IsModal: true })
                {
                    break;
                }
            }
        }
        else
        {
            if (App.TopRunnableView.NewKeyUpEvent (key))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public bool RaiseKeyDownEvent (Key key)
    {
        Trace.Keyboard ("app", key, "Entry");

        // TODO: This should match standard event patterns
        KeyDown?.Invoke (this, key);

        if (key.Handled)
        {
            return true;
        }

        IPopoverView? activePopover = App?.Popovers?.GetActivePopover ();

        if (activePopover is { } && App?.Popovers?.DispatchKeyDownToActivePopover (key) is true)
        {
            return true;
        }

        if (App?.TopRunnableView is null)
        {
            if (App?.SessionStack is { })
            {
                foreach (IRunnable? runnable in App.SessionStack.Select (r => r.Runnable))
                {
                    if (runnable is View view && view.NewKeyDownEvent (key))
                    {
                        return true;
                    }

                    if (runnable is { IsModal: true })
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            if (App.TopRunnableView.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        if (App?.Popovers?.DispatchKeyDownToInactivePopovers (key, activePopover) is true)
        {
            return true;
        }

        bool? commandHandled = InvokeCommandsBoundToKey (key);

        if (commandHandled is true)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool? InvokeCommandsBoundToKey (Key key)
    {
        bool? handled = null;

        // Invoke any Application-scoped KeyBindings.
        // The first view that handles the key will stop the loop.
        // foreach (KeyValuePair<Key, KeyBinding> binding in KeyBindings.GetBindings (key))
        if (!KeyBindings.TryGet (key, out KeyBinding binding))
        {
            return handled;
        }
        Trace.Keyboard ("app", key, "Entry");

        if (binding.Target is { })
        {
            if (!binding.Target.Enabled)
            {
                return null;
            }

            handled = binding.Target?.InvokeCommands (binding.Commands, binding with { Source = binding.Target is { } t ? new WeakReference<View> (t) : null });
        }
        else
        {
            bool? toReturn = null;

            foreach (Command command in binding.Commands)
            {
                toReturn = InvokeCommand (command, key, binding);
            }

            handled = toReturn ?? true;
        }

        return handled;
    }

    /// <inheritdoc/>
    public bool? InvokeCommand (Command command, Key key, KeyBinding binding)
    {
        if (!_commandImplementations.ContainsKey (command))
        {
            throw new NotSupportedException (@$"A KeyBinding exists for {command} ({key}) but that command is not supported by Application.");
        }

        if (!_commandImplementations.TryGetValue (command, out View.CommandImplementation? implementation))
        {
            return null;
        }
        CommandContext context = new (command, null, binding); // Create the context here

        return implementation (context);
    }

    internal void AddKeyBindings ()
    {
        _commandImplementations.Clear ();

        // Things Application knows how to do
        AddCommand (Command.Quit,
                    () =>
                    {
                        App?.RequestStop ();

                        return true;
                    });

        AddCommand (Command.Suspend,
                    () =>
                    {
                        App?.Driver?.Suspend ();

                        // When the app is resumed, we need to force a full redraw to clear out any artifacts from the suspended console.
                        App?.ClearScreenNextIteration = true;

                        return true;
                    });

        AddCommand (Command.NextTabStop, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));

        AddCommand (Command.PreviousTabStop, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));

        AddCommand (Command.NextTabGroup, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup));

        AddCommand (Command.PreviousTabGroup, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup));

        AddCommand (Command.Refresh,
                    () =>
                    {
                        App?.LayoutAndDraw (true);

                        return true;
                    });

        AddCommand (Command.Arrange,
                    () =>
                    {
                        View? viewToArrange = App?.Navigation?.GetFocused ();

                        // Go up the superview hierarchy and find the first that is not ViewArrangement.Fixed
                        while (viewToArrange is { Arrangement: ViewArrangement.Fixed })
                        {
                            viewToArrange = viewToArrange switch
                            {
                                AdornmentView adornmentView => adornmentView.Adornment?.Parent,
                                _ => viewToArrange.SuperView
                            };
                        }

                        return viewToArrange is { } ? (viewToArrange.Border.View as BorderView)?.Arranger.EnterArrangeMode (ViewArrangement.Fixed) : false;
                    });

        // Bind keys from DefaultKeyBindings
        KeyBindings.Clear ();

        if (Application.DefaultKeyBindings is { })
        {
            foreach (KeyValuePair<Command, PlatformKeyBinding> entry in Application.DefaultKeyBindings)
            {
                foreach (Key key in entry.Value.GetCurrentPlatformKeys ())
                {
                    KeyBindings.ReplaceCommands (key, entry.Key);
                }
            }
        }

        // Hard-coded cursor arrow aliases for navigation
        KeyBindings.ReplaceCommands (Key.CursorRight, Command.NextTabStop);
        KeyBindings.ReplaceCommands (Key.CursorDown, Command.NextTabStop);
        KeyBindings.ReplaceCommands (Key.CursorLeft, Command.PreviousTabStop);
        KeyBindings.ReplaceCommands (Key.CursorUp, Command.PreviousTabStop);
    }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that do not require a <see cref="ICommandContext"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    private void AddCommand (Command command, Func<bool?> f) => _commandImplementations [command] = _ => f ();

    private void OnDefaultKeyBindingsChanged (object? sender, EventArgs e) => AddKeyBindings ();
}
