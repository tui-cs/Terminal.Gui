namespace Terminal.Gui.Views;

/// <summary>Interface for <see cref="FileDialog"/> restrictions on which file type(s) the user is allowed to select/enter.</summary>
public interface IAllowedType
{
    /// <summary>
    ///     Returns true if the file at <paramref name="path"/> is compatible with this allow option.  Note that the file
    ///     may not exist (e.g. in the case of saving).
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool IsAllowed (string path);
}
