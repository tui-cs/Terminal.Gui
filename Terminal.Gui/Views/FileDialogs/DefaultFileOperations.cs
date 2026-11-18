using System.IO.Abstractions;

namespace Terminal.Gui.Views;

/// <summary>Default file operation handlers using modal dialogs.</summary>
public class DefaultFileOperations : IFileOperations
{
    /// <summary>
    ///     Determines whether a candidate path is safely contained within the specified root directory.
    ///     Returns <see langword="false"/> if the name contains path-traversal sequences that escape the root.
    /// </summary>
    internal static bool IsContainedIn (string root, string candidate)
    {
        string rootFull = Path.GetFullPath (root)
                              .TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          + Path.DirectorySeparatorChar;

        string candidateFull = Path.GetFullPath (candidate);

        return candidateFull.StartsWith (rootFull, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the name contains characters that are not valid in a file or directory name,
    ///     including path separators, null characters, and control characters.
    /// </summary>
    internal static bool ContainsInvalidNameCharacters (string name)
    {
        if (string.IsNullOrWhiteSpace (name))
        {
            return true;
        }

        char [] invalidChars = Path.GetInvalidFileNameChars ();

        return name.IndexOfAny (invalidChars) >= 0;
    }
    /// <inheritdoc/>
    public bool Delete (IApplication? app, IEnumerable<IFileSystemInfo> toDelete)
    {
        // Default implementation does not allow deleting multiple files
        IEnumerable<IFileSystemInfo> fileSystemInfos = toDelete as IFileSystemInfo [] ?? toDelete.ToArray ();

        if (fileSystemInfos.Count () != 1)
        {
            return false;
        }

        IFileSystemInfo d = fileSystemInfos.Single ();
        string adjective = d.Name;

        int? result = MessageBox.Query (app ?? throw new ArgumentNullException (nameof (app)),
                                        string.Format (Strings.fdDeleteTitle, adjective),
                                        string.Format (Strings.fdDeleteBody, adjective),
                                        Strings.btnYes,
                                        Strings.btnNo);

        try
        {
            if (result == 0)
            {
                if (d is IFileInfo)
                {
                    d.Delete ();
                }
                else
                {
                    ((IDirectoryInfo)d).Delete (true);
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (app, Strings.fdDeleteFailedTitle, ex.Message, Strings.btnOk);
        }

        return false;
    }

    /// <inheritdoc/>
    public IFileSystemInfo? Rename (IApplication? app, IFileSystem fileSystem, IFileSystemInfo toRename)
    {
        // Don't allow renaming C: or D: or / (on linux) etc
        if (toRename is IDirectoryInfo { Parent: null })
        {
            return null;
        }

        if (!Prompt (app ?? throw new ArgumentNullException (nameof (app)), Strings.fdRenameTitle, toRename.Name, out string newName))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace (newName))
        {
            return null;
        }

        try
        {
            if (toRename is IFileInfo f)
            {
                if (ContainsInvalidNameCharacters (newName))
                {
                    MessageBox.ErrorQuery (app, Strings.fdRenameFailedTitle, Strings.fdPathTraversalError, Strings.btnOk);

                    return null;
                }

                string parentDir = f.Directory?.FullName ?? throw new InvalidOperationException ();
                string combined = Path.Combine (parentDir, newName);

                if (!IsContainedIn (parentDir, combined))
                {
                    MessageBox.ErrorQuery (app, Strings.fdRenameFailedTitle, Strings.fdPathTraversalError, Strings.btnOk);

                    return null;
                }

                IFileInfo newLocation = fileSystem.FileInfo.New (combined);
                f.MoveTo (newLocation.FullName);

                return newLocation;
            }
            else
            {
                var d = (IDirectoryInfo)toRename;

                if (ContainsInvalidNameCharacters (newName))
                {
                    MessageBox.ErrorQuery (app, Strings.fdRenameFailedTitle, Strings.fdPathTraversalError, Strings.btnOk);

                    return null;
                }

                string parentDir = d.Parent?.FullName ?? throw new InvalidOperationException ();
                string combined = Path.Combine (parentDir, newName);

                if (!IsContainedIn (parentDir, combined))
                {
                    MessageBox.ErrorQuery (app, Strings.fdRenameFailedTitle, Strings.fdPathTraversalError, Strings.btnOk);

                    return null;
                }

                IDirectoryInfo newLocation = fileSystem.DirectoryInfo.New (combined);
                d.MoveTo (newLocation.FullName);

                return newLocation;
            }
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (app, Strings.fdRenameFailedTitle, ex.Message, Strings.btnOk);
        }

        return null;
    }

    /// <inheritdoc/>
    public IFileSystemInfo? New (IApplication? app, IFileSystem fileSystem, IDirectoryInfo inDirectory)
    {
        if (app is null)
        {
            ArgumentNullException.ThrowIfNull (app);
        }
        var tv = new TextField { Width = Dim.Fill (0, 50), Height = 1 };
        string? result = app.TopRunnable?.Prompt<TextField, string> (tv, beginInitHandler: prompt => { prompt.Title = Strings.fdNewTitle; });

        if (string.IsNullOrWhiteSpace (result))
        {
            return null;
        }

        try
        {
            if (ContainsInvalidNameCharacters (result))
            {
                MessageBox.ErrorQuery (app, Strings.fdNewFailed, Strings.fdPathTraversalError, Strings.btnOk);

                return null;
            }

            string combined = Path.Combine (inDirectory.FullName, result);

            if (!IsContainedIn (inDirectory.FullName, combined))
            {
                MessageBox.ErrorQuery (app, Strings.fdNewFailed, Strings.fdPathTraversalError, Strings.btnOk);

                return null;
            }

            IDirectoryInfo newDir = fileSystem.DirectoryInfo.New (combined);
            newDir.Create ();

            return newDir;
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (app, Strings.fdNewFailed, ex.Message, Strings.btnOk);
        }

        return null;
    }

    private bool Prompt (IApplication app, string title, string defaultText, out string result)
    {
        var confirm = false;
        var btnOk = new Button { IsDefault = true, Text = Strings.btnOk };

        btnOk.Accepting += (s, e) =>
                           {
                               confirm = true;
                               (s as View)?.App?.RequestStop ();

                               // When Accepting is handled, set e.Handled to true to prevent further processing.
                               e.Handled = true;
                           };
        var btnCancel = new Button { Text = Strings.btnCancel };

        btnCancel.Accepting += (s, e) =>
                               {
                                   confirm = false;
                                   (s as View)?.App?.RequestStop ();

                                   // When Accepting is handled, set e.Handled to true to prevent further processing.
                                   e.Handled = true;
                               };

        var lbl = new Label { Text = Strings.fdRenamePrompt };
        var tf = new TextField { X = Pos.Right (lbl) + 1, Width = Dim.Fill (0, 50), Height = 1, Text = defaultText };
        tf.SelectAll ();

        var dlg = new Dialog { Title = title };
        dlg.Add (lbl);
        dlg.Add (tf);

        // Add buttons last so tab order is friendly
        // and TextField gets focus
        dlg.AddButton (btnOk);
        dlg.AddButton (btnCancel);

        app.Run (dlg);
        dlg.Dispose ();

        result = tf.Text;

        return confirm;
    }
}
