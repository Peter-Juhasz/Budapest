﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PartialClassWithOnlyOneDeclaration : DiagnosticAnalyzer
    {
        private const string Category = "Design";
        public const string DiagnosticId = nameof(PartialClassWithOnlyOneDeclaration);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as ClassDeclarationSyntax;

            if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                return;
            
            if (context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken).DeclaringSyntaxReferences.Length == 1)
                context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.Modifiers.Single(m => m.IsKind(SyntaxKind.PartialKeyword)).GetLocation()));
        }
    }
}

