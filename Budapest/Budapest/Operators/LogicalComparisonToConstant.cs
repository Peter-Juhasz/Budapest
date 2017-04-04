using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryBooleanComparison : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryBooleanComparison);

        private static readonly string Title = "Unnecessary boolean comparison";
        private static readonly string MessageFormat = "Comparison is redunant";
        private static readonly string Description = "Comparison is redundant.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeEquals, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeOr, SyntaxKind.LogicalOrExpression, SyntaxKind.LogicalAndExpression);
        }

        private static void AnalyzeEquals(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object>
                left = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x == true
            if (right.Value?.Equals(true) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.OperatorToken.GetLocation().SourceSpan.Start,
                                binary.Right.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // true == x
            else if (left.Value?.Equals(true) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.Left.GetLocation().SourceSpan.Start,
                                binary.OperatorToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }
        }

        private static void AnalyzeOr(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object>
                left = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x || true
            if (right.Value?.Equals(true) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.Left.GetLocation().SourceSpan.Start,
                                binary.OperatorToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // true || x
            else if (left.Value?.Equals(true) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.OperatorToken.GetLocation().SourceSpan.Start,
                                binary.Right.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // x || false
            else if (right.Value?.Equals(false) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.OperatorToken.GetLocation().SourceSpan.Start,
                                binary.Right.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // false || x
            else if (left.Value?.Equals(false) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.Left.GetLocation().SourceSpan.Start,
                                binary.OperatorToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }
        }

        private static void AnalyzeNotEquals(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object>
                left = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x != false
            if (right.Value?.Equals(false) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.OperatorToken.GetLocation().SourceSpan.Start,
                                binary.Right.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // false != x
            else if (left.Value?.Equals(false) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.Left.GetLocation().SourceSpan.Start,
                                binary.OperatorToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }
        }

        private static void AnalyzeAnd(SyntaxNodeAnalysisContext context)
        {
            var binary = context.Node as BinaryExpressionSyntax;

            Optional<object>
                left = context.SemanticModel.GetConstantValue(binary.Left, context.CancellationToken),
                right = context.SemanticModel.GetConstantValue(binary.Right, context.CancellationToken);

            // x && true
            if (right.HasValue && right.Value.Equals(true))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.OperatorToken.GetLocation().SourceSpan.Start,
                                binary.Right.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }

            // true && x
            else if (left.Value?.Equals(true) ?? false)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                binary.Left.GetLocation().SourceSpan.Start,
                                binary.OperatorToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }
        }
    }
}
