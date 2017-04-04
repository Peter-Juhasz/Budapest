using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System.Composition;
using System.Threading.Tasks;
using System.Linq;

namespace Budapest.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddNullCheckToParameter)), Shared]
    internal class AddNullCheckToParameter : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            // find parameter
            var parameter = root.FindNode(context.Span).FirstAncestorOrSelf<ParameterSyntax>();
            if (parameter == null)
                return;

            var parameterName = parameter.Identifier.ValueText;

            // find containing method
            var method = parameter.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
            if (method == null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            // check whether parameter type is a reference type
            if (!semanticModel.GetDeclaredSymbol(parameter).Type.IsReferenceType)
                return;
            
            // check whether the method can be invoked publicly
            if (semanticModel.GetDeclaredSymbol(method).DeclaredAccessibility != Accessibility.Public)
                return;

            if (method is ConstructorDeclarationSyntax)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        $"Check parameter '{parameterName}' for null",
                        ct => IntroduceField(context, method as ConstructorDeclarationSyntax, parameterName)
                    )
                );
            }
            else if (method is MethodDeclarationSyntax)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        $"Check parameter '{parameterName}' for null",
                        ct => IntroduceField(context, method as MethodDeclarationSyntax, parameterName)
                    )
                );
            }
        }

        private async Task<Document> IntroduceField(CodeRefactoringContext context, ConstructorDeclarationSyntax method, string parameterName)
        {
            // add assignment to constructor
            var newMethod = method.WithBody(
                method.Body.WithStatements(
                    method.Body.Statements.Insert(FindInsertionIndex(method.Body.Statements),
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                                SyntaxFactory.IdentifierName(parameterName),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                            ),
                            SyntaxFactory.ThrowStatement(
                                SyntaxFactory.Token(SyntaxKind.ThrowKeyword),
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword),
                                    SyntaxFactory.ParseTypeName("System.ArgumentNullException")
                                        .WithAdditionalAnnotations(Simplifier.Annotation),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.IdentifierName("nameof"),
                                                    SyntaxFactory.ArgumentList(
                                                        SyntaxFactory.SeparatedList(new[]
                                                        {
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName(parameterName)
                                                            )
                                                        })
                                                    )
                                                )
                                            )
                                        })
                                    ),
                                    null
                                ),
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            )
                                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                        )
                    )
                )
            );
            
            // replace root
            var oldRoot = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var newRoot = oldRoot.ReplaceNode(method, newMethod);

            return context.Document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> IntroduceField(CodeRefactoringContext context, MethodDeclarationSyntax method, string parameterName)
        {
            // add assignment to constructor
            var newMethod = method.WithBody(
                method.Body.WithStatements(
                    method.Body.Statements.Insert(FindInsertionIndex(method.Body.Statements),
                        SyntaxFactory.IfStatement(
                            SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                                SyntaxFactory.IdentifierName(parameterName),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                            ),
                            SyntaxFactory.ThrowStatement(
                                SyntaxFactory.Token(SyntaxKind.ThrowKeyword),
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword),
                                    SyntaxFactory.ParseTypeName("System.ArgumentNullException")
                                        .WithAdditionalAnnotations(Simplifier.Annotation),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.IdentifierName("nameof"),
                                                    SyntaxFactory.ArgumentList(
                                                        SyntaxFactory.SeparatedList(new[]
                                                        {
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName(parameterName)
                                                            )
                                                        })
                                                    )
                                                )
                                            )
                                        })
                                    ),
                                    null
                                ),
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            )
                        )
                            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                    )
                )
            );

            // replace root
            var oldRoot = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var newRoot = oldRoot.ReplaceNode(method, newMethod);
            
            return context.Document.WithSyntaxRoot(newRoot);
        }

        private static int FindInsertionIndex(SyntaxList<StatementSyntax> statements)
        {
            return statements
                .TakeWhile(s => s.IsKind(SyntaxKind.IfStatement))
                .Count();
        }
    }
}
