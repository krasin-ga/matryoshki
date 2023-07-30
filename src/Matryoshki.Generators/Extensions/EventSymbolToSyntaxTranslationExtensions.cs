using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Extensions;

public static class EventSymbolToSyntaxTranslationExtensions
{
    public static EventDeclarationSyntax ToEventDeclarationSyntax(
        this IEventSymbol eventSymbol)
    {
        var @event = SyntaxFactory.EventDeclaration(
            eventSymbol.Type.ToTypeSyntax(),
            SyntaxFactory.Identifier(eventSymbol.Name)
        ).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

        var accessorList = new List<AccessorDeclarationSyntax>();

        if (@eventSymbol.AddMethod is { })
        {
            accessorList.Add(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.AddAccessorDeclaration)
                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );
        }

        if (@eventSymbol.RemoveMethod is { })
        {
            accessorList.Add(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );
        }

        return @event.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessorList)));
    }
}