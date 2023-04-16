using System.Collections.Immutable;
using Matryoshki.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Matryoshki.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CallParameterAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Usage";
    private const string Title = "MT1001: Incorrect usage of Call<T> parameter";
    private const string Message = "Do not pass Call<T> without accessing member";

    private static readonly DiagnosticDescriptor Rule = new(
        id: "CallParameterAnalyzer",
        title: Title,
        messageFormat: Message,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(
            compilationStartAnalysisContext =>
            {
                var callType = compilationStartAnalysisContext
                               .Compilation
                               .GetTypeByMetadataName(CallType.GenericTypeName);
                if (callType is { })
                    compilationStartAnalysisContext.RegisterSyntaxNodeAction(
                        analysisContext => AnalyzeExpression(callType, analysisContext),
                        SyntaxKind.IdentifierName);
            });

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    }

    private static void AnalyzeExpression(
        INamedTypeSymbol callTypeSymbol,
        SyntaxNodeAnalysisContext context)
    {
        var identifier = (IdentifierNameSyntax)context.Node;
        var type = context.SemanticModel.GetTypeInfo(identifier).ConvertedType?.OriginalDefinition;

        if (!SymbolEqualityComparer.Default.Equals(type, callTypeSymbol)
            || identifier.Parent is MemberAccessExpressionSyntax)
            return;

        var location = identifier.Parent?.GetLocation() ?? identifier.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location);
        context.ReportDiagnostic(diagnostic);
    }
}