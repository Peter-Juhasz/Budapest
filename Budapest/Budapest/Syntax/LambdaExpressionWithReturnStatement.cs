using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LambdaExpressionWithReturnStatement : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(LambdaExpressionWithReturnStatement);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as LambdaExpressionSyntax;

            if (!syntax.Body.IsKind(SyntaxKind.Block))
                return;

            var block = syntax.Body as BlockSyntax;

            if (block.Statements.Count == 1)
            {
                var statement = block.Statements.Single();

                if (!statement.IsKind(SyntaxKind.ReturnStatement))
                    return;

                var @return = statement as ReturnStatementSyntax;

                if (@return.Expression == null)
                    context.ReportDiagnostic(Diagnostic.Create(Rule, @return.GetLocation()));
                else
                {
                    context.MarkAsUnnecessary(Rule, block.OpenBraceToken, @return.ReturnKeyword);
                    context.MarkAsUnnecessary(Rule, @return.SemicolonToken, block.CloseBraceToken);
                }
            }
        }
    }
}

