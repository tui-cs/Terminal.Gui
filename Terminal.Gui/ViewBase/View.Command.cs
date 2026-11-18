using Terminal.Gui.Tracing;

namespace Terminal.Gui.ViewBase;

public partial class View // Command APIs
{
    private readonly Dictionary<Command, CommandImplementation> _commandImplementations = new ();

    /// <summary>
    ///     Helper to configure all things Command related for a View. Called from the View constructor.
    /// </summary>
    private void SetupCommands ()
    {
        // Space or single-click - Raise Activating
        AddCommand (Command.Activate, DefaultActivateHandler);

        // Enter - Raise Accepted
        AddCommand (Command.Accept, DefaultAcceptHandler);

        // HotKey - SetFocus and raise HandlingHotKey
        AddCommand (Command.HotKey, DefaultHotKeyHandler);

        // NotBound - Invoked if no handler is bound
        AddCommand (Command.NotBound, DefaultCommandNotBoundHandler);

        // Paste - Default handler resolves payload (bracketed paste payload or clipboard),
        // sanitizes, raises Pasting/Pasted, and delegates insertion to OnPaste.
        AddCommand (Command.Paste, DefaultPasteHandler);

        // Context - By default, route through the NotBound handler so that
        // CommandNotBound fires (allowing subscribers to handle it). This keeps
        // Command.Context as a "supported" command for mouse-binding purposes
        // while not silently swallowing the invocation.
        AddCommand (Command.Context, DefaultCommandNotBoundHandler);
    }

    #region Command Management

    /// <summary>
    ///     Function signature for command invocations.
    /// </summary>
    /// <param name="ctx">Provides context about the circumstances of invoking the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    public delegate bool? CommandImplementation (ICommandContext? ctx);

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///         AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="impl"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that require <see cref="ICommandContext"/>.
    ///     </para>
    ///     <para>
    ///         See the Commands Deep Dive for more information: <see href="../docs/command.md"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="impl">The delegate.</param>
    protected void AddCommand (Command command, CommandImplementation impl) => _commandImplementations [command] = impl;

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///         AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="impl"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that do not require context.
    ///         If the command requires context, use
    ///         <see cref="AddCommand(Command,CommandImplementation)"/>
    ///     </para>
    ///     <para>
    ///         See the Commands Deep Dive for more information: <see href="../docs/command.md"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="impl">The delegate.</param>
    protected void AddCommand (Command command, Func<bool?> impl) => _commandImplementations [command] = _ => impl ();

    /// <summary>Returns all commands that are supported by this <see cref="View"/>.</summary>
    /// <returns></returns>
    public IEnumerable<Command> GetSupportedCommands () => _commandImplementations.Keys;

    #endregion Command Management

    #region Invoke

    /// <summary>
    ///     Invokes the specified commands.
    /// </summary>
    /// <param name="commands">The set of commands to invoke.</param>
    /// <param name="binding">The binding that caused the invocation, if any. This will be passed as context with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommands (Command [] commands, ICommandBinding? binding)
    {
        bool? toReturn = null;

        foreach (Command command in commands)
        {
            if (!_commandImplementations.ContainsKey (command))
            {
                Logging.Warning (@$"{command} is not supported by this View ({GetType ().Name}). Binding: {binding}.");
            }

            // each command has its own return value
            bool? thisReturn = InvokeCommand (command, binding);

            // if we haven't got anything yet, the current command result should be used
            toReturn ??= thisReturn;

            // if ever see a true then that's what we will return
            if (thisReturn ?? false)
            {
                toReturn = true;
            }
        }

        return toReturn;
    }

    /// <summary>
    ///     Invokes the specified command given a binding. The binding is used as context for the command invocation and can be
    ///     used by command handlers to make decisions based on the source of the command.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="binding">The binding that caused the invocation, if any. This will be passed as context with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand (Command command, ICommandBinding? binding)
    {
        // Capture value from IValue if this view implements it
        IReadOnlyList<object?> values = this is IValue iValue ? [iValue.GetValue ()] : [];

        return InvokeCommand (command, new CommandContext { Command = command, Source = new WeakReference<View> (this), Binding = binding, Values = values });
    }

    /// <summary>
    ///     Invokes the specified command given a context. This is the most general form of InvokeCommand and allows the caller
    ///     to specify arbitrary context.
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="ctx">The context to pass with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand (Command command, ICommandContext? ctx)
    {
        if (!_commandImplementations.TryGetValue (command, out CommandImplementation? implementation))
        {
            _commandImplementations.TryGetValue (Command.NotBound, out implementation);
        }

        Trace.Command (this, ctx, "Handler");

        return implementation! (ctx);
    }

    /// <summary>
    ///     Invokes the specified command with a default <see cref="CommandContext"/> where <see cref="CommandContext.Source"/>
    ///     is a weak reference to `this`. The binding in the context will be set to null since this method is for invocations
    ///     that are
    ///     not caused by a binding (e.g. bubbling, default button invocation, etc.).
    /// </summary>
    /// <param name="command">The command to invoke.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    public bool? InvokeCommand (Command command)
    {
        // Capture value from IValue if this view implements it
        IReadOnlyList<object?> values = this is IValue iValue ? [iValue.GetValue ()] : [];

        return InvokeCommand (command,
                              new CommandContext
                              {
                                  Command = command,
                                  Source = new WeakReference<View> (this),

                                  // By definition, this invocation has no binding
                                  Binding = null,
                                  Values = values
                              });
    }

    #endregion Invoke

    #region Default Event Handlers

    internal bool? DefaultCommandNotBoundHandler (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");

        // Reset dispatch state — mirrors DefaultActivateHandler/DefaultAcceptHandler.
        _dispatchState = DispatchState.None;

        bool? result = RaiseCommandNotBound (ctx);

        if (result is true)
        {
            return true;
        }

        // Report as handled if the command will bubble to an ancestor — mirrors
        // DefaultActivateHandler logic so that the key is consumed when an ancestor
        // subscribes to it via CommandsToBubbleUp.
        bool willBubble = ctx is not null && CommandWillBubbleToAncestor (ctx.Command);

        if (_dispatchState.HasFlag (DispatchState.DispatchOccurred) || willBubble)
        {
            return true;
        }

        // Propagate the null/false from RaiseCommandNotBound: null = nobody observed,
        // false = subscriber exists but did not handle.
        return result;
    }

    /// <summary>
    ///     Called when a command that has not been bound is invoked.
    /// </summary>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseCommandNotBound (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Event");

        CommandEventArgs args = new () { Context = ctx };

        // For robustness' sake, even if the virtual method returns true, if the args
        // indicate the event should be cancelled, we honor that.
        if (OnCommandNotBound (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        bool hasSubscribers = CommandNotBound is not null;
        CommandNotBound?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Framework dispatch: composite views delegate commands to a target SubView.
        bool dispatched = TryDispatchToTarget (ctx);

        if (dispatched)
        {
            return true;
        }

        // Bubble to SuperView if the command is in its CommandsToBubbleUp list.
        // Bubbling is a notification (not consumption) — return false so that the
        // DefaultCommandNotBoundHandler can still report handled via CommandWillBubbleToAncestor.
        bool bubbled = TryBubbleUp (ctx, false) is true;

        if (bubbled)
        {
            return false;
        }

        // Distinguish: null = nobody observed (no subscribers, no dispatch, no bubbling);
        // false = a subscriber existed but did not set Handled=true.
        return hasSubscribers ? false : null;
    }

    /// <summary>
    ///     Called when a command that has not been bound (via <c>AddCommand</c>) is invoked on this View.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is part of the command bubbling pipeline. When a <see cref="MouseBindings"/> entry fires a command
    ///         that has no <c>AddCommand</c> handler, this method is called. If the command is in an ancestor's
    ///         <see cref="CommandsToBubbleUp"/> list, <see cref="TryBubbleUp"/> will forward it to that ancestor.
    ///     </para>
    ///     <para>
    ///         Adding a <see cref="MouseBindings"/> entry without a corresponding <c>AddCommand</c> handler is
    ///         intentional and idiomatic — it signals that the command should bubble to an ancestor.
    ///     </para>
    /// </remarks>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnCommandNotBound (CommandEventArgs args) => false;

    /// <summary>
    ///     Cancelable event raised when a command that has not been bound (via <c>AddCommand</c>) is invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event fires as part of the command bubbling mechanism. A common use case is a SubView that binds
    ///         mouse-wheel events to scroll commands (via <see cref="MouseBindings"/>) without adding a local handler.
    ///         The unhandled command fires this event, and if not cancelled here, <see cref="TryBubbleUp"/> forwards
    ///         the command to the nearest ancestor whose <see cref="CommandsToBubbleUp"/> includes it.
    ///     </para>
    ///     <para>
    ///         See also: <see cref="CommandsToBubbleUp"/>, <see cref="TryBubbleUp"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? CommandNotBound;

    #region Accept

    /// <summary>
    ///     Called when the user is accepting the state of the View and <see cref="Command.Accept"/> has been invoked.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <returns><see langword="true"/> if the command was handled; otherwise, <see langword="false"/>.</returns>
    public bool? DefaultAcceptHandler (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");

        // Reset before RaiseAccepting — early-exit paths (OnAccepting returns true, Accepting
        // event sets Handled=true) skip TryDispatchToTarget. Without this reset, the flag would
        // retain its value from a prior invocation, causing spurious completion events.
        _dispatchState = DispatchState.None;

        if (RaiseAccepting (ctx) is true)
        {
            // If dispatch consumed the command, the composite view needs completion.
            // Bridged commands also need completion: the bridge brings a command from a remote
            // view (e.g., SubMenu), TryBubbleUp propagates it to the SuperView, but the owner
            // still needs its Accepted event to fire for subscribers (e.g., parentMenuItem.Accepted).
            if (!_dispatchState.HasFlag (DispatchState.DispatchOccurred) && ctx?.Routing != CommandRouting.Bridged)
            {
                return true;
            }
            ctx = RefreshValue (ctx);

            RaiseAccepted (ctx);

            return true;
        }

        // After this View's Accepting was raised (and not handled/cancelled),
        // forward Accept to the DefaultAcceptView so its Accepting/Accepted events fire too.
        // The defaultAcceptView != source check prevents self-invocation (infinite loops).
        //
        // Skip the DefaultAcceptView redirect if Accept will also bubble to an ancestor
        // via CommandsToBubbleUp. Both paths (bubble + redirect) would reach the same ancestor,
        // causing double Accepted events. The bubble path handles it.
        View? source = null;
        ctx?.Source?.TryGetTarget (out source);
        View? defaultAcceptView = DefaultAcceptView;

        var redirected = false;
        bool acceptWillBubble = CommandWillBubbleToAncestor (Command.Accept);

        if (!acceptWillBubble && defaultAcceptView is { } && defaultAcceptView != this && defaultAcceptView != source)
        {
            DispatchDown (defaultAcceptView, ctx);
            redirected = true;
        }

        // Composite views with dispatch targets always get completion on bubble.
        if (ctx?.Routing == CommandRouting.BubblingUp && GetDispatchTarget (ctx) is { })
        {
            ctx = RefreshValue (ctx);

            RaiseAccepted (ctx);

            return false;
        }

        ctx = RefreshValue (ctx);

        // When entering via DispatchingDown, mark that Accepted will fire during dispatch.
        // The outer (Direct) call checks this flag and skips its own RaiseAccepted.
        if (ctx?.Routing == CommandRouting.DispatchingDown)
        {
            _dispatchState |= DispatchState.AcceptedViaDispatch;
            Trace.Command (this, ctx, "DispatchFlag", "Setting AcceptedViaDispatch");
            Trace.Command (this, ctx, "Routing", "Calling RaiseAccepted (DispatchingDown)");
            RaiseAccepted (ctx);
        }
        else if (!_dispatchState.HasFlag (DispatchState.AcceptedViaDispatch))
        {
            Trace.Command (this, ctx, "Routing", "Calling RaiseAccepted");
            RaiseAccepted (ctx);
        }
        else
        {
            Trace.Command (this, ctx, "SkipAccepted", "AcceptedViaDispatch already set — skipping duplicate RaiseAccepted");
        }

        // Report as handled if:
        // - Accept was redirected to DefaultAcceptView (DispatchDown performed), or
        // - Accept will bubble to ancestor (so DefaultAcceptView redirect was skipped), or
        // - Accept bubbled up from a SubView (the full chain processed the command), or
        // - This view is an IAcceptTarget (e.g. Button) that genuinely handles Accept.
        // Report as not handled when Accept originated from a local key binding (e.g., Enter key)
        // on a non-IAcceptTarget view with no redirect - this allows the key to propagate up
        // the view hierarchy to reach a SuperView that can redirect to DefaultAcceptView.
        return redirected || acceptWillBubble || ctx?.Routing == CommandRouting.BubblingUp || this is IAcceptTarget;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked.
    ///     Calls <see cref="OnAccepting"/> which can be cancelled; if not cancelled raises <see cref="Accepting"/>.
    ///     event. The default <see cref="Command.Accept"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Accepting"/> event should be raised after the state of the View has changed (after
    ///         <see cref="Activating"/> is raised).
    ///     </para>
    ///     <para>
    ///         If the Accepting event is not handled, <see cref="Command.Accept"/> will be invoked on the SuperView, enabling
    ///         default Accept behavior.
    ///     </para>
    ///     <para>
    ///         If a peer-View raises the Accepting event and the event is not cancelled, the <see cref="Command.Accept"/> will
    ///         be invoked on the
    ///         first Button in the SuperView that has <see cref="Button.IsDefault"/> set to <see langword="true"/>.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseAccepting (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        Trace.Command (this, ctx, "Handler", "Calling OnAccepting");
        args.Handled = OnAccepting (args) || args.Handled;

        if (!args.Handled && Accepting is { })
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            Trace.Command (this, ctx, "Event", "Raising Accepting event");
            Accepting?.Invoke (this, args);
        }

        // Framework dispatch: composite views delegate commands to a target SubView.
        if (!args.Handled)
        {
            args.Handled = TryDispatchToTarget (ctx);
        }

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle Accept bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleUp (ctx, args.Handled) is true;
        }

        // Warn if cancellation (not dispatch) occurred on a bridged command.
        if (args.Handled && ctx?.Routing == CommandRouting.Bridged && !_dispatchState.HasFlag (DispatchState.DispatchOccurred))
        {
            Trace.Command (this,
                           ctx,
                           "BridgedCancellation",
                           "Cancellation across a CommandBridge has no effect. "
                           + "The remote view's OnAccepted has already fired before the bridge relayed the command.");
        }

        // Do not return null as the event was raised.
        return args.Handled;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="View.RaiseAccepting"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnAccepting (CommandEventArgs args) => false;

    /// <summary>
    ///     Cancelable event raised when the user is accepting the state of the View and the <see cref="Command.Accept"/> has
    ///     been invoked.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="View.RaiseAccepting"/> for more information.
    ///     </para>
    ///     <para>
    ///         <strong>When to use:</strong> Subscribe to <see cref="Accepting"/> only when you need to inspect or cancel the
    ///         in-flight Accept operation (e.g., set <c>e.Handled = true</c> to prevent the accept). For simple side-effects
    ///         that don't need to cancel, subscribe to <see cref="Accepted"/> instead — it is lighter-weight and communicates
    ///         intent more clearly.
    ///     </para>
    ///     <para>
    ///         <strong>Rule of thumb:</strong> If your handler doesn't read or set anything on <see cref="CommandEventArgs"/>
    ///         (no <c>e.Handled</c>, no inspection of context), use <see cref="Accepted"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepting;

    /// <summary>
    ///     Raises the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating the View has been accepted.
    ///     This is called after <see cref="Accepting"/> has been raised and not cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Accepting"/>, this event cannot be cancelled. It is raised after the View has been accepted.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <seealso cref="RaiseAccepting"/>
    protected internal void RaiseAccepted (ICommandContext? ctx)
    {
        OnAccepted (ctx);
        Accepted?.Invoke (this, new CommandEventArgs { Context = ctx });
    }

    /// <summary>
    ///     Called when the View has been accepted. This is called after <see cref="Accepting"/> has been raised and not
    ///     cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="OnAccepting"/>, this method is called after the View has been accepted and cannot cancel the
    ///         operation.
    ///     </para>
    /// </remarks>
    /// <param name="ctx"></param>
    protected virtual void OnAccepted (ICommandContext? ctx) { }

    /// <summary>
    ///     Event raised when the View has been accepted. This is raised after <see cref="Accepting"/> has been raised and not
    ///     cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Accepting"/>, this event cannot be cancelled. It is raised after the View has been accepted.
    ///     </para>
    ///     <para>
    ///         See <see cref="RaiseAccepted"/> for more information.
    ///     </para>
    ///     <para>
    ///         <strong>When to use:</strong> Subscribe to <see cref="Accepted"/> for fire-and-forget side-effects — things that
    ///         happen <em>after</em> the accept has completed and cannot be cancelled. This is the right choice for the vast
    ///         majority of button-click–style handlers.
    ///     </para>
    ///     <para>
    ///         <strong>Example:</strong>
    ///         <code>
    ///             button.Accepted += (_, _) =&gt; DoTheThing ();   // correct — side-effect only
    ///             button.Accepting += (_, e) =&gt; { if (!CanProceed ()) e.Handled = true; }; // correct — cancels
    ///             button.Accepting += (_, _) =&gt; DoTheThing (); // wrong — use Accepted instead
    ///         </code>
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;

    #endregion Accept

    #region Activate

    /// <summary>
    ///     Called when the user is activating the View and <see cref="Command.Activate"/> has been invoked.
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <returns><see langword="true"/> if the command was handled; otherwise, <see langword="false"/>.</returns>
    public bool? DefaultActivateHandler (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");

        // Reset before RaiseActivating — early-exit paths (OnActivating returns true, Activating
        // event sets Handled=true) skip TryDispatchToTarget. Without this reset, the flag would
        // retain its value from a prior invocation, causing spurious completion events.
        _dispatchState = DispatchState.None;

        if (RaiseActivating (ctx) is true)
        {
            if (!_dispatchState.HasFlag (DispatchState.DispatchOccurred))
            {
                return true;
            }
            ctx = RefreshValue (ctx);

            RaiseActivated (ctx);

            // After RaiseActivated, the composite may have updated its own value
            // (e.g., OptionSelector.ApplyActivation updates Value in OnActivated).
            // Append the composite's post-mutation value so that ctx.Value reflects
            // the composite's semantic value, not the dispatch target's raw value.
            //
            // Note on struct value semantics: `ccComposite` is captured from `ctx`
            // BEFORE RaiseActivated was called. Inside RaiseActivated (line ~782),
            // WithValue() is also called — but on a LOCAL copy of the struct, so the
            // caller's ctx/ccComposite is unaffected. BubbleActivatedUp therefore
            // receives the composite value appended once, not twice.
            if (this is IValue compositeValue && ctx is CommandContext ccComposite)
            {
                object? postMutationValue = compositeValue.GetValue ();
                ctx = ccComposite.WithValue (postMutationValue);
                Trace.Command (this, ctx, "CompositeValue", $"Appended composite value: {postMutationValue}");
            }

            // ConsumeDispatch consumed the command internally, but ancestors still need
            // notification. Walk up the SuperView chain and fire RaiseActivated on each
            // ancestor that subscribes to this command via CommandsToBubbleUp.
            // This replaces the old one-hop Shortcut.CommandView_Activated propagation
            // with full-chain notification (e.g., MenuItem → Menu → SuperView).
            BubbleActivatedUp (ctx);

            return true;
        }

        // When a SubView's activation bubbles up, the default behavior is notification:
        // Activating fires (above), but side effects (SetFocus) are skipped.
        // The originating view completes its own activation. Returning false tells TryBubbleUp
        // "not consumed" so the originator continues.
        //
        // Composite views with ConsumeDispatch=true already completed above (RaiseActivating returned true).
        // ALL ancestors (both composite and plain) receive Activated later via
        // BubbleActivatedUp, after the originator completes its state change.
        // This ensures bridges on plain views (e.g., Menu → PopoverMenu) see the post-change value.
        if (ctx?.Routing == CommandRouting.BubblingUp)
        {
            return false;
        }

        if (CanFocus)
        {
            SetFocus ();
        }

        // Refresh the value from the dispatch target (e.g. CheckBox after toggle) so that
        // RaiseActivated and BubbleActivatedUp carry the post-change value.
        ctx = RefreshValue (ctx);

        // When entering via DispatchingDown, mark that Activated will fire during dispatch.
        // The outer (Direct) call checks this flag and skips its own RaiseActivated to prevent
        // double-fire when the originator IS the dispatch target.
        if (ctx?.Routing == CommandRouting.DispatchingDown)
        {
            _dispatchState |= DispatchState.ActivatedViaDispatch;
            Trace.Command (this, ctx, "DispatchFlag", "Setting ActivatedViaDispatch");
            RaiseActivated (ctx);
        }
        else if (!_dispatchState.HasFlag (DispatchState.ActivatedViaDispatch))
        {
            RaiseActivated (ctx);
        }
        else
        {
            Trace.Command (this, ctx, "SkipActivated", "ActivatedViaDispatch already set — skipping duplicate RaiseActivated");
        }

        // After RaiseActivated, re-read this view's own value (if it implements IValue)
        // so that BubbleActivatedUp carries the post-change value even when the originator
        // IS the IValue (e.g., ToggleView mutates Value in OnActivated).
        // Only refresh when this view is the command source — intermediate views in a bridge
        // chain (e.g., MenuBarItem receiving a bridged command) should not overwrite the
        // original source's value.
        if (this is IValue selfValue && ctx is CommandContext cc && ctx.Source?.TryGetTarget (out View? src) == true && ReferenceEquals (src, this))
        {
            ctx = cc.WithValue (selfValue.GetValue ());
        }

        // Notify ALL ancestors (composite and plain) that activation completed.
        // This replaces the old BubblingUp-phase RaiseActivated on plain views, ensuring
        // bridges on plain views (e.g., Menu → PopoverMenu) see the post-change value.
        // Skip for DispatchingDown — the dispatching view handles its own Activated after dispatch returns.
        if (ctx?.Routing != CommandRouting.DispatchingDown)
        {
            BubbleActivatedUp (ctx);
        }

        // Report as handled if:
        // - Activate was dispatched to a target (composite view consumed it), or
        // - Activate will bubble to ancestor (so it will be processed up the chain), or
        // - Activate bubbled up from a SubView (the full chain processed the command).
        // Report as not handled when Activate originated from a local key binding (e.g., Space key)
        // on a plain view with no dispatch target and no bubble config — this allows the key to
        // propagate to HotKey dispatch. (Mirrors DefaultAcceptHandler's logic; fixes #4759.)
        bool activateWillBubble = CommandWillBubbleToAncestor (Command.Activate);

        return _dispatchState.HasFlag (DispatchState.DispatchOccurred) || activateWillBubble;
    }

    /// <summary>
    ///     Checks whether the given <paramref name="command"/> will bubble to an ancestor via
    ///     <see cref="CommandsToBubbleUp"/>. This mirrors the checks in <see cref="TryBubbleUp"/>.
    /// </summary>
    private bool CommandWillBubbleToAncestor (Command command)
    {
        if (SuperView?.CommandsToBubbleUp.Contains (command) == true)
        {
            return true;
        }

        if (SuperView is AdornmentView adornment && adornment.Adornment?.Parent?.CommandsToBubbleUp.Contains (command) == true)
        {
            return true;
        }

        return this is AdornmentView selfAdornment && selfAdornment.Adornment?.Parent?.CommandsToBubbleUp.Contains (command) == true;
    }

    /// <summary>
    ///     Walks up the SuperView chain from this view, firing <see cref="RaiseActivated"/> on
    ///     ancestors whose <see cref="CommandsToBubbleUp"/> contains the command. Unlike
    ///     <see cref="TryBubbleUp"/>, this does NOT re-invoke the full command pipeline
    ///     (no <see cref="RaiseActivating"/>, no <see cref="DefaultActivateHandler"/>).
    /// </summary>
    /// <param name="ctx">The command context.</param>
    /// <param name="compositeOnly">
    ///     When <see langword="true"/>, only fires on composite views (those with a dispatch target).
    ///     Plain views are assumed to have already received Activated during the BubblingUp phase.
    ///     When <see langword="false"/>, fires on all ancestors (used after ConsumeDispatch where
    ///     the BubblingUp phase was blocked).
    /// </param>
    private void BubbleActivatedUp (ICommandContext? ctx, bool compositeOnly = false)
    {
        if (ctx is null)
        {
            return;
        }

        for (View? ancestor = GetBubbleAncestor (this); ancestor?.CommandsToBubbleUp.Contains (ctx.Command) == true; ancestor = GetBubbleAncestor (ancestor))
        {
            if (compositeOnly && ancestor.GetDispatchTarget (ctx) is null)
            {
                continue;
            }
            CommandContext upCtx = new (ctx.Command, ctx.Source, ctx.Binding) { Routing = CommandRouting.BubblingUp, Values = ctx.Values };

            // Re-read the value from the ancestor's dispatch target, but only when
            // the dispatch target is the same view as the command source (i.e., the
            // originator IS the dispatch target). This ensures post-change values
            // (e.g., after ToggleView mutated in OnActivated) are carried correctly
            // without overwriting values from unrelated dispatch targets (e.g., MenuBar
            // dispatching to MenuBarItem would overwrite the MenuItem value).
            View? dispatchTarget = ancestor.GetDispatchTarget (ctx);

            if (dispatchTarget is IValue refreshedValue && ctx.Source?.TryGetTarget (out View? source) == true && ReferenceEquals (source, dispatchTarget))
            {
                upCtx = upCtx.WithValue (refreshedValue.GetValue ());
            }

            ancestor.RaiseActivated (upCtx);
        }

        return;

        // Resolves the next ancestor, handling AdornmentView → Parent traversal (mirrors TryBubbleUp).
        static View? GetBubbleAncestor (View current)
        {
            View? next = current.SuperView;

            if (next is AdornmentView adornment)
            {
                return adornment.Adornment?.Parent;
            }

            if (current is AdornmentView selfAdornment)
            {
                return selfAdornment.Adornment?.Parent;
            }

            return next;
        }
    }

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the View to change state
    ///     or preparing it for interaction.
    ///     Calls <see cref="OnActivating"/> which can be cancelled; if not cancelled raises <see cref="Activating"/>
    ///     event. The default <see cref="Command.Activate"/> handler calls this method.
    /// </summary>
    /// <remarks>
    ///     The <see cref="Activating"/> event should be raised after the state of the View has been changed and before see
    ///     <see cref="Accepting"/>.
    /// </remarks>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseActivating (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");

        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        if (OnActivating (args) || args.Handled)
        {
            // Warn if cancellation occurs on a bridged command — the remote side has already committed.
            if (ctx?.Routing == CommandRouting.Bridged)
            {
                Trace.Command (this,
                               ctx,
                               "BridgedCancellation",
                               "Cancellation across a CommandBridge has no effect. "
                               + "The remote view's OnActivated has already fired before the bridge relayed the command.");
            }

            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Trace.Command (this, ctx, "Event", "Invoking Activating event");
        Activating?.Invoke (this, args);

        // Framework dispatch: composite views delegate commands to a target SubView.
        if (!args.Handled)
        {
            args.Handled = TryDispatchToTarget (ctx);
        }

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle Activate bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleUp (ctx, args.Handled) is true;
        }

        // Warn if cancellation (not dispatch) occurred on a bridged command.
        if (args.Handled && ctx?.Routing == CommandRouting.Bridged && !_dispatchState.HasFlag (DispatchState.DispatchOccurred))
        {
            Trace.Command (this,
                           ctx,
                           "BridgedCancellation",
                           "Cancellation across a CommandBridge has no effect. "
                           + "The remote view's OnActivated has already fired before the bridge relayed the command.");
        }

        return args.Handled;
    }

    /// <summary>
    ///     Called when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the View to change state
    ///     or preparing it for interaction.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> and return <see langword="true"/> to indicate the event was
    ///     handled and processing should stop.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnActivating (CommandEventArgs args) => false;

    /// <summary>
    ///     Cancelable event raised when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the
    ///     View
    ///     to change state or preparing it for interaction.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <strong>When to use:</strong> Subscribe to <see cref="Activating"/> only when you need to inspect or cancel the
    ///         in-flight Activate operation (e.g., set <c>e.Handled = true</c> to prevent the state change). For simple
    ///         side-effects that don't need to cancel, subscribe to <see cref="Activated"/> instead — it is lighter-weight and
    ///         communicates intent more clearly.
    ///     </para>
    ///     <para>
    ///         <strong>Rule of thumb:</strong> If your handler doesn't read or set anything on <see cref="CommandEventArgs"/>
    ///         (no <c>e.Handled</c>, no inspection of context), use <see cref="Activated"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Activating;

    /// <summary>
    ///     Raises the <see cref="OnActivated"/>/<see cref="Activated"/> event indicating the View has been activated.
    ///     This is called after <see cref="Activated"/> has been raised and not cancelled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Activating"/>, this event cannot be cancelled. It is raised after the View has activated.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <seealso cref="RaiseActivating"/>
    protected internal void RaiseActivated (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Event");

        OnActivated (ctx);

        // After OnActivated, a ConsumeDispatch composite that just consumed a dispatch may have
        // updated its own value (e.g., OptionSelector.ApplyActivation updates Value in OnActivated).
        // Append its post-mutation value so that direct Activated subscribers see the composite's
        // semantic value, not the dispatch target's raw value.
        // Guard: only when DispatchOccurred is set — this means DefaultActivateHandler's
        // ConsumeDispatch branch initiated RaiseActivated (not BubbleActivatedUp or other paths).
        if (_dispatchState.HasFlag (DispatchState.DispatchOccurred) && this is IValue selfValue && ctx is CommandContext cc)
        {
            ctx = cc.WithValue (selfValue.GetValue ());
        }

        Activated?.Invoke (this, new EventArgs<ICommandContext?> (ctx));
    }

    /// <summary>
    ///     Re-reads the current <see cref="IValue.GetValue"/> from the dispatch target and returns
    ///     a context with the refreshed value. Only checks the dispatch target (e.g. a
    ///     <see cref="Shortcut"/>'s <c>CommandView</c>) — the actual <see cref="IValue"/> view whose
    ///     state changed during command processing. Does NOT fall back to
    ///     <see cref="ICommandContext.Source"/> because the source may be a different <see cref="IValue"/>
    ///     (e.g. <see cref="MenuItem"/> whose <c>GetValue</c> returns the Title, not the
    ///     <see cref="CheckBox"/> state).
    /// </summary>
    private ICommandContext? RefreshValue (ICommandContext? ctx)
    {
        if (ctx is not CommandContext cc)
        {
            return ctx;
        }

        Trace.Command (this, ctx, "Value");

        if (GetDispatchTarget (ctx) is IValue targetValue)
        {
            return cc.WithValue (targetValue.GetValue ());
        }

        return ctx;
    }

    /// <summary>
    ///     Called when the View has been activated. This is called after <see cref="Accepting"/> has been raised and not
    ///     cancelled.
    /// </summary>
    /// <param name="ctx">The event arguments.</param>
    protected virtual void OnActivated (ICommandContext? ctx) { }

    /// <summary>
    ///     Event raised when the user has performed an action (e.g. <see cref="Command.Activate"/>) causing the
    ///     View to change state or preparing it for interaction.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Unlike <see cref="Activating"/>, this event cannot be cancelled. It is raised after the View has activated.
    ///     </para>
    ///     <para>
    ///         <strong>When to use:</strong> Subscribe to <see cref="Activated"/> for fire-and-forget side-effects — things
    ///         that happen <em>after</em> the activation has completed and cannot be cancelled. This is the right choice for
    ///         the vast majority of state-change–reaction handlers.
    ///     </para>
    /// </remarks>
    public event EventHandler<EventArgs<ICommandContext?>>? Activated;

    #endregion Activate

    #region HotKey

    /// <summary>
    ///     Called when the user has pressed the View's <see cref="HotKey"/> and <see cref="Command.HotKey"/> has been invoked.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         View-local HotKeys require the View to be visible in the SuperView chain
    ///         (<c>CanBeVisible</c>). Application-scoped bindings — those whose
    ///         <see cref="KeyBinding.Target"/> is this View (e.g. <see cref="Shortcut.Key"/> with
    ///         <see cref="Shortcut.BindKeyToApplication"/>) — are honored even when this View is not
    ///         visible, so a <see cref="MenuItem"/> shortcut can fire while its <see cref="PopoverMenu"/> is closed.
    ///     </para>
    ///     <para>
    ///         To avoid focusing a hidden View, <see cref="SetFocus"/> runs only when the View can be visible.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <returns><see langword="true"/> if the command was handled; otherwise, <see langword="false"/>.</returns>
    public bool? DefaultHotKeyHandler (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");

        // Application-scoped bindings (KeyBinding.Target == this) must work even when this View
        // is not visible — e.g. MenuItem.Key with BindKeyToApplication inside a closed PopoverMenu.
        bool isApplicationScoped = ctx?.Binding is KeyBinding { Target: { } target } && ReferenceEquals (target, this);

        if ((!CanBeVisible (this) && !isApplicationScoped) || RaiseHandlingHotKey (ctx) is true)
        {
            // The hotkey was cancelled by OnHandlingHotKey or HandlingHotKey event.
            // Return false so the key is not consumed and can be processed as normal input
            // (e.g. text input in a TextField whose HotKey matches the character being typed).
            return false;
        }

        if (CanFocus && CanBeVisible (this))
        {
            // Set focus if not handled yet. Setting focus does NOT mean the event is handled, so we return.
            // Skip when not visible (application-scoped shortcut on a hidden MenuItem/Shortcut).
            SetFocus ();
        }

        RaiseHotKeyCommand (ctx);

        // Pass the original binding so downstream handlers (e.g. Shortcut.OnActivating)
        // can distinguish a user-initiated HotKey activation from a programmatic one.
        InvokeCommand (Command.Activate, ctx?.Binding);

        // QUESTION: Why do we return true here, indicating the hotkey was handled,
        // QUESTION: when we still want the key event to propagate for text input scenarios?
        // QUESTION: Should we return false to allow further processing?
        return true;
    }

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>s. Calls
    ///     <see cref="OnHandlingHotKey"/> which can be cancelled. If not cancelled raises <see cref="HandlingHotKey"/> event.
    ///     The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <param name="ctx">The context to pass with the command.</param>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    protected bool? RaiseHandlingHotKey (ICommandContext? ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        if (OnHandlingHotKey (args) || args.Handled)
        {
            return true;
        }

        // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
        Trace.Command (this, ctx, "Event", "Invoking HandlingHotKey event");
        HandlingHotKey?.Invoke (this, args);

        if (!args.Handled)
        {
            // Use TryBubbleToSuperView helper to handle bubbling (opt-in via CommandsToBubbleUp)
            args.Handled = TryBubbleUp (ctx, args.Handled) is true;
        }

        return args.Handled;
    }

    /// <summary>
    ///     Called when the View is handling the user pressing the View's <see cref="HotKey"/>.
    ///     Set CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should
    ///     stop.
    /// </summary>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnHandlingHotKey (CommandEventArgs args) => false;

    /// <summary>
    ///     Raises the <see cref="OnHotKeyCommand"/>/<see cref="HotKeyCommand"/> event indicating the View is handling the user
    ///     pressing the View's <see cref="HotKey"/>.
    ///     The default <see cref="Command.HotKey"/> handler calls this method.
    /// </summary>
    /// <param name="ctx"></param>
    protected void RaiseHotKeyCommand (ICommandContext? ctx)
    {
        OnHotKeyCommand (ctx);
        HotKeyCommand?.Invoke (this, new EventArgs<ICommandContext?> (ctx));
    }

    /// <summary>
    ///     Cancelable event raised when the View is handling the user pressing the View's <see cref="HotKey"/>. Set
    ///     CommandEventArgs.Handled to <see langword="true"/> to indicate the event was handled and processing should stop.
    /// </summary>
    public event EventHandler<CommandEventArgs>? HandlingHotKey;

    /// <summary>
    ///     Called when the View's <see cref="HotKey"/> is pressed, if the event was not handled by
    ///     <see cref="OnHandlingHotKey"/> or the <see cref="HandlingHotKey"/> event.
    /// </summary>
    /// <param name="ctx"></param>
    protected virtual void OnHotKeyCommand (ICommandContext? ctx) { }

    /// <summary>
    ///     Event raised when the View's <see cref="HotKey"/> is pressed, if the event was not handled by
    ///     <see cref="OnHandlingHotKey"/> or the <see cref="HandlingHotKey"/> event.
    /// </summary>
    public event EventHandler<EventArgs<ICommandContext?>>? HotKeyCommand;

    #endregion HotKey

    #endregion Default Event Handlers

    #region Dispatch (Composite Pattern)

    /// <summary>
    ///     Gets the SubView to dispatch commands to. Return <see langword="null"/> to skip dispatch.
    ///     The framework calls this during <see cref="RaiseActivating"/>/<see cref="RaiseAccepting"/>
    ///     after the <c>OnActivating</c> virtual and <c>OnAccepting</c> event have had a chance to cancel.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Override this in composite views that delegate commands to a primary SubView.
    ///         For example, <c>Shortcut</c> returns <c>CommandView</c> and selectors return <c>Focused</c>.
    ///     </para>
    ///     <para>
    ///         The framework guards against dispatch when:
    ///         <list type="bullet">
    ///             <item>Routing is <see cref="CommandRouting.DispatchingDown"/> (prevents re-entry)</item>
    ///             <item>No binding exists on the context (programmatic invocation — skip dispatch)</item>
    ///             <item>The binding source is within the target (prevents loops)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context.</param>
    /// <returns>The SubView to dispatch to, or <see langword="null"/> to skip dispatch.</returns>
    protected virtual View? GetDispatchTarget (ICommandContext? ctx) => null;

    /// <summary>
    ///     If <see langword="true"/>, dispatching to the target consumes the command, preventing the
    ///     original SubView from completing its own activation/acceptance. If <see langword="false"/>
    ///     (default), the dispatch is a relay and the original SubView completes normally.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, the composite view owns the state mutation. The framework
    ///         marks the command as handled after dispatch, stopping the originator. The composite's
    ///         <c>OnActivated</c>/<c>OnAccepted</c> fires to perform the mutation.
    ///     </para>
    ///     <para>
    ///         When <see langword="false"/> (relay), the command is dispatched to the target but not
    ///         consumed. The originator continues its own activation/acceptance.
    ///     </para>
    /// </remarks>
    protected virtual bool ConsumeDispatch => false;

    /// <summary>
    ///     Tracks dispatch state during the current command invocation. Used to prevent double-fire
    ///     of <c>RaiseActivated</c>/<c>RaiseAccepted</c> when the originator is also the dispatch target.
    /// </summary>
    [Flags]
    private enum DispatchState
    {
        None = 0,

        /// <summary>A dispatch (relay or consume) occurred during this invocation.</summary>
        DispatchOccurred = 1,

        /// <summary><see cref="RaiseActivated"/> already fired during a <see cref="CommandRouting.DispatchingDown"/> call.</summary>
        ActivatedViaDispatch = 2,

        /// <summary><see cref="RaiseAccepted"/> already fired during a <see cref="CommandRouting.DispatchingDown"/> call.</summary>
        AcceptedViaDispatch = 4
    }

    private DispatchState _dispatchState;

    /// <summary>
    ///     Attempts to dispatch the command to the <see cref="GetDispatchTarget"/> view.
    ///     For <see cref="ConsumeDispatch"/>=false (relay), performs a <see cref="DispatchDown"/>.
    ///     For <see cref="ConsumeDispatch"/>=true (consume), marks the command as handled without dispatching.
    /// </summary>
    /// <returns><see langword="true"/> if the command was consumed (ConsumeDispatch=true and dispatch conditions met).</returns>
    private bool TryDispatchToTarget (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry");

        _dispatchState = DispatchState.None;

        View? target = GetDispatchTarget (ctx);

        if (target is null)
        {
            return false;
        }

        // Guard: don't dispatch if already dispatching down (prevents re-entry)
        if (ctx?.Routing == CommandRouting.DispatchingDown)
        {
            return false;
        }

        // Guard: don't dispatch when a command arrives via bridge. The bridge brings
        // commands UP from a non-containment boundary (e.g., SubMenu → parentMenuItem);
        // dispatching down into the owner's CommandView would be incorrect.
        if (ctx?.Routing == CommandRouting.Bridged)
        {
            return false;
        }

        // Guard: for relay dispatch (ConsumeDispatch=false), don't dispatch for programmatic
        // invocations (no binding). This prevents accidental loops in composite views like Shortcut.
        // For consume dispatch (ConsumeDispatch=true), programmatic invocations DO dispatch because
        // the composite view forwards commands to the focused SubView.
        if (!ConsumeDispatch && ctx?.Binding is null)
        {
            return false;
        }

        if (ConsumeDispatch)
        {
            // Consume pattern (OptionSelector, FlagSelector).
            // When a SubView's command bubbles up (BubblingUp), consume without dispatching.
            // The composite handles state mutation in OnActivated/OnAccepted.
            // For programmatic/direct invocations, forward to the target via DispatchDown
            // so the target gets activated.
            if (ctx?.Routing != CommandRouting.BubblingUp)
            {
                DispatchDown (target, ctx);
            }

            _dispatchState |= DispatchState.DispatchOccurred;

            return true;
        }

        // Relay pattern (Shortcut): dispatch to target if source is not within target.
        if (IsSourceWithinView (target, ctx))
        {
            return false;
        }
        DispatchDown (target, ctx);
        _dispatchState |= DispatchState.DispatchOccurred;

        return false;
    }

    /// <summary>
    ///     Checks whether the binding source of the given context is within the specified view
    ///     (i.e., is the view itself or a descendant).
    /// </summary>
    private static bool IsSourceWithinView (View target, ICommandContext? ctx)
    {
        if (ctx?.Binding?.Source is not { } weakSource || !weakSource.TryGetTarget (out View? source))
        {
            return false;
        }

        View? current = source;

        while (current is { })
        {
            if (current == target)
            {
                return true;
            }

            current = current.SuperView;
        }

        return false;
    }

    #endregion Dispatch (Composite Pattern)

    #region Command Bubbling

    /// <summary>
    ///     Gets or sets the default accept view for this View. The default accept view will have <see cref="Command.Accept"/>
    ///     invoked on it
    ///     anytime a peer View raises <see cref="Command.Accept"/> and the event is not handled, or if
    ///     <see cref="Command.Accept"/> is invoked directly on this View.
    /// </summary>
    /// <remarks>
    ///     This is used to implement the common pattern of
    ///     having an "OK" button that accepts the dialog when the user presses Enter or clicks the button, without having to
    ///     set up explicit bindings for each control in the dialog that should trigger the "OK" button's Accept behavior.
    /// </remarks>
    public View? DefaultAcceptView { get => field ?? GetSubViews (includePadding: true).FirstOrDefault (v => v is IAcceptTarget { IsDefault: true }); set; }

    /// <summary>
    ///     Gets or sets the list of commands that should bubble up to this View from unhandled SubViews
    ///     or from SubViews within this View's adornments (Padding, Border, Margin).
    ///     When a SubView raises a command that is not handled, and the command is in the SuperView's
    ///     <see cref="CommandsToBubbleUp"/> list, the command will be invoked on the SuperView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For SubViews inside an <see cref="AdornmentView"/> (e.g., a view in Padding or Border),
    ///         the bubble target is <see cref="IAdornment.Parent"/> (the owning View) rather than
    ///         <see cref="SuperView"/>. This means a view added to <c>editor.Padding</c> will bubble
    ///         commands directly to <c>editor</c>, not to the <c>PaddingView</c>.
    ///     </para>
    ///     <para>
    ///         <b>Mouse-wheel forwarding pattern:</b> A SubView can add a <see cref="MouseBindings"/> entry
    ///         (e.g., <c>MouseBindings.Add(MouseFlags.WheeledUp, Command.ScrollUp)</c>) without calling
    ///         <c>AddCommand</c> for the command. The unhandled command will fire
    ///         <see cref="CommandNotBound"/> and then bubble via <see cref="TryBubbleUp"/> to the nearest
    ///         ancestor whose <see cref="CommandsToBubbleUp"/> contains it.
    ///     </para>
    ///     <para>
    ///         Example — enable scroll command bubbling:
    ///         <code>
    ///             editor.CommandsToBubbleUp = [Command.ScrollUp, Command.ScrollDown];
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Example — enable <see cref="Command.Activate"/> bubbling for hierarchical views:
    ///         <code>
    ///             menuBar.CommandsToBubbleUp = [Command.Activate];
    ///         </code>
    ///     </para>
    /// </remarks>
    public IReadOnlyList<Command> CommandsToBubbleUp { get; set; } = [];

    /// <summary>
    ///     Dispatches a command downward to a SubView with bubbling suppressed. Creates a new
    ///     <see cref="CommandContext"/> with <see cref="ICommandContext.Routing"/> set to
    ///     <see cref="CommandRouting.DispatchingDown"/>,
    ///     which causes <see cref="TryBubbleUp"/> to skip bubbling on the target, preventing re-entry.
    /// </summary>
    /// <param name="target">The SubView to dispatch the command to.</param>
    /// <param name="ctx">The original command context, used to determine the command and source.</param>
    /// <returns>
    ///     The result of invoking the command on the target.
    /// </returns>
    protected bool? DispatchDown (View target, ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Routing", $"DispatchDown to {target.ToIdentifyingString ()}");

        CommandContext downCtx = new (ctx?.Command ?? Command.NotBound, ctx?.Source, ctx?.Binding) { Routing = CommandRouting.DispatchingDown };

        return target.InvokeCommand (downCtx.Command, downCtx);
    }

    /// <summary>
    ///     Bubbles a command to the SuperView (or to <see cref="IAdornment.Parent"/> for adornment SubViews)
    ///     if the target's <see cref="CommandsToBubbleUp"/> list contains the command.
    ///     Handles the special case of invoking <see cref="Command.Accept"/> on a peer IsDefault button.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Bubbling is a <b>notification</b>, not a consumption. The SuperView's handler is invoked, but its
    ///         return value is ignored — this method always returns <see langword="false"/> after a successful bubble.
    ///         This ensures the originating view can complete its own processing (e.g., a CheckBox can toggle,
    ///         a Shortcut can raise Activated) without being blocked by the SuperView returning <see langword="true"/>.
    ///     </para>
    ///     <para>
    ///         To cancel a SubView's command, subscribe to the SubView's <c>Activating</c>/<c>Accepting</c> event
    ///         and set <c>Handled = true</c> — that guard runs <b>before</b> <see cref="TryBubbleUp"/> is called.
    ///     </para>
    /// </remarks>
    /// <param name="ctx">The command context to pass along.</param>
    /// <param name="handled">Whether the command was already handled by this View.</param>
    /// <returns>
    ///     <see langword="true"/> if the command was already handled locally.
    ///     <see langword="false"/> if the command was not handled (including after a successful bubble).
    /// </returns>
    protected bool? TryBubbleUp (ICommandContext? ctx, bool handled)
    {
        Trace.Command (this, ctx, "Entry", handled ? "already handled" : null);

        if (handled)
        {
            return true;
        }

        if (ctx is null || ctx.Routing == CommandRouting.DispatchingDown)
        {
            return false;
        }

        CommandContext? upCtx;

        if (ctx.Command == Command.Accept)
        {
            // Check this view's DefaultAcceptView first (for when Accept is invoked directly on this view),
            // then check SuperView's DefaultAcceptView (for when Accept bubbles up from a subview)
            View? isDefaultView = DefaultAcceptView ?? SuperView?.DefaultAcceptView;

            // Get the source view to determine how to handle the redirect
            View? source = null;
            ctx.Source?.TryGetTarget (out source);

            if (isDefaultView is { } && isDefaultView != this && isDefaultView != source)
            {
                if (source is IAcceptTarget acceptTarget)
                {
                    // Non-default IAcceptTarget sources bubble up to SuperView
                    // so it can determine which accept target was activated
                    if (acceptTarget.IsDefault)
                    {
                        return false;
                    }

                    upCtx = new CommandContext (Command.Accept, ctx.Source, ctx.Binding) { Routing = CommandRouting.BubblingUp, Values = ctx.Values };

                    // DefaultAcceptView redirect is a special case — it IS a consumption (not just a notification)
                    return SuperView?.InvokeCommand (Command.Accept, upCtx) is true;

                    // Default IAcceptTarget source - let it flow normally without redirect
                }
            }
        }

        // Check if SuperView wants this command bubbled up to it
        if (SuperView?.CommandsToBubbleUp.Contains (ctx.Command) == true)
        {
            // Refresh value from the dispatch target (e.g. CheckBox after toggle) so that
            // the bubbled context carries the post-change value.
            ICommandContext? refreshed = RefreshValue (ctx);

            Trace.Command (this, refreshed, "Routing", $"BubblingUp to {SuperView.ToIdentifyingString ()}");

            upCtx = new CommandContext (refreshed!.Command, refreshed.Source, refreshed.Binding)
            {
                Routing = CommandRouting.BubblingUp, Values = refreshed.Values
            };

            return SuperView.InvokeCommand (refreshed.Command, upCtx);
        }

        // SubView of an AdornmentView: bubble to the adornment's Parent (the owning View)
        if (SuperView is AdornmentView adornment && adornment.Adornment?.Parent?.CommandsToBubbleUp.Contains (ctx.Command) == true)
        {
            ICommandContext? refreshed = RefreshValue (ctx);

            Trace.Command (this, refreshed, "Routing", $"BubblingUp to Adornment.Parent {adornment.Adornment.Parent.ToIdentifyingString ()}");

            upCtx = new CommandContext (refreshed!.Command, refreshed.Source, refreshed.Binding)
            {
                Routing = CommandRouting.BubblingUp, Values = refreshed.Values
            };

            return adornment.Adornment.Parent.InvokeCommand (refreshed.Command, upCtx);
        }

        // THIS view is an AdornmentView: bubble to its own Parent
        if (this is not AdornmentView selfAdornment || selfAdornment.Adornment?.Parent?.CommandsToBubbleUp.Contains (ctx.Command) != true)
        {
            return handled;
        }

        ICommandContext? selfRefreshed = RefreshValue (ctx);

        Trace.Command (this, selfRefreshed, "Routing", $"BubblingUp from Adornment to {selfAdornment.Adornment.Parent.ToIdentifyingString ()}");

        upCtx = new CommandContext (selfRefreshed!.Command, selfRefreshed.Source, selfRefreshed.Binding)
        {
            Routing = CommandRouting.BubblingUp, Values = selfRefreshed.Values
        };

        return selfAdornment.Adornment.Parent.InvokeCommand (selfRefreshed.Command, upCtx);
    }

    #endregion Command Bubbling
}
