using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Extensions;

/// <summary>
/// Identifier is used to reduce chance of conflict between
/// IAdornment template and target interface members and method parameters
/// </summary>
internal readonly struct MatryoshkaIdentifier
{
    private readonly string _identifier;

    public MatryoshkaIdentifier(string identifier)
    {
        _identifier = identifier;
    }

    public MatryoshkaIdentifier(SyntaxToken syntaxToken)
    {
        _identifier = syntaxToken.Text;
    }

    public static implicit operator IdentifierNameSyntax(MatryoshkaIdentifier matryoshkaIdentifier)
    {
        return SyntaxFactory.IdentifierName(ModifyIdentifier(matryoshkaIdentifier._identifier));
    }

    public static implicit operator SyntaxToken(MatryoshkaIdentifier matryoshkaIdentifier)
    {
        return SyntaxFactory.Identifier(ModifyIdentifier(matryoshkaIdentifier._identifier));
    }

    private static string ModifyIdentifier(string parameterName)
    {
        return parameterName + "_Δ";
    }
}