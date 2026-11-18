#nullable disable
namespace Terminal.Gui.Views;

/// <summary>Generates autocomplete <see cref="Suggestion"/> based on a given cursor location within a string</summary>
public interface ISuggestionGenerator
{
    /// <summary>Generates autocomplete <see cref="Suggestion"/> based on a given <paramref name="context"/></summary>
    IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context);

    /// <summary>
    ///     Returns <see langword="true"/> if <paramref name="text"/> is a character that would continue autocomplete
    ///     suggesting. Returns <see langword="false"/> if it is a 'breaking' character (i.e. terminating current word
    ///     boundary)
    /// </summary>
    bool IsWordChar (string text);
}
