using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        return SyntaxFactory.Token(syntaxKind);
    }
}