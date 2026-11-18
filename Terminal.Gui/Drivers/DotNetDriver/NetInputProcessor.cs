using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="NetInput"/>, deals in <see cref="ConsoleKeyInfo"/> stream
/// </summary>
public class NetInputProcessor : InputProcessorImpl<ConsoleKeyInfo>
{
    /// <inheritdoc/>
    /// <param name="inputBuffer">The input buffer to process.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    public NetInputProcessor (ConcurrentQueue<ConsoleKeyInfo> inputBuffer, ITimeProvider? timeProvider = null) : base (inputBuffer,
                                                                                                                       new NetKeyConverter (),
                                                                                                                       timeProvider)
    { }

    /// <inheritdoc/>
    protected override void Process (ConsoleKeyInfo input)
    {
        foreach (Tuple<char, ConsoleKeyInfo> released in Parser.ProcessInput (Tuple.Create (input.KeyChar, input)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }
}
