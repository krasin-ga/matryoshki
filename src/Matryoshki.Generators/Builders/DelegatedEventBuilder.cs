using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders;

internal class DelegatedEventBuilder
{
    public EventDeclarationSyntax GenerateEventHandler(IEventSymbol eventSymbol)
    {
        var @event = EventDeclaration(
            eventSymbol.Type.ToTypeSyntax(),
            Identifier(eventSymbol.Name) 
        ).WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));

        var accessorList = new List<AccessorDeclarationSyntax>();

        if (@eventSymbol.AddMethod is { })
        {
            accessorList.Add(
                AccessorDeclaration(SyntaxKind.AddAccessorDeclaration)
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            ParseExpression($"{DecoratorType.InnerField}.{eventSymbol.Name} " +
                                            $"+= {DecoratorType.PropertyValue}")))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            );
        }

        if (@eventSymbol.RemoveMethod is { })
        {
            accessorList.Add(
                AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            ParseExpression($"{DecoratorType.InnerField}.{eventSymbol.Name} " +
                                            $"-= {DecoratorType.PropertyValue}")))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            );
        }

        return @event.WithAccessorList(AccessorList(List(accessorList)));
    }
}