namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>Closes the contents of the stream into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if stream was closed, <c>false</c> otherwise.</returns>
    public bool CloseFile ()
    {
        SetWrapModel ();
        bool res = _model.CloseFile ();
        ResetPosition ();
        UpdateWrapModel ();

        return res;
    }

    /// <summary>Loads the contents of the file into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if file was loaded, <c>false</c> otherwise.</returns>
    /// <param name="path">Path to the file to load.</param>
    public bool Load (string path)
    {
        bool res;

        try
        {
            SetWrapModel ();
            res = _model.LoadFile (path);
            _historyText.Clear (_model.GetAllLines ());
            ResetPosition ();
        }
        finally
        {
            UpdateWrapModel ();
            AdjustViewport ();
        }

        return res;
    }

    /// <summary>Loads the contents of the stream into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if stream was loaded, <c>false</c> otherwise.</returns>
    /// <param name="stream">Stream to load the contents from.</param>
    public void Load (Stream stream)
    {
        SetWrapModel ();
        _model.LoadStream (stream);
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        UpdateWrapModel ();
    }

    /// <summary>Loads the contents of the <see cref="Cell"/> list into the <see cref="TextView"/>.</summary>
    /// <param name="cells">Text cells list to load the contents from.</param>
    public void Load (List<Cell> cells)
    {
        SetWrapModel ();
        _model.LoadCells (cells, GetAttributeForRole (VisualRole.Focus));
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        UpdateWrapModel ();
        InheritsPreviousAttribute = true;
    }

    /// <summary>Loads the contents of the list of <see cref="Cell"/> list into the <see cref="TextView"/>.</summary>
    /// <param name="cellsList">List of rune cells list to load the contents from.</param>
    public void Load (List<List<Cell>> cellsList)
    {
        SetWrapModel ();
        InheritsPreviousAttribute = true;
        _model.LoadListCells (cellsList, GetAttributeForRole (VisualRole.Focus));
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        UpdateWrapModel ();
    }
}
