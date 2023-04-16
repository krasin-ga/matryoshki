﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Extensions;

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
}