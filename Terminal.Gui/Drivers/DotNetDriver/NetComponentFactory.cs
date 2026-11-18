using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IComponentFactory{T}"/> implementation for native csharp console I/O i.e. dotnet.
///     This factory creates instances of internal classes <see cref="NetInput"/>, <see cref="NetOutput"/> etc.
/// </summary>
public class NetComponentFactory : ComponentFactoryImpl<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    public override string? GetDriverName () => DriverRegistry.Names.DOTNET;

    /// <inheritdoc/>
    public override IInput<ConsoleKeyInfo> CreateInput () { return new NetInput (); }

    /// <inheritdoc/>
    public override IInputProcessor CreateInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer, ITimeProvider? timeProvider = null) { return new NetInputProcessor (inputBuffer, timeProvider); }

    /// <inheritdoc/>
    public override IOutput CreateOutput () { return new NetOutput (); }
}
