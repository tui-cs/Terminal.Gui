// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

public partial class TreeView<T>
{
    // TODO: Refactor to use CWP
    /// <summary>Called when the <see cref="SelectedObject"/> changes.</summary>
    public event EventHandler<SelectionChangedEventArgs<T>>? SelectionChanged;

    /// <summary>
    ///     Moves the currently selected object by the given number of screen lines.
    ///     Each branch occupies 1 line on screen. Supports negative values.
    /// </summary>
    /// <remarks>
    ///     If nothing is currently selected or the selected object is no longer in the tree then the first object in the
    ///     tree is selected instead.
    /// </remarks>
    /// <param name="offset">Positive to move the selection down the screen, negative to move it up</param>
    /// <param name="expandSelection">
    ///     True to expand the selection (assuming <see cref="MultiSelect"/> is enabled). False to
    ///     replace.
    /// </param>
    public void AdjustSelection (int offset, bool expandSelection = false)
    {
        // if it is not a shift click, or we don't allow multi select
        if (!expandSelection || !MultiSelect)
        {
            _multiSelectedRegions.Clear ();
        }

        if (SelectedObject is null)
        {
            SelectedObject = Roots?.Keys.FirstOrDefault ();
        }
        else
        {
            IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

            int idx = map.IndexOf (b => b.Model.Equals (SelectedObject));

            if (idx == -1)
            {
                // The current selection has disappeared!
                SelectedObject = Roots?.Keys.FirstOrDefault ();
            }
            else
            {
                int newIdx = Math.Min (Math.Max (0, idx + offset), map.Count - 1);

                Branch<T> newBranch = map.ElementAt (newIdx);

                // If it is a multi selection
                if (expandSelection && MultiSelect)
                {
                    if (_multiSelectedRegions.Any ())
                    {
                        // expand the existing head selection
                        TreeSelection<T> head = _multiSelectedRegions.Pop ();
                        _multiSelectedRegions.Push (new TreeSelection<T> (head.Origin, newIdx, map));
                    }
                    else
                    {
                        // or start a new multi selection region
                        _multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt (idx), newIdx, map));
                    }
                }

                SelectedObject = newBranch.Model;

                EnsureVisible (SelectedObject);
            }
        }

        UpdateCursor ();

        SetNeedsDraw ();
    }

    /// <summary>Moves the selection to the last child in the currently selected level.</summary>
    public void AdjustSelectionToBranchEnd ()
    {
        T? o = SelectedObject;

        if (o is null)
        {
            return;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int currentIdx = map.IndexOf (b => Equals (b.Model, o));

        if (currentIdx == -1)
        {
            return;
        }

        Branch<T> currentBranch = map.ElementAt (currentIdx);
        Branch<T> next = currentBranch;

        for (; currentIdx < map.Count; currentIdx++)
        {
            //if it is the end of the current depth of branch
            if (currentBranch.Depth != next.Depth)
            {
                SelectedObject = currentBranch.Model;
                EnsureVisible (currentBranch.Model);
                SetNeedsDraw ();

                return;
            }

            // look at next branch for consideration
            currentBranch = next;
            next = map.ElementAt (currentIdx);
        }

        GoToEnd ();
    }

    /// <summary>Moves the selection to the first child in the currently selected level.</summary>
    public void AdjustSelectionToBranchStart ()
    {
        T? o = SelectedObject;

        if (o is null)
        {
            return;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int currentIdx = map.IndexOf (b => Equals (b.Model, o));

        if (currentIdx == -1)
        {
            return;
        }

        Branch<T> currentBranch = map.ElementAt (currentIdx);
        Branch<T> next = currentBranch;

        for (; currentIdx >= 0; currentIdx--)
        {
            //if it is the beginning of the current depth of branch
            if (currentBranch.Depth != next.Depth)
            {
                SelectedObject = currentBranch.Model;
                EnsureVisible (currentBranch.Model);
                SetNeedsDraw ();

                return;
            }

            // look at next branch up for consideration
            currentBranch = next;
            next = map.ElementAt (currentIdx);
        }

        // We ran all the way to top of tree
        GoToFirst ();
    }

    /// <summary>
    ///     Returns true if the given object <paramref name="o"/> is exposed in the tree and can be expanded otherwise
    ///     false.
    /// </summary>
    /// <param name="o">An object in the tree to check.</param>
    /// <returns>True if the object is visible and expandable.</returns>
    public bool CanExpand (T o) => ObjectToBranch (o)?.CanExpand () ?? false;

    /// <summary>Collapses the <see cref="SelectedObject"/>.</summary>
    public void Collapse () => Collapse (SelectedObject);

    /// <summary>Collapses the supplied object if it is currently expanded.</summary>
    /// <param name="toCollapse">The object to collapse.</param>
    public void Collapse (T? toCollapse) => CollapseImpl (toCollapse, false);

    /// <summary>
    ///     Collapses the supplied object if it is currently expanded. Also collapses all children branches (this will
    ///     only become apparent when/if the user expands it again).
    /// </summary>
    /// <param name="toCollapse">The object to collapse.</param>
    public void CollapseAll (T? toCollapse) => CollapseImpl (toCollapse, true);

    /// <summary>Collapses all root nodes in the tree.</summary>
    public void CollapseAll ()
    {
        if (Roots is null)
        {
            return;
        }

        foreach (KeyValuePair<T, Branch<T>> item in Roots)
        {
            item.Value.Collapse ();
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Adjusts the <see cref="ScrollOffsetVertical"/> to ensure the given <paramref name="model"/> is visible. Has no
    ///     effect if already visible.
    /// </summary>
    public void EnsureVisible (T? model)
    {
        if (model is null)
        {
            return;
        }

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int idx = map.IndexOf (b => Equals (b.Model, model));

        if (idx == -1)
        {
            return;
        }

        if (idx < ScrollOffsetVertical)
        {
            //if user has scrolled up too far to see their selection
            ScrollOffsetVertical = idx;
        }
        else if (idx >= ScrollOffsetVertical + Viewport.Height)
        {
            //if user has scrolled off bottom of visible tree
            ScrollOffsetVertical = Math.Max (0, idx + 1 - Viewport.Height);
        }
    }

    /// <summary>Expands the currently <see cref="SelectedObject"/>.</summary>
    public void Expand () => Expand (SelectedObject);

    /// <summary>
    ///     Expands the supplied object if it is contained in the tree (either as a root object or as an exposed branch
    ///     object).
    /// </summary>
    /// <param name="toExpand">The object to expand.</param>
    public void Expand (T? toExpand)
    {
        if (toExpand is null)
        {
            return;
        }

        ObjectToBranch (toExpand)?.Expand ();
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Toggles the expansion of the supplied object if it is contained in the tree (either as a root object or as an
    ///     exposed branch
    ///     object).
    /// </summary>
    /// <param name="toToggle">The object to toggle expand/collapse state.</param>
    public void Toggle (T? toToggle)
    {
        if (toToggle is null)
        {
            return;
        }

        Branch<T>? branch = ObjectToBranch (toToggle);

        if (branch is null)
        {
            return;
        }

        if (branch.IsExpanded)
        {
            branch.Collapse ();
        }
        else
        {
            branch.Expand ();
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>Expands the supplied object and all child objects.</summary>
    /// <param name="toExpand">The object to expand.</param>
    public void ExpandAll (T? toExpand)
    {
        if (toExpand is null)
        {
            return;
        }

        ObjectToBranch (toExpand)?.ExpandAll ();
        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Fully expands all nodes in the tree, if the tree is very big and built dynamically this may take a while (e.g.
    ///     for file system).
    /// </summary>
    public void ExpandAll ()
    {
        if (Roots is null)
        {
            return;
        }

        foreach (KeyValuePair<T, Branch<T>> item in Roots)
        {
            item.Value.ExpandAll ();
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns <see cref="SelectedObject"/> (if not null) and all multi selected objects if <see cref="MultiSelect"/>
    ///     is true.
    /// </summary>
    /// <returns>All selected objects in the tree.</returns>
    public IEnumerable<T> GetAllSelectedObjects ()
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        // To determine multi selected objects, start with the line map, that avoids yielding
        // hidden nodes that were selected then the parent collapsed e.g. programmatically or
        // with mouse click
        if (MultiSelect)
        {
            foreach (T m in map.Select (b => b.Model).Where (IsSelected))
            {
                yield return m;
            }
        }
        else
        {
            if (SelectedObject is { })
            {
                yield return SelectedObject;
            }
        }
    }

    /// <summary>Returns all objects whose effective check state is <see cref="CheckState.Checked"/>.</summary>
    /// <returns>Checked objects in tree order, followed by any checked objects that are not currently in the tree.</returns>
    public IEnumerable<T> GetCheckedObjects ()
    {
        HashSet<T> yielded = [];

        foreach (T model in EnumerateKnownObjects ())
        {
            if (GetCheckState (model) == CheckState.Checked && yielded.Add (model))
            {
                yield return model;
            }
        }

        foreach (T model in _checkedStates.Keys)
        {
            if (GetCheckState (model) == CheckState.Checked && yielded.Add (model))
            {
                yield return model;
            }
        }
    }

    /// <summary>Gets the effective check state of <paramref name="o"/>, derived from children if applicable.</summary>
    /// <param name="o">The node object.</param>
    /// <returns>The effective check state.</returns>
    public CheckState GetCheckState (T? o) => o is null ? CheckState.UnChecked : GetEffectiveCheckState (o, []);

    /// <summary>Sets the explicit check state of <paramref name="o"/> and propagates to all descendants.</summary>
    /// <param name="o">The node object.</param>
    /// <param name="state">The new check state.</param>
    public void SetChecked (T? o, CheckState state)
    {
        if (o is null)
        {
            return;
        }

        SetCheckedRecursive (o, state);
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Returns the currently expanded children of the passed object. Returns an empty collection if the branch is not
    ///     exposed or not expanded.
    /// </summary>
    /// <param name="o">An object in the tree.</param>
    /// <returns>The child objects, or an empty collection if not expanded.</returns>
    public IEnumerable<T> GetChildren (T? o)
    {
        Branch<T>? branch = ObjectToBranch (o);

        if (branch is null || !branch.IsExpanded)
        {
            return [];
        }

        return branch.ChildBranches?.Select (b => b.Model).ToArray () ?? [];
    }

    /// <summary>
    ///     Returns the parent object of <paramref name="o"/> in the tree. Returns null if the object is not exposed in
    ///     the tree.
    /// </summary>
    /// <param name="o">An object in the tree.</param>
    /// <returns>The parent object, or null if the object is a root or not exposed.</returns>
    public T? GetParent (T? o) => ObjectToBranch (o)?.Parent?.Model;

    /// <summary>
    ///     Returns the index of the object <paramref name="o"/> if it is currently exposed (it's parent(s) have been
    ///     expanded). This can be used with <see cref="ScrollOffsetVertical"/> and <see cref="View.SetNeedsDraw()"/> to
    ///     scroll to a specific object.
    /// </summary>
    /// <remarks>Uses the Equals method and returns the first index at which the object is found or -1 if it is not found.</remarks>
    /// <param name="o">An object that appears in your tree and is currently exposed.</param>
    /// <returns>The index the object was found at or -1 if it is not currently revealed or not in the tree at all.</returns>
    public int GetScrollOffsetOf (T o)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        for (var i = 0; i < map.Count; i++)
        {
            if (map.ElementAt (i).Model.Equals (o))
            {
                return i;
            }
        }

        //object not found
        return -1;
    }

    /// <summary>
    ///     Changes the <see cref="SelectedObject"/> to <paramref name="toSelect"/> and scrolls to ensure it is visible.
    ///     Has no effect if <paramref name="toSelect"/> is not exposed in the tree (e.g. its parents are collapsed).
    /// </summary>
    /// <param name="toSelect">The object to select and scroll to.</param>
    public void GoTo (T toSelect)
    {
        if (ObjectToBranch (toSelect) is null)
        {
            return;
        }

        SelectedObject = toSelect;
        EnsureVisible (toSelect);
        SetNeedsDraw ();
    }

    /// <summary>Changes the <see cref="SelectedObject"/> to the last object in the tree and scrolls so that it is visible.</summary>
    public void GoToEnd ()
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();
        ScrollOffsetVertical = Math.Max (0, map.Count - Viewport.Height);
        SelectedObject = map.LastOrDefault ()?.Model;

        SetNeedsDraw ();
    }

    /// <summary>
    ///     Changes the <see cref="SelectedObject"/> to the first root object and resets the
    ///     <see cref="ScrollOffsetVertical"/> to 0.
    /// </summary>
    public void GoToFirst ()
    {
        ScrollOffsetVertical = 0;
        SelectedObject = Roots?.Keys.FirstOrDefault ();

        SetNeedsDraw ();
    }

    /// <summary>Returns true if the given object <paramref name="o"/> is exposed in the tree and expanded otherwise false.</summary>
    /// <param name="o">An object in the tree to check.</param>
    /// <returns>True if the object is visible and expanded.</returns>
    public bool IsExpanded (T? o) => ObjectToBranch (o)?.IsExpanded ?? false;

    /// <summary>
    ///     Returns true if the <paramref name="model"/> is either the <see cref="SelectedObject"/> or part of a
    ///     <see cref="MultiSelect"/>.
    /// </summary>
    /// <param name="model">An object in the tree to check.</param>
    /// <returns>True if the object is selected.</returns>
    public bool IsSelected (T? model) => Equals (SelectedObject, model) || (MultiSelect && _multiSelectedRegions.Any (s => model is { } && s.Contains (model)));

    /// <summary>Moves the selection down by the height of the control (1 page).</summary>
    /// <param name="expandSelection">True if the navigation should add the covered nodes to the selected current selection.</param>
    public void MovePageDown (bool expandSelection = false) => AdjustSelection (Viewport.Height, expandSelection);

    /// <summary>Moves the selection up by the height of the control (1 page).</summary>
    /// <param name="expandSelection">True if the navigation should add the covered nodes to the selected current selection.</param>
    public void MovePageUp (bool expandSelection = false) => AdjustSelection (-Viewport.Height, expandSelection);

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        // If the key was bound to key command, let normal KeyDown processing happen. This enables overriding the default handling.
        // See: https://github.com/tui-cs/Terminal.Gui/issues/3950#issuecomment-2807350939
        if (KeyBindings.TryGet (key, out _))
        {
            return false;
        }

        // If not a keybinding, is the key a searchable key press?
        if (!KeystrokeNavigator.Matcher.IsCompatibleKey (key) || !AllowLetterBasedNavigation || SelectedObject is null)
        {
            return false;
        }

        // If there has been a call to InvalidateMap since the last time
        // we need a new one to reflect the new exposed tree state
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        // Find the current selected object within the tree
        int current = map.IndexOf (b => b.Model == SelectedObject);

        // The currently selected object is no longer in line map somehow
        if (current < 0)
        {
            return false;
        }

        int? newIndex = KeystrokeNavigator.GetNextMatchingItem (current, (char)key);

        if (newIndex is not { } idx || idx == -1)
        {
            return false;
        }

        SelectedObject = map.ElementAt (idx).Model;
        EnsureVisible (SelectedObject);

        return true;
    }

    /// <summary>Scrolls the view area down a single line without changing the current selection.</summary>
    public void ScrollDown ()
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();
        int lineCount = map.Count;

        if (ScrollOffsetVertical >= lineCount - Viewport.Height)
        {
            return;
        }

        ScrollOffsetVertical++;
        SetNeedsDraw ();
    }

    /// <summary>Scrolls the view area up a single line without changing the current selection.</summary>
    public void ScrollUp ()
    {
        if (ScrollOffsetVertical <= 0)
        {
            return;
        }

        ScrollOffsetVertical--;
        SetNeedsDraw ();
    }

    /// <summary>Selects all objects in the tree when <see cref="MultiSelect"/> is enabled otherwise does nothing.</summary>
    public void SelectAll ()
    {
        if (!MultiSelect)
        {
            return;
        }

        _multiSelectedRegions.Clear ();

        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        if (map.Count == 0)
        {
            return;
        }

        _multiSelectedRegions.Push (new TreeSelection<T> (map.ElementAt (0), map.Count, map));
        SetNeedsDraw ();
        UpdateCursor ();

        OnSelectionChanged (new SelectionChangedEventArgs<T> (this, SelectedObject, SelectedObject));
    }

    /// <summary>
    ///     Implementation of <see cref="Collapse(T)"/> and <see cref="CollapseAll(T)"/>. Performs operation and updates
    ///     selection if disappeared.
    /// </summary>
    /// <param name="toCollapse">The object to collapse.</param>
    /// <param name="all">True to also collapse all children recursively.</param>
    protected void CollapseImpl (T? toCollapse, bool all)
    {
        if (toCollapse is null)
        {
            return;
        }

        Branch<T>? branch = ObjectToBranch (toCollapse);

        // Nothing to collapse
        if (branch is null)
        {
            return;
        }

        if (all)
        {
            branch.CollapseAll ();
        }
        else
        {
            branch.Collapse ();
        }

        if (SelectedObject is { } && ObjectToBranch (SelectedObject) is null)
        {
            // If the old selection suddenly became invalid then clear it
            SelectedObject = null;
        }

        InvalidateLineMap ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Determines systems behaviour when the left arrow key is pressed. Default behaviour is to collapse the current
    ///     tree node if possible otherwise changes selection to current branches parent.
    /// </summary>
    protected virtual void CursorLeft (bool ctrl)
    {
        if (!IsExpanded (SelectedObject))
        {
            T? parent = GetParent (SelectedObject);

            if (parent is null)
            {
                return;
            }

            SelectedObject = parent;
            AdjustSelection (0);
            SetNeedsDraw ();

            return;
        }

        if (ctrl)
        {
            CollapseAll (SelectedObject);
        }
        else
        {
            Collapse (SelectedObject);
        }
    }

    /// <summary>
    ///     Determines systems behaviour when the space key is pressed. Default behaviour is to toggle the expansion of the
    ///     currently selected node if it has children.
    /// </summary>
    protected virtual void Space ()
    {
        if (SelectedObject is null)
        {
            return;
        }

        if (CheckboxMode)
        {
            ToggleChecked (SelectedObject);

            return;
        }

        Toggle (SelectedObject);
    }

    /// <summary>Raises the SelectionChanged event.</summary>
    /// <param name="e">Event args describing the selection change.</param>

    // TODO: Refactor to use CWP
    protected virtual void OnSelectionChanged (SelectionChangedEventArgs<T> e) => SelectionChanged?.Invoke (this, e);

    private IEnumerable<T> EnumerateKnownObjects ()
    {
        if (Roots is null)
        {
            yield break;
        }

        HashSet<T> seen = [];

        foreach (T root in Roots.Keys)
        {
            foreach (T model in EnumerateKnownObjects (root, seen, 0))
            {
                yield return model;
            }
        }
    }

    private IEnumerable<T> EnumerateKnownObjects (T model, HashSet<T> seen, int depth)
    {
        if (!seen.Add (model))
        {
            yield break;
        }

        yield return model;

        if (depth >= MaxDepth || TreeBuilder is null)
        {
            yield break;
        }

        foreach (T child in TreeBuilder.GetChildren (model))
        {
            foreach (T descendant in EnumerateKnownObjects (child, seen, depth + 1))
            {
                yield return descendant;
            }
        }
    }

    internal Rune GetCheckGlyph (T model) =>
        GetCheckState (model) switch
        {
            CheckState.Checked => Glyphs.CheckStateChecked,
            CheckState.UnChecked => Glyphs.CheckStateUnChecked,
            CheckState.None => Glyphs.CheckStateNone,
            _ => throw new ArgumentOutOfRangeException ()
        };

    private CheckState GetEffectiveCheckState (T model, HashSet<T> visited)
    {
        if (!visited.Add (model))
        {
            return _checkedStates.GetValueOrDefault (model, CheckState.UnChecked);
        }

        CheckState explicitState = _checkedStates.GetValueOrDefault (model, CheckState.UnChecked);

        if (TreeBuilder is null)
        {
            return explicitState;
        }

        // Derive state from known children (ChildBranches). These are preserved even after
        // collapse, so we can derive correct tri-state without calling TreeBuilder.GetChildren.
        Branch<T>? branch = ObjectToBranch (model);

        if (branch is not { ChildBranches: { Count: > 0 } childBranches })
        {
            return explicitState;
        }

        CheckState [] childStates = childBranches
                                    .Select (child => GetEffectiveCheckState (child.Model, visited))
                                    .ToArray ();

        if (childStates.All (state => state is CheckState.Checked))
        {
            return CheckState.Checked;
        }

        if (childStates.All (state => state is CheckState.UnChecked))
        {
            return CheckState.UnChecked;
        }

        // Mixed states among children - indeterminate
        return CheckState.None;
    }

    private void SetCheckedRecursive (T model, CheckState state)
    {
        SetCheckedRecursive (model, state, []);
    }

    private void SetCheckedRecursive (T model, CheckState state, HashSet<T> visited)
    {
        if (!visited.Add (model))
        {
            return;
        }

        SetCheckedCore (model, state);

        if (TreeBuilder is null)
        {
            return;
        }

        foreach (T child in TreeBuilder.GetChildren (model))
        {
            SetCheckedRecursive (child, state, visited);
        }
    }

    private void SetCheckedCore (T model, CheckState state)
    {
        CheckState oldEffective = GetCheckState (model);

        if (oldEffective == state)
        {
            return;
        }

        _checkedStates [model] = state;
        CheckedChanged?.Invoke (this, new CheckedChangedEventArgs<T> (this, model, oldEffective, state));
    }

    private void ToggleChecked (T model)
    {
        CheckState current = GetCheckState (model);

        // If effective state is not UnChecked, toggle to UnChecked
        if (current != CheckState.UnChecked)
        {
            SetChecked (model, CheckState.UnChecked);

            return;
        }

        // Effective state is UnChecked - but for collapsed nodes, descendants might
        // still be explicitly checked in the dictionary. Check for that case.
        if (HasCheckedDescendantsInState (model))
        {
            SetChecked (model, CheckState.UnChecked);

            return;
        }

        SetChecked (model, CheckState.Checked);
    }

    private bool HasCheckedDescendantsInState (T model)
    {
        if (TreeBuilder is null)
        {
            return false;
        }

        HashSet<T> visited = [];

        return HasCheckedDescendantsRecursive (model, visited);
    }

    private bool HasCheckedDescendantsRecursive (T model, HashSet<T> visited)
    {
        if (!visited.Add (model))
        {
            return false;
        }

        if (TreeBuilder is null)
        {
            return false;
        }

        foreach (T child in TreeBuilder.GetChildren (model))
        {
            if (_checkedStates.TryGetValue (child, out CheckState state) && state == CheckState.Checked)
            {
                return true;
            }

            if (HasCheckedDescendantsRecursive (child, visited))
            {
                return true;
            }
        }

        return false;
    }
}
