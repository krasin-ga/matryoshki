using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.SyntaxRewriters;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders;

internal class DecoratedPropertyBuilder
{
    private readonly AdornmentMetadata _adornmentMetadata;
    private readonly ParameterNamesFieldBuilder _parameterNamesFieldBuilder;

    public DecoratedPropertyBuilder(
        ParameterNamesFieldBuilder parameterNamesFieldBuilder,
        AdornmentMetadata adornmentMetadata)
    {
        _parameterNamesFieldBuilder = parameterNamesFieldBuilder;
        _adornmentMetadata = adornmentMetadata;
    }

    public MemberDeclarationSyntax[] GenerateDecoratedProperty(
        IPropertySymbol property,
        CancellationToken cancellationToken)
    {
        if (property.IsIndexer)
            return new MemberDeclarationSyntax[]
                   {
                       _parameterNamesFieldBuilder.CreateFieldWithParameterNames(property),
                       GenerateIndexer(property, cancellationToken)
                   };

        var propertyName = Identifier(property.Name);
        var propertyIdentifierName = IdentifierName(propertyName);

        var propertyType = property.Type.ToTypeSyntax();

        var invokeNext = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            DecoratorType.InnerFieldIdentifier,
            propertyIdentifierName
        );

        var accessors = new List<AccessorDeclarationSyntax>();
        if (property.GetMethod is { } || property.IsReadOnly)
        {
            var getBodyFactory = new StatementsRewriter(
                bodyTemplate: _adornmentMetadata.MethodTemplate,
                nextInvocationExpression: invokeNext,
                parameters: Enumerable.Empty<IParameterSymbol>(),
                decoratedSymbol: property,
                returnType: property.Type,
                parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
                returnsNothing: false,
                isAsync: false,
                isSetter: false,
                cancellationToken
            );

            accessors.Add(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(getBodyFactory.CreateBody())
            );
        }

        if (property.SetMethod is { } || property.IsWriteOnly)
        {
            var setBodyFactory = new StatementsRewriter(
                bodyTemplate: _adornmentMetadata.MethodTemplate,
                nextInvocationExpression: NothingType.FromPropertyAction(
                    DecoratorType.InnerFieldIdentifier,
                    propertyIdentifierName,
                    DecoratorType.PropertyValueIdentifier),
                parameters: property.Parameters,
                decoratedSymbol: property,
                returnType: property.Type,
                parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
                returnsNothing: true,
                isAsync: false,
                isSetter: true,
                cancellationToken
            );

            accessors.Add(
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithBody(setBodyFactory.CreateBody())
            );
        }

        var propertyDeclaration = PropertyDeclaration(propertyType, propertyName)
                                  .WithModifiers(TokenList(_adornmentMetadata.MethodTemplate.GetSymbolModifier(property)))
                                  .WithAccessorList(AccessorList(List(accessors)));

        return new MemberDeclarationSyntax[]
               {
                   _parameterNamesFieldBuilder.CreateFieldWithParameterNames(property),
                   propertyDeclaration
               };
    }

    private BasePropertyDeclarationSyntax GenerateIndexer(
        IPropertySymbol indexer,
        CancellationToken cancellationToken)
    {
        var elementType = indexer.Type.ToTypeSyntax();

        var parameters = SeparatedList(
            indexer.Parameters.Select(
                p => Parameter(p.Name.ToMatryoshkiIdentifier())
                    .WithType(p.Type.ToTypeSyntax())
            ));

        var accessors = new List<AccessorDeclarationSyntax>();
        if (indexer.GetMethod is { } || indexer.IsReadOnly)
        {
            var getterExpression = ElementAccessExpression(DecoratorType.InnerFieldIdentifier)
                .WithArgumentList(
                    BracketedArgumentList(
                        SeparatedList(indexer.Parameters.Select(p => Argument(p.Name.ToMatryoshkiIdentifierName())))));

            var getBodyFactory = new StatementsRewriter(
                bodyTemplate: _adornmentMetadata.MethodTemplate,
                nextInvocationExpression: getterExpression,
                parameters: indexer.Parameters,
                decoratedSymbol: indexer,
                returnType: indexer.Type,
                parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
                returnsNothing: false,
                isAsync: false,
                isSetter: false,
                cancellationToken
            );

            accessors.Add(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(getBodyFactory.CreateBody())
            );
        }

        if (indexer.SetMethod is { } || indexer.IsWriteOnly)
        {
            var @params = indexer.Parameters.Select(p => p.Name.ToMatryoshkiIdentifierName()).ToArray();
            var setterExpression = NothingType.FromIndexerAction(
                DecoratorType.InnerFieldIdentifier,
                DecoratorType.PropertyValueIdentifier,
                @params
            );

            var setBodyFactory = new StatementsRewriter(
                bodyTemplate: _adornmentMetadata.MethodTemplate,
                nextInvocationExpression: setterExpression,
                parameters: indexer.Parameters,
                decoratedSymbol: indexer,
                returnType: indexer.Type,
                parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
                returnsNothing: true,
                isAsync: false,
                isSetter: true,
                cancellationToken
            );

            accessors.Add(
                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithBody(setBodyFactory.CreateBody())
            );
        }
       
        return IndexerDeclaration(elementType)
               .WithModifiers(TokenList(_adornmentMetadata.MethodTemplate.GetSymbolModifier(indexer)))
               .WithParameterList(BracketedParameterList(parameters))
               .WithAccessorList(AccessorList(List(accessors)));
    }
}