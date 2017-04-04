using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConditionalExpressionWithMatchingCases : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(ConditionalExpressionWithMatchingCases);

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
            var syntax = context.Node as ConditionalExpressionSyntax;
            
            // x ? y : y
            if (syntax.WhenTrue.IsEquivalentTo(syntax.WhenFalse))
            {
                context.MarkAsUnnecessary(Rule, syntax.Condition.GetLocation(), syntax.QuestionToken.GetLocation());
                context.MarkAsUnnecessary(Rule, syntax.ColonToken.GetLocation(), syntax.WhenFalse.GetLocation());
            }
        }
    }
}
