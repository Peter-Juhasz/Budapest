using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddOrSubtractConstantZero : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(AddOrSubtractConstantZero);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAdd, SyntaxKind.AddExpression);
            context.RegisterSyntaxNodeAction(AnalyzeSubtract, SyntaxKind.SubtractExpression);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.AddAssignmentExpression, SyntaxKind.SubtractAssignmentExpression);
        }

        private static void AnalyzeAdd(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object>
                left = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x + 0
            if (right.HasValue && right.Value.Equals(0))
            {
                context.MarkAsUnnecessary(Rule, binary.OperatorToken.GetLocation(), binary.Right.GetLocation());
            }

            // 0 + x
            else if (left.HasValue && left.Value.Equals(0))
            {
                context.MarkAsUnnecessary(Rule, binary.Left.GetLocation(), binary.OperatorToken.GetLocation());
            }
        }
        
        private static void AnalyzeSubtract(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object>
                left = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x - 0
            if (right.HasValue && right.Value.Equals(0))
            {
                context.MarkAsUnnecessary(Rule, binary.OperatorToken.GetLocation(), binary.Right.GetLocation());
            }

            // 0 - x
            else if (left.HasValue && left.Value.Equals(0))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, binary.Left.GetLocation()));
            }
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as AssignmentExpressionSyntax;

            Optional<object> right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x += 0
            // x -= 0
            if (right.HasValue && right.Value.Equals(0))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, binary.Parent.GetLocation()));
            }
        }

    }
}
