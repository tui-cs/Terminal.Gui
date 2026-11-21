namespace Terminal.Gui.App;

/// <summary>Optional application capability for ordered output above a live inline text region.</summary>
public interface IApplicationInlineOutput
{
    /// <summary>Queues literal styled lines for the next eligible rendering boundary.</summary>
    /// <param name="lines">Logical lines; an empty list emits nothing.</param>
    /// <param name="owner">Optional session whose end cancels pending output.</param>
    /// <param name="cancellationToken">Cancels before output is claimed.</param>
    /// <returns>Completion after insertion and origin publication, or cancellation or a failure.</returns>
    /// <remarks>
    ///     Requires an initialized inline application using ANSI output. All calls queue, including UI-thread calls.
    ///     Success means insertion bytes were accepted; it does not guarantee durable scrollback or live-frame delivery.
    ///     Once claimed, cancellation cannot interrupt insertion. A partial I/O failure is never automatically retried.
    /// </remarks>
    Task PrintAboveAsync (IReadOnlyList<InlineOutputLine> lines, SessionToken? owner, CancellationToken cancellationToken = default);
}
