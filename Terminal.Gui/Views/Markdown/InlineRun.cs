namespace Terminal.Gui.Views;

internal sealed record InlineRun (
    string Text,
    MarkdownStyleRole StyleRole,
    string? Url = null,
    string? ImageSource = null,
    Attribute? Attribute = null,
    VisualRole? Role = null)
{
    public TextStyle AdditionalStyle { get; init; }
    public int LinkIdentity { get; init; } = -1;

    public StyledSegment ToStyledSegment (string? text = null) =>
        new (text ?? Text, StyleRole, Url, ImageSource, Attribute, Role)
        {
            AdditionalStyle = AdditionalStyle,
            LinkIdentity = LinkIdentity
        };

    public static InlineRun FromStyledSegment (StyledSegment segment) =>
        new (segment.Text, segment.StyleRole, segment.Url, segment.ImageSource, segment.Attribute, segment.Role)
        {
            AdditionalStyle = segment.AdditionalStyle,
            LinkIdentity = segment.LinkIdentity
        };
}
