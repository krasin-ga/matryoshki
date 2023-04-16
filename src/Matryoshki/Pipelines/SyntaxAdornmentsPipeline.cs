using Matryoshki.Models;
using Matryoshki.SyntaxRewriters;
using Matryoshki.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Pipelines;

/// <summary>
/// Scans for adornment class declarations
/// </summary>
internal class SyntaxAdornmentsPipeline
{
    public IncrementalValuesProvider<AdornmentMetadata> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                          (node, _) => node.IsAdornmentClassDeclaration(),
                          static (ctx, _) => new AdornmentSyntaxWithSemantics(
                              ClassSyntax: (ClassDeclarationSyntax)ctx.Node,
                              DeclaredSymbol: ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node),
                              SemanticModel: ctx.SemanticModel)
                      ).Where(s => s.DeclaredSymbol is { })
                      .Combine(context.CompilationProvider)
                      .Select((c, ct) => RewriteAndCreateMetadata(c.Left, c.Right, ct));
    }

    private AdornmentMetadata RewriteAndCreateMetadata(
        AdornmentSyntaxWithSemantics input,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var testRewriter = new AdornmentRewriter(input.SemanticModel,
                                                 input.ClassSyntax,
                                                 cancellationToken);

        var processedSyntaxTree = testRewriter.Visit(input.ClassSyntax.SyntaxTree.GetRoot()).SyntaxTree;

        var modifiedClass = processedSyntaxTree.GetRoot()
                                               .DescendantNodes()
                                               .OfType<ClassDeclarationSyntax>()
                                               .Single(n => n.Identifier.Text == input.ClassSyntax.Identifier.Text);

        var newCompilation = CSharpCompilation.Create(
            assemblyName: null,
            new[] { processedSyntaxTree },
            compilation.References);

        var semanticModel = newCompilation.GetSemanticModel(processedSyntaxTree);
        var declaredSymbol = semanticModel.GetDeclaredSymbol(modifiedClass);

        return new AdornmentMetadata(declaredSymbol!,
                                     processedSyntaxTree,
                                     modifiedClass,
                                     semanticModel);
    }

    private record struct AdornmentSyntaxWithSemantics(
        ClassDeclarationSyntax ClassSyntax,
        INamedTypeSymbol? DeclaredSymbol,
        SemanticModel SemanticModel);
}