using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryConditionalExpression : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryConditionalExpression);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeEquals, SyntaxKind.ConditionalExpression);
        }

        private static void AnalyzeEquals(SyntaxNodeAnalysisContext context)
        {
            var ternary = context.Node as ConditionalExpressionSyntax;

            Optional<object>
                condition = context.SemanticModel.GetConstantValue(ternary.Condition, context.CancellationToken),
                left = context.SemanticModel.GetConstantValue(ternary.WhenTrue, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(ternary.WhenFalse, context.CancellationToken);

            // true ? x : y
            if (condition.HasValue && condition.Value.Equals(true))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                ternary.Condition.GetLocation().SourceSpan.Start,
                                ternary.QuestionToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                ternary.ColonToken.GetLocation().SourceSpan.Start,
                                ternary.WhenFalse.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // x ? true : false
            else if (
                left.HasValue && left.Value.Equals(true) &&
                right.HasValue && right.Value.Equals(false)
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                ternary.QuestionToken.GetLocation().SourceSpan.Start,
                                ternary.WhenFalse.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }
        }
    }
}
