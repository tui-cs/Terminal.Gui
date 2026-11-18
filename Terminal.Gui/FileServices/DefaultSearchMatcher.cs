using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

internal class DefaultSearchMatcher : ISearchMatcher
{
    private string []? _terms;
    public void Initialize (string terms) { _terms = terms.Split ([" "], StringSplitOptions.RemoveEmptyEntries); }

    public bool IsMatch (IFileSystemInfo f)
    {
        //Contains overload with StringComparison is not available in (net472) or (netstandard2.0)
        //return f.Name.Contains (terms, StringComparison.OrdinalIgnoreCase);

        return

            // At least one term must match the file name only e.g. "my" in "myfile.csv"
            _terms!.Any (t => f.Name.IndexOf (t, StringComparison.OrdinalIgnoreCase) >= 0)
            &&

            // All terms must exist in full path e.g. "dos my" can match "c:\documents\myfile.csv"
            _terms!.All (t => f.FullName.IndexOf (t, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
