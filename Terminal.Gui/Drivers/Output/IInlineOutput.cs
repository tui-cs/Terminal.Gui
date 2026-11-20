namespace Terminal.Gui.Drivers;

// Separate from replayable frames: a partial insertion must never be retried.
internal interface IInlineOutput
{
    bool IsInlineOutputAvailable { get; }
    bool IsInlinePlacementKnown { get; }
    void ValidateInsertion (IOutputBuffer buffer);
    int InsertAbove (InlineOutputLayout layout, Rectangle region, Size terminal);
    void LoseInlinePlacement ();
}
