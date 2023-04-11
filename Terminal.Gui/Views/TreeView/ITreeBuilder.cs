using System.Collections.Generic;

namespace Terminal.Gui {
	/// <summary>
	/// Interface for supplying data to a <see cref="TreeView{T}"/> on demand as root level nodes
	/// are expanded by the user
	/// </summary>
	public interface ITreeBuilder<T> {
		/// <summary>
		/// Returns true if <see cref="CanExpand"/> is implemented by this class
		/// </summary>
		/// <value></value>
		bool SupportsCanExpand { get; }

		/// <summary>
		/// Returns true/false for whether a model has children.  This method should be implemented
		/// when <see cref="GetChildren"/> is an expensive operation otherwise 
		/// <see cref="SupportsCanExpand"/> should return false (in which case this method will not
		/// be called)
		/// </summary>
		/// <remarks>Only implement this method if you have a very fast way of determining whether 
		/// an object can have children e.g. checking a Type (directories can always be expanded)
		/// </remarks>
		/// <param name="toExpand"></param>
		/// <returns></returns>
		bool CanExpand (T toExpand);

		/// <summary>
		/// Returns all children of a given <paramref name="forObject"/> which should be added to the 
		/// tree as new branches underneath it
		/// </summary>
		/// <param name="forObject"></param>
		/// <returns></returns>
		IEnumerable<T> GetChildren (T forObject);
	}
}