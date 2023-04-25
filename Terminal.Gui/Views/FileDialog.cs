using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NStack;
using Terminal.Gui.Resources;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui {

	/// <summary>
	/// Modal dialog for selecting files/directories. Has auto-complete and expandable
	/// navigation pane (Recent, Root drives etc).
	/// </summary>
	public partial class FileDialog : Dialog {

		/// <summary>
		/// Gets settings for controlling how visual elements behave.  Style changes should
		/// be made before the <see cref="Dialog"/> is loaded and shown to the user for the
		/// first time.
		/// </summary>
		public FileDialogStyle Style { get; } = new FileDialogStyle ();

		/// <summary>
		/// The maximum number of results that will be collected
		/// when searching before stopping.
		/// </summary>
		/// <remarks>
		/// This prevents performance issues e.g. when searching
		/// root of file system for a common letter (e.g. 'e').
		/// </remarks>
		[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
		public static int MaxSearchResults { get; set; } = 10000;

		/// <summary>
		/// True if the file/folder must exist already to be selected.
		/// This prevents user from entering the name of something that
		/// doesn't exist. Defaults to false.
		/// </summary>
		public bool MustExist { get; set; }

		/// <summary>
		/// Gets the Path separators for the operating system
		/// </summary>
		internal static char [] Separators = new []
		{
			System.IO.Path.AltDirectorySeparatorChar,
			System.IO.Path.DirectorySeparatorChar,
		};

		/// <summary>
		/// Characters to prevent entry into <see cref="tbPath"/>. Note that this is not using
		/// <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory
		/// separators, arrow keys etc.
		/// </summary>
		private static char [] badChars = new []
		{
			'"','<','>','|','*','?',
		};

		/// <summary>
		/// The UI selected <see cref="IAllowedType"/> from combo box. May be null.
		/// </summary>
		public IAllowedType CurrentFilter { get; private set; }

		private bool pushingState = false;
		private bool loaded = false;

		/// <summary>
		/// Gets the currently open directory and known children presented in the dialog.
		/// </summary>
		internal FileDialogState State { get; private set; }

		/// <summary>
		/// Locking object for ensuring only a single <see cref="SearchState"/> executes at once.
		/// </summary>
		internal object onlyOneSearchLock = new object ();

		private bool disposed = false;
		private IFileSystem fileSystem;
		private TextField tbPath;

		private FileDialogSorter sorter;
		private FileDialogHistory history;

		private DataTable dtFiles;
		private TableView tableView;
		private TreeView<object> treeView;
		private TileView splitContainer;
		private Button btnOk;
		private Button btnCancel;
		private Button btnToggleSplitterCollapse;
		private Button btnForward;
		private Button btnBack;
		private Button btnUp;
		private string feedback;

		private CollectionNavigator collectionNavigator = new CollectionNavigator ();

		private TextField tbFind;
		private SpinnerView spinnerView;
		private MenuBar allowedTypeMenuBar;
		private MenuBarItem allowedTypeMenu;
		private MenuItem [] allowedTypeMenuItems;
		private DataColumn filenameColumn;

		/// <summary>
		/// Event fired when user attempts to confirm a selection (or multi selection).
		/// Allows you to cancel the selection or undertake alternative behavior e.g.
		/// open a dialog "File already exists, Overwrite? yes/no".
		/// </summary>
		public event EventHandler<FilesSelectedEventArgs> FilesSelected;

		/// <summary>
		/// Gets or sets behavior of the <see cref="FileDialog"/> when the user attempts
		/// to delete a selected file(s).  Set to null to prevent deleting.
		/// </summary>
		/// <remarks>Ensure you use a try/catch block with appropriate
		/// error handling (e.g. showing a <see cref="MessageBox"/></remarks>
		public IFileOperations FileOperationsHandler { get; set; } = new DefaultFileOperations ();

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDialog"/> class.
		/// </summary>
		public FileDialog () : this(new FileSystem())
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileDialog"/> class with
		/// a custom <see cref="IFileSystem"/>.
		/// </summary>
		/// <remarks>This overload is mainly useful for testing.</remarks>
		public FileDialog (IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
			this.btnOk = new Button (Style.OkButtonText) {
				Y = Pos.AnchorEnd (1),
				X = Pos.Function (() =>
					this.Bounds.Width
					- btnOk.Bounds.Width
					// TODO: Fiddle factor, seems the Bounds are wrong for someone
					- 2)
			};
			this.btnOk.Clicked += (s, e) => this.Accept (true);
			this.btnOk.KeyPress += (s, k) => {
				this.NavigateIf (k, Key.CursorLeft, this.btnCancel);
				this.NavigateIf (k, Key.CursorUp, this.tableView);
			};

			this.btnCancel = new Button ("Cancel") {
				Y = Pos.AnchorEnd (1),
				X = Pos.Function (() =>
					this.Bounds.Width
					- btnOk.Bounds.Width
					- btnCancel.Bounds.Width
					- 1
					// TODO: Fiddle factor, seems the Bounds are wrong for someone
					- 2
					)
			};
			this.btnCancel.KeyPress += (s, k) => {
				this.NavigateIf (k, Key.CursorLeft, this.btnToggleSplitterCollapse);
				this.NavigateIf (k, Key.CursorUp, this.tableView);
				this.NavigateIf (k, Key.CursorRight, this.btnOk);
			};
			this.btnCancel.Clicked += (s, e) => {
				Application.RequestStop ();
			};

			this.btnUp = new Button () { X = 0, Y = 1, NoPadding = true };
			btnUp.Text = GetUpButtonText ();
			this.btnUp.Clicked += (s, e) => this.history.Up ();

			this.btnBack = new Button () { X = Pos.Right (btnUp) + 1, Y = 1, NoPadding = true };
			btnBack.Text = GetBackButtonText ();
			this.btnBack.Clicked += (s, e) => this.history.Back ();

			this.btnForward = new Button () { X = Pos.Right (btnBack) + 1, Y = 1, NoPadding = true };
			btnForward.Text = GetForwardButtonText();
			this.btnForward.Clicked += (s, e) => this.history.Forward ();

			this.tbPath = new TextField {
				Width = Dim.Fill (0),
				Caption = Style.PathCaption,
				CaptionColor = Color.Black
			};
			this.tbPath.KeyPress += (s, k) => {

				ClearFeedback ();

				this.AcceptIf (k, Key.Enter);

				this.SuppressIfBadChar (k);
			};

			tbPath.Autocomplete = new AppendAutocomplete (tbPath);
			tbPath.Autocomplete.SuggestionGenerator = new FilepathSuggestionGenerator ();

			this.splitContainer = new TileView () {
				X = 0,
				Y = 2,
				Width = Dim.Fill (0),
				Height = Dim.Fill (1),
			};
			this.splitContainer.SetSplitterPos (0, 30);
//			this.splitContainer.Border.BorderStyle = BorderStyle.None;
			this.splitContainer.Tiles.ElementAt (0).ContentView.Visible = false;

			this.tableView = new TableView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				FullRowSelect = true,
			};

			this.tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);
			Style.TableStyle = tableView.Style;

			this.tableView.KeyPress += (s, k) => {
				if (this.tableView.SelectedRow <= 0) {
					this.NavigateIf (k, Key.CursorUp, this.tbPath);
				}
				if (this.tableView.SelectedRow == this.tableView.Table.Rows.Count-1) {
					this.NavigateIf (k, Key.CursorDown, this.btnToggleSplitterCollapse);
				}

				if (splitContainer.Tiles.First ().ContentView.Visible && tableView.SelectedColumn == 0) {
					this.NavigateIf (k, Key.CursorLeft, this.treeView);
				}

				if (k.Handled) {
					return;
				}

				if (this.tableView.HasFocus &&
				!k.KeyEvent.Key.HasFlag (Key.CtrlMask) &&
				!k.KeyEvent.Key.HasFlag (Key.AltMask) &&
					char.IsLetterOrDigit ((char)k.KeyEvent.KeyValue)) {
					CycleToNextTableEntryBeginningWith (k);
				}

			};

			this.treeView = new TreeView<object> () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			this.treeView.TreeBuilder = new FileDialogTreeBuilder ();
			this.treeView.AspectGetter = (m) => m is IDirectoryInfo d ? d.Name : m.ToString ();
			this.Style.TreeStyle = treeView.Style;

			this.treeView.SelectionChanged += this.TreeView_SelectionChanged;

			this.splitContainer.Tiles.ElementAt (0).ContentView.Add (this.treeView);
			this.splitContainer.Tiles.ElementAt (1).ContentView.Add (this.tableView);

			this.btnToggleSplitterCollapse = new Button (GetToggleSplitterText (false)) {
				Y = Pos.AnchorEnd (1),
			};
			this.btnToggleSplitterCollapse.Clicked += (s, e) => {
				var tile = this.splitContainer.Tiles.ElementAt (0);

				var newState = !tile.ContentView.Visible;
				tile.ContentView.Visible = newState;
				this.btnToggleSplitterCollapse.Text = GetToggleSplitterText (newState);
				this.LayoutSubviews();
			};

			tbFind = new TextField {
				X = Pos.Right (this.btnToggleSplitterCollapse) + 1,
				Caption = Style.SearchCaption,
				CaptionColor = Color.Black,
				Width = 30,
				Y = Pos.AnchorEnd (1),
			};
			spinnerView = new SpinnerView () {
				X = Pos.Right (tbFind) + 1,
				Y = Pos.AnchorEnd (1),
				Visible = false,
			};

			tbFind.TextChanged += (s, o) => RestartSearch ();
			tbFind.KeyPress += (s, o) => {
				if (o.KeyEvent.Key == Key.Enter) {
					RestartSearch ();
					o.Handled = true;
				}

				if(o.KeyEvent.Key == Key.Esc) {
					if(CancelSearch()) {
						o.Handled = true;
					}
				}
				if(tbFind.CursorIsAtEnd()) {
					NavigateIf (o, Key.CursorRight, btnCancel);
				}
				if (tbFind.CursorIsAtStart ()) {
					NavigateIf (o, Key.CursorLeft, btnToggleSplitterCollapse);
				}
			};

			this.tableView.Style.ShowHorizontalHeaderOverline = true;
			this.tableView.Style.ShowVerticalCellLines = true;
			this.tableView.Style.ShowVerticalHeaderLines = true;
			this.tableView.Style.AlwaysShowHeaders = true;
			this.tableView.Style.ShowHorizontalHeaderUnderline = true;
			this.tableView.Style.ShowHorizontalScrollIndicators = true;

			this.SetupTableColumns ();

			this.sorter = new FileDialogSorter (this, this.tableView);
			this.history = new FileDialogHistory (this);

			this.tableView.Table = this.dtFiles;

			this.tbPath.TextChanged += (s, e) => this.PathChanged ();

			this.tableView.CellActivated += this.CellActivate;
			this.tableView.KeyUp += (s, k) => k.Handled = this.TableView_KeyUp (k.KeyEvent);
			this.tableView.SelectedCellChanged += this.TableView_SelectedCellChanged;

			this.tableView.AddKeyBinding (Key.Home, Command.TopHome);
			this.tableView.AddKeyBinding (Key.End, Command.BottomEnd);
			this.tableView.AddKeyBinding (Key.Home | Key.ShiftMask, Command.TopHomeExtend);
			this.tableView.AddKeyBinding (Key.End | Key.ShiftMask, Command.BottomEndExtend);

			this.treeView.KeyDown += (s, k) => {

				var selected = treeView.SelectedObject;
				if (selected != null) {
					if (!treeView.CanExpand (selected) || treeView.IsExpanded (selected)) {
						this.NavigateIf (k, Key.CursorRight, this.tableView);
					} else
					if (treeView.GetObjectRow (selected) == 0) {
						this.NavigateIf (k, Key.CursorUp, this.tbPath);
					}
				}

				if (k.Handled) {
					return;
				}

				k.Handled = this.TreeView_KeyDown (k.KeyEvent);

			};

			this.AllowsMultipleSelection = false;

			this.UpdateNavigationVisibility ();

			// Determines tab order
			this.Add (this.btnToggleSplitterCollapse);
			this.Add (this.tbFind);
			this.Add (this.spinnerView);
			this.Add (this.btnOk);
			this.Add (this.btnCancel);
			this.Add (this.btnUp);
			this.Add (this.btnBack);
			this.Add (this.btnForward);
			this.Add (this.tbPath);
			this.Add (this.splitContainer);

			// Default sort order is by name
			sorter.SortColumn(this.filenameColumn,true);
		}

		private string GetForwardButtonText ()
		{
			return "-" + Driver.RightArrow;
		}

		private string GetBackButtonText ()
		{
			return Driver.LeftArrow + "-";
		}

		private string GetUpButtonText ()
		{
			return Style.UseUnicodeCharacters ? "◭" : "▲";
		}

		private string GetToggleSplitterText (bool isExpanded)
		{
			return isExpanded ? 
				new string ((char)Driver.LeftArrow, 2) :
				new string ((char)Driver.RightArrow, 2);
		}

		private void Delete ()
		{
			var toDelete = GetFocusedFiles ();

			if (toDelete != null && FileOperationsHandler.Delete (toDelete)) {
				RefreshState ();
			}
		}

		private void Rename ()
		{
			var toRename = GetFocusedFiles ();

			if (toRename?.Length == 1) {
				var newNamed = FileOperationsHandler.Rename (this.fileSystem, toRename.Single ());

				if (newNamed != null) {
					RefreshState ();
					RestoreSelection (newNamed);
				}
			}
		}
		private void New ()
		{
			if (State != null) {
				var created = FileOperationsHandler.New (this.fileSystem, State.Directory);
				if (created != null) {
					RefreshState ();
					RestoreSelection (created);
				}
			}
		}
		private IFileSystemInfo [] GetFocusedFiles ()
		{

			if (!tableView.HasFocus || !tableView.CanFocus || FileOperationsHandler == null) {
				return null;
			}

			tableView.EnsureValidSelection ();

			if (tableView.SelectedRow < 0) {
				return null;
			}

			return tableView.GetAllSelectedCells ()
				.Select (c => c.Y)
				.Distinct ()
				.Select (RowToStats)
				.Where (s => !s.IsParent)
				.Select (d => d.FileSystemInfo)
				.ToArray ();
		}


		/// <inheritdoc/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			if (this.NavigateIf (keyEvent, Key.CtrlMask | Key.F, this.tbFind)) {
				return true;
			}

			ClearFeedback ();

			if (allowedTypeMenuBar != null &&
				keyEvent.Key == Key.Tab &&
				allowedTypeMenuBar.IsMenuOpen) {
				allowedTypeMenuBar.CloseMenu (false, false, false);
			}

			return base.ProcessHotKey (keyEvent);
		}
		private void RestartSearch ()
		{
			if (disposed || State?.Directory == null) {
				return;
			}

			if (State is SearchState oldSearch) {
				oldSearch.Cancel ();
			}

			// user is clearing search terms
			if (tbFind.Text == null || tbFind.Text.Length == 0) {

				// Wait for search cancellation (if any) to finish
				// then push the current dir state
				lock (onlyOneSearchLock) {
					PushState (new FileDialogState (State.Directory, this), false);
				}
				return;
			}

			PushState (new SearchState (State?.Directory, this, tbFind.Text.ToString ()), true);
		}

		/// <inheritdoc/>
		protected override void Dispose (bool disposing)
		{
			disposed = true;
			base.Dispose (disposing);

			CancelSearch ();
		}

		private bool CancelSearch ()
		{
			if (State is SearchState search) {
				return search.Cancel ();
			}

			return false;
		}

		private void ClearFeedback ()
		{
			feedback = null;
		}

		private void CycleToNextTableEntryBeginningWith (KeyEventEventArgs keyEvent)
		{
			if (tableView.Table.Rows.Count == 0) {
				return;
			}

			var row = tableView.SelectedRow;

			// There is a multi select going on and not just for the current row
			if (tableView.GetAllSelectedCells ().Any (c => c.Y != row)) {
				return;
			}

			int match = collectionNavigator.GetNextMatchingItem (row, (char)keyEvent.KeyEvent.KeyValue);

			if (match != -1) {
				tableView.SelectedRow = match;
				tableView.EnsureValidSelection ();
				tableView.EnsureSelectedCellIsVisible ();
				keyEvent.Handled = true;
			}
		}

		private void UpdateCollectionNavigator ()
		{
			tableView.EnsureValidSelection ();
			var col = tableView.SelectedColumn;
			var style = tableView.Style.GetColumnStyleIfAny (tableView.Table.Columns [col]);


			var collection = tableView
				.Table
				.Rows
				.Cast<DataRow> ()
				.Select ((o, idx) => col == 0 ? 
					RowToStats(idx).FileSystemInfo.Name :
					style.GetRepresentation (o [0])?.TrimStart('.'))
				.ToArray ();

			collectionNavigator = new CollectionNavigator (collection);
		}

		/// <summary>
		/// Gets or Sets which <see cref="System.IO.FileSystemInfo"/> type can be selected.
		/// Defaults to <see cref="OpenMode.Mixed"/> (i.e. <see cref="DirectoryInfo"/> or
		/// <see cref="FileInfo"/>).
		/// </summary>
		public OpenMode OpenMode { get; set; } = OpenMode.Mixed;

		/// <summary>
		/// Gets or Sets the selected path in the dialog. This is the result that should
		/// be used if <see cref="AllowsMultipleSelection"/> is off and <see cref="Canceled"/>
		/// is true.
		/// </summary>
		public string Path {
			get => this.tbPath.Text.ToString ();
			set {
				this.tbPath.Text = value;
				this.tbPath.MoveEnd ();
			}
		}

		/// <summary>
		/// Defines how the dialog matches files/folders when using the search
		/// box. Provide a custom implementation if you want to tailor how matching
		/// is performed.
		/// </summary>
		public ISearchMatcher SearchMatcher { get; set; } = new DefaultSearchMatcher ();

		/// <summary>
		/// Gets or Sets a value indicating whether to allow selecting 
		/// multiple existing files/directories. Defaults to false.
		/// </summary>
		public bool AllowsMultipleSelection {
			get => this.tableView.MultiSelect;
			set => this.tableView.MultiSelect = value;
		}

		/// <summary>
		/// Gets or Sets a collection of file types that the user can/must select. Only applies
		/// when <see cref="OpenMode"/> is <see cref="OpenMode.File"/> or <see cref="OpenMode.Mixed"/>.
		/// </summary>
		/// <remarks><see cref="AllowedTypeAny"/> adds the option to select any type (*.*). If this
		/// collection is empty then any type is supported and no Types drop-down is shown.</remarks> 
		public List<IAllowedType> AllowedTypes { get; set; } = new List<IAllowedType> ();

		/// <summary>
		/// Gets a value indicating whether the <see cref="FileDialog"/> was closed
		/// without confirming a selection.
		/// </summary>
		public bool Canceled { get; private set; } = true;

		/// <summary>
		/// Gets all files/directories selected or an empty collection
		/// <see cref="AllowsMultipleSelection"/> is <see langword="false"/> or <see cref="Canceled"/>.
		/// </summary>
		/// <remarks>If selecting only a single file/directory then you should use <see cref="Path"/> instead.</remarks>
		public IReadOnlyList<string> MultiSelected { get; private set; }
			= Enumerable.Empty<string> ().ToList ().AsReadOnly ();

		/// <inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			base.Redraw (bounds);

			if (!string.IsNullOrWhiteSpace (feedback)) {
				var feedbackWidth = feedback.Sum (c => Rune.ColumnWidth (c));
				var feedbackPadLeft = ((bounds.Width - feedbackWidth) / 2) - 1;

				feedbackPadLeft = Math.Min (bounds.Width, feedbackPadLeft);
				feedbackPadLeft = Math.Max (0, feedbackPadLeft);

				var feedbackPadRight = bounds.Width - (feedbackPadLeft + feedbackWidth + 2);
				feedbackPadRight = Math.Min (bounds.Width, feedbackPadRight);
				feedbackPadRight = Math.Max (0, feedbackPadRight);

				Move (0, Bounds.Height / 2);

				Driver.SetAttribute (new Attribute (Color.Red, this.ColorScheme.Normal.Background));
				Driver.AddStr (new string (' ', feedbackPadLeft));
				Driver.AddStr (feedback);
				Driver.AddStr (new string (' ', feedbackPadRight));
			}
		}

		/// <inheritdoc/>
		public override void OnLoaded ()
		{
			base.OnLoaded ();
			if (loaded) {
				return;
			}
			loaded = true;

			// May have been updated after instance was constructed
			this.btnOk.Text = Style.OkButtonText;
			this.btnUp.Text = this.GetUpButtonText();
			this.btnBack.Text = this.GetBackButtonText();
			this.btnForward.Text = this.GetForwardButtonText();
			this.btnToggleSplitterCollapse.Text = this.GetToggleSplitterText(false);
			
			tbPath.Autocomplete.ColorScheme.Normal = Attribute.Make (Color.Black, tbPath.ColorScheme.Normal.Background);

			treeView.AddObjects (Style.TreeRootGetter ());

			// if filtering on file type is configured then create the ComboBox and establish
			// initial filtering by extension(s)
			if (this.AllowedTypes.Any ()) {

				this.CurrentFilter = this.AllowedTypes [0];

				// Fiddle factor
				var width = this.AllowedTypes.Max (a => a.ToString ().Length) + 6;

				allowedTypeMenu = new MenuBarItem ("<placeholder>",
					allowedTypeMenuItems = AllowedTypes.Select (
						(a, i) => new MenuItem (a.ToString (), null, () => {
							AllowedTypeMenuClicked (i);
						}))
					.ToArray ());

				allowedTypeMenuBar = new MenuBar (new [] { allowedTypeMenu }) {
					Width = width,
					Y = 1,
					X = Pos.AnchorEnd (width),

					// TODO: Does not work, if this worked then we could tab to it instead
					// of having to hit F9
					CanFocus = true,
					TabStop = true
				};
				AllowedTypeMenuClicked (0);

				allowedTypeMenuBar.Enter += (s, e) => {
					allowedTypeMenuBar.OpenMenu (0);
				};

				allowedTypeMenuBar.DrawContentComplete += (s, e) => {
					
					allowedTypeMenuBar.Move (e.Rect.Width - 1, 0);
					Driver.AddRune (Driver.DownArrow);

				};

				this.Add (allowedTypeMenuBar);
			}

			// if no path has been provided
			if (this.tbPath.Text.Length <= 0) {
				this.tbPath.Text = Environment.CurrentDirectory;
			}

			// to streamline user experience and allow direct typing of paths
			// with zero navigation we start with focus in the text box and any
			// default/current path fully selected and ready to be overwritten
			this.tbPath.FocusFirst ();
			this.tbPath.SelectAll ();

			if (ustring.IsNullOrEmpty (Title)) {
				switch (OpenMode) {
				case OpenMode.File:
					this.Title = $"{Strings.fdOpen} {(MustExist ? Strings.fdExisting + " " : "")}{Strings.fdFile}";
					break;
				case OpenMode.Directory:
					this.Title = $"{Strings.fdOpen} {(MustExist ? Strings.fdExisting + " " : "")}{Strings.fdDirectory}";
					break;
				case OpenMode.Mixed:
					this.Title = $"{Strings.fdOpen} {(MustExist ? Strings.fdExisting : "")}";
					break;
				}
			}
			this.LayoutSubviews ();
		}

		private void AllowedTypeMenuClicked (int idx)
		{

			var allow = AllowedTypes [idx];
			for (int i = 0; i < AllowedTypes.Count; i++) {
				allowedTypeMenuItems [i].Checked = i == idx;
			}
			allowedTypeMenu.Title = allow.ToString ();

			this.CurrentFilter = allow;

			this.tbPath.ClearAllSelection ();
			this.tbPath.Autocomplete.ClearSuggestions ();

			if (this.State != null) {
				this.State.RefreshChildren ();
				this.WriteStateToTableView ();
			}
		}

		private void SuppressIfBadChar (KeyEventEventArgs k)
		{
			// don't let user type bad letters
			var ch = (char)k.KeyEvent.KeyValue;

			if (badChars.Contains (ch)) {
				k.Handled = true;
			}
		}

		private bool TreeView_KeyDown (KeyEvent keyEvent)
		{
			if (this.treeView.HasFocus && Separators.Contains ((char)keyEvent.KeyValue)) {
				this.tbPath.FocusFirst ();

				// let that keystroke go through on the tbPath instead
				return true;
			}

			return false;
		}

		private void AcceptIf (KeyEventEventArgs keyEvent, Key isKey)
		{
			if (!keyEvent.Handled && keyEvent.KeyEvent.Key == isKey) {
				keyEvent.Handled = true;

				// User hit Enter in text box so probably wants the
				// contents of the text box as their selection not
				// whatever lingering selection is in TableView
				this.Accept (false);
			}
		}

		private void Accept (IEnumerable<FileSystemInfoStats> toMultiAccept)
		{
			if (!this.AllowsMultipleSelection) {
				return;
			}

			// Don't include ".." (IsParent) in multiselections
			this.MultiSelected = toMultiAccept
				.Where(s=>!s.IsParent)
				.Select (s => s.FileSystemInfo.FullName)
				.ToList ().AsReadOnly ();

			this.tbPath.Text = this.MultiSelected.Count == 1 ? this.MultiSelected [0] : string.Empty;

			FinishAccept ();
		}

		private void Accept (IFileInfo f)
		{
			if (!this.IsCompatibleWithOpenMode (f.FullName, out var reason)) {
				feedback = reason;
				SetNeedsDisplay ();
				return;
			}

			this.tbPath.Text = f.FullName;

			if (AllowsMultipleSelection) {
				this.MultiSelected = new List<string> { f.FullName }.AsReadOnly ();
			}

			FinishAccept ();
		}

		private void Accept (bool allowMulti)
		{
			if(allowMulti && TryAcceptMulti())
			{
				return;
			}

			if (!this.IsCompatibleWithOpenMode (this.tbPath.Text.ToString (), out string reason)) {
				if (reason != null) {
					feedback = reason;
					SetNeedsDisplay ();
				}
				return;
			}

			FinishAccept ();
		}

		private void FinishAccept ()
		{
			var e = new FilesSelectedEventArgs (this);

			this.FilesSelected?.Invoke (this, e);

			if (e.Cancel) {
				return;
			}

			// if user uses Path selection mode (e.g. Enter in text box)
			// then also copy to MultiSelected
			if (AllowsMultipleSelection && (!MultiSelected.Any ())) {

				MultiSelected = string.IsNullOrWhiteSpace (Path) ?
						Enumerable.Empty<string> ().ToList ().AsReadOnly () :
						new List<string> () { Path }.AsReadOnly ();
			}

			this.Canceled = false;
			Application.RequestStop ();
		}

		private void NavigateIf (KeyEventEventArgs keyEvent, Key isKey, View to)
		{
			if (!keyEvent.Handled) {

				if (NavigateIf (keyEvent.KeyEvent, isKey, to)) {
					keyEvent.Handled = true;
				}
			}
		}

		private bool NavigateIf (KeyEvent keyEvent, Key isKey, View to)
		{
			if (keyEvent.Key == isKey) {

				to.FocusFirst ();
				if (to == tbPath) {
					tbPath.MoveEnd ();
				}
				return true;
			}

			return false;
		}

		private void TreeView_SelectionChanged (object sender, SelectionChangedEventArgs<object> e)
		{
			if (e.NewValue == null) {
				return;
			}

			this.tbPath.Text = FileDialogTreeBuilder.NodeToDirectory (e.NewValue).FullName;
		}

		private void UpdateNavigationVisibility ()
		{
			this.btnBack.Visible = this.history.CanBack ();
			this.btnForward.Visible = this.history.CanForward ();
			this.btnUp.Visible = this.history.CanUp ();
		}

		private void TableView_SelectedCellChanged (object sender, SelectedCellChangedEventArgs obj)
		{
			if (!this.tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows.Count == 0) {
				return;
			}

			if (this.tableView.MultiSelect && this.tableView.MultiSelectedRegions.Any ()) {
				return;
			}

			var stats = this.RowToStats (obj.NewRow);

			if (stats == null) {
				return;
			}
			IFileSystemInfo dest;

			if (stats.IsParent) {
				dest = State.Directory;
			} else {
				dest = stats.FileSystemInfo;
			}

			try {
				this.pushingState = true;

				this.tbPath.Text = dest.FullName;
				this.State.Selected = stats;
				this.tbPath.Autocomplete.ClearSuggestions ();

			} finally {

				this.pushingState = false;
			}

			if (obj.NewCol != obj.OldCol) {
				UpdateCollectionNavigator ();
			}
		}

		private bool TableView_KeyUp (KeyEvent keyEvent)
		{
			if (keyEvent.Key == Key.Backspace) {
				return this.history.Back ();
			}
			if (keyEvent.Key == (Key.ShiftMask | Key.Backspace)) {
				return this.history.Forward ();
			}

			if (keyEvent.Key == Key.DeleteChar) {

				Delete ();
				return true;
			}

			if (keyEvent.Key == (Key.CtrlMask | Key.R)) {

				Rename ();
				return true;
			}

			if (keyEvent.Key == (Key.CtrlMask | Key.N)) {
				New ();
				return true;
			}

			return false;
		}

		private void SetupTableColumns ()
		{
			this.dtFiles = new DataTable ();

			var nameStyle = this.tableView.Style.GetOrCreateColumnStyle (
				filenameColumn = this.dtFiles.Columns.Add (Style.FilenameColumnName, typeof (int))
				);
			nameStyle.RepresentationGetter = (i) => {

				var stats = this.State?.Children [(int)i];

				if (stats == null) {
					return string.Empty;
				}

				var icon = stats.IsParent ? null : Style.IconGetter?.Invoke (stats.FileSystemInfo);

				if (icon != null) {
					return icon + stats.Name;
				}
				return stats.Name;
			};

			nameStyle.MinWidth = 50;

			var sizeStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.SizeColumnName, typeof (int)));
			sizeStyle.RepresentationGetter = (i) => this.State?.Children [(int)i].HumanReadableLength ?? string.Empty;
			nameStyle.MinWidth = 10;

			var dateModifiedStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.ModifiedColumnName, typeof (int)));
			dateModifiedStyle.RepresentationGetter = (i) => 
			{
				var s = this.State?.Children [(int)i];
				if(s == null || s.IsParent || s.LastWriteTime == null)
				{
					return string.Empty;
				}
				return s.LastWriteTime.Value.ToString (Style.DateFormat);
			};

			dateModifiedStyle.MinWidth = 30;

			var typeStyle = this.tableView.Style.GetOrCreateColumnStyle (this.dtFiles.Columns.Add (Style.TypeColumnName, typeof (int)));
			typeStyle.RepresentationGetter = (i) => this.State?.Children [(int)i].Type ?? string.Empty;
			typeStyle.MinWidth = 6;

			foreach(var colStyle in Style.TableStyle.ColumnStyles) {
				colStyle.Value.ColorGetter = this.ColorGetter;
			}
			
		}

		private void CellActivate (object sender, CellActivatedEventArgs obj)
		{
			if(TryAcceptMulti())
			{
				return;
			}

			var stats = this.RowToStats (obj.Row);

			if (stats.FileSystemInfo is IDirectoryInfo d) {
				this.PushState (d, true);
				return;
			}

			if (stats.FileSystemInfo is IFileInfo f) {
				this.Accept (f);
			}
		}

		private bool TryAcceptMulti ()
		{
			var multi = this.MultiRowToStats ();
			string reason = null;
			
			if (!multi.Any ())
			{
				return false;
			}
			
			if (multi.All (m => this.IsCompatibleWithOpenMode (
				m.FileSystemInfo.FullName, out reason)))
			{
				this.Accept (multi);
				return true;
			} 
			else 
			{
				if (reason != null) {
					feedback = reason;
					SetNeedsDisplay ();
				}

				return false;
			}
		}

		/// <summary>
		/// Returns true if there are no <see cref="AllowedTypes"/> or one of them agrees
		/// that <paramref name="file"/> <see cref="IAllowedType.IsAllowed(string)"/>.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public bool IsCompatibleWithAllowedExtensions (IFileInfo file)
		{
			// no restrictions
			if (!this.AllowedTypes.Any ()) {
				return true;
			}
			return this.MatchesAllowedTypes (file);
		}

		private bool IsCompatibleWithAllowedExtensions (string path)
		{
			// no restrictions
			if (!this.AllowedTypes.Any ()) {
				return true;
			}

			return this.AllowedTypes.Any (t => t.IsAllowed (path));
		}

		/// <summary>
		/// Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private bool MatchesAllowedTypes (IFileInfo file)
		{
			return this.AllowedTypes.Any (t => t.IsAllowed (file.FullName));
		}
		private bool IsCompatibleWithOpenMode (string s, out string reason)
		{
			reason = null;
			if (string.IsNullOrWhiteSpace (s)) {
				return false;
			}

			if (!this.IsCompatibleWithAllowedExtensions (s)) {
				reason = Style.WrongFileTypeFeedback;
				return false;
			}

			switch (this.OpenMode) {
			case OpenMode.Directory:
				if (MustExist && !Directory.Exists (s)) {
					reason = Style.DirectoryMustExistFeedback;
					return false;
				}

				if (File.Exists (s)) {
					reason = Style.FileAlreadyExistsFeedback;
					return false;
				}
				return true;
			case OpenMode.File:

				if (MustExist && !File.Exists (s)) {
					reason = Style.FileMustExistFeedback;
					return false;
				}
				if (Directory.Exists (s)) {
					reason = Style.DirectoryAlreadyExistsFeedback;
					return false;
				}
				return true;
			case OpenMode.Mixed:
				if (MustExist && !File.Exists (s) && !Directory.Exists (s)) {
					reason = Style.FileOrDirectoryMustExistFeedback;
					return false;
				}
				return true;
			default: throw new ArgumentOutOfRangeException (nameof (this.OpenMode));
			}
		}

		/// <summary>
		/// Changes the dialog such that <paramref name="d"/> is being explored.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="addCurrentStateToHistory"></param>
		/// <param name="setPathText"></param>
		/// <param name="clearForward"></param>
		/// <param name="pathText">Optional alternate string to set path to.</param>
		internal void PushState (IDirectoryInfo d, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true, string pathText = null)
		{
			// no change of state
			if (d == this.State?.Directory) {
				return;
			}
			if (d.FullName == this.State?.Directory.FullName) {
				return;
			}

			PushState (new FileDialogState (d, this), addCurrentStateToHistory, setPathText, clearForward, pathText);
		}

		private void RefreshState ()
		{
			State.RefreshChildren ();
			PushState (State, false, false, false);
		}

		private void PushState (FileDialogState newState, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true, string pathText = null)
		{
			if (State is SearchState search) {
				search.Cancel ();
			}

			try {
				this.pushingState = true;

				// push the old state to history
				if (addCurrentStateToHistory) {
					this.history.Push (this.State, clearForward);
				}

				this.tbPath.Autocomplete.ClearSuggestions ();

				if(pathText != null)
				{
					this.tbPath.Text = pathText;
					this.tbPath.MoveEnd ();
				}
				else
				if (setPathText) {
					this.tbPath.Text = newState.Directory.FullName;
					this.tbPath.MoveEnd ();
				}

				this.State = newState;
				this.tbPath.Autocomplete.GenerateSuggestions (
					new AutocompleteFilepathContext (tbPath.Text, tbPath.CursorPosition, this.State));

				this.WriteStateToTableView ();

				if (clearForward) {
					this.history.ClearForward ();
				}

				this.tableView.RowOffset = 0;
				this.tableView.SelectedRow = 0;

				this.SetNeedsDisplay ();
				this.UpdateNavigationVisibility ();

			} finally {

				this.pushingState = false;
			}
			ClearFeedback ();
		}

		private void WriteStateToTableView ()
		{
			if (this.State == null) {
				return;
			}

			this.dtFiles.Rows.Clear ();

			for (int i = 0; i < this.State.Children.Length; i++) {
				this.BuildRow (i);
			}

			this.sorter.ApplySort ();
			this.tableView.Update ();
			UpdateCollectionNavigator ();
		}

		private void BuildRow (int idx)
		{
			this.tableView.Table.Rows.Add (idx, idx, idx, idx);
		}

		private ColorScheme ColorGetter (TableView.CellColorGetterArgs args)
		{
			var stats = this.RowToStats (args.RowIndex);

			if (!Style.UseColors) {
				return tableView.ColorScheme;
			}

			if (stats.IsDir ()) {
				return Style.ColorSchemeDirectory;
			}
			if (stats.IsImage ()) {
				return Style.ColorSchemeImage;
			}
			if (stats.IsExecutable ()) {
				return Style.ColorSchemeExeOrRecommended;
			}
			if (stats.FileSystemInfo is IFileInfo f && this.MatchesAllowedTypes (f)) {
				return Style.ColorSchemeExeOrRecommended;
			}

			return Style.ColorSchemeOther;
		}

		/// <summary>
		/// If <see cref="TableView.MultiSelect"/> is this returns a union of all
		/// <see cref="FileSystemInfoStats"/> in the selection.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<FileSystemInfoStats> MultiRowToStats ()
		{
			var toReturn = new HashSet<FileSystemInfoStats> ();

			if (this.AllowsMultipleSelection && this.tableView.MultiSelectedRegions.Any ()) {

				foreach (var p in this.tableView.GetAllSelectedCells ()) {

					var add = this.State?.Children [(int)this.tableView.Table.Rows [p.Y] [0]];
					if (add != null) {
						toReturn.Add (add);
					}
				}
			}

			return toReturn;
		}
		private FileSystemInfoStats RowToStats (int rowIndex)
		{
			return this.State?.Children [(int)this.tableView.Table.Rows [rowIndex] [0]];
		}
		private int? StatsToRow (IFileSystemInfo fileSystemInfo)
		{
			// find array index of the current state for the stats
			var idx = State?.Children.IndexOf ((f) => f.FileSystemInfo.FullName == fileSystemInfo.FullName);

			if (idx != -1 && idx != null) {

				// find the row number in our DataTable where the cell
				// contains idx
				var match = tableView.Table.Rows
					.Cast<DataRow> ()
					.Select ((r, rIdx) => new { row = r, rowIdx = rIdx })
					.Where (t => (int)t.row [0] == idx)
					.ToArray ();

				if (match.Length == 1) {
					return match [0].rowIdx;
				}
			}

			return null;
		}

		private void PathChanged ()
		{
			// avoid re-entry
			if (this.pushingState) {
				return;
			}

			var path = this.tbPath.Text?.ToString ();

			if (string.IsNullOrWhiteSpace (path)) {
				return;
			}

			var dir = this.StringToDirectoryInfo (path);

			if (dir.Exists) {
				this.PushState (dir, true, false);
			} else
			if (dir.Parent?.Exists ?? false) {
				this.PushState (dir.Parent, true, false);
			}

			tbPath.Autocomplete.GenerateSuggestions (new AutocompleteFilepathContext (tbPath.Text, tbPath.CursorPosition, State));
		}

		private IDirectoryInfo StringToDirectoryInfo (string path)
		{
			// if you pass new DirectoryInfo("C:") you get a weird object
			// where the FullName is in fact the current working directory.
			// really not what most users would expect
			if (Regex.IsMatch (path, "^\\w:$")) {
				return fileSystem.DirectoryInfo.New(path + System.IO.Path.DirectorySeparatorChar);
			}

			return fileSystem.DirectoryInfo.New(path);
		}

		/// <summary>
		/// Select <paramref name="toRestore"/> in the table view (if present)
		/// </summary>
		/// <param name="toRestore"></param>
		internal void RestoreSelection (IFileSystemInfo toRestore)
		{
			var toReselect = StatsToRow (toRestore);

			if (toReselect.HasValue) {
				tableView.SelectedRow = toReselect.Value;
				tableView.EnsureSelectedCellIsVisible ();
			}
		}
		private class FileDialogSorter {
			private readonly FileDialog dlg;
			private TableView tableView;

			private DataColumn currentSort = null;
			private bool currentSortIsAsc = true;

			public FileDialogSorter (FileDialog dlg, TableView tableView)
			{
				this.dlg = dlg;
				this.tableView = tableView;

				// if user clicks the mouse in TableView
				this.tableView.MouseClick += (s, e) => {

					var clickedCell = this.tableView.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out DataColumn clickedCol);

					if (clickedCol != null) {
						if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)) {

							// left click in a header
							this.SortColumn (clickedCol);
						} else if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

							// right click in a header
							this.ShowHeaderContextMenu (clickedCol, e);
						}
					} else {
						if (clickedCell != null && e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {

							// right click in rest of table
							this.ShowCellContextMenu (clickedCell, e);
						}
					}

				};

			}

			internal void ApplySort ()
			{
				var col = this.currentSort;

				// TODO: Consider preserving selection
				this.tableView.Table.Rows.Clear ();

				var colName = col == null ? null : StripArrows (col.ColumnName);

				var stats = this.dlg.State?.Children ?? new FileSystemInfoStats [0];

				// Do we sort on a column or just use the default sort order?
				Func<FileSystemInfoStats, object> sortAlgorithm;

				if (colName == null) {
					sortAlgorithm = (v) => v.GetOrderByDefault ();
					this.currentSortIsAsc = true;
				} else {
					sortAlgorithm = (v) => v.GetOrderByValue (dlg, colName);
				}

				// This portion is never reordered (aways .. at top then folders)
				var forcedOrder = stats.Select ((v, i) => new { v, i })
						.OrderByDescending (f => f.v.IsParent)
						.ThenBy (f => f.v.IsDir() ? -1:100);

				// This portion is flexible based on the column clicked (e.g. alphabetical)
				var ordered = 
					this.currentSortIsAsc ?
					    forcedOrder.ThenBy (f => sortAlgorithm (f.v)):
						forcedOrder.ThenByDescending (f => sortAlgorithm (f.v));

				foreach (var o in ordered) {
					this.dlg.BuildRow (o.i);
				}

				foreach (DataColumn c in this.tableView.Table.Columns) {

					// remove any lingering sort indicator
					c.ColumnName = StripArrows (c.ColumnName);

					// add a new one if this the one that is being sorted
					if (c == col) {
						c.ColumnName += this.currentSortIsAsc ? " (▲)" : " (▼)";
					}
				}

				this.tableView.Update ();
				dlg.UpdateCollectionNavigator ();
			}

			private static string StripArrows (string columnName)
			{
				return columnName.Replace (" (▼)", string.Empty).Replace (" (▲)", string.Empty);
			}

			private void SortColumn (DataColumn clickedCol)
			{
				this.GetProposedNewSortOrder (clickedCol, out var isAsc);
				this.SortColumn (clickedCol, isAsc);
			}

			internal void SortColumn (DataColumn col, bool isAsc)
			{
				// set a sort order
				this.currentSort = col;
				this.currentSortIsAsc = isAsc;

				this.ApplySort ();
			}

			private string GetProposedNewSortOrder (DataColumn clickedCol, out bool isAsc)
			{
				// work out new sort order
				if (this.currentSort == clickedCol && this.currentSortIsAsc) {
					isAsc = false;
					return $"{clickedCol.ColumnName} DESC";
				} else {
					isAsc = true;
					return $"{clickedCol.ColumnName} ASC";
				}
			}

			private void ShowHeaderContextMenu (DataColumn clickedCol, MouseEventEventArgs e)
			{
				var sort = this.GetProposedNewSortOrder (clickedCol, out var isAsc);

				var contextMenu = new ContextMenu (
					e.MouseEvent.X + 1,
					e.MouseEvent.Y + 1,
					new MenuBarItem (new MenuItem []
					{
						new MenuItem($"Hide {StripArrows(clickedCol.ColumnName)}", string.Empty, () => this.HideColumn(clickedCol)),
						new MenuItem($"Sort {StripArrows(sort)}",string.Empty, ()=> this.SortColumn(clickedCol,isAsc)),
					})
				);

				contextMenu.Show ();
			}

			private void ShowCellContextMenu (Point? clickedCell, MouseEventEventArgs e)
			{
				if (clickedCell == null) {
					return;
				}

				var contextMenu = new ContextMenu (
					e.MouseEvent.X + 1,
					e.MouseEvent.Y + 1,
					new MenuBarItem (new MenuItem []
					{
						new MenuItem($"New", string.Empty, () => dlg.New()),
						new MenuItem($"Rename",string.Empty, ()=>  dlg.Rename()),
						new MenuItem($"Delete",string.Empty, ()=>  dlg.Delete()),
					})
				);

				dlg.tableView.SetSelection (clickedCell.Value.X, clickedCell.Value.Y, false);

				contextMenu.Show ();
			}

			private void HideColumn (DataColumn clickedCol)
			{
				var style = this.tableView.Style.GetOrCreateColumnStyle (clickedCol);
				style.Visible = false;
				this.tableView.Update ();
			}
		}
		/// <summary>
		/// State representing a recursive search from <see cref="FileDialogState.Directory"/>
		/// downwards.
		/// </summary>
		internal class SearchState : FileDialogState {

			bool cancel = false;
			bool finished = false;

			// TODO: Add thread safe child adding
			List<FileSystemInfoStats> found = new List<FileSystemInfoStats> ();
			object oLockFound = new object ();
			CancellationTokenSource token = new CancellationTokenSource ();

			public SearchState (IDirectoryInfo dir, FileDialog parent, string searchTerms) : base (dir, parent)
			{
				parent.SearchMatcher.Initialize (searchTerms);
				Children = new FileSystemInfoStats [0];
				BeginSearch ();
			}

			private void BeginSearch ()
			{
				Task.Run (() => {
					RecursiveFind (Directory);
					finished = true;
				});

				Task.Run (() => {
					UpdateChildren ();
				});
			}

			private void UpdateChildren ()
			{
				lock (Parent.onlyOneSearchLock) {
					while (!cancel && !finished) {

						try {
							Task.Delay (250).Wait (token.Token);
						} catch (OperationCanceledException) {
							cancel = true;
						}

						if (cancel || finished) {
							break;
						}

						UpdateChildrenToFound ();
					}

					if (finished && !cancel) {
						UpdateChildrenToFound ();
					}

					Application.MainLoop.Invoke (() => {
						Parent.spinnerView.Visible = false;
					});
				}
			}

			private void UpdateChildrenToFound ()
			{
				lock (oLockFound) {
					Children = found.ToArray ();
				}

				Application.MainLoop.Invoke (() => {
					Parent.tbPath.Autocomplete.GenerateSuggestions (
						new AutocompleteFilepathContext (Parent.tbPath.Text, Parent.tbPath.CursorPosition, this)
						);
					Parent.WriteStateToTableView ();

					Parent.spinnerView.Visible = true;
					Parent.spinnerView.SetNeedsDisplay ();
				});
			}

			private void RecursiveFind (IDirectoryInfo directory)
			{
				foreach (var f in GetChildren (directory)) {

					if (cancel) {
						return;
					}

					if (f.IsParent) {
						continue;
					}

					lock (oLockFound) {
						if (found.Count >= FileDialog.MaxSearchResults) {
							finished = true;
							return;
						}
					}

					if (Parent.SearchMatcher.IsMatch (f.FileSystemInfo)) {
						lock (oLockFound) {
							found.Add (f);
						}
					}

					if (f.FileSystemInfo is IDirectoryInfo sub) {
						RecursiveFind (sub);
					}
				}
			}

			internal override void RefreshChildren ()
			{
			}

			/// <summary>
			/// Cancels the current search (if any).  Returns true if a search
			/// was running and cancellation was successfully set.
			/// </summary>
			/// <returns></returns>
			internal bool Cancel ()
			{
				var alreadyCancelled = token.IsCancellationRequested || cancel; 

				cancel = true;
				token.Cancel ();

				return !alreadyCancelled;
			}
		}
	}
}