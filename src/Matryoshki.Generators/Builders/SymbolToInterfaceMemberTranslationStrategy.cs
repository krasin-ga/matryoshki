using Matryoshki.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Builders;

public class SymbolToInterfaceMemberTranslationStrategy : ISymbolTranslationStrategy<MemberDeclarationSyntax>
{
    public MemberDeclarationSyntax? CreateFromMethodSymbol(IMethodSymbol methodSymbol)
    {
        return methodSymbol
               .ToMethodDeclarationSyntax(
                   Enumerable.Empty<SyntaxToken>(),
                   renameParameters: false)
               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    public MemberDeclarationSyntax? CreateFromPropertySymbol(IPropertySymbol propertySymbol)
    {
        return propertySymbol.ToPropertyDeclarationSyntax(
            Enumerable.Empty<SyntaxToken>());
    }

    public MemberDeclarationSyntax? CreateFromFieldSymbol(IFieldSymbol fieldSymbol)
    {
        //TODO: Translate to property?
        return null;
    }

    public MemberDeclarationSyntax? CreateFromIndexerSymbol(IPropertySymbol indexerSymbol)
    {
        return indexerSymbol.ToIndexerDeclarationSyntax(
            Enumerable.Empty<SyntaxToken>(),
            renameIndexerParameters: false);
    }

    public MemberDeclarationSyntax? CreateFromEventSymbol(IEventSymbol eventSymbol)
    {
        return eventSymbol.ToEventDeclarationSyntax();
    }
}