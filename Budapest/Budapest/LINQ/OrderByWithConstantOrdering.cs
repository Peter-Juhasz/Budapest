using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryThenBy : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryThenBy);

        private static readonly string Title = "Where clause has no effect";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeOrderByClause, SyntaxKind.OrderByClause);
            context.RegisterSyntaxNodeAction(AnalyzeOrderByOrThenByMethod, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeOrderByClause(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as OrderByClauseSyntax;

            if (syntax.Orderings.All(o => context.SemanticModel.GetConstantValue(o.Expression, context.CancellationToken).HasValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation()));
                return;
            }

            for (int i = 0; i < syntax.Orderings.Count; i++)
            {
                var ordering = syntax.Orderings[i];

                if (context.SemanticModel.GetConstantValue(ordering.Expression, context.CancellationToken).HasValue)
                {
                    if (i > 0)
                        context.MarkAsUnnecessary(Rule, syntax.Orderings.GetSeparator(i - 1).GetLocation(), ordering.GetLocation());
                    else
                        context.ReportDiagnostic(Diagnostic.Create(Rule, ordering.GetLocation()));
                }
            }
        }

        private static void AnalyzeOrderByOrThenByMethod(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax syntax = context.Node as InvocationExpressionSyntax;

            if (!syntax.ArgumentList.Arguments.Any())
                return;

            if (!syntax.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                return;

            MemberAccessExpressionSyntax access = syntax.Expression as MemberAccessExpressionSyntax;

            if (!access.Name.IsKind(SyntaxKind.IdentifierName))
                return;

            IdentifierNameSyntax identifier = access.Name as IdentifierNameSyntax;

            if (identifier.Identifier.ValueText != "OrderBy" &&
                identifier.Identifier.ValueText != "OrderByDescending" &&
                identifier.Identifier.ValueText != "ThenBy" &&
                identifier.Identifier.ValueText != "ThenByDescending")
                return;

            var arg = syntax.ArgumentList.Arguments.First().Expression;

            if (!arg.IsKind(SyntaxKind.SimpleLambdaExpression) &&
                !arg.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                return;

            var value = context.SemanticModel.GetConstantValue((arg as LambdaExpressionSyntax).Body);

            if (value.HasValue)
            {
                context.MarkAsUnnecessary(Rule, access.OperatorToken, syntax.ArgumentList.CloseParenToken);
            }
        }
    }
}
