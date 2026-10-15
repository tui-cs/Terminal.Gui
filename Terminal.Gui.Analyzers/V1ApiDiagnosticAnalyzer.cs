using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Gui.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class V1ApiDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const string Category = "TerminalGuiV2";

        public static readonly DiagnosticDescriptor RuleClicked = new DiagnosticDescriptor(
            "TGUI0001",
            "Use Accepted or Accepting instead of Clicked",
            "'{0}' is a removed v1 member. In Terminal.Gui v2, use 'Accepted' (post-event) or 'Accepting' (cancellable event)",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleApplicationStatic = new DiagnosticDescriptor(
            "TGUI0002",
            "Use instance Application instead of static methods",
            "Static Application.{0} is removed in Terminal.Gui v2. Use 'IApplication app = Application.Create().Init()', 'app.Run<T>()', and 'app.Dispose()'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleToplevel = new DiagnosticDescriptor(
            "TGUI0003",
            "Use Runnable or Window instead of Toplevel",
            "'Toplevel' has been removed in Terminal.Gui v2. Inherit from 'Runnable' or use 'Window'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleBounds = new DiagnosticDescriptor(
            "TGUI0004",
            "Use Viewport instead of Bounds",
            "'View.Bounds' was removed in Terminal.Gui v2. Use 'View.Viewport'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RulePositionalConstructor = new DiagnosticDescriptor(
            "TGUI0005",
            "Use object initializers for Views",
            "Terminal.Gui v2 views use parameterless constructors and object initializers: 'new {0} {{ Text = \"...\" }}'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleTabView = new DiagnosticDescriptor(
            "TGUI0006",
            "Use Tabs instead of TabView",
            "'TabView' was replaced by 'Tabs' in Terminal.Gui v2. Add SubViews directly and set 'View.Title' for tab labels",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleTileView = new DiagnosticDescriptor(
            "TGUI0007",
            "TileView is removed in Terminal.Gui v2",
            "'TileView' was removed in Terminal.Gui v2. Use Pos/Dim panes and ViewArrangement",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleRadioGroup = new DiagnosticDescriptor(
            "TGUI0008",
            "Use OptionSelector instead of RadioGroup",
            "'RadioGroup' was replaced by 'OptionSelector' in Terminal.Gui v2",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleColorScheme = new DiagnosticDescriptor(
            "TGUI0009",
            "Use Scheme instead of ColorScheme",
            "Terminal.Gui v2 replaces 'ColorScheme' with 'Scheme' and 'SchemeManager'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleKeyBitmask = new DiagnosticDescriptor(
            "TGUI0010",
            "Use Key fluent methods instead of KeyCode bitmasks",
            "Terminal.Gui v2 key bitmasks are replaced by fluent methods like 'Key.X.WithCtrl' or 'Key.X.WithAlt'",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleAcceptingIgnoresArgs = new DiagnosticDescriptor(
            "TGUI0011",
            "Use Accepted when event args are unused",
            "The 'Accepting' event is a pre-event for cancellation. If event arguments (e.g. e.Cancel) are not used, subscribe to 'Accepted' instead",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleMenuItem6ArgCtor = new DiagnosticDescriptor(
            "TGUI0012",
            "Use v2 MenuItem constructor",
            "Terminal.Gui v2 MenuItem constructor takes (title, help, action). Use object initializers for Shortcut/Data",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RuleLayoutStyle = new DiagnosticDescriptor(
            "TGUI0013",
            "LayoutStyle is removed in v2",
            "'LayoutStyle' was removed in Terminal.Gui v2. All layout is declarative via Pos and Dim",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RulePosAt = new DiagnosticDescriptor(
            "TGUI0014",
            "Use direct integer assignment instead of Pos.At / Pos.Left",
            "'Pos.At(n)' and 'Pos.Left(v)' are removed in Terminal.Gui v2. Assign integers directly e.g. X = 5",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new[] {
            RuleClicked,
            RuleApplicationStatic,
            RuleToplevel,
            RuleBounds,
            RulePositionalConstructor,
            RuleTabView,
            RuleTileView,
            RuleRadioGroup,
            RuleColorScheme,
            RuleKeyBitmask,
            RuleAcceptingIgnoresArgs,
            RuleMenuItem6ArgCtor,
            RuleLayoutStyle,
            RulePosAt });

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeBitwiseOr, SyntaxKind.BitwiseOrExpression);
            context.RegisterSyntaxNodeAction(AnalyzeEventSubscription, SyntaxKind.AddAssignmentExpression);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var name = memberAccess.Name.Identifier.Text;

            if (name == "Clicked")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleClicked, memberAccess.Name.GetLocation(), memberAccess.ToString()));
            }
            else if (name == "Bounds")
            {
                var exprType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
                if (exprType != null && IsOrInheritsFromView(exprType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleBounds, memberAccess.Name.GetLocation()));
                }
            }
            else if (name == "ColorScheme" || name == "ColorSchemes")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleColorScheme, memberAccess.Name.GetLocation()));
            }
            else if (name == "Top" && memberAccess.Expression is IdentifierNameSyntax id && id.Identifier.Text == "Application")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleApplicationStatic, memberAccess.GetLocation(), "Top"));
            }
            else if (name == "Computed" && memberAccess.Expression is IdentifierNameSyntax ls && ls.Identifier.Text == "LayoutStyle")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleLayoutStyle, memberAccess.GetLocation()));
            }
        }

        private static bool IsOrInheritsFromView(ITypeSymbol typeSymbol)
        {
            var current = typeSymbol;
            while (current != null)
            {
                if (current.Name == "View" || current.Name == "Toplevel" || current.Name == "Window" || current.Name == "Button")
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Expression is IdentifierNameSyntax id && id.Identifier.Text == "Application")
                {
                    var methodName = memberAccess.Name.Identifier.Text;
                    if (methodName == "Init" || methodName == "Run" || methodName == "Shutdown")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleApplicationStatic, memberAccess.GetLocation(), methodName));
                    }
                }
                else if (memberAccess.Expression is IdentifierNameSyntax pos && pos.Identifier.Text == "Pos")
                {
                    var posMethod = memberAccess.Name.Identifier.Text;
                    if (posMethod == "At" || posMethod == "Left")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RulePosAt, invocation.GetLocation()));
                    }
                }
            }
        }

        private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
        {
            var identifier = (IdentifierNameSyntax)context.Node;
            var text = identifier.Identifier.Text;

            if (identifier.Parent is TypeDeclarationSyntax ||
                identifier.Parent is NamespaceDeclarationSyntax ||
                identifier.Parent is ParameterSyntax ||
                identifier.Parent is VariableDeclaratorSyntax)
            {
                return;
            }

            if (text == "Toplevel")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleToplevel, identifier.GetLocation()));
            }
            else if (text == "TabView" || text == "Tab")
            {
                if (identifier.Parent is ObjectCreationExpressionSyntax ||
                    identifier.Parent is VariableDeclarationSyntax ||
                    identifier.Parent is GenericNameSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleTabView, identifier.GetLocation()));
                }
            }
            else if (text == "TileView")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleTileView, identifier.GetLocation()));
            }
            else if (text == "RadioGroup")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleRadioGroup, identifier.GetLocation()));
            }
            else if (text == "LayoutStyle")
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleLayoutStyle, identifier.GetLocation()));
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            string? typeName = objectCreation.Type switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                QualifiedNameSyntax qn => qn.Right.Identifier.Text,
                _ => null
            };

            if (typeName == "MenuItem" && objectCreation.ArgumentList.Arguments.Count > 3)
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleMenuItem6ArgCtor, objectCreation.GetLocation()));
            }
            else if (typeName == "Button" || typeName == "Label" || typeName == "Window" || typeName == "TextField")
            {
                context.ReportDiagnostic(Diagnostic.Create(RulePositionalConstructor, objectCreation.GetLocation(), typeName));
            }
        }

        private static void AnalyzeBitwiseOr(SyntaxNodeAnalysisContext context)
        {
            var binaryExpr = (BinaryExpressionSyntax)context.Node;
            string exprText = binaryExpr.ToString();

            if (exprText.Contains("CtrlMask") || exprText.Contains("AltMask") || exprText.Contains("ShiftMask"))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleKeyBitmask, binaryExpr.GetLocation()));
            }
        }

        private static void AnalyzeEventSubscription(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment.Left is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "Accepting")
            {
                if (assignment.Right is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    if (simpleLambda.Parameter.Identifier.Text == "_")
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleAcceptingIgnoresArgs, assignment.GetLocation()));
                    }
                }
                else if (assignment.Right is ParenthesizedLambdaExpressionSyntax lambda)
                {
                    var paramsList = lambda.ParameterList.Parameters;
                    if (paramsList.Count >= 2)
                    {
                        var arg2 = paramsList[1].Identifier.Text;
                        if (arg2 == "_" || !LambdaBodyUsesIdentifier(lambda.Body, arg2))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(RuleAcceptingIgnoresArgs, assignment.GetLocation()));
                        }
                    }
                }
            }
        }

        private static bool LambdaBodyUsesIdentifier(SyntaxNode body, string name)
        {
            if (body == null || string.IsNullOrEmpty(name) || name == "_")
            {
                return false;
            }

            return body.DescendantNodes()
                       .OfType<IdentifierNameSyntax>()
                       .Any(id => id.Identifier.Text == name);
        }
    }
}
