using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Models;

internal record struct NestingMetadata(
    ITypeSymbol Symbol,
    ITypeSymbol[] Adornments
);