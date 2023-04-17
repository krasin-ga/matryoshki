using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.SyntaxRewriters;

internal class AdornmentRewriter : CSharpSyntaxRewriter
{
    private readonly CancellationToken _cancellationToken;
    private readonly SemanticModel _semanticModel;
    private readonly ClassDeclarationSyntax _targetClassDeclaration;
    private readonly HashSet<SyntaxNode> _usagesHashset;

    public AdornmentRewriter(
        SemanticModel semanticModel,
        ClassDeclarationSyntax targetClassDeclaration,
        CancellationToken cancellationToken)
    {
        _semanticModel = semanticModel;
        _targetClassDeclaration = targetClassDeclaration;
        _cancellationToken = cancellationToken;
        var classSymbol = _semanticModel.GetDeclaredSymbol(targetClassDeclaration);

        _usagesHashset = new HashSet<SyntaxNode>(
            FindUsagesOfTargetClassMembers(targetClassDeclaration, classSymbol));
    }

    private IEnumerable<SyntaxNode> FindUsagesOfTargetClassMembers(
        ClassDeclarationSyntax targetClassDeclaration,
        INamedTypeSymbol? classSymbol)
    {
        foreach (var descendantNode in targetClassDeclaration.DescendantNodes())
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var symbol = _semanticModel.GetSymbolInfo(descendantNode).Symbol;
            if (symbol is null)
                continue;

            var isSuitable = symbol is IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol
                             && SymbolEqualityComparer.Default.Equals(symbol.ContainingSymbol, classSymbol);

            if (!isSuitable)
                continue;

            yield return descendantNode;
        }
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (_targetClassDeclaration != node)
            return node;

        var visited = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;

        var members = visited.Members.Select(
            member =>
            {
                return member switch
                {
                    FieldDeclarationSyntax field => field.WithDeclaration(
                        field.Declaration.WithVariables(
                            SyntaxFactory.SeparatedList(
                                field.Declaration.Variables.Select(
                                    variable => variable.WithIdentifier(
                                        NewIdentifier(variable.Identifier)))))),
                    PropertyDeclarationSyntax property => property.WithIdentifier(NewIdentifier(property.Identifier)),
                    MethodDeclarationSyntax method when !method.IsAdornmentTemplateMethod()
                        => method.WithIdentifier(NewIdentifier(method.Identifier)),
                    EventDeclarationSyntax @event => @event.WithIdentifier(NewIdentifier(@event.Identifier)),
                    _ => member
                };
            });

        return visited.WithMembers(SyntaxFactory.List(members));
    }

    public override SyntaxNode? VisitTypeOfExpression(TypeOfExpressionSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var type = _semanticModel.GetTypeInfo(node.Type).ConvertedType;
        if (type is null)
            return base.VisitTypeOfExpression(node);

        return SyntaxFactory.TypeOfExpression(type.ToTypeSyntax());
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!_usagesHashset.Remove(node))
            return base.VisitIdentifierName(node);

        return node.WithIdentifier(NewIdentifier(node.Identifier));
    }

    public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!_usagesHashset.Remove(node))
            return base.VisitGenericName(node);

        return node.WithIdentifier(NewIdentifier(node.Identifier));
    }

    private static SyntaxToken NewIdentifier(SyntaxToken nodeIdentifier)
    {
        return SyntaxFactory.Identifier($"{nodeIdentifier.Text}_Δ");
    }
}