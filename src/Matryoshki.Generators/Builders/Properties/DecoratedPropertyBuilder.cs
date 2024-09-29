using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.SyntaxRewriters;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders.Properties;

internal class DecoratedPropertyBuilder : DecoratedPropertyBuilderBase
{
    private readonly AdornmentMetadata _adornmentMetadata;
    private readonly TypeSyntax _targetType;
    private readonly ParameterNamesFieldBuilder _parameterNamesFieldBuilder;

    public DecoratedPropertyBuilder(
        ParameterNamesFieldBuilder parameterNamesFieldBuilder,
        AdornmentMetadata adornmentMetadata,
        TypeSyntax targetType)
    {
        _parameterNamesFieldBuilder = parameterNamesFieldBuilder;
        _adornmentMetadata = adornmentMetadata;
        _targetType = targetType;
    }

    public override MemberDeclarationSyntax[] GenerateDecoratedProperty(
        IPropertySymbol property,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        CancellationToken cancellationToken)
    {
        if (property.IsIndexer)
            return new MemberDeclarationSyntax[]
                   {
                       _parameterNamesFieldBuilder.CreateFieldWithParameterNames(property),
                       GenerateIndexer(property, explicitInterfaceSpecifierSyntax, cancellationToken)
                   };

        var propertyIdentifierName = IdentifierName(property.Name);

        var invokeNext = GetPropertyGetter(property);

        var (initOnlySettingActionFieldName, initOnlySettingActionFieldSyntax) =
            property.SetMethod?.IsInitOnly is true
            ? CreateSetterActionField(property, _targetType, property.Type.ToTypeSyntax())
            : default;

        var propertyDeclaration = property.ToPropertyDeclarationSyntax(
            _adornmentMetadata.MethodTemplate.GetSymbolModifier(property, explicitInterfaceSpecifierSyntax),
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
                nextInvocationExpression:
                initOnlySettingActionFieldName is null 
                ? NothingType.FromPropertyAction(
                    DecoratorType.InnerFieldIdentifier,
                    propertyIdentifierName,
                    DecoratorType.PropertyValueIdentifier)
                : NothingType.FromInitOnlyPropertyAction(
                    DecoratorType.InnerFieldIdentifier,
                    initOnlySettingActionFieldName, 
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

        if (explicitInterfaceSpecifierSyntax is { })
            propertyDeclaration = propertyDeclaration.WithExplicitInterfaceSpecifier(
                explicitInterfaceSpecifierSyntax);

        if (initOnlySettingActionFieldSyntax is { })
        {
            return new MemberDeclarationSyntax[]
                   {
                       initOnlySettingActionFieldSyntax,
                       _parameterNamesFieldBuilder.CreateFieldWithParameterNames(property),
                       propertyDeclaration
                   };
        }

        return new MemberDeclarationSyntax[]
               {
                   _parameterNamesFieldBuilder.CreateFieldWithParameterNames(property),
                   propertyDeclaration
               };
    }

    private (string FieldName, FieldDeclarationSyntax Syntax) CreateSetterActionField(
        IPropertySymbol property,
        TypeSyntax type,
        TypeSyntax propertyType)
    {
        LambdaExpressionSyntax lambdaExpression = SimpleLambdaExpression(
            Parameter(Identifier("prp"))
                .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword))),
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("prp"),
                IdentifierName(property.Name)
            )
        );

        var typeArgumentsList = TypeArgumentList(
            SeparatedList<TypeSyntax>(
                new SyntaxNodeOrToken[]
                {
                    type,
                    Token(SyntaxKind.CommaToken),
                    propertyType
                }
            )
        );

        var methodInvocation = InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                ParseName("Matryoshki.Abstractions.Assignment"),
                GenericName(Identifier("CreateAssignmentAction"))
                    .WithTypeArgumentList(typeArgumentsList))
        ).WithArgumentList(
            ArgumentList(SingletonSeparatedList(Argument(lambdaExpression))));

        var fieldName = $"InitOnlyPropertySetterFor{property.Name}";

        var fieldDeclarationSyntax = FieldDeclaration(
                VariableDeclaration(
                        GenericName(Identifier("Action")).WithTypeArgumentList(typeArgumentsList))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(Identifier(fieldName))
                                .WithInitializer(EqualsValueClause(methodInvocation)))))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword)
                )
            );
        return (fieldName, fieldDeclarationSyntax);
    }

    private BasePropertyDeclarationSyntax GenerateIndexer(
        IPropertySymbol indexer,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        CancellationToken cancellationToken)
    {
        var indexerDeclarationSyntax = indexer.ToIndexerDeclarationSyntax(
            modifiers: _adornmentMetadata.MethodTemplate.GetSymbolModifier(indexer, explicitInterfaceSpecifierSyntax),
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

        if (explicitInterfaceSpecifierSyntax is { })
            return indexerDeclarationSyntax.WithExplicitInterfaceSpecifier(
                explicitInterfaceSpecifierSyntax);

        return indexerDeclarationSyntax;
    }
}