# TableView Deep Dive

[TableView](~/api/Terminal.Gui.Views.TableView.yml) displays infinitely-sized tabular data from any [ITableSource](~/api/Terminal.Gui.Views.ITableSource.yml) and supports keyboard/mouse navigation, multi-cell selection, column styling, and checkbox columns.

## Table of Contents

- [Data Sources](#data-sources)
- [Selection Model](#selection-model)
- [Key & Mouse Bindings](#key--mouse-bindings)
- [Rendering & Scrolling](#rendering--scrolling)
- [Column Styling](#column-styling)
- [Checkbox Columns](#checkbox-columns)
- [Tree Tables](#tree-tables)
- [Events](#events)

---

## Data Sources

TableView does **not** own data. Assign an `ITableSource` to the `Table` property.

### ITableSource

The core interface. Implement it to bridge any data model into a TableView:

```csharp
public interface ITableSource
{
    int Rows { get; }
    int Columns { get; }
    string [] ColumnNames { get; }
    object this [int row, int col] { get; }
}
```

### Built-in Implementations

| Class | Use Case |
|-------|----------|
| `DataTableSource` | Wraps a `System.Data.DataTable` |
| `EnumerableTableSource<T>` | Projects a collection of objects into columns via lambdas |
| `ListTableSource` | Wraps an `IList` into a multi-column layout |
| `TreeTableSource<T>` | Adds expand/collapse tree behavior to rows |

### DataTable Example

```csharp
DataTable dt = new ();
dt.Columns.Add ("Name");
dt.Columns.Add ("Age", typeof (int));
dt.Rows.Add ("Alice", 30);
dt.Rows.Add ("Bob", 25);

TableView tv = new () { Table = new DataTableSource (dt) };
```

### Object Collection Example

```csharp
TableView tv = new ()
{
    Table = new EnumerableTableSource<Process> (
        Process.GetProcesses (),
        new Dictionary<string, Func<Process, object>> ()
        {
            { "ID", p => p.Id },
            { "Name", p => p.ProcessName },
            { "Threads", p => p.Threads.Count },
        })
};
```

### CSV Example

```csharp
DataTable dt = new ();
string [] lines = File.ReadAllLines (filename);

foreach (string h in lines [0].Split (','))
{
    dt.Columns.Add (h);
}

foreach (string line in lines.Skip (1))
{
    dt.Rows.Add (line.Split (','));
}

TableView tv = new () { Table = new DataTableSource (dt) };
```

---

## Selection Model

TableView implements `IValue<TableSelection?>` to expose the complete selection state as a single value.

### Key Types

| Type | Description |
|------|-------------|
| `TableSelection` | Immutable snapshot: `SelectedCell` (a `Point`) + `Regions` (an `IReadOnlyList<TableSelectionRegion>`) |
| `TableSelectionRegion` | A contiguous rectangular selection. Has `Origin`, `Rectangle`, and `IsExtended` |
| `Value` property | The current `TableSelection?`. `null` means no table is set or selection was cleared |

### SelectedCell

The selected cell is the active cell — the anchor for navigation. Access it via `Value.SelectedCell` (`Point` where `X` = column index, `Y` = row index).

Move the cursor programmatically with `SetSelection (col, row, extend)`.

### Multi-Selection

When `MultiSelect` is `true` (the default), users can create rectangular selection regions:

- **Shift+Arrow** — extends a region from the cursor to the new position
- **Ctrl+Click** — unions the clicked cell as an independent extended selection
- **Space** (`Command.ToggleExtend`) — toggles the current cell's `IsExtended` state
- **Ctrl+A** — selects all cells

Extended regions (`IsExtended = true`) persist through keyboard navigation. Non-extended regions are cleared on the next cursor move.

### FullRowSelect

When `FullRowSelect` is `true`, entire rows are selected instead of individual cells. All cells in the cursor's row are reported as selected by `GetAllSelectedCells ()` and `IsSelected ()`.

> **Tip — Home/End with FullRowSelect:** By default, `Home` and `End` navigate to the
> start/end of the current *row* (i.e. first/last column). To make `Home`/`End` jump to
> the first/last *row* instead (which is often more useful in full-row mode), rebind them
> to `Command.Start` and `Command.End`:
>
> ```csharp
> tableView.KeyBindings.Remove (Key.Home);
> tableView.KeyBindings.Add (Key.Home, Command.Start);
> tableView.KeyBindings.Remove (Key.End);
> tableView.KeyBindings.Add (Key.End, Command.End);
> ```
>
> This is the pattern used by `UICatalogRunnable` for its scenario list.

### Reading the Selection

```csharp
// Selected cell position
Point selectedCell = tv.Value!.SelectedCell; // (col, row)

// All selected cell coordinates
IEnumerable<Point> cells = tv.GetAllSelectedCells ();

// Check if a specific cell is selected
bool sel = tv.IsSelected (col, row);
```

---

## Key & Mouse Bindings

### Default Key Bindings

| Key | Command |
|-----|---------|
| Arrow keys | Move cursor one cell |
| Shift+Arrow | Extend selection |
| PageUp / PageDown | Move one page |
| Home / End | Move to start/end of row |
| Ctrl+Home / Ctrl+End | Move to first/last row |
| Shift+Home/End/Ctrl+Home/Ctrl+End | Extend selection to row/table boundary |
| Ctrl+A | Select all |
| Space | `Command.ToggleExtend` — toggle current cell's extended selection |

### Default Mouse Bindings

| Mouse Event | Command |
|-------------|---------|
| Click | `Command.Activate` — moves cursor to clicked cell |
| Ctrl+Click | `Command.ToggleExtend` — unions clicked cell into selection |
| Alt+Click | `Command.ToggleExtend` — extends rectangular region to clicked cell |
| Double-click | `Command.Accept` |
| Scroll wheel | Scroll up/down/left/right |

### Customizing Bindings

TableView uses the standard `KeyBindings` and `MouseBindings` infrastructure. Override `DefaultKeyBindings` (static) or instance-level bindings.

---

## Rendering & Scrolling

TableView renders only the visible portion of the table. Horizontal and vertical scrolling is handled via `ColumnOffset` and `RowOffset` (backed by `Viewport`).

### Table Rendering Model

1. **Header** — column names with optional overline, underline, and vertical separators (controlled by `TableStyle`)
2. **Data rows** — rendered from `RowOffset` until viewport is filled
3. **Columns** — rendered from `ColumnOffset` right, each column sized by content width (clamped by `MinCellWidth` / `MaxCellWidth` and per-column `ColumnStyle`)

### TableStyle

`TableStyle` controls the visual appearance:

| Property | Default | Description |
|----------|---------|-------------|
| `ShowHeaders` | `true` | Show column header row |
| `ShowHorizontalHeaderOverline` | `true` | Line above headers |
| `ShowHorizontalHeaderUnderline` | `true` | Line below headers |
| `ShowVerticalCellLines` | `true` | Vertical separators between cells |
| `ShowVerticalHeaderLines` | `true` | Vertical separators between headers |
| `ShowHorizontalBottomLine` | `false` | Line below last row |
| `AlwaysShowHeaders` | `false` | Lock headers when scrolling |
| `ExpandLastColumn` | `true` | Fill remaining space with last column |
| `SmoothHorizontalScrolling` | `true` | Minimal horizontal scroll increments |
| `InvertSelectedCellFirstCharacter` | `false` | Show cursor character inversion |
| `RowColorGetter` | `null` | Custom row coloring delegate |

### EnsureCursorIsVisible

After programmatic cursor changes, call `EnsureCursorIsVisible ()` to scroll the viewport so the cursor cell is on screen. `Update ()` does this automatically.

---

## Column Styling

Use `TableStyle.ColumnStyles` to customize individual columns:

```csharp
tv.Style.ColumnStyles [2] = new ColumnStyle
{
    Alignment = Alignment.End,
    MaxWidth = 20,
    MinWidth = 5,
    Format = "C2",        // currency format
    ColorGetter = args => args.CellValue is int v && v < 0
        ? new Scheme () { Normal = new (Color.Red, Color.Black) }
        : null
};
```

### ColumnStyle Properties

| Property | Description |
|----------|-------------|
| `Alignment` | Default text alignment for the column |
| `AlignmentGetter` | Per-cell alignment delegate (overrides `Alignment`) |
| `ColorGetter` | Per-cell `Scheme` delegate |
| `RepresentationGetter` | Custom `object` → `string` conversion |
| `Format` | `IFormattable.ToString` format string |
| `MaxWidth` | Maximum column width in characters |
| `MinWidth` | Minimum column width in characters |
| `MinAcceptableWidth` | Flexible lower bound for column width |
| `Visible` | Hide the column entirely |

---

## Checkbox Columns

Wrap any `ITableSource` with a checkbox column using `CheckBoxTableSourceWrapperByIndex` or `CheckBoxTableSourceWrapperByObject<T>`:

```csharp
// By row index
CheckBoxTableSourceWrapperByIndex checkSrc = new (tv, tv.Table!);
tv.Table = checkSrc;

// Read checked rows
HashSet<int> checked = checkSrc.CheckedRows;
```

```csharp
// By object property
CheckBoxTableSourceWrapperByObject<MyObj> checkSrc = new (
    tv,
    enumSource,
    obj => obj.IsSelected,
    (obj, val) => obj.IsSelected = val
);
tv.Table = checkSrc;
```

Space toggles checkboxes on the selected row(s). Clicking the checkbox column header toggles all rows. Set `UseRadioButtons = true` for single-select radio behavior.

---

## Tree Tables

`TreeTableSource<T>` combines `TreeView<T>` expand/collapse with `TableView` column rendering:

```csharp
TreeView<FileSystemInfo> tree = new ()
{
    TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (
        d => d is DirectoryInfo dir ? dir.GetFileSystemInfos () : [],
        d => d is DirectoryInfo),
    AspectGetter = f => f.Name
};

tree.AddObject (new DirectoryInfo ("/"));

TreeTableSource<FileSystemInfo> src = new (
    tv,
    "Name",
    tree,
    new Dictionary<string, Func<FileSystemInfo, object>> ()
    {
        { "Size", f => f is FileInfo fi ? fi.Length : 0 },
        { "Modified", f => f.LastWriteTime }
    });

tv.Table = src;
```

Arrow Left/Right collapse/expand nodes when the tree column has focus.

> **Note:** `TreeTableSource` renders tree structure (branch lines, expand/collapse symbols) in column 0 but does **not** render checkboxes from `TreeView.CheckboxMode`. To add checkboxes to a tree-table, wrap the `TreeTableSource` with a `CheckBoxTableSourceWrapperByIndex` or `CheckBoxTableSourceWrapperByObject<T>` (see [Checkbox Columns](#checkbox-columns) above).

---

## Events

TableView uses the standard `IValue<T>` and `View` event patterns:

| Event | When |
|-------|------|
| `ValueChanging` | Before `Value` changes. Set `Handled = true` to cancel. |
| `ValueChanged` | After `Value` changed. Use this to react to selection changes. |
| `Accepted` | User double-clicks or presses the Accept key on a cell. |
| `Activating` | User clicks a cell (`Command.Activate`). |

### Example: Reacting to Selection Changes

```csharp
tv.ValueChanged += (sender, e) =>
{
    if (e.NewValue is { } sel)
    {
        statusBar.Text = $"Row {sel.SelectedCell.Y}, Col {sel.SelectedCell.X}";
    }
};
```

### Example: Handling Cell Activation

```csharp
tv.Accepted += (sender, e) =>
{
    Point selectedCell = tv.Value!.SelectedCell;
    object cellValue = tv.Table! [selectedCell.Y, selectedCell.X];
    MessageBox.Query ("Cell", $"Value: {cellValue}", "OK");
};
```
