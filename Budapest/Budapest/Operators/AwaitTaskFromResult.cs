using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitTaskFromResult : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(AwaitTaskFromResult);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMultiply, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeMultiply(SyntaxNodeAnalysisContext context)
        {
            var unary = context.Node as AwaitExpressionSyntax;

            if (!unary.Expression.IsKind(SyntaxKind.InvocationExpression))
                return;

            var invocation = unary.Expression as InvocationExpressionSyntax;

            if (!invocation.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                return;

            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;

            var type = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
            var methodName = memberAccess.Name.Identifier.ValueText;

            if (type.Name == "Task" && methodName == "FromResult" && invocation.ArgumentList.Arguments.Any())
            {
                context.MarkAsUnnecessary(Rule, invocation.Expression.GetLocation(), invocation.ArgumentList.OpenParenToken.GetLocation());
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.ArgumentList.CloseParenToken.GetLocation()));
            }
        }
    }
}
