﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Budapest
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnnecessaryCoalesce : DiagnosticAnalyzer
    {
        private const string Category = "Performance";
        public const string DiagnosticId = nameof(UnnecessaryCoalesce);

        private static readonly string Title = "Unnecessary braces";
        private static readonly string MessageFormat = null;
        private static readonly string Description = null;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, isEnabledByDefault: true, description: Description, helpLinkUri: null, customTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.CoalesceExpression);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node as BinaryExpressionSyntax;

            var value = context.SemanticModel.GetConstantValue(syntax.Right, context.CancellationToken);

            if (value.HasValue && value.Value == null)
                context.MarkAsUnnecessary(Rule, syntax.OperatorToken.GetLocation(), syntax.Right.GetLocation());
        }
    }
}

