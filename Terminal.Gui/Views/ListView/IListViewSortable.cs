namespace Terminal.Gui.Views;

/// <summary>Optional sorting support <see cref="ListView"/> invokes after a column-header click.</summary>
internal interface IListViewSortable
{
    /// <summary>Gets whether a column sort is active.</summary>
    bool IsSorted { get; }

    /// <summary>Clears the active sort and restores source order.</summary>
    void ClearSort ();

    /// <summary>Returns the display index for an item that was selected before a source collection change.</summary>
    /// <param name="selectedItem">The display index before the collection change.</param>
    /// <returns>The matching display index, or <see langword="null"/> when no item remains selected.</returns>
    int? RestoreSelection (int? selectedItem);

    /// <summary>Sorts the column at a header position and returns the selected item's new display index.</summary>
    /// <param name="listView">The list view that owns the source.</param>
    /// <param name="x">The mouse position relative to the list view.</param>
    /// <param name="selectedItem">The selected display index before sorting.</param>
    /// <returns>The selected item's new display index.</returns>
    int? SortHeaderAt (ListView listView, int x, int? selectedItem);
}
