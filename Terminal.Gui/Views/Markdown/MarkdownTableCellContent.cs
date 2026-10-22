namespace Terminal.Gui.Views;

/// <summary>A cell origin and its retained, viewport-independent display blocks.</summary>
internal sealed class MarkdownTableCellContent (int row, TableCellData definition, IReadOnlyList<IntermediateBlock> blocks)
{
    public int Row { get; } = row;
    public TableCellData Definition { get; } = definition;
    public IReadOnlyList<IntermediateBlock> Blocks { get; } = blocks;
    public List<StyledSegment> Segments => [.. Blocks.SelectMany (block => block.Runs.Select (run => run.ToStyledSegment ()))];
}
