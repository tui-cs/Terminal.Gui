namespace Terminal.Gui.ViewBase;

/// <summary>
///     Extension methods for <see cref="WeakReference{T}"/> when T is <see cref="View"/>.
/// </summary>
public static class WeakReferenceExtensions
{
    /// <param name="weakRef">The weak reference to format.</param>
    extension(WeakReference<View>? weakRef)
    {
        /// <summary>
        ///     Returns a formatted string representation of the <see cref="WeakReference{T}"/> to a View.
        /// </summary>
        /// <returns>A string identifying the referenced View, or "(null)" if the reference is null or dead.</returns>
        public string ToIdentifyingString ()
        {
            if (weakRef is null || !weakRef.TryGetTarget (out View? view))
            {
                return $"{Glyphs.Null}";
            }

            return view.ToIdentifyingString ();
        }

        /// <summary>
        ///     Tries to get the source <see cref="View"/> from a <see cref="WeakReference{T}"/>.
        /// </summary>
        /// <param name="source">
        ///     When this method returns, contains the target View if the weak reference is not null and the target is still alive;
        ///     otherwise, null.
        /// </param>
        /// <returns>
        ///     <see langword="true"/> if the weak reference is not null and the target is still alive;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         This is a convenience method to simplify the common pattern of checking a weak reference
        ///         and retrieving its target. It's particularly useful with <see cref="ICommandContext.Source"/>.
        ///     </para>
        ///     <para>
        ///         Example usage:
        ///         <code>
        ///         if (commandContext.Source.TryGetSource(out View? view))
        ///         {
        ///             // use view
        ///         }
        ///         </code>
        ///     </para>
        /// </remarks>
        public bool TryGetSource (out View? source)
        {
            source = null;

            return weakRef?.TryGetTarget (out source) == true;
        }
    }
}
