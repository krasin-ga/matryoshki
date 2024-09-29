using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders.Properties;

internal abstract class DecoratedPropertyBuilderBase
{
    public abstract MemberDeclarationSyntax[] GenerateDecoratedProperty(
        IPropertySymbol property,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        CancellationToken cancellationToken);

    protected static MemberAccessExpressionSyntax GetPropertyGetter(
        IPropertySymbol property)
    {
        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            DecoratorType.InnerFieldIdentifier,
            IdentifierName(property.Name)
        );
    }

    protected static AssignmentExpressionSyntax GetPropertySetter(
        IPropertySymbol property)
    {
        return AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            GetPropertyGetter(property),
            DecoratorType.PropertyValueIdentifier);
    }

    protected static AssignmentExpressionSyntax GetIndexerSetterExpression(
        IPropertySymbol symbol,
        bool renameParameters)
    {
        return AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            ElementAccessExpression(DecoratorType.InnerFieldIdentifier)
                .WithArgumentList(
                    BracketedArgumentList(
                        GetParameters(symbol, renameParameters))),
            DecoratorType.PropertyValueIdentifier);
    }

    private static SeparatedSyntaxList<ArgumentSyntax> GetParameters(
        IPropertySymbol property,
        bool rename)
    {
        if (rename)
            return SyntaxFactory.SeparatedList(
                property.Parameters.Select(k => Argument(k.Name.ToMatryoshkiIdentifierName())));

        return SyntaxFactory.SeparatedList(
            property.Parameters.Select(k => Argument(IdentifierName(k.Name))));
    }

    protected static ElementAccessExpressionSyntax GetIndexerGetterExpression(
        IPropertySymbol symbol,
        bool renameParameters)
    {
        return ElementAccessExpression(DecoratorType.InnerFieldIdentifier)
            .WithArgumentList(
                BracketedArgumentList(GetParameters(symbol, renameParameters)));
    }
}