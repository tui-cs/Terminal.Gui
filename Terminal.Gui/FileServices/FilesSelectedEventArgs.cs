using System;

namespace Terminal.Gui {
	/// <summary>
	/// Event args for the <see cref="FileDialog.FilesSelected"/> event
	/// </summary>
	public class FilesSelectedEventArgs : EventArgs {
		/// <summary>
		/// Set to true if you want to prevent the selection
		/// going ahead (this will leave the <see cref="FileDialog"/>
		/// still showing).
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// The dialog where the choice is being made.  Use <see cref="FileDialog.Path"/>
		/// and/or <see cref="FileDialog.MultiSelected"/> to evaluate the users choice.
		/// </summary>
		public FileDialog Dialog { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="FilesSelectedEventArgs"/>
		/// </summary>
		/// <param name="dialog"></param>
		public FilesSelectedEventArgs (FileDialog dialog)
		{
			Dialog = dialog;
		}
	}
}