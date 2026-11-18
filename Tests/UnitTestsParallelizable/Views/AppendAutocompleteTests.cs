// Copilot
#nullable enable
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Parallelizable tests for <see cref="AppendAutocomplete"/> key handling and suggestion lifecycle.
/// </summary>
public class AppendAutocompleteTests : TestDriverBase
{
    [Fact]
    public void ProcessKey_Esc_ClearsSuggestionsAndReturnsTrue ()
    {
        // Arrange: text field with "f" and suggestions ["fish"]
        TextField tf = new () { Text = "f" };
        AppendAutocomplete ac = new (tf);
        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions = ["fish"];

        AutocompleteContext context = new ([new Cell (Grapheme: "f")], cursorPosition: 1);
        ac.GenerateSuggestions (context);

        Assert.NotEmpty (ac.Suggestions);

        // Act
        bool result = ac.ProcessKey (Key.Esc);

        // Assert
        Assert.True (result);
        Assert.Empty (ac.Suggestions);
    }

    [Fact]
    public void GenerateSuggestions_SuppressedByEsc_ResumesAfterLetterKey ()
    {
        // Arrange: suggestions populated
        TextField tf = new () { Text = "f" };
        AppendAutocomplete ac = new (tf);
        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions = ["fish"];

        AutocompleteContext context = new ([new Cell (Grapheme: "f")], cursorPosition: 1);
        ac.GenerateSuggestions (context);
        Assert.NotEmpty (ac.Suggestions);

        // Esc clears and suspends
        ac.ProcessKey (Key.Esc);
        Assert.Empty (ac.Suggestions);

        // GenerateSuggestions should be suppressed (once)
        ac.GenerateSuggestions (context);
        Assert.Empty (ac.Suggestions);

        // A letter key re-enables generation
        ac.ProcessKey (Key.F);

        ac.GenerateSuggestions (context);
        Assert.NotEmpty (ac.Suggestions);
    }

    [Fact]
    public void ProcessKey_CursorUp_CyclesSuggestions ()
    {
        // Arrange: two suggestions so cycling is meaningful
        TextField tf = new () { Text = "f" };
        AppendAutocomplete ac = new (tf);
        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions = ["fish", "friend"];

        AutocompleteContext context = new ([new Cell (Grapheme: "f")], cursorPosition: 1);
        ac.GenerateSuggestions (context);

        Assert.Equal (2, ac.Suggestions.Count);
        int initialIdx = ac.SelectedIdx;

        // Act: up cycles to next suggestion
        bool result = ac.ProcessKey (Key.CursorUp);

        // Assert
        Assert.True (result);
        Assert.NotEqual (initialIdx, ac.SelectedIdx);

        // Cycling again wraps back
        ac.ProcessKey (Key.CursorUp);
        Assert.Equal (initialIdx, ac.SelectedIdx);
    }

    [Fact]
    public void ProcessKey_End_ReturnsFalse_WhenSuggestionExists ()
    {
        // Copilot - End should not be consumed by AppendAutocomplete.
        // This lets TextField's normal End command move the cursor.
        // Arrange: focused text field with "f" at end and suggestion "fish"
        TextField tf = new () { Text = "f" };
        tf.SetFocus ();
        tf.MoveEnd ();

        AppendAutocomplete ac = new (tf);
        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions = ["fish"];

        AutocompleteContext context = new ([new Cell (Grapheme: "f")], cursorPosition: 1);
        ac.GenerateSuggestions (context);

        Assert.NotEmpty (ac.Suggestions);

        // Act
        bool result = ac.ProcessKey (Key.End);

        // Assert: End was not consumed by autocomplete
        Assert.False (result);
        Assert.Equal ("f", tf.Text);
        Assert.NotEmpty (ac.Suggestions);
    }

    [Fact]
    public void ProcessKey_End_ReturnsFalse_WhenNoSuggestions ()
    {
        // Copilot - End key should NOT consume the event when no suggestion is showing,
        // allowing the normal MoveEnd command to run.
        // Arrange: text field without suggestions
        TextField tf = new () { Text = "f" };
        AppendAutocomplete ac = new (tf);

        // Act
        bool result = ac.ProcessKey (Key.End);

        // Assert: End was not consumed
        Assert.False (result);
    }

    [Fact]
    public void AcceptSelectionIfAny_AcceptsSuggestionWhenFocused ()
    {
        // Arrange: focused text field with "f" at end and suggestion "fish"
        TextField tf = new () { Text = "f" };
        tf.SetFocus ();
        tf.MoveEnd ();

        AppendAutocomplete ac = new (tf);
        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions = ["fish"];

        AutocompleteContext context = new ([new Cell (Grapheme: "f")], cursorPosition: 1);
        ac.GenerateSuggestions (context);

        Assert.NotEmpty (ac.Suggestions);

        // Act
        bool accepted = ac.AcceptSelectionIfAny ();

        // Assert: suggestion accepted, text replaced
        Assert.True (accepted);
        Assert.Equal ("fish", tf.Text);
        Assert.Empty (ac.Suggestions);
    }
}
