// Copilot

using System.IO.Abstractions.TestingHelpers;
using System.Reflection;

namespace UnitTests.Views;

public class FileDialogColorTests
{
    [Fact]
    public void ParentRowColorGetter_UsesSelectionScheme_WhenNavigatingUp ()
    {
        MockFileSystem fileSystem = new ();
        fileSystem.AddDirectory ("/test-dir/sub-dir");

        using SaveDialog dialog = new TestableSaveDialog (fileSystem);
        dialog.PushState (fileSystem.DirectoryInfo.New ("/test-dir/sub-dir"), false);

        FieldInfo? tableViewField = typeof (FileDialog).GetField ("_tableView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull (tableViewField);

        TableView tableView = Assert.IsType<TableView> (tableViewField!.GetValue (dialog));
        ITableSource table = tableView.Table!;
        Assert.NotNull (table);
        Assert.True (dialog.State!.Children [0].IsParent);

        ColumnStyle nameStyle = tableView.Style.GetOrCreateColumnStyle (0);
        Assert.NotNull (nameStyle.ColorGetter);

        object cellValue = table [0, 0];
        string representation = cellValue.ToString () ?? string.Empty;
        Scheme rowScheme = tableView.GetScheme ();
        Scheme effectiveScheme = nameStyle.ColorGetter! (
                                                    new CellColorGetterArgs (
                                                                              table,
                                                                              0,
                                                                              0,
                                                                              cellValue,
                                                                              representation,
                                                                              rowScheme))!;

        Assert.Equal (rowScheme.Focus, effectiveScheme.Focus);
        Assert.Equal (rowScheme.Active, effectiveScheme.Active);
    }

    private sealed class TestableSaveDialog : SaveDialog
    {
        public TestableSaveDialog (MockFileSystem fileSystem) : base (fileSystem) { }
    }
}
