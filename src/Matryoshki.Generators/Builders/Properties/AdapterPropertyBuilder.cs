using Matryoshki.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Builders.Properties;

internal class AdapterPropertyBuilder : DecoratedPropertyBuilderBase
{
    public override MemberDeclarationSyntax[] GenerateDecoratedProperty(
        IPropertySymbol property,
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax,
        CancellationToken cancellationToken)
    {
        var syntaxTokens = new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) };

        if (property.IsIndexer)
        {
            const bool noRename = false;

            return new MemberDeclarationSyntax[]
                   {
                       property.ToIndexerDeclarationSyntax(
                           syntaxTokens,
                           renameIndexerParameters: noRename,
                           _ => SyntaxFactory.Block(SyntaxFactory.ReturnStatement(GetIndexerGetterExpression(property, noRename))),
                           _ => SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(GetIndexerSetterExpression(property, noRename))))
                   };
        }

        return new MemberDeclarationSyntax[]
               {
                   property.ToPropertyDeclarationSyntax(
                       syntaxTokens,
                       _ => SyntaxFactory.Block(SyntaxFactory.ReturnStatement(GetPropertyGetter(property))),
                       _ => SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(GetPropertySetter(property))))
               };
    }
}