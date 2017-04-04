using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryBraces : DiagnosticAnalyzer
    {
        private const string Category = "Style";
        public const string DiagnosticId = nameof(UnnecessaryBraces);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ElseClause);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.UsingStatement);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.LockStatement);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.FixedStatement);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            BlockSyntax block =
                (context.Node as IfStatementSyntax)?.Statement as BlockSyntax ??
                (context.Node as ElseClauseSyntax)?.Statement as BlockSyntax ??
                (context.Node as ForStatementSyntax)?.Statement as BlockSyntax ??
                (context.Node as ForEachStatementSyntax)?.Statement as BlockSyntax ??
                (context.Node as WhileStatementSyntax)?.Statement as BlockSyntax ??
                (context.Node as UsingStatementSyntax)?.Statement as BlockSyntax ??
                (context.Node as LockStatementSyntax)?.Statement as BlockSyntax ??
                (context.Node as FixedStatementSyntax)?.Statement as BlockSyntax
            ;

            if (block?.Statements.Count == 1 && ShouldHide(block.Statements.Single()))
            {
                if (!block.OpenBraceToken.IsMissing)
                    context.ReportDiagnostic(Diagnostic.Create(Rule, block.OpenBraceToken.GetLocation()));

                if (!block.CloseBraceToken.IsMissing)
                    context.ReportDiagnostic(Diagnostic.Create(Rule, block.CloseBraceToken.GetLocation()));
            }
        }

        private static bool ShouldHide(StatementSyntax statement)
        {
            bool isSimpleStatement = new[] {
                
                // if (condition)
                //     throw exception;
                SyntaxKind.ThrowStatement,

                // if (condition)
                //     return expression;
                SyntaxKind.ReturnKeyword,

                // foreach (var e in ...)
                //     yield return e;
                SyntaxKind.YieldReturnStatement,

            }
                .Any(k => statement.IsKind(k));

            var location = statement.GetLocation().GetLineSpan();
            bool isSingeLine = location.StartLinePosition.Line == location.EndLinePosition.Line;

            return isSimpleStatement && isSingeLine;
        }
    }
}
