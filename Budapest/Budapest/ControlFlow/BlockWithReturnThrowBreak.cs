using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BlockStatementWithReturnThrowBreak : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(BlockStatementWithReturnThrowBreak);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Block);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as BlockSyntax;
            
            int count = syntax.Statements.TakeWhile(s =>
                !s.IsKind(SyntaxKind.ReturnStatement) &&
                !s.IsKind(SyntaxKind.ThrowStatement) &&
                !s.IsKind(SyntaxKind.BreakStatement)
            ).Count();

            if (count < syntax.Statements.Count - 1)
            {
                context.MarkAsUnnecessary(Rule, syntax.Statements[count + 1], syntax.Statements.Last());
            }
        }
    }
}

