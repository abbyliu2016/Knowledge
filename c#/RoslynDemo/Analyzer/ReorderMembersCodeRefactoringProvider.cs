namespace Analyzer
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.Text;
    using Microsoft.CodeAnalysis.Editing;
    using System.Composition;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, LanguageNames.FSharp)]
    [Shared]
    internal sealed class ReorderMembersCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly DeclarationKind[] MemberKinds = new DeclarationKind[]
        {
            DeclarationKind.Field,
            DeclarationKind.Constructor,
            DeclarationKind.Destructor,
            DeclarationKind.Delegate,
            DeclarationKind.Event,
            DeclarationKind.Enum,
            DeclarationKind.Interface,
            DeclarationKind.Property,
            DeclarationKind.Indexer,
            DeclarationKind.Method,
            DeclarationKind.Struct,
            DeclarationKind.Class
        };

        private static readonly Accessibility[] MemberAccessibilities = new Accessibility[]
        {
            Accessibility.Public,
            Accessibility.Internal,
            Accessibility.ProtectedAndInternal,
            Accessibility.Protected,
            Accessibility.ProtectedOrInternal,
            Accessibility.Private,
            Accessibility.NotApplicable
        };

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            Document document = context.Document;
            SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            CancellationToken cancellationToken = context.CancellationToken;
            TextSpan span = context.Span;
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode node = root.FindNode(span);
            DeclarationKind kind = syntaxGenerator.GetDeclarationKind(node);

            if (kind == DeclarationKind.Class || kind == DeclarationKind.Struct || kind == DeclarationKind.Interface)
            {
                context.RegisterRefactoring(CodeAction.Create("Reorder members", c => this.ReorderMembersCoreAsync(document, node, c)));
            }
        }

        private async Task<Document> ReorderMembersCoreAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            DocumentEditor documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            SyntaxGenerator syntaxGenerator = documentEditor.Generator;

            // Member orders: http://stackoverflow.com/questions/150479/order-of-items-in-classes-fields-properties-constructors-methods
            IEnumerable<SyntaxNode> members = syntaxGenerator.GetMembers(node);
            List<SyntaxNode> membersInNewOrder = new List<SyntaxNode>();

            foreach (DeclarationKind kind in MemberKinds)
            {
                IEnumerable<SyntaxNode> membersOfKind = members.Where(m => syntaxGenerator.GetDeclarationKind(m) == kind);

                foreach (Accessibility accessibility in MemberAccessibilities)
                {
                    IEnumerable<SyntaxNode> membersOfAccessibility = membersOfKind.Where(m => syntaxGenerator.GetAccessibility(m) == accessibility);

                    IEnumerable<SyntaxNode> staticMemberOfAccessibility = membersOfAccessibility.Where(m => syntaxGenerator.GetModifiers(m).IsStatic);
                    IEnumerable<SyntaxNode> nonStaticMemberOfAccessibility = membersOfAccessibility.Except(staticMemberOfAccessibility);

                    membersInNewOrder.AddRange(staticMemberOfAccessibility.OrderBy(m => syntaxGenerator.GetName(m)));
                    membersInNewOrder.AddRange(nonStaticMemberOfAccessibility.OrderBy(m => syntaxGenerator.GetName(m)));
                }
            }

            foreach (SyntaxNode member in members)
            {
                documentEditor.RemoveNode(member);
            }

            foreach (SyntaxNode member in membersInNewOrder)
            {
                // TODO: Add empty line trivia before/after the member.
                SyntaxTriviaList leadingTrivia = member.GetLeadingTrivia();
                documentEditor.AddMember(node, member);
            }

            return documentEditor.GetChangedDocument();
        }
    }
}