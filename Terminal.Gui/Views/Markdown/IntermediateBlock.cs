namespace Terminal.Gui.Views;

internal sealed class IntermediateBlock (IReadOnlyList<InlineRun> runs,
                                         bool wrap,
                                         string prefix = "",
                                         string continuationPrefix = "",
                                         bool isCodeBlock = false,
                                         string? anchor = null,
                                         bool isThematicBreak = false,
                                         TableData? tableData = null,
                                         string? language = null,
                                         MarkdownTableContent? tableContent = null,
                                         string? sourceText = null)
{
    public IReadOnlyList<InlineRun> Runs { get; } = runs;
    public bool Wrap { get; } = wrap;
    public string Prefix { get; } = prefix;
    public string? SourceText { get; } = sourceText;
    public string ContinuationPrefix { get; } = continuationPrefix;
    public bool IsCodeBlock { get; } = isCodeBlock;
    public bool IsThematicBreak { get; } = isThematicBreak;

    /// <summary>Gets the parsed table data if this block represents a table; otherwise <see langword="null"/>.</summary>
    public TableData? TableData { get; } = tableData;

    public MarkdownTableContent? TableContent { get; } = tableContent;

    /// <summary>Gets whether this block represents a Markdown table.</summary>
    public bool IsTable => TableData is { };

    /// <summary>The GitHub-style anchor slug for heading blocks, or <see langword="null"/> for non-heading blocks.</summary>
    public string? Anchor { get; } = anchor;

    /// <summary>
    ///     The fenced code block language specifier (e.g. <c>"cs"</c>, <c>"python"</c>), or
    ///     <see langword="null"/> when this is not a code block or no language was given.
    /// </summary>
    public string? Language { get; } = language;
}
