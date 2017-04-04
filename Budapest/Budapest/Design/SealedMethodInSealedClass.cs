using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SealedMethodInSealedClass : DiagnosticAnalyzer
    {
        private const string Category = "Design";
        public const string DiagnosticId = nameof(SealedMethodInSealedClass);

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

            if (!syntax.Modifiers.Any(SyntaxKind.SealedKeyword))
                return;

            if (!syntax.Parent.IsKind(SyntaxKind.ClassDeclaration))
                return;

            var typeInfo = context.SemanticModel.GetDeclaredSymbol(syntax.Parent as ClassDeclarationSyntax, context.CancellationToken);

            if (typeInfo.IsSealed)
                context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Modifiers.Single(m => m.IsKind(SyntaxKind.SealedKeyword)).GetLocation()));
        }
    }
}

