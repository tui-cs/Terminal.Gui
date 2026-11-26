namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>Changes the <see cref="SelectedItem"/> to the next item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    /// <remarks>
    ///     <para>
    ///         When the selection is already at the last item and <see cref="View.TabStop"/> is
    ///         <see cref="TabBehavior.NoStop"/>, the selection wraps around to the first item.
    ///         Otherwise the method returns <see langword="false"/> without changing the selection.
    ///     </para>
    /// </remarks>
    public bool MoveDown (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false; //Nothing for us to move to
        }

        int newItem;

        if (SelectedItem is null || SelectedItem >= Source.Count)
        {
            // If SelectedItem is null or for some reason we are currently outside the
            // valid values range, we should select the first or bottommost valid value.
            // This can occur if the backing data source changes.
            newItem = SelectedItem is null ? 0 : Source.Count - 1;
        }
        else if (SelectedItem + 1 < Source.Count)
        {
            // Can move down by one.
            newItem = SelectedItem.Value + 1;
        }
        else if (Viewport.Height > 0 && SelectedItem >= Viewport.Y + PageSize)
        {
            // Just scroll viewport
            Viewport = Viewport with { Y = Source.Count - PageSize };

            return true;
        }
        else if (TabStop == TabBehavior.NoStop)
        {
            // Wrap to top
            newItem = 0;
        }
        else
        {
            // Already at bottom
            return false;
        }

        SetSelection (newItem, extend);

        if (SelectedItem >= Viewport.Y + PageSize)
        {
            Viewport = Viewport with { Y = Viewport.Y + 1 };
        }
        else if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to last item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MoveEnd (bool extend = false)
    {
        if (Source is not { Count: > 0 } || SelectedItem == Source.Count - 1)
        {
            return true;
        }

        int newItem = Source.Count - 1;
        SetSelection (newItem, extend);

        if (Viewport.Y + SelectedItem > PageSize - 1)
        {
            Viewport = Viewport with
            {
                Y = SelectedItem < PageSize - 1
                        ? Math.Max (PageSize - SelectedItem!.Value + 1, 0)
                        : Math.Max (SelectedItem!.Value - PageSize + 1, 0)
            };

            EnsureSelectedItemVisible ();
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the first item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MoveHome (bool extend = false)
    {
        if (SelectedItem == 0)
        {
            return true;
        }

        SetSelection (0, extend);
        Viewport = Viewport with { Y = SelectedItem!.Value };

        return true;
    }

    /// <summary>
    ///     Changes the <see cref="SelectedItem"/> to the item just below the bottom of the visible list, scrolling if
    ///     needed.
    /// </summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MovePageDown (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false;
        }

        int newItem = (SelectedItem ?? 0) + PageSize;

        if (newItem >= Source.Count)
        {
            newItem = Source.Count - 1;
        }

        if (newItem == SelectedItem)
        {
            return true;
        }

        SetSelection (newItem, extend);

        if (Source.Count >= PageSize)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value };
        }
        else
        {
            Viewport = Viewport with { Y = 0 };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the item at the top of the visible list.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MovePageUp (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false;
        }

        int newItem = (SelectedItem ?? 0) - PageSize;

        if (newItem < 0)
        {
            newItem = 0;
        }

        if (newItem == SelectedItem || !(newItem < Source?.Count))
        {
            return true;
        }

        SetSelection (newItem, extend);
        Viewport = Viewport with { Y = SelectedItem!.Value };

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the previous item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    /// <remarks>
    ///     <para>
    ///         When the selection is already at the first item and <see cref="View.TabStop"/> is
    ///         <see cref="TabBehavior.NoStop"/>, the selection wraps around to the last item.
    ///         Otherwise the method returns <see langword="false"/> without changing the selection.
    ///     </para>
    /// </remarks>
    public bool MoveUp (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false; //Nothing for us to move to
        }

        int newItem;

        if (SelectedItem is null || SelectedItem >= Source.Count)
        {
            // If SelectedItem is null or for some reason we are currently outside the
            // valid values range, we should select the bottommost valid value.
            // This can occur if the backing data source changes.
            newItem = Source.Count - 1;
        }
        else if (SelectedItem > 0)
        {
            newItem = SelectedItem.Value - 1;

            if (newItem > Source.Count)
            {
                newItem = Source.Count - 1;
            }
        }
        else if (SelectedItem < Viewport.Y)
        {
            // Just scroll viewport
            Viewport = Viewport with { Y = SelectedItem.Value };

            return true;
        }
        else if (TabStop == TabBehavior.NoStop)
        {
            // Wrap to bottom
            newItem = Source.Count - 1;
        }
        else
        {
            // Already at top
            return false;
        }

        SetSelection (newItem, extend);

        if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value };
        }
        else if (SelectedItem > Viewport.Y + PageSize)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value - PageSize + 1 };
        }

        return true;
    }
}
