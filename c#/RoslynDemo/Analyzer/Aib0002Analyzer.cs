namespace Analyzer
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic, LanguageNames.FSharp)]
    [Shared]
    internal sealed class Aib0002Analyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            new DiagnosticDescriptor(
                "AIB0002",
                "Namespace should start with Microsoft",

                @"namespace '{0}' should begin with Microsoft",
                "AIBuilder.Design",
                DiagnosticSeverity.Warning,
                true));

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(this.Analyze, SymbolKind.Namespace);
        }

        private void Analyze(SymbolAnalysisContext context)
        {
            var namespaceSymbol = (INamespaceSymbol)context.Symbol;

            // Just perform checks on the top level namespaces i.e. namespaces under global namespace.
            if (namespaceSymbol.ContainingNamespace?.IsGlobalNamespace is true)
            {
                if (!namespaceSymbol.Name.Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        this.SupportedDiagnostics[0],
                        namespaceSymbol.Locations[0],
                        namespaceSymbol.Name));
                }
            }
        }
    }
}
