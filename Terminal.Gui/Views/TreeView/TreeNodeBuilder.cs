using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="ITreeBuilder{T}"/> implementation for <see cref="ITreeNode"/> objects
	/// </summary>
	public class TreeNodeBuilder : TreeBuilder<ITreeNode> {

		/// <summary>
		/// Initialises a new instance of builder for any model objects of 
		/// Type <see cref="ITreeNode"/>
		/// </summary>
		public TreeNodeBuilder () : base (false)
		{

		}

		/// <summary>
		/// Returns <see cref="ITreeNode.Children"/> from <paramref name="model"/>
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		public override IEnumerable<ITreeNode> GetChildren (ITreeNode model)
		{
			return model.Children;
		}
	}
}