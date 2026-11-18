using System.ComponentModel;
using System.IO.Abstractions;

namespace Terminal.Gui.Views;

public partial class FileDialog
{
    /// <summary>
    ///     Returns true if there are no <see cref="AllowedTypes"/> or one of them agrees that <paramref name="file"/>
    ///     <see cref="IAllowedType.IsAllowed(string)"/>.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public bool IsCompatibleWithAllowedExtensions (IFileInfo file) => AllowedTypes.Count == 0 || MatchesAllowedTypes (file);

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // Check if the source is the Cancel button - if so, leave Result as null and stop
        View? sourceView = null;
        args.Context?.Source?.TryGetTarget (out sourceView);

        if (sourceView == _btnCancel)
        {
            // Explicitly clear Result so Canceled == true even if the dialog was previously accepted
            Result = null;

            if (IsModal)
            {
                App?.RequestStop ();
            }

            return true;
        }

        if (Accept (true))
        {
            return base.OnAccepting (args);
        }

        return false;
    }

    private void Accept (IEnumerable<FileSystemInfoStats> toMultiAccept)
    {
        if (!AllowsMultipleSelection)
        {
            return;
        }

        // Don't include ".." (IsParent) in multi-selections
        MultiSelected = toMultiAccept.Where (s => !s.IsParent).Select (s => s.FileSystemInfo!.FullName).ToList ().AsReadOnly ();

        Path = MultiSelected.Count == 1 ? MultiSelected [0] : string.Empty;

        FinishAccept ();
    }

    private void Accept (IFileInfo f)
    {
        if (!IsCompatibleWithOpenMode (f.FullName, out string reason))
        {
            _feedback = reason;
            SetNeedsDraw ();

            return;
        }

        Path = f.FullName;

        if (AllowsMultipleSelection)
        {
            MultiSelected = new List<string> { f.FullName }.AsReadOnly ();
        }

        FinishAccept ();
    }

    private bool Accept (bool allowMulti)
    {
        if (allowMulti && TryAcceptMulti ())
        {
            return false;
        }

        if (IsCompatibleWithOpenMode (_tbPath.Text, out string reason))
        {
            return FinishAccept ();
        }
        _feedback = reason;
        SetNeedsDraw ();

        return false;
    }

    private void AcceptIf (Key key, KeyCode isKey)
    {
        if (key.Handled || key.KeyCode != isKey)
        {
            return;
        }

        key.Handled = true;

        // User hit Enter in text box so probably wants the
        // contents of the text box as their selection not
        // whatever lingering selection is in TableView
        Accept (false);
    }

    private void Delete ()
    {
        IFileSystemInfo [] toDelete = GetFocusedFiles ()!;

        if (FileOperationsHandler.Delete (App, toDelete))
        {
            RefreshState ();
        }
    }

    private bool FinishAccept ()
    {
        FilesSelectedEventArgs e = new (this);

        // TODO: Refactor to use CWP
        FilesSelected?.Invoke (this, e);

        if (e.Cancel)
        {
            return false;
        }

        // if user uses Path selection mode (e.g. Enter in text box)
        // then also copy to MultiSelected
        if (AllowsMultipleSelection && !MultiSelected.Any ())
        {
            MultiSelected = string.IsNullOrWhiteSpace (Path) ? Enumerable.Empty<string> ().ToList ().AsReadOnly () : new List<string> { Path }.AsReadOnly ();
        }

        // Set the typed result to the selected paths
        if (AllowsMultipleSelection)
        {
            Result = MultiSelected;
        }
        else
        {
            Result = string.IsNullOrWhiteSpace (Path) ? new List<string> ().AsReadOnly () : new List<string> { Path }.AsReadOnly ();
        }

        if (!IsModal)
        {
            return false;
        }

        App?.RequestStop ();

        return true;
    }

    private IFileSystemInfo? []? GetFocusedFiles ()
    {
        if (!_tableView.HasFocus || !_tableView.CanFocus)
        {
            return null;
        }

        _tableView.EnsureValidSelection ();

        if (_tableView.Value is null)
        {
            return null;
        }

        return _tableView.GetAllSelectedCells ()
                         .Select (c => c.Y)
                         .Distinct ()
                         .Select (RowToStats)
                         .Where (s => !s.IsParent)
                         .Select (d => d.FileSystemInfo)
                         .ToArray ();
    }

    private bool IsCompatibleWithAllowedExtensions (string path) =>

        // no restrictions
        AllowedTypes.Count == 0 || AllowedTypes.Any (t => t.IsAllowed (path));

    private bool IsCompatibleWithOpenMode (string s, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace (s))
        {
            return false;
        }

        if (!IsCompatibleWithAllowedExtensions (s))
        {
            reason = Style.WrongFileTypeFeedback;

            return false;
        }

        switch (OpenMode)
        {
            case OpenMode.Directory:
                if (MustExist && !Directory.Exists (s))
                {
                    reason = Style.DirectoryMustExistFeedback;

                    return false;
                }

                if (!File.Exists (s))
                {
                    return true;
                }
                reason = Style.FileAlreadyExistsFeedback;

                return false;

            case OpenMode.File:

                if (MustExist && !File.Exists (s))
                {
                    reason = Style.FileMustExistFeedback;

                    return false;
                }

                if (!Directory.Exists (s))
                {
                    return true;
                }
                reason = Style.DirectoryAlreadyExistsFeedback;

                return false;

            case OpenMode.Mixed:
                if (!MustExist || File.Exists (s) || Directory.Exists (s))
                {
                    return true;
                }
                reason = Style.FileOrDirectoryMustExistFeedback;

                return false;

            default: throw new InvalidEnumArgumentException (nameof (OpenMode), (int)OpenMode, typeof (OpenMode));
        }
    }

    /// <summary>Returns true if any <see cref="AllowedTypes"/> matches <paramref name="file"/>.</summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private bool MatchesAllowedTypes (IFileInfo file) => AllowedTypes.Any (t => t.IsAllowed (file.FullName));

    /// <summary>
    ///     If <see cref="TableView.MultiSelect"/> is this returns a union of all <see cref="FileSystemInfoStats"/> in the
    ///     selection.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<FileSystemInfoStats> MultiRowToStats ()
    {
        HashSet<FileSystemInfoStats> toReturn = [];

        if (!AllowsMultipleSelection || _tableView.MultiSelectedRegions.Count == 0)
        {
            return toReturn;
        }

        foreach (Point p in _tableView.GetAllSelectedCells ())
        {
            FileSystemInfoStats add = State?.Children [p.Y]!;

            toReturn.Add (add);
        }

        return toReturn;
    }

    private void New ()
    {
        if (_fileSystem is null)
        {
            return;
        }
        IFileSystemInfo? created = FileOperationsHandler.New (App, _fileSystem, State!.Directory);

        RefreshState ();
        RestoreSelection (created);
    }

    private void Rename (IApplication? app)
    {
        IFileSystemInfo? []? toRename = GetFocusedFiles ();

        if (toRename?.Length != 1)
        {
            return;
        }

        IFileSystemInfo? newNamed = FileOperationsHandler.Rename (app, _fileSystem!, toRename.Single ()!);

        RefreshState ();
        RestoreSelection (newNamed);
    }

    private bool TryAcceptMulti ()
    {
        IEnumerable<FileSystemInfoStats> multi = MultiRowToStats ();
        string? reason = null;

        IEnumerable<FileSystemInfoStats> fileSystemInfoStatsEnumerable = multi as FileSystemInfoStats [] ?? multi.ToArray ();

        if (!fileSystemInfoStatsEnumerable.Any ())
        {
            return false;
        }

        if (!fileSystemInfoStatsEnumerable.All (m => m.FileSystemInfo is { } && IsCompatibleWithOpenMode (m.FileSystemInfo.FullName, out reason)))
        {
            if (reason is null)
            {
                return false;
            }
            _feedback = reason;
            SetNeedsDraw ();

            return false;
        }

        Accept (fileSystemInfoStatsEnumerable);

        return true;
    }
}
