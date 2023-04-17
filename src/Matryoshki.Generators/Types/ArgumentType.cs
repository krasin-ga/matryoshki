using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Types;

internal static class ArgumentType
{
    public static readonly SyntaxToken Identifier
        = SyntaxFactory.Identifier("Matryoshki.Abstractions.Argument");

    public static GenericNameSyntax Of(TypeSyntax genericArgument)
    {
        return SyntaxFactory.GenericName(
            Identifier,
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList(new[] { genericArgument })));
    }
}