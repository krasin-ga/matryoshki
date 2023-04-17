using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Extensions;

internal static class SyntaxExtensions
{
    internal static ExpressionSyntax AsStringLiteralExpression(this string value)
    {
        return LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            Literal(value)
        );
    }

    internal static ObjectCreationExpressionSyntax CreateNew(
        this TypeSyntax typeSyntax,
        params ExpressionSyntax[] args)
    {
        return ObjectCreationExpression(typeSyntax)
            .WithArgumentList(
                ArgumentList(
                    SeparatedList(
                        args.Select(a => Argument(a))
                    )));
    }

    /// <summary>
    /// Array.Empty
    /// </summary>
    internal static ExpressionSyntax EmptyArray(
        this TypeSyntax typeSyntax)
    {
        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Array"),
                GenericName(Identifier("Empty"))
                    .WithTypeArgumentList(
                        TypeArgumentList(SingletonSeparatedList(typeSyntax)))));
    }

    /// <summary>
    ///  new t[]{a1, a2, ..., aN} or Array.Empty
    /// </summary>
    internal static ExpressionSyntax InitializedArray(
        this TypeSyntax arrayElementType,
        params ExpressionSyntax[] args)
    {
        return InitializedArray(
            arrayElementType, 
            (IReadOnlyCollection<ExpressionSyntax>)args);
    }

    /// <summary>
    ///  new t[]{a1, a2, ..., aN} or Array.Empty
    /// </summary>
    internal static ExpressionSyntax InitializedArray(
        this TypeSyntax arrayElementType,
        IReadOnlyCollection<ExpressionSyntax> args)
    {
        if (args.Count == 0)
            return arrayElementType.EmptyArray();

        var rankSpecifier = new[] { ArrayRankSpecifier() };

        return ArrayCreationExpression(
            ArrayType(arrayElementType, List(rankSpecifier)),
            InitializerExpression(SyntaxKind.ArrayInitializerExpression,
                                  SeparatedList(args)));
    }

    internal static AssignmentExpressionSyntax ToDiscardVariable(
        this ExpressionSyntax expression)
    {
        return AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName("_"),
                                    expression);
    }

    internal static bool CanBeUsedAsStatement(this ExpressionSyntax expression)
    {
        return expression is AssignmentExpressionSyntax
            or InvocationExpressionSyntax
            or AwaitExpressionSyntax
            or ObjectCreationExpressionSyntax
            or PostfixUnaryExpressionSyntax;
    }

    public static TypeSyntax ToTypeSyntax(this ITypeSymbol symbol)
    {
        return ParseTypeName(symbol.ToDisplayString());
    }

    public static SyntaxToken ToSyntaxToken(this Accessibility accessibility)
    {
        var syntaxKind =  accessibility switch
        {
            Accessibility.Public => SyntaxKind.PublicKeyword,
            Accessibility.Internal => SyntaxKind.InternalKeyword,
            Accessibility.Protected => SyntaxKind.ProtectedKeyword,
            Accessibility.Private => SyntaxKind.PrivateKeyword,
            Accessibility.ProtectedOrInternal => SyntaxKind.ProtectedKeyword,
            Accessibility.ProtectedAndInternal => SyntaxKind.PrivateKeyword,
            _ => SyntaxKind.PublicKeyword
        };

        return Token(syntaxKind);
    }

    public static bool UnsafeEquals(this TypeSyntax a, TypeSyntax b)
    {
        return a.Span.Length == b.Span.Length
            && a.ToString() == b.ToString();
    }
}