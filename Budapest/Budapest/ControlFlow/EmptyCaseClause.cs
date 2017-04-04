using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EmptyCaseClause : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(EmptyCaseClause);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SwitchSection);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as SwitchSectionSyntax;
            
            if (syntax.Statements.SkipWhile(s => s.IsEmpty()).FirstOrDefault()?.IsKind(SyntaxKind.BreakStatement) ?? false)
            {
                var @default = (syntax.Parent as SwitchStatementSyntax).Sections.LastOrDefault(s => s.Labels.Any(l => l.IsKind(SyntaxKind.DefaultSwitchLabel)));

                if (@default == null ||
                    @default == syntax ||
                    (@default.Statements.FirstOrDefault()?.IsKind(SyntaxKind.BreakStatement) ?? false))
                    context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation()));
            }
        }
    }
}

