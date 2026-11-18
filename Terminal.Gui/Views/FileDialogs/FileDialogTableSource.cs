namespace Terminal.Gui.Views;

internal class FileDialogTableSource (FileDialog? dlg, FileDialogState? state, FileDialogStyle? style, int currentSortColumn, bool currentSortIsAsc)
    : ITableSource
{
    public object this [int row, int col] => state is { } ? GetColumnValue (col, state.Children [row]) : string.Empty;

    public int Rows => state is { } ? state.Children.Count () : 0;

    public int Columns => 4;

    public string [] ColumnNames =>
    [
        MaybeAddSortArrows (style!.FilenameColumnName, 0),
        MaybeAddSortArrows (style.SizeColumnName, 1),
        MaybeAddSortArrows (style.ModifiedColumnName, 2),
        MaybeAddSortArrows (style.TypeColumnName, 3)
    ];

    internal static object GetRawColumnValue (int col, FileSystemInfoStats? stats)
    {
        switch (col)
        {
            case 0: return stats!.FileSystemInfo!.Name;

            case 1: return stats!.MachineReadableLength;

            case 2: return stats!.LastWriteTime ?? default (DateTime);

            case 3: return stats!.Type;
        }

        throw new ArgumentOutOfRangeException (nameof (col));
    }

    private object GetColumnValue (int col, FileSystemInfoStats? stats)
    {
        switch (col)
        {
            case 0:
                // do not use icon for ".."
                if (stats?.IsParent ?? false)
                {
                    return stats.Name;
                }

                string icon = dlg!.Style.IconProvider.GetIconWithOptionalSpace (stats!.FileSystemInfo);

                return (icon + stats.Name).Trim ();

            case 1:
                return stats?.HumanReadableLength ?? string.Empty;

            case 2:
                if (stats is null || stats.IsParent || stats.LastWriteTime is null)
                {
                    return string.Empty;
                }

                return stats.LastWriteTime.Value.ToString (style!.DateFormat);

            case 3:
                return stats?.Type ?? string.Empty;

            default:
                throw new ArgumentOutOfRangeException (nameof (col));
        }
    }

    private string MaybeAddSortArrows (string name, int idx)
    {
        if (idx == currentSortColumn)
        {
            return name + (currentSortIsAsc ? " (▲)" : " (▼)");
        }

        return name;
    }
}
