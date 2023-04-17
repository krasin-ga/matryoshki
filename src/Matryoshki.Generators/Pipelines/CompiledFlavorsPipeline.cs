using System.Text;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Pipelines;

/// <summary>
/// Scans for attributes in all assemblies and deserialize syntax trees into AdornmentMetadata
/// </summary>
internal class CompiledAdornmentsPipeline
{
    public IncrementalValuesProvider<AdornmentMetadata> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
                      .SelectMany(GetAllAssemblies)
                      .SelectMany(GetAllAttributes)
                      .Select(ConstructAdornmentMetadata);
    }

    private AdornmentMetadata ConstructAdornmentMetadata(
        AttributeCompilation attributeCompilation,
        CancellationToken cancellationToken)
    {
        var arguments = attributeCompilation
                        .Attribute
                        .ConstructorArguments;

        if (arguments.Length == 0)
            return default;

        var className = ((string)arguments[1].Value!);
        var serializedCompilationUnit = ((string)arguments[2].Value!);

        var compilationUnitString = Encoding.UTF8.GetString(
            Convert.FromBase64String(serializedCompilationUnit));

        var compilationUnit = SyntaxFactory.ParseCompilationUnit(compilationUnitString);
        var syntaxTree = compilationUnit.SyntaxTree;
        var compilation = CSharpCompilation.Create(
            assemblyName: null,
            syntaxTrees: new[] { syntaxTree },
            references: attributeCompilation.Compilation.References
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

    private IEnumerable<AssemblyCompilation> GetAllAssemblies(
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var attributeType = compilation.GetTypeByMetadataName(CompiledAttributeType.Value);

        if (attributeType is null)
            return Enumerable.Empty<AssemblyCompilation>();

        static IEnumerable<AssemblyCompilation> ScanAssembly(
            IAssemblySymbol assembly,
            INamedTypeSymbol attributeType,
            Compilation compilation,
            HashSet<IAssemblySymbol> visited,
            CancellationToken cancellationToken)
        {
            if (!visited.Add(assembly))
                yield break;

            yield return new AssemblyCompilation(assembly, compilation, attributeType);

            foreach (var assemblyModule in assembly.Modules)
            {
                foreach (var referencedAssembly in assemblyModule.ReferencedAssemblySymbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var ra in ScanAssembly(referencedAssembly, attributeType, compilation, visited, cancellationToken))
                        yield return ra;
                }
            }
        }

        return ScanAssembly(
            compilation.Assembly,
            attributeType,
            compilation,
            new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default),
            cancellationToken);
    }

    private IEnumerable<AttributeCompilation> GetAllAttributes(
        AssemblyCompilation input,
        CancellationToken cancellationToken)
    {
        foreach (var attributeData in input.Assembly.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (input.BaseAttributeSymbol.IsAssignableFrom(attributeData.AttributeClass))
                yield return new AttributeCompilation(attributeData, input.Compilation);
        }
    }

    private record struct AssemblyCompilation(
        IAssemblySymbol Assembly,
        Compilation Compilation,
        INamedTypeSymbol BaseAttributeSymbol);

    private record struct AttributeCompilation(
        AttributeData Attribute,
        Compilation Compilation);
}