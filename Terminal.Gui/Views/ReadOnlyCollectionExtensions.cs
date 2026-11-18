#nullable disable
namespace Terminal.Gui.Views;

/// <summary>
///     Extends <see cref="IReadOnlyCollection{T}"/> with methods to find the index of an element.
/// </summary>
public static class ReadOnlyCollectionExtensions
{
    /// <summary>
    ///     Returns the index of the first element in the collection that matches the specified predicate.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int IndexOf<T> (this IReadOnlyCollection<T> self, Func<T, bool> predicate)
    {
        var i = 0;

        foreach (T element in self)
        {
            if (predicate (element))
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    /// <summary>
    ///     Returns the index of the first element in the collection that matches the specified predicate.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="toFind"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int IndexOf<T> (this IReadOnlyCollection<T> self, T toFind)
    {
        var i = 0;

        foreach (T element in self)
        {
            if (Equals (element, toFind))
            {
                return i;
            }

            i++;
        }

        return -1;
    }
}
