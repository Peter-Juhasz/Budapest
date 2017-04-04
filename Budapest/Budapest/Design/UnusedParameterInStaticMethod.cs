using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedParameterInStaticMethod : DiagnosticAnalyzer
    {
        private const string Category = "Design";
        public const string DiagnosticId = nameof(UnusedParameterInStaticMethod);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as MethodDeclarationSyntax;

            if (!syntax.Modifiers.Any(SyntaxKind.StaticKeyword))
                return;

            if (!syntax.Parent.IsKind(SyntaxKind.ClassDeclaration))
                return;

            if (!syntax.ParameterList.Parameters.Any())
                return;

            var analysis = context.SemanticModel.AnalyzeDataFlow(syntax.Body);

            for (int i = 0; i < syntax.ParameterList.Parameters.Count; i++)
            {
                var parameter = syntax.ParameterList.Parameters[i];
                var symbol = context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken);

                if (!analysis.ReadInside.Contains(symbol))
                {
                    if (syntax.ParameterList.Parameters.SeparatorCount > i)
                        context.MarkAsUnnecessary(Rule, parameter.GetLocation(), syntax.ParameterList.Parameters.GetSeparator(i).GetLocation());
                    else if (i > 0)
                        context.MarkAsUnnecessary(Rule, syntax.ParameterList.Parameters.GetSeparator(i - 1).GetLocation(), parameter.GetLocation());
                    else
                        context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.GetLocation()));
                }
            }
        }
    }
}

