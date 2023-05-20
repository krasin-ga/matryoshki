using Matryoshki.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Builders.Methods;

internal class AdapterMethodBuilder : DecoratedMethodBuilderBase
{
    public override MemberDeclarationSyntax[] GenerateDecoratedMethod(
        IMethodSymbol methodSymbol, 
        CancellationToken cancellationToken)
    {
        var inner = CreateInvocationExpression(methodSymbol, renameArguments: false);

        return new MemberDeclarationSyntax[]
               {
                   methodSymbol.ToMethodDeclarationSyntax(
                                   modifiers: new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) },
                                   renameParameters: false)
                               .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(inner))
                               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
               };
    }
}