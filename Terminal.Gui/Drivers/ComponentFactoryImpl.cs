using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
/// Abstract base class implementation of <see cref="IComponentFactory{TInputRecord}"/> that provides a default implementation of <see cref="CreateSizeMonitor"/>.</summary>
/// <typeparam name="TInputRecord">The platform specific keyboard input type (e.g. <see cref="ConsoleKeyInfo"/> or <see cref="WindowsConsole.InputRecord"/></typeparam>
public abstract class ComponentFactoryImpl<TInputRecord> : IComponentFactory<TInputRecord> where TInputRecord : struct
{
    /// <inheritdoc />
    public abstract string? GetDriverName ();

    /// <inheritdoc />
    public AppModel AppModel { get; set; }

    /// <inheritdoc />
    public abstract IInput<TInputRecord> CreateInput ();

    /// <inheritdoc />
    public abstract IInputProcessor CreateInputProcessor (ConcurrentQueue<TInputRecord> inputBuffer, ITimeProvider? timeProvider = null);

    /// <inheritdoc />
    public virtual ISizeMonitor CreateSizeMonitor (IOutput consoleOutput, IOutputBuffer outputBuffer)
    {
        return new SizeMonitorImpl (consoleOutput);
    }

    /// <inheritdoc />
    public abstract IOutput CreateOutput ();
}
