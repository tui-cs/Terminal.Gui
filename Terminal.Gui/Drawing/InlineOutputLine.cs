namespace Terminal.Gui.Drawing;

/// <summary>One logical line of literal styled text to print above an inline application.</summary>
public sealed class InlineOutputLine
{
    /// <summary>To create a line, supply its spans in display order.</summary>
    /// <param name="spans">Spans copied into a read-only snapshot.</param>
    public InlineOutputLine (IEnumerable<InlineOutputSpan> spans)
    {
        ArgumentNullException.ThrowIfNull (spans);
        Spans = Array.AsReadOnly (spans.ToArray ());
    }

    /// <summary>Gets the immutable span sequence.</summary>
    public IReadOnlyList<InlineOutputSpan> Spans { get; }
}
