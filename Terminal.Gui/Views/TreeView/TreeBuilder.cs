namespace Terminal.Gui.Views;

/// <summary>Abstract implementation of <see cref="ITreeBuilder{T}"/>.</summary>
public abstract class TreeBuilder<T> : ITreeBuilder<T>
{
    /// <summary>Constructs base and initializes <see cref="SupportsCanExpand"/>.</summary>
    /// <param name="supportsCanExpand">Pass true if you intend to implement <see cref="CanExpand(T)"/> otherwise false.</param>
    protected TreeBuilder (bool supportsCanExpand) => SupportsCanExpand = supportsCanExpand;

    /// <inheritdoc/>
    public bool SupportsCanExpand { get; protected set; }

    /// <summary>
    ///     Override this method to return a rapid answer as to whether <see cref="GetChildren(T)"/> returns results.  If
    ///     you are implementing this method ensure you passed true in base constructor or set <see cref="SupportsCanExpand"/>.
    /// </summary>
    /// <param name="toExpand">The object to check for expandability.</param>
    /// <returns>True if the object has children that can be expanded.</returns>
    public virtual bool CanExpand (T toExpand) => GetChildren (toExpand).Any ();

    /// <inheritdoc/>
    public abstract IEnumerable<T> GetChildren (T forObject);
}
