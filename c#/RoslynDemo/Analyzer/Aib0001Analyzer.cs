namespace Analyzer
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Editing;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Shared]
    internal sealed class Aib0001Analyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            new DiagnosticDescriptor(
                "AIB0001",
                "Do not use arrays as return values",
                "Code return arrays may expose unnecessary data structures to the caller",
                "AIBuilder.Design",
                DiagnosticSeverity.Warning,
                true));

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(this.Analyze, SyntaxKind.MethodDeclaration);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = (MethodDeclarationSyntax)context.Node;
            var returnType = node.ReturnType;

            if (node.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                switch (returnType)
                {
                    case ArrayTypeSyntax _:
                    case NullableTypeSyntax nullableType when nullableType.ElementType is ArrayTypeSyntax:
                        context.ReportDiagnostic(Diagnostic.Create(this.SupportedDiagnostics[0], returnType.GetLocation()));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
