namespace Terminal.Gui.Tracing;

/// <summary>
///     Captures information about a trace event for debugging and testing.
/// </summary>
/// <param name="Category">The trace category (Command, Mouse, Keyboard, etc.).</param>
/// <param name="Id">Identifying string of the related object (e.g. <c>View.ToIdentifyingString()</c>).</param>
/// <param name="Phase">The phase of processing (e.g., "Entry", "Exit", "Routing").</param>
/// <param name="Method">The method name where the trace occurred.</param>
/// <param name="Message">Optional additional context message.</param>
/// <param name="Timestamp">When the trace was captured.</param>
/// <param name="Data">Category-specific data (Command, MouseFlags, Key, etc.).</param>
public readonly record struct TraceEntry (TraceCategory Category, string Id, string Phase, string Method, string? Message, DateTime Timestamp, object? Data);
