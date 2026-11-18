namespace Terminal.Gui.Views;

/// <summary>Event args for object activation in <see cref="TreeView{T}"/></summary>
/// <typeparam name="T"></typeparam>
public class ObjectActivatedEventArgs<T> where T : class
{
    /// <summary>Creates a new instance documenting activation of the <paramref name="activated"/> object</summary>
    /// <param name="tree">Tree in which the activation is happening</param>
    /// <param name="activated">What object is being activated</param>
    public ObjectActivatedEventArgs (TreeView<T> tree, T activated)
    {
        Tree = tree;
        ActivatedObject = activated;
    }

    /// <summary>The object that was selected at the time of activation.</summary>
    public T ActivatedObject { get; }

    /// <summary>The tree in which the activation occurred.</summary>
    public TreeView<T> Tree { get; }
}
