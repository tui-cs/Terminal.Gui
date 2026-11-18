using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DocSnippetValidator;

/// <summary>
///     Compiles extracted snippets against the built Terminal.Gui assembly. Complete compilation
///     units compile as-is (with standard usings prepended); statement fragments are wrapped in a
///     harness class so references to <c>X</c>, <c>App</c>, <c>Result</c>, etc. resolve.
/// </summary>
public class SnippetCompiler
{
    private const string StandardUsings = """
                                          using System;
                                          using System.Collections.Generic;
                                          using System.Collections.ObjectModel;
                                          using System.Data;
                                          using System.IO;
                                          using System.Linq;
                                          using Terminal.Gui.App;
                                          using Terminal.Gui.Configuration;
                                          using Terminal.Gui.Drawing;
                                          using Terminal.Gui.Input;
                                          using Terminal.Gui.Text;
                                          using Terminal.Gui.ViewBase;
                                          using Terminal.Gui.Views;

                                          """;

    // Fields (not parameters) so snippet locals may shadow them without CS0136.
    // No blanket "#pragma warning disable" here: it would also suppress the
    // obsolete-API warnings (CS0618/CS0612) this validator elevates to errors to
    // catch v1 API rot. Harness-induced warnings (unused fields, nullability) are
    // harmless because CompileSource only fails on errors.
    private const string HarnessHeader = """
                                         class __SnippetHost : Runnable<string?>
                                         {
                                             IApplication app = null!;
                                             View view = null!;
                                             View otherView = null!;
                                             Button button = null!;
                                             Button loginButton = null!;
                                             TextField textField = null!;
                                             TextField usernameField = null!;
                                             ListView listView = null!;
                                             CheckBox checkbox = null!;
                                             Label label = null!;

                                         """;

    private static readonly CSharpParseOptions _parseOptions = CSharpParseOptions.Default.WithLanguageVersion (LanguageVersion.Preview);

    private readonly List<MetadataReference> _references = [];

    public SnippetCompiler (string libPath)
    {
        string tpa = (string)AppContext.GetData ("TRUSTED_PLATFORM_ASSEMBLIES")!;

        foreach (string path in tpa.Split (Path.PathSeparator))
        {
            _references.Add (MetadataReference.CreateFromFile (path));
        }

        _references.Add (MetadataReference.CreateFromFile (libPath));
    }

    /// <summary>Compiles <paramref name="snippet"/>; returns compile errors (empty when it compiles).</summary>
    public IReadOnlyList<string> Compile (Snippet snippet)
    {
        SyntaxTree probe = CSharpSyntaxTree.ParseText (snippet.Code, _parseOptions);
        CompilationUnitSyntax root = probe.GetCompilationUnitRoot ();

        bool hasTypes = root.Members.Any (m => m is not GlobalStatementSyntax);
        bool hasGlobals = root.Members.OfType<GlobalStatementSyntax> ().Any ();

        if (hasTypes || root.Usings.Count > 0)
        {
            string source = StandardUsings + snippet.Code;
            OutputKind kind = hasGlobals ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;

            return CompileSource (source, kind, preambleLines: CountLines (StandardUsings));
        }

        // Statement fragment: try as a method body first, then as class members.
        string methodWrap = StandardUsings + HarnessHeader + "    void __Demo ()\n    {\n" + snippet.Code + "\n    }\n}\n";
        int methodPreamble = CountLines (StandardUsings) + CountLines (HarnessHeader) + 2;
        IReadOnlyList<string> statementErrors = CompileSource (methodWrap, OutputKind.DynamicallyLinkedLibrary, methodPreamble);

        if (statementErrors.Count == 0)
        {
            return statementErrors;
        }

        string memberWrap = StandardUsings + HarnessHeader + snippet.Code + "\n}\n";
        int memberPreamble = CountLines (StandardUsings) + CountLines (HarnessHeader);
        IReadOnlyList<string> memberErrors = CompileSource (memberWrap, OutputKind.DynamicallyLinkedLibrary, memberPreamble);

        return memberErrors.Count == 0 ? memberErrors : statementErrors;
    }

    private IReadOnlyList<string> CompileSource (string source, OutputKind kind, int preambleLines)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText (source, _parseOptions);

        CSharpCompilationOptions options = new (
                                                kind,
                                                nullableContextOptions: NullableContextOptions.Enable,
                                                specificDiagnosticOptions:
                                                [
                                                    // Duplicate-using noise from prepending StandardUsings.
                                                    new ("CS0105", ReportDiagnostic.Suppress),

                                                    // Treat obsolete-API use as a failure so v1 rot (e.g. the
                                                    // legacy static Application.Init) cannot pass as "compiled".
                                                    new ("CS0612", ReportDiagnostic.Error),
                                                    new ("CS0618", ReportDiagnostic.Error)
                                                ]);

        CSharpCompilation compilation = CSharpCompilation.Create ("Snippet", [tree], _references, options);

        return
        [
            .. compilation.GetDiagnostics ()
                          .Where (d => d.Severity == DiagnosticSeverity.Error)
                          .Select (d => Format (d, preambleLines))
        ];
    }

    private static string Format (Diagnostic diagnostic, int preambleLines)
    {
        int line = diagnostic.Location.GetLineSpan ().StartLinePosition.Line + 1 - preambleLines;

        return $"(snippet line {line}) {diagnostic.Id}: {diagnostic.GetMessage ()}";
    }

    private static int CountLines (string text) => text.Count (c => c == '\n');
}
