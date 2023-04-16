using Matryoshki.Models;
using Matryoshki.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Pipelines;

/// <summary>
/// Scans for target types
/// </summary>
internal class MatryoshkaTypesPipeline
{
    public IncrementalValuesProvider<MatryoshkaMetadata> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                          (node, _) => node is InvocationExpressionSyntax
                          {
                              Expression: MemberAccessExpressionSyntax
                              {
                                  Expression: GenericNameSyntax { Identifier.Text: MatryoshkaType.TypeName or MatryoshkaType.Alias },
                                  Name: GenericNameSyntax
                                  {
                                      Identifier.Text: MatryoshkaType.Methods.With or MatryoshkaType.Methods.WithNesting
                                  }
                              }
                          },
                          Transform
                      ).Where(v => v != null)
                      .Select((v, _) => v!.Value);
    }

    private MatryoshkaMetadata? Transform(
        GeneratorSyntaxContext context,
        CancellationToken token)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Expression: GenericNameSyntax { Identifier.Text: MatryoshkaType.TypeName or MatryoshkaType.Alias } mixSyntax,
                Name: GenericNameSyntax
                {
                    Identifier.Text: MatryoshkaType.Methods.With or MatryoshkaType.Methods.WithNesting
                } withSyntax
            })
            return null;

        var targetType = GetFirstTypeArgument(mixSyntax, context.SemanticModel);
        var withType = GetFirstTypeArgument(withSyntax, context.SemanticModel);

        var parentType = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();

        if (withType is null)
            return null;

        //.Name<TypeName>
        var typeNameOption = (invocation.Parent?.DescendantNodes().OfType<GenericNameSyntax>()
                                        .LastOrDefault(
                                            g => g.Identifier.Text is MatryoshkaType.Methods.Name && g.Arity == 1
                                        )?.TypeArgumentList.Arguments.First() as SimpleNameSyntax)
                             ?.Identifier.Text;

        var typeSymbol = parentType is { }
            ? context.SemanticModel.GetDeclaredSymbol(parentType)
            : null;

        var @namespace = typeSymbol?.ContainingNamespace.ToDisplayString();
        var isGlobal = @namespace is null && invocation.FirstAncestorOrSelf<GlobalStatementSyntax>() is { };

        ITypeSymbol? adornmentSymbol = null;
        INamedTypeSymbol? packSymbol = null;

        if (withSyntax.Identifier.Text is MatryoshkaType.Methods.With)
            adornmentSymbol = withType;
        else
            packSymbol = withType as INamedTypeSymbol;

        if (targetType is null || (packSymbol is null && adornmentSymbol is null))
            return null;

        return new MatryoshkaMetadata(
            targetType,
            Nesting: packSymbol,
            Adornment: adornmentSymbol,
            TypeName: typeNameOption,
            SourceNameSpace: @namespace,
            IsInGlobalStatement: isGlobal,
            Location: invocation.GetLocation());
    }

    private static ITypeSymbol? GetFirstTypeArgument(
        GenericNameSyntax puffSyntax,
        SemanticModel semanticModel)
    {
        var firstArg = puffSyntax.TypeArgumentList.Arguments.FirstOrDefault();

        if (firstArg is null)
            return null;

        return semanticModel.GetTypeInfo(firstArg).ConvertedType;
    }
}