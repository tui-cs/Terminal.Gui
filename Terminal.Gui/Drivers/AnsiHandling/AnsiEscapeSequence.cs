namespace Terminal.Gui.Drivers;

/// <summary>
///     Describes an Ansi escape sequence. This is a 'blueprint'. If you
///     want to send the sequence you should instead use <see cref="AnsiEscapeSequenceRequest"/>
/// </summary>
public class AnsiEscapeSequence
{
    /// <summary>
    ///     Request to send e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    /// </summary>
    public required string Request { get; init; }

    /// <summary>
    ///     <para>
    ///         The terminator that uniquely identifies the type of response as responded
    ///         by the console. e.g. for
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///         </see>
    ///         the terminator is
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Terminator</cref>
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         After sending a request, the first response with matching terminator will be matched
    ///         to the oldest outstanding request.
    ///     </para>
    /// </summary>
    public required string? Terminator { get; init; }

    /// <summary>
    ///     The value expected in the response e.g.
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_ReportTerminalSizeInChars.Value</cref>
    ///     </see>
    ///     which will have a 't' as terminator but also other different request may return the same terminator with a
    ///     different value.
    /// </summary>
    public string? Value { get; init; }
}
