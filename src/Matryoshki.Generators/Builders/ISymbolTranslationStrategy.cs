using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Builders;

public interface ISymbolTranslationStrategy<out T>
{
    T? CreateFromMethodSymbol(IMethodSymbol methodSymbol);
    T? CreateFromPropertySymbol(IPropertySymbol propertySymbol);
    T? CreateFromFieldSymbol(IFieldSymbol fieldSymbol);
    T? CreateFromIndexerSymbol(IPropertySymbol indexerSymbol);
    T? CreateFromEventSymbol(IEventSymbol eventSymbol);
}