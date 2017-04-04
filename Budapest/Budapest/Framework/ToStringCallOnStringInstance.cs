using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryToStringCall : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryToStringCall);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax syntax = context.Node as InvocationExpressionSyntax;

            if (syntax.ArgumentList.Arguments.Any())
                return;

            if (!syntax.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                return;
            
            MemberAccessExpressionSyntax access = syntax.Expression as MemberAccessExpressionSyntax;

            if (!access.Name.IsKind(SyntaxKind.IdentifierName))
                return;

            IdentifierNameSyntax identifier = access.Name as IdentifierNameSyntax;

            TypeInfo type = context.SemanticModel.GetTypeInfo(access.Expression, context.CancellationToken);
            
            if (type.Type == context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_String) &&
                identifier.Identifier.ValueText == "ToString")
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        Location.Create(context.Node.SyntaxTree,
                            TextSpan.FromBounds(
                                access.OperatorToken.GetLocation().SourceSpan.Start,
                                syntax.ArgumentList.CloseParenToken.GetLocation().SourceSpan.End
                            )
                        )
                    )
                );
            }
        }
    }
}
