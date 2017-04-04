using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LogicalComparisonToSelf : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(LogicalComparisonToSelf);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSameForBooleansOnly, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeForSame, SyntaxKind.LogicalOrExpression, SyntaxKind.LogicalAndExpression);
        }

        private static void AnalyzeSameForBooleansOnly(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            var booleanType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Boolean);

            if (context.SemanticModel.GetTypeInfo(binary.Left, context.CancellationToken).Type == booleanType &&
                context.SemanticModel.GetTypeInfo(binary.Right, context.CancellationToken).Type == booleanType)
                AnalyzeForSame(context);
        }

        private static void AnalyzeForSame(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            if (binary.Left.IsEquivalentTo(binary.Right))
                context.MarkAsUnnecessary(Rule, binary.OperatorToken.GetLocation(), binary.Right.GetLocation());
        }
    }
}
