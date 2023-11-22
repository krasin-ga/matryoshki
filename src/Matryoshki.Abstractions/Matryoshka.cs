using System.Diagnostics;
using System.Reflection;

namespace Matryoshki.Abstractions;

/// <summary>
/// Starting point to define Matryoshka for <typeparamref name="T" />
/// </summary>
public class Matryoshka<T>
{
    private const string RootNamespace = "MatryoshkiGenerated";

    protected Matryoshka()
    {
    }

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
        var type = assembly.GetType(typeName) ?? GetTypeByFullyQualifiedName(assembly, typeName);
        if (type is { })
            return type;

        var stackTrace = new StackTrace();
        Assembly? lastAssembly = null;

        foreach (var frame in stackTrace.GetFrames() ?? Array.Empty<StackFrame>())
        {
            if (!frame.HasMethod())
                continue;

            var declaringType = frame.GetMethod().DeclaringType;

            if (declaringType is null)
                continue;

            var declaringTypeAssembly = declaringType.Assembly;
            if(lastAssembly == declaringTypeAssembly)
                continue;

            lastAssembly = declaringTypeAssembly;

            type = declaringTypeAssembly.GetType(typeName)
                   ?? GetTypeByFullyQualifiedName(declaringTypeAssembly, typeName);

            if (type is { })
                return type;

            if (declaringType.Namespace is null)
                continue;

            type = GetTypeByFullyQualifiedName(declaringTypeAssembly, typeName, declaringType.Namespace);

            if (type is { })
                return type;
        }

        throw new InvalidOperationException($"Type `{typeName}` was not found");
    }

    private static Type? GetTypeByFullyQualifiedName(Assembly assembly, string typeName)
        => assembly.GetType($"{assembly.GetName().Name}.{typeName}");

    private static Type? GetTypeByFullyQualifiedName(Assembly assembly, string typeName, string @namespace)
        => assembly.GetType($"{@namespace}.{typeName}");
}