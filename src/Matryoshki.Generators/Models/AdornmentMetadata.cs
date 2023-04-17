using System.Diagnostics.Contracts;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.SyntaxRewriters;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Models;

internal record struct AdornmentMetadata(
    ITypeSymbol Symbol,
    SyntaxTree Tree,
    ClassDeclarationSyntax ClassDeclaration,
    SemanticModel SemanticModel)
{
    public MethodTemplate MethodTemplate { get; } = new(
        FindMethod(ClassDeclaration, AdornmentType.Methods.TemplateMethodName),
        SemanticModel,
        IsAsyncTemplate: false);

    public MethodTemplate AsyncMethodTemplate { get; } = new(
        TryFindMethod(ClassDeclaration, AdornmentType.Methods.AsyncTemplateMethodName) ??
        FindMethod(ClassDeclaration, AdornmentType.Methods.TemplateMethodName),
        SemanticModel,
        IsAsyncTemplate: true);

    [Pure]
    public MethodTemplate GetTemplate(IMethodSymbol method)
    {
        return method.ReturnType.DerivesFromTaskOrValueTask()
            ? AsyncMethodTemplate
            : MethodTemplate;
    }

    [Pure]
    private static MethodDeclarationSyntax FindMethod(ClassDeclarationSyntax @class, string methodName)
    {
        return TryFindMethod(@class, methodName)
               ?? throw new InvalidOperationException($"Can't find {methodName}");
    }

    [Pure]
    private static MethodDeclarationSyntax? TryFindMethod(ClassDeclarationSyntax @class, string methodName)
    {
        return @class.Members.OfType<MethodDeclarationSyntax>()
                     .FirstOrDefault(m => m.Identifier.Text == methodName);
    }

    public AdornmentMetadata Recompile(INamedTypeSymbol generic, Compilation compilation)
    {
        var syntaxTree = new GenericArgumentsRewriter(generic, ClassDeclaration).Visit(ClassDeclaration.SyntaxTree.GetRoot()).SyntaxTree;

        var newCompilation = CSharpCompilation.Create(
            assemblyName: null,
            syntaxTrees: new[] { syntaxTree },
            references: compilation.References,
            options: compilation.Options as CSharpCompilationOptions
        );

        ClassDeclarationSyntax? updated = null;
        foreach (var descendantNode in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            if (descendantNode.Identifier.Text == ClassDeclaration.Identifier.Text)
                updated = descendantNode;

        if (updated is null)
            throw new Exception("Cannot find updated class");

        var semanticModel = newCompilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
        var declaredSymbol = semanticModel.GetDeclaredSymbol(updated) ?? throw new Exception("Cannot get declared symbol");

        return new AdornmentMetadata(declaredSymbol, syntaxTree, updated, semanticModel);
    }
}