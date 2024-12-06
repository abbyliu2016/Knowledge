namespace Analyzer
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Operations;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic, LanguageNames.FSharp)]
    internal sealed class Aib0003Analyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            new DiagnosticDescriptor(
                "AIB0003",
                "Do not call DateTime.Now",
                "Code using DateTime.Now may have globalization issues, use DateTime.UtcNow instead",
                "AIBuilder.Design",
                DiagnosticSeverity.Error,
                true));

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(this.Analyze, OperationKind.PropertyReference);
        }

        private void Analyze(OperationAnalysisContext context)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;
            var compilation = context.Compilation;

            // Get property symbol for System.DateTime.Now.
            var dateTimeSymbol = compilation.GetSpecialType(SpecialType.System_DateTime);

            // System.DateTime symbol could be null if the mscorlib/netstandard is not imported.
            if (dateTimeSymbol is null)
            {
                return;
            }

            var nowSymbol = dateTimeSymbol.GetMembers(nameof(DateTime.Now)).OfType<IPropertySymbol>().FirstOrDefault();

            if (nowSymbol is null)
            {
                return;
            }

            // Check if the property reference is System.DateTime.Now.
            if (operation.Property.Equals(nowSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    this.SupportedDiagnostics[0],
                    operation.Syntax.GetLocation()));
            }
        }
    }
}
