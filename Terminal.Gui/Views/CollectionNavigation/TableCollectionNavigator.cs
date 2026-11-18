namespace Terminal.Gui.Views;

/// <summary>Collection navigator for cycling selections in a <see cref="TableView"/>.</summary>
internal class TableCollectionNavigator : CollectionNavigatorBase
{
    private readonly TableView _tableView;

    /// <summary>Creates a new instance for navigating the data in the wrapped <paramref name="tableView"/>.</summary>
    public TableCollectionNavigator (TableView tableView) => _tableView = tableView;

    /// <inheritdoc/>
    protected override object ElementAt (int idx)
    {
        int col = _tableView.FullRowSelect ? 0 : _tableView.Value?.SelectedCell.X ?? 0;
        object? rawValue = _tableView.Table? [idx, col];

        if (rawValue is null or DBNull)
        {
            return string.Empty;
        }

        ColumnStyle? style = _tableView.Style.GetColumnStyleIfAny (col);
        string? representation = style?.RepresentationGetter?.Invoke (rawValue);

        return representation ?? rawValue;
    }

    /// <inheritdoc/>
    protected override int GetCollectionLength () => _tableView.Table?.Rows ?? 0;
}
