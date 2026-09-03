// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Hierarchical tree view with expandable branches. Branch objects are dynamically determined when expanded using
///     a user defined <see cref="ITreeBuilder{T}"/>.
///     <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
/// <remarks>
///     <para>
///         Key bindings are configurable via <see cref="DefaultKeyBindings"/> (TreeView-specific commands)
///         and <see cref="View.DefaultKeyBindings"/> (shared navigation commands).
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Up</term> <description>Moves up one node.</description>
///         </item>
///         <item>
///             <term>Down</term> <description>Moves down one node.</description>
///         </item>
///         <item>
///             <term>Right</term> <description>Expands the current branch.</description>
///         </item>
///         <item>
///             <term>Ctrl+Right</term> <description>Expands the current branch and all sub-branches.</description>
///         </item>
///         <item>
///             <term>Left</term> <description>Collapses the current branch.</description>
///         </item>
///         <item>
///             <term>Ctrl+Left</term> <description>Collapses the current branch and all sub-branches.</description>
///         </item>
///         <item>
///             <term>Ctrl+Up</term> <description>Moves up to the first branch at the same level.</description>
///         </item>
///         <item>
///             <term>Ctrl+Down</term> <description>Moves down to the last branch at the same level.</description>
///         </item>
///         <item>
///             <term>PageUp / PageDown</term> <description>Moves one page up or down.</description>
///         </item>
///         <item>
///             <term>Shift+Up / Shift+Down</term> <description>Extends the selection up or down.</description>
///         </item>
///         <item>
///             <term>Shift+PageUp / Shift+PageDown</term> <description>Extends the selection by one page.</description>
///         </item>
///         <item>
///             <term>Home / End</term> <description>Moves to the first or last node.</description>
///         </item>
///         <item>
///             <term>Ctrl+A</term> <description>Selects all nodes.</description>
///         </item>
///     </list>
/// </remarks>
public partial class TreeView<T> : View, ITreeView where T : class
{
    /// <summary>
    ///     Error message to display when the control is not properly initialized at draw time (nodes added but no tree
    ///     builder set).
    /// </summary>
    private const string NO_BUILDER_ERROR = "ERROR: TreeBuilder Not Set";

    /// <summary>
    ///     Creates a new tree view with absolute positioning. Use <see cref="AddObjects(IEnumerable{T})"/> to set
    ///     root objects for the tree. Children will not be rendered until you set <see cref="TreeBuilder"/>.
    /// </summary>
    public TreeView ()
    {
        CanFocus = true;

        // Enable built-in scroll bars via the View content-area system.
        ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        // Things this view knows how to do
        AddCommand (Command.PageUp,
                    () =>
                    {
                        MovePageUp ();

                        return true;
                    });

        AddCommand (Command.PageDown,
                    () =>
                    {
                        MovePageDown ();

                        return true;
                    });

        AddCommand (Command.PageUpExtend,
                    () =>
                    {
                        MovePageUp (true);

                        return true;
                    });

        AddCommand (Command.PageDownExtend,
                    () =>
                    {
                        MovePageDown (true);

                        return true;
                    });

        AddCommand (Command.Expand,
                    () =>
                    {
                        Expand ();

                        return true;
                    });

        AddCommand (Command.ExpandAll,
                    () =>
                    {
                        ExpandAll (SelectedObject);

                        return true;
                    });

        AddCommand (Command.Collapse,
                    () =>
                    {
                        CursorLeft (false);

                        return true;
                    });

        AddCommand (Command.CollapseAll,
                    () =>
                    {
                        CursorLeft (true);

                        return true;
                    });

        AddCommand (Command.Toggle,
                    () =>
                    {
                        Space ();

                        return true;
                    });

        AddCommand (Command.Up,
                    () =>
                    {
                        AdjustSelection (-1);

                        return true;
                    });

        AddCommand (Command.UpExtend,
                    () =>
                    {
                        AdjustSelection (-1, true);

                        return true;
                    });

        AddCommand (Command.LineUpToFirstBranch,
                    () =>
                    {
                        AdjustSelectionToBranchStart ();

                        return true;
                    });

        AddCommand (Command.Down,
                    () =>
                    {
                        AdjustSelection (1);

                        return true;
                    });

        AddCommand (Command.DownExtend,
                    () =>
                    {
                        AdjustSelection (1, true);

                        return true;
                    });

        AddCommand (Command.LineDownToLastBranch,
                    () =>
                    {
                        AdjustSelectionToBranchEnd ();

                        return true;
                    });

        AddCommand (Command.Start,
                    () =>
                    {
                        GoToFirst ();

                        return true;
                    });

        AddCommand (Command.End,
                    () =>
                    {
                        GoToEnd ();

                        return true;
                    });

        AddCommand (Command.SelectAll,
                    () =>
                    {
                        SelectAll ();

                        return true;
                    });

        AddCommand (Command.ScrollUp,
                    () =>
                    {
                        ScrollUp ();

                        return true;
                    });

        AddCommand (Command.ScrollDown,
                    () =>
                    {
                        ScrollDown ();

                        return true;
                    });

        // Apply configurable key bindings (TreeView-specific layer + base layer)
        ApplyKeyBindings (DefaultKeyBindings, View.DefaultKeyBindings);

        KeystrokeNavigator.Matcher = new TreeViewCollectionNavigatorMatcher<T> (this);

        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
    }

    /// <summary>
    ///     Initialises <see cref="TreeBuilder"/>. Creates a new tree view with absolute positioning. Use
    ///     <see cref="AddObjects(IEnumerable{T})"/> to set root objects for the tree.
    /// </summary>
    public TreeView (ITreeBuilder<T> builder) : this () => TreeBuilder = builder;

    /// <summary>
    ///     Gets or sets the default key bindings for <see cref="TreeView{T}"/>. These are layered on top of
    ///     <see cref="View.DefaultKeyBindings"/> when the view is created.
    ///     <para>
    ///         <b>IMPORTANT:</b> This is a process-wide static property. Change with care.
    ///         Do not set in parallelizable unit tests.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is not a configuration-bound property because
    ///         <see cref="TreeView{T}"/> is a generic type. Use <see cref="View.ViewKeyBindings"/> to
    ///         override key bindings for TreeView via configuration.
    ///     </para>
    /// </remarks>

    // ReSharper disable once StaticMemberInGenericType
    public new static Dictionary<Command, PlatformKeyBinding> DefaultKeyBindings { get; set; } = new ()
    {
        // Tree-specific expand/collapse
        [Command.Expand] = Bind.All (Key.CursorRight),
        [Command.ExpandAll] = Bind.All (Key.CursorRight.WithCtrl),
        [Command.Collapse] = Bind.All (Key.CursorLeft),
        [Command.CollapseAll] = Bind.All (Key.CursorLeft.WithCtrl),

        // Branch navigation
        [Command.LineUpToFirstBranch] = Bind.All (Key.CursorUp.WithCtrl),
        [Command.LineDownToLastBranch] = Bind.All (Key.CursorDown.WithCtrl),

        // TreeView adds Home/End as additional Start/End bindings (the base layer also provides Ctrl+Home/Ctrl+End)
        [Command.Start] = Bind.All (Key.Home),
        [Command.End] = Bind.All (Key.End)
    };

    /// <summary>
    ///     Interface for filtering which lines of the tree are displayed e.g. to provide text searching.  Defaults to
    ///     <see langword="null"/> (no filtering).
    /// </summary>
    public ITreeViewFilter<T>? Filter { get; set; } = null;

    /// <summary>Enables built-in checkbox rendering and toggling for tree nodes.</summary>
    public bool CheckboxMode
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            UpdateContentSize ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Raised when a node check state changes.</summary>
    public event EventHandler<CheckedChangedEventArgs<T>>? CheckedChanged;

    /// <summary>True makes a letter key press navigate to the next visible branch that begins with that letter/digit.</summary>
    public bool AllowLetterBasedNavigation { get; set; } = true;

    /// <summary>
    ///     Returns the string representation of model objects hosted in the tree. Default implementation is to call
    ///     <see cref="object.ToString"/>.
    /// </summary>
    public AspectGetterDelegate<T> AspectGetter { get; set; } = o => o.ToString () ?? "";

    /// <summary>
    ///     Delegate for basic multi-colored tree view use cases. Return the <see cref="Scheme"/> to use for each passed
    ///     object, or <see langword="null"/> to use the default tree view scheme.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         TreeView uses only <see cref="Scheme.Normal"/>, <see cref="Scheme.Focus"/>, and <see cref="Scheme.Active"/>
    ///         from the returned scheme.
    ///         <list type="bullet">
    ///             <item>
    ///                 <term>Normal</term>
    ///                 <description>Used for unselected branches.</description>
    ///             </item>
    ///             <item>
    ///                <term>Focus</term>
    ///               <description>Used for selected branches when the tree view has focus.</description>
    ///             </item>
    ///             <item>
    ///                 <term>Active</term>
    ///                 <description>Used for selected branches when the tree view does not have focus.</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         For greater control over rendering, handle the <see cref="DrawLine"/> event instead.
    ///     </para>
    /// </remarks>
    public Func<T, Scheme?>? ColorGetter { get; set; }

    /// <summary>
    ///     Gets the <see cref="CollectionNavigator"/> that searches the <see cref="Objects"/> collection as the user
    ///     types.
    /// </summary>
    public IListCollectionNavigator KeystrokeNavigator { get; } = new CollectionNavigator ();

    /// <summary>Maximum number of nodes that can be expanded in any given branch.</summary>
    public int MaxDepth { get; set; } = 100;

    /// <summary>True to allow multiple objects to be selected at once.</summary>
    public bool MultiSelect { get; set; } = true;

    /// <summary>The root objects in the tree, note that this collection is of root objects only.</summary>
    public IEnumerable<T>? Objects => Roots?.Keys;

    /// <summary>The amount of tree view that has been scrolled to the right (horizontally).</summary>
    /// <remarks>
    ///     Setting a value of less than 0 will result in an offset of 0. To see changes in the UI call
    ///     <see cref="View.SetNeedsDraw()"/>. This property delegates to <see cref="View.Viewport"/><c>.X</c>.
    /// </remarks>
    public int ScrollOffsetHorizontal
    {
        get => Viewport.X;
        set
        {
            Viewport = Viewport with { X = Math.Max (0, value) };
            SetNeedsDraw ();
        }
    }

    /// <summary>The amount of tree view that has been scrolled off the top of the screen (by the user scrolling down).</summary>
    /// <remarks>
    ///     Setting a value of less than 0 will result in an offset of 0. To see changes in the UI call
    ///     <see cref="View.SetNeedsDraw()"/>. This property delegates to <see cref="View.Viewport"/><c>.Y</c>.
    /// </remarks>
    public int ScrollOffsetVertical
    {
        get => Viewport.Y;
        set
        {
            Viewport = Viewport with { Y = Math.Max (0, value) };
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     The currently selected object in the tree. When <see cref="MultiSelect"/> is true this is the object at which
    ///     the cursor is at.
    /// </summary>
    public T? SelectedObject
    {
        get;
        set
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            T? oldValue = field;
            field = value;

            OnSelectionChanged (new SelectionChangedEventArgs<T> (this, oldValue, value));
            SetNeedsDraw ();
        }
    }

    /// <summary>Determines how sub-branches of the tree are dynamically built at runtime as the user expands root nodes.</summary>
    public ITreeBuilder<T>? TreeBuilder { get; set; }

    /// <summary>
    ///     Map of root objects to the branches under them. All objects have a <see cref="Branch{T}"/> even if that branch
    ///     has no children.
    /// </summary>
    internal Dictionary<T, Branch<T>>? Roots { get; set; } = new ();

    /// <summary>Contains options for changing how the tree is rendered.</summary>
    public TreeStyle Style { get; set; } = new ();

    /// <summary>Secondary selected regions of tree when <see cref="MultiSelect"/> is true.</summary>
    private readonly Stack<TreeSelection<T>> _multiSelectedRegions = new ();

    /// <summary>Explicit check states for nodes in checkbox mode.</summary>
    private readonly Dictionary<T, CheckState> _checkedStates = new ();

    /// <summary>Removes all objects from the tree and clears <see cref="SelectedObject"/>.</summary>
    public void ClearObjects ()
    {
        SelectedObject = null;
        _multiSelectedRegions.Clear ();
        _checkedStates.Clear ();
        Roots = new Dictionary<T, Branch<T>> ();
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);

        // Mouse activation: use hit-test to find the clicked branch directly
        if (ctx?.Binding is MouseBinding { MouseEvent: { } } mouseBinding)
        {
            Branch<T>? clickedBranch = HitTest (mouseBinding.MouseEvent.Position!.Value.Y);

            if (clickedBranch is null)
            {
                return;
            }

            SelectedObject = clickedBranch.Model;

            if (clickedBranch.IsHitOnCheckbox (mouseBinding.MouseEvent.Position!.Value.X))
            {
                ToggleChecked (clickedBranch.Model);

                return;
            }

            if (clickedBranch.IsHitOnExpandableSymbol (mouseBinding.MouseEvent.Position!.Value.X))
            {
                Toggle (clickedBranch.Model);
            }

            return;
        }

        // Keyboard activation: operate on currently selected object
        T? o = SelectedObject;

        if (o is null)
        {
            return;
        }

        if (CheckboxMode)
        {
            ToggleChecked (o);

            return;
        }

        Toggle (o);
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

        T? o = SelectedObject;

        if (o is null)
        {
            return;
        }

        // Only toggle on mouse (double-click), not keyboard (enter) - This is what most tree views do
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } } mouseBinding)
        {
            return;
        }

        // The line they clicked on a branch
        Branch<T>? clickedBranch = HitTest (mouseBinding.MouseEvent.Position!.Value.Y);

        if (clickedBranch is null)
        {
            return;
        }

        Toggle (SelectedObject);
    }

    /// <summary>Adds a new root level object unless it is already a root of the tree.</summary>
    /// <param name="o">The object to add as a root node.</param>
    public void AddObject (T? o)
    {
        if (o is null)
        {
            return;
        }

        if (Roots is null || Roots.ContainsKey (o))
        {
            return;
        }

        Roots.Add (o, new Branch<T> (this, null, o));
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>Adds many new root level objects. Objects that are already root objects are ignored.</summary>
    /// <param name="collection">Objects to add as new root level objects.</param>
    public void AddObjects (IEnumerable<T> collection)
    {
        var objectsAdded = false;

        foreach (T o in collection)
        {
            if (Roots is null || Roots.ContainsKey (o))
            {
                continue;
            }

            Roots.Add (o, new Branch<T> (this, null, o));
            objectsAdded = true;
        }

        if (!objectsAdded)
        {
            return;
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Rebuilds the tree structure for all exposed objects starting with the root objects. Call this method when you
    ///     know there are changes to the tree but don't know which objects have changed (otherwise use
    ///     <see cref="RefreshObject(T, bool)"/>).
    /// </summary>
    public void RebuildTree ()
    {
        if (Roots is { })
        {
            foreach (Branch<T> branch in Roots.Values)
            {
                branch.Rebuild ();
            }
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Refreshes the state of the object <paramref name="o"/> in the tree. This will recompute children, string
    ///     representation etc.
    /// </summary>
    /// <remarks>This has no effect if the object is not exposed in the tree.</remarks>
    /// <param name="o">The object to refresh.</param>
    /// <param name="startAtTop">
    ///     True to also refresh all ancestors of the objects branch (starting with the root). False to
    ///     refresh only the passed node.
    /// </param>
    public void RefreshObject (T? o, bool startAtTop = false)
    {
        Branch<T>? branch = ObjectToBranch (o);

        branch?.Refresh (startAtTop);
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>Removes the given root object from the tree.</summary>
    /// <remarks>If <paramref name="o"/> is the currently <see cref="SelectedObject"/> then the selection is cleared.</remarks>
    /// <param name="o">The root object to remove.</param>
    public void Remove (T o)
    {
        // ReSharper disable once CanSimplifyDictionaryRemovingWithSingleCall
        if (Roots is null || !Roots.ContainsKey (o))
        {
            return;
        }

        foreach (T model in EnumerateKnownObjects (o, [], 0))
        {
            _checkedStates.Remove (model);
        }

        Roots.Remove (o);
        InvalidateLineMap ();
        SetNeedsDraw ();

        if (Equals (SelectedObject, o))
        {
            SelectedObject = null;
        }
    }

    /// <summary>Cached result of <see cref="BuildLineMap"/></summary>
    private IReadOnlyCollection<Branch<T>>? _cachedLineMap;

    /// <summary>Reentrancy guard for <see cref="UpdateContentSize"/>.</summary>
    private bool _updatingContentSize;

    /// <summary>Clears any cached results of the tree state.</summary>
    public void InvalidateLineMap ()
    {
        _cachedLineMap = null;

        // Eagerly update the content size so the View's content-area system stays in
        // sync (e.g. so scroll offset clamping is correct after expand/collapse).
        if (!_updatingContentSize)
        {
            UpdateContentSize ();
        }
    }

    /// <summary>
    ///     Calculates all currently visible/expanded branches (including leafs) and outputs them by index from the top of
    ///     the screen.
    /// </summary>
    /// <remarks>
    ///     Index 0 of the returned array is the first item that should be visible in the top of the control, index 1 is
    ///     the next etc.
    /// </remarks>
    /// <returns></returns>
    internal IReadOnlyCollection<Branch<T>> BuildLineMap ()
    {
        if (_cachedLineMap is { })
        {
            return _cachedLineMap;
        }

        List<Branch<T>> toReturn = [];

        if (Roots is { })
        {
            foreach (Branch<T> root in Roots.Values)
            {
                IEnumerable<Branch<T>> toAdd = AddToLineMap (root, false, out bool isMatch);

                if (isMatch)
                {
                    toReturn.AddRange (toAdd);
                }
            }
        }

        _cachedLineMap = new ReadOnlyCollection<Branch<T>> (toReturn);

        // Update the collection used for search-typing
        KeystrokeNavigator.Collection = _cachedLineMap.Select (b => b.Model).ToArray ();

        return _cachedLineMap;
    }

    private IEnumerable<Branch<T>> AddToLineMap (Branch<T> currentBranch, bool parentMatches, out bool match)
    {
        bool weMatch = IsFilterMatch (currentBranch);
        var anyChildMatches = false;

        List<Branch<T>> toReturn = new ();
        List<Branch<T>> children = new ();

        if (currentBranch is { IsExpanded: true, ChildBranches: { } })
        {
            foreach (Branch<T> subBranch in currentBranch.ChildBranches)
            {
                foreach (Branch<T> sub in AddToLineMap (subBranch, weMatch, out bool childMatch))
                {
                    if (!childMatch)
                    {
                        continue;
                    }

                    children.Add (sub);
                    anyChildMatches = true;
                }
            }
        }

        if (parentMatches || weMatch || anyChildMatches)
        {
            match = true;
            toReturn.Add (currentBranch);
        }
        else
        {
            match = false;
        }

        toReturn.AddRange (children);

        return toReturn;
    }

    private bool IsFilterMatch (Branch<T> branch) => Filter?.IsMatch (branch.Model) ?? true;

    /// <summary>
    ///     Keeps the View content-area in sync with the tree's logical dimensions. Called from
    ///     <see cref="InvalidateLineMap"/> and <see cref="OnViewportChanged"/>.
    /// </summary>
    private void UpdateContentSize ()
    {
        if (_updatingContentSize)
        {
            return;
        }

        _updatingContentSize = true;

        try
        {
            SetContentSize (GetSize ());
        }
        finally
        {
            _updatingContentSize = false;
        }
    }

    /// <summary>
    ///     Calculates the logical size of the tree based on currently expanded branches. The width is the maximum width of
    ///     all visible branches, and the height is the total number of visible branches.
    /// </summary>
    /// <returns></returns>
    public Size GetSize ()
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        return new Size (map.Count > 0 ? map.Max (b => b.GetWidth ()) : 0, map.Count);
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        // Only update content size when viewport size changes (e.g. resize),
        // not on every scroll (position change), to avoid O(n) recomputation.
        if (e.OldViewport.Size != e.NewViewport.Size)
        {
            UpdateContentSize ();
        }
    }

    /// <summary>
    ///     Returns the corresponding <see cref="Branch{T}"/> in the tree for <paramref name="toFind"/>. This will not
    ///     work for objects hidden by their parent being collapsed.
    /// </summary>
    /// <param name="toFind"></param>
    /// <returns>The branch for <paramref name="toFind"/> or null if it is not currently exposed in the tree.</returns>
    private Branch<T>? ObjectToBranch (T? toFind) => BuildLineMap ().FirstOrDefault (o => o.Model.Equals (toFind));

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        ColorGetter = null;
    }
}
