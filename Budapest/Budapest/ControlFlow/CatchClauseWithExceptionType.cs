using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnnecessaryCatchType : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryCatchType);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CatchClause);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as CatchClauseSyntax;

            if (context.SemanticModel.GetTypeInfo(syntax.Declaration.Type, context.CancellationToken).Type.Equals(context.SemanticModel.Compilation.GetTypeByMetadataName("System.Exception")))
            {
                if (syntax.Declaration.Identifier.IsKind(SyntaxKind.None))
                {
                    context.MarkAsUnnecessary(Rule, syntax.Declaration.OpenParenToken, syntax.Declaration.CloseParenToken);
                }
                else
                {
                    var analysis = context.SemanticModel.AnalyzeDataFlow(syntax.Block);
                    var symbol = context.SemanticModel.GetDeclaredSymbol(syntax.Declaration, context.CancellationToken);

                    if (!analysis.ReadInside.Contains(symbol))
                        context.MarkAsUnnecessary(Rule, syntax.Declaration.OpenParenToken, syntax.Declaration.CloseParenToken);
                }
            }
        }
    }
}

