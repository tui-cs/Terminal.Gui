namespace Terminal.Gui.Views;

public partial class TextView
{
    private readonly HistoryText _historyText = new ();

    /// <summary>
    ///     Indicates whatever the text has history changes or not. <see langword="true"/> if the text has history changes
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool HasHistoryChanges => _historyText.HasHistoryChanges;

    /// <summary>Allows clearing the <see cref="HistoryTextItemEventArgs"/> items updating the original text.</summary>
    public void ClearHistoryChanges () => _historyText.Clear (_model.GetAllLines ());

    private void HistoryText_ChangeText (object? sender, HistoryTextItemEventArgs? obj)
    {
        SetWrapModel ();

        if (obj is { })
        {
            int startLine = obj.InsertionPoint.Y;

            if (obj.RemovedOnAdded is { })
            {
                int offset;

                if (obj.IsUndoing)
                {
                    offset = Math.Max (obj.RemovedOnAdded.Lines.Count - obj.Lines.Count, 1);
                }
                else
                {
                    offset = obj.RemovedOnAdded.Lines.Count - 1;
                }

                for (var i = 0; i < offset; i++)
                {
                    if (Lines > obj.RemovedOnAdded.InsertionPoint.Y)
                    {
                        _model.RemoveLine (obj.RemovedOnAdded.InsertionPoint.Y);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            for (var i = 0; i < obj.Lines.Count; i++)
            {
                if (i == 0 || obj.LineStatus == TextEditingLineStatus.Original || obj.LineStatus == TextEditingLineStatus.Attribute)
                {
                    _model.ReplaceLine (startLine, obj.Lines [i]);
                }
                else if (obj is { IsUndoing: true, LineStatus: TextEditingLineStatus.Removed } or { IsUndoing: false, LineStatus: TextEditingLineStatus.Added })
                {
                    _model.AddLine (startLine, obj.Lines [i]);
                }
                else if (Lines > obj.InsertionPoint.Y + 1)
                {
                    _model.RemoveLine (obj.InsertionPoint.Y + 1);
                }

                startLine++;
            }

            InsertionPoint = obj.FinalInsertionPoint;
        }

        UpdateWrapModel ();

        AdjustViewport ();
        OnContentsChanged ();
    }
}
