using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace Terminal.Gui {

	internal class FileDialogHistory {
		private Stack<FileDialogState> back = new Stack<FileDialogState> ();
		private Stack<FileDialogState> forward = new Stack<FileDialogState> ();
		private FileDialog dlg;

		public FileDialogHistory (FileDialog dlg)
		{
			this.dlg = dlg;
		}

		public bool Back ()
		{

			IDirectoryInfo goTo = null;
			FileSystemInfoStats restoreSelection = null;
			string restorePath = null;

			if (this.CanBack ()) {

				var backTo = this.back.Pop ();
				goTo = backTo.Directory;
				restoreSelection = backTo.Selected;
				restorePath = backTo.Path;

			} else if (this.CanUp ()) {
				goTo = this.dlg.State?.Directory.Parent;
			}

			// nowhere to go
			if (goTo == null) {
				return false;
			}

			this.forward.Push (this.dlg.State);
			this.dlg.PushState (goTo, false, true, false, restorePath);


			if (restoreSelection != null) {
				this.dlg.RestoreSelection (restoreSelection.FileSystemInfo);
			}

			return true;
		}

		internal bool CanBack ()
		{
			return this.back.Count > 0;
		}

		internal bool Forward ()
		{
			if (this.forward.Count > 0) {

				this.dlg.PushState (this.forward.Pop ().Directory, true, true, false);
				return true;
			}

			return false;
		}

		internal bool Up ()
		{
			var parent = this.dlg.State?.Directory.Parent;
			if (parent != null) {

				this.back.Push (new FileDialogState (parent, this.dlg));
				this.dlg.PushState (parent, false);
				return true;
			}

			return false;
		}

		internal bool CanUp ()
		{
			return this.dlg.State?.Directory.Parent != null;
		}

		internal void Push (FileDialogState state, bool clearForward)
		{
			if (state == null) {
				return;
			}

			// if changing to a new directory push onto the Back history
			if (this.back.Count == 0 || this.back.Peek ().Directory.FullName != state.Directory.FullName) {

				this.back.Push (state);
				if (clearForward) {
					this.ClearForward ();
				}
			}
		}

		internal bool CanForward ()
		{
			return this.forward.Count > 0;
		}

		internal void ClearForward ()
		{
			this.forward.Clear ();
		}
	}
}