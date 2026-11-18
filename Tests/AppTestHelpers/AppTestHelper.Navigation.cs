#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AppTestHelpers;

public partial class AppTestHelper
{
    /// <summary>
    ///     Sets the input focus to the given <see cref="View"/>.
    ///     Throws <see cref="ArgumentException"/> if focus did not change due to system
    ///     constraints e.g. <paramref name="toFocus"/>
    ///     <see cref="View.CanFocus"/> is <see langword="false"/>
    /// </summary>
    /// <param name="toFocus"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public AppTestHelper Focus (View toFocus)
    {
        toFocus.FocusDeepest (NavigationDirection.Forward, TabBehavior.TabStop);

        if (!toFocus.HasFocus)
        {
            throw new ArgumentException ("Failed to set focus, FocusDeepest did not result in HasFocus becoming true. Ensure view is added and focusable");
        }

        return WaitIteration ();
    }

    /// <summary>
    ///     Tabs through the UI until a View matching the <paramref name="evaluator"/>
    ///     is found (of Type T) or all views are looped through (back to the beginning)
    ///     in which case triggers hard stop and Exception
    /// </summary>
    /// <param name="evaluator">
    ///     Delegate that returns true if the passed View is the one
    ///     you are trying to focus. Leave <see langword="null"/> to focus the first view of type
    ///     <typeparamref name="T"/>
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public AppTestHelper Focus<T> (Func<T, bool>? evaluator = null) where T : View
    {
        evaluator ??= _ => true;
        View? t = App?.TopRunnableView;

        HashSet<View> seen = new ();

        if (t == null)
        {
            Fail ("Application.TopRunnable was null when trying to set focus");

            return this;
        }

        do
        {
            View? next = t.MostFocused;

            // Is view found?
            if (next is T v && evaluator (v))
            {
                return this;
            }

            // No, try tab to the next (or first)
            KeyDown (Application.GetDefaultKey (Command.NextTabStop));
            WaitIteration ();

            next = t.MostFocused;

            if (next is null)
            {
                Fail (
                      "Failed to tab to a view which matched the Type and evaluator constraints of the test because MostFocused became or was always null"
                      + DescribeSeenViews (seen));

                return this;
            }

            // Track the views we have seen
            // We have looped around to the start again if it was already there
            if (!seen.Add (next))
            {
                Fail (
                      "Failed to tab to a view which matched the Type and evaluator constraints of the test before looping back to the original View"
                      + DescribeSeenViews (seen));

                return this;
            }
        }
        while (true);
    }

    private string DescribeSeenViews (HashSet<View> seen) { return Environment.NewLine + string.Join (Environment.NewLine, seen); }
}
