// 

using System.IO.Abstractions;

namespace Terminal.Gui.Views;

/// <summary>Provides an interactive <see cref="Dialog"/> for selecting files or directories for saving</summary>
/// <remarks>
///     <para>
///         To use, create an instance of <see cref="SaveDialog"/>, and pass it to
///         <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>. This will run the dialog modally, and when
///         this returns,
///         the <see cref="FileName"/>property will contain the selected file name or null if the user canceled.
///     </para>
/// </remarks>
public class SaveDialog : FileDialog
{
    /// <summary>Initializes a new <see cref="SaveDialog"/>.</summary>
    public SaveDialog () => Style.OkButtonText = Strings.btnSave;

    internal SaveDialog (IFileSystem fileSystem) : base (fileSystem) => Style.OkButtonText = Strings.btnSave;

    /// <summary>
    ///     Gets the name of the file the user selected for saving, or null if the user canceled the
    ///     <see cref="SaveDialog"/>.
    /// </summary>
    /// <value>The name of the file.</value>
    public string? FileName => Result is null ? null : Path;

    /// <summary>Gets the default title for the <see cref="SaveDialog"/>.</summary>
    /// <returns></returns>
    protected override string GetDefaultTitle ()
    {
        List<string> titleParts = [Strings.fdSave];

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
}
