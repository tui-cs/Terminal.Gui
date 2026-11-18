# Terminal.Gui Common Patterns Cookbook

> **Recipes for common UI patterns in Terminal.Gui applications.**

## Table of Contents

1. [Form with Validation](#form-with-validation)
2. [List with Selection](#list-with-selection)
3. [Menu Bar Application](#menu-bar-application)
4. [Split View Layout](#split-view-layout)
5. [Tab View](#tab-view)
6. [Progress Indicator](#progress-indicator)
7. [File Browser](#file-browser)
8. [Data Table](#data-table)
9. [Tree View](#tree-view)
10. [Status Bar with Shortcuts](#status-bar-with-shortcuts)
11. [Scrollable Content Container](#scrollable-content-container)
12. [Standard Application Layout](#standard-application-layout)

---

## Form with Validation

A typical form with labels, text fields, and validation.

```csharp
public sealed class FormWindow : Runnable<FormData?>
{
    public FormWindow ()
    {
        Title = "User Registration";
        Width = 50;
        Height = 15;

        Label nameLabel = new () { Text = "Name:", Y = 1 };
        TextField nameField = new () { X = 12, Y = 1, Width = Dim.Fill (1) };

        Label emailLabel = new () { Text = "Email:", Y = 3 };
        TextField emailField = new () { X = 12, Y = 3, Width = Dim.Fill (1) };

        Label ageLabel = new () { Text = "Age:", Y = 5 };
        NumericUpDown<int> ageField = new () { X = 12, Y = 5, Value = 18 };

        Label errorLabel = new ()
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill (1),
            SchemeName = "Error"
        };

        Button submitButton = new ()
        {
            Text = "Submit",
            X = Pos.Center (),
            Y = 9
        };

        submitButton.Accepting += (_, e) =>
        {
            // Validate
            if (string.IsNullOrWhiteSpace (nameField.Text))
            {
                errorLabel.Text = "Name is required";
                nameField.SetFocus ();
                e.Handled = true;
                return;
            }

            if (!emailField.Text.Contains ('@'))
            {
                errorLabel.Text = "Invalid email address";
                emailField.SetFocus ();
                e.Handled = true;
                return;
            }

        };

        submitButton.Accepted += (_, _) =>
        {
            Result = new FormData (nameField.Text, emailField.Text, ageField.Value);
            App!.RequestStop ();
        };

        Add (nameLabel, nameField, emailLabel, emailField, ageLabel, ageField, errorLabel, submitButton);
    }
}

public record FormData (string Name, string Email, int Age);
```

---

## List with Selection

A scrollable list with item selection and actions.

```csharp
using System.Collections.ObjectModel;

public sealed class ListWindow : Runnable
{
    private readonly ListView _listView;
    private readonly Label _detailLabel;
    private readonly ObservableCollection<string> _items = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];

    public ListWindow ()
    {
        Title = "Fruit Picker";

        _listView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent (50),
            Height = Dim.Fill (2),
            Source = new ListWrapper<string> (_items)
        };

        _detailLabel = new ()
        {
            X = Pos.Right (_listView) + 1,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (2),
            Text = "Select an item"
        };

        Button selectButton = new ()
        {
            Text = "Select",
            X = Pos.Center (),
            Y = Pos.AnchorEnd (1)
        };

        // ListView is IValue<int?> — the selected index.
        _listView.ValueChanged += (_, e) =>
        {
            if (e.NewValue is int index && index < _items.Count)
            {
                _detailLabel.Text = $"Selected: {_items [index]}";
            }
        };

        selectButton.Accepted += (_, _) =>
        {
            if (_listView.Value is int selected)
            {
                MessageBox.Query (App!, "Selection", $"You selected: {_items [selected]}", "OK");
            }
        };

        Add (_listView, _detailLabel, selectButton);
    }
}
```

---

## Menu Bar Application

Application with a menu bar and standard menu items.

```csharp
public sealed class MenuApp : Runnable
{
    public MenuApp ()
    {
        Title = "Menu Demo";

        MenuBar menuBar = new ();

        menuBar.Add (new MenuBarItem ("_File",
                                      [
                                          new MenuItem { Title = "_New", Key = Key.N.WithCtrl, Action = NewFile },
                                          new MenuItem { Title = "_Open...", Key = Key.O.WithCtrl, Action = OpenFile },
                                          new MenuItem { Title = "_Save", Key = Key.S.WithCtrl, Action = SaveFile },
                                          new Line (), // Separator
                                          new MenuItem { Title = "_Quit", Key = Key.Q.WithCtrl, Action = () => App!.RequestStop () }
                                      ]));

        menuBar.Add (new MenuBarItem ("_Help",
                                      [
                                          new MenuItem { Title = "_About...", Action = ShowAbout }
                                      ]));

        // Main content area below the menu bar. (For a real multi-line editor,
        // use tui-cs/Editor's EditorView — the core TextView is deprecated.)
        View content = new ()
        {
            X = 0,
            Y = Pos.Bottom (menuBar),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Add (menuBar, content);
    }

    private void NewFile () => MessageBox.Query (App!, "New", "New file created", "OK");
    private void OpenFile () { /* Show OpenDialog */ }
    private void SaveFile () { /* Show SaveDialog */ }
    private void ShowAbout () => MessageBox.Query (App!, "About", "Menu Demo v1.0", "OK");
}
```

---

## Split View Layout

Side-by-side panes positioned with `Pos`/`Dim`. (`TileView` does not exist in v2 — use plain views and `ViewArrangement` for user-resizable panes.)

```csharp
public sealed class SplitWindow : Runnable
{
    public SplitWindow ()
    {
        Title = "Split View Demo";

        FrameView leftPane = new ()
        {
            Title = "Left Pane",
            Width = Dim.Percent (50),
            Height = Dim.Fill (),

            // Let the user resize this pane with mouse or keyboard (optional)
            Arrangement = ViewArrangement.RightResizable
        };
        leftPane.Add (new Label { Text = "Left content", X = 1, Y = 1 });

        FrameView rightPane = new ()
        {
            Title = "Right Pane",
            X = Pos.Right (leftPane),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        rightPane.Add (new Label { Text = "Right content", X = 1, Y = 1 });

        Add (leftPane, rightPane);
    }
}
```

---

## Tab View

Tabbed interface with multiple content pages. (v1's `TabView` is now `Tabs`: each added `View` becomes a tab, and its `Title` is the tab label.)

```csharp
public sealed class TabbedWindow : Runnable
{
    public TabbedWindow ()
    {
        Title = "Tabbed Interface";

        Tabs tabs = new ();

        // Tab 1: Settings — Title becomes the tab label
        View settingsTab = new () { Title = "Settings" };
        settingsTab.Add (
            new Label { Text = "Enable Feature:", X = 1, Y = 1 },
            new CheckBox { X = 20, Y = 1, Text = "Enabled" }
        );

        // Tab 2: About
        View aboutTab = new () { Title = "About" };
        aboutTab.Add (new Label { Text = "Version 1.0.0", X = 1, Y = 1 });

        tabs.Add (settingsTab, aboutTab);

        // Tabs is IValue<View?> — set Value to switch tabs programmatically
        tabs.Value = settingsTab;

        Add (tabs);
    }
}
```

---

## Progress Indicator

Long-running operation with progress feedback.

```csharp
public sealed class ProgressWindow : Runnable
{
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private int _progress;

    public ProgressWindow ()
    {
        Title = "Processing...";
        Width = 50;
        Height = 8;

        _statusLabel = new ()
        {
            Text = "Starting...",
            X = Pos.Center (),
            Y = 1
        };

        _progressBar = new ()
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill (1),
            ProgressBarStyle = ProgressBarStyle.Continuous
        };

        Button cancelButton = new ()
        {
            Text = "Cancel",
            X = Pos.Center (),
            Y = 5
        };

        cancelButton.Accepted += (_, _) =>
        {
            App!.RequestStop ();
        };

        Add (_statusLabel, _progressBar, cancelButton);
    }

    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (!newIsRunning)
        {
            return;
        }

        // Start simulated work
        App!.AddTimeout (TimeSpan.FromMilliseconds (100), () =>
        {
            _progress += 5;
            _progressBar.Fraction = _progress / 100f;
            _statusLabel.Text = $"Processing... {_progress}%";

            if (_progress >= 100)
            {
                _statusLabel.Text = "Complete!";
                return false;  // Stop timer
            }
            return true;  // Continue timer
        });
    }
}
```

---

## File Browser

Open and save file dialogs.

```csharp
// Open single file
private void OpenFile ()
{
    OpenDialog dialog = new ()
    {
        Title = "Open File",
        AllowsMultipleSelection = false,
        Path = Environment.CurrentDirectory  // Starting directory
    };

    // Optional: filter by extension
    dialog.AllowedTypes = [new AllowedType ("Text Files", ".txt", ".md")];

    App!.Run (dialog);

    if (!dialog.Canceled && dialog.FilePaths.Any ())
    {
        string path = dialog.FilePaths.First ();
        // Process the file...
    }
}

// Save file
private void SaveFile ()
{
    SaveDialog dialog = new ()
    {
        Title = "Save File",
        Path = Environment.CurrentDirectory  // Starting directory
    };

    App!.Run (dialog);

    // FileName is the name portion of the chosen Path (null if canceled)
    if (!dialog.Canceled && !string.IsNullOrEmpty (dialog.FileName))
    {
        string path = dialog.Path;
        // Save to path...
    }
}

// Open multiple files
private void OpenMultipleFiles ()
{
    OpenDialog dialog = new ()
    {
        Title = "Select Files",
        AllowsMultipleSelection = true
    };

    App!.Run (dialog);

    if (!dialog.Canceled)
    {
        foreach (string path in dialog.FilePaths)
        {
            // Process each file...
        }
    }
}
```

---

## Data Table

Display tabular data with TableView.

```csharp
using System.Data;

public sealed class DataTableWindow : Runnable
{
    public DataTableWindow ()
    {
        Title = "Data Table";

        // Create DataTable
        DataTable table = new ();
        table.Columns.Add ("ID", typeof (int));
        table.Columns.Add ("Name", typeof (string));
        table.Columns.Add ("Price", typeof (decimal));
        table.Columns.Add ("InStock", typeof (bool));

        // Add rows
        table.Rows.Add (1, "Widget", 9.99m, true);
        table.Rows.Add (2, "Gadget", 19.99m, false);
        table.Rows.Add (3, "Doohickey", 4.99m, true);
        table.Rows.Add (4, "Thingamajig", 29.99m, true);

        TableView tableView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Table = new DataTableSource (table),
            FullRowSelect = true
        };

        Label statusLabel = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Text = "Select a row"
        };

        // TableView is IValue<TableSelection?> — ValueChanged fires when the selection moves.
        // SelectedCell is a Point: X = column, Y = row.
        tableView.ValueChanged += (_, e) =>
        {
            if (e.NewValue is not { } selection)
            {
                return;
            }

            int row = selection.SelectedCell.Y;

            if (row >= 0 && row < table.Rows.Count)
            {
                DataRow dataRow = table.Rows [row];
                statusLabel.Text = $"Selected: {dataRow ["Name"]} - ${dataRow ["Price"]}";
            }
        };

        Add (tableView, statusLabel);
    }
}
```

---

## Tree View

Hierarchical data display.

```csharp
using System.IO;

public sealed class TreeWindow : Runnable
{
    public TreeWindow ()
    {
        Title = "Tree View";

        TreeView<FileSystemInfo> treeView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Set up tree builder for file system
        treeView.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (
            GetChildren,
            CanExpand
        );

        // Add root nodes (drives or directories)
        DirectoryInfo root = new (Environment.CurrentDirectory);
        treeView.AddObject (root);

        treeView.SelectionChanged += (_, e) =>
        {
            if (e.NewValue is FileInfo file)
            {
                Title = $"Selected: {file.Name}";
            }
        };

        Add (treeView);
    }

    private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo parent)
    {
        if (parent is DirectoryInfo dir)
        {
            try
            {
                return dir.GetFileSystemInfos ();
            }
            catch
            {
                return [];
            }
        }
        return [];
    }

    private bool CanExpand (FileSystemInfo item) => item is DirectoryInfo;
}
```

---

## Status Bar with Shortcuts

`StatusBar` auto-positions at the bottom (`Y = Pos.AnchorEnd ()`, `Width = Dim.Fill ()`)
— **do NOT set `Y` or `Width` manually**. Content above it should use `Dim.Fill (1)` to
leave room.

For shortcuts that need to work app-wide (not just when focused), set `BindKeyToApplication = true`
and handle the `Activated` event or set `Action` for side effects.

```csharp
public sealed class StatusBarApp : Runnable
{
    public StatusBarApp ()
    {
        Title = "Status Bar Demo";

        // Main content area. (For a real multi-line editor, use tui-cs/Editor's
        // EditorView — the core TextView is deprecated.)
        View content = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (1)  // Leave 1 row for StatusBar
        };

        StatusBar statusBar = new ();
        // StatusBar auto-positions at bottom — do NOT set Y or Width

        Shortcut saveShortcut = new ()
        {
            Title = "Save",
            Key = Key.S.WithCtrl,
            BindKeyToApplication = true
        };

        saveShortcut.Activated += (_, _) =>
                                  {
                                      MessageBox.Query (App!, "Save", "Saving...", "OK");
                                  };

        Shortcut quitShortcut = new ()
        {
            Title = "Quit",
            Key = Key.Q.WithCtrl,
            BindKeyToApplication = true
        };

        quitShortcut.Activated += (_, _) =>
                                   {
                                       App?.RequestStop ();
                                   };

        statusBar.Add (saveShortcut, quitShortcut);
        Add (content, statusBar);
    }
}
```

---

## Scrollable Content Container

To make a `View` scrollable, set `ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar`.
**Do NOT create custom subclasses just for scrolling** — the built-in scrollbar handles mouse
wheel, drag, and viewport management automatically.

```csharp
// ✅ Correct — just set the flag on a plain View
View container = new ()
{
    Width = Dim.Fill (),
    Height = Dim.Fill (),
    CanFocus = true
};

container.ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

// Add subviews stacked vertically
View? previous = null;

foreach (View item in items)
{
    item.Y = previous is { } ? Pos.Bottom (previous) : 0;
    item.Width = Dim.Fill ();
    container.Add (item);
    previous = item;
}

// ❌ Wrong — do NOT create a custom subclass just for scrolling
// internal class ScrollableContainer : View { ... AddCommand(...) ... }
```

Only subclass (like `AllViewsView` does) when you need **additional** keyboard command
bindings (e.g., arrow keys for line-by-line scrolling, PageUp/PageDown, Home/End) beyond
what the scrollbar provides. Even then, prefer adding bindings in the owning `Runnable`
if possible.

---

## Tips for All Patterns

1. **Use `-ed` events (`Accepted`) for side effects**; use `Accepting` + `e.Handled = true` only to inspect or cancel
2. **Use `Dim.Fill()` and `Pos.Center()`** instead of hardcoded values
3. **Call `App!.RequestStop()`** to close the current window
4. **Use `MessageBox.Query`** for simple dialogs
5. **Dispose resources properly** when the window closes
