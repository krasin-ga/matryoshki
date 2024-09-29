using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.SyntaxRewriters;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders.Methods;

internal class DecoratedMethodBuilder : DecoratedMethodBuilderBase
{
    private readonly AdornmentMetadata _adornmentMetadata;
    private readonly ParameterNamesFieldBuilder _parameterNamesFieldBuilder;

    public DecoratedMethodBuilder(
        ParameterNamesFieldBuilder parameterNamesFieldBuilder,
        AdornmentMetadata adornmentMetadata)
    {
        _parameterNamesFieldBuilder = parameterNamesFieldBuilder;
        _adornmentMetadata = adornmentMetadata;
    }

    public override MemberDeclarationSyntax[] GenerateDecoratedMethod(
        IMethodSymbol methodSymbol,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        CancellationToken cancellationToken)
    {
        var template = _adornmentMetadata.GetTemplate(methodSymbol);

        var modifiers = template.GetSymbolModifier(methodSymbol, explicitInterfaceSpecifierSyntax);
        var isAsync = template.HasAsyncModifier || template.NeedToConvertToAsync;

        var declaration = methodSymbol.ToMethodDeclarationSyntax(
            modifiers,
            explicitInterfaceSpecifierSyntax,
            renameParameters: true);

        ExpressionSyntax next = CreateInvocationExpression(
            methodSymbol,
            renameArguments: true);

        var returnsNothing = methodSymbol.ReturnsVoid
                             || (isAsync && methodSymbol.ReturnType.DerivesFromNonTypedTaskOrValueTask());

        var nothingMethodWrapper = returnsNothing
            ? declaration.WithIdentifier(GetVoidMethodWrapperIdentifier(declaration.Identifier))
                .WithModifiers(TokenList(isAsync
                                             ? new[] { Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.AsyncKeyword) }
                                             : new[] { Token(SyntaxKind.PrivateKeyword) }))
                .WithReturnType(isAsync
                                    ? NothingType.ValueTask
                                    : NothingType.IdentifierName)
                .WithBody(Block(ExpressionStatement(isAsync
                                                        ? AwaitExpression(next)
                                                        : next),
                                ReturnStatement(NothingType.Instance)))
            : null;

        if (nothingMethodWrapper is { })
            next = next is InvocationExpressionSyntax nextInvocationExpressionSyntax
                ? nextInvocationExpressionSyntax.WithExpression(IdentifierName(nothingMethodWrapper.Identifier))
                : CreateInvocationExpression(methodSymbol, renameArguments: true)
                    .WithExpression(IdentifierName(nothingMethodWrapper.Identifier));

        if (template.NeedToConvertToAsync)
            next = AwaitExpression(next);

        var statementsRewriter = new StatementsRewriter(
            bodyTemplate: template,
            nextInvocationExpression: next,
            parameters: methodSymbol.Parameters,
            decoratedSymbol: methodSymbol,
            returnType: methodSymbol.ReturnType,
            parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
            returnsNothing: returnsNothing,
            isAsync: isAsync,
            isSetter: false,
            cancellationToken
        );

        declaration = declaration.WithBody(statementsRewriter.CreateBody());

        var fieldWithParameterNames = _parameterNamesFieldBuilder.CreateFieldWithParameterNames(methodSymbol);

        if (nothingMethodWrapper is { })
            return new MemberDeclarationSyntax[]
            {
                fieldWithParameterNames,
                nothingMethodWrapper,
                declaration,
            };

        return new MemberDeclarationSyntax[]
        {
            fieldWithParameterNames,
            declaration,
        };
    }

    private static SyntaxToken GetVoidMethodWrapperIdentifier(SyntaxToken identifier)
    {
        return Identifier($"{identifier}NothingWrapper");
    }
}