using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnaryZero : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnaryZero);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMultiply, SyntaxKind.UnaryMinusExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMultiply, SyntaxKind.UnaryPlusExpression);
        }

        private static void AnalyzeMultiply(SyntaxNodeAnalysisContext context)
        {
            var unary = context.Node as PrefixUnaryExpressionSyntax;

            Optional<object> value = context.SemanticModel.GetConstantValue(unary.Operand, context.CancellationToken);

            // -0, +0
            if (value.HasValue && value.Value.Equals(0))
                context.ReportDiagnostic(Diagnostic.Create(Rule, unary.OperatorToken.GetLocation()));
        }
    }
}
