using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Extensions;

public static class PropertySymbolToSyntaxTranslationExtensions
{
    public static PropertyDeclarationSyntax ToPropertyDeclarationSyntax(
        this IPropertySymbol property,
        IEnumerable<SyntaxToken> modifiers,
        Func<IPropertySymbol, BlockSyntax>? getterBodyFactory = null,
        Func<IPropertySymbol, BlockSyntax>? setterBodyFactory = null)
    {
        if (property.IsIndexer)
            throw new InvalidOperationException("Property is indexer");

        var accessors = CreateAccessors(property, getterBodyFactory, setterBodyFactory);

        var propertyName = SyntaxFactory.Identifier(property.Name);

        var propertyType = property.Type.ToTypeSyntax();

        return SyntaxFactory.PropertyDeclaration(propertyType, propertyName)
                            .WithModifiers(SyntaxFactory.TokenList(modifiers))
                            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
    }

    public static IndexerDeclarationSyntax ToIndexerDeclarationSyntax(
        this IPropertySymbol indexer,
        IEnumerable<SyntaxToken> modifiers,
        bool renameIndexerParameters,
        Func<IPropertySymbol, BlockSyntax>? getterBodyFactory = null,
        Func<IPropertySymbol, BlockSyntax>? setterBodyFactory = null)
    {
        if (!indexer.IsIndexer)
            throw new InvalidOperationException("Property is not indexer");

        var accessors = CreateAccessors(indexer, getterBodyFactory, setterBodyFactory);

        var elementType = indexer.Type.ToTypeSyntax();

        SeparatedSyntaxList<ParameterSyntax> parameters;
        if (renameIndexerParameters)
            parameters = SyntaxFactory.SeparatedList(
                indexer.Parameters.Select(
                    p => SyntaxFactory.Parameter(p.Name.ToMatryoshkiIdentifier())
                                      .WithType(p.Type.ToTypeSyntax())
                ));
        else
            parameters = SyntaxFactory.SeparatedList(
                indexer.Parameters.Select(
                    p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                                      .WithType(p.Type.ToTypeSyntax())
                ));

        return SyntaxFactory.IndexerDeclaration(elementType)
                            .WithModifiers(SyntaxFactory.TokenList(modifiers))
                            .WithParameterList(SyntaxFactory.BracketedParameterList(parameters))
                            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
    }

    private static List<AccessorDeclarationSyntax> CreateAccessors(
        IPropertySymbol property,
        Func<IPropertySymbol, BlockSyntax>? getterBodyFactory,
        Func<IPropertySymbol, BlockSyntax>? setterBodyFactory)
    {
        var accessors = new List<AccessorDeclarationSyntax>();

        if (property.GetMethod is { } || property.IsReadOnly)
        {
            var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);

            accessors.Add(
                getterBodyFactory is { }
                    ? SetAccessorBody(getter, getterBodyFactory(property))
                    : getter.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        if (property.SetMethod is { } || property.IsWriteOnly)
        {
            var setter = SyntaxFactory.AccessorDeclaration(
                property.SetMethod?.IsInitOnly is true
                    ? SyntaxKind.InitAccessorDeclaration
                    : SyntaxKind.SetAccessorDeclaration);

            accessors.Add(
                setterBodyFactory is { }
                    ? SetAccessorBody(setter, setterBodyFactory(property))
                    : setter.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        return accessors;
    }

    private static AccessorDeclarationSyntax SetAccessorBody(
        AccessorDeclarationSyntax syntax,
        BlockSyntax block)
    {
        if (block.Statements.Count > 1)
            return syntax.WithBody(block);

        if (block.Statements[0] is ExpressionStatementSyntax expressionStatementSyntax)
            return syntax.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(expressionStatementSyntax.Expression))
                         .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        if (block.Statements[0] is ReturnStatementSyntax { Expression: { } returnExpression })
            return syntax.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(returnExpression))
                         .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return syntax.WithBody(block);
    }
}