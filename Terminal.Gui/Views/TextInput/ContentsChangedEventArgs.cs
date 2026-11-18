namespace Terminal.Gui.Views;

/// <summary>
///     Event arguments for events for when the contents of the TextView change. E.g. the
///     <see cref="TextView.ContentsChanged"/> event.
/// </summary>
[Obsolete ("ContentsChangedEventArgs is obsolete because TextView is superseded by tui-cs/Editor. See https://github.com/tui-cs/Editor.", error: false)]
public class ContentsChangedEventArgs : EventArgs
{
    /// <summary>Creates a new <see cref="TextView.ContentsChanged"/> instance.</summary>
    /// <param name="currentRow">Contains the row where the change occurred.</param>
    /// <param name="currentColumn">Contains the column where the change occured.</param>
    public ContentsChangedEventArgs (int currentRow, int currentColumn)
    {
        Row = currentRow;
        Col = currentColumn;
    }

    /// <summary>Contains the column where the change occurred.</summary>
    public int Col { get; private set; }

    /// <summary>Contains the row where the change occurred.</summary>
    public int Row { get; private set; }
}
