using Matryoshki.Models;
using Matryoshki.Types;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Builders;

internal class TemplateMembersFactory
{
    private readonly List<Parameter> _additionalParameters = new();
    private readonly AdornmentMetadata _adornmentMetadata;
    private readonly string _className;

    public TemplateMembersFactory(
        string className,
        AdornmentMetadata adornmentMetadata)
    {
        _className = className;
        _adornmentMetadata = adornmentMetadata;
    }

    public TemplateMembersFactory AddParameter(
        string name,
        TypeSyntax type)
    {
        _additionalParameters.Add(new Parameter(name, type));

        return this;
    }

    public IEnumerable<MemberDeclarationSyntax> GetMembers()
    {
        foreach (var constructor in GetConstructors())
            yield return constructor;

        foreach (var parameter in _additionalParameters)
        {
            yield return FieldDeclaration(
                VariableDeclaration(
                    parameter.Type,
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(parameter.FieldName)))
                )).AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
        }

        var members = _adornmentMetadata.ClassDeclaration
                                        .DescendantNodes()
                                        .OfType<MemberDeclarationSyntax>()
                                        .ToArray();

        foreach (var memberDeclarationSyntax in members)
        {
            if (memberDeclarationSyntax is ConstructorDeclarationSyntax)
                continue;

            if (memberDeclarationSyntax.IsAdornmentTemplateMethod())
                continue;

            yield return memberDeclarationSyntax;
        }
    }

    private IEnumerable<MemberDeclarationSyntax> GetConstructors()
    {
        var parameters = _additionalParameters.Select(
            p => Parameter(Identifier(p.Name))
                .WithType(p.Type)
        ).ToArray();

        var assignments = _additionalParameters.Select(
            p => (StatementSyntax)ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(p.FieldName),
                    IdentifierName(p.Name)))
        ).ToArray();

        var block = Block(assignments);

        var constructors = _adornmentMetadata.ClassDeclaration
                                             .DescendantNodes()
                                             .OfType<ConstructorDeclarationSyntax>()
                                             .ToArray();

        if (constructors.Length == 0)
        {
            yield return ConstructorDeclaration(Identifier(_className))
                         .AddParameterListParameters(parameters)
                         .AddModifiers(Token(SyntaxKind.PublicKeyword))
                         .WithBody(block);

            yield break;
        }

        foreach (var constructor in constructors)
        {
            var body = constructor.Body;
            if (constructor.Body is null && constructor.ExpressionBody is { })
            {
                body = Block(ExpressionStatement(constructor.ExpressionBody.Expression));
            }

            if (body is null)
                throw new InvalidOperationException("Cannot find constructor body");

            body = body.AddStatements(assignments);
            yield return constructor.WithIdentifier(Identifier(_className))
                                    .WithBody(body)
                                    .AddParameterListParameters(parameters);
        }
    }

    private record struct Parameter(string Name, TypeSyntax Type)
    {
        public string FieldName { get; } = $"_{Name}";
    }
}