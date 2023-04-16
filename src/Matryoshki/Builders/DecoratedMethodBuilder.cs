using System.Linq.Expressions;
using Matryoshki.Extensions;
using Matryoshki.Models;
using Matryoshki.SyntaxRewriters;
using Matryoshki.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Builders;

internal class DecoratedMethodBuilder
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

    public MemberDeclarationSyntax[] GenerateDecoratedMethodWithHelperFields(
        IMethodSymbol methodSymbol,
        CancellationToken cancellationToken)
    {
        var template = _adornmentMetadata.GetTemplate(methodSymbol);

        var modifiers = template.GetSymbolModifier(methodSymbol);
        var isAsync = template.HasAsyncModifier || template.NeedToConvertToAsync;

        var declaration = MethodDeclaration(methodSymbol.ReturnType.ToTypeSyntax(), methodSymbol.Name)
                          .WithModifiers(TokenList(modifiers))
                          .WithTypeParameterList(GetTypeParameterSyntaxNode(methodSymbol))
                          .WithParameterList(GetParameterSyntaxNode(methodSymbol));

        ExpressionSyntax next = CreateInvocationExpression(methodSymbol);

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
                         .WithBody(Block(ExpressionStatement(template.NeedToConvertToAsync
                                                                 ? AwaitExpression(next)
                                                                 : next),
                                         ReturnStatement(NothingType.Instance)))
            : null;

        if (nothingMethodWrapper is { })
            next = next is InvocationExpressionSyntax nextInvocationExpressionSyntax
                ? nextInvocationExpressionSyntax.WithExpression(IdentifierName(nothingMethodWrapper.Identifier))
                : CreateInvocationExpression(methodSymbol).WithExpression(IdentifierName(nothingMethodWrapper.Identifier));

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

    private static InvocationExpressionSyntax CreateInvocationExpression(IMethodSymbol method)
    {
        SimpleNameSyntax methodName = method.IsGenericMethod
            ? GenericName(Identifier(method.Name),
                          TypeArgumentList(
                              SeparatedList(method.TypeParameters.Select(tp => tp.ToTypeSyntax()))))
            : IdentifierName(method.Name);

        return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("_inner"),
                    methodName))
            .WithArgumentList(
                ArgumentList(SeparatedList(GetArgumentSyntaxNodes(method))));
    }

    private static TypeParameterListSyntax? GetTypeParameterSyntaxNode(IMethodSymbol method)
    {
        if (!method.TypeParameters.Any())
            return null;

        var typeParameterNodes = method.TypeParameters.Select(tp => TypeParameter(tp.Name));
        return TypeParameterList(SeparatedList(typeParameterNodes));
    }

    private static ParameterListSyntax GetParameterSyntaxNode(IMethodSymbol method)
    {
        var parameterNodes =  method.Parameters.Select(
            parameter =>
            {
                var parameterSyntax = Parameter(parameter.Name.ToMatryoshkiIdentifier())
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

    private static IEnumerable<ArgumentSyntax> GetArgumentSyntaxNodes(IMethodSymbol method)
    {
        return method.Parameters.Select(
            parameter =>
            {
                var identifier = parameter.Name.ToMatryoshkiIdentifierName();
                return TryCreateTokenFromRefKind(parameter.RefKind) is not {} token 
                    ? Argument(identifier)
                    : Argument(identifier).WithRefKindKeyword(token);
            }
        );
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

    private static SyntaxToken GetVoidMethodWrapperIdentifier(SyntaxToken identifier)
    {
        return Identifier($"{identifier}NothingWrapper");
    }
}