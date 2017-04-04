using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ForWithConstantFalseCondition : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(ForWithConstantFalseCondition);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForStatement);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as ForStatementSyntax;

            var value = context.SemanticModel.GetConstantValue(syntax.Condition, context.CancellationToken);

            if (value.HasValue && value.Value.Equals(false))
            {
                context.MarkAsUnnecessary(Rule, syntax.ForKeyword, syntax.OpenParenToken);
                //context.MarkAsUnnecessary(Rule, syntax.Initializers.First().);
                context.MarkAsUnnecessary(Rule, syntax.Condition.GetLocation(), syntax.Statement.GetLocation());
            }
        }
    }
}

