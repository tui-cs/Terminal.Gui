namespace Terminal.Gui.Drawing;

/// <summary>A segment of styled text produced by <see cref="Markdown"/> during layout.</summary>
/// <remarks>
///     Each segment carries the display text, a <see cref="MarkdownStyleRole"/> that controls
///     visual rendering, and optional URL / image-source metadata for hyperlinks and images.
/// </remarks>
public sealed class StyledSegment
{
    /// <summary>Initializes a new <see cref="StyledSegment"/>.</summary>
    /// <param name="text">The display text of the segment.</param>
    /// <param name="styleRole">The semantic role that controls rendering style.</param>
    /// <param name="url">Optional hyperlink URL. <see langword="null"/> for non-link segments.</param>
    /// <param name="imageSource">Optional image source path. <see langword="null"/> for non-image segments.</param>
    /// <param name="attribute">
    ///     Optional explicit <see cref="Drawing.Attribute"/>. When non-null, this attribute is used directly
    ///     for rendering, bypassing the <see cref="StyleRole"/>-based resolution in
    ///     <see cref="MarkdownAttributeHelper.GetAttributeForSegment"/>.
    /// </param>
    /// <param name="role">Optional token-level <see cref="VisualRole"/> used for source-code rendering.</param>
    public StyledSegment (
        string text,
        MarkdownStyleRole styleRole,
        string? url = null,
        string? imageSource = null,
        Attribute? attribute = null,
        VisualRole? role = null)
    {
        Text = text;
        StyleRole = styleRole;
        Url = url;
        ImageSource = imageSource;
        Attribute = attribute;
        Role = role;
    }

    /// <summary>Gets the display text of this segment.</summary>
    public string Text { get; }

    /// <summary>Gets the semantic role that determines how this segment is rendered.</summary>
    public MarkdownStyleRole StyleRole { get; }

    /// <summary>Gets the hyperlink URL, or <see langword="null"/> if this is not a link segment.</summary>
    public string? Url { get; }

    /// <summary>Gets the image source path, or <see langword="null"/> if this is not an image segment.</summary>
    public string? ImageSource { get; }

    /// <summary>
    ///     Gets the explicit <see cref="Drawing.Attribute"/> for this segment, or <see langword="null"/>
    ///     if the attribute should be resolved from <see cref="StyleRole"/>.
    /// </summary>
    /// <remarks>
    ///     When set (e.g., by a syntax highlighter), this attribute is used directly for rendering,
    ///     bypassing the normal <see cref="MarkdownStyleRole"/>-based resolution.
    /// </remarks>
    public Attribute? Attribute { get; }

    /// <summary>
    ///     Gets the token-level <see cref="VisualRole"/> for this segment, or <see langword="null"/>
    ///     if the role should be resolved from <see cref="StyleRole"/>.
    /// </summary>
    public VisualRole? Role { get; }

    // Preserve nested Markdown styles without fixing colors before the view's scheme is resolved.
    internal TextStyle AdditionalStyle { get; init; }
    internal int LinkIdentity { get; init; } = -1;

    internal StyledSegment WithText (string text, VisualRole? role = null) =>
        new (text, StyleRole, Url, ImageSource, Attribute, role ?? Role)
        {
            AdditionalStyle = AdditionalStyle,
            LinkIdentity = LinkIdentity
        };
}
