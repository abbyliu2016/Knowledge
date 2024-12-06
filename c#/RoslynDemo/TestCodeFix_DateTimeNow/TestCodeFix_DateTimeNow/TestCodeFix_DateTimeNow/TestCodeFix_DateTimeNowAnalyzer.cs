using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TestCodeFix_DateTimeNow
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestCodeFix_DateTimeNowAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
          new DiagnosticDescriptor(
              "AIB0003",
              "Do not call DateTime.Now",
              "Code using DateTime.Now may have globalization issues, use DateTime.UtcNow instead",
              "AIBuilder.Design",
              DiagnosticSeverity.Warning,
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

            // Get property symbol for System.DateTim.
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

            if (operation.Property.Equals(nowSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(this.SupportedDiagnostics[0], operation.Syntax.GetLocation()));
            }

        }
    }
}
