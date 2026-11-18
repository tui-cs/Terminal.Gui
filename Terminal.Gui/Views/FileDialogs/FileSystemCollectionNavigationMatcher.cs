using System.IO.Abstractions;

namespace Terminal.Gui.Views;

/// <summary>
/// Search matcher that uses the name only (not file path, icon etc)
/// </summary>
internal class FileSystemCollectionNavigationMatcher : DefaultCollectionNavigatorMatcher
{

    public override bool IsMatch (string search, object? value)
    {
        if (value is IFileSystemInfo fsi)
        {
            return fsi.Name.StartsWith (search, Comparer);
        }

        return base.IsMatch (search, value);
    }
}