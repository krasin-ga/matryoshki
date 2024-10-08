﻿using Matryoshki.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders;

public class ParameterNamesFieldBuilder
{
    public FieldDeclarationSyntax CreateFieldWithParameterNames(IMethodSymbol methodSymbol)
    {
        return CreateFieldWithParameterNames(
            GetParameterNamesArrayHelperFieldIdentifier(methodSymbol),
            methodSymbol.Parameters);
    }

    public FieldDeclarationSyntax CreateFieldWithParameterNames(IPropertySymbol propertySymbol)
    {
        return CreateFieldWithParameterNames(
            GetParameterNamesArrayHelperFieldIdentifier(propertySymbol),
            propertySymbol.Parameters);
    }

    private static FieldDeclarationSyntax CreateFieldWithParameterNames(
        SyntaxToken name,
        IEnumerable<IParameterSymbol> parameters)
    {
        var arguments = parameters
                        .Select(p => p.Name.AsStringLiteralExpression())
                        .ToArray();

        var rankSpecifier = new[] { ArrayRankSpecifier() };

        var stringArrayType = ArrayType(ParseTypeName("string"), List(rankSpecifier));

        var variable = VariableDeclarator(name)
            .WithInitializer(
                EqualsValueClause(
                    ArrayCreationExpression(
                        stringArrayType,
                        InitializerExpression(
                            SyntaxKind.ArrayInitializerExpression,
                            SeparatedList(arguments)))));

        return FieldDeclaration(
                VariableDeclaration(stringArrayType).WithVariables(
                    SingletonSeparatedList(variable)))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PrivateKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.ReadOnlyKeyword)));
    }

    public SyntaxToken GetParameterNamesArrayHelperFieldIdentifier(
        ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol methodSymbol => GetParameterNamesArrayHelperFieldIdentifier(methodSymbol),
            IPropertySymbol propertySymbol => GetParameterNamesArrayHelperFieldIdentifier(propertySymbol),
            _ => throw new ArgumentOutOfRangeException(nameof(symbol))
        };
    }

    private static SyntaxToken GetParameterNamesArrayHelperFieldIdentifier(
        IMethodSymbol methodSymbol)
    {
        var trailer = string.Join(
            "_",
            methodSymbol.Parameters.Select(
                p => p.Type.GetSafeTypeName()
            ));
   
        var name = $"Method_Parameter_Names_{methodSymbol.Name}_{methodSymbol.ContainingType?.GetSafeTypeName()}";

        return Identifier(
            string.IsNullOrWhiteSpace(trailer)
                ? name
                : $"{name}_{trailer}");
    }

    private static SyntaxToken GetParameterNamesArrayHelperFieldIdentifier(
        IPropertySymbol propertySymbol)
    {
        if (propertySymbol.IsIndexer)
            return Identifier(
                $"Property_Parameter_Names" +
                $"_{propertySymbol.Type.GetSafeTypeName()}" +
                $"_{propertySymbol.ContainingType?.GetSafeTypeName()}" +
                $"_{string.Join("_", propertySymbol.Parameters.Select(p => p.Type.GetSafeTypeName()))}");

        return Identifier($"Property_Parameter_Names_{propertySymbol.Name}_{propertySymbol.ContainingType?.GetSafeTypeName()}");
    }
}