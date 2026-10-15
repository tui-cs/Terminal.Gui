using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terminal.Gui.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(V1ApiCodeFixProvider)), Shared]
    public class V1ApiCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(new[] {
            V1ApiDiagnosticAnalyzer.RuleClicked.Id,
            V1ApiDiagnosticAnalyzer.RuleToplevel.Id,
            V1ApiDiagnosticAnalyzer.RuleBounds.Id,
            V1ApiDiagnosticAnalyzer.RuleTabView.Id,
            V1ApiDiagnosticAnalyzer.RuleRadioGroup.Id,
            V1ApiDiagnosticAnalyzer.RuleAcceptingIgnoresArgs.Id });

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var token = root.FindToken(diagnosticSpan.Start);

                if (diagnostic.Id == V1ApiDiagnosticAnalyzer.RuleClicked.Id)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with 'Accepted'",
                            createChangedDocument: c => ReplaceTokenAsync(context.Document, root, token, "Accepted", c),
                            equivalenceKey: "UseAccepted"),
                        diagnostic);
                }
                else if (diagnostic.Id == V1ApiDiagnosticAnalyzer.RuleBounds.Id)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with 'Viewport'",
                            createChangedDocument: c => ReplaceTokenAsync(context.Document, root, token, "Viewport", c),
                            equivalenceKey: "UseViewport"),
                        diagnostic);
                }
                else if (diagnostic.Id == V1ApiDiagnosticAnalyzer.RuleToplevel.Id)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with 'Window'",
                            createChangedDocument: c => ReplaceTokenAsync(context.Document, root, token, "Window", c),
                            equivalenceKey: "UseWindow"),
                        diagnostic);
                }
                else if (diagnostic.Id == V1ApiDiagnosticAnalyzer.RuleTabView.Id)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with 'Tabs'",
                            createChangedDocument: c => ReplaceTokenAsync(context.Document, root, token, "Tabs", c),
                            equivalenceKey: "UseTabs"),
                        diagnostic);
                }
                else if (diagnostic.Id == V1ApiDiagnosticAnalyzer.RuleRadioGroup.Id)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with 'OptionSelector'",
                            createChangedDocument: c => ReplaceTokenAsync(context.Document, root, token, "OptionSelector", c),
                            equivalenceKey: "UseOptionSelector"),
                        diagnostic);
                }
                else if (diagnostic.Id == V1ApiDiagnosticAnalyzer.RuleAcceptingIgnoresArgs.Id)
                {
                    var node = root.FindNode(diagnosticSpan);
                    if (node is AssignmentExpressionSyntax assignment && assignment.Left is MemberAccessExpressionSyntax memberAccess)
                    {
                        var eventToken = memberAccess.Name.Identifier;
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: "Switch event to 'Accepted'",
                                createChangedDocument: c => ReplaceTokenAsync(context.Document, root, eventToken, "Accepted", c),
                                equivalenceKey: "SwitchToAccepted"),
                            diagnostic);
                    }
                }
            }
        }

        private static Task<Document> ReplaceTokenAsync(Document document, SyntaxNode root, SyntaxToken oldToken, string newName, CancellationToken cancellationToken)
        {
            var newToken = SyntaxFactory.Identifier(oldToken.LeadingTrivia, newName, oldToken.TrailingTrivia);
            var newRoot = root.ReplaceToken(oldToken, newToken);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}
