using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.SyntaxRewriters;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders.Properties;

internal class DecoratedPropertyBuilder: DecoratedPropertyBuilderBase
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

    public override MemberDeclarationSyntax[] GenerateDecoratedProperty(
        IPropertySymbol property,
        CancellationToken cancellationToken)
    {
        if (property.IsIndexer)
            return new MemberDeclarationSyntax[]
                   {
                       _parameterNamesFieldBuilder.CreateFieldWithParameterNames(property),
                       GenerateIndexer(property, cancellationToken)
                   };

        var propertyIdentifierName = IdentifierName(property.Name);

        var invokeNext = GetPropertyGetter(property);

        var propertyDeclaration = property.ToPropertyDeclarationSyntax(
            _adornmentMetadata.MethodTemplate.GetSymbolModifier(property),
            _ => new StatementsRewriter(
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
            ).CreateBody(),
            _ => new StatementsRewriter(
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
            ).CreateBody());

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

        return indexer.ToIndexerDeclarationSyntax(
            modifiers: _adornmentMetadata.MethodTemplate.GetSymbolModifier(indexer),
            renameIndexerParameters: true,
            getterBodyFactory:
            symbol => new StatementsRewriter(
                bodyTemplate: _adornmentMetadata.MethodTemplate,
                nextInvocationExpression: GetIndexerGetterExpression(symbol, renameParameters: true),
                parameters: indexer.Parameters,
                decoratedSymbol: indexer,
                returnType: indexer.Type,
                parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
                returnsNothing: false,
                isAsync: false,
                isSetter: false,
                cancellationToken
            ).CreateBody(),
            setterBodyFactory:
            symbol =>
                new StatementsRewriter(
                    bodyTemplate: _adornmentMetadata.MethodTemplate,
                    nextInvocationExpression: NothingType.FromIndexerAction(
                        DecoratorType.InnerFieldIdentifier,
                        DecoratorType.PropertyValueIdentifier,
                        symbol.Parameters.Select(p => p.Name.ToMatryoshkiIdentifierName()).ToArray()
                    ),
                    parameters: indexer.Parameters,
                    decoratedSymbol: indexer,
                    returnType: indexer.Type,
                    parameterNamesFieldBuilder: _parameterNamesFieldBuilder,
                    returnsNothing: true,
                    isAsync: false,
                    isSetter: true,
                    cancellationToken
                ).CreateBody()
        );
    }
}