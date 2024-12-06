using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;

namespace TestAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestAnalyzerCodeFixProvider)), Shared]
    public class TestAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use Array.Empty<T>()";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(TestAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);

            if (nodeToFix == null)
            {
                return;
            }

            var codeAction = CodeAction.Create(title, ct => CovertToArrayEmptyAsync(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> CovertToArrayEmptyAsync(
            Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            //Get the generator that will generate the SyntaxNode for the expected language.
            var generator = editor.Generator;

            //Get the type of the elements of the array (new int[] => int)
            var elementType = GetArrayElementType(nodeToFix, semanticModel, cancellationToken);

            if (elementType == null)
            {
                return document;
            }

            //Generate the new node "Array.Empty<T>()"(replace T with elementType)
            var arrayTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Array");
        //
        //    var typeExpression = generator.TypeExpression(arrayTypeSymbol);
        //
        //    var operation = semanticModel.GetOperation(nodeToFix);
        //
        //    var typesymbol = (IArrayTypeSymbol)operation.Type;
        //
        //    var elementType = typesymbol.ElementType;
        //
        //    var genericExpression = generator.GenericName("Empty", elementType);

            var arrayEmptyName = generator.MemberAccessExpression(
                generator.TypeExpression(arrayTypeSymbol),
                generator.GenericName("Empty", elementType));

            var arrayEmptyInvocation = generator.InvocationExpression(arrayEmptyName);

            editor.ReplaceNode(nodeToFix, arrayEmptyInvocation);

            return editor.GetChangedDocument();
        }

        private static ITypeSymbol GetArrayElementType(
            SyntaxNode arrayCreationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(arrayCreationExpression, cancellationToken);

            var arrayType = (IArrayTypeSymbol)(typeInfo.Type ?? typeInfo.ConvertedType);

            return arrayType?.ElementType;
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }

    internal class async
    {
    }
}
