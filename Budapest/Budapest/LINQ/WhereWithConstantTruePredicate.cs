using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WhereWithConstantTruePredicate : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(WhereWithConstantTruePredicate);

        private static readonly string Title = "Where clause has no effect";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeWhereClause, SyntaxKind.WhereClause);
            context.RegisterSyntaxNodeAction(AnalyzeWhereMethod, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeWhereClause(SyntaxNodeAnalysisContext context)
        {
            WhereClauseSyntax syntax = context.Node as WhereClauseSyntax;

            var value = context.SemanticModel.GetConstantValue(syntax.Condition, context.CancellationToken);

            if (value.HasValue && value.Value.Equals(true))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation()));
            }
        }

        private static void AnalyzeWhereMethod(SyntaxNodeAnalysisContext context)
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

            if (identifier.Identifier.ValueText != "Where")
                return;

            var arg = syntax.ArgumentList.Arguments.First().Expression;

            if (!arg.IsKind(SyntaxKind.SimpleLambdaExpression) &&
                !arg.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                return;

            var value = context.SemanticModel.GetConstantValue((arg as LambdaExpressionSyntax).Body);

            if (value.HasValue && value.Value.Equals(true))
            {
                context.MarkAsUnnecessary(Rule, access.OperatorToken, syntax.ArgumentList.CloseParenToken);
            }
        }
    }
}
