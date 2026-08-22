using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

/// <summary>Determines which symbol to use to represent files and directories.</summary>
public class FileSystemIconProvider
{
    private readonly NerdFonts _nerd = new ();

    /// <summary>
    ///     Returns the character to use to represent <paramref name="fileSystemInfo"/> or an empty space if no icon
    ///     should be used.
    /// </summary>
    /// <param name="fileSystemInfo">The file or directory requiring an icon.</param>
    /// <returns></returns>
    public Rune GetIcon (IFileSystemInfo? fileSystemInfo)
    {
        if (UseNerdIcons)
        {
            return new Rune (_nerd.GetNerdIcon (fileSystemInfo, fileSystemInfo is IDirectoryInfo dir && IsOpenGetter (dir)));
        }

        if (fileSystemInfo is IDirectoryInfo)
        {
            return UseUnicodeCharacters ? Glyphs.Folder : new Rune (Path.DirectorySeparatorChar);
        }

        return UseUnicodeCharacters ? Glyphs.File : new Rune (' ');
    }

    /// <summary>
    ///     Returns <see cref="GetIcon(IFileSystemInfo)"/> with an extra space on the end if icon is likely to overlap
    ///     adjacent cells.
    /// </summary>
    public string GetIconWithOptionalSpace (IFileSystemInfo? fileSystemInfo)
    {
        string space = UseNerdIcons ? " " : "";

        return GetIcon (fileSystemInfo!) + space;
    }

    /// <summary>
    ///     Gets or sets the delegate to be used to determine opened state of directories when resolving
    ///     <see cref="GetIcon(IFileSystemInfo)"/>.  Defaults to always false.
    /// </summary>
    public Func<IDirectoryInfo, bool> IsOpenGetter { get; set; } = d => false;

    /// <summary>
    ///     <para>
    ///         Gets or sets a flag indicating whether to use Nerd Font icons. Defaults to <see cref="NerdFonts.Enable"/>
    ///         which can be configured by end users from their <c>./.tui/config.json</c> via
    ///         <see cref="TuiConfigurationBuilder"/>.
    ///     </para>
    ///     <remarks>Enabling <see cref="UseNerdIcons"/> implicitly disables <see cref="UseUnicodeCharacters"/>.</remarks>
    /// </summary>
    public bool UseNerdIcons
    {
        get;
        set
        {
            field = value;

            if (value)
            {
                UseUnicodeCharacters = false;
            }
        }
    } = NerdFonts.Enable;

    /// <summary>Gets or sets a flag indicating whether to use common unicode characters for file/directory icons.</summary>
    public bool UseUnicodeCharacters
    {
        get;
        set
        {
            field = value;

            if (value)
            {
                UseNerdIcons = false;
            }
        }
    }
}
