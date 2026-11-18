using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

/// <summary>Defines whether a given file/directory matches a set of search terms.</summary>
public interface ISearchMatcher
{
    /// <summary>Called once for each new search. Defines the string the user has provided as search terms.</summary>
    void Initialize (string terms);

    /// <summary>Return true if <paramref name="f"/> is a match to the last provided search terms</summary>
    bool IsMatch (IFileSystemInfo f);
}
