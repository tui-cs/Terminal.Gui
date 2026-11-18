#nullable enable

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PartialSplitter;

internal class Program
{
    private static async Task<int> Main (string [] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine ("Usage: PartialSplitter <file.cs>");

            return 1;
        }

        string filePath = args [0];

        if (!File.Exists (filePath))
        {
            Console.Error.WriteLine ($"File not found: {filePath}");

            return 1;
        }

        try
        {
            string originalCode = await File.ReadAllTextAsync (filePath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText (originalCode);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot ();

            // Find all classes in the file
            IEnumerable<ClassDeclarationSyntax> classes = root.DescendantNodes ().OfType<ClassDeclarationSyntax> ();

            foreach (ClassDeclarationSyntax classDecl in classes)
            {
                // Analyze and split if needed
                var analyzer = new SemanticGroupAnalyzer ();
                List<MemberGroup> groups = analyzer.AnalyzeClass (classDecl);

                if (groups.Count > 1)
                {
                    var generator = new PartialFileGenerator ();
                    generator.GeneratePartials (filePath, root, classDecl, groups);

                    Console.WriteLine ($"✓ Split {classDecl.Identifier.Text} into {groups.Count} partial files");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"Error: {ex.Message}");
            Console.Error.WriteLine (ex.StackTrace);

            return 1;
        }
    }
}

/// <summary>
/// Represents a group of related class members that should be in the same partial file.
/// </summary>
/// <param name="Name">The name of the group (e.g., "Mouse", "Keyboard", "Drawing")</param>
/// <param name="Members">The members belonging to this group</param>
internal record MemberGroup (string Name, List<MemberDeclarationSyntax> Members);

/// <summary>
/// Analyzes class members and groups them by functionality using naming patterns and dependencies.
/// </summary>
internal class SemanticGroupAnalyzer
{
    // Naming pattern heuristics based on Terminal.Gui conventions
    private static readonly Dictionary<string, string []> NamingPatterns = new ()
    {
        ["Mouse"] = ["*Mouse*", "On*MouseEvent", "_isButton*", "_isDoubleClick*", "*Click*"],
        ["Keyboard"] = ["On*KeyEvent", "*Keyboard*", "*Key*", "OnKey*"],
        ["Drawing"] = ["*Draw*", "OnDraw*", "Redraw*", "*Paint*", "*Render*"],
        ["Layout"] = ["*Layout*", "*Position*", "*Size*", "*Bounds*", "*Frame*"],
        ["Navigation"] = ["*Navigate*", "*Focus*", "*Tab*", "SetFocus*", "*CanFocus*"],
        ["Commands"] = ["*Command*", "InvokeCommand*", "*Execute*"],
        ["History"] = ["*History*", "*Undo*", "*Redo*"],
        ["Selection"] = ["*Selection*", "*Select*", "ClearSelection*"],
        ["Text"] = ["*Text*"],
        ["Files"] = ["*File*", "*Load*", "*Save*"],
        ["Movement"] = ["*Move*", "*Scroll*"],
        ["WordWrap"] = ["*Wrap*"],
        ["Find"] = ["*Find*", "*Search*"]
    };

    private const int MinimumLinesPerPartial = 100;

    public List<MemberGroup> AnalyzeClass (ClassDeclarationSyntax classDecl)
    {
        List<MemberDeclarationSyntax> allMembers = [.. classDecl.Members];

        // Group members by naming patterns
        Dictionary<string, List<MemberDeclarationSyntax>> groupedMembers = [];
        HashSet<MemberDeclarationSyntax> assignedMembers = [];

        // First pass: assign members to groups based on naming patterns
        foreach (MemberDeclarationSyntax member in allMembers)
        {
            string memberName = GetMemberName (member);

            if (string.IsNullOrEmpty (memberName))
            {
                continue;
            }

            foreach (KeyValuePair<string, string []> pattern in NamingPatterns)
            {
                string groupName = pattern.Key;
                string [] patterns = pattern.Value;

                foreach (string pat in patterns)
                {
                    if (MatchesPattern (memberName, pat))
                    {
                        if (!groupedMembers.ContainsKey (groupName))
                        {
                            groupedMembers [groupName] = [];
                        }

                        groupedMembers [groupName].Add (member);
                        assignedMembers.Add (member);

                        goto NextMember; // Member assigned, move to next
                    }
                }
            }

            NextMember: ;
        }

        // Second pass: handle backing fields - keep them with their properties
        Dictionary<string, FieldDeclarationSyntax> backingFieldMap = BuildBackingFieldMap (allMembers);

        foreach (MemberDeclarationSyntax member in allMembers)
        {
            if (member is PropertyDeclarationSyntax property
                && backingFieldMap.TryGetValue (property.Identifier.Text, out FieldDeclarationSyntax? backingField)
                && !assignedMembers.Contains (backingField))
            {
                // Find which group the property is in and add the backing field to the same group
                foreach (List<MemberDeclarationSyntax> groupMembers in groupedMembers.Values)
                {
                    if (groupMembers.Contains (property))
                    {
                        groupMembers.Add (backingField);
                        assignedMembers.Add (backingField);

                        break;
                    }
                }
            }
        }

        // Third pass: unassigned members go to "Core" group
        List<MemberDeclarationSyntax> coreMembers = [];

        foreach (MemberDeclarationSyntax member in allMembers)
        {
            if (!assignedMembers.Contains (member))
            {
                // Constructors always go to Core
                coreMembers.Add (member);
            }
        }

        // Build final groups (only include groups that meet minimum line threshold)
        List<MemberGroup> result = [];

        foreach (KeyValuePair<string, List<MemberDeclarationSyntax>> group in groupedMembers)
        {
            if (EstimateLineCount (group.Value) >= MinimumLinesPerPartial)
            {
                result.Add (new MemberGroup (group.Key, group.Value));
            }
            else
            {
                // Too small, add to core
                coreMembers.AddRange (group.Value);
            }
        }

        // Core group always exists
        if (coreMembers.Count > 0 || result.Count == 0)
        {
            result.Insert (0, new MemberGroup ("Core", coreMembers));
        }

        return result;
    }

    private static string GetMemberName (MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            FieldDeclarationSyntax field => string.Join (",", field.Declaration.Variables.Select (v => v.Identifier.Text)),
            EventDeclarationSyntax evt => evt.Identifier.Text,
            _ => string.Empty
        };
    }

    private static bool MatchesPattern (string memberName, string pattern)
    {
        // Simple wildcard matching: * matches anything
        if (pattern.StartsWith ('*') && pattern.EndsWith ('*'))
        {
            string middle = pattern.Trim ('*');

            return memberName.Contains (middle, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith ('*'))
        {
            string suffix = pattern [1..];

            return memberName.EndsWith (suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith ('*'))
        {
            string prefix = pattern [..^1];

            return memberName.StartsWith (prefix, StringComparison.OrdinalIgnoreCase);
        }

        return memberName.Equals (pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, FieldDeclarationSyntax> BuildBackingFieldMap (List<MemberDeclarationSyntax> members)
    {
        Dictionary<string, FieldDeclarationSyntax> map = [];

        foreach (MemberDeclarationSyntax member in members)
        {
            if (member is FieldDeclarationSyntax field)
            {
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    string fieldName = variable.Identifier.Text;

                    if (fieldName.StartsWith ('_') && fieldName.Length > 1)
                    {
                        // _fieldName -> FieldName
                        string potentialPropertyName = char.ToUpper (fieldName [1]) + fieldName [2..];
                        map [potentialPropertyName] = field;
                    }
                }
            }
        }

        return map;
    }

    private static int EstimateLineCount (List<MemberDeclarationSyntax> members)
    {
        int lines = 0;

        foreach (MemberDeclarationSyntax member in members)
        {
            string memberText = member.ToFullString ();
            lines += memberText.Split ('\n').Length;
        }

        return lines;
    }
}

/// <summary>
/// Generates partial class files from member groups.
/// </summary>
internal class PartialFileGenerator
{
    public void GeneratePartials (
        string originalFilePath,
        CompilationUnitSyntax root,
        ClassDeclarationSyntax classDecl,
        List<MemberGroup> groups
    )
    {
        string directory = Path.GetDirectoryName (originalFilePath) ?? ".";
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension (originalFilePath);
        string className = classDecl.Identifier.Text;

        // Get common elements (usings, namespace)
        SyntaxList<UsingDirectiveSyntax> usings = root.Usings;
        BaseNamespaceDeclarationSyntax? namespaceDecl = root.DescendantNodes ()
                                                           .OfType<BaseNamespaceDeclarationSyntax> ()
                                                           .FirstOrDefault ();

        // Collect namespace-level type declarations (delegates, records, enums)
        List<MemberDeclarationSyntax> namespaceLevelDeclarations = [];
        if (namespaceDecl is not null)
        {
            foreach (MemberDeclarationSyntax member in namespaceDecl.Members)
            {
                // Include delegates, records, enums, other types (but not the main class)
                if (member is not ClassDeclarationSyntax cls || cls != classDecl)
                {
                    namespaceLevelDeclarations.Add (member);
                }
            }
        }

        // Generate a partial file for each group
        foreach (MemberGroup group in groups)
        {
            string partialFileName = group.Name == "Core"
                                         ? $"{fileNameWithoutExtension}.cs"
                                         : $"{fileNameWithoutExtension}.{group.Name}.cs";

            string partialFilePath = Path.Combine (directory, partialFileName);

            bool isCore = group.Name == "Core";

            // Build the partial class
            ClassDeclarationSyntax partialClass;

            if (isCore)
            {
                // Core file: preserve base class, interfaces, attributes
                partialClass = SyntaxFactory
                               .ClassDeclaration (className)
                               .WithModifiers (SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.PublicKeyword), SyntaxFactory.Token (SyntaxKind.PartialKeyword)))
                               .WithBaseList (classDecl.BaseList)  // Preserve : View, IDesignable
                               .WithAttributeLists (classDecl.AttributeLists)
                               .WithMembers (new SyntaxList<MemberDeclarationSyntax> (group.Members))
                               .WithLeadingTrivia (classDecl.GetLeadingTrivia ())
                               .WithTrailingTrivia (classDecl.GetTrailingTrivia ());
            }
            else
            {
                // Non-core partials: just public partial class
                partialClass = SyntaxFactory
                               .ClassDeclaration (className)
                               .WithModifiers (SyntaxFactory.TokenList (SyntaxFactory.Token (SyntaxKind.PublicKeyword), SyntaxFactory.Token (SyntaxKind.PartialKeyword)))
                               .WithMembers (new SyntaxList<MemberDeclarationSyntax> (group.Members))
                               .WithLeadingTrivia (classDecl.GetLeadingTrivia ())
                               .WithTrailingTrivia (classDecl.GetTrailingTrivia ());
            }

            // Build the compilation unit
            CompilationUnitSyntax partialRoot;

            if (namespaceDecl is not null)
            {
                // Add namespace-level declarations to Core file only
                List<MemberDeclarationSyntax> namespaceMembers = [];
                if (isCore)
                {
                    namespaceMembers.AddRange (namespaceLevelDeclarations);
                }
                namespaceMembers.Add (partialClass);

                BaseNamespaceDeclarationSyntax partialNamespace = namespaceDecl is FileScopedNamespaceDeclarationSyntax fileScopedNs
                                                                      ? SyntaxFactory
                                                                        .FileScopedNamespaceDeclaration (fileScopedNs.Name)
                                                                        .WithMembers (new SyntaxList<MemberDeclarationSyntax> (namespaceMembers))
                                                                      : SyntaxFactory
                                                                        .NamespaceDeclaration (namespaceDecl.Name)
                                                                        .WithMembers (new SyntaxList<MemberDeclarationSyntax> (namespaceMembers));

                partialRoot = SyntaxFactory.CompilationUnit ()
                                           .WithUsings (usings)
                                           .AddMembers (partialNamespace)
                                           .NormalizeWhitespace ();
            }
            else
            {
                partialRoot = SyntaxFactory.CompilationUnit ()
                                           .WithUsings (usings)
                                           .AddMembers (partialClass)
                                           .NormalizeWhitespace ();
            }

            // Write to file
            string code = partialRoot.ToFullString ();
            File.WriteAllText (partialFilePath, code, Encoding.UTF8);

            Console.WriteLine ($"  Created: {partialFileName} ({group.Members.Count} members)");
        }

        // Delete the original file if it's not the Core file
        string coreFileName = $"{fileNameWithoutExtension}.cs";

        if (!groups.Any (g => g.Name == "Core") || Path.GetFileName (originalFilePath) != coreFileName)
        {
            // Rename original to .backup
            string backupPath = originalFilePath + ".backup";
            File.Move (originalFilePath, backupPath, true);
            Console.WriteLine ($"  Backed up original file to: {Path.GetFileName (backupPath)}");
        }
    }
}
