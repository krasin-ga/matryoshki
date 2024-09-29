using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Models;

internal class MatryoshkiCompilation
{
    private readonly Compilation _compilation;
    private readonly Dictionary<string, NestingMetadata> _packsMap;
    private readonly Dictionary<string, AdornmentMetadata> _adornmentMap;

    public MatryoshkiCompilation(Compilation compilation)
    {
        _compilation = compilation;
        _packsMap = new Dictionary<string, NestingMetadata>();
        _adornmentMap = new Dictionary<string, AdornmentMetadata>();
    }

    public void AddNestingMetadata(INamedTypeSymbol packSymbol, bool isStrict)
    {
        var adornments = packSymbol
                      .AllInterfaces
                      .Where(i => i.IsImplementingInterface(NestingType.Name) && i.IsGenericType)
                      .SelectMany(i => i.TypeArguments).ToArray();

        var packMetadata = new NestingMetadata(packSymbol, adornments, isStrict);
        _packsMap[GetKey(packSymbol)] = packMetadata;
    }

    public void AddAdornmentMetadata(AdornmentMetadata adornmentMetadata)
    {
        _adornmentMap[GetKey(adornmentMetadata.Symbol)] = adornmentMetadata;
    }

    public IEnumerable<AdornmentMetadata> GetAdornments(ITypeSymbol packSymbol)
    {
        if (!_packsMap.TryGetValue(GetKey(packSymbol), out var pack))
            yield break;

        foreach (var adornmentSymbol in pack.Adornments)
            yield return _adornmentMap[GetKey(adornmentSymbol)];
    }

    public bool IsStrict(ITypeSymbol packSymbol)
    {
        return !_packsMap.TryGetValue(GetKey(packSymbol), out var pack)  || pack.IsStrict;
    }

    public AdornmentMetadata GetAdornment(ITypeSymbol adornment)
    {
        if (adornment is INamedTypeSymbol { IsGenericType: true } generic)
        {
            var root =  _adornmentMap[GetKey(generic.ConstructedFrom)];
            
            return root.Recompile(generic, _compilation);
        }

        return _adornmentMap[GetKey(adornment)];
    }

    private static string GetKey(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetFullName();
    }
}