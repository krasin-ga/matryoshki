using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Builders.Methods;

internal abstract class DecoratedMethodBuilderBase 
{
    public abstract MemberDeclarationSyntax[] GenerateDecoratedMethod(
        IMethodSymbol methodSymbol,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        CancellationToken cancellationToken);

    protected static InvocationExpressionSyntax CreateInvocationExpression(
        IMethodSymbol method, 
        bool renameArguments)
    {
        SimpleNameSyntax methodName = method.IsGenericMethod
            ? SyntaxFactory.GenericName(SyntaxFactory.Identifier(method.Name),
                                        SyntaxFactory.TypeArgumentList(
                                            SyntaxFactory.SeparatedList(method.TypeParameters.Select(tp => tp.ToTypeSyntax()))))
            : SyntaxFactory.IdentifierName(method.Name);

        return SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(DecoratorType.InnerField),
                                    methodName))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(GetArgumentSyntaxNodes(method, renameArguments))));
    }

    private static IEnumerable<ArgumentSyntax> GetArgumentSyntaxNodes(
        IMethodSymbol method, 
        bool renameArguments)
    {
        return method.Parameters.Select(
            parameter =>
            {
                var identifier = renameArguments 
                        ? parameter.Name.ToMatryoshkiIdentifierName() 
                        : SyntaxFactory.IdentifierName(parameter.Name);

                return parameter.RefKind.TryCreateTokenFromRefKind() is not { } token
                    ? SyntaxFactory.Argument(identifier)
                    : SyntaxFactory.Argument(identifier).WithRefKindKeyword(token);
            }
        );
    }
}