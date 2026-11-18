namespace Terminal.Gui.Views;

/// <inheritdoc/>
internal abstract class CollectionNavigatorBase : ICollectionNavigator
{
    private readonly object _lock = new ();
    private DateTime _lastKeystroke = DateTime.Now;
    private string _searchString = "";

    /// <inheritdoc/>
    public ICollectionNavigatorMatcher Matcher { get; set; } = new DefaultCollectionNavigatorMatcher ();

    /// <inheritdoc/>
    public string SearchString
    {
        get
        {
            lock (_lock)
            {
                return _searchString;
            }
        }
        private set
        {
            lock (_lock)
            {
                _searchString = value;
            }

            OnSearchStringChanged (new (value));
        }
    }

    /// <inheritdoc/>
    public int TypingDelay { get; set; } = 500;

    /// <inheritdoc/>
    public int? GetNextMatchingItem (int? currentIndex, char keyStruck)
    {
        if (currentIndex.HasValue && currentIndex < 0)
        {
            throw new ArgumentOutOfRangeException (nameof (currentIndex), @"Must be non-negative");
        }

        if (!char.IsControl (keyStruck))
        {
            // maybe user pressed 'd' and now presses 'd' again.
            // a candidate search is things that begin with "dd"
            // but if we find none then we must fallback on cycling
            // d instead and discard the candidate state
            var candidateState = "";
            TimeSpan elapsedTime;
            string currentSearchString;

            lock (_lock)
            {
                elapsedTime = DateTime.Now - _lastKeystroke;
                currentSearchString = _searchString;
            }

            // is it a second or third (etc) keystroke within a short time
            if (currentSearchString.Length > 0 && elapsedTime < TimeSpan.FromMilliseconds (TypingDelay))
            {
                // "dd" is a candidate
                candidateState = currentSearchString + keyStruck;
            }
            else
            {
                // its a fresh keystroke after some time
                // or its first ever key press
                SearchString = new (keyStruck, 1);
            }

            int? idxCandidate = GetNextMatchingItem (
                                                     currentIndex,
                                                     candidateState,

                                                     // prefer not to move if there are multiple characters e.g. "ca" + 'r' should stay on "car" and not jump to "cart"
                                                     candidateState.Length > 1
                                                    );

            if (idxCandidate is { })
            {
                // found "dd" so candidate search string is accepted
                lock (_lock)
                {
                    _lastKeystroke = DateTime.Now;
                }

                SearchString = candidateState;

                return idxCandidate;
            }

            // nothing matches "dd" so discard it as a candidate
            // and just cycle "d" instead
            lock (_lock)
            {
                _lastKeystroke = DateTime.Now;
            }

            idxCandidate = GetNextMatchingItem (currentIndex, candidateState);

            // if a match wasn't found, the user typed a 'wrong' key in their search ("can" + 'z'
            // instead of "can" + 'd').
            if (SearchString.Length > 1 && idxCandidate is null)
            {
                // ignore it since we're still within the typing delay
                // don't add it to SearchString either
                return currentIndex;
            }

            // if no changes to current state manifested
            if (idxCandidate == currentIndex || idxCandidate is null)
            {
                // clear history and treat as a fresh letter
                ClearSearchString ();

                // match on the fresh letter alone
                SearchString = new (keyStruck, 1);
                idxCandidate = GetNextMatchingItem (currentIndex, SearchString);

                return idxCandidate ?? currentIndex;
            }

            // Found another "d" or just leave index as it was
            return idxCandidate;
        }

        // clear state because keypress was a control char
        ClearSearchString ();

        // control char indicates no selection
        return null;
    }

    /// <summary>This event is raised when <see cref="SearchString"/> is changed. Useful for debugging.</summary>
    public event EventHandler<KeystrokeNavigatorEventArgs>? SearchStringChanged;

    /// <summary>Returns the collection being navigated element at <paramref name="idx"/>.</summary>
    /// <returns></returns>
    protected abstract object ElementAt (int idx);

    /// <summary>Return the number of elements in the collection</summary>
    protected abstract int GetCollectionLength ();

    /// <summary>
    ///     Raised when the <see cref="SearchString"/> is changed. Useful for debugging. Raises the
    ///     <see cref="SearchStringChanged"/> event.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnSearchStringChanged (KeystrokeNavigatorEventArgs e) { SearchStringChanged?.Invoke (this, e); }

    /// <summary>Gets the index of the next item in the collection that matches <paramref name="search"/>.</summary>
    /// <param name="currentIndex">The index in the collection to start the search from.</param>
    /// <param name="search">The search string to use.</param>
    /// <param name="minimizeMovement">
    ///     Set to <see langword="true"/> to stop the search on the first match if there are
    ///     multiple matches for <paramref name="search"/>. e.g. "ca" + 'r' should stay on "car" and not jump to "cart". If
    ///     <see langword="false"/> (the default), the next matching item will be returned, even if it is above in the
    ///     collection.
    /// </param>
    /// <returns>The index of the next matching item or <see langword="null"/> if no match was found.</returns>
    internal int? GetNextMatchingItem (int? currentIndex, string search, bool minimizeMovement = false)
    {
        if (string.IsNullOrEmpty (search))
        {
            return null;
        }

        int collectionLength = GetCollectionLength ();

        if (currentIndex.HasValue && currentIndex < collectionLength && Matcher.IsMatch (search, ElementAt (currentIndex.Value)))
        {
            // we are already at a match
            if (minimizeMovement)
            {
                // if we would rather not jump around (e.g. user is typing lots of text to get this match)
                return currentIndex;
            }

            for (var i = 1; i < collectionLength; i++)
            {
                //circular
                int? idxCandidate = (i + currentIndex) % collectionLength;

                if (Matcher.IsMatch (search, ElementAt (idxCandidate!.Value)))
                {
                    return idxCandidate;
                }
            }

            // nothing else starts with the search term
            return currentIndex;
        }

        // search terms no longer match the current selection or there is none
        for (var i = 0; i < collectionLength; i++)
        {
            if (Matcher.IsMatch (search, ElementAt (i)))
            {
                return i;
            }
        }

        // Nothing matches
        return null;
    }

    private void ClearSearchString ()
    {
        SearchString = "";

        lock (_lock)
        {
            _lastKeystroke = DateTime.Now;
        }
    }
}
