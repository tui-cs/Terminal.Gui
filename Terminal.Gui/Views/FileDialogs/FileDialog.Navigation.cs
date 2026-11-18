using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Terminal.Gui.Views;

public partial class FileDialog
{
    /// <summary>Changes the dialog such that <paramref name="d"/> is being explored.</summary>
    /// <param name="d"></param>
    /// <param name="addCurrentStateToHistory"></param>
    /// <param name="setPathText"></param>
    /// <param name="clearForward"></param>
    /// <param name="pathText">Optional alternate string to set path to.</param>
    internal void PushState (IDirectoryInfo d, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true, string? pathText = null)
    {
        // no change of state
        if (d == State?.Directory)
        {
            return;
        }

        if (d.FullName == State?.Directory.FullName)
        {
            return;
        }

        PushState (new FileDialogState (d, this), addCurrentStateToHistory, setPathText, clearForward, pathText);
    }

    /// <summary>Select <paramref name="toRestore"/> in the table view (if present)</summary>
    /// <param name="toRestore"></param>
    internal void RestoreSelection (IFileSystemInfo? toRestore)
    {
        int row = State!.Children.IndexOf (r => r.FileSystemInfo == toRestore);
        _tableView.SetSelection (0, row >= 0 ? row : 0, false);
        _tableView.EnsureCursorIsVisible ();
    }

    private bool CancelSearch ()
    {
        if (State is SearchState search)
        {
            return search.Cancel ();
        }

        return false;
    }

    private string GetBackButtonText () => Glyphs.LeftArrow + "-";

    private string GetForwardButtonText () => "-" + Glyphs.RightArrow;

    private string GetUpButtonText () => Style.UseUnicodeCharacters ? "◭" : "▲";

    private void PathChanged ()
    {
        // avoid re-entry
        if (_pushingState)
        {
            return;
        }

        string path = _tbPath.Text;

        if (string.IsNullOrWhiteSpace (path))
        {
            return;
        }

        IDirectoryInfo dir = StringToDirectoryInfo (path);

        if (dir.Exists)
        {
            PushState (dir, true, false);
        }
        else if (dir.Parent?.Exists ?? false)
        {
            PushState (dir.Parent, true, false);
        }

        if (State is { })
        {
            _tbPath.Autocomplete?.GenerateSuggestions (new AutocompleteFilepathContext (_tbPath.Text, _tbPath.InsertionPoint, State));
        }
    }

    private void PushState (FileDialogState newState, bool addCurrentStateToHistory, bool setPathText = true, bool clearForward = true, string? pathText = null)
    {
        if (State is SearchState search)
        {
            search.Cancel ();
        }

        try
        {
            _pushingState = true;

            // push the old state to history
            if (addCurrentStateToHistory)
            {
                _history.Push (State, clearForward);
            }

            _tbPath.Autocomplete?.ClearSuggestions ();

            if (pathText is { })
            {
                Path = pathText;
            }
            else if (setPathText)
            {
                SetPathToSelectedObject (newState.Directory);
            }

            State = newState;

            _tbPath.Autocomplete!.GenerateSuggestions (new AutocompleteFilepathContext (_tbPath.Text, _tbPath.InsertionPoint, State));

            WriteStateToTableView ();

            if (clearForward)
            {
                _history.ClearForward ();
            }

            if (_tableView.Viewport.Y != 0)
            {
                _tableView.Viewport = _tableView.Viewport with { Y = 0 };
            }

            if (_tableView.Viewport.X != 0)
            {
                _tableView.Viewport = _tableView.Viewport with { X = 0 };
            }
            _tableView.SetSelection (0, 0, false);

            SetNeedsDraw ();
            UpdateNavigationVisibility ();
        }
        finally
        {
            _pushingState = false;
        }

        ClearFeedback ();
    }

    private void RefreshState ()
    {
        State!.RefreshChildren ();
        PushState (State, false, false, false);
    }

    private void RestartSearch ()
    {
        if (_disposed || State?.Directory is null)
        {
            return;
        }

        if (State is SearchState oldSearch)
        {
            oldSearch.Cancel ();
        }

        // user is clearing search terms
        if (_tbFind.Text.Length == 0)
        {
            // Wait for search cancellation (if any) to finish
            // then push the current dir state
            lock (_onlyOneSearchLock)
            {
                PushState (new FileDialogState (State.Directory, this), false);
            }

            return;
        }

        PushState (new SearchState (State?.Directory!, this, _tbFind.Text), true);
    }

    private IDirectoryInfo StringToDirectoryInfo (string path)
    {
        // if you pass new DirectoryInfo("C:") you get a weird object
        // where the FullName is in fact the current working directory.
        // really not what most users would expect
        if (Regex.IsMatch (path, "^\\w:$"))
        {
            return _fileSystem!.DirectoryInfo.New (path + _fileSystem.Path.DirectorySeparatorChar);
        }

        return _fileSystem!.DirectoryInfo.New (path);
    }

    private static void SuppressIfBadChar (Key k)
    {
        // Only suppress actual typed printable characters.
        // Navigation keys (Home/End/etc.) must pass through to key bindings.
        if (k.IsAlt || k.IsCtrl)
        {
            return;
        }

        string grapheme = k.AsGrapheme;

        if (grapheme.Length != 1)
        {
            return;
        }

        char ch = grapheme [0];

        if (_badChars.Contains (ch))
        {
            k.Handled = true;
        }
    }

    private void TreeView_SelectionChanged (object? sender, SelectionChangedEventArgs<IFileSystemInfo> e) => SetPathToSelectedObject (e.NewValue);

    private void SetPathToSelectedObject (IFileSystemInfo? selected)
    {
        if (selected is null)
        {
            return;
        }

        if (selected is IDirectoryInfo && Style.PreserveFilenameOnDirectoryChanges)
        {
            if (!string.IsNullOrWhiteSpace (Path) && _fileSystem is { } && !_fileSystem.Directory.Exists (Path))
            {
                string currentFile = _fileSystem.Path.GetFileName (Path);

                if (!string.IsNullOrWhiteSpace (currentFile))
                {
                    Path = _fileSystem.Path.Combine (selected.FullName, currentFile);

                    return;
                }
            }
        }

        string path = _tbPath.Text;

        if (string.IsNullOrWhiteSpace (path))
        {
            return;
        }

        Path = selected.FullName;
    }

    private void UpdateNavigationVisibility ()
    {
        _btnBack.Visible = _history.CanBack ();
        _btnForward.Visible = _history.CanForward ();
        _btnUp.Visible = _history.CanUp ();
    }

    // --- Tree visibility management ---

    private void ToggleTreeVisibility () => SetTreeVisible (!_treeView.Visible);

    private void SetTreeVisible (bool visible)
    {
        _treeView.Enabled = visible;
        _treeView.Visible = visible;

        if (visible)
        {
            // When visible, the table view's left edge is a splitter next to the tree
            _treeView.Width = Dim.Fill (_tableViewContainer);
            _treeView.Height = Dim.Height (_tableViewContainer);
            _tableViewContainer.X = 30;
            _tableViewContainer.Arrangement = ViewArrangement.LeftResizable;
            _tableViewContainer.Border.Thickness = new Thickness (1, 0, 0, 0);
        }
        else
        {
            // When hidden, table occupies full width and splitter is hidden/disabled
            _treeView.Width = 0;
            _treeView.Height = 0;
            _tableViewContainer.X = 0;
            _tableViewContainer.Width = Dim.Fill ();
            _tableViewContainer.Arrangement = ViewArrangement.Fixed;
            _tableViewContainer.Border.Thickness = new Thickness (1, 0, 0, 0);
        }
        _btnTreeToggle.Text = GetTreeToggleText (visible);

        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    private string GetTreeToggleText (bool visible) => visible ? $"{Glyphs.LeftArrow}{Strings.fdTree}" : $"{Glyphs.RightArrow}{Strings.fdTree}";
}
