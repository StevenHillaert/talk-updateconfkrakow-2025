using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "API0001";
        private const string Category = "API";
        
        // New rule for DateTime.Now and DateTime.UtcNow
        private static readonly DiagnosticDescriptor DateTimeNowRule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Do not use DateTime.Now or DateTime.UtcNow",
            messageFormat: "Do not use '{0}'. Use an injected clock or abstraction instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Direct usage of DateTime.Now or DateTime.UtcNow is prohibited. Using these api's creates flaky tests."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DateTimeNowRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // Check if the member is Now or UtcNow
            string memberName = memberAccess.Name.Identifier.Text;
            if (memberName != "Now" && memberName != "UtcNow")
            {
                return;
            }

            // Check if the expression is System.DateTime or DateTime
            SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken);

            if (!(symbolInfo.Symbol is INamedTypeSymbol typeSymbol))
            {
                // Try to get the type from the expression
                TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
                typeSymbol = typeInfo.Type as INamedTypeSymbol;
            }

            if (typeSymbol == null)
            {
                return;
            }

            if (typeSymbol.ToString() == "System.DateTime")
            {
                var diagnostic = Diagnostic.Create(DateTimeNowRule, memberAccess.GetLocation(), $"DateTime.{memberName}");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
