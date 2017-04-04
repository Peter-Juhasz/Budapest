using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryIfClause : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryIfClause);

        private static readonly string Title = "Where clause has no effect";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeWhereClause, SyntaxKind.IfStatement);
        }

        private static void AnalyzeWhereClause(SyntaxNodeAnalysisContext context)
        {
            IfStatementSyntax syntax = context.Node as IfStatementSyntax;

            var value = context.SemanticModel.GetConstantValue(syntax.Condition, context.CancellationToken);
            
            if (value.HasValue && value.Value.Equals(false) &&
                !syntax.Else.IsMissing)
            {
                if (syntax.Else.Statement.IsKind(SyntaxKind.Block))
                {
                    if (syntax.Else.Statement.DescendantNodes().Any(n => n.IsKind(SyntaxKind.VariableDeclaration)))
                        context.MarkAsUnnecessary(Rule, syntax.GetLocation(), syntax.Else.ElseKeyword.GetLocation());
                    else
                    {
                        var block = syntax.Else.Statement as BlockSyntax;
                        context.MarkAsUnnecessary(Rule, syntax.GetLocation(), block.OpenBraceToken.GetLocation());
                        context.ReportDiagnostic(Diagnostic.Create(Rule, block.CloseBraceToken.GetLocation()));
                    }
                }
                else
                    context.MarkAsUnnecessary(Rule, syntax.GetLocation(), syntax.Else.ElseKeyword.GetLocation());
            }
        }
    }
}
