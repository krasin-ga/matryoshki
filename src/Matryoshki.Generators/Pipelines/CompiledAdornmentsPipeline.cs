using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.Serialization;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;

namespace Matryoshki.Generators.Pipelines;

internal class BuiltInAdornmentsPipeline
{
    private static readonly object FakeOutput = new ();

    public IncrementalValuesProvider<AdornmentMetadata> Create(
        IncrementalGeneratorInitializationContext context)
    {
        return context.CompilationProvider
                      .Select(static (_, _) => FakeOutput) 
                      .SelectMany(static (_, ct) => GetBuiltInAdornments(ct));
    }

    private static IEnumerable<AdornmentMetadata> GetBuiltInAdornments(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        yield return PassthroughAdornment.AdornmentMetadata;
    }
}


internal static class PassthroughAdornment
{
    public static readonly AdornmentMetadata AdornmentMetadata =
        AdornmentSerializer.DeserializeAndCompile(
            AdornmentSerializer.Serialize(SourceCode),
            ClassName,
            Enumerable.Empty<MetadataReference>()
        );

    private const string ClassName = "PassthroughAdornment";
    private const string SourceCode = """
        namespace Matryoshki.BuilInAdornments;

        public class PassthroughAdornment : IAdornment
        {
            public TResult MethodTemplate<TResult>(Call<TResult> call)
            {
                return call.Forward();
            }

            public Task<TResult> AsyncMethodTemplate<TResult>(Call<TResult> call)
            {
                return call.ForwardAsync();
            }
        }

        """;
}

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

    private static AdornmentMetadata ConstructAdornmentMetadata(
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

        return AdornmentSerializer.DeserializeAndCompile(
            serializedCompilationUnit,
            className,
            attributeCompilation.Compilation?.References
            ?? Array.Empty<MetadataReference>());
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

    private readonly struct AttributeCompilation
    {
        public AttributeData Attribute { get; }

        public Compilation? Compilation { get; }

        public AttributeCompilation(
            AttributeData attribute,
            Compilation? compilation)
        {
            Attribute = attribute;
            Compilation = compilation;
        }

        public override bool Equals(object? obj)
        {
            return obj is AttributeCompilation other
                   && Attribute.Equals(other.Attribute);
        }

        public override int GetHashCode()
        {
            return Attribute.GetHashCode();
        }
    }
}