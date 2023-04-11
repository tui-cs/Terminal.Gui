using System.Collections.Generic;

namespace Terminal.Gui {
		
	/// <summary>
	/// Interface to implement when you want the regular (non generic) <see cref="TreeView"/>
	/// to automatically determine children for your class (without having to specify 
	/// an <see cref="ITreeBuilder{T}"/>)
	/// </summary>
	public interface ITreeNode {
		/// <summary>
		/// Text to display when rendering the node
		/// </summary>
		string Text { get; set; }

		/// <summary>
		/// The children of your class which should be rendered underneath it when expanded
		/// </summary>
		/// <value></value>
		IList<ITreeNode> Children { get; }

		/// <summary>
		/// Optionally allows you to store some custom data/class here.
		/// </summary>
		object Tag { get; set; }
	}

	/// <summary>
	/// Simple class for representing nodes, use with regular (non generic) <see cref="TreeView"/>.
	/// </summary>
	public class TreeNode : ITreeNode {
		/// <summary>
		/// Children of the current node
		/// </summary>
		/// <returns></returns>
		public virtual IList<ITreeNode> Children { get; set; } = new List<ITreeNode> ();

		/// <summary>
		/// Text to display in tree node for current entry
		/// </summary>
		/// <value></value>
		public virtual string Text { get; set; }

		/// <summary>
		/// Optionally allows you to store some custom data/class here.
		/// </summary>
		public object Tag { get; set; }

		/// <summary>
		/// returns <see cref="Text"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return Text ?? "Unamed Node";
		}

		/// <summary>
		/// Initialises a new instance with no <see cref="Text"/>
		/// </summary>
		public TreeNode ()
		{

		}
		/// <summary>
		/// Initialises a new instance and sets starting <see cref="Text"/>
		/// </summary>
		public TreeNode (string text)
		{
			Text = text;
		}
	}
}