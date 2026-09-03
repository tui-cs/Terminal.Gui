using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Views;

internal class AutocompleteFilepathContext (string currentLine, int cursorPosition, FileDialogState state)
    : AutocompleteContext (Cell.ToCellList (currentLine), cursorPosition)
{
    public FileDialogState State { get; set; } = state;
}

internal class FilepathSuggestionGenerator : ISuggestionGenerator
{
    private FileDialogState? _state;

    public IEnumerable<Suggestion> GenerateSuggestions (AutocompleteContext context)
    {
        if (context is AutocompleteFilepathContext fileState)
        {
            _state = fileState.State;
        }

        var path = Cell.ToString (context.CurrentLine);
        int last = path.LastIndexOfAny (FileDialog.Separators);

        if (string.IsNullOrWhiteSpace (path) || !Path.IsPathRooted (path) || _state is null)
        {
            return [];
        }

        string term = path [(last + 1)..];

        // If path is /tmp/ then don't just list everything in it
        if (string.IsNullOrWhiteSpace (term))
        {
            return [];
        }

        if (term.Equals (_state?.Directory.Name))
        {
            // Clear suggestions
            return [];
        }

        bool isWindows = OperatingSystem.IsWindows ();

        string? [] suggestions = _state?.Children.Where (d => !d.IsParent)
                                       .Select (e => e.FileSystemInfo is IDirectoryInfo d ? d.Name + Path.DirectorySeparatorChar : e.FileSystemInfo?.Name)
                                       .ToArray ()
                                 ?? [];

        string [] validSuggestions = suggestions
                                      .Where (s => s?.StartsWith (term,
                                                                  isWindows ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture)
                                                   == true)
                                      .OfType<string> ()
                                      .OrderBy (m => m.Length)
                                      .ToArray ();

        // nothing to suggest
        if (validSuggestions.Length == 0 || validSuggestions [0].Length == term.Length)
        {
            return [];
        }

        return validSuggestions.Select (f => new Suggestion (term.Length, f, f)).ToList ();
    }

    public bool IsWordChar (string text) => text != "\n";
}
