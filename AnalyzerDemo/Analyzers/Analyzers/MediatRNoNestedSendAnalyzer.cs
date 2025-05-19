using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MediatRNoNestedSendAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "API0003";
        private const string Category = "API";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Do not send MediatR requests from within a handler",
            messageFormat: "Handler '{0}' should not send another MediatR request using ISender.Send or ISender.SendAsync",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "MediatR handlers should not send other MediatR requests to avoid nested dispatching. Publishing notifications is allowed."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var classDeclaration = methodDeclaration.Parent as ClassDeclarationSyntax;
            if (classDeclaration == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
            if (classSymbol == null)
            {
                return;
            }

            // Check if the class implements any MediatR handler interface
            bool isHandler = classSymbol.AllInterfaces.Any(i =>
                i.Name.StartsWith("IRequestHandler") || i.Name.StartsWith("INotificationHandler"));

            if (!isHandler)
            {
                return;
            }

            // Find all invocation expressions in the method
            var invocations = methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in invocations)
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess == null)
                    continue;

                var methodName = memberAccess.Name.Identifier.Text;
                if (methodName != "Send" && methodName != "SendAsync")
                    continue; // Only flag Send/SendAsync, not Publish

                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken);
                var typeSymbol = symbolInfo.Symbol?.GetType() as ITypeSymbol
                    ?? semanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
                if (typeSymbol == null)
                    continue;

                // Check if the type is ISender (from MediatR)
                if (typeSymbol.ToString() == "MediatR.ISender")
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        memberAccess.Name.GetLocation(),
                        classSymbol.Name
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
