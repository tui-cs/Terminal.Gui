namespace Terminal.Gui.App;

/// <summary>Provides ordered, awaitable literal output above an inline application.</summary>
public static class ApplicationInlineOutputExtensions
{
    /// <summary>Queues text above the live inline region, using terminal default styling.</summary>
    /// <param name="app">Initialized inline application.</param>
    /// <param name="text">Literal text. CRLF and CR become LF; one trailing LF terminates the last line.</param>
    /// <param name="cancellationToken">Cancels before insertion begins.</param>
    /// <returns>A task completed after insertion, canceled before insertion, or faulted on failure.</returns>
    /// <remarks>
    ///     To print from a UI handler, await without blocking the event loop. Calls always queue in admission order.
    ///     Empty text prints one blank line. Styled text uses the line overload. Raw ANSI controls are rejected.
    ///     Content-sized inline views can shrink before insertion. Resize recovers the live origin without clearing printed text.
    ///     Requires the ANSI driver in Inline mode; raster graphics and headless output are unsupported.
    ///     Pending output ends with its session or disposal. After an uncertain I/O failure, stop the application;
    ///     further inline placement is suppressed until a new initialized lifetime. Never blindly retry a failed insertion.
    /// </remarks>
    /// <exception cref="NotInitializedException">The application is not initialized or is stopping.</exception>
    /// <exception cref="NotSupportedException">The application or output cannot support inline printing.</exception>
    /// <exception cref="InvalidOperationException">The mode, geometry, or queue capacity is invalid.</exception>
    /// <exception cref="ArgumentException">Text contains invalid Unicode or terminal controls.</exception>
    public static Task PrintAboveAsync (this IApplication app, string text, CancellationToken cancellationToken = default) =>
        PrintText (app, text, null, cancellationToken);

    /// <summary>Queues literal text owned by a session.</summary>
    /// <param name="app">Initialized inline application.</param>
    /// <param name="owner">Session that owns pending output; an ended or foreign session cancels it.</param>
    /// <param name="text">Literal text with the same newline rules as the unowned overload.</param>
    /// <param name="cancellationToken">Cancels before insertion begins.</param>
    /// <returns>The insertion task.</returns>
    /// <inheritdoc cref="PrintAboveAsync(IApplication,string,CancellationToken)" path="/remarks|/exception"/>
    public static Task PrintAboveAsync (this IApplication app, SessionToken owner, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (owner);
        return PrintText (app, text, owner, cancellationToken);
    }

    /// <summary>Queues logical styled lines above the live inline region.</summary>
    /// <param name="app">Initialized inline application.</param>
    /// <param name="lines">Logical lines copied before admission; an empty list emits nothing.</param>
    /// <param name="cancellationToken">Cancels before insertion begins.</param>
    /// <returns>The insertion task.</returns>
    /// <inheritdoc cref="PrintAboveAsync(IApplication,string,CancellationToken)" path="/remarks|/exception"/>
    public static Task PrintAboveAsync (this IApplication app, IReadOnlyList<InlineOutputLine> lines, CancellationToken cancellationToken = default) =>
        GetOutput (app).PrintAboveAsync (lines, null, cancellationToken);

    /// <summary>Queues logical styled lines owned by a session.</summary>
    /// <param name="app">Initialized inline application.</param>
    /// <param name="owner">Session that owns pending output.</param>
    /// <param name="lines">Logical lines copied before admission.</param>
    /// <param name="cancellationToken">Cancels before insertion begins.</param>
    /// <returns>The insertion task.</returns>
    /// <inheritdoc cref="PrintAboveAsync(IApplication,string,CancellationToken)" path="/remarks|/exception"/>
    public static Task PrintAboveAsync (this IApplication app, SessionToken owner, IReadOnlyList<InlineOutputLine> lines, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (owner);
        return GetOutput (app).PrintAboveAsync (lines, owner, cancellationToken);
    }

    private static Task PrintText (IApplication app, string text, SessionToken? owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull (text);
        IApplicationInlineOutput output = GetOutput (app);
        return output is ApplicationImpl implementation
                   ? implementation.PrintAboveTextAsync (text, owner, cancellationToken)
                   : output.PrintAboveAsync (Split (text), owner, cancellationToken);
    }

    internal static InlineOutputLine [] Split (string text)
    {
        ArgumentNullException.ThrowIfNull (text);
        long units = 0;
        int lineLength = 0;
        bool endedWithLineBreak = false;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text [index];
            if (character is not ('\r' or '\n'))
            {
                lineLength++;
                endedWithLineBreak = false;
                continue;
            }
            if (character == '\r' && index + 1 < text.Length && text [index + 1] == '\n')
            {
                index++;
            }
            // Each materialized line has one span. Count the line and at least one span unit.
            units += 1 + Math.Max (1, lineLength);
            if (units > InlineOutputLayout.MaximumUnits)
            {
                throw new InvalidOperationException ("Inline output payload is too large. Submit smaller awaited batches.");
            }
            lineLength = 0;
            endedWithLineBreak = true;
        }
        if (!endedWithLineBreak || text.Length == 0)
        {
            units += 1 + Math.Max (1, lineLength);
        }
        if (units > InlineOutputLayout.MaximumUnits)
        {
            throw new InvalidOperationException ("Inline output payload is too large. Submit smaller awaited batches.");
        }
        string normalized = text.Replace ("\r\n", "\n").Replace ('\r', '\n');
        if (normalized.EndsWith ('\n'))
        {
            normalized = normalized [..^1];
        }
        return normalized.Split ('\n').Select (line => new InlineOutputLine ([new InlineOutputSpan (line)])).ToArray ();
    }

    private static IApplicationInlineOutput GetOutput (IApplication app)
    {
        ArgumentNullException.ThrowIfNull (app);
        return app as IApplicationInlineOutput ?? throw new NotSupportedException ("This application does not support inline output.");
    }
}
