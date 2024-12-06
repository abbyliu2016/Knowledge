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

namespace TestAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TestAnalyzer_DiagnosticEmptyArray";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Fixing Array";

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
        DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "");

       // private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
       //     DiagnosticId,
       //     Title,
       //     MessageFormat, 
       //     Category, 
       //     DiagnosticSeverity.Warning,
       //     isEnabledByDefault: true,
       //     description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(s_rule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            // context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterOperationAction(AnalyzeArrayCreationOperation, OperationKind.ArrayCreation);
        }

        private void AnalyzeArrayCreationOperation(OperationAnalysisContext context)
        {
            var operation = (IArrayCreationOperation)context.Operation;
            if (IsZerorLengthArrayCreation(operation))
            {
                var diagnostic = Diagnostic.Create(s_rule, operation.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsZerorLengthArrayCreation(IArrayCreationOperation operation)
        {
            if (operation.DimensionSizes.Length != 1)
            {
                return false;
            }

            var dimensionSize = operation.DimensionSizes[0].ConstantValue;

            return dimensionSize.HasValue && dimensionSize.Value is 0;
        }
        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // Find just those named type symbols with names containing lowercase letters.
            if (namedTypeSymbol.Name.ToCharArray().Any(char.IsLower))
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(s_rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
