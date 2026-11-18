// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

namespace Terminal.Gui.Views;

/// <summary>
///     Collection navigator matcher that uses the TreeView AspectGetter to determine what is
///     displayed for the given object values.
/// </summary>
/// <typeparam name="T"></typeparam>
internal class TreeViewCollectionNavigatorMatcher<T> : DefaultCollectionNavigatorMatcher where T : class
{
    private readonly TreeView<T> _treeView;

    /// <summary>
    ///     Creates a new instance of the matcher which tracks the current value of tree view AspectGetter.
    /// </summary>
    /// <param name="treeView"></param>
    public TreeViewCollectionNavigatorMatcher (TreeView<T> treeView) => _treeView = treeView;

    /// <summary>
    ///     Matches based on the search terms match to the rendered string value (based on tree view AspectGetter)
    /// </summary>
    /// <param name="search"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override bool IsMatch (string search, object? value)
    {
        if (value is T t)
        {
            return base.IsMatch (search, _treeView.AspectGetter (t));
        }

        return base.IsMatch (search, value);
    }
}
