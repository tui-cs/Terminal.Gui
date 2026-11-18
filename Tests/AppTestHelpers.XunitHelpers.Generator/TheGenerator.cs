using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AppTestHelpers.XunitHelpers.Generator;

[Generator]
public class TheGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize (IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> provider = context.SyntaxProvider.CreateSyntaxProvider (
                                                                             static (node, _) => IsClass (node, "XunitContextExtensions"),
                                                                             static (ctx, _) =>
                                                                                 (ClassDeclarationSyntax)ctx.Node)
                                                                            .Where (m => m is { });

        IncrementalValueProvider<(Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right)> compilation =
            context.CompilationProvider.Combine (provider.Collect ());
        context.RegisterSourceOutput (compilation, Execute);
    }

    private static bool IsClass (SyntaxNode node, string named) { return node is ClassDeclarationSyntax c && c.Identifier.Text == named; }

    private void Execute (SourceProductionContext context, (Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right) arg2)
    {
        INamedTypeSymbol assertType = arg2.Left.GetTypeByMetadataName ("Xunit.Assert")
            ?? throw new NotSupportedException ("Referencing codebase does not include Xunit, could not find Xunit.Assert");

        GenerateMethods (assertType, context, "Equal", false);

        GenerateMethods (assertType, context, "All", true);
        GenerateMethods (assertType, context, "Collection", true);
        GenerateMethods (assertType, context, "Contains", true);
        GenerateMethods (assertType, context, "Distinct", true);
        GenerateMethods (assertType, context, "DoesNotContain", true);
        GenerateMethods (assertType, context, "DoesNotMatch", true);
        GenerateMethods (assertType, context, "Empty", true);
        GenerateMethods (assertType, context, "EndsWith", false);
        GenerateMethods (assertType, context, "Equivalent", true);
        GenerateMethods (assertType, context, "Fail", true);
        GenerateMethods (assertType, context, "False", true);
        GenerateMethods (assertType, context, "InRange", true);
        GenerateMethods (assertType, context, "IsAssignableFrom", true);
        GenerateMethods (assertType, context, "IsNotAssignableFrom", true);
        GenerateMethods (assertType, context, "IsType", true);
        GenerateMethods (assertType, context, "IsNotType", true);

        GenerateMethods (assertType, context, "Matches", true);
        GenerateMethods (assertType, context, "Multiple", true);
        GenerateMethods (assertType, context, "NotEmpty", true);
        GenerateMethods (assertType, context, "NotEqual", true);
        GenerateMethods (assertType, context, "NotInRange", true);
        GenerateMethods (assertType, context, "NotNull", false);
        GenerateMethods (assertType, context, "NotSame", true);
        GenerateMethods (assertType, context, "NotStrictEqual", true);
        GenerateMethods (assertType, context, "Null", false);
        GenerateMethods (assertType, context, "ProperSubset", true);
        GenerateMethods (assertType, context, "ProperSuperset", true);
        GenerateMethods (assertType, context, "Raises", true);
        GenerateMethods (assertType, context, "RaisesAny", true);
        GenerateMethods (assertType, context, "Same", true);
        GenerateMethods (assertType, context, "Single", true);
        GenerateMethods (assertType, context, "StartsWith", false);

        GenerateMethods (assertType, context, "StrictEqual", true);
        GenerateMethods (assertType, context, "Subset", true);
        GenerateMethods (assertType, context, "Superset", true);

        //        GenerateMethods (assertType, context, "Throws", true);
        //      GenerateMethods (assertType, context, "ThrowsAny", true);
        GenerateMethods (assertType, context, "True", false);
    }

    private void GenerateMethods (INamedTypeSymbol assertType, SourceProductionContext context, string methodName, bool invokeTExplicitly)
    {
        var sb = new StringBuilder ();

        // Create a HashSet to track unique method signatures
        HashSet<string> signaturesDone = new ();

        List<IMethodSymbol> methods = assertType
                                      .GetMembers (methodName)
                                      .OfType<IMethodSymbol> ()
                                      .ToList ();

        var header = """"
                     #nullable enable
                     using AppTestHelpers;
                     using Xunit;

                     namespace AppTestHelpers.XunitHelpers;

                     public static partial class XunitContextExtensions
                     {


                     """";

        var tail = """

                   }
                   """;

        sb.AppendLine (header);

        foreach (IMethodSymbol? m in methods)
        {
            string signature = GetModifiedMethodSignature (m, methodName, invokeTExplicitly, out string [] paramNames, out string typeParams);

            if (!signaturesDone.Add (signature))
            {
                continue;
            }

            var method = $$"""
                               {{signature}}
                               {
                                   try
                                   {
                                       Xunit.Assert.{{methodName}}{{typeParams}} ({{string.Join (",", paramNames)}});
                                   }
                                   catch(Exception ex)
                                   {
                                       context.HardStop (ex);
                                       
                                   
                                       throw;
                                   
                                   }
                                   
                                   return context;
                               }
                           """;

            sb.AppendLine (method.Replace ("*", ""));
        }

        sb.AppendLine (tail);

        context.AddSource ($"XunitContextExtensions{methodName}.g.cs", sb.ToString ());
    }

    private string GetModifiedMethodSignature (
        IMethodSymbol methodSymbol,
        string methodName,
        bool invokeTExplicitly,
        out string [] paramNames,
        out string typeParams
    )
    {
        typeParams = string.Empty;

        // Create the "this AppTestHelper context" parameter
        ParameterSyntax contextParam = SyntaxFactory.Parameter (SyntaxFactory.Identifier ("context"))
                                                    .WithType (SyntaxFactory.ParseTypeName ("AppTestHelper"))
                                                    .AddModifiers (SyntaxFactory.Token (SyntaxKind.ThisKeyword)); // Add the "this" keyword

        // Extract the parameter names (expected and actual)
        paramNames = new string [methodSymbol.Parameters.Length];

        for (var i = 0; i < methodSymbol.Parameters.Length; i++)
        {
            paramNames [i] = methodSymbol.Parameters.ElementAt (i).Name;

            // Check if the parameter name is a reserved keyword and prepend "@" if it is
            if (IsReservedKeyword (paramNames [i]))
            {
                paramNames [i] = "@" + paramNames [i];
            }
            else
            {
                paramNames [i] = paramNames [i];
            }
        }

        // Get the current method parameters and add the context parameter at the start
        List<ParameterSyntax> parameters = methodSymbol.Parameters.Select (p => CreateParameter (p)).ToList ();

        parameters.Insert (0, contextParam); // Insert 'context' as the first parameter

        // Change the return type to AppTestHelper
        TypeSyntax returnType = SyntaxFactory.ParseTypeName ("AppTestHelper");

        // Change the method name to AssertEqual
        SyntaxToken newMethodName = SyntaxFactory.Identifier ($"Assert{methodName}");

        // Handle generic type parameters if the method is generic
        TypeParameterSyntax [] typeParameters = methodSymbol.TypeParameters.Select (
                                                                                    tp =>
                                                                                        SyntaxFactory.TypeParameter (SyntaxFactory.Identifier (tp.Name))
                                                                                   )
                                                            .ToArray ();

        MethodDeclarationSyntax dec = SyntaxFactory.MethodDeclaration (returnType, newMethodName)
                                                   .WithModifiers (
                                                                   SyntaxFactory.TokenList (
                                                                                            SyntaxFactory.Token (SyntaxKind.PublicKeyword),
                                                                                            SyntaxFactory.Token (SyntaxKind.StaticKeyword)))
                                                   .WithParameterList (SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList (parameters)));

        if (typeParameters.Any ())
        {
            // Add the <T> here
            dec = dec.WithTypeParameterList (SyntaxFactory.TypeParameterList (SyntaxFactory.SeparatedList (typeParameters)));

            // Handle type parameter constraints
            List<TypeParameterConstraintClauseSyntax> constraintClauses = new ();

            foreach (ITypeParameterSymbol tp in methodSymbol.TypeParameters)
            {
                List<TypeParameterConstraintSyntax> constraints = new ();

                // Add class/struct constraints
                if (tp.HasReferenceTypeConstraint)
                {
                    constraints.Add (SyntaxFactory.ClassOrStructConstraint (SyntaxKind.ClassConstraint));
                }
                else if (tp.HasValueTypeConstraint)
                {
                    constraints.Add (SyntaxFactory.ClassOrStructConstraint (SyntaxKind.StructConstraint));
                }
                else if (tp.HasNotNullConstraint)
                {
                    // Add notnull constraint
                    constraints.Add (SyntaxFactory.TypeConstraint (SyntaxFactory.IdentifierName ("notnull")));
                }

                // Add type constraints
                foreach (ITypeSymbol constraintType in tp.ConstraintTypes)
                {
                    constraints.Add (
                        SyntaxFactory.TypeConstraint (
                            SyntaxFactory.ParseTypeName (constraintType.ToDisplayString ())));
                }

                // Add new() constraint
                if (tp.HasConstructorConstraint)
                {
                    constraints.Add (SyntaxFactory.ConstructorConstraint ());
                }

                // Only add constraint clause if there are constraints
                if (constraints.Any ())
                {
                    constraintClauses.Add (
                        SyntaxFactory.TypeParameterConstraintClause (tp.Name)
                            .WithConstraints (SyntaxFactory.SeparatedList (constraints)));
                }
            }

            if (constraintClauses.Any ())
            {
                dec = dec.WithConstraintClauses (SyntaxFactory.List (constraintClauses));
            }

            // Add the <T> here
            if (invokeTExplicitly)
            {
                typeParams = "<" + string.Join (", ", typeParameters.Select (tp => tp.Identifier.ValueText)) + ">";
            }
        }

        // Build the method signature syntax tree
        MethodDeclarationSyntax methodSyntax = dec.NormalizeWhitespace ();

        // Convert the method syntax to a string
        var methodString = methodSyntax.ToString ();

        return methodString;
    }

    /// <summary>
    ///     Creates a <see cref="ParameterSyntax"/> from a discovered parameter on real xunit method parameter
    ///     <paramref name="p"/>
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private ParameterSyntax CreateParameter (IParameterSymbol p)
    {
        string paramName = p.Name;

        // Check if the parameter name is a reserved keyword and prepend "@" if it is
        if (IsReservedKeyword (paramName))
        {
            paramName = "@" + paramName;
        }

        // Create the basic parameter syntax with the modified name and type
        ParameterSyntax parameterSyntax = SyntaxFactory.Parameter (SyntaxFactory.Identifier (paramName))
                                                       .WithType (SyntaxFactory.ParseTypeName (p.Type.ToDisplayString ()));

        // Add 'params' keyword if the parameter has the Params modifier
        var modifiers = new List<SyntaxToken> ();

        if (p.IsParams)
        {
            modifiers.Add (SyntaxFactory.Token (SyntaxKind.ParamsKeyword));
        }

        // Handle ref/out/in modifiers
        if (p.RefKind != RefKind.None)
        {
            SyntaxKind modifierKind = p.RefKind switch
            {
                RefKind.Ref => SyntaxKind.RefKeyword,
                RefKind.Out => SyntaxKind.OutKeyword,
                RefKind.In => SyntaxKind.InKeyword,
                _ => throw new NotSupportedException ($"Unsupported RefKind: {p.RefKind}")
            };


            modifiers.Add (SyntaxFactory.Token (modifierKind));
        }


        if (modifiers.Any ())
        {
            parameterSyntax = parameterSyntax.WithModifiers (SyntaxFactory.TokenList (modifiers));
        }

        // Add default value if one is present
        if (p.HasExplicitDefaultValue)
        {
            ExpressionSyntax defaultValueExpression = p.ExplicitDefaultValue switch
            {
                null => SyntaxFactory.LiteralExpression (SyntaxKind.NullLiteralExpression),
                bool b => SyntaxFactory.LiteralExpression (
                                                           b
                                                               ? SyntaxKind.TrueLiteralExpression
                                                               : SyntaxKind.FalseLiteralExpression),
                int i => SyntaxFactory.LiteralExpression (
                                                          SyntaxKind.NumericLiteralExpression,
                                                          SyntaxFactory.Literal (i)),
                double d => SyntaxFactory.LiteralExpression (
                                                             SyntaxKind.NumericLiteralExpression,
                                                             SyntaxFactory.Literal (d)),
                string s => SyntaxFactory.LiteralExpression (
                                                             SyntaxKind.StringLiteralExpression,
                                                             SyntaxFactory.Literal (s)),
                _ => SyntaxFactory.ParseExpression (p.ExplicitDefaultValue.ToString ()) // Fallback
            };

            parameterSyntax = parameterSyntax.WithDefault (
                                                           SyntaxFactory.EqualsValueClause (defaultValueExpression)
                                                          );
        }

        return parameterSyntax;
    }

    // Helper method to check if a parameter name is a reserved keyword
    private bool IsReservedKeyword (string name) { return string.Equals (name, "object"); }
}