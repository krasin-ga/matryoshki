using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Types;

internal static class DecoratorType
{
    public const string InnerField = "_inner";
    public const string InnerParameter = "inner";
    public const string PropertyValue = "value";

    public static readonly IdentifierNameSyntax InnerFieldIdentifier = SyntaxFactory.IdentifierName(InnerField);
    public static readonly IdentifierNameSyntax PropertyValueIdentifier = SyntaxFactory.IdentifierName(PropertyValue);
}