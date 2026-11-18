namespace Terminal.Gui.Views;

internal class TreeSelection<T> where T : class
{
    private readonly HashSet<T> _included = new ();

    /// <summary>Creates a new selection between two branches in the tree</summary>
    /// <param name="from"></param>
    /// <param name="toIndex"></param>
    /// <param name="map"></param>
    public TreeSelection (Branch<T> from, int toIndex, IReadOnlyCollection<Branch<T>> map)
    {
        Origin = from;
        _included.Add (Origin.Model);

        int oldIdx = map.IndexOf (from);

        int lowIndex = Math.Min (oldIdx, toIndex);
        int highIndex = Math.Max (oldIdx, toIndex);

        // Select everything between the old and new indexes
        foreach (Branch<T> alsoInclude in map.Skip (lowIndex).Take (highIndex - lowIndex))
        {
            _included.Add (alsoInclude.Model);
        }
    }

    public Branch<T> Origin { get; }
    public bool Contains (T model) => _included.Contains (model);
}
