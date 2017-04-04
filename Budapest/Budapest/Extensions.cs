using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Linq;

namespace Budapest
{
    internal static class Extensions
    {
        public static void MarkAsUnnecessary(
            this SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            Location start,
            Location end
        )
        {
            context.ReportDiagnostic(Diagnostic.Create(rule,
                Location.Create(context.Node.SyntaxTree,
                    TextSpan.FromBounds(
                        start.SourceSpan.Start,
                        end.SourceSpan.End
                    )
                )
            ));
        }
        
        public static void MarkAsUnnecessary(
            this SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            SyntaxToken start,
            SyntaxToken end
        )
        {
            context.MarkAsUnnecessary(rule, start.GetLocation(), end.GetLocation());
        }

        public static void MarkAsUnnecessary(
            this SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            SyntaxNode start,
            SyntaxNode end
        )
        {
            context.MarkAsUnnecessary(rule, start.GetLocation(), end.GetLocation());
        }


        public static bool IsEmpty(this StatementSyntax syntax)
        {
            if (syntax == null)
                return true;

            if (syntax.IsKind(SyntaxKind.EmptyStatement))
                return true;

            if (syntax.IsKind(SyntaxKind.Block))
                return (syntax as BlockSyntax).Statements.All(IsEmpty);
            
            return false;
        }
    }
}
