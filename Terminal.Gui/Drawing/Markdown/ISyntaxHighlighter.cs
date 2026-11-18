namespace Terminal.Gui.Drawing;

/// <summary>Provides syntax highlighting for fenced code blocks in <see cref="Markdown"/>.</summary>
/// <remarks>
///     Assign an implementation to <see cref="Markdown.SyntaxHighlighter"/> to colorize
///     code blocks. Each line of the code block is passed individually to <see cref="Highlight"/>.
/// </remarks>
public interface ISyntaxHighlighter
{
    /// <summary>Highlights a single line of code and returns styled segments.</summary>
    /// <param name="code">The source code line to highlight.</param>
    /// <param name="language">
    ///     The language identifier from the fence (e.g. <c>csharp</c>), or <see langword="null"/> if not
    ///     specified.
    /// </param>
    /// <returns>A list of <see cref="StyledSegment"/> instances representing the highlighted tokens.</returns>
    IReadOnlyList<StyledSegment> Highlight (string code, string? language);

    /// <summary>
    ///     Resets internal tokenizer state. Called by <see cref="Markdown"/> at the start
    ///     of each new code block so that stateful tokenizers (e.g., TextMate) begin fresh.
    /// </summary>
    void ResetState ();

    /// <summary>
    ///     Gets the name of the currently active syntax highlighting theme.
    /// </summary>
    string ThemeName { get; }

    /// <summary>
    ///     Gets the default background color from the active syntax highlighting theme.
    ///     Used by code block views to fill their viewport background consistently with
    ///     per-token backgrounds. Returns <see langword="null"/> if no theme background is available.
    /// </summary>
    Color? DefaultBackground { get; }

    /// <summary>
    ///     Returns a theme-derived <see cref="Attribute"/> for the given markdown style role,
    ///     or <see langword="null"/> if this highlighter has no specific styling for that role.
    /// </summary>
    /// <param name="role">The markdown style role to resolve.</param>
    /// <returns>
    ///     An <see cref="Attribute"/> with colors from the active syntax theme, or <see langword="null"/>
    ///     to fall back to the default <see cref="MarkdownStyleRole"/>-based text style.
    /// </returns>
    Attribute? GetAttributeForScope (MarkdownStyleRole role);
}
