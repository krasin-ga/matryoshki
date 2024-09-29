using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Extensions;

public static class MethodSymbolToSyntaxTranslationExtensions
{
    public static MethodDeclarationSyntax ToMethodDeclarationSyntax(
        this IMethodSymbol methodSymbol,
        IEnumerable<SyntaxToken> modifiers,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        bool renameParameters)
    {
        return MethodDeclaration(methodSymbol.ReturnType.ToTypeSyntax(), methodSymbol.Name)
            .WithExplicitInterfaceSpecifier(explicitInterfaceSpecifierSyntax)
            .WithModifiers(TokenList(modifiers))
            .WithTypeParameterList(GetTypeParameterSyntaxNode(methodSymbol))
            .WithParameterList(GetParameterSyntaxNode(methodSymbol, renameParameters));
    }

    private static TypeParameterListSyntax? GetTypeParameterSyntaxNode(IMethodSymbol method)
    {
        if (!method.TypeParameters.Any())
            return null;

        var typeParameterNodes = method.TypeParameters.Select(tp => TypeParameter(tp.Name));
        return TypeParameterList(SeparatedList(typeParameterNodes));
    }

    private static ParameterListSyntax GetParameterSyntaxNode(IMethodSymbol method, bool renameParameters)
    {
        var parameterNodes = method.Parameters.Select(
            parameter =>
            {
                var identifier = renameParameters
                    ? parameter.Name.ToMatryoshkiIdentifier()
                    : Identifier(parameter.Name);

                var parameterSyntax = Parameter(identifier)
                    .WithType(parameter.Type.ToTypeSyntax());

                if (TryCreateTokenFromRefKind(parameter.RefKind) is { } token)
                    parameterSyntax = parameterSyntax.WithModifiers(
                        SyntaxTokenList.Create(token)
                    );

                return parameterSyntax;
            }
        );

        return ParameterList(SeparatedList(parameterNodes));
    }

    private static SyntaxToken? TryCreateTokenFromRefKind(RefKind refKind)
    {
        SyntaxKind syntaxKind = refKind switch
        {
            RefKind.None => SyntaxKind.None,
            RefKind.Ref => SyntaxKind.RefKeyword,
            RefKind.Out => SyntaxKind.OutKeyword,
            RefKind.In => SyntaxKind.InKeyword,
            _ => SyntaxKind.None
        };

        if (refKind == RefKind.None)
            return null;
        return Token(syntaxKind);
    }
}