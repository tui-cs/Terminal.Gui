namespace Terminal.Gui.Views;

/// <summary>A complete positioned grapheme, shared by rendering, links, and selection.</summary>
internal sealed record MarkdownTableGlyph (string Text, StyledSegment Segment, Point Position, int Width, bool IsHeader, int LinkIndex);
