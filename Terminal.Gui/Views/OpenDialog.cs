// 
// FileDialog.cs: File system dialogs for open and save
//
// TODO:
//   * Add directory selector
//   * Implement subclasses
//   * Figure out why message text does not show
//   * Remove the extra space when message does not show
//   * Use a line separator to show the file listing, so we can use same colors as the rest
//   * DirListView: Add mouse support

using System;
using System.Collections.Generic;
using NStack;
using System.IO;
using System.Linq;
using Terminal.Gui.Resources;
using System.Collections.ObjectModel;

namespace Terminal.Gui {

	/// <summary>
	/// Determine which <see cref="System.IO"/> type to open.
	/// </summary>
	public enum OpenMode {
		/// <summary>
		/// Opens only file or files.
		/// </summary>
		File,
		/// <summary>
		/// Opens only directory or directories.
		/// </summary>
		Directory,
		/// <summary>
		/// Opens files and directories.
		/// </summary>
		Mixed
	}

	/// <summary>
	/// The <see cref="OpenDialog"/>provides an interactive dialog box for users to select files or directories.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The open dialog can be used to select files for opening, it can be configured to allow
	///   multiple items to be selected (based on the AllowsMultipleSelection) variable and
	///   you can control whether this should allow files or directories to be selected.
	/// </para>
	/// <para>
	///   To use, create an instance of <see cref="OpenDialog"/>, and pass it to
	///   <see cref="Application.Run(Func{Exception, bool})"/>. This will run the dialog modally,
	///   and when this returns, the list of files will be available on the <see cref="FilePaths"/> property.
	/// </para>
	/// <para>
	/// To select more than one file, users can use the spacebar, or control-t.
	/// </para>
	/// </remarks>
	public class OpenDialog : FileDialog {		

		/// <summary>
		/// Initializes a new <see cref="OpenDialog"/>.
		/// </summary>
		public OpenDialog () : this (title: string.Empty) { }

		/// <summary>
		/// Initializes a new <see cref="OpenDialog"/>.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="allowedTypes">The allowed types.</param>
		/// <param name="openMode">The open mode.</param>
		public OpenDialog (ustring title, List<IAllowedType> allowedTypes = null, OpenMode openMode = OpenMode.File)
		{
			this.OpenMode = openMode;
			Title = title;
			Style.OkButtonText = openMode == OpenMode.File ? Strings.fdOpen : openMode == OpenMode.Directory ? Strings.fdSelectFolder : Strings.fdSelectMixed;
			
			if (allowedTypes != null) {
				AllowedTypes = allowedTypes;
			}
		}
		/// <summary>
		/// Returns the selected files, or an empty list if nothing has been selected
		/// </summary>
		/// <value>The file paths.</value>
		public IReadOnlyList<string> FilePaths {
			get => Canceled ? Enumerable.Empty<string> ().ToList().AsReadOnly()
				: AllowsMultipleSelection ? base.MultiSelected : new ReadOnlyCollection<string>(new [] { Path });
		}
	}
}
