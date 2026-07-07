using System.IO.Abstractions;

namespace Terminal.Gui.Views;

internal class FileDialogState
{
    private static readonly EnumerationOptions _ignoreInaccessibleEnumerationOptions = new () { IgnoreInaccessible = true };

    public FileDialogState (IDirectoryInfo dir, FileDialog parent)
    {
        Parent = parent;
        Directory = dir;
        Path = parent.Path;
        Children = GetChildren (Directory).ToArray ();
    }

    /// <summary>
    ///     Constructor for subclasses that manage their own Children population (e.g. <see cref="FileDialog.SearchState"/>).
    /// </summary>
    protected FileDialogState (IDirectoryInfo dir, FileDialog parent, bool skipInitialEnumeration)
    {
        Parent = parent;
        Directory = dir;
        Path = parent.Path;
        Children = skipInitialEnumeration ? [] : GetChildren (Directory).ToArray ();
    }

    protected FileDialog Parent { get; }

    public FileSystemInfoStats [] Children { get; internal set; }
    public IDirectoryInfo Directory { get; }

    /// <summary>Gets what was entered in the path text box of the dialog when the state was active.</summary>
    public string Path { get; }

    public FileSystemInfoStats? Selected { get; set; }

    protected IEnumerable<FileSystemInfoStats> GetChildren (IDirectoryInfo dir)
    {
        List<FileSystemInfoStats> children = [];

        AddReadableChildren (children, dir);

        // if only allowing specific file types
        if (Parent.AllowedTypes.Count > 0 && Parent.OpenMode == OpenMode.File)
        {
            children = children.Where (c => c.IsDir || (c.FileSystemInfo is IFileInfo f && Parent.IsCompatibleWithAllowedExtensions (f))).ToList ();
        }

        // if there's a UI filter in place too
        if (Parent.CurrentFilter is { })
        {
            children = children.Where (MatchesApiFilter).ToList ();
        }

        AddParentNavigation (children, dir);

        return children;
    }

    private void AddReadableChildren (List<FileSystemInfoStats> children, IDirectoryInfo dir)
    {
        try
        {
            foreach (IFileSystemInfo entry in EnumerateReadableEntries (dir))
            {
                AddReadableChild (children, entry);
            }
        }
        catch (Exception)
        {
            // Access permission exceptions, missing directories, etc.
        }
    }

    private IEnumerable<IFileSystemInfo> EnumerateReadableEntries (IDirectoryInfo dir)
    {
        // if directories only
        if (Parent.OpenMode == OpenMode.Directory)
        {
            return dir.EnumerateDirectories ("*", _ignoreInaccessibleEnumerationOptions);
        }

        return dir.EnumerateFileSystemInfos ("*", _ignoreInaccessibleEnumerationOptions);
    }

    private void AddReadableChild (List<FileSystemInfoStats> children, IFileSystemInfo entry)
    {
        try
        {
            children.Add (new FileSystemInfoStats (entry, Parent.Style.Culture));
        }
        catch (Exception)
        {
            // A single unreadable entry should not hide the rest of the directory.
        }
    }

    private void AddParentNavigation (List<FileSystemInfoStats> children, IDirectoryInfo dir)
    {
        if (dir.Parent is not { } parent)
        {
            return;
        }

        try
        {
            children.Add (new FileSystemInfoStats (parent, Parent.Style.Culture) { IsParent = true });
        }
        catch (Exception)
        {
            // If even the parent cannot be stat'ed/read metadata, keep the readable children.
        }
    }

    protected bool MatchesApiFilter (FileSystemInfoStats arg) =>
        Parent.CurrentFilter is { } && (arg.IsDir || (arg.FileSystemInfo is IFileInfo f && Parent.CurrentFilter.IsAllowed (f.FullName)));

    internal virtual void RefreshChildren () => Children = GetChildren (Directory).ToArray ();
}
