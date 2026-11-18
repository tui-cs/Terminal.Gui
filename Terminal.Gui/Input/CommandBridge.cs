using Terminal.Gui.Tracing;

namespace Terminal.Gui.Input;

/// <summary>
///     Bridges command routing across non-containment boundaries (e.g., between a <see cref="MenuBarItem"/>
///     and its <see cref="PopoverMenu"/>, which is registered with Application.Popover rather than the
///     SuperView hierarchy).
/// </summary>
/// <remarks>
///     <para>
///         The bridge subscribes to the remote view's <c>Accepted</c>, <c>Activated</c>, and/or
///         <c>CommandNotBound</c> events. When fired, it creates a new <see cref="CommandContext"/> with
///         <see cref="CommandRouting.Bridged"/> routing and invokes the command on the owner via
///         <see cref="View.InvokeCommand(Command, ICommandContext?)"/>. This re-enters the full
///         command pipeline (RaiseActivating/RaiseAccepting → TryDispatchToTarget → TryBubbleUp →
///         RaiseActivated/RaiseAccepted), enabling bridged commands to propagate through the
///         owner's SuperView hierarchy.
///     </para>
///     <para>
///         Both references are weak — the bridge does not prevent GC of either view.
///         The bridge becomes inert if either <see cref="WeakReference{T}"/> target is collected.
///     </para>
///     <para>
///         The bridge is one-way: remote fires event → owner receives command.
///         If bidirectional routing is needed, create two bridges.
///     </para>
/// </remarks>
public class CommandBridge : IDisposable
{
    private readonly HashSet<Command> _commands;
    private readonly WeakReference<View> _owner;
    private readonly WeakReference<View> _remote;
    private readonly bool _subscribedToCommandNotBound;
    private bool _disposed;

    private CommandBridge (View owner, View remote, Command [] commands)
    {
        _owner = new WeakReference<View> (owner);
        _remote = new WeakReference<View> (remote);
        _commands = new HashSet<Command> (commands);

        // Subscribe to the remote view's completion events for bridged commands.
        if (_commands.Contains (Command.Accept))
        {
            remote.Accepted += OnRemoteAccepted;
        }

        if (_commands.Contains (Command.Activate))
        {
            remote.Activated += OnRemoteActivated;
        }

        // For commands other than Accept/Activate, subscribe to CommandNotBound.
        // These are commands that the remote view does not have explicit handlers for,
        // so they flow through DefaultCommandNotBoundHandler.
        _subscribedToCommandNotBound = _commands.Any (c => c != Command.Accept && c != Command.Activate);

        if (_subscribedToCommandNotBound)
        {
            remote.CommandNotBound += OnRemoteCommandNotBound;
        }
    }

    /// <summary>
    ///     Connects an owner view to a remote view for the specified commands.
    ///     When the remote view raises <c>Accepted</c>, <c>Activated</c>, or <c>CommandNotBound</c>
    ///     for any of the specified commands, the owner view receives the command with
    ///     <see cref="CommandRouting.Bridged"/> routing.
    /// </summary>
    /// <param name="owner">The view that will receive bridged commands.</param>
    /// <param name="remote">The view whose events will be bridged.</param>
    /// <param name="commands">The commands to bridge (e.g., <c>Command.Accept</c>, <c>Command.Activate</c>, <c>Command.Up</c>).</param>
    /// <returns>A <see cref="CommandBridge"/> that can be disposed to tear down the connection.</returns>
    public static CommandBridge Connect (View owner, View remote, params Command [] commands) => new (owner, remote, commands);

    /// <summary>
    ///     Tears down event subscriptions. Safe to call multiple times.
    /// </summary>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_remote.TryGetTarget (out View? remote))
        {
            return;
        }

        remote.Accepted -= OnRemoteAccepted;
        remote.Activated -= OnRemoteActivated;

        if (_subscribedToCommandNotBound)
        {
            remote.CommandNotBound -= OnRemoteCommandNotBound;
        }
    }

    private void OnRemoteAccepted (object? sender, CommandEventArgs e)
    {
        if (_disposed || !_owner.TryGetTarget (out View? owner))
        {
            return;
        }

        Trace.Command (owner, e.Context, "Bridge", $"{_remote.ToIdentifyingString ()}->{_owner.ToIdentifyingString ()}");

        CommandContext bridgedCtx = new ()
        {
            Command = Command.Accept,
            Source = e.Context?.Source,
            Binding = e.Context?.Binding,
            Routing = CommandRouting.Bridged,
            Values = e.Context?.Values ?? []
        };

        owner.InvokeCommand (Command.Accept, bridgedCtx);
    }

    private void OnRemoteActivated (object? sender, EventArgs<ICommandContext?> e)
    {
        if (_disposed || !_owner.TryGetTarget (out View? owner))
        {
            return;
        }

        Trace.Command (owner, e.Value, "Bridge", $"{_remote.ToIdentifyingString ()}->{_owner.ToIdentifyingString ()}");

        CommandContext bridgedCtx = new ()
        {
            Command = Command.Activate,
            Source = e.Value?.Source,
            Binding = e.Value?.Binding,
            Routing = CommandRouting.Bridged,
            Values = e.Value?.Values ?? []
        };

        owner.InvokeCommand (Command.Activate, bridgedCtx);
    }

    private void OnRemoteCommandNotBound (object? sender, CommandEventArgs e)
    {
        if (_disposed || !_owner.TryGetTarget (out View? owner))
        {
            return;
        }

        // If the event was already handled/cancelled by another handler, do not bridge.
        if (e.Handled)
        {
            return;
        }

        Command command = e.Context?.Command ?? Command.NotBound;

        // Only bridge commands that this bridge is configured for.
        if (!_commands.Contains (command))
        {
            return;
        }

        Trace.Command (owner, e.Context, "Bridge", $"{_remote.ToIdentifyingString ()}->{_owner.ToIdentifyingString ()} ({command})");

        CommandContext bridgedCtx = new ()
        {
            Command = command,
            Source = e.Context?.Source,
            Binding = e.Context?.Binding,
            Routing = CommandRouting.Bridged,
            Values = e.Context?.Values ?? []
        };

        owner.InvokeCommand (command, bridgedCtx);

        // Mark as handled so the remote's own dispatch/bubbling does not duplicate processing.
        e.Handled = true;
    }
}
