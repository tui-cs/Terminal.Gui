namespace DocSnippetValidator;

/// <summary>A fenced C# code block extracted from a markdown file.</summary>
/// <param name="FilePath">The markdown file the block came from.</param>
/// <param name="StartLine">The 1-based line of the opening fence.</param>
/// <param name="Code">The code inside the fence, with fence indentation stripped.</param>
/// <param name="Ignored">Whether the block is excluded from compilation (marked WRONG or `snippet: ignore`).</param>
public record Snippet (string FilePath, int StartLine, string Code, bool Ignored);
