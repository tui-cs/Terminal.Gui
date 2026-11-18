namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>Find the next text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was forward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If is replacing.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindNextText (string textToFind,
                              out bool gaveFullTurn,
                              bool matchCase = false,
                              bool matchWholeWord = false,
                              string? textToReplace = null,
                              bool replace = false)
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos = _model.FindNextText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Find the previous text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was backward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If the text was found.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindPreviousText (string textToFind,
                                  out bool gaveFullTurn,
                                  bool matchCase = false,
                                  bool matchWholeWord = false,
                                  string? textToReplace = null,
                                  bool replace = false)
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos = _model.FindPreviousText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Reset the flag to stop continuous find.</summary>
    public void FindTextChanged () => _continuousFind = false;
}
