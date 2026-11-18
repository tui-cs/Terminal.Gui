namespace Terminal.Gui.Views;

/// <summary>
///     Implementation of <see cref="CheckBoxTableSourceWrapperBase"/> which records toggled rows by a property on row
///     objects.
/// </summary>
public class CheckBoxTableSourceWrapperByObject<T> : CheckBoxTableSourceWrapperBase
{
    private readonly Func<T, bool> _getter;
    private readonly Action<T, bool> _setter;
    private readonly IEnumerableTableSource<T> _toWrap;

    /// <summary>Creates a new instance of the class wrapping the collection <paramref name="toWrap"/>.</summary>
    /// <param name="tableView">The table you will use the source with.</param>
    /// <param name="toWrap">The collection of objects you will record checked state for</param>
    /// <param name="getter">Delegate method for retrieving checked state from your objects of type <typeparamref name="T"/>.</param>
    /// <param name="setter">Delegate method for setting new checked states on your objects of type <typeparamref name="T"/>.</param>
    public CheckBoxTableSourceWrapperByObject (TableView tableView, IEnumerableTableSource<T> toWrap, Func<T, bool> getter, Action<T, bool> setter) :
        base (tableView, toWrap)
    {
        _toWrap = toWrap;
        _getter = getter;
        _setter = setter;
    }

    /// <inheritdoc/>
    protected override void ClearAllToggles ()
    {
        foreach (T e in _toWrap.GetAllObjects ())
        {
            _setter (e, false);
        }
    }

    /// <inheritdoc/>
    protected override bool IsChecked (int row) => _getter (_toWrap.GetObjectOnRow (row));

    /// <inheritdoc/>
    protected override void ToggleAllRows () => ToggleRows (Enumerable.Range (0, _toWrap.Rows).ToArray ());

    /// <inheritdoc/>
    protected override void ToggleRow (int row)
    {
        T d = _toWrap.GetObjectOnRow (row);
        _setter (d, !_getter (d));
    }

    /// <inheritdoc/>
    protected override void ToggleRows (int [] range)
    {
        // if all are ticked untick them
        if (range.All (IsChecked))
        {
            // select none
            foreach (int r in range)
            {
                _setter (_toWrap.GetObjectOnRow (r), false);
            }
        }
        else
        {
            // otherwise tick all
            foreach (int r in range)
            {
                _setter (_toWrap.GetObjectOnRow (r), true);
            }
        }
    }
}
