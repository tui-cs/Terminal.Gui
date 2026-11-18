#nullable enable
using System.Collections.ObjectModel;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for <see cref="TreeView{T}"/> settings and actions.
/// </summary>
public sealed class TreeViewEditor : EditorBase
{
    private CheckBox? _cbMultiSelect;
    private CheckBox? _cbCheckboxMode;
    private CheckBox? _cbShowBranchLines;
    private CheckBox? _cbColorExpandSymbol;
    private CheckBox? _cbInvertExpandSymbolColors;
    private CheckBox? _cbHighlightModelTextOnly;
    private CheckBox? _cbAllowLetterBasedNavigation;
    private DropDownList? _ddlSchemeName;

    public TreeViewEditor ()
    {
        Title = "TreeViewEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += TreeViewEditor_Initialized;
    }

    protected override void OnViewToEditChanged ()
    {
        base.OnViewToEditChanged ();

        foreach (View subview in SubViews)
        {
            subview.Enabled = ViewToEdit is { };
        }

        if (ViewToEdit is null)
        {
            return;
        }

        UpdatingLayoutSettings = true;
        SyncFromView ();
        UpdatingLayoutSettings = false;
    }

    /// <inheritdoc/>
    protected override void OnUpdateLayoutSettings ()
    {
        base.OnUpdateLayoutSettings ();

        if (ViewToEdit is null)
        {
            return;
        }

        UpdatingLayoutSettings = true;
        SyncFromView ();
        UpdatingLayoutSettings = false;
    }

    private void SyncFromView ()
    {
        if (ViewToEdit is not ITreeView treeView)
        {
            return;
        }

        _cbMultiSelect!.Value = treeView.MultiSelect ? CheckState.Checked : CheckState.UnChecked;
        _cbCheckboxMode!.Value = treeView.CheckboxMode ? CheckState.Checked : CheckState.UnChecked;
        _cbShowBranchLines!.Value = treeView.Style.ShowBranchLines ? CheckState.Checked : CheckState.UnChecked;
        _cbColorExpandSymbol!.Value = treeView.Style.ColorExpandSymbol ? CheckState.Checked : CheckState.UnChecked;
        _cbInvertExpandSymbolColors!.Value = treeView.Style.InvertExpandSymbolColors ? CheckState.Checked : CheckState.UnChecked;
        _cbHighlightModelTextOnly!.Value = treeView.Style.HighlightModelTextOnly ? CheckState.Checked : CheckState.UnChecked;
        _cbAllowLetterBasedNavigation!.Value = treeView.AllowLetterBasedNavigation ? CheckState.Checked : CheckState.UnChecked;
        _ddlSchemeName!.Value = ViewToEdit.SchemeName ?? "";
    }

    private void TreeViewEditor_Initialized (object? s, EventArgs e)
    {
        // ── TreeStyle toggles ──

        _cbMultiSelect = new CheckBox { Title = "MultiSelect", CanFocus = true };
        _cbMultiSelect.ValueChanging += (_, args) => SetBool (args, (tv, v) => tv.MultiSelect = v);

        _cbCheckboxMode = new CheckBox { X = Pos.Right (_cbMultiSelect) + 1, Title = "CheckboxMode", CanFocus = true };
        _cbCheckboxMode.ValueChanging += (_, args) => SetBool (args, (tv, v) => tv.CheckboxMode = v);

        _cbShowBranchLines = new CheckBox { Y = Pos.Bottom (_cbMultiSelect), Title = "ShowBranchLines", CanFocus = true };
        _cbShowBranchLines.ValueChanging += (_, args) => SetStyleBool (args, (style, v) => style.ShowBranchLines = v);

        _cbColorExpandSymbol = new CheckBox { X = Pos.Right (_cbCheckboxMode) + 1, Title = "ColorExpandSymbol", CanFocus = true };
        _cbColorExpandSymbol.ValueChanging += (_, args) => SetStyleBool (args, (style, v) => style.ColorExpandSymbol = v);

        _cbInvertExpandSymbolColors = new CheckBox
        {
            X = Pos.Right (_cbShowBranchLines) + 1, Y = Pos.Top (_cbShowBranchLines), Title = "InvertExpandSymbolColors", CanFocus = true
        };
        _cbInvertExpandSymbolColors.ValueChanging += (_, args) => SetStyleBool (args, (style, v) => style.InvertExpandSymbolColors = v);

        _cbHighlightModelTextOnly = new CheckBox { Y = Pos.Bottom (_cbShowBranchLines), Title = "HighlightModelTextOnly", CanFocus = true };
        _cbHighlightModelTextOnly.ValueChanging += (_, args) => SetStyleBool (args, (style, v) => style.HighlightModelTextOnly = v);

        _cbAllowLetterBasedNavigation = new CheckBox
        {
            X = Pos.Right (_cbHighlightModelTextOnly) + 1, Y = Pos.Top (_cbHighlightModelTextOnly), Title = "AllowLetterBasedNavigation", CanFocus = true
        };
        _cbAllowLetterBasedNavigation.ValueChanging += (_, args) => SetBool (args, (tv, v) => tv.AllowLetterBasedNavigation = v);

        // ── SchemeName dropdown ──

        Label lblScheme = new () { Y = Pos.Bottom (_cbHighlightModelTextOnly), Title = "Scheme:" };

        ObservableCollection<string> schemeNames = new (SchemeManager.GetSchemeNames ());

        _ddlSchemeName = new DropDownList
        {
            X = Pos.Right (lblScheme) + 1,
            Y = Pos.Top (lblScheme),
            Width = 20,
            Source = new ListWrapper<string> (schemeNames),
            ReadOnly = true
        };

        _ddlSchemeName.ValueChanged += (_, args) =>
                                       {
                                           if (UpdatingLayoutSettings || ViewToEdit is null)
                                           {
                                               return;
                                           }

                                           if (!string.IsNullOrEmpty (args.NewValue))
                                           {
                                               ViewToEdit.SchemeName = args.NewValue;
                                           }
                                       };

        // ── Action buttons (selected item) ──

        Button btnExpand = new () { Y = Pos.Bottom (lblScheme) + 1, Text = "Expand" };

        btnExpand.Accepting += (_, _) =>
                               {
                                   if (ViewToEdit is ITreeView treeView)
                                   {
                                       treeView.Expand ();
                                   }
                               };

        Button btnCollapse = new () { X = Pos.Right (btnExpand) + 1, Y = Pos.Top (btnExpand), Text = "Collapse" };

        btnCollapse.Accepting += (_, _) =>
                                 {
                                     if (ViewToEdit is ITreeView treeView)
                                     {
                                         treeView.Collapse ();
                                     }
                                 };

        Button btnToggle = new () { X = Pos.Right (btnCollapse) + 1, Y = Pos.Top (btnExpand), Text = "Toggle" };

        btnToggle.Accepting += (_, _) => ViewToEdit?.InvokeCommand (Command.Toggle);

        // ── Action buttons (all) ──

        Button btnExpandAll = new () { X = Pos.Right (btnToggle) + 1, Y = Pos.Top (btnExpand), Text = "Expand All" };

        btnExpandAll.Accepting += (_, _) =>
                                  {
                                      if (ViewToEdit is ITreeView treeView)
                                      {
                                          treeView.ExpandAll ();
                                      }
                                  };

        Button btnCollapseAll = new () { X = Pos.Right (btnExpandAll) + 1, Y = Pos.Top (btnExpand), Text = "Collapse All" };

        btnCollapseAll.Accepting += (_, _) =>
                                    {
                                        if (ViewToEdit is ITreeView treeView)
                                        {
                                            treeView.CollapseAll ();
                                        }
                                    };

        Button btnRebuild = new () { X = Pos.Right (btnCollapseAll) + 1, Y = Pos.Top (btnExpand), Text = "Rebuild" };

        btnRebuild.Accepting += (_, _) =>
                                {
                                    if (ViewToEdit is ITreeView treeView)
                                    {
                                        treeView.RebuildTree ();
                                    }
                                };

        Add (_cbMultiSelect,
             _cbCheckboxMode,
             _cbColorExpandSymbol,
             _cbShowBranchLines,
             _cbInvertExpandSymbolColors,
             _cbHighlightModelTextOnly,
             _cbAllowLetterBasedNavigation,
             lblScheme,
             _ddlSchemeName,
             btnExpand,
             btnCollapse,
             btnToggle,
             btnExpandAll,
             btnCollapseAll,
             btnRebuild);
    }

    private void SetBool (ValueChangingEventArgs<CheckState> args, Action<ITreeView, bool> setter)
    {
        if (UpdatingLayoutSettings || ViewToEdit is not ITreeView treeView)
        {
            return;
        }

        setter (treeView, args.NewValue == CheckState.Checked);
        ViewToEdit!.SetNeedsDraw ();
    }

    private void SetStyleBool (ValueChangingEventArgs<CheckState> args, Action<TreeStyle, bool> setter)
    {
        if (UpdatingLayoutSettings || ViewToEdit is not ITreeView treeView)
        {
            return;
        }

        setter (treeView.Style, args.NewValue == CheckState.Checked);
        ViewToEdit!.SetNeedsDraw ();
    }
}
