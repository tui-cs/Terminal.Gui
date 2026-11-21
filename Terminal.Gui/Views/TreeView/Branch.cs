namespace Terminal.Gui.Views;

internal class Branch<T> where T : class
{
    private readonly TreeView<T> _tree;

    /// <summary>
    ///     Declares a new branch of <paramref name="tree"/> in which the users object <paramref name="model"/> is
    ///     presented.
    /// </summary>
    /// <param name="tree">The UI control in which the branch resides.</param>
    /// <param name="parentBranchIfAny">Pass null for root level branches, otherwise pass the parent.</param>
    /// <param name="model">The user's object that should be displayed.</param>
    public Branch (TreeView<T> tree, Branch<T>? parentBranchIfAny, T model)
    {
        _tree = tree;
        Model = model;

        if (parentBranchIfAny is null)
        {
            return;
        }

        Depth = parentBranchIfAny.Depth + 1;
        Parent = parentBranchIfAny;
    }

    /// <summary>
    ///     The children of the current branch.  This is null until the first call to <see cref="FetchChildren"/> to avoid
    ///     enumerating the entire underlying hierarchy.
    /// </summary>
    public List<Branch<T>>? ChildBranches { get; set; }

    /// <summary>The depth of the current branch.  Depth of 0 indicates root level branches.</summary>
    public int Depth { get; }

    /// <summary>True if the branch is expanded to reveal child branches.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>The users object that is being displayed by this branch of the tree.</summary>
    public T Model { get; private set; }

    /// <summary>The parent <see cref="Branch{T}"/> or null if it is a root.</summary>
    public Branch<T>? Parent { get; }

    /// <summary>
    ///     Returns true if the current branch can be expanded according to the <see cref="TreeBuilder{T}"/> or cached
    ///     children already fetched.
    /// </summary>
    /// <returns></returns>
    public bool CanExpand ()
    {
        // if we do not know the children yet
        if (ChildBranches is { })
        {
            //we fetched or already know the children, so return whether we have any
            return ChildBranches.Any ();
        }

        //if there is a rapid method for determining whether there are children
        if (_tree.TreeBuilder is { SupportsCanExpand: true })
        {
            return _tree.TreeBuilder.CanExpand (Model);
        }

        //there is no way of knowing whether we can expand without fetching the children
        ChildBranches = FetchChildren ();

        //we fetched or already know the children, so return whether we have any
        return ChildBranches.Any ();
    }

    /// <summary>Marks the branch as collapsed (<see cref="IsExpanded"/> false).</summary>
    public void Collapse () => IsExpanded = false;

    /// <summary>Renders the current <see cref="Model"/> on the specified line <paramref name="y"/>.</summary>
    /// <param name="y"></param>
    /// <param name="availableWidth"></param>
    public virtual void Draw (int y, int availableWidth)
    {
        List<Cell> cells = new ();
        int? indexOfExpandCollapseSymbol = null;
        int indexOfModelText;

        // true if the current line of the tree is the selected one and control has focus
        bool isSelected = _tree.IsSelected (Model);

        Attribute textColor = isSelected
                                  ? _tree.HasFocus ? _tree.GetAttributeForRole (VisualRole.Focus) : _tree.GetAttributeForRole (VisualRole.Active)
                                  : _tree.GetAttributeForRole (VisualRole.Normal);
        Attribute symbolColor = _tree.Style.HighlightModelTextOnly ? _tree.GetAttributeForRole (VisualRole.Normal) : textColor;

        // Everything on the line before the expansion run and branch text
        string [] prefix = GetLinePrefix ().ToArray ();
        string expansion = GetExpandableSymbol ();
        string lineBody = _tree.AspectGetter (Model);

        if (_tree.CheckboxMode)
        {
            lineBody = $"{_tree.GetCheckGlyph (Model)} {lineBody}";
        }

        _tree.Move (0, y);

        // if we have scrolled to the right then bits of the prefix will have disappeared off the screen
        int toSkip = _tree.ScrollOffsetHorizontal;
        Attribute attr = symbolColor;

        // Draw the line prefix (all parallel lanes or whitespace and an expand/collapse/leaf symbol)
        foreach (string s in prefix)
        {
            string? visible = TakeVisibleGrapheme (s, ref toSkip);

            if (visible is null)
            {
                continue;
            }

            cells.Add (NewCell (attr, visible));
            availableWidth -= visible.GetColumns ();
        }

        // pick color for expanded symbol
        if (_tree.Style.ColorExpandSymbol || _tree.Style.InvertExpandSymbolColors)
        {
            Attribute color;

            if (_tree.Style.ColorExpandSymbol)
            {
                if (isSelected)
                {
                    color = _tree.Style.HighlightModelTextOnly ? _tree.GetAttributeForRole (VisualRole.Highlight) :
                            _tree.HasFocus ? _tree.GetAttributeForRole (VisualRole.Focus) : _tree.GetAttributeForRole (VisualRole.Active);
                }
                else
                {
                    color = _tree.GetAttributeForRole (VisualRole.Highlight);
                }
            }
            else
            {
                color = symbolColor;
            }

            if (_tree.Style.InvertExpandSymbolColors)
            {
                color = new Attribute (color.Background, color.Foreground, color.Style);
            }

            attr = color;
        }

        string? visibleExpansion = TakeVisibleGrapheme (expansion, ref toSkip);

        if (visibleExpansion is { })
        {
            if (visibleExpansion == expansion)
            {
                indexOfExpandCollapseSymbol = cells.Count;
            }

            cells.Add (NewCell (attr, visibleExpansion));
            availableWidth -= visibleExpansion.GetColumns ();
        }

        List<string> lineBodyGraphemes = lineBody.ToStringList ();

        // horizontal scrolling has already skipped the prefix but now must also skip some of the line body
        if (toSkip > 0)
        {
            // For the event record a negative location for where model text starts since it
            // is pushed off to the left because of scrolling.
            // Account for checkbox cells (glyph + space) prepended to lineBody.
            int checkboxOffset = _tree.CheckboxMode ? 2 : 0;

            if (toSkip >= lineBody.GetColumns ())
            {
                indexOfModelText = -(toSkip - checkboxOffset);
                lineBodyGraphemes = [];
            }
            else
            {
                int columnsSkipped = 0;
                List<string> remaining = [];

                foreach (string grapheme in lineBodyGraphemes)
                {
                    if (columnsSkipped >= toSkip)
                    {
                        remaining.Add (grapheme);

                        continue;
                    }

                    int graphemeColumns = grapheme.GetColumns ();

                    if (columnsSkipped + graphemeColumns <= toSkip)
                    {
                        columnsSkipped += graphemeColumns;

                        continue;
                    }

                    int leftover = columnsSkipped + graphemeColumns - toSkip;
                    columnsSkipped += graphemeColumns;

                    if (leftover > 0)
                    {
                        remaining.Add (new string (' ', leftover));
                    }
                }

                // Use the actual display-column width skipped, not the requested offset.
                indexOfModelText = -(columnsSkipped - checkboxOffset);
                lineBodyGraphemes = remaining;
            }
        }
        else
        {
            indexOfModelText = cells.Count + (_tree.CheckboxMode ? 2 : 0);
        }

        List<string> visibleLineBody = [];

        foreach (string grapheme in lineBodyGraphemes)
        {
            int columns = grapheme.GetColumns ();

            if (columns <= availableWidth)
            {
                visibleLineBody.Add (grapheme);
                availableWidth -= columns;

                continue;
            }

            if (availableWidth > 0)
            {
                visibleLineBody.Add (new string (' ', availableWidth));
                availableWidth = 0;
            }

            break;
        }

        // default behaviour is for model to use the scheme
        // of the tree view
        Attribute modelColor = textColor;

        // if custom color delegate invoke it
        if (_tree.ColorGetter is { })
        {
            Scheme? modelScheme = _tree.ColorGetter (Model);

            // if custom scheme is defined for this Model
            if (modelScheme is { })
            {
                // use it
                modelColor = isSelected
                                 ? _tree.HasFocus ? modelScheme.Focus : modelScheme.Active
                                 : modelScheme.Normal;
            }
            else
            {
                modelColor = new Attribute ();
            }
        }

        attr = modelColor;

        foreach (string grapheme in visibleLineBody)
        {
            cells.Add (NewCell (attr, grapheme));
        }

        if (availableWidth > 0)
        {
            attr = symbolColor;

            cells.AddRange (Enumerable.Repeat (NewCell (attr, " "), availableWidth));
        }

        DrawTreeViewLineEventArgs<T> e = new ()
        {
            Model = Model,
            Y = y,
            Cells = cells,
            Tree = _tree,
            IndexOfExpandCollapseSymbol = indexOfExpandCollapseSymbol,
            IndexOfModelText = indexOfModelText
        };
        _tree.OnDrawLine (e);

        if (!e.Handled)
        {
            foreach (Cell cell in cells)
            {
                _tree.SetAttribute ((Attribute)cell.Attribute!);
                _tree.AddStr (cell.Grapheme);
            }
        }

        _tree.SetAttribute (_tree.GetAttributeForRole (VisualRole.Normal));
    }

    /// <summary>Expands the current branch if possible.</summary>
    public void Expand ()
    {
        ChildBranches ??= FetchChildren ();

        if (ChildBranches.Any ())
        {
            IsExpanded = true;
        }
    }

    /// <summary>Fetch the children of this branch. This method populates <see cref="ChildBranches"/>.</summary>
    private List<Branch<T>> FetchChildren ()
    {
        if (_tree.TreeBuilder is null)
        {
            return [];
        }

        IEnumerable<T> children = Depth >= _tree.MaxDepth ? [] : _tree.TreeBuilder.GetChildren (Model);

        return children.Select (o => new Branch<T> (_tree, this, o)).ToList ();
    }

    /// <summary>
    ///     Returns an appropriate symbol for displaying next to the string representation of the <see cref="Model"/>
    ///     object to indicate whether it <see cref="IsExpanded"/> or not (or it is a leaf).
    /// </summary>
    /// <returns></returns>
    public string GetExpandableSymbol ()
    {
        Rune leafSymbol = _tree.Style.ShowBranchLines ? Glyphs.HLine : (Rune)' ';

        if (IsExpanded)
        {
            return _tree.Style.CollapseableSymbol.ToString () ?? leafSymbol.ToString ();
        }

        if (CanExpand ())
        {
            return _tree.Style.ExpandableSymbol.ToString () ?? leafSymbol.ToString ();
        }

        return leafSymbol.ToString ();
    }

    /// <summary>
    ///     Returns the width of the line including prefix and the results of <see cref="TreeView{T}.AspectGetter"/> (the
    ///     line body).
    /// </summary>
    /// <returns></returns>
    public virtual int GetWidth () =>
        GetLinePrefix ().Sum (r => r.GetColumns ())
        + GetExpandableSymbol ().GetColumns ()
        + (_tree.CheckboxMode ? 2 : 0)
        + _tree.AspectGetter (Model).GetColumns ();

    /// <summary>Refreshes cached knowledge in this branch e.g. what children an object has.</summary>
    /// <param name="startAtTop">True to also refresh all <see cref="Parent"/> branches (starting with the root).</param>
    public void Refresh (bool startAtTop)
    {
        // if we must go up and refresh from the top down
        if (startAtTop)
        {
            Parent?.Refresh (true);
        }

        // we don't want to lose the state of our children so lets be selective about how we refresh
        //if we don't know about any children yet just use the normal method
        if (ChildBranches is null)
        {
            ChildBranches = FetchChildren ();
        }
        else
        {
            // we already knew about some children so preserve the state of the old children

            // first gather the new Children
            T [] newChildren = _tree.TreeBuilder?.GetChildren (Model).ToArray () ?? [];

            // Children who no longer appear need to go
            foreach (Branch<T> toRemove in ChildBranches.Where (b => !newChildren.Contains (b.Model)).ToArray ())
            {
                ChildBranches.Remove (toRemove);

                //also if the user has this node selected (its disappearing) so lets change selection to us (the parent object) to be helpful
                if (Equals (_tree.SelectedObject, toRemove.Model))
                {
                    _tree.SelectedObject = Model;
                }
            }

            // New children need to be added
            foreach (T newChild in newChildren)
            {
                Branch<T>? existingBranch = ChildBranches.FirstOrDefault (b => b.Model.Equals (newChild));

                // If we don't know about the child, yet we need a new branch
                if (existingBranch == null)
                {
                    ChildBranches.Add (new Branch<T> (_tree, this, newChild));
                }
                else
                {
                    //we already have this object but update the reference anyway in case Equality match but the references are new
                    existingBranch.Model = newChild;
                }
            }

            // Order the list
            ChildBranches = ChildBranches.OrderBy (b => newChildren.IndexOf (b.Model)).ToList ();
        }
    }

    /// <summary>
    ///     Collapses the current branch and all children branches (even though those branches are no longer visible they
    ///     retain collapse/expansion state).
    /// </summary>
    internal void CollapseAll ()
    {
        Collapse ();

        if (ChildBranches is null)
        {
            return;
        }

        foreach (Branch<T> child in ChildBranches)
        {
            child.CollapseAll ();
        }
    }

    /// <summary>Expands the current branch and all children branches.</summary>
    internal void ExpandAll ()
    {
        Expand ();

        if (ChildBranches is null)
        {
            return;
        }

        foreach (Branch<T> child in ChildBranches)
        {
            child.ExpandAll ();
        }
    }

    /// <summary>
    ///     Gets all characters to render prior to the current branches line.  This includes indentation whitespace and
    ///     any tree branches (if enabled).
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<string> GetLinePrefix ()
    {
        // If not showing line branches or this is a root object.
        if (!_tree.Style.ShowBranchLines)
        {
            for (var i = 0; i < Depth; i++)
            {
                yield return new string (" ");
            }

            yield break;
        }

        // yield indentations with runes appropriate to the state of the parents
        foreach (Branch<T> cur in GetParentBranches ().Reverse ())
        {
            if (cur.IsLast ())
            {
                yield return new string (" ");
            }
            else
            {
                yield return Glyphs.VLine.ToString ();
            }

            yield return new string (" ");
        }

        if (IsLast ())
        {
            yield return Glyphs.LLCorner.ToString ();
        }
        else
        {
            yield return Glyphs.LeftTee.ToString ();
        }
    }

    /// <summary>
    ///     Returns true if the given x offset on the branch line is the +/- symbol.  Returns false if not showing
    ///     expansion symbols or leaf node etc.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    internal bool IsHitOnExpandableSymbol (int x)
    {
        // if leaf node then we cannot expand
        if (!CanExpand ())
        {
            return false;
        }

        // if we could theoretically expand
        if (!IsExpanded && _tree.Style.ExpandableSymbol != null)
        {
            return x == GetLinePrefix ().Count ();
        }

        // if we could theoretically collapse
        if (IsExpanded && _tree.Style.CollapseableSymbol != null)
        {
            return x == GetLinePrefix ().Count ();
        }

        return false;
    }

    /// <summary>Returns true if the given x offset on the branch line is the checkbox glyph.</summary>
    /// <param name="x">The x offset on the branch line.</param>
    /// <returns><see langword="true"/> if checkbox mode is enabled and <paramref name="x"/> hits the checkbox glyph.</returns>
    internal bool IsHitOnCheckbox (int x) => _tree.CheckboxMode && x == GetLinePrefix ().Count () + GetExpandableSymbol ().GetColumns ();

    /// <summary>Calls <see cref="Refresh(bool)"/> on the current branch and all expanded children.</summary>
    internal void Rebuild ()
    {
        Refresh (false);

        // if we know about our children
        if (ChildBranches is null)
        {
            return;
        }

        if (IsExpanded)
        {
            // if we are expanded we need to update the visible children
            foreach (Branch<T> child in ChildBranches)
            {
                child.Rebuild ();
            }
        }
        else
        {
            // we are not expanded so should forget about children because they may not exist anymore
            ChildBranches = null;
        }
    }

    /// <summary>Returns all parents starting with the immediate parent and ending at the root.</summary>
    /// <returns></returns>
    private IEnumerable<Branch<T>> GetParentBranches ()
    {
        Branch<T>? cur = Parent;

        while (cur is { })
        {
            yield return cur;

            cur = cur.Parent;
        }
    }

    /// <summary>
    ///     Returns true if this branch has parents, and it is the last node of its parents branches (or last root of the
    ///     tree).
    /// </summary>
    /// <returns></returns>
    private bool IsLast ()
    {
        if (Parent is null)
        {
            return this == _tree.Roots?.Values.LastOrDefault ();
        }

        Parent.ChildBranches ??= Parent.FetchChildren ();

        return Parent.ChildBranches.LastOrDefault () == this;
    }

    private static Cell NewCell (Attribute attr, string s) => new () { Grapheme = s, Attribute = new Attribute (attr) };

    /// <summary>
    ///     Consumes <paramref name="toSkip"/> display columns from <paramref name="grapheme"/>.
    ///     Returns the grapheme when it is fully visible, leftover blank columns when a wide cluster is split by
    ///     the scroll offset, or <see langword="null"/> when it lies entirely in the skipped region.
    /// </summary>
    /// <param name="grapheme">The grapheme cluster to consider.</param>
    /// <param name="toSkip">Remaining display columns to skip; updated to reflect columns consumed.</param>
    /// <returns>The visible remainder, or <see langword="null"/> if nothing should be drawn.</returns>
    private static string? TakeVisibleGrapheme (string grapheme, ref int toSkip)
    {
        if (toSkip <= 0)
        {
            return grapheme;
        }

        int graphemeColumns = grapheme.GetColumns ();

        if (graphemeColumns <= toSkip)
        {
            toSkip -= graphemeColumns;

            return null;
        }

        int leftover = graphemeColumns - toSkip;
        toSkip = 0;

        return leftover > 0 ? new string (' ', leftover) : null;
    }
}
