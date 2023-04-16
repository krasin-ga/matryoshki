using System.Reflection;

namespace Matryoshki.Abstractions;

/// <summary>
/// Starting point to define Matryoshka for <typeparamref name="T" />
/// </summary>
/// <typeparam name="T"></typeparam>
public class Matryoshka<T>
{
    private const string RootNamespace = "MatryoshkiGenerated";

    /// <summary>
    /// Decorates <typeparamref name="T" /> with <typeparamref name="TAdornment" />
    /// </summary>
    public static MatryoshkaType With<TAdornment>()
        where TAdornment : IAdornment
    {
        var typeName = GetDefaultTypeName(typeof(TAdornment));
        return new MatryoshkaType(typeof(T), () => LocateType(Assembly.GetCallingAssembly(), typeName));
    }

    /// <summary>
    /// Decorates <typeparamref name="T" /> with <typeparamref name="TNesting" />
    /// </summary>
    /// <returns>Types in order from outer to inner</returns>
    public static MatryoshkaTypes WithNesting<TNesting>()
        where TNesting : INesting
    {
        var @namespace = $"{RootNamespace}.{typeof(TNesting).Name}";

        var interfaces = typeof(TNesting).GetInterfaces();
        var callingAssembly = Assembly.GetCallingAssembly();

        var decoratorTypes = interfaces
                             .First(t => typeof(INesting).IsAssignableFrom(t))
                             .GenericTypeArguments
                             .Select(
                                 adornmentType => LocateType(
                                     callingAssembly, 
                                     GetTypeName(@namespace, adornmentType)))
                             .ToArray();

        return new MatryoshkaTypes(typeof(T), decoratorTypes);
    }

    private static string GetTypeName(string @namespace, Type adornmentType)
    {
        return $"{@namespace}.{GetDefaultTypeName(adornmentType)}";
    }

    private static string GetDefaultTypeName(MemberInfo adornmentType)
    {
        return $"{typeof(T).Name}With{adornmentType.Name}";
    }

    private static Type LocateType(Assembly assembly, string typeName)
    {
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"Type `{typeName}` was not found");
    }
}