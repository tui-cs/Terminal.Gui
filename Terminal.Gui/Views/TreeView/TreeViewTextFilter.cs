namespace Terminal.Gui.Views;

/// <summary>
///     <see cref="ITreeViewFilter{T}"/> implementation which searches the <see cref="TreeView{T}.AspectGetter"/> of
///     the model for the given <see cref="Text"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class TreeViewTextFilter<T> : ITreeViewFilter<T> where T : class
{
    private readonly TreeView<T> _forTree;

    /// <summary>
    ///     Creates a new instance of the filter for use with <paramref name="forTree"/>. Set <see cref="Text"/> to begin
    ///     filtering.
    /// </summary>
    /// <param name="forTree">The tree view this filter applies to.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public TreeViewTextFilter (TreeView<T> forTree) => _forTree = forTree ?? throw new ArgumentNullException (nameof (forTree));

    /// <summary>The case sensitivity of the search match. Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/>.</summary>
    public StringComparison Comparer { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>The text that will be searched for in the <see cref="TreeView{T}"/></summary>
    public string Text
    {
        get;
        set
        {
            field = value;
            RefreshTreeView ();
        }
    } = string.Empty;

    /// <summary>
    ///     Returns <see langword="true"/> if there is no <see cref="Text"/> or the text matches the
    ///     <see cref="TreeView{T}.AspectGetter"/> of the <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The object to test for a match.</param>
    /// <returns>True if the model matches the filter or no filter text is set.</returns>
    public bool IsMatch (T model)
    {
        if (string.IsNullOrWhiteSpace (Text))
        {
            return true;
        }

        return _forTree.AspectGetter (model)?.IndexOf (Text, Comparer) != -1;
    }

    private void RefreshTreeView ()
    {
        _forTree.InvalidateLineMap ();
        _forTree.SetNeedsDraw ();
    }
}
