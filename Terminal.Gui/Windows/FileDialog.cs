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

namespace Terminal.Gui {
	internal class DirListView : View {
		int top, selected;
		DirectoryInfo dirInfo;
		FileSystemWatcher watcher;
		List<(string, bool, bool)> infos;
		internal bool canChooseFiles = true;
		internal bool canChooseDirectories = false;
		internal bool allowsMultipleSelection = false;
		FileDialog host;

		public DirListView (FileDialog host)
		{
			infos = new List<(string, bool, bool)> ();
			CanFocus = true;
			this.host = host;
		}

		bool IsAllowed (FileSystemInfo fsi)
		{
			if (fsi.Attributes.HasFlag (FileAttributes.Directory))
				return true;
			if (allowedFileTypes == null)
				return true;
			foreach (var ft in allowedFileTypes)
				if (fsi.Name.EndsWith (ft, StringComparison.InvariantCultureIgnoreCase) || ft == ".*")
					return true;
			return false;
		}

		internal bool Reload (ustring value = null)
		{
			bool valid = false;
			try {
				dirInfo = new DirectoryInfo (value == null ? directory.ToString () : value.ToString ());

				// Dispose of the old watcher
				watcher?.Dispose ();

				watcher = new FileSystemWatcher (dirInfo.FullName);
				watcher.NotifyFilter = NotifyFilters.Attributes
				 | NotifyFilters.CreationTime
				 | NotifyFilters.DirectoryName
				 | NotifyFilters.FileName
				 | NotifyFilters.LastAccess
				 | NotifyFilters.LastWrite
				 | NotifyFilters.Security
				 | NotifyFilters.Size;
				watcher.Changed += Watcher_Changed;
				watcher.Created += Watcher_Changed;
				watcher.Deleted += Watcher_Changed;
				watcher.Renamed += Watcher_Changed;
				watcher.Error += Watcher_Error;
				watcher.EnableRaisingEvents = true;
				infos = (from x in dirInfo.GetFileSystemInfos ()
					 where IsAllowed (x) && (!canChooseFiles ? x.Attributes.HasFlag (FileAttributes.Directory) : true)
					 orderby (!x.Attributes.HasFlag (FileAttributes.Directory)) + x.Name
					 select (x.Name, x.Attributes.HasFlag (FileAttributes.Directory), false)).ToList ();
				infos.Insert (0, ("..", true, false));
				top = 0;
				selected = 0;
				valid = true;
			} catch (Exception ex) {
				switch (ex) {
				case DirectoryNotFoundException _:
				case ArgumentException _:
					dirInfo = null;
					watcher?.Dispose ();
					watcher = null;
					infos.Clear ();
					valid = true;
					break;
				default:
					valid = false;
					break;
				}
			} finally {
				if (valid) {
					SetNeedsDisplay ();
				}
			}
			return valid;
		}

		private bool _disposedValue;
		protected override void Dispose (bool disposing)
		{
			if (!_disposedValue) {
				if (disposing) {
					if (watcher != null) {
						watcher.Changed -= Watcher_Changed;
						watcher.Created -= Watcher_Changed;
						watcher.Deleted -= Watcher_Changed;
						watcher.Renamed -= Watcher_Changed;
						watcher.Error -= Watcher_Error;
					}
					watcher?.Dispose ();
					watcher = null;
				}

				_disposedValue = true;
			}

			// Call base class implementation.
			base.Dispose (disposing);
		}

		void Watcher_Error (object sender, ErrorEventArgs e)
		{
			if (Application.MainLoop == null)
				return;

			Application.MainLoop.Invoke (() => Reload ());
		}

		void Watcher_Changed (object sender, FileSystemEventArgs e)
		{
			if (Application.MainLoop == null)
				return;

			Application.MainLoop.Invoke (() => Reload ());
		}

		ustring directory;
		public ustring Directory {
			get => directory;
			set {
				if (directory == value) {
					return;
				}
				if (Reload (value)) {
					directory = value;
				}
			}
		}

		public override void PositionCursor ()
		{
			Move (0, selected - top);
		}

		int lastSelected;
		bool shiftOnWheel;
		public override bool MouseEvent (MouseEvent me)
		{
			if ((me.Flags & (MouseFlags.Button1Clicked | MouseFlags.Button1DoubleClicked |
				MouseFlags.WheeledUp | MouseFlags.WheeledDown)) == 0)
				return false;

			if (!HasFocus)
				SetFocus ();

			if (infos == null)
				return false;

			if (me.Y + top >= infos.Count)
				return true;

			int lastSelectedCopy = shiftOnWheel ? lastSelected : selected;

			switch (me.Flags) {
			case MouseFlags.Button1Clicked:
				SetSelected (me);
				OnSelectionChanged ();
				SetNeedsDisplay ();
				break;
			case MouseFlags.Button1DoubleClicked:
				UnMarkAll ();
				SetSelected (me);
				if (ExecuteSelection ()) {
					host.canceled = false;
					Application.RequestStop ();
				}
				return true;
			case MouseFlags.Button1Clicked | MouseFlags.ButtonShift:
				SetSelected (me);
				if (shiftOnWheel)
					lastSelected = lastSelectedCopy;
				shiftOnWheel = false;
				PerformMultipleSelection (lastSelected);
				return true;
			case MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl:
				SetSelected (me);
				PerformMultipleSelection ();
				return true;
			case MouseFlags.WheeledUp:
				SetSelected (me);
				selected = lastSelected;
				MoveUp ();
				return true;
			case MouseFlags.WheeledDown:
				SetSelected (me);
				selected = lastSelected;
				MoveDown ();
				return true;
			case MouseFlags.WheeledUp | MouseFlags.ButtonShift:
				SetSelected (me);
				selected = lastSelected;
				lastSelected = lastSelectedCopy;
				shiftOnWheel = true;
				MoveUp ();
				return true;
			case MouseFlags.WheeledDown | MouseFlags.ButtonShift:
				SetSelected (me);
				selected = lastSelected;
				lastSelected = lastSelectedCopy;
				shiftOnWheel = true;
				MoveDown ();
				return true;
			}

			return true;
		}

		void UnMarkAll ()
		{
			for (int i = 0; i < infos.Count; i++) {
				if (infos [i].Item3) {
					infos [i] = (infos [i].Item1, infos [i].Item2, false);
				}
			}
		}

		void SetSelected (MouseEvent me)
		{
			lastSelected = selected;
			selected = top + me.Y;
		}

		void DrawString (int line, string str)
		{
			var f = Frame;
			var width = f.Width;
			var ustr = ustring.Make (str);

			Move (allowsMultipleSelection ? 3 : 2, line);
			int byteLen = ustr.Length;
			int used = allowsMultipleSelection ? 2 : 1;
			for (int i = 0; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
				if (used + count >= width)
					break;
				Driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width - 1; used++) {
				Driver.AddRune (' ');
			}
		}

		public override void Redraw (Rect bounds)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = top;
			bool focused = HasFocus;
			var width = bounds.Width;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == selected;
				Move (0, row);
				var newcolor = focused ? (isSelected ? ColorScheme.HotNormal : ColorScheme.Focus)
					: Enabled ? ColorScheme.Focus : ColorScheme.Disabled;
				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}
				if (item >= infos.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
					continue;
				}
				var fi = infos [item];

				Driver.AddRune (isSelected ? '>' : ' ');

				if (allowsMultipleSelection)
					Driver.AddRune (fi.Item3 ? '*' : ' ');

				if (fi.Item2)
					Driver.AddRune ('/');
				else
					Driver.AddRune (' ');
				DrawString (row, fi.Item1);
			}
		}

		public Action<(string, bool)> SelectedChanged { get; set; }
		public Action<ustring> DirectoryChanged { get; set; }
		public Action<ustring> FileChanged { get; set; }

		string splitString = ",";

		void OnSelectionChanged ()
		{
			if (allowsMultipleSelection) {
				if (FilePaths.Count > 0) {
					FileChanged?.Invoke (string.Join (splitString, GetFilesName (FilePaths)));
				} else {
					FileChanged?.Invoke (infos [selected].Item2 && !canChooseDirectories ? "" : Path.GetFileName (infos [selected].Item1));
				}
			} else {
				var sel = infos [selected];
				SelectedChanged?.Invoke ((sel.Item1, sel.Item2));
			}
		}

		List<string> GetFilesName (IReadOnlyList<string> files)
		{
			List<string> filesName = new List<string> ();

			foreach (var file in files) {
				filesName.Add (Path.GetFileName (file));
			}

			return filesName;
		}

		public bool GetValidFilesName (string files, out string result)
		{
			result = string.Empty;
			if (infos?.Count == 0) {
				return false;
			}

			var valid = true;
			IReadOnlyList<string> filesList = new List<string> (files.Split (splitString.ToArray (), StringSplitOptions.None));
			var filesName = new List<string> ();
			UnMarkAll ();

			foreach (var file in filesList) {
				if (!allowsMultipleSelection && filesName.Count > 0) {
					break;
				}
				var idx = infos.IndexOf (x => x.Item1.IndexOf (file, StringComparison.OrdinalIgnoreCase) >= 0);
				if (idx > -1 && string.Equals (infos [idx].Item1, file, StringComparison.OrdinalIgnoreCase)) {
					if (canChooseDirectories && !canChooseFiles && !infos [idx].Item2) {
						valid = false;
					}
					if (allowsMultipleSelection && !infos [idx].Item3) {
						infos [idx] = (infos [idx].Item1, infos [idx].Item2, true);
					}
					if (!allowsMultipleSelection) {
						selected = idx;
					}
					filesName.Add (Path.GetFileName (infos [idx].Item1));
				} else if (idx > -1) {
					valid = false;
					filesName.Add (Path.GetFileName (file));
				}
			}
			result = string.Join (splitString, filesName);
			if (string.IsNullOrEmpty (result)) {
				valid = false;
			}
			return valid;
		}

		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorUp:
			case Key.P | Key.CtrlMask:
				MoveUp ();
				return true;

			case Key.CursorDown:
			case Key.N | Key.CtrlMask:
				MoveDown ();
				return true;

			case Key.V | Key.CtrlMask:
			case Key.PageDown:
				var n = (selected + Frame.Height);
				if (n > infos.Count)
					n = infos.Count - 1;
				if (n != selected) {
					selected = n;
					if (infos.Count >= Frame.Height)
						top = selected;
					else
						top = 0;
					OnSelectionChanged ();

					SetNeedsDisplay ();
				}
				return true;

			case Key.Enter:
				UnMarkAll ();
				if (ExecuteSelection ())
					return false;
				else
					return true;

			case Key.PageUp:
				n = (selected - Frame.Height);
				if (n < 0)
					n = 0;
				if (n != selected) {
					selected = n;
					top = selected;
					OnSelectionChanged ();
					SetNeedsDisplay ();
				}
				return true;

			case Key.Space:
			case Key.T | Key.CtrlMask:
				PerformMultipleSelection ();
				return true;

			case Key.Home:
				MoveFirst ();
				return true;

			case Key.End:
				MoveLast ();
				return true;
			}
			return base.ProcessKey (keyEvent);
		}

		void MoveLast ()
		{
			selected = infos.Count - 1;
			top = infos.Count () - 1;
			OnSelectionChanged ();
			SetNeedsDisplay ();
		}

		void MoveFirst ()
		{
			selected = 0;
			top = 0;
			OnSelectionChanged ();
			SetNeedsDisplay ();
		}

		void MoveDown ()
		{
			if (selected + 1 < infos.Count) {
				selected++;
				if (selected >= top + Frame.Height)
					top++;
				OnSelectionChanged ();
				SetNeedsDisplay ();
			}
		}

		void MoveUp ()
		{
			if (selected > 0) {
				selected--;
				if (selected < top)
					top = selected;
				OnSelectionChanged ();
				SetNeedsDisplay ();
			}
		}

		internal bool ExecuteSelection (bool navigateFolder = true)
		{
			if (infos.Count == 0) {
				return false;
			}
			var isDir = infos [selected].Item2;

			if (isDir) {
				Directory = Path.GetFullPath (Path.Combine (Path.GetFullPath (Directory.ToString ()), infos [selected].Item1));
				DirectoryChanged?.Invoke (Directory);
				if (canChooseDirectories && !navigateFolder) {
					return true;
				}
			} else {
				OnSelectionChanged ();
				if (canChooseFiles) {
					// Ensures that at least one file is selected.
					if (FilePaths.Count == 0)
						PerformMultipleSelection ();
					// Let the OK handler take it over
					return true;
				}
				// No files allowed, do not let the default handler take it.
			}
			return false;
		}

		void PerformMultipleSelection (int? firstSelected = null)
		{
			if (allowsMultipleSelection) {
				int first = Math.Min (firstSelected ?? selected, selected);
				int last = Math.Max (selected, firstSelected ?? selected);
				for (int i = first; i <= last; i++) {
					if ((canChooseFiles && infos [i].Item2 == false) ||
					    (canChooseDirectories && infos [i].Item2 &&
					     infos [i].Item1 != "..")) {
						infos [i] = (infos [i].Item1, infos [i].Item2, !infos [i].Item3);
					}
				}
				OnSelectionChanged ();
				SetNeedsDisplay ();
			}
		}

		string [] allowedFileTypes;
		public string [] AllowedFileTypes {
			get => allowedFileTypes;
			set {
				allowedFileTypes = value;
				Reload ();
			}
		}

		public string MakePath (string relativePath)
		{
			var dir = Directory.ToString ();
			return string.IsNullOrEmpty (dir) ? "" : Path.GetFullPath (Path.Combine (dir, relativePath));
		}

		public IReadOnlyList<string> FilePaths {
			get {
				if (allowsMultipleSelection) {
					var res = new List<string> ();
					foreach (var item in infos) {
						if (item.Item3)
							res.Add (MakePath (item.Item1));
					}
					if (res.Count == 0 && infos.Count > 0 && infos [selected].Item1 != "..") {
						res.Add (MakePath (infos [selected].Item1));
					}
					return res;
				} else {
					if (infos.Count == 0) {
						return null;
					}
					if (infos [selected].Item2) {
						if (canChooseDirectories) {
							var sel = infos [selected].Item1;
							return sel == ".." ? new List<string> () : new List<string> () { MakePath (infos [selected].Item1) };
						}
						return Array.Empty<string> ();
					} else {
						if (canChooseFiles) {
							return new List<string> () { MakePath (infos [selected].Item1) };
						}
						return Array.Empty<string> ();
					}
				}
			}
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}

	/// <summary>
	/// Base class for the <see cref="OpenDialog"/> and the <see cref="SaveDialog"/>
	/// </summary>
	public class FileDialog : Dialog {
		Button prompt, cancel;
		Label nameFieldLabel, message, nameDirLabel;
		TextField dirEntry, nameEntry;
		internal DirListView dirListView;
		ComboBox cmbAllowedTypes;

		/// <summary>
		/// Initializes a new <see cref="FileDialog"/>.
		/// </summary>
		public FileDialog () : this (title: string.Empty, prompt: string.Empty,
			nameFieldLabel: string.Empty, message: string.Empty)
		{ }

		/// <summary>
		/// Initializes a new instance of <see cref="FileDialog"/>
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="prompt">The prompt.</param>
		/// <param name="nameFieldLabel">The name of the file field label..</param>
		/// <param name="message">The message.</param>
		/// <param name="allowedTypes">The allowed types.</param>
		public FileDialog (ustring title, ustring prompt, ustring nameFieldLabel, ustring message, List<string> allowedTypes = null)
			: this (title, prompt, ustring.Empty, nameFieldLabel, message, allowedTypes) { }

		/// <summary>
		/// Initializes a new instance of <see cref="FileDialog"/>
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="prompt">The prompt.</param>
		/// <param name="message">The message.</param>
		/// <param name="allowedTypes">The allowed types.</param>
		public FileDialog (ustring title, ustring prompt, ustring message, List<string> allowedTypes)
			: this (title, prompt, ustring.Empty, message, allowedTypes) { }

		/// <summary>
		/// Initializes a new instance of <see cref="FileDialog"/>
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="prompt">The prompt.</param>
		/// <param name="nameDirLabel">The name of the directory field label.</param>
		/// <param name="nameFieldLabel">The name of the file field label..</param>
		/// <param name="message">The message.</param>
		/// <param name="allowedTypes">The allowed types.</param>
		public FileDialog (ustring title, ustring prompt, ustring nameDirLabel, ustring nameFieldLabel, ustring message,
			List<string> allowedTypes = null) : base (title)//, Driver.Cols - 20, Driver.Rows - 5, null)
		{
			this.message = new Label (message) {
				X = 1,
				Y = 0,
			};
			Add (this.message);
			var msgLines = TextFormatter.MaxLines (message, Driver.Cols - 20);

			this.nameDirLabel = new Label (nameDirLabel.IsEmpty ? $"{Strings.fdDirectory}: " : $"{nameDirLabel}: ") {
				X = 1,
				Y = 1 + msgLines,
				AutoSize = true
			};

			dirEntry = new TextField ("") {
				X = Pos.Right (this.nameDirLabel),
				Y = 1 + msgLines,
				Width = Dim.Fill () - 1,
			};
			dirEntry.TextChanged += (s, e) => {
				DirectoryPath = dirEntry.Text;
				nameEntry.Text = ustring.Empty;
			};
			Add (this.nameDirLabel, dirEntry);

			this.nameFieldLabel = new Label (nameFieldLabel.IsEmpty ? $"{Strings.fdFile}: " : $"{nameFieldLabel}: ") {
				X = 1,
				Y = 3 + msgLines,
				AutoSize = true
			};
			nameEntry = new TextField ("") {
				X = Pos.Left (dirEntry),
				Y = 3 + msgLines,
				Width = Dim.Percent (70, true)
			};
			Add (this.nameFieldLabel, nameEntry);

			cmbAllowedTypes = new ComboBox () {
				X = Pos.Right (nameEntry) + 2,
				Y = Pos.Top (nameEntry),
				Width = Dim.Fill (1),
				Height = SetComboBoxHeight (allowedTypes),
				Text = allowedTypes?.Count > 0 ? allowedTypes [0] : string.Empty,
				SelectedItem = allowedTypes?.Count > 0 ? 0 : -1,
				ReadOnly = true,
				HideDropdownListOnClick = true
			};
			cmbAllowedTypes.SetSource (allowedTypes ?? new List<string> ());
			cmbAllowedTypes.OpenSelectedItem += (s, e) => {
				dirListView.AllowedFileTypes = cmbAllowedTypes.Text.ToString ().Split (';');
				dirListView.Reload ();
			};
			Add (cmbAllowedTypes);

			dirListView = new DirListView (this) {
				X = 1,
				Y = 3 + msgLines + 2,
				Width = Dim.Fill () - 1,
				Height = Dim.Fill () - 2,
			};
			DirectoryPath = Path.GetFullPath (Environment.CurrentDirectory);
			Add (dirListView);

			AllowedFileTypes = allowedTypes?.Count > 0 ? allowedTypes?.ToArray () : null;
			dirListView.DirectoryChanged = (dir) => { nameEntry.Text = ustring.Empty; dirEntry.Text = dir; };
			dirListView.FileChanged = (file) => nameEntry.Text = file == ".." ? "" : file;
			dirListView.SelectedChanged = (file) => nameEntry.Text = file.Item1 == ".." ? "" : file.Item1;
			this.cancel = new Button ("Cancel");
			this.cancel.Clicked += (s,e) => {
				Cancel ();
			};
			AddButton (cancel);

			this.prompt = new Button (prompt.IsEmpty ? "Ok" : prompt) {
				IsDefault = true,
				Enabled = nameEntry.Text.IsEmpty ? false : true
			};
			this.prompt.Clicked += (s,e) => {
				if (this is OpenDialog) {
					if (!dirListView.GetValidFilesName (nameEntry.Text.ToString (), out string res)) {
						nameEntry.Text = res;
						dirListView.SetNeedsDisplay ();
						return;
					}
					if (!dirListView.canChooseDirectories && !dirListView.ExecuteSelection (false)) {
						return;
					}
				} else if (this is SaveDialog) {
					var name = nameEntry.Text.ToString ();
					if (FilePath.IsEmpty || name.Split (',').Length > 1) {
						return;
					}
					var ext = name.EndsWith (cmbAllowedTypes.Text.ToString ())
						? "" : cmbAllowedTypes.Text.ToString ();
					FilePath = Path.Combine (FilePath.ToString (), $"{name}{ext}");
				}
				canceled = false;
				Application.RequestStop ();
			};
			AddButton (this.prompt);

			nameEntry.TextChanged += (s,e) => {
				if (nameEntry.Text.IsEmpty) {
					this.prompt.Enabled = false;
				} else {
					this.prompt.Enabled = true;
				}
			};

			Width = Dim.Percent (80);
			Height = Dim.Percent (80);

			// On success, we will set this to false.
			canceled = true;

			KeyPress += (s, e) => {
				if (e.KeyEvent.Key == Key.Esc) {
					Cancel ();
					e.Handled = true;
				}
			};
			void Cancel ()
			{
				canceled = true;
				Application.RequestStop ();
			}
		}

		private static int SetComboBoxHeight (List<string> allowedTypes)
		{
			return allowedTypes != null ? Math.Min (allowedTypes.Count + 1, 8) : 8;
		}

		internal bool canceled;

		///<inheritdoc/>
		public override void WillPresent ()
		{
			base.WillPresent ();
			dirListView.SetFocus ();
		}

		//protected override void Dispose (bool disposing)
		//{
		//	message?.Dispose ();
		//	base.Dispose (disposing);
		//}

		/// <summary>
		/// Gets or sets the prompt label for the <see cref="Button"/> displayed to the user
		/// </summary>
		/// <value>The prompt.</value>
		public ustring Prompt {
			get => prompt.Text;
			set {
				prompt.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the directory field label.
		/// </summary>
		/// <value>The name of the directory field label.</value>
		public ustring NameDirLabel {
			get => nameDirLabel.Text;
			set {
				nameDirLabel.Text = $"{value}: ";
			}
		}

		/// <summary>
		/// Gets or sets the name field label.
		/// </summary>
		/// <value>The name field label.</value>
		public ustring NameFieldLabel {
			get => nameFieldLabel.Text;
			set {
				nameFieldLabel.Text = $"{value}: ";
			}
		}

		/// <summary>
		/// Gets or sets the message displayed to the user, defaults to nothing
		/// </summary>
		/// <value>The message.</value>
		public ustring Message {
			get => message.Text;
			set {
				message.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FileDialog"/> can create directories.
		/// </summary>
		/// <value><c>true</c> if can create directories; otherwise, <c>false</c>.</value>
		public bool CanCreateDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FileDialog"/> is extension hidden.
		/// </summary>
		/// <value><c>true</c> if is extension hidden; otherwise, <c>false</c>.</value>
		public bool IsExtensionHidden { get; set; }

		/// <summary>
		/// Gets or sets the directory path for this panel
		/// </summary>
		/// <value>The directory path.</value>
		public ustring DirectoryPath {
			get => dirEntry.Text;
			set {
				dirEntry.Text = value;
				dirListView.Directory = value;
			}
		}

		private string [] allowedFileTypes;

		/// <summary>
		/// The array of filename extensions allowed, or null if all file extensions are allowed.
		/// </summary>
		/// <value>The allowed file types.</value>
		public string [] AllowedFileTypes {
			get => allowedFileTypes;
			set {
				allowedFileTypes = value;
				var selected = cmbAllowedTypes.SelectedItem;
				cmbAllowedTypes.SetSource (value);
				cmbAllowedTypes.SelectedItem = selected > -1 ? selected : 0;
				SetComboBoxHeight (value?.ToList ());
				dirListView.AllowedFileTypes = value != null
					? value [cmbAllowedTypes.SelectedItem].Split (';')
					: null;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="FileDialog"/> allows the file to be saved with a different extension
		/// </summary>
		/// <value><c>true</c> if allows other file types; otherwise, <c>false</c>.</value>
		public bool AllowsOtherFileTypes { get; set; }

		/// <summary>
		/// The File path that is currently shown on the panel
		/// </summary>
		/// <value>The absolute file path for the file path entered.</value>
		public ustring FilePath {
			get => dirListView.MakePath (nameEntry.Text.ToString ());
			set {
				nameEntry.Text = Path.GetFileName (value.ToString ());
			}
		}

		/// <summary>
		/// Check if the dialog was or not canceled.
		/// </summary>
		public bool Canceled { get => canceled; }
	}

	/// <summary>
	///  The <see cref="SaveDialog"/> provides an interactive dialog box for users to pick a file to 
	///  save.
	/// </summary>
	/// <remarks>
	/// <para>
	///   To use, create an instance of <see cref="SaveDialog"/>, and pass it to
	///   <see cref="Application.Run(Func{Exception, bool})"/>. This will run the dialog modally,
	///   and when this returns, the <see cref="FileName"/>property will contain the selected file name or 
	///   null if the user canceled. 
	/// </para>
	/// </remarks>
	public class SaveDialog : FileDialog {
		/// <summary>
		/// Initializes a new <see cref="SaveDialog"/>.
		/// </summary>
		public SaveDialog () : this (title: string.Empty, message: string.Empty) { }

		/// <summary>
		/// Initializes a new <see cref="SaveDialog"/>.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="message">The message.</param>
		/// <param name="allowedTypes">The allowed types.</param>
		public SaveDialog (ustring title, ustring message, List<string> allowedTypes = null)
			: base (title, prompt: Strings.fdSave, nameFieldLabel: $"{Strings.fdSaveAs}:", message: message, allowedTypes) { }

		/// <summary>
		/// Gets the name of the file the user selected for saving, or null
		/// if the user canceled the <see cref="SaveDialog"/>.
		/// </summary>
		/// <value>The name of the file.</value>
		public ustring FileName {
			get {
				if (canceled)
					return null;
				return Path.GetFileName (FilePath.ToString ());
			}
		}
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
		OpenMode openMode;

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
		/// Initializes a new <see cref="OpenDialog"/>.
		/// </summary>
		public OpenDialog () : this (title: string.Empty, message: string.Empty) { }

		/// <summary>
		/// Initializes a new <see cref="OpenDialog"/>.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="message">The message.</param>
		/// <param name="allowedTypes">The allowed types.</param>
		/// <param name="openMode">The open mode.</param>
		public OpenDialog (ustring title, ustring message, List<string> allowedTypes = null, OpenMode openMode = OpenMode.File) : base (title,
			prompt: openMode == OpenMode.File ? Strings.fdOpen : openMode == OpenMode.Directory ? Strings.fdSelectFolder : Strings.fdSelectMixed,
			nameFieldLabel: Strings.fdOpen, message: message, allowedTypes)
		{
			this.openMode = openMode;
			switch (openMode) {
			case OpenMode.File:
				CanChooseFiles = true;
				CanChooseDirectories = false;
				break;
			case OpenMode.Directory:
				CanChooseFiles = false;
				CanChooseDirectories = true;
				break;
			case OpenMode.Mixed:
				CanChooseFiles = true;
				CanChooseDirectories = true;
				AllowsMultipleSelection = true;
				break;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Terminal.Gui.OpenDialog"/> can choose files.
		/// </summary>
		/// <value><c>true</c> if can choose files; otherwise, <c>false</c>.  Defaults to <c>true</c></value>
		public bool CanChooseFiles {
			get => dirListView.canChooseFiles;
			set {
				dirListView.canChooseFiles = value;
				dirListView.Reload ();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="OpenDialog"/> can choose directories.
		/// </summary>
		/// <value><c>true</c> if can choose directories; otherwise, <c>false</c> defaults to <c>false</c>.</value>
		public bool CanChooseDirectories {
			get => dirListView.canChooseDirectories;
			set {
				dirListView.canChooseDirectories = value;
				dirListView.Reload ();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="OpenDialog"/> allows multiple selection.
		/// </summary>
		/// <value><c>true</c> if allows multiple selection; otherwise, <c>false</c>, defaults to false.</value>
		public bool AllowsMultipleSelection {
			get => dirListView.allowsMultipleSelection;
			set {
				if (!value && openMode == OpenMode.Mixed) {
					return;
				}
				dirListView.allowsMultipleSelection = value;
				dirListView.Reload ();
			}
		}

		/// <summary>
		/// Returns the selected files, or an empty list if nothing has been selected
		/// </summary>
		/// <value>The file paths.</value>
		public IReadOnlyList<string> FilePaths {
			get => dirListView.FilePaths;
		}
	}
}
