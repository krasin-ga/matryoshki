using Microsoft.CodeAnalysis;

namespace Matryoshki.Models;

internal record struct NestingMetadata(
    ITypeSymbol Symbol,
    ITypeSymbol[] Adornments
);