using System.IO.Abstractions;

namespace Terminal.Gui.Views;

internal class FileDialogHistory (FileDialog dlg)
{
    private readonly Stack<FileDialogState> _back = new ();
    private readonly Stack<FileDialogState> _forward = new ();

    public bool Back ()
    {
        IDirectoryInfo? goTo = null;
        FileSystemInfoStats? restoreSelection = null;
        string? restorePath = null;

        if (CanBack ())
        {
            FileDialogState backTo = _back.Pop ();
            goTo = backTo.Directory;
            restoreSelection = backTo.Selected;
            restorePath = backTo.Path;
        }
        else if (CanUp ())
        {
            goTo = dlg.State?.Directory.Parent;
        }

        // nowhere to go
        if (goTo is null)
        {
            return false;
        }

        _forward.Push (dlg.State ?? throw new InvalidOperationException ());
        dlg.PushState (goTo, false, true, false, restorePath);

        if (restoreSelection is { })
        {
            dlg.RestoreSelection (restoreSelection.FileSystemInfo);
        }

        return true;
    }

    internal bool CanBack () => _back.Count > 0;
    internal bool CanForward () => _forward.Count > 0;
    internal bool CanUp () => dlg.State?.Directory.Parent != null;
    internal void ClearForward () => _forward.Clear ();

    internal bool Forward ()
    {
        if (_forward.Count <= 0)
        {
            return false;
        }
        dlg.PushState (_forward.Pop ().Directory, true, true, false);

        return true;
    }

    internal void Push (FileDialogState? state, bool clearForward)
    {
        if (state is null)
        {
            return;
        }

        // if changing to a new directory push onto the Back history
        if (_back.Count != 0 && _back.Peek ().Directory.FullName == state.Directory.FullName)
        {
            return;
        }
        _back.Push (state);

        if (clearForward)
        {
            ClearForward ();
        }
    }

    internal bool Up ()
    {
        IDirectoryInfo? parent = dlg.State?.Directory.Parent;

        if (parent is null)
        {
            return false;
        }
        _back.Push (new FileDialogState (parent, dlg));
        dlg.PushState (parent, false);

        return true;
    }
}
