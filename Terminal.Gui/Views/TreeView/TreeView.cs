// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

/// <summary>
///     Convenience implementation of generic <see cref="TreeView{T}"/> for any tree were all nodes implement
///     <see cref="ITreeNode"/>. <a href="../docs/treeview.md">See TreeView Deep Dive for more information</a>.
/// </summary>
/// <remarks>
/// <img src="../images/views/TreeView.gif" alt="TreeView demo"/>
/// </remarks>
public class TreeView : TreeView<ITreeNode>, IDesignable
{
    /// <summary>
    ///     Creates a new instance of the tree control and initialises
    ///     <see cref="TreeBuilder{T}"/> with default <see cref="ITreeNode"/> based builder.
    /// </summary>
    public TreeView ()
    {
        CanFocus = true;

        TreeBuilder = new TreeNodeBuilder ();
        AspectGetter = o => o.Text;
    }

    /// <inheritdoc/>
    public string? GetDemoKeyStrokes () => "wait:500,CursorDown,wait:400,CursorDown,wait:400,CursorDown,wait:400,Space,wait:400,Space,wait:400,CursorDown,wait:400,Space,wait:800";

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        TreeNode root1 = new () { Text = "Root1" };
        root1.Children.Add (new TreeNode { Text = "Child1.1" });
        root1.Children.Add (new TreeNode { Text = "Child1.2" });

        TreeNode root2 = new () { Text = "Root2" };
        TreeNode child21 = new () { Text = "Child2.1" };
        child21.Children.Add (new TreeNode { Text = "Child2.1.1" });
        child21.Children.Add (new TreeNode { Text = "Child2.1.2" });
        root2.Children.Add (child21);
        root2.Children.Add (new TreeNode { Text = "Child2.2" });

        AddObject (root1);
        AddObject (root2);

        ExpandAll ();

        return true;
    }
}
