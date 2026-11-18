namespace Terminal.Gui.Views;

/// <summary>Simple class for representing nodes, use with regular (non-generic) <see cref="TreeView"/>.</summary>
public class TreeNode : ITreeNode
{
    /// <summary>Initialises a new instance with no <see cref="Text"/>.</summary>
    public TreeNode () { }

    /// <summary>Children of the current node.</summary>
    public virtual IList<ITreeNode> Children { get; set; } = new List<ITreeNode> ();

    /// <summary>Text to display in tree node for current entry.</summary>
    public virtual string Text { get; set; } = "Unnamed Node";

    /// <summary>Optionally allows you to store some custom data/class here.</summary>
    public virtual object? Tag { get; set; }

    /// <summary>Returns <see cref="Text"/>.</summary>
    public override string ToString () => Text;
}
