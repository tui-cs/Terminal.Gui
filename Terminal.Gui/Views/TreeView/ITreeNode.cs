namespace Terminal.Gui.Views;

/// <summary>
///     Interface to implement when you want the regular (non-generic) <see cref="TreeView"/> to automatically
///     determine children for your class (without having to specify an <see cref="ITreeBuilder{T}"/>)
/// </summary>
public interface ITreeNode
{
    /// <summary>The children of your class which should be rendered underneath it when expanded.</summary>
    IList<ITreeNode> Children { get; }

    /// <summary>Optionally allows you to store some custom data/class here.</summary>
    object? Tag { get; set; }

    /// <summary>Text to display when rendering the node</summary>
    string Text { get; set; }
}
