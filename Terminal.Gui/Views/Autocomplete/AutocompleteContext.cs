
#nullable disable
namespace Terminal.Gui.Views;

/// <summary>
///     Describes the current state of a <see cref="View"/> which is proposing autocomplete. Suggestions are based on
///     this state.
/// </summary>
public class AutocompleteContext
{
    /// <summary>Creates a new instance of the <see cref="AutocompleteContext"/> class</summary>
    public AutocompleteContext (List<Cell> currentLine, int cursorPosition, bool canceled = false)
    {
        CurrentLine = currentLine;
        CursorPosition = cursorPosition;
        Canceled = canceled;
    }

    /// <summary>Gets or sets if the autocomplete was canceled from popup.</summary>
    public bool Canceled { get; set; }

    /// <summary>The text on the current line.</summary>
    public List<Cell> CurrentLine { get; set; }

    /// <summary>The position of the input cursor within the <see cref="CurrentLine"/>.</summary>
    public int CursorPosition { get; set; }
}
