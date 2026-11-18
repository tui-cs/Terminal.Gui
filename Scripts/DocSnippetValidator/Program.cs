// Compiles every fenced C# block in the given markdown files against the built
// Terminal.Gui assembly. Blocks demonstrating anti-patterns (containing "// WRONG",
// "❌", or "✗") and blocks preceded by `<!-- snippet: ignore -->` are skipped.
//
// Usage: dotnet run --project Scripts/DocSnippetValidator -- <Terminal.Gui.dll> <file.md>...

using DocSnippetValidator;

if (args.Length < 2)
{
    Console.Error.WriteLine ("Usage: DocSnippetValidator <path-to-Terminal.Gui.dll> <markdown-file>...");

    return 2;
}

string libPath = Path.GetFullPath (args [0]);

if (!File.Exists (libPath))
{
    Console.Error.WriteLine ($"Library not found: {libPath} — build Terminal.Gui first.");

    return 2;
}

SnippetCompiler compiler = new (libPath);
int failed = 0;
int compiled = 0;
int skipped = 0;

foreach (string mdPath in args.Skip (1))
{
    if (!File.Exists (mdPath))
    {
        Console.Error.WriteLine ($"File not found: {mdPath}");
        failed++;

        continue;
    }

    foreach (Snippet snippet in SnippetExtractor.Extract (mdPath))
    {
        if (snippet.Ignored)
        {
            skipped++;

            continue;
        }

        IReadOnlyList<string> errors = compiler.Compile (snippet);
        compiled++;

        if (errors.Count == 0)
        {
            continue;
        }

        failed++;
        Console.Error.WriteLine ($"{snippet.FilePath}({snippet.StartLine}): snippet does not compile:");

        foreach (string error in errors)
        {
            Console.Error.WriteLine ($"    {error}");
        }
    }
}

Console.WriteLine ($"Doc snippets: {compiled} compiled, {skipped} skipped, {failed} failed.");

return failed == 0 ? 0 : 1;
