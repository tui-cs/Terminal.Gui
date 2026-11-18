using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>Provides an interactive <see cref="Dialog"/> for selecting files or directories for opening</summary>
/// <remarks>
///     <para>
///         The open dialog can be used to select files for opening, it can be configured to allow multiple items to be
///         selected (based on the AllowsMultipleSelection) variable, and you can control whether this should allow files or
///         directories to be selected.
///     </para>
///     <para>
///         To use, create an instance of <see cref="OpenDialog"/>, and pass it to
///         <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>. This will run the dialog modally, and when this returns,
///         the list of files will be available on the <see cref="FilePaths"/> property.
///     </para>
///     <para>Use `Ctrl-click` or `Space` to select multiple files. `Alt-Click` extends the selection.</para>
/// </remarks>
public class OpenDialog : FileDialog
{
    /// <summary>Initializes a new <see cref="OpenDialog"/>.</summary>
    public OpenDialog () { }

    /// <summary>Returns the selected files, or an empty list if nothing has been selected</summary>
    /// <value>The file paths.</value>
    public IReadOnlyList<string> FilePaths =>
        Result is null ? Enumerable.Empty<string> ().ToList ().AsReadOnly () :
        AllowsMultipleSelection ? MultiSelected : new ReadOnlyCollection<string> ([Path]);

    /// <inheritdoc/>
    public override OpenMode OpenMode
    {
        get => base.OpenMode;
        set
        {
            base.OpenMode = value;

            Style.OkButtonText = value == OpenMode.File ? Strings.btnOpen :
                                 value == OpenMode.Directory ? Strings.fdSelectFolder : Strings.fdSelectMixed;
        }
    }
}
