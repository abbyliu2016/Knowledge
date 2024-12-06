using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace TestCodeFix_DateTimeNow
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestCodeFix_DateTimeNowCodeFixProvider)), Shared]
    public class TestCodeFix_DateTimeNowCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("AIB0003");

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Document.SourceCodeKind == SourceCodeKind.Script)
            {
                return Task.CompletedTask;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use DateTime.UtcNow",
                    c => this.FixAsync(context.Document, context.Span, c),
                    nameof(TestCodeFix_DateTimeNowCodeFixProvider)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        private async Task<Document> FixAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(document);

            SyntaxNode oldSyntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode oldNode = oldSyntaxRoot.FindNode(span, false, true);

            Compilation compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            ITypeSymbol dateTimeSymbol = compilation.GetSpecialType(SpecialType.System_DateTime);

            if (dateTimeSymbol is null)
            {
                return document;
            }

            SyntaxNode newNode = syntaxGenerator.MemberAccessExpression(
                syntaxGenerator.TypeExpression(dateTimeSymbol),
                "UtcNow")
             .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.SpecialTypeAnnotation, Formatter.Annotation);

            SyntaxNode newSyntaxRoot = syntaxGenerator.ReplaceNode(
                oldSyntaxRoot,
                oldNode,
                newNode);

            return document.WithSyntaxRoot(newSyntaxRoot);

        }
    }
}
