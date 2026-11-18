namespace Terminal.Gui.Views;

/// <summary>An <see cref="ITableSource"/> with expandable rows.</summary>
/// <typeparam name="T"></typeparam>
public class TreeTableSource<T> : IEnumerableTableSource<T>, IDisposable where T : class
{
    private readonly Dictionary<string, Func<T, object>> _lambdas;
    private readonly TableView _tableView;
    private readonly TreeView<T> _tree;

    /// <summary>
    ///     Creates a new instance of <see cref="TreeTableSource{T}"/> presenting the given <paramref name="tree"/>. This
    ///     source should only be used with <paramref name="table"/>.
    /// </summary>
    /// <param name="table">The table this source will provide data for.</param>
    /// <param name="firstColumnName">
    ///     Column name to use for the first column of the table (where the tree branches/leaves will
    ///     be rendered).
    /// </param>
    /// <param name="tree">
    ///     The tree data to render. This should be a new view and not used elsewhere (e.g. via
    ///     <see cref="View.Add(View)"/>).
    /// </param>
    /// <param name="subsequentColumns">
    ///     Getter methods for each additional property you want to present in the table. For example:
    ///     <code>
    ///  new () {
    ///     { "Col name1", (t)=>t.SomeField},
    ///     { "Col name2", (t)=>t.SomeOtherField}
    /// }
    ///  </code>
    /// </param>
    public TreeTableSource (TableView table, string firstColumnName, TreeView<T> tree, Dictionary<string, Func<T, object>> subsequentColumns)
    {
        _tableView = table;
        _tree = tree;
        _tableView.KeyDown += Table_KeyPress;
        _tableView.Activating += Table_Activating;

        List<string> colList = subsequentColumns.Keys.ToList ();
        colList.Insert (0, firstColumnName);

        ColumnNames = colList.ToArray ();

        _lambdas = subsequentColumns;
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        _tableView.KeyDown -= Table_KeyPress;
        _tableView.Activating -= Table_Activating;
        _tree.Dispose ();
    }

    /// <inheritdoc/>
    public object this [int row, int col] => col == 0 ? GetColumnZeroRepresentationFromTree (row) : _lambdas [ColumnNames [col]] (RowToObject (row));

    /// <inheritdoc/>
    public int Rows => _tree.BuildLineMap ().Count;

    /// <inheritdoc/>
    public int Columns => _lambdas.Count + 1;

    /// <inheritdoc/>
    public string [] ColumnNames { get; }

    /// <inheritdoc/>
    public T GetObjectOnRow (int row) => RowToObject (row);

    /// <inheritdoc/>
    public IEnumerable<T> GetAllObjects () => _tree.BuildLineMap ().Select (b => b.Model);

    /// <summary>Returns the tree model object rendering on the given <paramref name="row"/> of the table.</summary>
    /// <param name="row">Row in table.</param>
    /// <returns></returns>
    public T RowToObject (int row) => _tree.BuildLineMap ().ElementAt (row).Model;

    private string GetColumnZeroRepresentationFromTree (int row)
    {
        Branch<T> branch = RowToBranch (row);

        // Everything on the line before the expansion run and branch text
        string [] prefix = branch.GetLinePrefix ().ToArray ();
        string expansion = branch.GetExpandableSymbol ();
        string lineBody = _tree.AspectGetter (branch.Model);

        var sb = new StringBuilder ();

        foreach (string p in prefix)
        {
            sb.Append (p);
        }

        sb.Append (expansion);
        sb.Append (lineBody);

        return sb.ToString ();
    }

    private bool IsInTreeColumn (int column, bool isKeyboard)
    {
        string [] colNames = _tableView.Table!.ColumnNames;

        if (column < 0 || column >= colNames.Length)
        {
            return false;
        }

        // if full row is selected then it is hard to tell which sub cell in the tree
        // has focus so we should typically just always respond with expand/collapse
        if (_tableView.FullRowSelect && isKeyboard)
        {
            return true;
        }

        // we cannot just check that the cursor column is 0 because source may
        // be wrapped e.g. with a CheckBoxTableSourceWrapperBase
        return colNames [column] == ColumnNames [0];
    }

    private Branch<T> RowToBranch (int row) => _tree.BuildLineMap ().ElementAt (row);

    private void Table_KeyPress (object? sender, Key e)
    {
        if (!IsInTreeColumn (_tableView.Value?.SelectedCell.X ?? 0, true))
        {
            return;
        }

        T? obj = _tree.GetObjectOnRow (_tableView.Value?.SelectedCell.Y ?? 0);

        if (obj is null)
        {
            return;
        }

        if (e.KeyCode == KeyCode.CursorLeft)
        {
            if (_tree.IsExpanded (obj))
            {
                _tree.Collapse (obj);
                e.Handled = true;
            }
        }

        if (e.KeyCode == KeyCode.CursorRight)
        {
            if (_tree.CanExpand (obj) && !_tree.IsExpanded (obj))
            {
                _tree.Expand (obj);
                e.Handled = true;
            }
        }

        if (!e.Handled)
        {
            return;
        }
        _tree.InvalidateLineMap ();
        _tableView.RefreshContentSize ();
        _tableView.SetNeedsDraw ();
    }

    private void Table_Activating (object? sender, CommandEventArgs e)
    {
        // Only handle mouse clicks, not keyboard selections
        if (e.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return;
        }

        Point? hit = _tableView.ScreenToCell (mouse.Position!.Value.X, mouse.Position!.Value.Y, out int? headerIfAny, out int? offsetX);

        if (hit is null || headerIfAny is { } || !IsInTreeColumn (hit.Value.X, false) || offsetX is null)
        {
            return;
        }

        Branch<T> branch = RowToBranch (hit.Value.Y);

        if (branch.IsHitOnExpandableSymbol (offsetX.Value))
        {
            T m = branch.Model;

            if (_tree.CanExpand (m) && !_tree.IsExpanded (m))
            {
                _tree.Expand (m);

                e.Handled = true;
            }
            else if (_tree.IsExpanded (m))
            {
                _tree.Collapse (m);
                e.Handled = true;
            }
        }

        if (!e.Handled)
        {
            return;
        }
        _tree.InvalidateLineMap ();
        _tableView.RefreshContentSize ();
        _tableView.SetNeedsDraw ();
    }
}
