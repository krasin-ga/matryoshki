using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Builders;

public static class SymbolTranslationStrategyExtensions
{
    public static T? Translate<T>(this ISymbolTranslationStrategy<T> translationStrategy, ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol methodSymbol
                => translationStrategy.CreateFromMethodSymbol(methodSymbol),

            IPropertySymbol { IsIndexer: true } propertySymbol
                => translationStrategy.CreateFromIndexerSymbol(propertySymbol),

            IPropertySymbol propertySymbol
                => translationStrategy.CreateFromPropertySymbol(propertySymbol),

            IFieldSymbol fieldSymbol
                => translationStrategy.CreateFromFieldSymbol(fieldSymbol),

            IEventSymbol eventSymbol
                => translationStrategy.CreateFromEventSymbol(eventSymbol),

            _ => default
        };
    }
}