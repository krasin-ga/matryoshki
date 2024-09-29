using System.Diagnostics.Contracts;
using Matryoshki.Generators.Extensions;

namespace Matryoshki.Generators.Models;

internal record struct DecoratorGenerationContext(
    MatryoshkaMetadata MatryoshkaMetadata,
    AdornmentMetadata CurrentAdornment,
    AdornmentMetadata? NextAdornment,
    bool IsStrict)
{
    private const string RootNamespace = "MatryoshkiGenerated";

    [Pure]
    public string? GetNamespace()
    {
        if (MatryoshkaMetadata.IsInGlobalStatement)
            return null;

        if (MatryoshkaMetadata.Nesting is { })
            return $"{RootNamespace}.{MatryoshkaMetadata.Nesting.Name}"; 

        return MatryoshkaMetadata.SourceNameSpace ?? RootNamespace;
    }

    [Pure]
    public string GetClassName()
    {
        return InternalGetClassName(CurrentAdornment);
    }

    [Pure]
    public string GetInnerTypeName()
    {
        if (NextAdornment is null || !IsStrict)
            return MatryoshkaMetadata.Target.GetFullName();

        return InternalGetClassName(NextAdornment.Value);
    }

    private string InternalGetClassName(AdornmentMetadata adornmentMetadata)
    {
        if (MatryoshkaMetadata.TypeName is {} typeName)
            return typeName;

        return $"{MatryoshkaMetadata.Target.Name}With{adornmentMetadata.Symbol.Name}";
    }
}