using Matryoshki.Types;
using Microsoft.CodeAnalysis;

namespace Matryoshki;

internal class CoreMatryoshkiSymbols
{
    public INamedTypeSymbol Adornment { get; }
    public INamedTypeSymbol Pack { get; }

    public CoreMatryoshkiSymbols(
        INamedTypeSymbol adornment,
        INamedTypeSymbol pack)
    {
        Adornment = adornment;
        Pack = pack;
    }

    public static bool TryCreate(Compilation compilation, out CoreMatryoshkiSymbols coreMatryoshkiSymbols)
    {
        coreMatryoshkiSymbols = default!;

        var adornmentType = compilation.GetTypeByMetadataName(AdornmentType.FullName);
        var nestingType = compilation.GetTypeByMetadataName(NestingType.FullName);

        if (adornmentType is null || nestingType is null)
            return false;

        coreMatryoshkiSymbols = new CoreMatryoshkiSymbols(
            adornmentType,
            nestingType
        );

        return true;
    }

    public static CoreMatryoshkiSymbols? Create(Compilation compilation)
    {
        TryCreate(compilation, out var symbols);
        return symbols;
    }

    private bool Equals(CoreMatryoshkiSymbols other)
    {
        var equalityComparer = SymbolEqualityComparer.Default;

        return equalityComparer.Equals(Adornment, other.Adornment)
               && equalityComparer.Equals(Pack, other.Pack);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var equalityComparer = SymbolEqualityComparer.Default;

            return (equalityComparer.GetHashCode(Adornment) * 397) ^ equalityComparer.GetHashCode(Pack);
        }
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != this.GetType())
            return false;
        return Equals((CoreMatryoshkiSymbols)obj);
    }
}