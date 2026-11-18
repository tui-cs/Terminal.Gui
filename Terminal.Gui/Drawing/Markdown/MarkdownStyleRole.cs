namespace Terminal.Gui.Drawing;

/// <summary>Identifies the semantic role of a styled text segment within a <see cref="Markdown"/>.</summary>
/// <remarks>
///     The role determines how the segment is rendered (font style, color, background).
///     See <see cref="Markdown"/> for the mapping of roles to visual attributes.
/// </remarks>
public enum MarkdownStyleRole
{
    /// <summary>Plain body text with no special formatting.</summary>
    Normal,

    /// <summary>Heading text (<c># … ######</c>). Rendered bold.</summary>
    Heading,

    /// <summary>Heading marker characters (<c>#</c>, <c>##</c>, etc.). Rendered bold.</summary>
    HeadingMarker,

    /// <summary>Emphasized text (<c>*italic*</c>). Rendered italic.</summary>
    Emphasis,

    /// <summary>Strongly emphasized text (<c>**bold**</c>). Rendered bold.</summary>
    Strong,

    /// <summary>Inline code span (<c>`code`</c>). Rendered bold with a dimmed background.</summary>
    InlineCode,

    /// <summary>Fenced code block line. Rendered bold with a full-width dimmed background.</summary>
    CodeBlock,

    /// <summary>Block-quote text (<c>&gt; …</c>). Rendered faint.</summary>
    Quote,

    /// <summary>The bullet or number prefix of a list item (e.g. <c>•</c>). Rendered bold.</summary>
    ListMarker,

    /// <summary>Hyperlink text (<c>[text](url)</c>). Absolute URLs and anchor links are underlined.</summary>
    Link,

    /// <summary>Table row text. Rendered bold.</summary>
    Table,

    /// <summary>Thematic break (<c>---</c>, <c>***</c>, <c>___</c>). Rendered faint.</summary>
    ThematicBreak,

    /// <summary>Alt-text for an image (<c>![alt](src)</c>). Rendered italic.</summary>
    ImageAlt,

    /// <summary>Completed task-list item (<c>[x]</c>). Rendered with strikethrough.</summary>
    TaskDone,

    /// <summary>Incomplete task-list item (<c>[ ]</c>). Rendered bold.</summary>
    TaskTodo,

    /// <summary>Strikethrough text (<c>~~text~~</c>). Rendered with strikethrough style.</summary>
    Strikethrough
}
