using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncMethodAwaitsOnlyAtReturn : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(AsyncMethodAwaitsOnlyAtReturn);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as MethodDeclarationSyntax;

            if (!syntax.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            var awaits = syntax.DescendantNodes().OfType<AwaitExpressionSyntax>().ToList();

            if (awaits.Any() && awaits.All(a => a.Parent.IsKind(SyntaxKind.ReturnStatement)))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Modifiers.Single(m => m.IsKind(SyntaxKind.AsyncKeyword)).GetLocation()));

                foreach (var await in awaits)
                    context.ReportDiagnostic(Diagnostic.Create(Rule, await.AwaitKeyword.GetLocation()));
            }
        }
    }
}

