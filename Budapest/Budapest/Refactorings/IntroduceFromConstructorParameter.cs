using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Budapest.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(IntroduceFromConstructorParameter)), Shared]
    internal class IntroduceFromConstructorParameter : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            byte[] delay = new byte[1];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            // find parameter
            var parameter = root.FindNode(context.Span).FirstAncestorOrSelf<ParameterSyntax>();
            if (parameter == null)
                return;

            var parameterName = parameter.Identifier.ValueText;

            // find constructor
            var constructor = parameter.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (constructor == null)
                return;

            // find containing class
            var classSyntax = constructor.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classSyntax == null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var memberNames = semanticModel.GetDeclaredSymbol(classSyntax).MemberNames;

            // check whether there is already a field named like the parameter
            var fieldName = $"_{parameterName}";
            if (!memberNames.Contains(fieldName))
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        $"Introduce and initialize read-only field '{fieldName}'",
                        ct => IntroduceField(context, parameter, parameterName, fieldName, ct)
                    )
                );
            }

            // check whether there is already a field named like the parameter
            var propertyName = Char.ToUpper(parameterName[0]) + parameterName.Substring(1);
            if (!memberNames.Contains(fieldName))
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        $"Introduce and initialize property '{propertyName}'",
                        ct => IntroduceProperty(context, parameter, parameterName, propertyName, ct)
                    )
                );
            }
        }

        private async Task<Document> IntroduceField(
            CodeRefactoringContext context, ParameterSyntax parameter,
            string paramName, string fieldName,
            CancellationToken cancellationToken
        )
        {
            // add assignment to constructor
            var oldConstructor = parameter.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            var newConstructor = oldConstructor.WithBody(
                oldConstructor.Body.AddStatements(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName(
                                    SyntaxFactory.Identifier(fieldName)
                                        .WithAdditionalAnnotations(RenameAnnotation.Create())
                                )
                            )
                                .WithAdditionalAnnotations(Simplifier.Annotation),
                            SyntaxFactory.IdentifierName(paramName)
                        )
                    )
                )
            );

            var oldClass = parameter.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var oldClassWithNewCtor = oldClass.ReplaceNode(oldConstructor, newConstructor);

            // add field declaration
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                ),
                SyntaxFactory.VariableDeclaration(
                    parameter.Type,
                    SyntaxFactory.SeparatedList(new[] {
                        SyntaxFactory.VariableDeclarator(fieldName)
                    })
                )
            );
            
            var newClass = oldClassWithNewCtor.WithMembers(
                oldClassWithNewCtor.Members.Insert(
                    oldClassWithNewCtor.Members.LastIndexOf(m => m.IsKind(SyntaxKind.ConstructorDeclaration)) + 1,
                    fieldDeclaration
                )
            )
                .WithAdditionalAnnotations(Formatter.Annotation);

            // replace root
            var oldRoot = await context.Document
                .GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);

            var newRoot = oldRoot.ReplaceNode(oldClass, newClass);

            return context.Document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> IntroduceProperty(
            CodeRefactoringContext context, ParameterSyntax parameter,
            string paramName, string fieldName,
            CancellationToken cancellationToken
        )
        {
            // add assignment to constructor
            var oldConstructor = parameter.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            var newConstructor = oldConstructor.WithBody(
                oldConstructor.Body.AddStatements(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName(
                                    SyntaxFactory.Identifier(fieldName)
                                        .WithAdditionalAnnotations(RenameAnnotation.Create())
                                )
                            ),
                            SyntaxFactory.IdentifierName(paramName)
                        )
                    )
                )
            );

            var oldClass = parameter.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var oldClassWithNewCtor = oldClass.ReplaceNode(oldConstructor, newConstructor);

            // add field declaration
            var fieldDeclaration = SyntaxFactory.PropertyDeclaration(
                default(SyntaxList<AttributeListSyntax>),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                ),
                parameter.Type,
                null,
                SyntaxFactory.Identifier(fieldName),
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List(
                        new []
                        {
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration,
                                default(SyntaxList<AttributeListSyntax>),
                                default(SyntaxTokenList),
                                SyntaxFactory.Token(SyntaxKind.GetKeyword),
                                null,
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            ),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration,
                                default(SyntaxList<AttributeListSyntax>),
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                                ),
                                SyntaxFactory.Token(SyntaxKind.SetKeyword),
                                null,
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            ),
                        }
                    )
                )
            );

            var published = oldClassWithNewCtor.Members.Publish();

            var newClass = oldClassWithNewCtor.WithMembers(
                oldClassWithNewCtor.Members.Insert(
                    published.TakeWhile(m => m.IsKind(SyntaxKind.ConstructorDeclaration))
                        .Concat(published.TakeWhile(m => m.IsKind(SyntaxKind.FieldDeclaration)))
                        .Concat(published.TakeWhile(m => m.IsKind(SyntaxKind.PropertyDeclaration)))
                        .Count(),
                    fieldDeclaration
                )
            )
                .WithAdditionalAnnotations(Formatter.Annotation);

            // replace root
            var oldRoot = await context.Document
                .GetSyntaxRootAsync(cancellationToken)
                .ConfigureAwait(false);

            var newRoot = oldRoot.ReplaceNode(oldClass, newClass);

            return context.Document.WithSyntaxRoot(newRoot);
        }
    }
}
