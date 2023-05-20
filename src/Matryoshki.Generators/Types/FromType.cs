using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Types;

internal static class FromType
{
    public const string TypeName = "From";

    public static class Methods
    {
        public const string ExtractInterface = nameof(ExtractInterface);
    }

    public static bool TryParseInterfaceExtractionExpression(
        this ExpressionSyntax expression,
        out GenericNameSyntax targetTypeSyntax,
        out string interfaceName)
    {
        interfaceName = default!;
        targetTypeSyntax = default!;

        if (expression is not MemberAccessExpressionSyntax
            {
                Expression: GenericNameSyntax { Identifier.Text: TypeName } target,
                Name: GenericNameSyntax
                {
                    Identifier.Text: Methods.ExtractInterface
                } decoration
            })
            return false;

        targetTypeSyntax = target;
        if (decoration.TypeArgumentList.Arguments.FirstOrDefault() 
            is not SimpleNameSyntax interfaceNameSyntax)
            return false;

        interfaceName = interfaceNameSyntax.Identifier.Text;
        return true;
    }

    public static bool IsInterfaceExtractionExpression(this ExpressionSyntax expression)
    {
        return expression.TryParseInterfaceExtractionExpression(out _, out _);
    }

}