using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace Terminal.Gui {

	class FileDialogTreeBuilder : ITreeBuilder<object> {
		readonly FileDialog _dlg;

		public FileDialogTreeBuilder(FileDialog dlg)
		{
			_dlg = dlg;
		}

		public bool SupportsCanExpand => true;

		public bool CanExpand (object toExpand)
		{
			return this.TryGetDirectories (NodeToDirectory (toExpand)).Any ();
		}

		public IEnumerable<object> GetChildren (object forObject)
		{
			return this.TryGetDirectories (NodeToDirectory (forObject));
		}

		internal static IDirectoryInfo NodeToDirectory (object toExpand)
		{
			return toExpand is FileDialogRootTreeNode f ? f.Path : (IDirectoryInfo)toExpand;
		}

		internal string AspectGetter(object o)
		{
			string icon;
			string name;

			if(o is FileDialogRootTreeNode r)
			{
				icon = _dlg.Style.IconGetter.Invoke(
					new FileDialogIconGetterArgs(_dlg, r.Path, FileDialogIconGetterContext.Tree));
				name = r.DisplayName;
			}
			else
			{
				var dir  = (IDirectoryInfo)o;
				icon = _dlg.Style.IconGetter.Invoke(
					new FileDialogIconGetterArgs(_dlg, dir, FileDialogIconGetterContext.Tree));
				name = dir.Name;
			}

			return icon + name;
		}

		private IEnumerable<IDirectoryInfo> TryGetDirectories (IDirectoryInfo directoryInfo)
		{
			try {
				return directoryInfo.EnumerateDirectories ();
			} catch (Exception) {

				return Enumerable.Empty<IDirectoryInfo> ();
			}
		}

	}
}