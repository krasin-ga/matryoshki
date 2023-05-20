using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Extensions;

internal static class MatryoshkiIdentifierExtensions
{
    public static SyntaxToken ToMatryoshkiIdentifier(this SyntaxToken identifier)
    {
        return new MatryoshkaIdentifier(identifier);
    }

    public static SyntaxToken ToMatryoshkiIdentifier(this string id)
    {
        return new MatryoshkaIdentifier(id);
    }

    public static IdentifierNameSyntax ToMatryoshkiIdentifierName(this string parameterName)
    {
        return new MatryoshkaIdentifier(parameterName);
    }

    public static IdentifierNameSyntax ToConditionalMatryoshkiIdentifierName(this string parameterName, bool condition)
    {
        if (condition)
            return new MatryoshkaIdentifier(parameterName);

        return SyntaxFactory.IdentifierName(parameterName);
    }
}