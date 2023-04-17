using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Models;

internal record struct MethodTemplate(
    MethodDeclarationSyntax Syntax, 
    SemanticModel SemanticModel,
    bool IsAsyncTemplate)
{
    public SyntaxToken ParameterIdentifier { get; } = Syntax.ParameterList.Parameters.Single().Identifier;
    public SyntaxToken TypeParameterIdentifier { get; } = GetTypeParameter(Syntax).Identifier;

    public bool NeedToConvertToAsync { get; }
        = IsAsyncTemplate && Syntax.Identifier.Text != AdornmentType.Methods.AsyncTemplateMethodName;

    public bool HasAsyncModifier { get; } = IsAsyncTemplate && Syntax.Modifiers.Any(m => m.Value?.Equals("async") is true);

    private static TypeParameterSyntax GetTypeParameter(MethodDeclarationSyntax syntax)
    {
        return syntax.TypeParameterList!.Parameters.Single();
    }


    public IReadOnlyCollection<SyntaxToken> GetSymbolModifier(ISymbol methodSymbol)
    {
        var modifiers = new List<SyntaxToken>(3) { methodSymbol.DeclaredAccessibility.ToSyntaxToken() };

        if (methodSymbol.NeedToOverride())
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));

        var isAsync = HasAsyncModifier || NeedToConvertToAsync;

        if (isAsync)
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

        return modifiers;
    }
}