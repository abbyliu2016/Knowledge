namespace Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Operations;
    using Microsoft.CodeAnalysis.Simplification;
    using Microsoft.CodeAnalysis.Text;

    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, LanguageNames.FSharp)]
    [Shared]
    internal sealed class Aib0003CodeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("AIB0003");

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Document.SourceCodeKind == SourceCodeKind.Script)
            {
                return Task.CompletedTask;
            }

            context.RegisterCodeFix(
                CodeAction.Create("Use DateTime.UtcNow", c => this.FixAsync(context.Document, context.Span, c), nameof(Aib0003CodeFixer)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private async Task<Document> FixAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            SyntaxNode oldSyntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Compilation compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode oldNode = oldSyntaxRoot.FindNode(span, false, true);
            ITypeSymbol dateTimeSymbol = compilation.GetSpecialType(SpecialType.System_DateTime);

            if (dateTimeSymbol is null)
            {
                return document;
            }

            SyntaxNode newNode = syntaxGenerator
                .MemberAccessExpression(
                    syntaxGenerator.TypeExpression(dateTimeSymbol),
                    "UtcNow");
                //.WithAdditionalAnnotations(
                //    /* Simplifier.Annotation, Simplifier.SpecialTypeAnnotation, */
                //    Formatter.Annotation);

            SyntaxNode newSyntaxRoot = syntaxGenerator.ReplaceNode(
                oldSyntaxRoot,
                oldNode,
                newNode);

            return document.WithSyntaxRoot(newSyntaxRoot);
        }
    }
}
