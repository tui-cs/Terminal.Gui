using System.IO.Abstractions;

// ReSharper disable AccessToDisposedClosure

namespace UICatalog.Scenarios;

[ScenarioMetadata ("FileDialog", "Demonstrates how to the FileDialog class")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Files and IO")]
public class FileDialogExamples : Scenario
{
    private CheckBox _cbAllowMultipleSelection;
    private CheckBox _cbAlwaysTableShowHeaders;
    private CheckBox _cbCaseSensitive;
    private CheckBox _cbDrivesOnlyInTree;
    private CheckBox _cbPreserveFilenameOnDirectoryChanges;
    private CheckBox _cbMustExist;
    private CheckBox _cbShowTreeBranchLines;
    private CheckBox _cbUseColors;
    private OptionSelector _osAllowedTypes;
    private OptionSelector _osCaption;
    private OptionSelector _osIcons;
    private OptionSelector _osOpenMode;
    private TextField _tbCancelButton;
    private TextField _tbOkButton;

    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();
        var y = 0;
        var x = 1;
        using Window win = new ();
        win.Title = GetQuitKeyAndName ();

        _cbMustExist = new CheckBox { Value = CheckState.Checked, Y = y++, X = x, Text = "Must E_xist" };
        win.Add (_cbMustExist);

        _cbUseColors = new CheckBox
        {
            Value = FileDialogStyle.DefaultUseColors ? CheckState.Checked : CheckState.UnChecked, Y = y++, X = x, Text = "_Use Colors"
        };
        win.Add (_cbUseColors);

        _cbCaseSensitive = new CheckBox { Value = CheckState.UnChecked, Y = y++, X = x, Text = "_Case Sensitive Search" };
        win.Add (_cbCaseSensitive);

        _cbAllowMultipleSelection = new CheckBox { Value = CheckState.UnChecked, Y = y++, X = x, Text = "_Multiple" };
        win.Add (_cbAllowMultipleSelection);

        _cbShowTreeBranchLines = new CheckBox { Value = CheckState.Checked, Y = y++, X = x, Text = "Tree Branch _Lines" };
        win.Add (_cbShowTreeBranchLines);

        _cbAlwaysTableShowHeaders = new CheckBox { Value = CheckState.Checked, Y = y++, X = x, Text = "Always Show _Headers" };
        win.Add (_cbAlwaysTableShowHeaders);

        _cbDrivesOnlyInTree = new CheckBox { Value = CheckState.UnChecked, Y = y++, X = x, Text = "Only Show _Drives" };
        win.Add (_cbDrivesOnlyInTree);

        _cbPreserveFilenameOnDirectoryChanges = new CheckBox { Value = CheckState.UnChecked, Y = y, X = x, Text = "Preserve Filename" };
        win.Add (_cbPreserveFilenameOnDirectoryChanges);

        y = 0;
        x = 24;

        win.Add (new Line { Orientation = Orientation.Vertical, X = x++, Y = 1, Height = 4 });
        win.Add (new Label { X = x++, Y = y++, Text = "Caption" });

        _osCaption = new OptionSelector { X = x, Y = y };
        _osCaption.Labels = [Strings.btnOk, Strings.cmdOpen, Strings.cmdSave];
        win.Add (_osCaption);

        y = 0;
        x = 34;

        win.Add (new Line { Orientation = Orientation.Vertical, X = x++, Y = 1, Height = 4 });
        win.Add (new Label { X = x++, Y = y++, Text = "OpenMode" });

        _osOpenMode = new OptionSelector { X = x, Y = y };
        _osOpenMode.Labels = [Strings.menuFile, "D_irectory", "_Mixed"];
        win.Add (_osOpenMode);

        y = 0;
        x = 48;

        win.Add (new Line { Orientation = Orientation.Vertical, X = x++, Y = 1, Height = 4 });
        win.Add (new Label { X = x++, Y = y++, Text = "Icons" });

        _osIcons = new OptionSelector { X = x, Y = y };
        _osIcons.Labels = ["_None", "_Unicode", "Nerd_*"];
        _osIcons.Value = 0;
        win.Add (_osIcons);

        Label label = new () { Y = Pos.AnchorEnd (), Text = "* Requires installing Nerd fonts:" };
        win.Add (label);
        Link link = new () { Y = Pos.Top (label), X = Pos.Right (label) + 1, Url = "https://github.com/devblackops/Terminal-Icons" };
        app.ToolTips!.SetToolTip (link, () => link.Url);
        win.Add (link);

        y = 5;
        x = 24;

        win.Add (new Line { Orientation = Orientation.Vertical, X = x++, Y = y + 1, Height = 4 });
        win.Add (new Label { X = x++, Y = y++, Text = "Allowed" });

        _osAllowedTypes = new OptionSelector { X = x, Y = y };
        _osAllowedTypes.Labels = ["An_y", "Cs_v (Recommended)", "Csv (S_trict)"];
        win.Add (_osAllowedTypes);

        y = 5;
        x = 45;

        win.Add (new Line { Orientation = Orientation.Vertical, X = x++, Y = y + 1, Height = 4 });
        win.Add (new Label { X = x++, Y = y++, Text = "Buttons" });

        win.Add (new Label { X = x, Y = y++, Text = "O_k Text:" });
        _tbOkButton = new TextField { X = x, Y = y++, Width = 12 };
        win.Add (_tbOkButton);
        win.Add (new Label { X = x, Y = y++, Text = "_Cancel Text:" });
        _tbCancelButton = new TextField { X = x, Y = y, Width = 12 };
        win.Add (_tbCancelButton);
        var btn = new Button { X = 1, Y = 9, IsDefault = true, Text = "Run Dialog" };

        win.CommandsToBubbleUp = [Command.Accept];

        win.Accepting += (_, e) =>
                         {
                             try
                             {
                                 CreateDialog (app);
                             }
                             catch (Exception ex)
                             {
                                 MessageBox.ErrorQuery (app, "Error", ex.ToString (), Strings.btnOk);
                             }
                             finally
                             {
                                 e.Handled = true;
                             }
                         };
        win.Add (btn);

        app.Run (win);
    }

    private void ConfirmOverwrite (object sender, FilesSelectedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace (e.Dialog.Path))
        {
            return;
        }

        if (!File.Exists (e.Dialog.Path))
        {
            return;
        }

        int? result = MessageBox.Query (e.Dialog.App!, "Overwrite?", "File already exists", Strings.btnNo, Strings.btnYes);
        e.Cancel = result is 0 or null;
    }

    private void CreateDialog (IApplication app)
    {
        if (_osOpenMode.Value is null)
        {
            return;
        }

        using FileDialog fd = new ();

        if (_osOpenMode.Labels is { })
        {
            fd.OpenMode = Enum.Parse<OpenMode> (_osOpenMode.Labels
                                                           .Select (l => TextFormatter.FindHotKey (l, _osOpenMode.HotKeySpecifier, out int hotPos, out Key _)

                                                                             // Remove the hotkey specifier at the found position
                                                                             ? TextFormatter.RemoveHotKeySpecifier (l, hotPos, _osOpenMode.HotKeySpecifier)

                                                                             // No hotkey found, return the label as is
                                                                             : l)
                                                           .ToArray () [_osOpenMode.Value.Value]);
            fd.MustExist = _cbMustExist.Value == CheckState.Checked;
            fd.AllowsMultipleSelection = _cbAllowMultipleSelection.Value == CheckState.Checked;

            if (_osCaption.Labels is { })
            {
                fd.Style.OkButtonText = _osCaption.Labels.ElementAt (_osCaption.Value!.Value);
            }
        }

        // If Save style dialog then give them an overwrite prompt
        if (_osCaption.Value == 2)
        {
            fd.FilesSelected += ConfirmOverwrite;
        }

        fd.Style.IconProvider.UseUnicodeCharacters = _osIcons.Value == 1;
        fd.Style.IconProvider.UseNerdIcons = _osIcons.Value == 2;

        if (_cbCaseSensitive.Value == CheckState.Checked)
        {
            fd.SearchMatcher = new CaseSensitiveSearchMatcher ();
        }

        fd.Style.UseColors = _cbUseColors.Value == CheckState.Checked;

        fd.Style.TableStyle?.AlwaysShowHeaders = _cbAlwaysTableShowHeaders.Value == CheckState.Checked;
        fd.Style.TreeStyle?.ShowBranchLines = _cbShowTreeBranchLines.Value == CheckState.Checked;

        IDirectoryInfoFactory dirInfoFactory = new FileSystem ().DirectoryInfo;

        if (_cbDrivesOnlyInTree.Value == CheckState.Checked)
        {
            fd.Style.TreeRootGetter = () => { return Environment.GetLogicalDrives ().ToDictionary (dirInfoFactory.New, k => k); };
        }

        fd.Style.PreserveFilenameOnDirectoryChanges = _cbPreserveFilenameOnDirectoryChanges.Value == CheckState.Checked;

        if (_osAllowedTypes.Value > 0)
        {
            fd.AllowedTypes.Add (new AllowedType ("Data File", ".csv", ".tsv"));

            if (_osAllowedTypes.Value == 1)
            {
                fd.AllowedTypes.Insert (1, new AllowedTypeAny ());
            }
        }

        if (!string.IsNullOrWhiteSpace (_tbOkButton.Text))
        {
            fd.Style.OkButtonText = _tbOkButton.Text;
        }

        if (!string.IsNullOrWhiteSpace (_tbCancelButton.Text))
        {
            fd.Style.CancelButtonText = _tbCancelButton.Text;
        }

        fd.Path = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);

        app.Run (fd);

        IReadOnlyList<string> multiSelected = fd.MultiSelected;
        string path = fd.Path;

        if (fd.Canceled)
        {
            MessageBox.Query (app, "Canceled", "You canceled navigation and did not pick anything", Strings.btnOk);
        }
        else if (_cbAllowMultipleSelection.Value == CheckState.Checked)
        {
            MessageBox.Query (app,
                              "Chosen!",
                              "You chose:" + Environment.NewLine + string.Join (Environment.NewLine, multiSelected.Select (m => m)),
                              Strings.btnOk);
        }
        else
        {
            MessageBox.Query (app, "Chosen!", "You chose:" + Environment.NewLine + path, Strings.btnOk);
        }
    }

    private class CaseSensitiveSearchMatcher : ISearchMatcher
    {
        private string _terms;
        public void Initialize (string terms) => _terms = terms;
        public bool IsMatch (IFileSystemInfo f) => f.Name.Contains (_terms, StringComparison.CurrentCulture);
    }
}
