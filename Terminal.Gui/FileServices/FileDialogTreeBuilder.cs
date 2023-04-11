using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Terminal.Gui {

	class FileDialogTreeBuilder : ITreeBuilder<object> {
		public bool SupportsCanExpand => true;

		public bool CanExpand (object toExpand)
		{
			return this.TryGetDirectories (NodeToDirectory (toExpand)).Any ();
		}

		public IEnumerable<object> GetChildren (object forObject)
		{
			return this.TryGetDirectories (NodeToDirectory (forObject));
		}

		internal static DirectoryInfo NodeToDirectory (object toExpand)
		{
			return toExpand is FileDialogRootTreeNode f ? f.Path : (DirectoryInfo)toExpand;
		}

		private IEnumerable<DirectoryInfo> TryGetDirectories (DirectoryInfo directoryInfo)
		{
			try {
				return directoryInfo.EnumerateDirectories ();
			} catch (Exception) {

				return Enumerable.Empty<DirectoryInfo> ();
			}
		}

	}
}