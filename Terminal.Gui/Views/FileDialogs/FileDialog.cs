using System.Collections.ObjectModel;
using System.IO.Abstractions;
using Terminal.Gui.FileServices;

namespace Terminal.Gui.Views;

/// <summary>
///     The base-class for <see cref="OpenDialog"/> and <see cref="SaveDialog"/>
/// </summary>
/// <remarks>
/// <img src="../images/views/FileDialog.gif" alt="FileDialog demo"/>
/// </remarks>
public partial class FileDialog : Dialog<IReadOnlyList<string>?>, IDesignable
{
    /// <summary>Gets the Path separators for the operating system</summary>

    // ReSharper disable once InconsistentNaming
    internal static readonly char [] Separators = [System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar];

    /// <summary>
    ///     Characters to prevent entry into <see cref="_tbPath"/>. Note that this is not using
    ///     <see cref="System.IO.Path.GetInvalidFileNameChars"/> because we do want to allow directory separators, arrow keys
    ///     etc.
    /// </summary>
    private static readonly char [] _badChars = ['"', '<', '>', '|', '*', '?'];

    /// <summary>Locking object for ensuring only a single <see cref="SearchState"/> executes at once.</summary>
    internal readonly object _onlyOneSearchLock = new ();

    private readonly IFileSystem? _fileSystem;

    private readonly Button _btnBack;

    private readonly Button _btnForward;
    private readonly Button _btnCancel;

    /// <summary>
    ///     Gets whether the dialog was canceled (i.e., the user dismissed it without accepting a selection).
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="true"/> if <see cref="IRunnable{TResult}.Result"/> is <see langword="null"/>.
    /// </remarks>
    public bool Canceled => Result is null;

    /// <summary>
    ///     Gets the index of the cancel button for the dialog.
    /// </summary>
    public int CancelButtonIndex => Buttons.IndexOf (_btnCancel);

    private readonly Button _btnOk;
    private readonly Button _btnUp;
    private readonly FileDialogHistory _history;
    private readonly SpinnerView _spinnerView;
    private readonly View _tableViewContainer;
    private readonly TableView _tableView;
    private readonly TextField _tbFind;
    private readonly TextField _tbPath;
    private readonly Button _btnTreeToggle;
    private readonly TreeView<IFileSystemInfo> _treeView;
    private Dictionary<IDirectoryInfo, string> _treeRoots = new ();
    private readonly DropDownList? _typeFilterDropDown;
    private int _currentSortColumn;
    private bool _currentSortIsAsc = true;
    private bool _disposed;
    private string? _feedback;

    private bool _pushingState;

    /// <summary>Initializes a new instance of the <see cref="FileDialog"/> class.</summary>
    public FileDialog () : this (new FileSystem ()) { }

    /// <summary>Initializes a new instance of the <see cref="FileDialog"/> class with a custom <see cref="IFileSystem"/>.</summary>
    /// <remarks>This overload is mainly useful for testing.</remarks>
    internal FileDialog (IFileSystem? fileSystem)
    {
        Height = Dim.Percent (80);
        Width = Dim.Percent (80);

        _fileSystem = fileSystem;
        Style = new FileDialogStyle (fileSystem);
        ButtonAlignment = Alignment.End;
        ButtonAlignmentModes = AlignmentModes.IgnoreFirstOrLast;

        // Ensure we get Accept for any subviews; esp TreeView
        CommandsToBubbleUp = [Command.Accept];

        _btnCancel = new Button { Text = Strings.btnCancel };

        _btnOk = new Button { Text = Style.OkButtonText };

        _btnUp = new Button { X = 0, Y = 1, NoPadding = true };
        _btnUp.Text = GetUpButtonText ();

        _btnUp.Accepting += (_, e) =>
                            {
                                _history?.Up ();
                                e.Handled = true;
                            };

        _btnBack = new Button { X = Pos.Right (_btnUp) + 1, Y = 1, NoPadding = true };
        _btnBack.Text = GetBackButtonText ();

        _btnBack.Accepting += (_, e) =>
                              {
                                  _history?.Back ();
                                  e.Handled = true;
                              };

        _btnForward = new Button { X = Pos.Right (_btnBack) + 1, Y = 1, NoPadding = true };
        _btnForward.Text = GetForwardButtonText ();

        _btnForward.Accepting += (_, e) =>
                                 {
                                     _history?.Forward ();
                                     e.Handled = true;
                                 };

        _tbPath = new TextField
        {
            // This sets the default width of the FileDialog as it is the widest subview
            Width = Dim.Fill ()
        };

        _tbPath.KeyDown += (_, k) =>
                           {
                               ClearFeedback ();

                               AcceptIf (k, KeyCode.Enter);

                               SuppressIfBadChar (k);
                           };

        _tbPath.Autocomplete = new AppendAutocomplete (_tbPath);
        _tbPath.Autocomplete.SuggestionGenerator = new FilepathSuggestionGenerator ();

        _typeFilterDropDown = new DropDownList
        {
            X = Pos.AnchorEnd (),
            Y = 1,
            Visible = false
        };
        Add (_typeFilterDropDown);

        // Create table view container (right pane)
        _tableViewContainer = new View
        {
            X = -1,
            Y = Pos.Bottom (_btnBack),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Arrangement = ViewArrangement.LeftResizable,
            BorderStyle = LineStyle.Dashed,
            SuperViewRendersLineCanvas = true,
            TabStop = TabBehavior.TabStop,
            CanFocus = true,
            Id = "_tableViewContainer"
        };

        // Tree toggle button - Goes in Dialog Button Area
        _btnTreeToggle = new Button { NoPadding = true };

        _btnTreeToggle.Accepting += (_, e) =>
                                    {
                                        e.Handled = true;
                                        ToggleTreeVisibility ();
                                    };

        // Create tree view container (left pane)
        _treeView = new TreeView<IFileSystemInfo>
        {
            X = 0,
            Y = Pos.Bottom (_btnBack),
            Width = Dim.Fill (30, _tableViewContainer),
            Height = Dim.Height (_tableViewContainer),
            Visible = true
        };

        var fileDialogTreeBuilder = new FileSystemTreeBuilder { IncludeFiles = false };
        _treeView.TreeBuilder = fileDialogTreeBuilder;
        _treeView.AspectGetter = AspectGetter;
        Style.TreeStyle = _treeView.Style;

        _treeView.SelectionChanged += TreeView_SelectionChanged;
        _treeView.KeystrokeNavigator.Matcher = new FileSystemCollectionNavigationMatcher ();

        _tableView = new TableView { Width = Dim.Fill (), Height = Dim.Fill (_tbFind!) - 1, FullRowSelect = true, Id = "_tableView" };
        _tableView.CollectionNavigator = new FileDialogCollectionNavigator (this, _tableView);
        _tableView.KeyBindings.ReplaceCommands (Key.Space, Command.Toggle);
        _tableView.Activating += OnTableViewActivating;
        _tableView.ViewportSettings |= ViewportSettingsFlags.HasScrollBars;

        Style.TableStyle = _tableView.Style;

        ColumnStyle nameStyle = Style.TableStyle.GetOrCreateColumnStyle (0);
        nameStyle.MinWidth = 10;
        nameStyle.ColorGetter = ColorGetter;

        ColumnStyle sizeStyle = Style.TableStyle.GetOrCreateColumnStyle (1);
        sizeStyle.MinWidth = 10;
        sizeStyle.ColorGetter = ColorGetter;

        ColumnStyle dateModifiedStyle = Style.TableStyle.GetOrCreateColumnStyle (2);
        dateModifiedStyle.MinWidth = 30;
        dateModifiedStyle.ColorGetter = ColorGetter;

        ColumnStyle typeStyle = Style.TableStyle.GetOrCreateColumnStyle (3);
        typeStyle.MinWidth = 6;
        typeStyle.ColorGetter = ColorGetter;

        _tableViewContainer.Add (_tableView);

        _tableView.Style.ShowHorizontalHeaderOverline = false;
        _tableView.Style.ShowVerticalCellLines = true;
        _tableView.Style.ShowVerticalCellLineForFirstColumn = false;
        _tableView.Style.ShowVerticalCellLineForLastColumn = false;
        _tableView.Style.ShowVerticalHeaderLines = true;
        _tableView.Style.AlwaysShowHeaders = true;
        _tableView.Style.ShowHorizontalHeaderUnderline = false;
        _tableView.Style.ShowHorizontalBottomLine = false;
        _tableView.Accepted += TableViewOnAccepted;
        _tableView.KeyDown += (_, k) => k.Handled = TableView_KeyDown (k);
        _tableView.ValueChanged += TableViewOnValueChanged;

        _tableView.KeyBindings.ReplaceCommands (Key.Home, Command.Start);
        _tableView.KeyBindings.ReplaceCommands (Key.End, Command.End);
        _tableView.KeyBindings.ReplaceCommands (Key.Home.WithShift, Command.StartExtend);
        _tableView.KeyBindings.ReplaceCommands (Key.End.WithShift, Command.EndExtend);
        _history = new FileDialogHistory (this);

        // Changing the key-bindings of a View is not allowed, however,
        // by default, Runnable doesn't bind to Command.Context, so
        // we can take advantage of the CommandNotBound event to handle it
        _tableView.CommandNotBound += TableViewHandleCommandNotBound;

        // ReplaceCommands, not Add: PopoverMenu.DefaultKey is configurable and may collide with a key
        // TableView already binds (e.g. Ctrl+P -> Command.Up); the user's context-menu key wins.
        _tableView.KeyBindings.ReplaceCommands (PopoverMenu.DefaultKey, Command.Context);
        _tableView.MouseBindings.Add (MouseFlags.RightButtonClicked, Command.Context);

        _tbPath.TextChanged += (_, _) => PathChanged ();

        _tbFind = new TextField { X = 0, Width = Dim.Width (_tableView) - 1, Y = Pos.AnchorEnd (), Id = "_tbFind" };

        _spinnerView = new SpinnerView
        {
            // The spinner view is positioned over the last column of _tbFind
            X = Pos.Right (_tbFind) - 8,
            Y = Pos.Top (_tbFind),
            Width = Dim.Auto (),
            Visible = false,
            Style = new SpinnerStyle.Aesthetic (),
            Arrangement = ViewArrangement.Overlapped
        };

        _tbFind.TextChanged += (_, _) => RestartSearch ();

        _tbFind.KeyDown += (_, o) =>
                           {
                               if (o.KeyCode == KeyCode.Enter)
                               {
                                   RestartSearch ();
                                   o.Handled = true;
                               }

                               if (o.KeyCode == KeyCode.Esc && CancelSearch ())
                               {
                                   o.Handled = true;
                               }
                           };

        AllowsMultipleSelection = false;

        UpdateNavigationVisibility ();

        // Add the toggle along with OK/Cancel so they align as a group
        AddButton (_btnTreeToggle);
        AddButton (_btnCancel);
        AddButton (_btnOk);

        Add (_tbPath);
        Add (_btnUp);
        Add (_btnBack);
        Add (_btnForward);
        Add (_treeView);

        Add (_tableViewContainer);
        _tableViewContainer.Add (_tbFind);
        _tableViewContainer.Add (_spinnerView);

        // to streamline user experience and allow direct typing of paths
        // with zero navigation we start with focus in the text box and any
        // default/current path fully selected and ready to be overwritten
        _tbPath.SetFocus ();

        SetTreeVisible (false);
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        // Style may have been updated after instance was constructed
        _btnOk.Text = Style.OkButtonText;
        _btnCancel.Text = Style.CancelButtonText;
        _btnUp.Text = GetUpButtonText ();
        _btnBack.Text = GetBackButtonText ();
        _btnForward.Text = GetForwardButtonText ();
        _tbPath.Title = Style.PathCaption;
        _tbFind.Title = Style.SearchCaption;
        _treeRoots = Style.TreeRootGetter ();
        Style.IconProvider.IsOpenGetter = _treeView.IsExpanded;
        _treeView.AddObjects (_treeRoots.Keys);

        // if filtering on file type is configured then create the DropDownList and establish
        // initial filtering by extension(s)
        if (AllowedTypes.Count > 0)
        {
            CurrentFilter = AllowedTypes [0];

            _typeFilterDropDown?.Visible = true;
            _typeFilterDropDown?.Source = new ListWrapper<string> (new ObservableCollection<string> (AllowedTypes.Select (a => a.ToString ()!).ToList ()));
            _typeFilterDropDown?.Value = AllowedTypes [0].ToString () ?? string.Empty;

            if (State is { })
            {
                RefreshState ();
            }
        }

        // if no path has been provided
        if (Path.Length <= 0)
        {
            Path = _fileSystem!.Directory.GetCurrentDirectory ();
        }

        _tbPath.SelectAll ();

        if (string.IsNullOrEmpty (Title))
        {
            Title = GetDefaultTitle ();
        }
    }

    /// <summary>
    ///     Gets or Sets a collection of file types that the user can/must select. Only applies when
    ///     <see cref="OpenMode"/> is <see cref="OpenMode.File"/> or <see cref="OpenMode.Mixed"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="AllowedTypeAny"/> adds the option to select any type (*.*). If this collection is empty then any
    ///     type is supported and no Types drop-down is shown.
    /// </remarks>
    public List<IAllowedType> AllowedTypes { get; set; } = [];

    /// <summary>
    ///     Gets or Sets a value indicating whether to allow selecting multiple existing files/directories. Defaults to
    ///     false.
    /// </summary>
    public bool AllowsMultipleSelection { get => _tableView.MultiSelect; set => _tableView.MultiSelect = value; }

    /// <summary>The UI selected <see cref="IAllowedType"/> from combo box. May be null.</summary>
    public IAllowedType? CurrentFilter { get; private set; }

    /// <summary>
    ///     Gets or sets behavior of the <see cref="FileDialog"/> when the user attempts to delete a selected file(s). Set
    ///     to null to prevent deleting.
    /// </summary>
    /// <remarks>
    ///     Ensure you use a try/catch block with appropriate error handling (e.g. showing a <see cref="MessageBox"/>
    /// </remarks>
    public IFileOperations FileOperationsHandler { get; set; } = new DefaultFileOperations ();

    /// <summary>The maximum number of results that will be collected when searching before stopping.</summary>
    /// <remarks>This prevents performance issues e.g. when searching root of file system for a common letter (e.g. 'e').</remarks>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static int MaxSearchResults
    {
        get => FileDialogSettings.Defaults.MaxSearchResults;
        set => FileDialogSettings.Defaults.MaxSearchResults = value;
    }

    /// <summary>
    ///     Gets all files/directories selected or an empty collection <see cref="AllowsMultipleSelection"/> is
    ///     <see langword="false"/> or <see cref="CancelSearch"/>.
    /// </summary>
    /// <remarks>If selecting only a single file/directory then you should use <see cref="Path"/> instead.</remarks>
    public IReadOnlyList<string> MultiSelected { get; private set; } = Enumerable.Empty<string> ().ToList ().AsReadOnly ();

    /// <summary>
    ///     True if the file/folder must exist already to be selected. This prevents user from entering the name of
    ///     something that doesn't exist. Defaults to false.
    /// </summary>
    public bool MustExist { get; set; }

    /// <summary>
    ///     Gets or Sets which <see cref="System.IO.FileSystemInfo"/> type can be selected. Defaults to
    ///     <see cref="OpenMode.Mixed"/> (i.e. <see cref="DirectoryInfo"/> or <see cref="FileInfo"/>).
    /// </summary>
    public virtual OpenMode OpenMode { get; set; } = OpenMode.Mixed;

    /// <summary>
    ///     Gets or Sets the selected path in the dialog. This is the result that should be used if
    ///     <see cref="AllowsMultipleSelection"/> is off and <see cref="CancelSearch"/> is true.
    /// </summary>
    public string Path
    {
        get => _tbPath.Text;
        set
        {
            _tbPath.Text = value;
            _tbPath.MoveEnd ();
        }
    }

    /// <summary>
    ///     Defines how the dialog matches files/folders when using the search box. Provide a custom implementation if you
    ///     want to tailor how matching is performed.
    /// </summary>
    public ISearchMatcher SearchMatcher { get; set; } = new DefaultSearchMatcher ();

    /// <summary>
    ///     Gets settings for controlling how visual elements behave.  Style changes should be made before the
    ///     <see cref="Dialog"/> is loaded and shown to the user for the first time.
    /// </summary>
    public FileDialogStyle Style { get; }

    /// <summary>Gets the currently open directory and known children presented in the dialog.</summary>
    internal FileDialogState? State { get; private set; }

    /// <summary>
    ///     Event fired when user attempts to confirm a selection (or multi selection). Allows you to cancel the selection
    ///     or undertake alternative behavior e.g. open a dialog "File already exists, Overwrite? yes/no".
    /// </summary>

    // TODO: Refactor to use CWP
    public event EventHandler<FilesSelectedEventArgs>? FilesSelected;

    /// <summary>
    ///     Gets a default dialog title, when <see cref="View.Title"/> is not set or empty, result of the function will be
    ///     shown.
    /// </summary>
    protected virtual string GetDefaultTitle ()
    {
        List<string> titleParts = [Strings.fdOpen];

        if (MustExist)
        {
            titleParts.Add (Strings.fdExisting);
        }

        switch (OpenMode)
        {
            case OpenMode.File:
                titleParts.Add (Strings.fdFile);

                break;

            case OpenMode.Directory:
                titleParts.Add (Strings.fdDirectory);

                break;
        }

        return string.Join (' ', titleParts);
    }

    /// <summary>State representing a recursive search from <see cref="FileDialogState.Directory"/> downwards.</summary>
    internal sealed class SearchState : FileDialogState
    {
        // TODO: Add thread safe child adding
        private readonly List<FileSystemInfoStats> _found = [];
        private readonly Lock _oLockFound = new ();
        private readonly CancellationTokenSource _token = new ();
        private bool _cancel;
        private bool _finished;

        public SearchState (IDirectoryInfo dir, FileDialog parent, string searchTerms) : base (dir, parent, skipInitialEnumeration: true)
        {
            parent.SearchMatcher.Initialize (searchTerms);
            Children = [];
            BeginSearch ();
            RefreshChildren ();
        }

        /// <summary>
        ///     Cancels the current search (if any).  Returns true if a search was running and cancellation was successfully
        ///     set.
        /// </summary>
        /// <returns></returns>
        internal bool Cancel ()
        {
            bool alreadyCancelled = _token.IsCancellationRequested || _cancel;

            _cancel = true;
            _token.Cancel ();

            return !alreadyCancelled;
        }

        private void BeginSearch ()
        {
            Task.Run (() =>
                      {
                          RecursiveFind (Directory);
                          _finished = true;
                      });

            Task.Run (UpdateChildren);
        }

        private void RecursiveFind (IDirectoryInfo directory)
        {
            foreach (FileSystemInfoStats f in GetChildren (directory))
            {
                if (_cancel)
                {
                    return;
                }

                if (f.IsParent)
                {
                    continue;
                }

                lock (_oLockFound)
                {
                    if (_found.Count >= MaxSearchResults)
                    {
                        _finished = true;

                        return;
                    }
                }

                if (Parent.SearchMatcher.IsMatch (f.FileSystemInfo!))
                {
                    lock (_oLockFound)
                    {
                        _found.Add (f);
                    }
                }

                if (f.FileSystemInfo is IDirectoryInfo sub && !FileSystemTreeBuilder.IsReparsePoint (sub))
                {
                    RecursiveFind (sub);
                }
            }
        }

        private void UpdateChildren ()
        {
            lock (Parent._onlyOneSearchLock)
            {
                while (!_cancel && !_finished)
                {
                    try
                    {
                        Task.Delay (250).Wait (_token.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _cancel = true;
                    }

                    if (_cancel || _finished)
                    {
                        break;
                    }

                    UpdateChildrenToFound ();
                }

                if (_finished && !_cancel)
                {
                    UpdateChildrenToFound ();
                }

                Parent.App?.Invoke (_ => { Parent._spinnerView.Visible = false; });
            }
        }

        private void UpdateChildrenToFound ()
        {
            lock (_oLockFound)
            {
                Children = _found.ToArray ();
            }

            Parent.App?.Invoke (_ =>
                                {
                                    Parent._tbPath.Autocomplete?.GenerateSuggestions (new AutocompleteFilepathContext (Parent._tbPath.Text,
                                                                                          Parent._tbPath.InsertionPoint,
                                                                                          this));
                                    Parent.WriteStateToTableView ();
                                    Parent._spinnerView.AutoSpin = true;
                                    Parent._spinnerView.Visible = true;
                                    Parent._spinnerView.SetNeedsDraw ();
                                });
        }
    }

    /// <inheritdoc/>
    string? IDesignable.GetDemoKeyStrokes () => "wait:500,Alt+T,wait:300,Shift+Tab,Shift+Tab,wait:300,CursorRight,wait:300,Tab,wait:300,Alt+T,wait:300,Alt+F,wait:300,`.md`,wait:800,Shift+Tab,wait:300,Enter,wait:800";

    bool IDesignable.EnableForDesign ()
    {
        OnIsRunningChanged (true);

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        _disposed = true;
        base.Dispose (disposing);

        CancelSearch ();
    }
}
