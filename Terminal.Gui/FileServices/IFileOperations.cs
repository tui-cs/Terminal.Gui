using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

/// <summary>
///     Interface for defining how to handle file/directory deletion, rename and newing attempts in
///     <see cref="FileDialog"/>.
/// </summary>
public interface IFileOperations
{
    /// <summary>Specifies how to handle file/directory deletion attempts in <see cref="FileDialog"/>.</summary>
    /// <param name="app"></param>
    /// <param name="toDelete"></param>
    /// <returns><see langword="true"/> if operation was completed or <see langword="false"/> if cancelled</returns>
    /// <remarks>
    ///     Ensure you use a try/catch block with appropriate error handling (e.g. showing a <see cref="MessageBox"/>
    /// </remarks>
    bool Delete (IApplication? app, IEnumerable<IFileSystemInfo> toDelete);

    /// <summary>Specifies how to handle 'new directory' operation in <see cref="FileDialog"/>.</summary>
    /// <param name="app"></param>
    /// <param name="fileSystem"></param>
    /// <param name="inDirectory">The parent directory in which the new directory should be created</param>
    /// <returns>The newly created directory or null if cancelled.</returns>
    /// <remarks>
    ///     Ensure you use a try/catch block with appropriate error handling (e.g. showing a <see cref="MessageBox"/>
    /// </remarks>
    IFileSystemInfo? New (IApplication? app, IFileSystem fileSystem, IDirectoryInfo inDirectory);

    /// <summary>Specifies how to handle file/directory rename attempts in <see cref="FileDialog"/>.</summary>
    /// <param name="app"></param>
    /// <param name="fileSystem"></param>
    /// <param name="toRename"></param>
    /// <returns>The new name for the file or null if cancelled</returns>
    /// <remarks>
    ///     Ensure you use a try/catch block with appropriate error handling (e.g. showing a <see cref="MessageBox"/>
    /// </remarks>
    IFileSystemInfo? Rename (IApplication? app, IFileSystem fileSystem, IFileSystemInfo toRename);
}
