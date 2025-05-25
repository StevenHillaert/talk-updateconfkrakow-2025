using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Document = Microsoft.CodeAnalysis.Document;

namespace Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeAnalyzerCodeFixProvider)), Shared]
    public class DateTimeAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DateTimeAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);
            var memberAccess = node as MemberAccessExpressionSyntax
                ?? node.FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
            if (memberAccess == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use IDateTimeProvider.UtcNow",
                    createChangedDocument: c => UseDateTimeProviderAsync(context.Document, memberAccess, c),
                    equivalenceKey: "UseIDateTimeProvider"),
                diagnostic);
        }

        private async Task<Document> UseDateTimeProviderAsync(Document document, MemberAccessExpressionSyntax dateTimeAccess, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Find the containing class
            var classDecl = dateTimeAccess.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return document;

            // 1. Replace DateTime.Now/DateTime.UtcNow with _dateTimeProvider.UtcNow
            var newAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("_dateTimeProvider"),
                SyntaxFactory.IdentifierName("UtcNow"));
            editor.ReplaceNode(dateTimeAccess, newAccess);

            // 2. Add private readonly IDateTimeProvider _dateTimeProvider; if not present
            var fieldExists = classDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .Any(f =>
                    f.Declaration.Type is IdentifierNameSyntax id &&
                    id.Identifier.Text == "IDateTimeProvider" &&
                    f.Declaration.Variables.Any(v => v.Identifier.Text == "_dateTimeProvider"));

            if (!fieldExists)
            {
                var field = SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.IdentifierName("IDateTimeProvider"))
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator("_dateTimeProvider"))))
                    .WithModifiers(SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                        SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));

                editor.InsertMembers(classDecl, 0, new[] { field });
            }

            // 3. Add IDateTimeProvider parameter to constructor and assign to field if not present
            var ctor = classDecl.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            if (ctor == null)
            {
                // No constructor: create one
                var newCtor = SyntaxFactory.ConstructorDeclaration(classDecl.Identifier)
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(
                        SyntaxFactory.ParameterList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("dateTimeProvider"))
                                    .WithType(SyntaxFactory.IdentifierName("IDateTimeProvider")))))
                    .WithBody(
                        SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("_dateTimeProvider"),
                                    SyntaxFactory.IdentifierName("dateTimeProvider")))));

                editor.InsertMembers(classDecl, 0, new[] { newCtor });
            }
            else
            {
                // Constructor exists: add parameter and assignment if needed
                var hasParam = ctor.ParameterList.Parameters.Any(p =>
                    p.Type is IdentifierNameSyntax id && id.Identifier.Text == "IDateTimeProvider");

                if (!hasParam)
                {
                    // Add parameter
                    var newParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("dateTimeProvider"))
                        .WithType(SyntaxFactory.IdentifierName("IDateTimeProvider"));
                    var newParamList = ctor.ParameterList.AddParameters(newParam);

                    // Add assignment to body
                    var assignment = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName("_dateTimeProvider"),
                            SyntaxFactory.IdentifierName("dateTimeProvider")));

                    var newBody = ctor.Body.AddStatements(assignment);

                    var newCtor = ctor.WithParameterList(newParamList)
                                      .WithBody(newBody);

                    editor.ReplaceNode(ctor, newCtor);
                }
            }

            return editor.GetChangedDocument();
        }
    }
}