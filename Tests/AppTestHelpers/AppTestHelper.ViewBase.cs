#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AppTestHelpers;

public partial class AppTestHelper
{
    /// <summary>
    ///     Adds the given <paramref name="v"/> to the current top level view
    ///     and performs layout.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public AppTestHelper Add (View v)
    {
        WaitIteration ((app) =>
                       {
                           View top = App?.TopRunnableView ?? throw new ("Top was null so could not add view");
                           top.Add (v);
                           // BUGBUG: This Layout call is a hack to work around some bug in Layout.
                           // BUGBUG: See https://github.com/tui-cs/Terminal.Gui/issues/4522
                           top.Layout ();
                           _lastView = v;
                       });

        return this;
    }

    private View? _lastView;

    /// <summary>
    ///     The last view added (e.g. with <see cref="Add"/>) or the root/current top.
    /// </summary>
    public View LastView => _lastView ?? App?.TopRunnableView ?? throw new ("Could not determine which view to add to");

    private T Find<T> (Func<T, bool> evaluator) where T : View
    {
        View? t = App?.TopRunnableView;

        if (t == null)
        {
            Fail ("App.TopRunnable was null when attempting to find view");
        }

        T? f = FindRecursive (t!, evaluator);

        if (f == null)
        {
            Fail ("Failed to tab to a view which matched the Type and evaluator constraints in any SubViews of top");
        }

        return f!;
    }

    private T? FindRecursive<T> (View current, Func<T, bool> evaluator) where T : View
    {
        foreach (View subview in current.GetSubViews(includePadding:true))
        {
            if (subview is T match && evaluator (match))
            {
                return match;
            }

            // Recursive call
            T? result = FindRecursive (subview, evaluator);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
