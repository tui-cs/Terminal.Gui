namespace Terminal.Gui.Views;

public partial class TextField
{
    private readonly HistoryText _historyText;

    /// <summary>
    ///     Indicates whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool IsDirty => _historyText.IsDirty ([Cell.StringToCells (Text)]);

    /// <summary>Clears the history.</summary>
    public void ClearHistoryChanges () { _historyText.Clear ([Cell.StringToCells (Text)]); }

    private void HistoryText_ChangeText (object? sender, HistoryTextItemEventArgs? obj)
    {
        if (obj is null)
        {
            return;
        }

        Text = Cell.ToString (obj.Lines [obj.InsertionPoint.Y]);
        InsertionPoint = obj.InsertionPoint.X;
        Adjust ();
    }
}
