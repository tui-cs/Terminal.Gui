namespace Terminal.Gui.Views;

public partial class ListView
{
    #region Marking

    /// <summary>
    ///     Gets or sets whether multiple items can be marked simultaneously.
    /// </summary>
    /// <value>
    ///     <see langword="true"/> to allow multiple marks (checkbox style);
    ///     <see langword="false"/> for single mark (radio button style).
    ///     The default is <see langword="false"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, marking one item does not clear others (checkbox behavior).
    ///         When <see langword="false"/>, marking one item clears all other marks (radio button behavior).
    ///     </para>
    ///     <para>
    ///         Marks can exist and be set regardless of the <see cref="ShowMarks"/> property value.
    ///         <see cref="ShowMarks"/> only controls whether mark glyphs are visually displayed.
    ///     </para>
    /// </remarks>
    public bool MarkMultiple
    {
        get;
        set
        {
            field = value;

            if (!field && Source is { })
            {
                // Clear all marks except selected
                for (var i = 0; i < Source.Count; i++)
                {
                    if (Source.IsMarked (i) && SelectedItem.HasValue && i != SelectedItem.Value)
                    {
                        Source.SetMark (i, false);
                    }
                }
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Gets or sets whether mark glyphs (checkboxes/radio buttons) are visually displayed.</summary>
    /// <value>Set to <see langword="true"/> to show mark glyphs; <see langword="false"/> to hide them.</value>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, marks are rendered with glyphs: checkboxes (☑/☐) when
    ///         <see cref="MarkMultiple"/> is <see langword="true"/>, or radio buttons (◉/○) when <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         When <see langword="false"/>, marks can still exist and affect rendering through visual roles
    ///         (e.g., <see cref="VisualRole.Highlight"/>), but no glyphs are shown. The default is <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         The SPACE key toggles marking regardless of this property's value.
    ///     </para>
    /// </remarks>
    public bool ShowMarks
    {
        get;
        set
        {
            field = value;

            // Recalculate content size since mark columns affect effective width
            if (Source is { })
            {
                SetContentSize (new Size (EffectiveMaxItemLength, Source.Count));
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Marks the <see cref="SelectedItem"/> if it is not already marked.</summary>
    /// <returns><see langword="true"/> if the <see cref="SelectedItem"/> was marked.</returns>
    public bool MarkUnmarkSelectedItem ()
    {
        // Allow marking if ShowMarks OR MarkMultiple is true (not in Combination 1)
        if ((!ShowMarks && !MarkMultiple) || Source is null || SelectedItem is null || !UnmarkAllButSelected ())
        {
            return false;
        }

        Source.SetMark (SelectedItem.Value, !Source.IsMarked (SelectedItem.Value));
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     If <see cref="MarkMultiple"/> is <see langword="true"/>, marks or unmarks all items.
    /// </summary>
    /// <param name="mark"><see langword="true"/> marks all items; otherwise unmarks all items.</param>
    /// <returns><see langword="true"/> if marking was successful.</returns>
    public bool MarkAll (bool mark)
    {
        // Only allow MarkAll when multiple marking is enabled
        if (!MarkMultiple)
        {
            return false;
        }

        for (var i = 0; i < Source?.Count; i++)
        {
            Source.SetMark (i, mark);
        }

        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Clears all marks in the data source.
    /// </summary>
    public void UnmarkAll ()
    {
        if (Source is null)
        {
            return;
        }

        for (var i = 0; i < Source.Count; i++)
        {
            Source.SetMark (i, false);
        }

        SetNeedsDraw ();
    }

    /// <summary>
    ///     If marking is enabled (<see cref="ShowMarks"/> OR <see cref="MarkMultiple"/>)
    ///     and <see cref="MarkMultiple"/> is <see langword="false"/>,
    ///     unmarks all marked items other than <see cref="SelectedItem"/>.
    /// </summary>
    /// <returns><see langword="true"/> if unmarking was successful.</returns>
    public bool UnmarkAllButSelected ()
    {
        // Allow marking if ShowMarks OR MarkMultiple is true (not in Combination 1)
        if (!ShowMarks && !MarkMultiple)
        {
            return false;
        }

        if (MarkMultiple)
        {
            return true;
        }

        for (var i = 0; i < Source?.Count; i++)
        {
            if (!Source.IsMarked (i) || i == SelectedItem)
            {
                continue;
            }
            Source.SetMark (i, false);

            return true;
        }

        return true;
    }

    /// <summary>
    ///     Gets all marked item indices from the data source.
    /// </summary>
    /// <returns>An enumerable of marked item indices in ascending order.</returns>
    public IEnumerable<int> GetAllMarkedItems ()
    {
        if (Source is null)
        {
            return [];
        }

        List<int> marked = [];

        for (var i = 0; i < Source.Count; i++)
        {
            if (Source.IsMarked (i))
            {
                marked.Add (i);
            }
        }

        return marked;
    }

    #endregion Marking

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame) => EnsureSelectedItemVisible ();

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? currentFocused, View? newFocused)
    {
        if (newHasFocus && _lastSelectedItem != SelectedItem)
        {
            EnsureSelectedItemVisible ();
        }
    }

    /// <summary>Ensures the selected item is always visible on the screen.</summary>
    public void EnsureSelectedItemVisible ()
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem.Value };
        }
        else if (Viewport.Height > 0 && SelectedItem >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = SelectedItem.Value - Viewport.Height + 1 };
        }
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the specified item is selected (is the <see cref="SelectedItem"/>)
    ///     or marked (via <see cref="IListDataSource.IsMarked"/>).
    /// </summary>
    /// <param name="item">The item index to check.</param>
    /// <returns><see langword="true"/> if the item is selected or marked; otherwise <see langword="false"/>.</returns>
    public bool IsSelectedOrMarked (int item) => item == SelectedItem || (Source?.IsMarked (item) ?? false);

    /// <summary>
    ///     Sets the selected item, optionally extending marking to create a range.
    /// </summary>
    /// <param name="item">The item index to select.</param>
    /// <param name="extendExistingSelection">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends marking from the anchor point to <paramref name="item"/>.
    /// </param>
    public void SetSelection (int item, bool extendExistingSelection)
    {
        if (Source is null || item < 0 || item >= Source.Count)
        {
            return;
        }

        if (!MarkMultiple || !extendExistingSelection)
        {
            // Single-selection mode or not extending: handle marks based on mode
            if (!MarkMultiple && ShowMarks)
            {
                // Radio button mode: clear all marks except the new selection
                for (var i = 0; i < Source.Count; i++)
                {
                    Source.SetMark (i, i == item);
                }
            }
            else if (MarkMultiple && !ShowMarks)
            {
                // Hidden marks mode: clear all marks (transient range selections)
                for (var i = 0; i < Source.Count; i++)
                {
                    Source.SetMark (i, false);
                }
            }

            // Checkbox mode (ShowMarks=true, MarkMultiple=true): don't touch marks (persistent checkboxes)

            _selectionAnchor = item;
        }
        else if (extendExistingSelection && _selectionAnchor.HasValue && MarkMultiple)
        {
            // Multi-select mode: mark range from anchor to item
            int start = Math.Min (_selectionAnchor.Value, item);
            int end = Math.Max (_selectionAnchor.Value, item);

            for (int i = start; i <= end; i++)
            {
                Source.SetMark (i, true);
            }

            // Note: anchor stays at original position
        }

        SelectedItem = item;
        EnsureSelectedItemVisible ();
        SetNeedsDraw ();
    }

    private bool? HandleSelectAll (ICommandContext? ctx)
    {
        if (ctx?.Binding is not KeyBinding keyBinding)
        {
            return false;
        }

        return keyBinding.Data is { } && MarkAll ((bool)keyBinding.Data);
    }

    private int? _lastSelectedItem;
    private int? _selectionAnchor;

    #region IValue<int?> Implementation

    /// <summary>Gets or sets the index of the currently selected item.</summary>
    /// <value>The index of selected item or <see langword="null"/> if no item is selected.</value>
    public int? Value { get => SelectedItem; set => SelectedItem = value; }

    /// <summary>Gets index of the currently selected item.</summary>
    /// <value>The index of selected item or <see langword="null"/> if no item is selected.</value>
    object? IValue.GetValue () => SelectedItem;

    /// <summary>
    ///     Called when the <see cref="ListView"/> <see cref="Value"/>/<see cref="SelectedItem"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<int?> args) => false;

    /// <summary>
    ///     Raised when the <see cref="ListView"/> <see cref="Value"/>/<see cref="SelectedItem"/> is changing.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="ListView"/> <see cref="Value"/>/<see cref="SelectedItem"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<int?> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;

    /// <inheritdoc />
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    #endregion

    /// <summary>This is a convenience property that is an alias for <see cref="Value"/>. Get or set the index of the currently selected item.</summary>
    /// <value>The index of selected item or <see langword="null"/> if no item is selected.</value>
    public int? SelectedItem
    {
        get;
        set
        {
            // No source: only "no selection" is valid.
            if (Source is null)
            {
                if (value is null)
                {
                    // fall through and clear (or set field = null directly)
                }
                else
                {
                    return; // cannot select an index with no source
                }
            }
            else if (value.HasValue && (value < 0 || value >= Source.Count))
            {
                throw new ArgumentException (@"SelectedItem must be greater than or equal to 0 and less than the number of items.");
            }

            int? oldValue = field;

            if (oldValue == value)
            {
                return;
            }

            ValueChangingEventArgs<int?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            field = value;
            SetNeedsDraw ();

            // Initialize selection anchor when no anchor is set
            if (!_selectionAnchor.HasValue && value.HasValue)
            {
                _selectionAnchor = value.Value;
            }

            if (SelectedItem != _lastSelectedItem)
            {
                _lastSelectedItem = SelectedItem;
                EnsureSelectedItemVisible ();
            }

            ValueChangedEventArgs<int?> changedArgs = new (oldValue, field);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
            ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, field));
        }
    }
}
