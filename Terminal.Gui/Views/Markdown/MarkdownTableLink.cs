namespace Terminal.Gui.Views;

internal sealed record MarkdownTableLink (string Url, int CellIndex, int Occurrence)
{
    public bool IsSafeAbsolute { get; } = MarkdownAttributeHelper.TryCreateSafeAbsoluteUri (Url, out _);
}
