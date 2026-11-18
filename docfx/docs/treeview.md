# TreeView Deep Dive

TreeView displays and navigates hierarchical data with expandable/collapsible branches.

It comes in two forms:

- **<xref:Terminal.Gui.TreeView>** — convenience class where all nodes implement <xref:Terminal.Gui.ITreeNode>.
- **<xref:Terminal.Gui.TreeView`1>** — generic class that displays any `T : class` using an <xref:Terminal.Gui.ITreeBuilder`1>.

Both share the same rendering, navigation, selection, and command behavior.

## Table of Contents

- [Getting Started](#getting-started)
  - [Using TreeView with TreeNode](#using-treeview-with-treenode)
  - [Using TreeView\<T\> with ITreeBuilder](#using-treeviewt-with-itreebuilder)
- [Data Model](#data-model)
  - [ITreeNode and TreeNode](#itreenode-and-treenode)
  - [ITreeBuilder\<T\>](#itreebuilder)
  - [DelegateTreeBuilder\<T\>](#delegatetreebuilder)
  - [Custom ITreeNode Subclasses](#custom-itreenode-subclasses)
- [Commands and Input](#commands-and-input)
  - [Keyboard Bindings](#keyboard-bindings)
  - [Mouse Behavior](#mouse-behavior)
  - [Command Architecture](#command-architecture)
- [Events](#events)
  - [Accepting and Accepted (CWP)](#accepting-and-accepted-cwp)
  - [SelectionChanged](#selectionchanged)
  - [DrawLine](#drawline)
- [Appearance](#appearance)
  - [TreeStyle](#treestyle)
  - [AspectGetter](#aspectgetter)
  - [ColorGetter](#colorgetter)
- [Navigation and Selection](#navigation-and-selection)
  - [Programmatic Navigation](#programmatic-navigation)
  - [Multi-Select](#multi-select)
  - [Letter-Based Navigation](#letter-based-navigation)
- [Filtering](#filtering)
- [Checkbox Mode](#checkbox-mode)
- [Dynamic Updates](#dynamic-updates)
- [See Also](#see-also)

## Getting Started

### Using TreeView with TreeNode

The simplest approach uses the non-generic <xref:Terminal.Gui.TreeView> with <xref:Terminal.Gui.TreeNode> objects:

```csharp
TreeView tree = new ()
{
    Width = 40,
    Height = 20
};

TreeNode root1 = new () { Text = "Root1" };
root1.Children.Add (new TreeNode { Text = "Child1.1" });
root1.Children.Add (new TreeNode { Text = "Child1.2" });

TreeNode root2 = new () { Text = "Root2" };
root2.Children.Add (new TreeNode { Text = "Child2.1" });
root2.Children.Add (new TreeNode { Text = "Child2.2" });

tree.AddObject (root1);
tree.AddObject (root2);
```

This produces:

```
├-Root1
│ ├─Child1.1
│ └─Child1.2
└-Root2
  ├─Child2.1
  └─Child2.2
```

### Using TreeView\<T\> with ITreeBuilder

When your data model already exists (e.g. `Army` and `Unit` classes), use the generic `TreeView<T>` with an <xref:Terminal.Gui.ITreeBuilder`1> to tell the tree how objects relate:

```csharp
TreeView<GameObject> tree = new ()
{
    Width = 40,
    Height = 20,
    TreeBuilder = new DelegateTreeBuilder<GameObject> (
        childGetter: o => o is Army a ? a.Units : Enumerable.Empty<GameObject> (),
        canExpand: o => o is Army)
};

tree.AddObject (new Army
{
    Designation = "3rd Infantry",
    Units = [new Unit { Name = "Orc" }, new Unit { Name = "Troll" }]
});
```

## Data Model

### ITreeNode and TreeNode

<xref:Terminal.Gui.ITreeNode> is the interface for nodes in the non-generic `TreeView`:

| Member | Type | Description |
|--------|------|-------------|
| `Text` | `string` | Display text |
| `Children` | `IList<ITreeNode>` | Child nodes |
| `Tag` | `object?` | User data |

<xref:Terminal.Gui.TreeNode> is the default concrete implementation. `Children` is mutable — add or remove nodes at any time, then call `RefreshObject` to update the display.

### ITreeBuilder

For `TreeView<T>`, implement <xref:Terminal.Gui.ITreeBuilder`1> to describe the hierarchy:

```csharp
class GameObjectTreeBuilder : ITreeBuilder<GameObject>
{
    public bool SupportsCanExpand => true;

    public bool CanExpand (GameObject model) => model is Army;

    public IEnumerable<GameObject> GetChildren (GameObject model)
    {
        if (model is Army a)
        {
            return a.Units;
        }

        return Enumerable.Empty<GameObject> ();
    }
}
```

`SupportsCanExpand` enables a fast check for the expand/collapse symbol without fetching children. Set it to `true` when `CanExpand` is cheap.

### DelegateTreeBuilder

<xref:Terminal.Gui.DelegateTreeBuilder`1> provides the same thing with lambdas:

```csharp
tree.TreeBuilder = new DelegateTreeBuilder<GameObject> (
    childGetter: o => o is Army a ? a.Units : Enumerable.Empty<GameObject> (),
    canExpand: o => o is Army);
```

The first delegate returns children; the second is the `CanExpand` check (both are required).

### Custom ITreeNode Subclasses

You can subclass <xref:Terminal.Gui.TreeNode> to wrap your own data:

```csharp
class House : TreeNode
{
    public string Address { get; set; } = "";
    public List<Room> Rooms { get; set; } = [];

    public override IList<ITreeNode> Children => Rooms.Cast<ITreeNode> ().ToList ();
    public override string Text { get => Address; set => Address = value; }
}

class Room : TreeNode
{
    public string Name { get; set; } = "";
    public override string Text { get => Name; set => Name = value; }
}
```

Then add your objects directly:

```csharp
tree.AddObject (new House
{
    Address = "23 Nowhere Street",
    Rooms = [new Room { Name = "Ballroom" }, new Room { Name = "Bedroom" }]
});
```

## Commands and Input

TreeView integrates with the Terminal.Gui [command system](command.md). Input flows through `IInputProcessor` → `KeyBindings`/`MouseBindings` → `Command` → handler.

### Keyboard Bindings

| Key | Command | Behavior |
|-----|---------|----------|
| **Enter** | `Command.Accept` | Raises `Accepting`/`Accepted` (CWP) |
| **Space** | `Command.Activate` | Raises `Activating`/`Activated`; toggles expand/collapse (or toggles checkbox when `CheckboxMode` is enabled) |
| **→** | `Command.Expand` | Expand selected node |
| **Ctrl+→** | `Command.ExpandAll` | Expand node and all descendants |
| **←** | `Command.Collapse` | Collapse selected node, or navigate to parent node |
| **Ctrl+←** | `Command.CollapseAll` | Collapse node and all descendants |
| **↑** | `Command.Up` | Move selection up one row |
| **↓** | `Command.Down` | Move selection down one row |
| **Shift+↑** | `Command.UpExtend` | Extend selection up (multi-select) |
| **Shift+↓** | `Command.DownExtend` | Extend selection down (multi-select) |
| **Ctrl+↑** | `Command.LineUpToFirstBranch` | Jump to first sibling |
| **Ctrl+↓** | `Command.LineDownToLastBranch` | Jump to last sibling |
| **PageUp** | `Command.PageUp` | Move selection up one page |
| **PageDown** | `Command.PageDown` | Move selection down one page |
| **Shift+PageUp** | `Command.PageUpExtend` | Extend selection up one page |
| **Shift+PageDown** | `Command.PageDownExtend` | Extend selection down one page |
| **Home** | `Command.Start` | Select first node |
| **End** | `Command.End` | Select last node |
| **Ctrl+A** | `Command.SelectAll` | Select all (when `MultiSelect` is enabled) |
| *Any letter* | *(collection navigator)* | Jump to next matching node |

### Mouse Behavior

| Input | Behavior |
|-------|----------|
| **Single click** | Select the clicked node. If the click lands on the expand/collapse symbol (`+`/`-`), toggle expansion. If `CheckboxMode` is enabled and the click lands on the checkbox glyph, toggle check state. |
| **Double click** | Raises `Command.Accept` → fires `Accepting`/`Accepted` (CWP). Also toggles expand/collapse. |
| **Wheel up/down** | Scroll viewport vertically |
| **Wheel left/right** | Scroll viewport horizontally |

### Command Architecture

TreeView registers handlers for `Command.Activate` and `Command.Accept`:

- **`Command.Activate`** (`OnActivated`): Toggles expand/collapse on the selected node. For mouse clicks, only toggles when clicking the expand symbol. This is the handler for **Space** key and **single click**.

- **`Command.Accept`** (`OnAccepted`): Follows the standard [Cancellable Workflow Pattern](cancellable-work-pattern.md) — raises `Accepting`, then `Accepted` if not canceled. For mouse double-clicks, also toggles expand/collapse. **Enter** raises Accept without toggling.

- **`Command.Toggle`**: Directly toggles expand/collapse on the selected node regardless of context. **Space** is bound to `Command.Activate` in the base `View` class, but TreeView also supports `Command.Toggle` explicitly.

## Events

### Accepting and Accepted (CWP)

TreeView follows the standard [Cancellable Workflow Pattern](cancellable-work-pattern.md) for `Accept`:

```csharp
tree.Accepting += (sender, e) =>
                  {
                      // Fires on Enter key or double-click
                      // Set e.Cancel = true to prevent Accepted from firing
                  };

tree.Accepted += (_, _) =>
                 {
                     // Node was accepted (confirmed)
                     ITreeNode? selected = tree.SelectedObject;
                 };
```

| Trigger | Event Raised |
|---------|-------------|
| **Enter** key | `Accepting` → `Accepted` |
| **Double click** | `Accepting` → `Accepted` |
| **Space** key | `Activating` → `Activated` (not Accept) |
| **Single click** on expand symbol | `Activating` → `Activated` (not Accept) |

### SelectionChanged

Fires whenever `SelectedObject` changes:

```csharp
tree.SelectionChanged += (sender, e) =>
                         {
                             // e.OldValue is the previously selected object
                             // e.NewValue is the newly selected object
                         };
```

### DrawLine

Fires for each visible line during rendering, allowing per-line customization:

```csharp
tree.DrawLine += (sender, e) =>
                 {
                     // e.Model is the object being drawn
                     // e.IndexOfModelText is the column where text starts
                     // e.Cells is the list of cells to render
                     // Set e.Handled = true to suppress default rendering
                 };
```

## Appearance

### TreeStyle

Control rendering via the <xref:Terminal.Gui.TreeStyle> property:

| Property | Default | Description |
|----------|---------|-------------|
| `ShowBranchLines` | `true` | Show `│`, `├`, `└` connector lines |
| `ExpandableSymbol` | `+` | Symbol for collapsed expandable nodes |
| `CollapseableSymbol` | `-` | Symbol for expanded nodes |
| `ColorExpandSymbol` | `false` | Render expand symbol in highlight color |
| `InvertExpandSymbolColors` | `false` | Swap foreground/background on expand symbol |
| `HighlightModelTextOnly` | `false` | Highlight only the text, not the full row |

Set symbols to `null` to hide them entirely.

### AspectGetter

By default, TreeView renders each node using its `ToString()` method. Override this with `AspectGetter`:

```csharp
treeViewFiles.AspectGetter = f => f.FullName;
```

### ColorGetter

Assign per-node colors:

```csharp
tree.ColorGetter = node =>
                   {
                       if (node is HiddenFile)
                       {
                           return new Scheme { Normal = new Attribute (Color.Gray, Color.Black) };
                       }

                       return null; // Use default scheme
                   };
```

## Navigation and Selection

### Programmatic Navigation

| Method | Description |
|--------|-------------|
| `GoTo (T obj)` | Select and scroll to a specific object |
| `GoToFirst ()` | Select the first root node |
| `GoToEnd ()` | Select the last visible node |
| `EnsureVisible (T obj)` | Scroll so the object is in the viewport |
| `Expand (T obj)` | Expand a specific node |
| `ExpandAll (T obj)` | Expand a node and all its descendants |
| `Collapse (T obj)` | Collapse a specific node |
| `CollapseAll (T obj)` | Collapse a node and all its descendants |
| `Toggle (T obj)` | Toggle expand/collapse |
| `IsExpanded (T obj)` | Check if a node is expanded |
| `GetParent (T obj)` | Get the parent node (only works for expanded branches) |
| `GetChildren (T obj)` | Get the visible child nodes |
| `GetObjectOnRow (int row)` | Get the object at a viewport row |

### Multi-Select

Enable multi-selection with the `MultiSelect` property (default: `true`):

```csharp
tree.MultiSelect = true;
```

| Key | Behavior |
|-----|----------|
| **Shift+↑/↓** | Extend selection by one row |
| **Shift+PageUp/PageDown** | Extend selection by one page |
| **Ctrl+A** | Select all visible nodes |

Retrieve the selection with `GetAllSelectedObjects ()`. Single navigation clears extended selections.

### Letter-Based Navigation

When `AllowLetterBasedNavigation` is `true` (the default), pressing a letter key jumps to the next node whose `AspectGetter` text matches. This uses the `KeystrokeNavigator` property, which supports typing multiple characters in quick succession to refine the match.

Disable this when you have custom key bindings that conflict:

```csharp
tree.AllowLetterBasedNavigation = false;
```

## Filtering

Apply a filter to show only matching nodes (and their ancestor paths):

```csharp
tree.Filter = new TreeViewTextFilter<FileSystemInfo> (tree)
{
    Text = "*.cs"
};
```

Implement <xref:Terminal.Gui.ITreeViewFilter`1> for custom logic:

```csharp
class MyFilter : ITreeViewFilter<GameObject>
{
    public bool IsMatch (GameObject model) => model.ToString ().Contains ("Orc");
}
```

When a filter is active, parent nodes leading to matches remain visible even if they don't match, ensuring the tree structure is navigable.

Set `Filter` to `null` to remove filtering.

## Checkbox Mode

To enable built-in checkboxes, set `CheckboxMode = true`. Each node displays a checkbox glyph between the expand symbol and the text:

```csharp
tree.CheckboxMode = true;
```

This produces:

```
├-☐ Root1
│ ├─☐ Child1.1
│ └─☐ Child1.2
└-☐ Root2
```

### Tri-State Behavior

TreeView implements standard tri-state checkbox semantics:

| Parent State | Meaning |
|-------------|---------|
| **Unchecked** | No descendants are checked |
| **Checked** | All descendants are checked |
| **Indeterminate** | Some (but not all) descendants are checked |

- **Toggling a parent** propagates the new state to all descendants.
- **Toggling a leaf** updates ancestor states automatically via derivation.
- The indeterminate state is always derived — it cannot be set directly by the user.

### Interacting with Checkboxes

| Input | Behavior |
|-------|----------|
| **Space** | Toggle the check state of the selected node |
| **Click on checkbox glyph** | Toggle the check state of the clicked node |

### Programmatic Access

```csharp
// Get the effective check state (derived for parents)
CheckState state = tree.GetCheckState (node);

// Set check state (propagates to descendants)
tree.SetChecked (node, CheckState.Checked);

// Get all checked objects (includes parents derived as checked from children)
IEnumerable<T> checkedObjects = tree.GetCheckedObjects ();
```

### CheckedChanged Event

Fires for each node whose state changes (including descendants during propagation):

```csharp
tree.CheckedChanged += (sender, e) =>
                       {
                           // e.Object — the node whose state changed
                           // e.OldValue — previous CheckState
                           // e.NewValue — new CheckState
                       };
```

### CheckboxMode vs. CheckBoxTableSourceWrapper

When using `TreeTableSource<T>` to display a tree inside a `TableView`, the checkbox glyphs from `CheckboxMode` are **not** rendered in the table column. To add checkboxes to a tree-table, wrap the `TreeTableSource` with a `CheckBoxTableSourceWrapperByIndex` or `CheckBoxTableSourceWrapperByObject<T>` (see [TableView — Checkbox Columns](tableview.md#checkbox-columns)).

## Dynamic Updates

TreeView caches the expanded tree structure. After modifying nodes at runtime:

| Method | When to Use |
|--------|-------------|
| `RefreshObject (T obj)` | After changing a node's children or text |
| `RebuildTree ()` | After replacing the `TreeBuilder` or making sweeping changes |
| `InvalidateLineMap ()` | After changes that affect which lines are visible |

Example — adding a child node at runtime:

```csharp
TreeNode parent = (TreeNode)tree.SelectedObject!;
parent.Children.Add (new TreeNode { Text = "New Child" });
tree.RefreshObject (parent);
```

Example — removing a node:

```csharp
ITreeNode toDelete = tree.SelectedObject!;

if (tree.Objects.Contains (toDelete))
{
    // It's a root node
    tree.Remove (toDelete);
}
else
{
    // It's a child node — remove from parent's children
    ITreeNode? parent = tree.GetParent (toDelete);
    parent?.Children.Remove (toDelete);
    tree.RefreshObject (parent);
}
```

## See Also

- [Command Deep Dive](command.md) — Command architecture and input routing
- [Cancellable Workflow Pattern](cancellable-work-pattern.md) — The `Accepting`/`Accepted` event model
- [Views Catalog](views.md) — Overview of all built-in views
- [Keyboard Deep Dive](keyboard.md) — Key processing pipeline
- API Reference: <xref:Terminal.Gui.TreeView>, <xref:Terminal.Gui.TreeView`1>, <xref:Terminal.Gui.TreeNode>, <xref:Terminal.Gui.ITreeBuilder`1>, <xref:Terminal.Gui.TreeStyle>
