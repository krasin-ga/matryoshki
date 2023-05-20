using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Extensions;

public static class SymbolToSyntaxTranslationExtensions
{
    public static SyntaxToken? TryCreateTokenFromRefKind(this RefKind refKind)
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
}

public static class EventSymbolToSyntaxTranslationExtensions
{
    public static EventDeclarationSyntax ToEventDeclarationSyntax(
        this IEventSymbol eventSymbol)
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
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            );
        }

        if (@eventSymbol.RemoveMethod is { })
        {
            accessorList.Add(
                AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            );
        }

        return @event.WithAccessorList(AccessorList(List(accessorList)));
    }
}

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

        var propertyName = Identifier(property.Name);

        var propertyType = property.Type.ToTypeSyntax();

        return PropertyDeclaration(propertyType, propertyName)
               .WithModifiers(TokenList(modifiers))
               .WithAccessorList(AccessorList(List(accessors)));
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
            parameters = SeparatedList(
                indexer.Parameters.Select(
                    p => Parameter(p.Name.ToMatryoshkiIdentifier())
                        .WithType(p.Type.ToTypeSyntax())
                ));
        else
            parameters = SeparatedList(
                indexer.Parameters.Select(
                    p => Parameter(Identifier(p.Name))
                        .WithType(p.Type.ToTypeSyntax())
                ));

        return IndexerDeclaration(elementType)
               .WithModifiers(TokenList(modifiers))
               .WithParameterList(BracketedParameterList(parameters))
               .WithAccessorList(AccessorList(List(accessors)));
    }

    private static List<AccessorDeclarationSyntax> CreateAccessors(
        IPropertySymbol property,
        Func<IPropertySymbol, BlockSyntax>? getterBodyFactory,
        Func<IPropertySymbol, BlockSyntax>? setterBodyFactory)
    {
        var accessors = new List<AccessorDeclarationSyntax>();

        if (property.GetMethod is { } || property.IsReadOnly)
        {
            var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);

            accessors.Add(
                getterBodyFactory is { }
                    ? SetAccessorBody(getter, getterBodyFactory(property))
                    : getter.WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
        }

        if (property.SetMethod is { } || property.IsWriteOnly)
        {
            var setter = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration);

            accessors.Add(
                setterBodyFactory is { }
                    ? SetAccessorBody(setter, setterBodyFactory(property))
                    : setter.WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
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
            return syntax.WithExpressionBody(ArrowExpressionClause(expressionStatementSyntax.Expression))
                         .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        if (block.Statements[0] is ReturnStatementSyntax { Expression: { } returnExpression })
            return syntax.WithExpressionBody(ArrowExpressionClause(returnExpression))
                         .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        return syntax.WithBody(block);
    }
}

public static class MethodSymbolToSyntaxTranslationExtensions
{
    public static MethodDeclarationSyntax ToMethodDeclarationSyntax(
        this IMethodSymbol methodSymbol,
        IEnumerable<SyntaxToken> modifiers,
        bool renameParameters)
    {
        return MethodDeclaration(methodSymbol.ReturnType.ToTypeSyntax(), methodSymbol.Name)
               .WithModifiers(TokenList(modifiers))
               .WithTypeParameterList(GetTypeParameterSyntaxNode(methodSymbol))
               .WithParameterList(GetParameterSyntaxNode(methodSymbol, renameParameters));
    }

    private static TypeParameterListSyntax? GetTypeParameterSyntaxNode(IMethodSymbol method)
    {
        if (!method.TypeParameters.Any())
            return null;

        var typeParameterNodes = method.TypeParameters.Select(tp => TypeParameter(tp.Name));
        return TypeParameterList(SeparatedList(typeParameterNodes));
    }

    private static ParameterListSyntax GetParameterSyntaxNode(IMethodSymbol method, bool renameParameters)
    {
        var parameterNodes = method.Parameters.Select(
            parameter =>
            {
                var identifier = renameParameters
                    ? parameter.Name.ToMatryoshkiIdentifier()
                    : Identifier(parameter.Name);

                var parameterSyntax = Parameter(identifier)
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
}