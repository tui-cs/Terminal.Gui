using System.IO.Abstractions;

namespace Terminal.Gui {

	/// <summary>
	/// Arguments for the <see cref="FileDialogStyle.IconGetter"/> delegate
	/// </summary>
	public class FileDialogIconGetterArgs {

		/// <summary>
		/// Creates a new instance of the class
		/// </summary>
		public FileDialogIconGetterArgs (FileDialog fileDialog, IFileSystemInfo file, FileDialogIconGetterContext context)
		{
			FileDialog = fileDialog;
			File = file;
			Context = context;
		}

		/// <summary>
		/// Gets the dialog that requires the icon.
		/// </summary>
		public FileDialog FileDialog { get; }

		/// <summary>
		/// Gets the file/folder for which the icon is required.
		/// </summary>
		public IFileSystemInfo File { get; }

		/// <summary>
		/// Gets the context in which the icon will be used in.
		/// </summary>
		public FileDialogIconGetterContext Context { get; }
	}
}