namespace Terminal.Gui.Views;

/// <summary>Implementation of <see cref="ITreeBuilder{T}"/> that uses user defined functions.</summary>
public class DelegateTreeBuilder<T> : TreeBuilder<T>
{
    private readonly Func<T, bool> _canExpand;
    private readonly Func<T, IEnumerable<T>> _childGetter;

    /// <summary>
    ///     Constructs an implementation of <see cref="ITreeBuilder{T}"/> that calls the user defined method
    ///     <paramref name="childGetter"/> to determine children and <paramref name="canExpand"/> to determine expandability.
    /// </summary>
    /// <param name="childGetter">Delegate that returns the children of a given object.</param>
    /// <param name="canExpand">Delegate that returns whether a given object can be expanded.</param>
    public DelegateTreeBuilder (Func<T, IEnumerable<T>> childGetter, Func<T, bool> canExpand) : base (true)
    {
        _childGetter = childGetter;
        _canExpand = canExpand;
    }

    /// <summary>Returns whether a node can be expanded based on the delegate passed during construction.</summary>
    /// <param name="toExpand">The object to check for expandability.</param>
    /// <returns>True if the object can be expanded.</returns>
    public override bool CanExpand (T toExpand) => _canExpand.Invoke (toExpand);

    /// <summary>Returns children using the delegate method passed during construction.</summary>
    /// <param name="forObject">The object whose children should be returned.</param>
    /// <returns>The child objects.</returns>
    public override IEnumerable<T> GetChildren (T forObject) => _childGetter.Invoke (forObject);
}
