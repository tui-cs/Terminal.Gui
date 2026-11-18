namespace Terminal.Gui.Views;

internal class FileDialogCollectionNavigator (FileDialog fileDialog, TableView tableView) : CollectionNavigatorBase
{
    protected override object ElementAt (int idx)
    {
        object val = FileDialogTableSource.GetRawColumnValue (tableView.Value?.SelectedCell.X ?? 0, fileDialog.State?.Children [idx]);

        return val.ToString ()?.Trim ('.') ?? string.Empty;
    }

    protected override int GetCollectionLength () => fileDialog.State?.Children.Length ?? 0;
}
