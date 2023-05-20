using System.Text;
using Matryoshki.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Serialization;

internal static class AdornmentSerializer
{
    public static string Serialize(string compilationUnit)
    {
        var compilationUnitBytes = Encoding.UTF8.GetBytes(compilationUnit);
        return Convert.ToBase64String(compilationUnitBytes);
    }

    public static AdornmentMetadata DeserializeAndCompile(
        string serializedCompilationUnit,
        string className,
        IEnumerable<MetadataReference> metadataReferences)
    {
        var compilationUnitString = Encoding.UTF8.GetString(
            Convert.FromBase64String(serializedCompilationUnit));

        var compilationUnit = SyntaxFactory.ParseCompilationUnit(compilationUnitString);
        var syntaxTree = compilationUnit.SyntaxTree;
        var compilation = CSharpCompilation.Create(
            assemblyName: null,
            syntaxTrees: new[] { syntaxTree },
            references: metadataReferences
        );

        var @class = syntaxTree
                     .GetRoot()
                     .DescendantNodes()
                     .OfType<ClassDeclarationSyntax>()
                     .Single(c => c.Identifier.Text == className);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var declaredSymbol = semanticModel.GetDeclaredSymbol(@class);

        return new AdornmentMetadata(
            declaredSymbol!,
            syntaxTree,
            @class,
            semanticModel);
    }
}