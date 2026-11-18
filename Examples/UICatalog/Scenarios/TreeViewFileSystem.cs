#nullable enable

using System.IO.Abstractions;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("File System Explorer", "Hierarchical file system explorer demonstrating TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
[ScenarioCategory ("Files and IO")]
public class TreeViewFileSystem : Scenario
{
    private readonly FileSystemIconProvider _iconProvider = new ();
    private DetailsFrame? _detailsFrame;
    private CheckBox? _miArrowSymbolsCheckBox;
    private CheckBox? _miBasicIconsCheckBox;
    private CheckBox? _miColoredSymbolsCheckBox;
    private CheckBox? _miCursorCheckBox;
    private CheckBox? _miCustomColorsCheckBox;
    private CheckBox? _miFullPathsCheckBox;
    private CheckBox? _miHighlightModelTextOnlyCheckBox;
    private CheckBox? _miInvertSymbolsCheckBox;
    private CheckBox? _miMultiSelectCheckBox;
    private CheckBox? _miNerdIconsCheckBox;
    private CheckBox? _miNoSymbolsCheckBox;
    private CheckBox? _miPlusMinusCheckBox;
    private CheckBox? _miShowLinesCheckBox;
    private CheckBox? _miUnicodeIconsCheckBox;

    /// <summary>A tree view where nodes are files and folders</summary>
    private TreeView<IFileSystemInfo>? _treeViewFiles;

    public override void Main ()
    {

        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable win = new ();
        win.Title = GetName ();

        // MenuBar
        MenuBar menu = new ();

        _treeViewFiles = new TreeView<IFileSystemInfo>
        {
            Y = Pos.Bottom (menu),
            Height = Dim.Fill (),
        };
        _treeViewFiles.DrawLine += TreeViewFiles_DrawLine;

        _detailsFrame = new DetailsFrame (_iconProvider)
        {
            X = Pos.AnchorEnd (), Y = Pos.Bottom (menu), Width = 50, Height = Dim.Fill ()
        };
        _treeViewFiles.Width = Dim.Fill (_detailsFrame);

        win.Add (menu, _treeViewFiles, _detailsFrame);

        _treeViewFiles.Activating += TreeViewFiles_Activating;
        _treeViewFiles.KeyDown += TreeViewFiles_KeyPress;
        _treeViewFiles.SelectionChanged += TreeViewFiles_SelectionChanged;

        SetupFileTree ();

        // Setup menu checkboxes
        _miFullPathsCheckBox = new CheckBox { Title = "_Full Paths" };
        _miFullPathsCheckBox.ValueChanged += (_, _) => SetFullName ();

        _miMultiSelectCheckBox = new CheckBox
        {
            Title = "_Multi Select"
        };
        _miMultiSelectCheckBox.ValueChanged += (_, _) => SetMultiSelect ();

        _miShowLinesCheckBox = new CheckBox { Title = "_Show Lines", Value = CheckState.Checked };
        _miShowLinesCheckBox.ValueChanged += (_, _) => ShowLines ();

        _miPlusMinusCheckBox = new CheckBox { Title = "_Plus Minus Symbols", Value = CheckState.UnChecked };
        _miPlusMinusCheckBox.ValueChanged += (_, _) => SetExpandableSymbols ((Rune)'+', (Rune)'-');

        _miArrowSymbolsCheckBox = new CheckBox { Title = "_Arrow Symbols" };
        _miArrowSymbolsCheckBox.ValueChanged += (_, _) => SetExpandableSymbols ((Rune)'>', (Rune)'v');

        _miNoSymbolsCheckBox = new CheckBox { Title = "_No Symbols" };
        _miNoSymbolsCheckBox.ValueChanged += (_, _) => SetExpandableSymbols (default (Rune), null);

        _miColoredSymbolsCheckBox = new CheckBox { Title = "_Colored Symbols" };
        _miColoredSymbolsCheckBox.ValueChanged += (_, _) => ShowColoredExpandableSymbols ();

        _miInvertSymbolsCheckBox = new CheckBox { Title = "_Invert Symbols" };
        _miInvertSymbolsCheckBox.ValueChanged += (_, _) => InvertExpandableSymbols ();

        _miBasicIconsCheckBox = new CheckBox { Title = "_Basic Icons" };
        _miBasicIconsCheckBox.ValueChanged += (_, _) => SetNoIcons ();

        _miUnicodeIconsCheckBox = new CheckBox { Title = "_Unicode Icons" };
        _miUnicodeIconsCheckBox.ValueChanged += (_, _) => SetUnicodeIcons ();

        _miNerdIconsCheckBox = new CheckBox { Title = "_Nerd Icons" };
        _miNerdIconsCheckBox.ValueChanged += (_, _) => SetNerdIcons ();

        _miHighlightModelTextOnlyCheckBox = new CheckBox { Title = "_Highlight Model Text Only", Value = CheckState.Checked };
        SetCheckHighlightModelTextOnly ();
        _miHighlightModelTextOnlyCheckBox.ValueChanged += (_, _) => SetCheckHighlightModelTextOnly ();

        _miCustomColorsCheckBox = new CheckBox { Title = "C_ustom Colors Hidden Files" };
        _miCustomColorsCheckBox.ValueChanged += (_, _) => SetCustomColors ();

        _miCursorCheckBox = new CheckBox
        {
            Title = "Curs_or"

            //CheckedState = CheckState.Checked
        };
        SetCursor ();
        _miCursorCheckBox.ValueChanged += (_, _) => SetCursor ();

        menu.Add (new MenuBarItem (Strings.menuFile,
                                   [new MenuItem { Title = Strings.cmdQuit, Key = Application.GetDefaultKey (Command.Quit), Action = Quit }]));

        menu.Add (new MenuBarItem ("_View", [new MenuItem { CommandView = _miFullPathsCheckBox }, new MenuItem { CommandView = _miMultiSelectCheckBox }]));

        menu.Add (new MenuBarItem ("_Style",
                                   [
                                       new MenuItem { CommandView = _miShowLinesCheckBox },
                                       new MenuItem { CommandView = _miPlusMinusCheckBox },
                                       new MenuItem { CommandView = _miArrowSymbolsCheckBox },
                                       new MenuItem { CommandView = _miNoSymbolsCheckBox },
                                       new MenuItem { CommandView = _miColoredSymbolsCheckBox },
                                       new MenuItem { CommandView = _miInvertSymbolsCheckBox },
                                       new MenuItem { CommandView = _miBasicIconsCheckBox },
                                       new MenuItem { CommandView = _miUnicodeIconsCheckBox },
                                       new MenuItem { CommandView = _miNerdIconsCheckBox },
                                       new MenuItem { CommandView = _miHighlightModelTextOnlyCheckBox },
                                       new MenuItem { CommandView = _miCustomColorsCheckBox },
                                       new MenuItem { CommandView = _miCursorCheckBox }
                                   ]));

        SetNerdIcons ();

        _treeViewFiles.GoToFirst ();
        _treeViewFiles.Expand ();
        _treeViewFiles.SetFocus ();

        UpdateIconCheckState ();

        app.Run (win);
    }

    private string AspectGetter (IFileSystemInfo f) => (_iconProvider.GetIconWithOptionalSpace (f) + f.Name).Trim ();

    private void InvertExpandableSymbols ()
    {
        if (_treeViewFiles is null || _miInvertSymbolsCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.InvertExpandSymbolColors = _miInvertSymbolsCheckBox.Value == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void Quit () => _treeViewFiles?.App?.RequestStop ();

    private void SetCheckHighlightModelTextOnly ()
    {
        if (_treeViewFiles is null || _miHighlightModelTextOnlyCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.HighlightModelTextOnly = _miHighlightModelTextOnlyCheckBox.Value == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetCursor ()
    {
        if (_treeViewFiles is null || _miCursorCheckBox is null)
        {
            return;
        }

        if (_miCursorCheckBox.Value == CheckState.Checked)
        {
            // Provide a non-null position to enable the cursor
            _treeViewFiles.Cursor = _treeViewFiles.Cursor with { Position = Point.Empty, Style = CursorStyle.BlinkingBlock };
        }
        else
        {
            _treeViewFiles.Cursor = _treeViewFiles.Cursor with { Position = null };
        }
    }

    private void SetCustomColors ()
    {
        if (_treeViewFiles is null || _miCustomColorsCheckBox is null)
        {
            return;
        }

        if (_miCustomColorsCheckBox.Value == CheckState.Checked)
        {
            _treeViewFiles.ColorGetter = m =>
            {
                if ((m is IDirectoryInfo && m.Attributes.HasFlag (FileAttributes.Hidden)) || (m is IFileInfo && m.Attributes.HasFlag (FileAttributes.Hidden)))
                {
                    return new Scheme
                    {
                        Focus = new Attribute (Color.BrightRed,
                                               _treeViewFiles.GetAttributeForRole (VisualRole.Focus).Background),
                        Normal = new Attribute (Color.BrightYellow,
                                                _treeViewFiles.GetAttributeForRole (VisualRole.Normal).Background)
                    };
                }

                return null;
            };
        }
        else
        {
            _treeViewFiles.ColorGetter = null;
        }

        _treeViewFiles.SetNeedsDraw ();
    }

    private bool _settingExpandableSymbols;

    private void SetExpandableSymbols (Rune expand, Rune? collapse)
    {
        if (_treeViewFiles is null || _settingExpandableSymbols)
        {
            return;
        }

        _settingExpandableSymbols = true;
        _miPlusMinusCheckBox?.Value = expand.Value == '+' ? CheckState.Checked : CheckState.UnChecked;
        _miArrowSymbolsCheckBox?.Value = expand.Value == '>' ? CheckState.Checked : CheckState.UnChecked;
        _miNoSymbolsCheckBox?.Value = expand.Value == 0 ? CheckState.Checked : CheckState.UnChecked;
        _settingExpandableSymbols = false;

        _treeViewFiles.Style.ExpandableSymbol = expand;
        _treeViewFiles.Style.CollapseableSymbol = collapse;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetFullName ()
    {
        if (_treeViewFiles is null || _miFullPathsCheckBox is null)
        {
            return;
        }

        if (_miFullPathsCheckBox.Value == CheckState.Checked)
        {
            _treeViewFiles.AspectGetter = f => f.FullName;
        }
        else
        {
            _treeViewFiles.AspectGetter = f => f.Name;
        }

        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetMultiSelect ()
    {
        if (_treeViewFiles is null || _miMultiSelectCheckBox is null)
        {
            return;
        }

        _treeViewFiles.MultiSelect = _miMultiSelectCheckBox.Value == CheckState.Checked;
    }

    private bool _settingIcons;

    private void SetNerdIcons ()
    {
        if (_settingIcons)
        {
            return;
        }
        _iconProvider.UseNerdIcons = true;
        _settingIcons = true;
        UpdateIconCheckState ();
        _settingIcons = false;
    }

    private void SetNoIcons ()
    {
        if (_settingIcons)
        {
            return;
        }

        _iconProvider.UseUnicodeCharacters = false;
        _iconProvider.UseNerdIcons = false;
        _settingIcons = true;
        UpdateIconCheckState ();
        _settingIcons = false;
    }

    private void SetUnicodeIcons ()
    {
        if (_settingIcons)
        {
            return;
        }

        _iconProvider.UseUnicodeCharacters = true;
        _settingIcons = true;
        UpdateIconCheckState ();
        _settingIcons = false;
    }

    private void SetupFileTree ()
    {
        if (_treeViewFiles is null)
        {
            return;
        }

        // setup how to build tree
        FileSystem fs = new ();

        IEnumerable<IDirectoryInfo> rootDirs = DriveInfo.GetDrives ().Select (d => fs.DirectoryInfo.New (d.RootDirectory.FullName));
        _treeViewFiles.TreeBuilder = new FileSystemTreeBuilder ();
        _treeViewFiles.AddObjects (rootDirs);

        // Determines how to represent objects as strings on the screen
        _treeViewFiles.AspectGetter = AspectGetter;

        _iconProvider.IsOpenGetter = _treeViewFiles.IsExpanded;
    }

    private void ShowColoredExpandableSymbols ()
    {
        if (_treeViewFiles is null || _miColoredSymbolsCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.ColorExpandSymbol = _miColoredSymbolsCheckBox.Value == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void ShowContextMenu (Point screenPoint, IFileSystemInfo forObject)
    {
        PopoverMenu contextMenu = new ([new MenuItem ("Properties", $"Show {forObject.Name} properties", () => ShowPropertiesOf (forObject))]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        _detailsFrame?.App?.Popovers?.Register (contextMenu);

        _detailsFrame?.App?.Invoke (() => contextMenu.MakeVisible (screenPoint));
    }

    private void ShowLines ()
    {
        if (_treeViewFiles is null || _miShowLinesCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.ShowBranchLines = _miShowLinesCheckBox.Value == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void ShowPropertiesOf (IFileSystemInfo? fileSystemInfo) => _detailsFrame?.FileInfo = fileSystemInfo;

    private void TreeViewFiles_DrawLine (object? sender, DrawTreeViewLineEventArgs<IFileSystemInfo> e)
    {
        // Render directory icons in yellow
        if (e.Model is not IDirectoryInfo)
        {
            return;
        }

        if (_iconProvider is { UseNerdIcons: false, UseUnicodeCharacters: false })
        {
            return;
        }

        switch (e.Cells)
        {
            case { } when e.IndexOfModelText <= 0:
            case { } when e.IndexOfModelText >= e.Cells.Count:
                return;

            case { }:
                {
                    Cell cell = e.Cells [e.IndexOfModelText];

                    cell.Attribute = new Attribute (Color.BrightYellow, cell.Attribute!.Value.Background, cell.Attribute!.Value.Style);

                    break;
                }
        }
    }

    private void TreeViewFiles_KeyPress (object? sender, Key obj)
    {
        if (_treeViewFiles is null)
        {
            return;
        }

        if (obj.KeyCode != (KeyCode.R | KeyCode.CtrlMask))
        {
            return;
        }
        IFileSystemInfo? selected = _treeViewFiles.SelectedObject;

        // nothing is selected
        if (selected is null)
        {
            return;
        }

        int? location = _treeViewFiles.GetObjectRow (selected);

        //selected object is offscreen or somehow not found
        if (location is null || location < 0 || location > _treeViewFiles.Frame.Height)
        {
            return;
        }

        ShowContextMenu (new Point (5 + _treeViewFiles.Frame.X, location.Value + _treeViewFiles.Frame.Y + 2), selected);
    }

    private void TreeViewFiles_Activating (object? sender, CommandEventArgs e)
    {
        if (_treeViewFiles is null)
        {
            return;
        }

        // Only handle mouse clicks
        if (e.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return;
        }

        // if user right clicks
        if (!mouse.Flags.HasFlag (MouseFlags.RightButtonClicked))
        {
            return;
        }
        IFileSystemInfo? rightClicked = _treeViewFiles.GetObjectOnRow (mouse.Position!.Value.Y);

        // nothing was clicked
        if (rightClicked is null)
        {
            return;
        }

        ShowContextMenu (new Point (mouse.Position!.Value.X + _treeViewFiles.Frame.X, mouse.Position!.Value.Y + _treeViewFiles.Frame.Y + 2), rightClicked);
    }

    private void TreeViewFiles_SelectionChanged (object? sender, SelectionChangedEventArgs<IFileSystemInfo> e) => ShowPropertiesOf (e.NewValue);

    private void UpdateIconCheckState ()
    {
        _miBasicIconsCheckBox?.Value = _iconProvider is { UseNerdIcons: false, UseUnicodeCharacters: false } ? CheckState.Checked : CheckState.UnChecked;

        _miUnicodeIconsCheckBox?.Value = _iconProvider.UseUnicodeCharacters ? CheckState.Checked : CheckState.UnChecked;

        _miNerdIconsCheckBox?.Value = _iconProvider.UseNerdIcons ? CheckState.Checked : CheckState.UnChecked;

        _treeViewFiles?.SetNeedsDraw ();
    }

    private class DetailsFrame : FrameView
    {
        private readonly FileSystemIconProvider _iconProvider;

        public DetailsFrame (FileSystemIconProvider iconProvider)
        {
            Title = "Details";
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
            _iconProvider = iconProvider;
        }

        public IFileSystemInfo? FileInfo
        {
            set
            {
                field = value;
                StringBuilder? sb = null;

                try
                {
                    switch (field)
                    {
                        case IFileInfo f:
                            Title = $"{_iconProvider.GetIconWithOptionalSpace (f)}{f.Name}".Trim ();
                            sb = new StringBuilder ();
                            sb.AppendLine ($"Path:\n {f.FullName}\n");
                            sb.AppendLine ($"Size:\n {f.Length:N0} bytes\n");
                            sb.AppendLine ($"Modified:\n {f.LastWriteTime}\n");
                            sb.AppendLine ($"Created:\n {f.CreationTime}");

                            break;

                        case IDirectoryInfo dir:
                            Title = $"{_iconProvider.GetIconWithOptionalSpace (dir)}{dir.Name}".Trim ();
                            sb = new StringBuilder ();
                            sb.AppendLine ($"Path:\n {dir.FullName}\n");
                            sb.AppendLine ($"Modified:\n {dir.LastWriteTime}\n");
                            sb.AppendLine ($"Created:\n {dir.CreationTime}\n");

                            break;
                    }
                }
                catch (IOException ioe)
                {
                    if (field is IFileInfo f)
                    {
                        Title = $"{_iconProvider.GetIconWithOptionalSpace (f)}{f.Name}".Trim ();
                    }

                    if (field is IDirectoryInfo dir)
                    {
                        Title = $"{_iconProvider.GetIconWithOptionalSpace (dir)}{dir.Name}".Trim ();
                    }

                    sb = new StringBuilder ();
                    sb.AppendLine ($"Path:\n {field?.FullName}\n");
                    sb.AppendLine ($"Exception:\n {ioe.Message}");
                }

                Text = sb?.ToString () ?? string.Empty;
            }
        }
    }
}
