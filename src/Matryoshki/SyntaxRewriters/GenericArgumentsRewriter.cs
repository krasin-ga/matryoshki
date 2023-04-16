using Matryoshki.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.SyntaxRewriters;

internal class GenericArgumentsRewriter : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, TypeSyntax> _replacement;
    private readonly ClassDeclarationSyntax _targetClassDeclarationSyntax;
    private bool _isInsideTargetClass;

    public GenericArgumentsRewriter(INamedTypeSymbol generic, ClassDeclarationSyntax targetClassDeclarationSyntax)
    {
        _targetClassDeclarationSyntax = targetClassDeclarationSyntax;
        _replacement = generic.TypeParameters.Zip(generic.TypeArguments, (a, b) => (a.Name, Actual: b.ToTypeSyntax()))
                              .ToDictionary(k => k.Name, v => v.Actual);
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (!node.IsEquivalentTo(_targetClassDeclarationSyntax))
            return node;

        try
        {
            _isInsideTargetClass = true;
            return ((ClassDeclarationSyntax)base.VisitClassDeclaration(node)!)
                   .WithIdentifier(Identifier(node.Identifier.Text))
                   .WithTypeParameterList(null)
                   .WithConstraintClauses(new SyntaxList<TypeParameterConstraintClauseSyntax>());
        }
        finally
        {
            _isInsideTargetClass = false;
        }
    }

    public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    {
        if (!_isInsideTargetClass)
            return base.VisitGenericName(node);

        var modifiedList = new List<TypeSyntax>();

        foreach (var argument in node.TypeArgumentList.Arguments)
        {
            if (argument is SimpleNameSyntax name && _replacement.TryGetValue(name.Identifier.Text, out var replacement))
                modifiedList.Add(replacement);
            else
                modifiedList.Add(argument);
        }

        return node.WithTypeArgumentList(TypeArgumentList(SeparatedList(modifiedList)));
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (!_isInsideTargetClass)
            return base.VisitIdentifierName(node);

        if (_replacement.TryGetValue(node.Identifier.Text, out var replacement))
            return replacement;

        return base.VisitIdentifierName(node);
    }
}