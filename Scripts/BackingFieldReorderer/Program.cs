#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BackingFieldReorderer;

internal class Program
{
    private static async Task<int> Main (string [] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine ("Usage: BackingFieldReorderer <file.cs>");

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

            var reorderer = new BackingFieldReordererRewriter ();
            SyntaxNode reordered = reorderer.Visit (root);

            string reorderedCode = reordered.ToFullString ();
            await File.WriteAllTextAsync (filePath, reorderedCode);

            Console.WriteLine ($"✓ Reordered backing fields in {Path.GetFileName (filePath)}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"Error: {ex.Message}");

            return 1;
        }
    }
}

/// <summary>
/// Roslyn syntax rewriter that reorders class members to place backing fields immediately before their corresponding properties.
/// </summary>
internal class BackingFieldReordererRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration (ClassDeclarationSyntax node)
    {
        // Get all members
        List<MemberDeclarationSyntax> members = [.. node.Members];

        // Build mapping: property name -> backing field
        Dictionary<string, FieldDeclarationSyntax> backingFieldMap = [];
        Dictionary<string, PropertyDeclarationSyntax> propertyMap = [];

        foreach (MemberDeclarationSyntax member in members)
        {
            if (member is FieldDeclarationSyntax field)
            {
                // Check if this is a backing field (starts with _)
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    string fieldName = variable.Identifier.Text;

                    if (fieldName.StartsWith ('_') && fieldName.Length > 1)
                    {
                        // Potential backing field: _fieldName -> FieldName
                        string potentialPropertyName = char.ToUpper (fieldName [1]) + fieldName [2..];
                        backingFieldMap [potentialPropertyName] = field;
                    }
                }
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                propertyMap [property.Identifier.Text] = property;
            }
        }

        // Reorder members: place backing field immediately before its property
        List<MemberDeclarationSyntax> reorderedMembers = [];
        HashSet<MemberDeclarationSyntax> processedBackingFields = [];

        foreach (MemberDeclarationSyntax member in members)
        {
            // If this member is a backing field that should be with a property, skip it here
            // (it will be added before its property)
            if (member is FieldDeclarationSyntax field && backingFieldMap.ContainsValue (field))
            {
                // Check if any property uses this backing field
                bool isBackingField = false;

                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    string fieldName = variable.Identifier.Text;

                    if (fieldName.StartsWith ('_') && fieldName.Length > 1)
                    {
                        string potentialPropertyName = char.ToUpper (fieldName [1]) + fieldName [2..];

                        if (propertyMap.ContainsKey (potentialPropertyName))
                        {
                            isBackingField = true;

                            break;
                        }
                    }
                }

                if (isBackingField)
                {
                    continue; // Skip, will be added before property
                }
            }

            // If this member is a property with a backing field, add backing field first
            if (member is PropertyDeclarationSyntax property
                && backingFieldMap.TryGetValue (property.Identifier.Text, out FieldDeclarationSyntax? backingField)
                && !processedBackingFields.Contains (backingField))
            {
                reorderedMembers.Add (backingField);
                processedBackingFields.Add (backingField);
            }

            // Add the member itself
            reorderedMembers.Add (member);
        }

        // Create new class with reordered members
        ClassDeclarationSyntax reorderedClass = node.WithMembers (new SyntaxList<MemberDeclarationSyntax> (reorderedMembers));

        return base.VisitClassDeclaration (reorderedClass);
    }
}
