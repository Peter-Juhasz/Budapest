using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DivideByConstantOne : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(DivideByConstantOne);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMultiply, SyntaxKind.DivideExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMultiply, SyntaxKind.DivideAssignmentExpression);
        }

        private static void AnalyzeMultiply(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object> right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x / 1
            if (right.HasValue && right.Value.Equals(1))
            {
                context.MarkAsUnnecessary(Rule, binary.OperatorToken.GetLocation(), binary.Right.GetLocation());
            }
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as AssignmentExpressionSyntax;

            Optional<object> right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x /= 1
            if (right.HasValue && right.Value.Equals(1))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, binary.Parent.GetLocation()));
            }
        }
    }
}
