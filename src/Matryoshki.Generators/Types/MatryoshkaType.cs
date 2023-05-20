using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Types;

internal static class MatryoshkaType
{
    public const string TypeName = "Matryoshka";
    public const string Alias = "Decorate";

    public static bool TryParseMatryoshkaExpression(
        this ExpressionSyntax expression,
        out GenericNameSyntax targetTypeSyntax,
        out GenericNameSyntax decorationSyntax)
    {
        if (expression is MemberAccessExpressionSyntax
            {
                Expression: GenericNameSyntax { Identifier.Text: TypeName or Alias } target,
                Name: GenericNameSyntax
                {
                    Identifier.Text: Methods.With or Methods.WithNesting
                } decoration
            })
        {
            targetTypeSyntax = target;
            decorationSyntax = decoration;

            return true;
        }

        decorationSyntax = default!;
        targetTypeSyntax = default!;
        return false;
    }

    public static bool IsMatryoshkaExpression(this ExpressionSyntax expression)
    {
        return expression.TryParseMatryoshkaExpression(out _, out _);
    }

    public static class Methods
    {
        public const string With = nameof(With);
        public const string WithNesting = nameof(WithNesting);
        public const string Name = nameof(Name);
    }
}