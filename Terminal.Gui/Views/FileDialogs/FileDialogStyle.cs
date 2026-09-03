using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;

namespace Terminal.Gui.Views;

/// <summary>Stores style settings for <see cref="FileDialog"/>.</summary>
public class FileDialogStyle
{
    private readonly IFileSystem? _fileSystem;

    /// <summary>Creates a new instance of the <see cref="FileDialogStyle"/> class.</summary>
    public FileDialogStyle (IFileSystem? fileSystem)
    {
        _fileSystem = fileSystem;

        TreeRootGetter = DefaultTreeRootGetter;

        DateFormat = CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern;
    }

    /// <summary>Gets or sets the text on the 'Cancel' button.</summary>
    public string CancelButtonText { get; set; } = Strings.btnCancel;

    /// <summary>
    ///     Gets or sets the class that is responsible for determining which color to use to represent files and
    ///     directories when <see cref="UseColors"/> is <see langword="true"/>.
    /// </summary>
    public FileSystemColorProvider ColorProvider { get; set; } = new ();

    /// <summary>
    ///     Gets or sets the culture to use (e.g. for number formatting). Defaults to
    ///     <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentUICulture;

    /// <summary>
    ///     Gets or sets the format to use for date/times in the Modified column. Defaults to
    ///     <see cref="DateTimeFormatInfo.SortableDateTimePattern"/> of the <see cref="CultureInfo.CurrentCulture"/>
    /// </summary>
    public string DateFormat { get; set; }

    /// <summary>
    ///     Gets or sets the default value to use for <see cref="UseColors"/>. This can be populated from .tui config
    ///     files via <see cref="TuiConfigurationBuilder"/>
    /// </summary>
    public static bool DefaultUseColors
    {
        get => FileDialogStyleSettings.Defaults.DefaultUseColors;
        set => FileDialogStyleSettings.Defaults.DefaultUseColors = value;
    }

    /// <summary>
    ///     Gets or sets the default value to use for <see cref="UseUnicodeCharacters"/>. This can be populated from .tui
    ///     config files via <see cref="TuiConfigurationBuilder"/>
    /// </summary>
    public static bool DefaultUseUnicodeCharacters
    {
        get => FileDialogStyleSettings.Defaults.DefaultUseUnicodeCharacters;
        set => FileDialogStyleSettings.Defaults.DefaultUseUnicodeCharacters = value;
    }

    /// <summary>
    ///     Gets or sets error message when user <see cref="OpenMode"/> is <see cref="OpenMode.File"/> and user enters the
    ///     name of an existing directory (File system cannot have a folder with the same name as a file).
    /// </summary>
    public string DirectoryAlreadyExistsFeedback { get; set; } = Strings.fdDirectoryAlreadyExistsFeedback;

    /// <summary>
    ///     Gets or sets error message when user selects a directory that does not exist and <see cref="OpenMode"/> is
    ///     <see cref="OpenMode.Directory"/> and <see cref="FileDialog.MustExist"/> is <see langword="true"/>.
    /// </summary>
    public string DirectoryMustExistFeedback { get; set; } = Strings.fdDirectoryMustExistFeedback;

    /// <summary>
    ///     Gets or sets error message when user <see cref="OpenMode"/> is <see cref="OpenMode.Directory"/> and user
    ///     enters the name of an existing file (File system cannot have a folder with the same name as a file).
    /// </summary>
    public string FileAlreadyExistsFeedback { get; set; } = Strings.fdFileAlreadyExistsFeedback;

    /// <summary>
    ///     Gets or sets error message when user selects a file that does not exist and <see cref="OpenMode"/> is
    ///     <see cref="OpenMode.File"/> and <see cref="FileDialog.MustExist"/> is <see langword="true"/>.
    /// </summary>
    public string FileMustExistFeedback { get; set; } = Strings.fdFileMustExistFeedback;

    /// <summary>Gets or sets the header text displayed in the Filename column of the files table.</summary>
    public string FilenameColumnName { get; set; } = Strings.fdFilename;

    /// <summary>
    ///     Gets or sets error message when user selects a file/dir that does not exist and <see cref="OpenMode"/> is
    ///     <see cref="OpenMode.Mixed"/> and <see cref="FileDialog.MustExist"/> is <see langword="true"/>.
    /// </summary>
    public string FileOrDirectoryMustExistFeedback { get; set; } = Strings.fdFileOrDirectoryMustExistFeedback;

    /// <summary>Gets or sets the class responsible for determining which symbol to use to represent files and directories.</summary>
    public FileSystemIconProvider IconProvider { get; set; } = new ();

    /// <summary>Gets or sets the header text displayed in the Modified column of the files table.</summary>
    public string ModifiedColumnName { get; set; } = Strings.fdModified;

    /// <summary>Gets or sets the text on the 'Ok' button.  Typically, you may want to change this to "Open" or "Save" etc.</summary>
    public string OkButtonText { get; set; } = Strings.btnOk;

    /// <summary>Gets or sets the text displayed in the 'Path' text box when user has not supplied any input yet.</summary>
    public string PathCaption { get; set; } = Strings.fdPathCaption;

    /// <summary>Gets or sets the text displayed in the 'Search' text box when user has not supplied any input yet.</summary>
    public string SearchCaption { get; set; } = Strings.fdSearchCaption;

    /// <summary>Gets or sets the header text displayed in the Size column of the files table.</summary>
    public string SizeColumnName { get; set; } = Strings.fdSize;

    /// <summary>Gets the style settings for the table of files (in currently selected directory).</summary>
    public TableStyle? TableStyle { get; internal set; }

    /// <summary>
    ///     Gets or Sets the method for getting the root tree objects that are displayed in the collapse-able tree in the
    ///     <see cref="FileDialog"/>.  Defaults to all accessible <see cref="System.Environment.GetLogicalDrives"/> and unique
    ///     <see cref="Environment.SpecialFolder"/>.
    /// </summary>
    /// <remarks>Must be configured before showing the dialog.</remarks>
    public Func<Dictionary<IDirectoryInfo, string>> TreeRootGetter { get; set; }

    /// <summary>Gets the style settings for the collapse-able directory/places tree</summary>
    public TreeStyle? TreeStyle { get; internal set; }

    /// <summary>Gets or sets the header text displayed in the Type column of the files table.</summary>
    public string TypeColumnName { get; set; } = Strings.fdType;

    /// <summary>
    ///     Gets or Sets a value indicating whether different colors should be used for different file types/directories.
    ///     Defaults to false.
    /// </summary>
    public bool UseColors { get; set; } = DefaultUseColors;

    /// <summary>Gets or sets whether to use advanced unicode characters which might not be installed on all users computers.</summary>
    public bool UseUnicodeCharacters { get; set; } = DefaultUseUnicodeCharacters;

    /// <summary>
    ///     Gets or sets error message when user attempts to select a file type that is not one of
    ///     <see cref="FileDialog.AllowedTypes"/>
    /// </summary>
    public string WrongFileTypeFeedback { get; set; } = Strings.fdWrongFileTypeFeedback;

    /// <summary>
    ///     <para>
    ///         Gets or sets a flag that determines behaviour when opening (double click/enter) or selecting a
    ///         directory in a <see cref="FileDialog"/>.
    ///     </para>
    ///     <para>
    ///         If <see langword="false"/> (the default) then the <see cref="FileDialog.Path"/> is simply
    ///         updated to the new directory path.
    ///     </para>
    ///     <para>
    ///         If <see langword="true"/> then any typed or previously selected file
    ///         name is preserved (e.g. "c:/hello.csv" when opening "temp" becomes "c:/temp/hello.csv").
    ///     </para>
    /// </summary>
    public bool PreserveFilenameOnDirectoryChanges { get; set; }

    private Dictionary<IDirectoryInfo, string> DefaultTreeRootGetter ()
    {
        if (_fileSystem is null)
        {
            return [];
        }
        Dictionary<IDirectoryInfo, string> roots = new ();

        try
        {
            foreach (string d in _fileSystem.Directory.GetLogicalDrives ())
            {
                IDirectoryInfo dir = _fileSystem.DirectoryInfo.New (d);

                roots.TryAdd (dir, d);
            }
        }
        catch (Exception)
        {
            // Cannot get the system disks, that's fine
        }

        try
        {
            foreach (Environment.SpecialFolder special in Enum.GetValues<Environment.SpecialFolder> ())
            {
                try
                {
                    string path = Environment.GetFolderPath (special);

                    if (string.IsNullOrWhiteSpace (path))
                    {
                        continue;
                    }

                    IDirectoryInfo dir = _fileSystem.DirectoryInfo.New (path);

                    if (!roots.ContainsKey (dir) && !roots.ContainsValue (special.ToString ()) && dir.Exists)
                    {
                        roots.Add (dir, special.ToString ());
                    }
                }
                catch (Exception)
                {
                    // Special file exists but contents are unreadable (permissions?)
                    // skip it anyway
                }
            }
        }
        catch (Exception)
        {
            // Cannot get the special files for this OS, oh well
        }

        return roots;
    }
}
