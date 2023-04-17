using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Types;

internal static class AdornmentType
{
    public const string Name = "IAdornment";
    public const string FullName = "Matryoshki.Abstractions.IAdornment";

    public static bool IsAdornmentClassDeclaration(
        this SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { BaseList: { } baseList } @class
               && baseList.Types.Any(
                   t => t is SimpleBaseTypeSyntax { Type: IdentifierNameSyntax { Identifier.Text: Name } }
                       or SimpleBaseTypeSyntax { Type: QualifiedNameSyntax { Right.Identifier.Text: Name } })
               && @class.Members.OfType<MethodDeclarationSyntax>().Count(
                   m => m.IsAdornmentTemplateMethod()) >= 1;
    }

    public static bool IsAdornmentTemplateMethod(this MemberDeclarationSyntax memberDeclarationSyntax)
    {
        return memberDeclarationSyntax is MethodDeclarationSyntax
               {
                   Identifier.Text: (Methods.TemplateMethodName or Methods.AsyncTemplateMethodName),
                   TypeParameterList.Parameters.Count: 1
               } methodDeclarationSyntax
               && methodDeclarationSyntax.ParameterList.Parameters.Any(p => p.Type is GenericNameSyntax
               {
                   Identifier.Text: CallType.TypeName
               });
    }

    public static class Methods
    {
        public const string TemplateMethodName = "MethodTemplate";
        public const string AsyncTemplateMethodName = "AsyncMethodTemplate";
    }
}