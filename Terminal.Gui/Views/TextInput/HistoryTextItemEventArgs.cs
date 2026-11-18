namespace Terminal.Gui.Views;

/// <summary>
///     Event arguments for <see cref="HistoryText"/> item events. Used by <see cref="TextField"/> and
///     <see cref="TextView"/>.
/// </summary>
/// <remarks>
///     This class encapsulates information about changes to text, such as the affected lines, cursor positions, and the
///     type of change.
///     It is primarily used to support undo/redo functionality in text editing controls like <see cref="TextView"/> and
///     <see cref="TextField"/>.
/// </remarks>
public class HistoryTextItemEventArgs : EventArgs
{
    /// <summary>
    ///      Gets or sets the insertion point within the text, measured as a 0-based index into text elements.
    /// </summary>
    public Point InsertionPoint { get; set; }

    /// <summary>
    ///     Gets or sets the final insertion point after the change is applied.
    /// </summary>
    public Point FinalInsertionPoint { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the change is part of an undo operation.
    /// </summary>
    public bool IsUndoing { get; set; }

    /// <summary>
    ///     Gets or sets the lines of text affected by the change.
    /// </summary>
    /// <remarks>
    ///     Each line is represented as a list of <see cref="Cell"/> objects, which include the text and its attributes.
    /// </remarks>
    public List<List<Cell>> Lines { get; set; }

    /// <summary>
    ///     Gets or sets the status of the line(s) affected by the change.
    /// </summary>
    /// <seealso cref="TextEditingLineStatus"/>
    public TextEditingLineStatus LineStatus { get; set; }

    /// <summary>
    ///     Gets or sets the associated <see cref="HistoryTextItemEventArgs"/> for a removed line when an added line replaces
    ///     it.
    /// </summary>
    /// <remarks>
    ///     This property is used to track the relationship between added and removed lines during undo/redo operations.
    /// </remarks>
    public HistoryTextItemEventArgs? RemovedOnAdded { get; set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class with the specified lines, cursor
    ///     position, line status, and associated removed line.
    /// </summary>
    /// <param name="lines">The lines of text affected by the change.</param>
    /// <param name="insertionPoints">The insertion point at the time of the change.</param>
    /// <param name="linesStatus">The status of the line(s) affected by the change.</param>
    /// <param name="removedOnAdded">
    ///     The associated <see cref="HistoryTextItemEventArgs"/> for a removed line when an added
    ///     line replaces it.
    /// </param>
    public HistoryTextItemEventArgs (List<List<Cell>> lines, Point insertionPoints, TextEditingLineStatus linesStatus, HistoryTextItemEventArgs removedOnAdded)
    {
        Lines = lines;
        InsertionPoint = insertionPoints;
        LineStatus = linesStatus;
        RemovedOnAdded = removedOnAdded;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class by copying an existing instance and
    ///     associating it with a removed line.
    /// </summary>
    /// <param name="historyTextItem">The existing <see cref="HistoryTextItemEventArgs"/> to copy.</param>
    /// <param name="removedOnAdded">
    ///     The associated <see cref="HistoryTextItemEventArgs"/> for a removed line when an added
    ///     line replaces it.
    /// </param>
    public HistoryTextItemEventArgs (HistoryTextItemEventArgs historyTextItem, HistoryTextItemEventArgs removedOnAdded)
    {
        RemovedOnAdded = removedOnAdded;
        Lines = [.. historyTextItem.Lines];
        InsertionPoint = new (historyTextItem.InsertionPoint.X, historyTextItem.InsertionPoint.Y);
        LineStatus = historyTextItem.LineStatus;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class with the specified lines, cursor
    ///     position, and line status.
    /// </summary>
    /// <param name="lines">The lines of text affected by the change.</param>
    /// <param name="curPos">The cursor position at the time of the change.</param>
    /// <param name="linesStatus">The status of the line(s) affected by the change.</param>
    public HistoryTextItemEventArgs (List<List<Cell>> lines, Point curPos, TextEditingLineStatus linesStatus)
    {
        Lines = lines;
        InsertionPoint = curPos;
        LineStatus = linesStatus;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryTextItemEventArgs"/> class by copying an existing instance.
    /// </summary>
    /// <param name="historyTextItem">The existing <see cref="HistoryTextItemEventArgs"/> to copy.</param>
    public HistoryTextItemEventArgs (HistoryTextItemEventArgs historyTextItem)
    {
        Lines = [.. historyTextItem.Lines];
        InsertionPoint = new (historyTextItem.InsertionPoint.X, historyTextItem.InsertionPoint.Y);
        LineStatus = historyTextItem.LineStatus;
    }

    /// <summary>
    ///     Returns a string representation of the <see cref="HistoryTextItemEventArgs"/> instance.
    /// </summary>
    /// <returns>A string containing the count of lines, insertion point, and line status.</returns>
    public override string ToString () => $"(Count: {Lines.Count}, InsertionPoint: {InsertionPoint}, Status: {LineStatus})";
}
