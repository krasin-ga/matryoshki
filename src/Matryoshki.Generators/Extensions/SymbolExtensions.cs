using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Extensions;

internal static class SymbolExtensions
{
    private static INamedTypeSymbol? _taskSymbol;
    private static INamedTypeSymbol? _valueTaskSymbol;
    private static INamedTypeSymbol? _genericValueTaskSymbol;

    public static string GetFullName(this ISymbol type)
    {
        return type.ToDisplayString();
    }

    public static bool DerivesFromTaskOrValueTask(this ITypeSymbol typeSymbol)
    {
        var task = _taskSymbol ??= typeSymbol.ContainingAssembly.GetTypeByMetadataName(typeof(Task).FullName);
        var valueTask = _valueTaskSymbol ??= typeSymbol.ContainingAssembly.GetTypeByMetadataName(typeof(ValueTask).FullName);
        var valueTaskTyped = _genericValueTaskSymbol ??= typeSymbol.ContainingAssembly.GetTypeByMetadataName(typeof(ValueTask<>).FullName);

        if (task is null || valueTask is null)
            return false;

        return IsAssignableFrom(task, typeSymbol)
               || SymbolEqualityComparer.Default.Equals(valueTask, typeSymbol)
               || SymbolEqualityComparer.Default.Equals(valueTaskTyped, (typeSymbol as INamedTypeSymbol)?.ConstructedFrom);
    }

    public static bool DerivesFromNonTypedTaskOrValueTask(this ITypeSymbol typeSymbol)
    {
        var task = _taskSymbol ??= typeSymbol.ContainingAssembly.GetTypeByMetadataName(typeof(Task).FullName);
        var valueTask = _valueTaskSymbol ??= typeSymbol.ContainingAssembly.GetTypeByMetadataName(typeof(ValueTask).FullName);

        if (task is null || valueTask is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(valueTask, typeSymbol)
               || SymbolEqualityComparer.Default.Equals(task, typeSymbol);
    }

    public static bool IsAssignableFrom(
        this ITypeSymbol? targetType,
        ITypeSymbol? sourceType,
        bool exactMatch = false)
    {
        if (targetType is null)
            return false;

        if (targetType.SpecialType == SpecialType.System_Object)
            return true;

        if (exactMatch)
            return SymbolEqualityComparer.Default.Equals(sourceType, targetType);

        while (sourceType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
                return true;

            if (targetType.TypeKind == TypeKind.Interface)
                return sourceType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetType));

            sourceType = sourceType.BaseType;
        }

        return false;
    }

    public static IEnumerable<ISymbol> GetAllInterfaceMembers(
        this ITypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
            yield return member;

        foreach (var @interface in typeSymbol.AllInterfaces)
        foreach (var member in @interface.GetMembers())
            yield return member;
    }

    public static IEnumerable<ISymbol> GetMembersThatCanBeDecorated(
        this ITypeSymbol type,
        HashSet<ISymbol>? except = null)
    {
        static bool IsSuitable(ISymbol symbol, HashSet<ISymbol> alreadyAdded)
        {
            if (alreadyAdded.Contains(symbol)
                || alreadyAdded.Contains(symbol.OriginalDefinition))
                return false;

            if(symbol.IsSealed)
                return false;


            if (symbol is IPropertySymbol { OverriddenProperty: { } overriddenProperty })
                symbol = overriddenProperty;

            if (symbol is IMethodSymbol { OverriddenMethod: { } overriddenMethod })
                symbol = overriddenMethod;

            return (symbol.IsVirtual || symbol.IsAbstract)
                   && symbol is IEventSymbol or IPropertySymbol or IMethodSymbol;
        }

        except ??= new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        if (type.TypeKind is TypeKind.Interface)
        {
            foreach (var member in type.GetAllInterfaceMembers())
                yield return member;

            yield break;
        }

        foreach (var member in type.GetMembers())
        {
            if (IsSuitable(member, except))
                yield return member;

            if (member is IPropertySymbol { OverriddenProperty: { } overriddenProperty })
                except.Add(overriddenProperty);

            if (member is IMethodSymbol { OverriddenMethod: { } overriddenMethod })
                except.Add(overriddenMethod);
        }
          

        if (type.BaseType is { SpecialType: not SpecialType.System_Object })
            foreach (var member in GetMembersThatCanBeDecorated(type.BaseType, except))
                    yield return member;
    }

    public static bool IsImplementingInterface(
        this INamedTypeSymbol typeSymbol,
        string interfaceName)
    {
        foreach (var interfaceSymbol in typeSymbol.AllInterfaces)
            if (interfaceSymbol.Name.Equals(interfaceName))
                return true;

        return false;
    }

    public static bool NeedToOverride(this ISymbol symbol)
    {
        if (symbol.ContainingType.TypeKind == TypeKind.Interface)
            return false;

        return symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride;
    }
}