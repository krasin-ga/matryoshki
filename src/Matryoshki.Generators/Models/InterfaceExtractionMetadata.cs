using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Models;

public record struct InterfaceExtractionMetadata(
    INamedTypeSymbol Target,
    string InterfaceName,
    string? Namespace)
{
}