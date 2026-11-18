namespace Terminal.Gui.Views;

/// <summary>Determine which <see cref="System.IO"/> type to open.</summary>
public enum OpenMode
{
    /// <summary>Opens only file or files.</summary>
    File,

    /// <summary>Opens only directory or directories.</summary>
    Directory,

    /// <summary>Opens files and directories.</summary>
    Mixed
}
