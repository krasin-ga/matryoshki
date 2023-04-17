using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Extensions;

internal static class NamingExtensions
{
    private static readonly ThreadLocal<Regex> SafeNameRegex = new ThreadLocal<Regex>(
        () => new Regex(@"[<>`,()]|\s", RegexOptions.Compiled)
    );

    public static string GetSafeTypeName(this ITypeSymbol type)
    {
        return type.GetFullName().GetSafeName();
    }

    public static string GetSafeName(this string name)
    {
        return SafeNameRegex.Value.Replace(name, "_");
    }
}