namespace Terminal.Gui.Drivers;

// A physical row owns its style runs and measured width; output need not segment its text again.
internal readonly record struct InlineOutputRow (IReadOnlyList<InlineOutputSpan> Spans, int Columns);
