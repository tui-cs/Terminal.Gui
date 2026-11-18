namespace Terminal.Gui.Views;

/// <summary>Describes a tree node check state change.</summary>
/// <typeparam name="T">The type of object represented by nodes in the tree.</typeparam>
public class CheckedChangedEventArgs<T> (TreeView<T> tree, T @object, CheckState oldValue, CheckState newValue) : EventArgs where T : class
{
    /// <summary>The tree whose node check state changed.</summary>
    public TreeView<T> Tree { get; } = tree;

    /// <summary>The object whose check state changed.</summary>
    public T Object { get; } = @object;

    /// <summary>The previous check state.</summary>
    public CheckState OldValue { get; } = oldValue;

    /// <summary>The new check state.</summary>
    public CheckState NewValue { get; } = newValue;
}
