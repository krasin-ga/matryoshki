﻿using System.Diagnostics.Contracts;
using Matryoshki.Extensions;

namespace Matryoshki.Models;

internal record struct DecoratorGenerationContext(
    MatryoshkaMetadata MatryoshkaMetadata,
    AdornmentMetadata CurrentAdornment,
    AdornmentMetadata? NextAdornment)
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
        if (NextAdornment is null)
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