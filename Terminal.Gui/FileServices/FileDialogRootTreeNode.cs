using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace Terminal.Gui {

	/// <summary>
	/// Delegate for providing an implementation that returns all <see cref="FileDialogRootTreeNode"/>
	/// that should be shown in a <see cref="FileDialog"/> (in the collapse-able tree area of the dialog).
	/// </summary>
	/// <returns></returns>
	public delegate IEnumerable<FileDialogRootTreeNode> FileDialogTreeRootGetter ();

	/// <summary>
	/// Describes a top level directory that should be offered to the user in the
	/// tree view section of a <see cref="FileDialog"/>.  For example "Desktop",
	/// "Downloads", "Documents" etc.
	/// </summary>
	public class FileDialogRootTreeNode {

		/// <summary>
		/// Creates a new instance of the <see cref="FileDialogRootTreeNode"/> class
		/// </summary>
		/// <param name="displayName"></param>
		/// <param name="path"></param>
		public FileDialogRootTreeNode (string displayName, IDirectoryInfo path)
		{
			this.DisplayName = displayName;
			this.Path = path;
		}

		/// <summary>
		/// Gets the text that should be displayed in the tree for this item.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Gets the path that should be shown/explored when selecting this node
		/// of the tree.
		/// </summary>
		public IDirectoryInfo Path { get; }

		/// <summary>
		/// Returns a string representation of this instance (<see cref="DisplayName"/>).
		/// </summary>
		/// <returns></returns>
		public override string ToString ()
		{
			return this.DisplayName;
		}
	}
}