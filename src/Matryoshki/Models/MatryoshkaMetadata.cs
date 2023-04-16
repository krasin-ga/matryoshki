using Microsoft.CodeAnalysis;

namespace Matryoshki.Models;

internal record struct MatryoshkaMetadata(
    ITypeSymbol Target,
    INamedTypeSymbol? Nesting,
    ITypeSymbol? Adornment,
    string? SourceNameSpace,
    string? TypeName,
    bool IsInGlobalStatement,
    Location Location)
{
    public AdornmentMetadata[] GetAdornments(MatryoshkiCompilation matryoshkiCompilation)
    {
        if (Nesting is { })
            return matryoshkiCompilation.GetAdornments(Nesting).ToArray();

        if (Adornment is { })
            return new[] { matryoshkiCompilation.GetAdornment(Adornment) };

        return Array.Empty<AdornmentMetadata>();
    }

    public readonly bool Equals(MatryoshkaMetadata other)
    {
        return SymbolEqualityComparer.Default.Equals(Target, other.Target)
               && SymbolEqualityComparer.Default.Equals(Nesting, other.Nesting)
               && SymbolEqualityComparer.Default.Equals(Adornment, other.Adornment)
               && SourceNameSpace == other.SourceNameSpace
               && TypeName == other.TypeName
               && IsInGlobalStatement == other.IsInGlobalStatement;
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(Target);
            hashCode = (hashCode * 397) ^ (Nesting != null ? SymbolEqualityComparer.Default.GetHashCode(Nesting) : 0);
            hashCode = (hashCode * 397) ^ (Adornment != null ? SymbolEqualityComparer.Default.GetHashCode(Adornment) : 0);
            hashCode = (hashCode * 397) ^ (SourceNameSpace != null ? SourceNameSpace.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (TypeName != null ? TypeName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ IsInGlobalStatement.GetHashCode();
            return hashCode;
        }
    }
}