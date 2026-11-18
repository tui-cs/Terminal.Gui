


namespace Terminal.Gui.Views;

/// <summary>
///     Navigates a collection of items using keystrokes. The keystrokes are used to build a search string. The
///     <see cref="SearchString"/> is used to find the next item in the collection that matches the search string when
///     <see cref="GetNextMatchingItem(int?, char)"/> is called.
///     <para>
///         If the user types keystrokes that can't be found in the collection, the search string is cleared and the next
///         item is found that starts with the last keystroke.
///     </para>
///     <para>If the user pauses keystrokes for a short time (see <see cref="TypingDelay"/>), the search string is cleared.</para>
/// </summary>
public interface ICollectionNavigator
{
    /// <summary>
    ///     Gets or sets the number of milliseconds to delay before clearing the search string. The delay is reset on each
    ///     call to <see cref="GetNextMatchingItem(int?, char)"/>. The default is 500ms.
    /// </summary>
    public int TypingDelay { get; set; }

    /// <summary>This event is invoked when <see cref="SearchString"/>  changes. Useful for debugging.</summary>
    public event EventHandler<KeystrokeNavigatorEventArgs>? SearchStringChanged;

    /// <summary>
    ///     Gets the current search string. This includes the set of keystrokes that have been pressed since the last
    ///     unsuccessful match or after <see cref="TypingDelay"/>) milliseconds. Useful for debugging.
    /// </summary>
    string SearchString { get; }

    /// <summary>
    ///     Class responsible for deciding whether given entries in the collection match
    ///     the search term the user is typing.
    /// </summary>
    ICollectionNavigatorMatcher Matcher { get; set; }

    /// <summary>
    ///     Gets the index of the next item in the collection that matches the current <see cref="SearchString"/> plus the
    ///     provided character (typically from a key press).
    /// </summary>
    /// <param name="currentIndex">The index in the collection to start the search from.</param>
    /// <param name="keyStruck">The character of the key the user pressed.</param>
    /// <returns>
    ///     The index of the item that matches what the user has typed. Returns <see langword="null"/> if no item in the
    ///     collection matched.
    /// </returns>
    int? GetNextMatchingItem (int? currentIndex, char keyStruck);
}
